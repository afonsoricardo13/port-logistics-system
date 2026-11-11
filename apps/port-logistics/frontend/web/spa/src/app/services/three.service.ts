import { Injectable } from '@angular/core';
import * as THREE from 'three';
import * as TWEEN from '@tweenjs/tween.js';
import { GLTFLoader } from 'three/examples/jsm/loaders/GLTFLoader.js';
import { PortLayoutDTO, PortElementDTO, Dimensions3D, Position3D, Rotation3D } from './port-layout.service';
import { DockService } from './dock.service';
import { VesselService } from './vessel.service';
import { PhysicalResourceService } from './physical-resource.service';
import { StorageAreaService } from './storage-area.service';

@Injectable({
  providedIn: 'root'
})
export class ThreeService {
  private scene!: THREE.Scene;
  private camera!: THREE.PerspectiveCamera;
  private renderer!: THREE.WebGLRenderer;
  private animationFrameId: number | null = null;
  private portElements: THREE.Object3D[] = [];

  // GLTFLoader for loading 3D models
  private gltfLoader = new GLTFLoader();
  private modelCache = new Map<string, THREE.Group>();

  // US 3.3.6: Camera control properties
  private cameraTarget = new THREE.Vector3(0, 0, 0); // Point camera orbits around
  private cameraDistance = 200; // Distance from target
  private cameraAzimuth = Math.PI / 4; // Horizontal angle (radians)
  private cameraElevation = Math.PI / 6; // Vertical angle (radians)

  // US 4.2.7: Default camera position for reset
  private defaultCameraTarget = new THREE.Vector3(0, 0, 0);
  private defaultCameraDistance = 200;
  private defaultCameraAzimuth = Math.PI / 4;
  private defaultCameraElevation = Math.PI / 6;

  // Night mode properties
  private isNightMode = false;
  private dayAmbientLight!: THREE.AmbientLight;
  private dayDirectionalLight!: THREE.DirectionalLight;
  private moonLight!: THREE.DirectionalLight;
  private moon: THREE.Mesh | null = null;
  private stars: THREE.Points | null = null;
  private portLights: THREE.PointLight[] = [];
  private lighthouseSpotlight: THREE.SpotLight | null = null;
  private lighthouseRotationAngle = 0;

  // Vessel navigation animation
  private vessels: Array<{
    mesh: THREE.Group;
    startPosition: THREE.Vector3;
    endPosition: THREE.Vector3;
    progress: number;
  }> = [];
  private isNavigating = false;
  private navigationSpeed = 0.001;

  // Mouse control state
  private isDragging = false;
  private previousMousePosition = { x: 0, y: 0 };

// Dynamic inspection spotlight
  private inspectionSpotlight: THREE.SpotLight | null = null;
  private inspectionSpotlightTarget: THREE.Object3D | null = null;
  private readonly INSPECTION_SPOT_INTENSITY = 3.0;
  private readonly INSPECTION_SPOT_DISTANCE = 1200;
  private readonly INSPECTION_SPOT_ANGLE = Math.PI / 8; // foco relativamente estreito
  private readonly INSPECTION_SPOT_PENUMBRA = 0.85; // claramente visível penumbra
  private readonly INSPECTION_SPOT_DECAY = 2;


  // Control sensitivity and limits
  private readonly ROTATION_SPEED = 0.005; // Radians per pixel
  private readonly ZOOM_SPEED = 5; // Units per wheel tick
  private readonly MIN_DISTANCE = 50; // Minimum zoom distance
  private readonly MAX_DISTANCE = 500; // Maximum zoom distance
  private readonly MIN_ELEVATION = 0.1; // Prevent camera from going underground
  private readonly MAX_ELEVATION = Math.PI / 2 - 0.1; // Prevent camera from flipping

  // Placement tuning (can be overridden via setPlacementOffsets)
  private WATER_EDGE_OVERLAP = 0.6; // how much water overlaps under the platform edge (higher = closer to asphalt)
  private SHIP_WATER_GAP = 5.0;    // safety gap between ship hull and platform edge (units) - ensures ships never touch asphalt
  private CRANE_DOCK_INSET = 4.0;   // how far quay cranes sit inside the dock from the edge
  private YARD_CRANE_YARD_OFFSET = 8.0; // how far yard cranes sit outside the yard edge (further back from docks)
  // Layout-specific safety: Layout B needs a bit more clearance to avoid visual contact with asphalt
  private readonly LAYOUT_B_CLEARANCE = 40; // minimum clearance used only for Layout B

  // Track current layout so we can apply layout-specific tweaks safely
  private currentLayoutId: string | null = null;

  // US 4.2.2: Object picking properties
  private raycaster = new THREE.Raycaster();
  private mouse = new THREE.Vector2();
  private selectedObject: THREE.Object3D | null = null;
  private highlightedMeshes: THREE.Mesh[] = []; // Track which meshes were highlighted
  private outlineMaterial: THREE.LineBasicMaterial | null = null;
  private isInspectionMode = false; // When true, WASD is disabled

  // US 4.2.6: Smooth animations
  private animationDuration = 1000; // Default 1 second for animations
  private animationEasing = TWEEN.Easing.Quadratic.InOut; // Default easing function
  private tweenGroup = new TWEEN.Group(); // Dedicated TWEEN group for camera animations

  constructor(
    private dockService: DockService,
    private vesselService: VesselService,
    private physicalResourceService: PhysicalResourceService,
    private storageAreaService: StorageAreaService
  ) {}

  /**
   * Override default placement offsets for fine-tuning scene layout
   */
  setPlacementOffsets(opts: Partial<{ waterOverlap: number; shipGap: number; craneInset: number }>): void {
    console.log(`🔧 setPlacementOffsets called with:`, opts);
    if (opts.waterOverlap !== undefined) this.WATER_EDGE_OVERLAP = opts.waterOverlap;
    if (opts.shipGap !== undefined) this.SHIP_WATER_GAP = opts.shipGap;
    if (opts.craneInset !== undefined) this.CRANE_DOCK_INSET = opts.craneInset;
    // Re-run alignment if scene already has elements
    const side = this.alignDocksToWaterEdge();
    console.log(`🔧 alignDocksToWaterEdge returned side: ${side}`);
    if (side) {
      console.log(`🔧 Calling alignShipsAndCranes with side: ${side}`);
      this.alignShipsAndCranes(side);
    }
  }

  /**
   * Initialize Three.js scene with container element
   * @param container HTML element to attach the renderer
   */
  initScene(container: HTMLElement): void {
    const width = container.clientWidth;
    const height = container.clientHeight;

    // Create scene
    this.scene = new THREE.Scene();
    this.scene.background = new THREE.Color(0x87ceeb); // Sky blue
    // Add subtle fog for depth
    this.scene.fog = new THREE.Fog(0x87ceeb, 200, 900);

    // Create camera - positioned to see the entire port
    this.camera = new THREE.PerspectiveCamera(75, width / height, 0.1, 2000);

    // US 3.3.6: Initialize camera using spherical coordinates for smooth control
    this.updateCameraPosition();

    // Create renderer
    this.renderer = new THREE.WebGLRenderer({ antialias: true });
    this.renderer.setSize(width, height);
    this.renderer.setPixelRatio(window.devicePixelRatio);
    // Enable soft shadows
    this.renderer.shadowMap.enabled = true;
    this.renderer.shadowMap.type = THREE.PCFSoftShadowMap;
    container.appendChild(this.renderer.domElement);

    // Add basic lighting
    this.addLighting();

    // Initialize night mode elements
    this.createStarrySky();
    this.createMoon();
    this.createPortLights();
    // Create the inspection spotlight used by US 4.2.5
    this.createInspectionSpotlight();
  }

  /**
   * Add basic lighting to the scene
   */
  private addLighting(): void {
    // Day ambient light for overall illumination
    this.dayAmbientLight = new THREE.AmbientLight(0xffffff, 0.6);
    this.scene.add(this.dayAmbientLight);

    // Day directional light (sun)
    this.dayDirectionalLight = new THREE.DirectionalLight(0xffffff, 0.9);
    this.dayDirectionalLight.position.set(80, 120, 60);
    this.dayDirectionalLight.castShadow = true;
    this.dayDirectionalLight.shadow.mapSize.width = 2048;
    this.dayDirectionalLight.shadow.mapSize.height = 2048;
    this.dayDirectionalLight.shadow.camera.near = 1;
    this.dayDirectionalLight.shadow.camera.far = 800;
    this.dayDirectionalLight.shadow.camera.left = -500;
    this.dayDirectionalLight.shadow.camera.right = 500;
    this.dayDirectionalLight.shadow.camera.top = 500;
    this.dayDirectionalLight.shadow.camera.bottom = -500;
    this.scene.add(this.dayDirectionalLight);

    // Moon light (initially hidden)
    this.moonLight = new THREE.DirectionalLight(0xadd8e6, 0.5);
    this.moonLight.position.set(-100, 150, 80);
    this.moonLight.castShadow = false; // Disable shadow to save texture units
    this.moonLight.visible = false;
    this.scene.add(this.moonLight);
  }

// US Create spotlight inspection
private createInspectionSpotlight(): void {
  // Target é um Object3D separado para permitir animação independente
  this.inspectionSpotlightTarget = new THREE.Object3D();
  this.scene.add(this.inspectionSpotlightTarget);

  this.inspectionSpotlight = new THREE.SpotLight(0xffffff, this.INSPECTION_SPOT_INTENSITY, this.INSPECTION_SPOT_DISTANCE, this.INSPECTION_SPOT_ANGLE, this.INSPECTION_SPOT_PENUMBRA, this.INSPECTION_SPOT_DECAY);
  this.inspectionSpotlight.castShadow = true;
  this.inspectionSpotlight.shadow.mapSize.width = 1024;
  this.inspectionSpotlight.shadow.mapSize.height = 1024;
  this.inspectionSpotlight.shadow.camera.near = 0.5;
  this.inspectionSpotlight.shadow.camera.far = this.INSPECTION_SPOT_DISTANCE;
  this.inspectionSpotlight.visible = false; // ligado apenas quando houver seleção

  // Aponta para o alvo que posicionaremos dinamicamente
  this.inspectionSpotlight.target = this.inspectionSpotlightTarget;

  // Pequeno helper opcional (comente se não desejar)
  // const helper = new THREE.SpotLightHelper(this.inspectionSpotlight);
  // this.scene.add(helper);

  this.scene.add(this.inspectionSpotlight);
  console.log('🔦 Inspection spotlight created (penumbra:', this.INSPECTION_SPOT_PENUMBRA, ')');
}

// ======= Atualizar spotlight para mirar no centro do objeto selecionado =======
private updateInspectionSpotlightTarget(object: THREE.Object3D | null): void {
  if (!this.inspectionSpotlight || !this.inspectionSpotlightTarget) return;

  if (!object) {
    this.inspectionSpotlight.visible = false;
    return;
  }

  // Calcular centro do objeto
  const box = new THREE.Box3().setFromObject(object);
  const center = box.getCenter(new THREE.Vector3());

  // Mover o target para o centro do objeto (usa world coords)
  this.inspectionSpotlightTarget.position.copy(center);
  this.inspectionSpotlightTarget.updateMatrixWorld();

  // Posicionar a luz na posição da câmera (liga a luz apenas se câmera existir)
  if (this.camera) {
    // Colocar luz exatamente na posição da câmera para que o feixe saia da vista do usuário
    this.inspectionSpotlight.position.copy(this.camera.position);

    // Opcional: deslocar levemente a luz um pouco à frente da câmera para evitar auto-oclusões
    const dir = new THREE.Vector3();
    this.camera.getWorldDirection(dir);
    const forwardOffset = 2.0;
    this.inspectionSpotlight.position.add(dir.multiplyScalar(forwardOffset));
  }

  this.inspectionSpotlight.visible = true;
  // aumentar intensidade levemente quando em inspeção (opcional)
  this.inspectionSpotlight.intensity = this.INSPECTION_SPOT_INTENSITY;
}

  /**
   * Create starry sky for night mode
   */
  private createStarrySky(): void {
    const starGeometry = new THREE.BufferGeometry();
    const starCount = 3000;
    const positions = new Float32Array(starCount * 3);

    for (let i = 0; i < starCount * 3; i += 3) {
      // Distribute stars in a sphere around the scene
      positions[i] = (Math.random() - 0.5) * 2000;
      positions[i + 1] = Math.random() * 800 + 200; // Only in upper hemisphere
      positions[i + 2] = (Math.random() - 0.5) * 2000;
    }

    starGeometry.setAttribute('position', new THREE.BufferAttribute(positions, 3));
    const starMaterial = new THREE.PointsMaterial({
      color: 0xffffff,
      size: 2,
      sizeAttenuation: true,
      transparent: true,
      opacity: 0.8
    });

    this.stars = new THREE.Points(starGeometry, starMaterial);
    this.stars.visible = false;
    this.scene.add(this.stars);
  }

  /**
   * Create moon for night mode
   */
  private createMoon(): void {
    const moonGeometry = new THREE.SphereGeometry(30, 32, 32);
    const moonMaterial = new THREE.MeshStandardMaterial({
      color: 0xffffee,
      emissive: 0xffffcc,
      emissiveIntensity: 1.2,
      roughness: 0.9
    });

    this.moon = new THREE.Mesh(moonGeometry, moonMaterial);
    this.moon.position.set(-400, 350, 200);
    this.moon.visible = false;
    this.scene.add(this.moon);
  }

  /**
   * Create lighthouse structure on the platform
   */
  private async addLighthouseToLayout(): Promise<void> {
    try {
      // Load lighthouse 3D model
      const model = await new Promise<THREE.Group>((resolve, reject) => {
        const loader = new GLTFLoader();
        loader.load(
          'assets/models/light_house.glb',
          (gltf) => resolve(gltf.scene),
          undefined,
          (error) => reject(error)
        );
      });

      // Get model size and center it
      const box = new THREE.Box3().setFromObject(model);
      const size = box.getSize(new THREE.Vector3());
      const center = box.getCenter(new THREE.Vector3());

      console.log(`🗼 Lighthouse model original size: ${size.x.toFixed(2)} × ${size.y.toFixed(2)} × ${size.z.toFixed(2)}`);

      // Scale to desired height (~150)
      const desiredHeight = 150;
      const scaleFactor = desiredHeight / size.y;
      model.scale.setScalar(scaleFactor);

      // Recalculate box after scaling
      const scaledBox = new THREE.Box3().setFromObject(model);
      const yOffset = -scaledBox.min.y; // Offset to place bottom at y=0

      // Apply shadows
      model.traverse((child) => {
        if (child instanceof THREE.Mesh) {
          child.castShadow = true;
          child.receiveShadow = true;
        }
      });

      // Position lighthouse on platform (adjust for Layout B)
      const lighthouseX = this.currentLayoutId === 'B' ? 500 : 400;
      const lighthouseY = this.currentLayoutId === 'B' ? 0.4 : yOffset;
      const lighthouseZ = this.currentLayoutId === 'B' ? -220 : -165;
      model.position.set(lighthouseX, lighthouseY, lighthouseZ);
      model.userData = { type: 'lighthouse', name: 'Lighthouse' };

      this.scene.add(model);
      this.portElements.push(model);

      // Add strong point light at the top of lighthouse
      const lightHeight = scaledBox.max.y - scaledBox.min.y + yOffset;
      const lighthouseLight = new THREE.PointLight(0xffffaa, 300, 400);
      lighthouseLight.position.set(lighthouseX, lightHeight, lighthouseZ);
      lighthouseLight.castShadow = false;
      lighthouseLight.visible = this.isNightMode; // Use current night mode state
      lighthouseLight.userData = { isLighthouseLight: true };
      this.scene.add(lighthouseLight);
      this.portLights.push(lighthouseLight);

      // Add rotating spotlight (lighthouse beam)
      this.lighthouseSpotlight = new THREE.SpotLight(0xffffaa, 3000, 800, Math.PI / 6, 0.5, 1.0);
      this.lighthouseSpotlight.position.set(lighthouseX, lightHeight, lighthouseZ);
      this.lighthouseSpotlight.castShadow = false;
      this.lighthouseSpotlight.visible = this.isNightMode; // Use current night mode state
      this.scene.add(this.lighthouseSpotlight);
      this.scene.add(this.lighthouseSpotlight.target);

      console.log(`🗼 Lighthouse loaded at position (${lighthouseX}, ${lighthouseY.toFixed(1)}, ${lighthouseZ}) with rotating beam at height ${lightHeight.toFixed(1)}`);
    } catch (error) {
      console.warn('Failed to load lighthouse model, using procedural fallback:', error);
      this.createProceduralLighthouse();
    }
  }

  /**
   * Create procedural lighthouse as fallback
   */
  private createProceduralLighthouse(): void {
    const lighthouseGroup = new THREE.Group();

    // Lighthouse dimensions (half the original size: ~150 height)
    const baseRadius = 4;
    const towerHeight = 140;
    const topRadius = 3;

    // Tower base (wider at bottom)
    const baseGeometry = new THREE.CylinderGeometry(baseRadius, baseRadius * 1.3, 30, 16);
    const baseMaterial = new THREE.MeshStandardMaterial({
      color: 0xeeeeee,
      roughness: 0.8,
      metalness: 0.2
    });
    const base = new THREE.Mesh(baseGeometry, baseMaterial);
    base.position.y = 15;
    base.castShadow = true;
    base.receiveShadow = true;
    lighthouseGroup.add(base);

    // Main tower (tapered cylinder)
    const towerGeometry = new THREE.CylinderGeometry(topRadius, baseRadius, towerHeight, 16);
    const towerMaterial = new THREE.MeshStandardMaterial({
      color: 0xffffff,
      roughness: 0.7,
      metalness: 0.1
    });
    const tower = new THREE.Mesh(towerGeometry, towerMaterial);
    tower.position.y = 30 + towerHeight / 2;
    tower.castShadow = true;
    tower.receiveShadow = true;
    lighthouseGroup.add(tower);

    // Red stripes
    const stripeHeight = 40;
    for (let i = 0; i < 3; i++) {
      const stripeGeometry = new THREE.CylinderGeometry(
        topRadius + (baseRadius - topRadius) * (1 - (i * 0.3 + 0.2)),
        topRadius + (baseRadius - topRadius) * (1 - (i * 0.3 + 0.4)),
        stripeHeight,
        16
      );
      const stripeMaterial = new THREE.MeshStandardMaterial({
        color: 0xff0000,
        roughness: 0.7,
        metalness: 0.1
      });
      const stripe = new THREE.Mesh(stripeGeometry, stripeMaterial);
      stripe.position.y = 60 + i * 80 + stripeHeight / 2;
      stripe.castShadow = true;
      lighthouseGroup.add(stripe);
    }

    // Top lantern room
    const lanternGeometry = new THREE.CylinderGeometry(topRadius * 1.2, topRadius * 1.2, 20, 8);
    const lanternMaterial = new THREE.MeshStandardMaterial({
      color: 0x333333,
      roughness: 0.3,
      metalness: 0.7
    });
    const lantern = new THREE.Mesh(lanternGeometry, lanternMaterial);
    lantern.position.y = 30 + towerHeight + 10;
    lantern.castShadow = true;
    lighthouseGroup.add(lantern);

    // Position lighthouse on platform (adjust for Layout B)
    const lighthouseX = this.currentLayoutId === 'B' ? 500 : 400;
    const lighthouseY = this.currentLayoutId === 'B' ? 0.4 : 0;
    const lighthouseZ = this.currentLayoutId === 'B' ? -220 : -165;
    lighthouseGroup.position.set(lighthouseX, lighthouseY, lighthouseZ);
    lighthouseGroup.userData = { type: 'lighthouse', name: 'Lighthouse' };

    this.scene.add(lighthouseGroup);
    this.portElements.push(lighthouseGroup);

    // Add strong point light at the top
    const lightHeight = 30 + towerHeight + 20;
    const lighthouseLight = new THREE.PointLight(0xffffaa, 300, 400);
    lighthouseLight.position.set(lighthouseX, lightHeight, lighthouseZ);
    lighthouseLight.castShadow = false;
    lighthouseLight.visible = this.isNightMode; // Use current night mode state
    lighthouseLight.userData = { isLighthouseLight: true };
    this.scene.add(lighthouseLight);
    this.portLights.push(lighthouseLight);

    // Add rotating spotlight (lighthouse beam)
    this.lighthouseSpotlight = new THREE.SpotLight(0xffffaa, 3000, 800, Math.PI / 6, 0.5, 1.0);
    this.lighthouseSpotlight.position.set(lighthouseX, lightHeight, lighthouseZ);
    this.lighthouseSpotlight.castShadow = false;
    this.lighthouseSpotlight.visible = this.isNightMode; // Use current night mode state
    this.scene.add(this.lighthouseSpotlight);
    this.scene.add(this.lighthouseSpotlight.target);

    console.log(`🗼 Procedural lighthouse created at position (${lighthouseX}, ${lighthouseY}, ${lighthouseZ}) with rotating beam`);
  }

  /**
   * Create port lights (using imported lamp post models if available)
   */
  private createPortLights(): void {
    // Clear existing port lights
    this.portLights.forEach(light => {
      this.scene.remove(light);
    });
    this.portLights = [];

    // Add fewer point lights with no shadows to save GPU resources
    const lightPositions = [
      { x: -100, z: -100 }, { x: 100, z: -100 },
      { x: -100, z: 0 }, { x: 100, z: 0 },
      { x: -100, z: 100 }, { x: 100, z: 100 },
    ];

    lightPositions.forEach(pos => {
      const light = new THREE.PointLight(0xffffaa, 200, 200);
      light.position.set(pos.x, 20, pos.z);
      light.castShadow = false; // Disable shadows to save texture units
      light.visible = false;
      this.scene.add(light);
      this.portLights.push(light);
    });

    // Note: Lighthouse spotlight is now created in addLighthouseToLayout() with correct position
  }

  /**
   * Toggle between day and night mode
   */
  toggleNightMode(): void {
    this.isNightMode = !this.isNightMode;

    if (this.isNightMode) {
      // Switch to night mode
      this.scene.background = new THREE.Color(0x001a33); // Lighter dark blue night sky
      this.scene.fog = new THREE.Fog(0x001a33, 400, 1200); // Much less fog

      // Keep strong illumination for objects at night (almost day-level)
      this.dayAmbientLight.intensity = 0.85;
      this.dayDirectionalLight.intensity = 0.75;

      // Show night elements
      this.moonLight.visible = true;
      if (this.moon) this.moon.visible = true;
      if (this.stars) this.stars.visible = true;

      // Turn on port lights
      this.portLights.forEach(light => {
        light.visible = true;
      });

      // Turn on vessel lights
      this.toggleVesselLights(true);

      // Turn on lighthouse rotating spotlight
      if (this.lighthouseSpotlight) {
        this.lighthouseSpotlight.visible = true;
      }

      console.log('🌙 Night mode activated');
    } else {
      // Switch to day mode
      this.scene.background = new THREE.Color(0x87ceeb); // Sky blue
      this.scene.fog = new THREE.Fog(0x87ceeb, 200, 900);

      // Restore day lights
      this.dayAmbientLight.intensity = 0.6;
      this.dayDirectionalLight.intensity = 0.9;

      // Hide night elements
      this.moonLight.visible = false;
      if (this.moon) this.moon.visible = false;
      if (this.stars) this.stars.visible = false;

      // Turn off port lights
      this.portLights.forEach(light => {
        light.visible = false;
      });

      // Turn off vessel lights
      this.toggleVesselLights(false);

      // Turn off lighthouse rotating spotlight
      if (this.lighthouseSpotlight) {
        this.lighthouseSpotlight.visible = false;
      }

      console.log('☀️ Day mode activated');
    }
  }

  /**
   * Get current night mode state
   */
  isNight(): boolean {
    return this.isNightMode;
  }

  /**
   * Toggle vessel navigation animation
   */
  toggleVesselNavigation(): void {
    if (this.isNavigating) {
      this.resetVesselPositions();
    } else {
      this.startVesselNavigation();
    }
  }

  /**
   * Start vessel navigation only if not already navigating
   * Used for automatic animation triggering
   */
  startVesselNavigationIfNeeded(): void {
    if (!this.isNavigating) {
      this.startVesselNavigation();
    }
  }

  /**
   * Check if vessels are currently navigating
   */
  areVesselsNavigating(): boolean {
    return this.isNavigating;
  }

  /**
   * Toggle vessel navigation lights on/off
   */
  private toggleVesselLights(on: boolean): void {
    this.portElements.forEach(element => {
      if (element.userData?.['elementType'] === 'ship') {
        element.traverse((child) => {
          if (child instanceof THREE.PointLight && child.userData?.['isVesselLight']) {
            child.visible = on;
          }
        });
      }
    });
  }

  /**
   * Start animation loop
   */
  animate(): void {
    const render = (time?: number) => {
      this.animationFrameId = requestAnimationFrame(render);

      // US 4.2.6: Update TWEEN animations with current time
      this.tweenGroup.update(time);

      // Animate lighthouse rotating spotlight
      if (this.lighthouseSpotlight && this.lighthouseSpotlight.visible) {
        // Rotate at moderate speed (full rotation every ~15 seconds)
        this.lighthouseRotationAngle += 0.004;

        // Calculate target position in a circle around the lighthouse at port level
        const radius = 400;
        const baseX = this.lighthouseSpotlight.position.x;
        const baseZ = this.lighthouseSpotlight.position.z;
        const targetX = baseX + Math.cos(this.lighthouseRotationAngle) * radius;
        const targetZ = baseZ + Math.sin(this.lighthouseRotationAngle) * radius;

        // Target at port level (y=20) instead of ground
        this.lighthouseSpotlight.target.position.set(targetX, 20, targetZ);
        this.lighthouseSpotlight.target.updateMatrixWorld();
      }

      // Per-frame update: make inspection spotlight follow camera and target the selected object
      if (this.inspectionSpotlight && this.inspectionSpotlight.visible && this.selectedObject) {
        // keep the light aligned with the camera
        if (this.camera) {
          this.inspectionSpotlight.position.copy(this.camera.position);

          const tmpDir = new THREE.Vector3();
          this.camera.getWorldDirection(tmpDir);
          this.inspectionSpotlight.position.add(tmpDir.multiplyScalar(2.0));
        }

        // update spotlight target to the center of the selected object
        const box = new THREE.Box3().setFromObject(this.selectedObject);
        const center = box.getCenter(new THREE.Vector3());
        if (this.inspectionSpotlight.target) {
          this.inspectionSpotlight.target.position.copy(center);
          this.inspectionSpotlight.target.updateMatrixWorld();
        }
      }

      // Animate vessel navigation
      if (this.isNavigating && this.vessels.length > 0) {
        let allComplete = true;

        this.vessels.forEach(vessel => {
          if (vessel.progress < 1) {
            allComplete = false;
            vessel.progress += this.navigationSpeed;

            if (vessel.progress >= 1) {
              vessel.progress = 1;
            }

            const t = vessel.progress;
            const smoothProgress = t < 0.5 ? 2 * t * t : -1 + (4 - 2 * t) * t;

            vessel.mesh.position.lerpVectors(
              vessel.startPosition,
              vessel.endPosition,
              smoothProgress
            );
          }
        });

        if (allComplete) {
          this.isNavigating = false;
          console.log('🚢 All vessels arrived at docks');
        }
      }

      // Render the scene
      this.renderer.render(this.scene, this.camera);
    };
    render();
  }

  /**
   * Handle window resize
   * @param width New width
   * @param height New height
   */
  onResize(width: number, height: number): void {
    if (this.camera && this.renderer) {
      this.camera.aspect = width / height;
      this.camera.updateProjectionMatrix();
      this.renderer.setSize(width, height);
    }
  }

  /**
   * US 3.3.6: Update camera position based on spherical coordinates
   * Calculates camera position on a sphere around the target
   */
  private updateCameraPosition(): void {
    if (!this.camera) return;

    // Convert spherical coordinates to Cartesian
    const x = this.cameraTarget.x + this.cameraDistance * Math.cos(this.cameraElevation) * Math.cos(this.cameraAzimuth);
    const y = this.cameraTarget.y + this.cameraDistance * Math.sin(this.cameraElevation);
    const z = this.cameraTarget.z + this.cameraDistance * Math.cos(this.cameraElevation) * Math.sin(this.cameraAzimuth);

    this.camera.position.set(x, y, z);
    this.camera.lookAt(this.cameraTarget);
  }

  /**
   * US 3.3.6: Handle mouse down event (start dragging)
   * @param event Mouse event
   */
  onMouseDown(event: MouseEvent): void {
    if (event.button === 2) { // Right mouse button
      this.isDragging = true;
      this.previousMousePosition = { x: event.clientX, y: event.clientY };
      event.preventDefault();
    }
  }

  /**
   * US 3.3.6: Handle mouse move event (orbit camera)
   * @param event Mouse event
   */
  onMouseMove(event: MouseEvent): void {
    if (!this.isDragging) return;

    const deltaX = event.clientX - this.previousMousePosition.x;
    const deltaY = event.clientY - this.previousMousePosition.y;

    // Update azimuth (horizontal rotation)
    this.cameraAzimuth -= deltaX * this.ROTATION_SPEED;

    // US 4.2.2: Only allow vertical rotation when NOT in inspection mode
    if (!this.isInspectionMode) {
      // Update elevation (vertical rotation) with limits
      this.cameraElevation += deltaY * this.ROTATION_SPEED;
      this.cameraElevation = Math.max(this.MIN_ELEVATION, Math.min(this.MAX_ELEVATION, this.cameraElevation));
    }

    // Update camera position
    this.updateCameraPosition();

    // Store current position for next frame
    this.previousMousePosition = { x: event.clientX, y: event.clientY };
  }

  /**
   * US 3.3.6: Handle mouse up event (stop dragging)
   * @param event Mouse event
   */
  onMouseUp(event: MouseEvent): void {
    if (event.button === 2) { // Right mouse button
      this.isDragging = false;
    }
  }

  /**
   * US 3.3.6: Handle mouse wheel event (zoom in/out)
   * @param event Wheel event
   */
  onMouseWheel(event: WheelEvent): void {
    event.preventDefault();

    // Zoom in/out based on wheel direction
    const zoomDelta = event.deltaY > 0 ? this.ZOOM_SPEED : -this.ZOOM_SPEED;
    this.cameraDistance += zoomDelta;

    // Clamp distance within safe limits
    this.cameraDistance = Math.max(this.MIN_DISTANCE, Math.min(this.MAX_DISTANCE, this.cameraDistance));

    // Update camera position
    this.updateCameraPosition();
  }

  /**
   * Handle keyboard input for WASD camera movement
   * @param event Keyboard event
   */
  onKeyDown(event: KeyboardEvent): void {
    // US 4.2.2: Allow ESC to deselect object and exit inspection mode
    if (event.key === 'Escape' && this.isInspectionMode) {
      this.clearSelection();
      return;
    }

    // US 4.2.7: Allow R to reset camera to default position
    if (event.key.toLowerCase() === 'r') {
      this.resetCamera();
      return;
    }

    // US 4.2.2: Disable WASD when in inspection mode
    if (this.isInspectionMode) {
      return;
    }

    const moveSpeed = 10; // Units to move per key press
    const key = event.key.toLowerCase();

    // Calculate forward and right vectors based on camera orientation
    const forward = new THREE.Vector3();
    const right = new THREE.Vector3();

    // Forward is the direction from camera to target (on XZ plane)
    forward.subVectors(this.cameraTarget, this.camera.position);
    forward.y = 0; // Keep movement horizontal
    forward.normalize();

    // Right is perpendicular to forward
    right.crossVectors(forward, new THREE.Vector3(0, 1, 0));
    right.normalize();

    // Apply movement based on key
    switch (key) {
      case 'w': // Forward
        this.cameraTarget.addScaledVector(forward, moveSpeed);
        break;
      case 's': // Backward
        this.cameraTarget.addScaledVector(forward, -moveSpeed);
        break;
      case 'a': // Left
        this.cameraTarget.addScaledVector(right, -moveSpeed);
        break;
      case 'd': // Right
        this.cameraTarget.addScaledVector(right, moveSpeed);
        break;
      default:
        return; // Ignore other keys
    }

    // Update camera position to orbit around new target
    this.updateCameraPosition();
  }

  /**
   * US 3.3.6: Set camera target (point to orbit around)
   * @param target Target position
   */
  setCameraTarget(target: THREE.Vector3): void {
    this.cameraTarget.copy(target);
    this.updateCameraPosition();
  }

  /**
   * US 3.3.6: Get renderer DOM element for attaching event listeners
   */
  getRendererElement(): HTMLCanvasElement | null {
    return this.renderer ? this.renderer.domElement : null;
  }

  /**
   * Clean up resources when component is destroyed
   */
  dispose(): void {
    if (this.animationFrameId !== null) {
      cancelAnimationFrame(this.animationFrameId);
    }
    if (this.renderer) {
      this.renderer.dispose();
    }
  }

  /**
   * Get the scene for external manipulation
   */
  getScene(): THREE.Scene {
    return this.scene;
  }

  /**
   * US 4.2.2: Get currently selected object
   */
  getSelectedObject(): { object: THREE.Object3D; type: string; metadata: any } | null {
    if (!this.selectedObject) return null;

    return {
      object: this.selectedObject,
      type: this.selectedObject.userData['type'] || 'unknown',
      metadata: this.selectedObject.userData['metadata'] || {}
    };
  }

  /**
   * US 4.2.7: Check if in inspection mode
   */
  isInspecting(): boolean {
    return this.isInspectionMode;
  }

  /**
   * US 4.2.6: Configure animation duration for testing
   * @param duration Duration in milliseconds (default: 1000ms)
   */
  setAnimationDuration(duration: number): void {
    this.animationDuration = duration;
    console.log(`⚙️ US 4.2.6: Animation duration set to ${duration}ms`);
  }

  /**
   * US 4.2.6: Configure animation easing function for testing
   * @param easing TWEEN easing function (e.g., TWEEN.Easing.Quadratic.InOut)
   */
  setAnimationEasing(easing: (amount: number) => number): void {
    this.animationEasing = easing;
    console.log('⚙️ US 4.2.6: Animation easing function configured');
  }

  /**
   * US 4.2.6: Get available TWEEN easing functions for configuration
   */
  getAvailableEasingFunctions(): typeof TWEEN.Easing {
    return TWEEN.Easing;
  }

  /**
   * US 4.2.7: Reset camera to default overview position
   * @param animated Whether to use smooth animation (default: true for 5+ elements)
   */
  resetCamera(animated: boolean = true): void {
    console.log('🔄 Resetting camera to default position');

    // Clear any selection
    this.clearSelection();

    // Check if we should use animation (5+ port elements)
    const shouldAnimate = animated && this.portElements.length >= 5;

    if (shouldAnimate) {
      // Smooth animation
      this.animateCameraToPosition(
        this.defaultCameraTarget.clone(),
        this.defaultCameraDistance,
        this.defaultCameraAzimuth,
        this.defaultCameraElevation,
        1000 // 1 second duration
      );
    } else {
      // Instant reset
      this.cameraTarget.copy(this.defaultCameraTarget);
      this.cameraDistance = this.defaultCameraDistance;
      this.cameraAzimuth = this.defaultCameraAzimuth;
      this.cameraElevation = this.defaultCameraElevation;
      this.updateCameraPosition();
    }
  }

  /**
   * US 4.2.6 & 4.2.7: Animate camera to a specific position using TWEEN.js
   * Provides smooth, configurable transitions for camera movement
   */
  private animateCameraToPosition(
    targetPosition: THREE.Vector3,
    targetDistance: number,
    targetAzimuth: number,
    targetElevation: number,
    duration: number
  ): void {
    console.log(`🎬 US 4.2.6: animateCameraToPosition called with duration=${duration}ms`);

    // Stop any existing camera tweens
    this.tweenGroup.removeAll();

    // Store start values
    const startTarget = { x: this.cameraTarget.x, y: this.cameraTarget.y, z: this.cameraTarget.z };
    const startDistance = this.cameraDistance;
    const startAzimuth = this.cameraAzimuth;
    const startElevation = this.cameraElevation;

    console.log(`📍 Start: target=(${startTarget.x.toFixed(1)}, ${startTarget.y.toFixed(1)}, ${startTarget.z.toFixed(1)}), distance=${startDistance.toFixed(1)}`);
    console.log(`📍 End: target=(${targetPosition.x.toFixed(1)}, ${targetPosition.y.toFixed(1)}, ${targetPosition.z.toFixed(1)}), distance=${targetDistance.toFixed(1)}`);

    // Combine all values into a single tween object for better performance
    const tweenValues = {
      targetX: startTarget.x,
      targetY: startTarget.y,
      targetZ: startTarget.z,
      distance: startDistance,
      azimuth: startAzimuth,
      elevation: startElevation
    };

    const targetValues = {
      targetX: targetPosition.x,
      targetY: targetPosition.y,
      targetZ: targetPosition.z,
      distance: targetDistance,
      azimuth: targetAzimuth,
      elevation: targetElevation
    };

    // Create single tween that updates everything - using our dedicated group
    const cameraTween = new TWEEN.Tween(tweenValues, this.tweenGroup)
      .to(targetValues, duration)
      .easing(this.animationEasing)
      .onStart(() => {
        console.log(`🎬 US 4.2.6: Camera tween STARTED!`);
      })
      .onUpdate((values) => {
        // Update camera target
        this.cameraTarget.set(values.targetX, values.targetY, values.targetZ);
        // Update camera parameters
        this.cameraDistance = values.distance;
        this.cameraAzimuth = values.azimuth;
        this.cameraElevation = values.elevation;
        // Apply changes
        this.updateCameraPosition();
      })
      .onComplete(() => {
        console.log('✅ US 4.2.6: Camera animation completed using TWEEN.js');
      });

    // Start the tween
    console.log(`🚀 About to start tween...`);
    cameraTween.start();
    console.log(`🚀 Tween started! Active tweens in group: ${this.tweenGroup.getAll().length}`);
  }

  /**
   * US 4.2.7: Set default camera position (called after loading layout)
   */
  setDefaultCameraPosition(target: THREE.Vector3, distance: number, azimuth: number, elevation: number): void {
    this.defaultCameraTarget.copy(target);
    this.defaultCameraDistance = distance;
    this.defaultCameraAzimuth = azimuth;
    this.defaultCameraElevation = elevation;

    console.log(`📷 Default camera position set: target(${target.x.toFixed(1)}, ${target.y.toFixed(1)}, ${target.z.toFixed(1)}), distance=${distance.toFixed(1)}`);
  }

  /**
   * Get the camera for external manipulation
   */
  getCamera(): THREE.PerspectiveCamera {
    return this.camera;
  }

  /**
   * US 4.2.2: Handle object picking via mouse click
   * @param event Mouse event from canvas
   * @returns Selected object info or null
   */
  pickObject(event: MouseEvent): { object: THREE.Object3D; type: string; name: string } | null {
    const canvas = this.renderer.domElement;
    const rect = canvas.getBoundingClientRect();

    // Calculate mouse position in normalized device coordinates (-1 to +1)
    this.mouse.x = ((event.clientX - rect.left) / rect.width) * 2 - 1;
    this.mouse.y = -((event.clientY - rect.top) / rect.height) * 2 + 1;

    // Update raycaster with camera and mouse position
    this.raycaster.setFromCamera(this.mouse, this.camera);

    // Increase threshold for better picking of complex models
    this.raycaster.params.Points!.threshold = 5;
    this.raycaster.params.Line!.threshold = 5;

    // Calculate objects intersecting the picking ray
    const intersects = this.raycaster.intersectObjects(this.portElements, true);

    console.log(`🔍 Raycaster found ${intersects.length} intersections`);

    if (intersects.length > 0) {
      // Get the first intersected object
      let intersectedObject = intersects[0].object;

      console.log(`🔍 First intersected object:`, intersectedObject.name, intersectedObject.userData);

      // Traverse up to find the root port element with metadata
      let attempts = 0;
      while (intersectedObject.parent && !intersectedObject.userData['type'] && attempts < 20) {
        intersectedObject = intersectedObject.parent;
        console.log(`🔍 Traversing up to parent (${attempts}):`, intersectedObject.name, intersectedObject.userData);
        attempts++;
      }

      const elementType = intersectedObject.userData['type'] || 'unknown';
      const metadata = intersectedObject.userData['metadata'] || {};

      // US 4.2.1 & 4.2.2: Allow selection of port facilities (docks, storage areas) and operational resources (vessels, cranes, trucks)
      const selectableTypes = ['vessel', 'ship', 'crane', 'truck', 'dock', 'warehouse', 'containeryard'];
      if (!selectableTypes.includes(elementType)) {
        console.log(`⚠️ Object type '${elementType}' is not selectable`);
        // Don't clear selection - only ESC can deselect
        return null;
      }

      // Extract the actual type and name from metadata to match database
      let type = elementType;
      let name = '';

      if (elementType === 'ship' || elementType === 'vessel') {
        // US 4.2.2: For vessels: use vesselType from database (e.g., "Panamax", "Feeder")
        // and vessel name from database
        type = metadata.vesselType || metadata.type || 'Vessel';
        name = metadata.name || metadata.imoNumber || metadata.callSign || 'Unknown Vessel';
      } else if (elementType === 'crane' || elementType === 'truck') {
        // US 4.2.2: For physical resources: use the specific type from metadata (e.g., "STS", "RTG", "Truck")
        // and use the identifier/code as name
        type = metadata.type || elementType;
        name = metadata.identifier || metadata.code || metadata.name || elementType;
      } else if (elementType === 'dock') {
        // US 4.2.1: For docks: use dock name from database
        type = 'Dock';
        name = metadata.name || metadata.code || metadata.identifier || 'Unknown Dock';
      } else if (elementType === 'warehouse' || elementType === 'containeryard') {
        // US 4.2.1: For storage areas: use identifier as name
        type = elementType === 'warehouse' ? 'Warehouse' : 'Container Yard';
        name = metadata.identifier || metadata.code || metadata.name || `Unknown ${type}`;
      } else {
        name = metadata.name || metadata.code || metadata.identifier || elementType;
      }

      // Clear previous selection
      this.clearSelection();

      // Highlight selected object
      this.selectedObject = intersectedObject;
      this.highlightObject(intersectedObject);

      // Enter inspection mode (disables WASD, enables orbit around object)
      this.isInspectionMode = true;

      // Move camera to focus on selected object (center and zoom)
      this.focusOnObject(intersectedObject);

      // US 4.2.5: activate/update inspection spotlight to target the selected object
      this.updateInspectionSpotlightTarget(this.selectedObject);

      // US 4.2.1: Log which US triggered the selection
      const isFacility = ['dock', 'warehouse', 'containeryard'].includes(elementType);
      const usTag = isFacility ? 'US 4.2.1' : 'US 4.2.2';
      console.log(`🎯 ${usTag}: Object picked: ${type} - ${name} (Inspection mode: ON, press ESC to exit)`);

      return {
        object: intersectedObject,
        type: type,
        name: name
      };
    }

    // No object intersected - don't clear selection (only ESC can deselect)
    console.log('⚠️ No selectable object clicked');
    return null;
  }

  /**
   * US 4.2.2: Highlight selected object with yellow outline
   */
  private highlightObject(object: THREE.Object3D): void {
    // Clear previous highlights list
    this.highlightedMeshes = [];

    console.log(`💡 Highlighting object:`, object.name, object.userData);

    // Add yellow outline by creating edge geometry
    object.traverse((child) => {
      if (child instanceof THREE.Mesh) {
        // Create outline using LineSegments
        const edges = new THREE.EdgesGeometry(child.geometry, 15);
        const line = new THREE.LineSegments(
          edges,
          new THREE.LineBasicMaterial({ color: 0xffff00, linewidth: 3 })
        );
        line.userData['isOutline'] = true;
        child.add(line);

        // Store this mesh for later restoration
        this.highlightedMeshes.push(child);
      }
    });

    console.log(`💡 Highlighted ${this.highlightedMeshes.length} meshes with yellow outline`);
  }

  /**
   * US 4.2.2: Clear selection and remove outlines
   */
  public clearSelection(): void {
    if (this.selectedObject) {
      console.log(`🧹 Clearing selection, removing outlines from ${this.highlightedMeshes.length} meshes`);

      // Remove outline from highlighted meshes
      this.highlightedMeshes.forEach((mesh) => {
        // Find and remove outline children
        const outlines = mesh.children.filter(child => child.userData['isOutline']);
        outlines.forEach(outline => {
          mesh.remove(outline);
          if (outline instanceof THREE.LineSegments) {
            outline.geometry.dispose();
            (outline.material as THREE.Material).dispose();
          }
        });
      });

      this.highlightedMeshes = [];
      this.selectedObject = null;

      // US 4.2.5: Ensure inspection spotlight is turned off when no selection
      this.updateInspectionSpotlightTarget(null);

      // Exit inspection mode
      this.exitInspectionMode();
    }
  }

  /**
   * US 4.2.2: Exit inspection mode and re-enable WASD
   */
  private exitInspectionMode(): void {
    if (this.isInspectionMode) {
      this.isInspectionMode = false;
      console.log('❌ Inspection mode OFF - WASD controls re-enabled');
    }
  }

  /**
   * US 4.2.2 & 4.2.6: Focus camera on selected object (center and zoom for inspection)
   * Uses smooth animation with TWEEN.js
   */
  private focusOnObject(object: THREE.Object3D): void {
    const box = new THREE.Box3().setFromObject(object);
    const center = box.getCenter(new THREE.Vector3());
    const size = box.getSize(new THREE.Vector3());

    // Calculate optimal distance based on object size
    const maxDim = Math.max(size.x, size.y, size.z);
    const optimalDistance = maxDim * 2.5; // Adjust multiplier for comfortable viewing

    // Clamp distance to valid range
    const targetDistance = Math.max(optimalDistance, this.MIN_DISTANCE);
    const clampedDistance = Math.min(targetDistance, this.MAX_DISTANCE);

    // US 4.2.6: Smooth animation to object using TWEEN.js
    this.animateCameraToPosition(
      center,
      clampedDistance,
      this.cameraAzimuth, // Keep current horizontal angle
      this.cameraElevation, // Keep current vertical angle
      this.animationDuration
    );
    console.log(`📷 US 4.2.6: Animating camera focus to object at (${center.x.toFixed(1)}, ${center.y.toFixed(1)}, ${center.z.toFixed(1)})`);
  }

  /**
   * Load 3D model from assets and cache it
   * @param path Path to the model file (relative to assets folder)
   * @returns Promise with the loaded model
   */
  private loadModel(path: string): Promise<THREE.Group> {
    // Return cached model if available
    if (this.modelCache.has(path)) {
      return Promise.resolve(this.modelCache.get(path)!.clone());
    }

    // Load model from file
    return new Promise((resolve, reject) => {
      this.gltfLoader.load(
        path,
        (gltf) => {
          console.log(`✅ Model loaded: ${path}`);
          this.modelCache.set(path, gltf.scene);
          resolve(gltf.scene.clone());
        },
        undefined,
        (error) => {
          console.error(`❌ Error loading model ${path}:`, error);
          reject(error);
        }
      );
    });
  }

  /**
   * Load and render port layout from JSON data
   * US 3.3.2 - 3D Port Visualization with Real Data
   * @param layout Port layout data from backend
   */
  async loadPortLayout(layout: PortLayoutDTO): Promise<void> {
    // Keep the current layout id for any layout-specific adjustments (normalize case)
    this.currentLayoutId = (layout.layoutId || '').toUpperCase();
    // Clear existing port elements
    this.clearPortElements();

    console.log(`Loading port layout: ${layout.layoutName} (${layout.layoutId})`);
    console.log(`Elements to render: ${layout.elements.length}`);

    // Render each element (now async)
    for (const element of layout.elements) {
      try {
        const mesh = await this.createPortElement(element);
        if (mesh) {
          this.scene.add(mesh);
          this.portElements.push(mesh);
        }
      } catch (error) {
        console.error(`Error creating element ${element.name}:`, error);
      }
    }

    // Add beautification decorations after core elements
    await this.addDecorations();

    // Add lighthouse after decorations
    this.addLighthouseToLayout();

    // Align docks and water edge after objects are in scene
    const side = this.alignDocksToWaterEdge();
    if (side) {
      this.alignShipsAndCranes(side);
      this.addFendersAlongDocks(side);
    }

    // After docks/water alignment, if there are no cranes, create default STS cranes and align them
    const hasAnyCrane = this.portElements.some(o => (o as any).userData?.type === 'crane');
    if (!hasAnyCrane) {
      await this.addDefaultQuayCranesForDocks(side);
      if (side) {
        this.alignShipsAndCranes(side);
      }
    }

    // Layout C: ensure exactly one yard crane if backend didn't provide any
    if (this.currentLayoutId === 'C') {
      const hasYardCrane = this.portElements.some(o => (o as any).userData?.type === 'crane' &&
        ['yard','rtg','rmg'].includes(((o as any).userData?.metadata?.craneType || '').toString().toLowerCase()));
      if (!hasYardCrane && side) {
        await this.addDefaultYardCraneForFirstYard(side);
        this.alignShipsAndCranes(side);
      }
    }

    // Layout C: Trucks now come from real PhysicalResources in database
    // No longer auto-spawning default trucks

    // Apply camera settings from layout (if provided)
    if (layout.settings && layout.settings['cameraPosition']) {
      const camPos = layout.settings['cameraPosition'] as any;
      const camTarget = layout.settings['cameraTarget'] as any;

      this.camera.position.set(camPos.x, camPos.y, camPos.z);
      this.camera.lookAt(camTarget.x, camTarget.y, camTarget.z);

      console.log(`📷 Camera updated: position (${camPos.x}, ${camPos.y}, ${camPos.z}), target (${camTarget.x}, ${camTarget.y}, ${camTarget.z})`);
    }

    console.log(`Port layout ${layout.layoutId} loaded successfully`);
  }

  /**
   * Spawn at least one STS (ship-to-shore) crane per dock when layout has none.
   * Cranes get metadata with dockCode so alignment logic can place them on the dock edge.
   */
  private async addDefaultQuayCranesForDocks(side?: 'west'|'east'|'north'|'south'): Promise<void> {
    const docks = this.portElements.filter(o => (o as any).userData?.type === 'dock');
    if (docks.length === 0) return;
    for (const dock of docks) {
      const dBox = new THREE.Box3().setFromObject(dock);
      const dSize = dBox.getSize(new THREE.Vector3());
      const dCenter = dBox.getCenter(new THREE.Vector3());
      const md = (dock as any).userData?.['metadata'] || {};
      const dockCode = md['code'] || md['name'] || undefined;

      // INCREASED: Cranes must be significantly larger than ships
      const dims = {
        length: 120,
        height: 300,
        width: 100
      } as Dimensions3D;

      const crane = await this.createCrane(dims, '#d32f2f', { craneType: 'sts', dockCode });
      // Rough placement: center or edge depending on info available
      if (dockCode && side) {
        // Let alignShipsAndCranes place it precisely later
        crane.position.copy(dCenter);
      } else if (side) {
        // Place on dock edge even without code, using current side
        const inward = this.CRANE_DOCK_INSET;
        if (side === 'west') {
          crane.position.set(dBox.min.x + inward, 0, dCenter.z);
        } else if (side === 'east') {
          crane.position.set(dBox.max.x - inward, 0, dCenter.z);
        } else if (side === 'north') {
          crane.position.set(dCenter.x, 0, dBox.min.z + inward);
        } else {
          crane.position.set(dCenter.x, 0, dBox.max.z - inward);
        }
        // Ensure base on ground
        const cBox = new THREE.Box3().setFromObject(crane);
        crane.position.y -= cBox.min.y;
      } else {
        crane.position.copy(dCenter);
      }
      crane.userData = { ...(crane as any).userData, type: 'crane', metadata: { craneType: 'sts', dockCode } };
      this.scene.add(crane);
      this.portElements.push(crane);
    }
  }

  /**
   * Add exactly one default yard crane next to the first/closest container yard when none is provided
   * (used for Layout C real-data view).
   */
  private async addDefaultYardCraneForFirstYard(side?: 'west'|'east'|'north'|'south'): Promise<void> {
    const yards = this.portElements.filter(o => (o as any).userData?.type === 'containeryard');
    if (yards.length === 0) return;

    // Pick the yard closest to the docked side if side is known, otherwise the first
    let yard = yards[0];
    if (side) {
      const score = (y: THREE.Object3D) => {
        const box = new THREE.Box3().setFromObject(y);
        const c = box.getCenter(new THREE.Vector3());
        if (side === 'west') return c.x;
        if (side === 'east') return -c.x;
        if (side === 'north') return c.z;
        return -c.z;
      };
      yards.forEach(y => { if (score(y) < score(yard)) yard = y; });
    }

    const yBox = new THREE.Box3().setFromObject(yard);
    const yCenter = yBox.getCenter(new THREE.Vector3());
    const yMeta = (yard as any).userData?.['metadata'] || {};
    const yardCode = yMeta['code'] || yMeta['name'] || yard.name || 'YARD-1';

    const dims: Dimensions3D = { length: 50, height: 100, width: 45 };
    const crane = await this.createCrane(dims, '#ffc107', { craneType: 'yard', yardCode });

    // Place outside yard edge facing docks, check for collisions
    const offset = this.YARD_CRANE_YARD_OFFSET;
    let cx = yCenter.x, cz = yCenter.z;
    if (side === 'west')      cx = yBox.min.x - offset;
    else if (side === 'east') cx = yBox.max.x + offset;
    else if (side === 'north') cz = yBox.min.z - offset;
    else if (side === 'south') cz = yBox.max.z + offset;
    crane.position.set(cx, 0, cz);

    const cBox = new THREE.Box3().setFromObject(crane);
    crane.position.y -= cBox.min.y;

    // Check collision with all existing elements and adjust if needed
    const adjusted = this.resolveYardCraneCollisions(crane, yard, side);
    crane.position.copy(adjusted);
    crane.position.y -= cBox.min.y;

    (crane as any).userData = { ...(crane as any).userData, type: 'crane', metadata: { craneType: 'yard', yardCode } };
    this.scene.add(crane);
    this.portElements.push(crane);
  }

  /**
   * Check and resolve collisions for yard crane placement
   */
  private resolveYardCraneCollisions(crane: THREE.Object3D, yard: THREE.Object3D, side?: 'west'|'east'|'north'|'south'): THREE.Vector3 {
    const craneBox = new THREE.Box3().setFromObject(crane);
    const craneSize = craneBox.getSize(new THREE.Vector3());
    const pos = crane.position.clone();
    const margin = 5;

    const obstacles = this.portElements.filter(o => {
      const t = (o as any).userData?.type;
      return o !== crane && o !== yard && t !== 'water' && t !== 'platform';
    });

    let hasCollision = true;
    let attempts = 0;
    const maxAttempts = 10;

    while (hasCollision && attempts < maxAttempts) {
      hasCollision = false;
      crane.position.copy(pos);
      const testBox = new THREE.Box3().setFromObject(crane);

      for (const obs of obstacles) {
        const obsBox = new THREE.Box3().setFromObject(obs);
        if (testBox.intersectsBox(obsBox)) {
          hasCollision = true;
          if (side === 'west') pos.x -= (craneSize.x / 2 + margin);
          else if (side === 'east') pos.x += (craneSize.x / 2 + margin);
          else if (side === 'north') pos.z -= (craneSize.z / 2 + margin);
          else if (side === 'south') pos.z += (craneSize.z / 2 + margin);
          break;
        }
      }
      attempts++;
    }

    return pos;
  }

  /**
   * Ensure object doesn't collide with any other elements, adjust position if needed
   */
  private ensureNoCollision(obj: THREE.Object3D, excludeList: THREE.Object3D[]): void {
    const platform = this.portElements.find(o => (o as any).userData?.type === 'platform');
    const obstacles = this.portElements.filter(o => {
      const t = (o as any).userData?.type;
      return o !== obj && !excludeList.includes(o) && t !== 'water' && t !== 'platform';
    });

    let attempts = 0;
    const maxAttempts = 20;
    const margin = 3;

    while (attempts < maxAttempts) {
      const objBox = new THREE.Box3().setFromObject(obj);
      let hasCollision = false;

      for (const obs of obstacles) {
        const obsBox = new THREE.Box3().setFromObject(obs);
        if (objBox.intersectsBox(obsBox)) {
          hasCollision = true;
          const objCenter = objBox.getCenter(new THREE.Vector3());
          const obsCenter = obsBox.getCenter(new THREE.Vector3());
          const direction = new THREE.Vector3().subVectors(objCenter, obsCenter).normalize();

          const objSize = objBox.getSize(new THREE.Vector3());
          const obsSize = obsBox.getSize(new THREE.Vector3());
          const overlapX = (objSize.x + obsSize.x) / 2 - Math.abs(objCenter.x - obsCenter.x);
          const overlapZ = (objSize.z + obsSize.z) / 2 - Math.abs(objCenter.z - obsCenter.z);

          if (Math.abs(overlapX) < Math.abs(overlapZ)) {
            obj.position.x += direction.x * (Math.abs(overlapX) + margin);
          } else {
            obj.position.z += direction.z * (Math.abs(overlapZ) + margin);
          }
          break;
        }
      }

      if (!hasCollision) break;

      // Check if still within platform bounds
      if (platform) {
        const pBox = new THREE.Box3().setFromObject(platform);
        const testBox = new THREE.Box3().setFromObject(obj);
        if (!pBox.containsBox(testBox)) break; // Stop if pushed outside platform
      }

      attempts++;
    }
  }

  /**
   * Create a 3D mesh for a port element based on its type
   * @param element Port element data
   * @returns Three.js mesh or null
   */
  private async createPortElement(element: PortElementDTO): Promise<THREE.Object3D | null> {
    const { type, position, dimensions, rotation, color, name } = element;

    let mesh: THREE.Object3D | null = null;

    switch (type.toLowerCase()) {
      case 'dock':
        mesh = this.createDock(dimensions, color, element.metadata);
        break;
      case 'warehouse':
        mesh = await this.createWarehouse(dimensions, color);
        break;
      case 'containeryard':
        mesh = await this.createContainerYard(dimensions, color);
        break;
      case 'truck':
        mesh = await this.createTruck(dimensions, color, element.metadata);
        break;
      case 'crane':
        // Always render crane; placement will use metadata (dockCode/yardId/craneType)
        // Pass the element name into metadata so model/type detection can use it
        mesh = await this.createCrane(dimensions, color, { ...(element.metadata || {}), name });
        break;
      case 'water':
        mesh = await this.createWater(dimensions, color, element.metadata);
        break;
      case 'platform':
        mesh = await this.createPlatform(dimensions, color);
        break;
      case 'ship':
        // Show all vessels regardless of approval status for visualization
        console.log(`🚢 Creating ship: ${name}, dimensions:`, dimensions, `metadata:`, element.metadata);
        mesh = await this.createShip(dimensions, color);
        console.log(`🚢 Ship created:`, mesh ? 'SUCCESS' : 'FAILED');
        break;
      case 'adminbuilding':
        mesh = this.createAdminBuilding(dimensions, color);
        break;
      default:
        console.warn(`Unknown element type: ${type}`);
        mesh = this.createGenericBox(dimensions, color);
    }

    if (mesh) {
      // Apply position
      const t = type.toLowerCase();
      const posYOverride = t === 'platform' ? 0 : t === 'water' ? -0.05 : t === 'ship' ? -0.05 : position.y;
      mesh.position.set(position.x, posYOverride, position.z);

      // Apply rotation (convert degrees to radians)
      mesh.rotation.set(
        rotation.x * Math.PI / 180,
        rotation.y * Math.PI / 180,
        rotation.z * Math.PI / 180
      );

      // Add name and metadata for debugging
      mesh.name = name;
      (mesh as any).userData = { ...(mesh as any).userData, ['type']: t, ['metadata']: (element as any).metadata };

      console.log(`✅ Created ${type}: ${name} at (${position.x}, ${posYOverride}, ${position.z})`);
    }

    return mesh;
  }

  /**
   * Create a dock using DockService (without pier - pier added separately)
   */
  private createDock(dimensions: Dimensions3D, color: string, metadata?: any): THREE.Group {
    return this.dockService.createDock(
      { length: dimensions.length, width: dimensions.width, height: dimensions.height },
      color,
      metadata
    );
  }

  /**
   * Create pier that extends from dock edge into water
   */
  private createPierForDock(dockSize: THREE.Vector3, side: 'west'|'east'|'north'|'south'): THREE.Group {
    const pierGroup = new THREE.Group();

    const pierWidth = dockSize.z * 0.8;
    const pierLength = 80; // Extends into water
    const pierHeight = dockSize.y * 0.6;

    const pierGeometry = new THREE.BoxGeometry(
      side === 'west' || side === 'east' ? pierLength : pierWidth,
      pierHeight,
      side === 'west' || side === 'east' ? pierWidth : pierLength
    );

    const pierMaterial = new THREE.MeshStandardMaterial({
      color: 0x8b7355,
      metalness: 0.2,
      roughness: 0.8
    });

    const pier = new THREE.Mesh(pierGeometry, pierMaterial);

    // Position pier extending from origin
    if (side === 'west') {
      pier.position.set(-pierLength / 2, 0, 0);
    } else if (side === 'east') {
      pier.position.set(pierLength / 2, 0, 0);
    } else if (side === 'north') {
      pier.position.set(0, 0, -pierLength / 2);
    } else {
      pier.position.set(0, 0, pierLength / 2);
    }

    const edges = new THREE.EdgesGeometry(pierGeometry);
    const edgeLine = new THREE.LineSegments(edges, new THREE.LineBasicMaterial({ color: 0x000000 }));
    pier.add(edgeLine);

    pierGroup.add(pier);
    return pierGroup;
  }

  /**
   * Procedural dock creation (backup method)
   */
  private createProceduralDock(dimensions: Dimensions3D, color: string): THREE.Group {
    const dockGroup = new THREE.Group();

    // Main platform (concrete surface)
    const platformGeometry = new THREE.BoxGeometry(dimensions.length, dimensions.height, dimensions.width);
    const platformMaterial = new THREE.MeshStandardMaterial({
      color: new THREE.Color(color),
      metalness: 0.2,
      roughness: 0.8
    });
    const platform = new THREE.Mesh(platformGeometry, platformMaterial);
    dockGroup.add(platform);

    // Platform edges (black lines)
    const edges = new THREE.EdgesGeometry(platformGeometry);
    const edgeLine = new THREE.LineSegments(edges, new THREE.LineBasicMaterial({ color: 0x000000 }));
    dockGroup.add(edgeLine);

    // Bollards (mooring posts) along the edge
    const bollardRadius = Math.min(dimensions.length, dimensions.width) * 0.02;
    const bollardHeight = dimensions.height * 3;
    const bollardGeometry = new THREE.CylinderGeometry(bollardRadius, bollardRadius, bollardHeight, 8);
    const bollardMaterial = new THREE.MeshStandardMaterial({
      color: 0x2c3e50,
      metalness: 0.7,
      roughness: 0.3
    });

    // Place bollards along the length
    const numBollards = Math.max(3, Math.floor(dimensions.length / 30));
    for (let i = 0; i < numBollards; i++) {
      const bollard = new THREE.Mesh(bollardGeometry, bollardMaterial);
      const xPos = (i / (numBollards - 1) - 0.5) * dimensions.length * 0.9;
      const zPos = dimensions.width / 2 - bollardRadius;
      bollard.position.set(xPos, dimensions.height / 2 + bollardHeight / 2, zPos);
      dockGroup.add(bollard);
    }

    // Yellow painted lines on dock surface
    const lineWidth = dimensions.length * 0.95;
    const lineThickness = 0.1;
    const lineDepth = dimensions.width * 0.05;
    const lineGeometry = new THREE.BoxGeometry(lineWidth, lineThickness, lineDepth);
    const lineMaterial = new THREE.MeshStandardMaterial({
      color: 0xffd700,
      emissive: 0x554400,
      metalness: 0,
      roughness: 1
    });

    // Add 3 parallel lines
    for (let i = -1; i <= 1; i++) {
      const line = new THREE.Mesh(lineGeometry, lineMaterial);
      line.position.set(0, dimensions.height / 2 + lineThickness, i * dimensions.width * 0.25);
      dockGroup.add(line);
    }
    // Sit the entire dock on the ground (bottom at y=0)
    dockGroup.position.y = dimensions.height / 2;

    return dockGroup;
  }

  /**
   * Create a warehouse using StorageAreaService
   */
  private async createWarehouse(dimensions: Dimensions3D, color: string, metadata?: any): Promise<THREE.Group> {
    return this.storageAreaService.createWarehouse(
      { length: dimensions.length, depth: dimensions.width, height: dimensions.height },
      color,
      metadata
    );
  }

  /**
   * Create a container yard using StorageAreaService
   */
  private async createContainerYard(dimensions: Dimensions3D, color: string, metadata?: any): Promise<THREE.Group> {
    return this.storageAreaService.createContainerYard(
      { length: dimensions.length, width: dimensions.width },
      color,
      metadata
    );
  }

  /**
   * Procedural warehouse creation (backup method)
   */
  private createProceduralWarehouse(dimensions: Dimensions3D, color: string): THREE.Group {
    const warehouseGroup = new THREE.Group();

    // Main building body
    const bodyGeometry = new THREE.BoxGeometry(dimensions.length, dimensions.height, dimensions.width);
    const bodyMaterial = new THREE.MeshStandardMaterial({
      color: new THREE.Color(color),
      metalness: 0.5,
      roughness: 0.7
    });
    const body = new THREE.Mesh(bodyGeometry, bodyMaterial);
    warehouseGroup.add(body);

    // Building edges
    const bodyEdges = new THREE.EdgesGeometry(bodyGeometry);
    const edgeLine = new THREE.LineSegments(bodyEdges, new THREE.LineBasicMaterial({ color: 0x000000 }));
    warehouseGroup.add(edgeLine);

    // Sloped roof (two angled panels forming peak)
    const roofHeight = dimensions.height * 0.3;
    const roofOverhang = dimensions.length * 0.05;

    // Left roof panel
    const leftRoofGeometry = new THREE.BoxGeometry(
      dimensions.length + roofOverhang,
      roofHeight * 0.3,
      dimensions.width / 2 + roofOverhang
    );
    const roofMaterial = new THREE.MeshStandardMaterial({
      color: 0x4a4a4a,
      metalness: 0.7,
      roughness: 0.4
    });
    const leftRoof = new THREE.Mesh(leftRoofGeometry, roofMaterial);
    leftRoof.rotation.z = Math.PI / 6; // 30 degree angle
    leftRoof.position.set(0, dimensions.height / 2 + roofHeight * 0.4, -dimensions.width / 4);
    warehouseGroup.add(leftRoof);

    // Right roof panel
    const rightRoof = new THREE.Mesh(leftRoofGeometry, roofMaterial);
    rightRoof.rotation.z = -Math.PI / 6;
    rightRoof.position.set(0, dimensions.height / 2 + roofHeight * 0.4, dimensions.width / 4);
    warehouseGroup.add(rightRoof);

    // Doors on front face
    const doorWidth = dimensions.length * 0.15;
    const doorHeight = dimensions.height * 0.7;
    const doorThickness = 0.2;
    const doorGeometry = new THREE.BoxGeometry(doorWidth, doorHeight, doorThickness);
    const doorMaterial = new THREE.MeshStandardMaterial({
      color: 0x1a1a1a,
      metalness: 0.6,
      roughness: 0.5
    });

    // 3 doors evenly spaced
    for (let i = -1; i <= 1; i++) {
      const door = new THREE.Mesh(doorGeometry, doorMaterial);
      door.position.set(
        i * dimensions.length * 0.3,
        -dimensions.height * 0.15,
        dimensions.width / 2 + doorThickness / 2
      );
      warehouseGroup.add(door);
    }

    // Small windows
    const windowSize = dimensions.height * 0.08;
    const windowGeometry = new THREE.BoxGeometry(windowSize, windowSize, 0.1);
    const windowMaterial = new THREE.MeshStandardMaterial({
      color: 0x87ceeb,
      metalness: 0.9,
      roughness: 0.1,
      emissive: 0x335577
    });

    // Add windows on side
    const numWindows = Math.floor(dimensions.length / (windowSize * 3));
    for (let i = 0; i < numWindows; i++) {
      const window = new THREE.Mesh(windowGeometry, windowMaterial);
      const xPos = (i / numWindows - 0.5) * dimensions.length * 0.8;
      window.position.set(xPos, dimensions.height * 0.2, dimensions.width / 2 + 0.1);
      warehouseGroup.add(window);
    }

    return warehouseGroup;
  }

  /**
   * Create a container yard using loaded 3D model
   */
  /**
   * Create a crane using PhysicalResourceService
   */
  private async createCrane(dimensions: Dimensions3D, color: string, metadata?: any): Promise<THREE.Group> {
    return this.physicalResourceService.createCrane(
      { width: dimensions.width, height: dimensions.height, length: dimensions.length },
      color,
      metadata
    );
  }

  /**
   * Procedural gantry crane creation (backup method)
   */
  private createProceduralCrane(dimensions: Dimensions3D, color: string): THREE.Group {
    const craneGroup = new THREE.Group();
    const craneColor = new THREE.Color(color);

    const legRadius = dimensions.length * 0.02;
    const legHeight = dimensions.height;
    const beamThickness = dimensions.length * 0.03;

    // Material for crane structure
    const craneMaterial = new THREE.MeshStandardMaterial({
      color: craneColor,
      metalness: 0.8,
      roughness: 0.3
    });

    // Left leg (vertical support)
    const leftLegGeometry = new THREE.CylinderGeometry(legRadius, legRadius, legHeight, 8);
    const leftLeg = new THREE.Mesh(leftLegGeometry, craneMaterial);
    leftLeg.position.set(-dimensions.length / 2.5, legHeight / 2, 0);
    craneGroup.add(leftLeg);

    // Right leg (vertical support)
    const rightLeg = new THREE.Mesh(leftLegGeometry, craneMaterial);
    rightLeg.position.set(dimensions.length / 2.5, legHeight / 2, 0);
    craneGroup.add(rightLeg);

    // Horizontal beam connecting tops (gantry span)
    const beamGeometry = new THREE.BoxGeometry(
      dimensions.length * 0.9,
      beamThickness,
      beamThickness
    );
    const beam = new THREE.Mesh(beamGeometry, craneMaterial);
    beam.position.y = legHeight;
    craneGroup.add(beam);

    // Trolley (moving carriage on beam)
    const trolleyGeometry = new THREE.BoxGeometry(
      beamThickness * 2,
      beamThickness * 1.5,
      beamThickness * 2
    );
    const trolleyMaterial = new THREE.MeshStandardMaterial({
      color: 0x303030,
      metalness: 0.7,
      roughness: 0.4
    });
    const trolley = new THREE.Mesh(trolleyGeometry, trolleyMaterial);
    trolley.position.set(dimensions.length * 0.1, legHeight - beamThickness, 0);
    craneGroup.add(trolley);

    // Cable hanging from trolley
    const cableGeometry = new THREE.CylinderGeometry(0.1, 0.1, legHeight * 0.4, 6);
    const cableMaterial = new THREE.MeshStandardMaterial({
      color: 0x1a1a1a,
      metalness: 0.9,
      roughness: 0.2
    });
    const cable = new THREE.Mesh(cableGeometry, cableMaterial);
    cable.position.set(dimensions.length * 0.1, legHeight - legHeight * 0.3, 0);
    craneGroup.add(cable);

    // Hook at end of cable
    const hookGeometry = new THREE.SphereGeometry(legRadius * 2, 8, 6);
    const hook = new THREE.Mesh(hookGeometry, craneMaterial);
    hook.position.set(dimensions.length * 0.1, legHeight - legHeight * 0.5, 0);
    craneGroup.add(hook);

    // Base rails on ground
    const railGeometry = new THREE.BoxGeometry(
      dimensions.length * 0.95,
      beamThickness * 0.3,
      beamThickness * 0.5
    );
    const railMaterial = new THREE.MeshStandardMaterial({
      color: 0x654321,
      metalness: 0.6,
      roughness: 0.5
    });

    // Left rail
    const leftRail = new THREE.Mesh(railGeometry, railMaterial);
    leftRail.position.set(0, 0, -dimensions.length / 2.5);
    craneGroup.add(leftRail);

    // Right rail
    const rightRail = new THREE.Mesh(railGeometry, railMaterial);
    rightRail.position.set(0, 0, dimensions.length / 2.5);
    craneGroup.add(rightRail);

    return craneGroup;
  }

  /**
   * Create water surface using loaded ocean model
   */
  private async createWater(dimensions: Dimensions3D, color: string, metadata?: any): Promise<THREE.Group> {
    try {
      // Try to load animated ocean model
      const model = await this.loadModel('assets/models/ocean__water_perfect_loop.glb');

      // Scale to cover the entire area
      const box = new THREE.Box3().setFromObject(model);
      const size = box.getSize(new THREE.Vector3());
      const center = box.getCenter(new THREE.Vector3());

      console.log(`🌊 Water model original size: ${size.x.toFixed(2)} × ${size.y.toFixed(2)} × ${size.z.toFixed(2)}`);

      // Center the model
      model.position.sub(center);

      model.scale.set(
        dimensions.length / size.x,
        1,
        dimensions.width / size.z
      );

      console.log(`🌊 Water scaled to: ${dimensions.length} × ${dimensions.width}`);

      // Make water slightly transparent
      model.traverse((child) => {
        if (child instanceof THREE.Mesh) {
          child.material.transparent = true;
          child.material.opacity = 0.85;
          child.receiveShadow = false;
          child.castShadow = false;
        }
      });

      return model;
    } catch (error) {
      console.warn('Failed to load ocean model, using procedural water:', error);
      return this.createProceduralWater(dimensions, color);
    }
  }

  // Decorations: roads, fence, lights, rails, buoys, breakwater, gatehouse
  private async addDecorations(): Promise<void> {
    const platform = this.portElements.find(o => (o as any).userData?.type === 'platform');
    const water = this.portElements.find(o => (o as any).userData?.type === 'water');
    const docks = this.portElements.filter(o => (o as any).userData?.type === 'dock');
    if (!platform) return;

    const pBox = new THREE.Box3().setFromObject(platform);
    const wBox = water ? new THREE.Box3().setFromObject(water) : null;

    this.addRoads(pBox);
    this.addFence(pBox);
    this.addLightPoles(pBox);
    this.addRailsNearDocks(docks);
    if (wBox) {
      this.addBuoys(wBox);
      this.addBreakwaterAlongEdge(pBox, wBox);
    }
    this.addGateHouse(pBox);
  }

  private addRoads(bounds: THREE.Box3): void {
    const group = new THREE.Group();
    const width = bounds.max.x - bounds.min.x;
    const depth = bounds.max.z - bounds.min.z;
    const centerX = (bounds.min.x + bounds.max.x) / 2;
    const centerZ = (bounds.min.z + bounds.max.z) / 2;

    const roadMat = new THREE.MeshStandardMaterial({ color: 0x2f2f2f, roughness: 0.9 });
    const stripeMat = new THREE.MeshStandardMaterial({ color: 0xdddd00, emissive: 0x333300, roughness: 1 });

    const mainRoad = new THREE.Mesh(new THREE.BoxGeometry(width * 0.9, 0.1, depth * 0.05), roadMat);
    mainRoad.position.set(centerX, 0.05, centerZ);
    group.add(mainRoad);

    const cross1 = new THREE.Mesh(new THREE.BoxGeometry(width * 0.25, 0.1, depth * 0.03), roadMat);
    cross1.position.set(centerX - width * 0.25, 0.05, centerZ + depth * 0.15);
    group.add(cross1);

    const cross2 = new THREE.Mesh(new THREE.BoxGeometry(width * 0.25, 0.1, depth * 0.03), roadMat);
    cross2.position.set(centerX + width * 0.25, 0.05, centerZ - depth * 0.15);
    group.add(cross2);

    // Dashed center stripes on main road
    const stripeLen = width * 0.04;
    const gap = width * 0.02;
    const count = Math.floor((width * 0.9) / (stripeLen + gap));
    for (let i = 0; i < count; i++) {
      const stripe = new THREE.Mesh(new THREE.BoxGeometry(stripeLen, 0.2, depth * 0.003), stripeMat);
      const start = centerX - (count / 2) * (stripeLen + gap) + i * (stripeLen + gap);
      stripe.position.set(start, 0.11, centerZ);
      group.add(stripe);
    }

    group.userData['type'] = 'roads';
    this.scene.add(group);
    this.portElements.push(group);
  }

  private addFence(bounds: THREE.Box3): void {
    const group = new THREE.Group();
    const fenceMat = new THREE.MeshStandardMaterial({ color: 0x888888, metalness: 0.7, roughness: 0.3 });
    const height = 3;
    const thickness = 0.1;
    const segments = 30;
    const addSide = (x1: number, z1: number, x2: number, z2: number) => {
      for (let i = 0; i <= segments; i++) {
        const t = i / segments;
        const x = x1 + (x2 - x1) * t;
        const z = z1 + (z2 - z1) * t;
        const post = new THREE.Mesh(new THREE.BoxGeometry(thickness, height, thickness), fenceMat);
        post.position.set(x, height / 2, z);
        group.add(post);
      }
    };
    addSide(bounds.min.x, bounds.min.z, bounds.max.x, bounds.min.z);
    addSide(bounds.max.x, bounds.min.z, bounds.max.x, bounds.max.z);
    addSide(bounds.max.x, bounds.max.z, bounds.min.x, bounds.max.z);
    addSide(bounds.min.x, bounds.max.z, bounds.min.x, bounds.min.z);
    group.userData['type'] = 'fence';
    this.scene.add(group);
    this.portElements.push(group);
  }

  private addLightPoles(bounds: THREE.Box3): void {
    const group = new THREE.Group();
    const poleMat = new THREE.MeshStandardMaterial({ color: 0xcccccc, metalness: 0.6, roughness: 0.4 });
    const height = 12;
    const count = 10;
    for (let i = 0; i < count; i++) {
      const t = i / (count - 1);
      const x = bounds.min.x + t * (bounds.max.x - bounds.min.x);
      const z = bounds.max.z - 2;
      const pole = new THREE.Mesh(new THREE.CylinderGeometry(0.15, 0.15, height, 8), poleMat);
      pole.position.set(x, height / 2, z);
      const lamp = new THREE.Mesh(new THREE.BoxGeometry(0.6, 0.3, 0.6), new THREE.MeshStandardMaterial({ color: 0xffffee, emissive: 0x444400 }));
      lamp.position.set(0, height / 2, 0);
      pole.add(lamp);
      const light = new THREE.PointLight(0xffffff, 0.8, 60);
      light.position.set(0, height / 2, 0);
      pole.add(light);
      group.add(pole);
    }
    group.userData['type'] = 'lightpoles';
    this.scene.add(group);
    this.portElements.push(group);
  }

  private addRailsNearDocks(docks: THREE.Object3D[]): void {
    docks.forEach(dock => {
      const box = new THREE.Box3().setFromObject(dock);
      const len = (box.max.x - box.min.x) * 0.9;
      const offsetZ = (box.max.z - box.min.z) * 0.6;
      const center = new THREE.Vector3();
      box.getCenter(center);
      const mat = new THREE.MeshStandardMaterial({ color: 0x6b4f2e, metalness: 0.4, roughness: 0.6 });
      const rail1 = new THREE.Mesh(new THREE.BoxGeometry(len, 0.2, 0.3), mat);
      rail1.position.set(center.x, 0.1, center.z - offsetZ);
      const rail2 = new THREE.Mesh(new THREE.BoxGeometry(len, 0.2, 0.3), mat);
      rail2.position.set(center.x, 0.1, center.z + offsetZ);
      const group = new THREE.Group();
      group.add(rail1);
      group.add(rail2);
      group.userData['type'] = 'rails';
      this.scene.add(group);
      this.portElements.push(group);
    });
  }

  // Add rubber fenders along the water-facing edge of each dock
  private addFendersAlongDocks(side: 'west'|'east'|'north'|'south'): void {
    const docks = this.portElements.filter(o => (o as any).userData?.type === 'dock');
    const fenderMat = new THREE.MeshStandardMaterial({ color: 0x222222, metalness: 0.2, roughness: 0.9 });
    docks.forEach(dock => {
      const box = new THREE.Box3().setFromObject(dock);
      const min = box.min, max = box.max;
      const height = max.y - min.y;
      const y = min.y + height * 0.35; // around lower-mid height of dock wall
      const group = new THREE.Group();
      const count = 12;
      const thickness = 0.2;
      const lengthAlong = side === 'west' || side === 'east' ? (max.z - min.z) : (max.x - min.x);
      const step = lengthAlong / (count + 1);
      for (let i = 1; i <= count; i++) {
        // Fender dimensions: long along the dock edge direction, shallow depth
        const long = Math.min(2.0, step * 0.8);
        const tall = Math.min(1.0, height * 0.4);
        const geomWE = new THREE.BoxGeometry(thickness, tall, long); // for west/east sides
        const geomNS = new THREE.BoxGeometry(long, tall, thickness); // for north/south sides
        const mesh = new THREE.Mesh((side === 'west' || side === 'east') ? geomWE : geomNS, fenderMat);
        if (side === 'west') {
          const z = min.z + i * step;
          mesh.position.set(min.x - thickness * 0.5, y, z);
        } else if (side === 'east') {
          const z = min.z + i * step;
          mesh.position.set(max.x + thickness * 0.5, y, z);
        } else if (side === 'north') {
          const x = min.x + i * step;
          mesh.position.set(x, y, min.z - thickness * 0.5);
        } else { // south
          const x = min.x + i * step;
          mesh.position.set(x, y, max.z + thickness * 0.5);
        }
        group.add(mesh);
      }
      group.userData['type'] = 'fenders';
      this.scene.add(group);
      this.portElements.push(group);
    });
  }

  private addBuoys(waterBounds: THREE.Box3): void {
    const group = new THREE.Group();
    const count = 6;
    for (let i = 0; i < count; i++) {
      const x = THREE.MathUtils.lerp(waterBounds.min.x, waterBounds.max.x, (i + 1) / (count + 1));
      const z = waterBounds.min.z + ((i % 2 === 0) ? 10 : -10);
      const buoy = new THREE.Group();
      const base = new THREE.Mesh(new THREE.CylinderGeometry(0.6, 1, 1.2, 8), new THREE.MeshStandardMaterial({ color: 0xff3d00, metalness: 0.6, roughness: 0.5 }));
      base.position.y = 0.6;
      const top = new THREE.Mesh(new THREE.ConeGeometry(0.4, 0.8, 8), new THREE.MeshStandardMaterial({ color: 0xffffff }));
      top.position.y = 1.4;
      buoy.add(base);
      buoy.add(top);
      buoy.position.set(x, -0.05, z);
      group.add(buoy);
    }
    group.userData['type'] = 'buoys';
    this.scene.add(group);
    this.portElements.push(group);
  }

  private addBreakwaterAlongEdge(platformBounds: THREE.Box3, waterBounds: THREE.Box3): void {
    const group = new THREE.Group();
    const edgeZ = platformBounds.min.z; // assume water near negative Z edge
    const startX = platformBounds.min.x;
    const endX = platformBounds.max.x;
    const step = 6;
    for (let x = startX; x <= endX; x += step) {
      const rock = new THREE.Mesh(
        new THREE.DodecahedronGeometry(THREE.MathUtils.randFloat(1.2, 2.4)),
        new THREE.MeshStandardMaterial({ color: 0x7d7d7d, roughness: 1 })
      );
      rock.position.set(x + THREE.MathUtils.randFloat(-0.5, 0.5), 0.6, edgeZ - THREE.MathUtils.randFloat(1, 2));
      group.add(rock);
    }
    group.userData['type'] = 'breakwater';
    this.scene.add(group);
    this.portElements.push(group);
  }

  private addGateHouse(bounds: THREE.Box3): void {
    const group = new THREE.Group();
    const body = new THREE.Mesh(new THREE.BoxGeometry(6, 3, 4), new THREE.MeshStandardMaterial({ color: 0x8d6e63, roughness: 0.7 }));
    const roof = new THREE.Mesh(new THREE.BoxGeometry(6.2, 0.4, 4.2), new THREE.MeshStandardMaterial({ color: 0x4e342e, metalness: 0.2, roughness: 0.8 }));
    roof.position.y = 1.7;
    body.add(roof);
    body.position.set(bounds.min.x + 6, 1.5, bounds.max.z - 6);
    group.add(body);
    group.userData['type'] = 'gatehouse';
    this.scene.add(group);
    this.portElements.push(group);
  }

  // Align ships next to docks on the water side and place cranes on the dock edge near ships
  private alignShipsAndCranes(side: 'west'|'east'|'north'|'south'): void {
    const docks = this.portElements.filter(o => (o as any).userData?.['type'] === 'dock');
    const ships = this.portElements.filter(o => (o as any).userData?.['type'] === 'ship');
    const cranes = this.portElements.filter(o => (o as any).userData?.['type'] === 'crane');
    const yards  = this.portElements.filter(o => (o as any).userData?.['type'] === 'containeryard');
    const platform = this.portElements.find(o => (o as any).userData?.['type'] === 'platform');

    console.log(`🚢 alignShipsAndCranes called with side: ${side}, found ${ships.length} ships, ${docks.length} docks, ${cranes.length} cranes`);

    // Index docks by several identifiers to match cranes from DB
    const dockByCode = new Map<string, THREE.Object3D>();
    const dockById   = new Map<string, THREE.Object3D>();
    const dockByName = new Map<string, THREE.Object3D>();
    docks.forEach(d => {
      const md = (d as any).userData?.['metadata'] || {};
      if (md['code']) dockByCode.set(String(md['code']), d);
      if (md['id'])   dockById.set(String(md['id']), d);
      if (md['name']) dockByName.set(String(md['name']), d);
    });
    // Map yards by id/code/name to support yard crane placement
    const yardById = new Map<string, THREE.Object3D>();
    yards.forEach(y => {
      const md = (y as any).userData?.['metadata'] || {};
      if (md['id']) yardById.set(String(md['id']), y);
      if (md['code']) yardById.set(String(md['code']), y);
      yardById.set(y.name, y);
    });

    // Track where each ship ends up along the dock axis (for crane alignment)
    const shipAlongDock = new Map<string, number>();

    // Find water and platform bounds for safety clamping
    const water = this.portElements.find(o => (o as any).userData?.['type'] === 'water');
    const waterBounds = water ? new THREE.Box3().setFromObject(water) : undefined;
    const platformBounds = platform ? new THREE.Box3().setFromObject(platform) : undefined;

    // Group ships by dock code to handle multiple ships per dock
    const shipsByDock = new Map<string, THREE.Object3D[]>();
    ships.forEach(s => {
      const md = (s as any).userData?.['metadata'] || {};
      const code = String(md['dockCode'] || 'UNKNOWN');
      console.log(`🚢 Ship ${s.name} assigned to dock: ${code}`);
      if (!shipsByDock.has(code)) shipsByDock.set(code, []);
      shipsByDock.get(code)!.push(s);
    });
    console.log(`🚢 Ships grouped by dock:`, Array.from(shipsByDock.entries()).map(([k,v]) => `${k}: ${v.length} ships`).join(', '));

    // Ships use backend positions - only adjust rotation to align with dock
    ships.forEach(s => {
      const md = (s as any).userData?.['metadata'] || {};
      const code = md['dockCode'];
      const dock = dockByCode.get(String(code));
      if (!dock) return;
      const dBox = new THREE.Box3().setFromObject(dock);
      const dSize = dBox.getSize(new THREE.Vector3());
      const dCenter = dBox.getCenter(new THREE.Vector3());

      // Determine dock long axis and align ship rotation with it
      const dockAxis: 'x'|'z' = dSize.x >= dSize.z ? 'x' : 'z';
      s.rotation.y = dockAxis === 'x' ? 0 : Math.PI / 2;

      // Layout C: ensure ship sits in water adjacent to dock edge, with a small configurable gap
      if (this.currentLayoutId === 'C' && platformBounds && waterBounds) {
        const shipBox = new THREE.Box3().setFromObject(s);
        const shipSize = shipBox.getSize(new THREE.Vector3());
        const halfX = shipSize.x / 2;
        const halfZ = shipSize.z / 2;
        // Slightly increased gap (max 18) to push ship a bit further back as requested
        const gap = Math.max(4.0, Math.min(this.SHIP_WATER_GAP + 8.0, 18.0));
        if (side === 'west') {
          s.position.x = platformBounds.min.x - gap - halfX;
          // Clamp within water
          const wMin = waterBounds.min.x + halfX + 1;
          const wMax = waterBounds.max.x - halfX - 1;
          s.position.x = THREE.MathUtils.clamp(s.position.x, wMin, wMax);
        } else if (side === 'east') {
          s.position.x = platformBounds.max.x + gap + halfX;
          const wMin = waterBounds.min.x + halfX + 1;
          const wMax = waterBounds.max.x - halfX - 1;
          s.position.x = THREE.MathUtils.clamp(s.position.x, wMin, wMax);
        } else if (side === 'north') {
          s.position.z = platformBounds.min.z - gap - halfZ;
          const wMin = waterBounds.min.z + halfZ + 1;
          const wMax = waterBounds.max.z - halfZ - 1;
          s.position.z = THREE.MathUtils.clamp(s.position.z, wMin, wMax);
        } else if (side === 'south') {
          s.position.z = platformBounds.max.z + gap + halfZ;
          const wMin = waterBounds.min.z + halfZ + 1;
          const wMax = waterBounds.max.z - halfZ - 1;
          s.position.z = THREE.MathUtils.clamp(s.position.z, wMin, wMax);
        }
      }

      // Layout B: backend provides correct positions for ships, so skip position overrides
      // Only apply rotation alignment
      if (this.currentLayoutId === 'B') {
        // Keep backend position, just track for crane alignment
        if (side === 'west' || side === 'east') {
          shipAlongDock.set(String(code), s.position.z);
        } else {
          shipAlongDock.set(String(code), s.position.x);
        }
        return; // skip further position adjustments
      }

      // Layout B only: force ships to sit fully on water right next to the platform (clear of asphalt)
      if (this.currentLayoutId === 'B' && platformBounds) {
        const pCenter = platformBounds.getCenter(new THREE.Vector3());
        const sBox0 = new THREE.Box3().setFromObject(s);
        const sSize0 = sBox0.getSize(new THREE.Vector3());
        const half = Math.max(0.1, sSize0.x / 2);
        // Ensure a reasonable clearance for B regardless of current default
        const gap = Math.max(this.SHIP_WATER_GAP, this.LAYOUT_B_CLEARANCE);

        let targetX: number | undefined;
        if (s.position.x < pCenter.x) {
          // West side: just left of platform's left edge
          targetX = platformBounds.min.x - gap - half;
          if (waterBounds) {
            const minAllowedCenter = waterBounds.min.x + 1 + half;
            if (targetX < minAllowedCenter) targetX = minAllowedCenter;
          }
        } else {
          // East side: just right of platform's right edge
          targetX = platformBounds.max.x + gap + half;
          if (waterBounds) {
            const maxAllowedCenter = waterBounds.max.x - 1 - half;
            if (targetX > maxAllowedCenter) targetX = maxAllowedCenter;
          }
        }
        if (targetX !== undefined) {
          s.position.x = targetX;
          // Re-check after move; if any edge still intersects platform, push a little more
          const sBox1 = new THREE.Box3().setFromObject(s);
          const epsilon = 3; // slightly larger visual buffer
          // West overlap check
          if (s.position.x < pCenter.x) {
            const desiredMaxX = platformBounds.min.x - gap - epsilon;
            if (sBox1.max.x > desiredMaxX) {
              const extra = sBox1.max.x - desiredMaxX;
              s.position.x -= extra;
            }
          } else { // East overlap check
            const desiredMinX = platformBounds.max.x + gap + epsilon;
            if (sBox1.min.x < desiredMinX) {
              const extra = desiredMinX - sBox1.min.x;
              s.position.x += extra;
            }
          }
        }
      }

      // Layout B only: snap ships to water side next to platform so none sit on asphalt
      if (this.currentLayoutId === 'B' && platformBounds) {
        const pCenter = platformBounds.getCenter(new THREE.Vector3());
        const sBox = new THREE.Box3().setFromObject(s);
        const sSize = sBox.getSize(new THREE.Vector3());
        const halfX = sSize.x / 2;

        // Decide side based on relative displacement to platform center
        const alongX = Math.abs(s.position.x - pCenter.x) >= Math.abs(s.position.z - pCenter.z);
        if (alongX) {
          if (s.position.x < pCenter.x) {
            // West of platform → place to the left edge, over water
            const platformLeft = platformBounds.min.x;
            s.position.x = platformLeft - this.SHIP_WATER_GAP - halfX;
          } else {
            // East of platform → place to the right edge, over water
            const platformRight = platformBounds.max.x;
            s.position.x = platformRight + this.SHIP_WATER_GAP + halfX;
          }
        }

        // Optional guard: keep within water if bounds known
        if (waterBounds) {
          const wMin = waterBounds.min.x + halfX;
          const wMax = waterBounds.max.x - halfX;
          s.position.x = Math.min(wMax, Math.max(wMin, s.position.x));
        }
      }

      // Track ship position along dock for crane placement (use existing position from backend, after any safe nudge)
      if (side === 'west' || side === 'east') {
        shipAlongDock.set(String(code), s.position.z);
      } else {
        shipAlongDock.set(String(code), s.position.x);
      }
    });

    // Distribute multiple ships along the same dock - stack behind each other
    shipsByDock.forEach((shipsForDock, dockCode) => {
      console.log(`🚢 Distributing ${shipsForDock.length} ships for dock ${dockCode}`);
      if (shipsForDock.length <= 1) return; // No overlap if only one ship

      const dock = dockByCode.get(dockCode);
      if (!dock) return;

      const dBox = new THREE.Box3().setFromObject(dock);
      const dSize = dBox.getSize(new THREE.Vector3());
      const dCenter = dBox.getCenter(new THREE.Vector3());

      const shipSpacing = 40; // Space between ships stacked behind each other
      const shipSizes = shipsForDock.map(s => {
        const sBox = new THREE.Box3().setFromObject(s);
        return sBox.getSize(new THREE.Vector3());
      });

      // Stack ships behind each other (perpendicular to dock, away from dock)
      const isAlongX = (side === 'west' || side === 'east');
      const dockAlignPos = isAlongX ? dCenter.z : dCenter.x; // Keep aligned with dock center

      shipsForDock.forEach((ship, idx) => {
        const shipSize = shipSizes[idx];
        const halfSize = (isAlongX ? shipSize.z : shipSize.x) / 2;

        // Position along dock axis: all ships aligned with dock center
        if (isAlongX) {
          ship.position.z = dockAlignPos;
        } else {
          ship.position.x = dockAlignPos;
        }

        // Position perpendicular to dock: stack away from dock
        const stackOffset = idx * (shipSize.x + shipSpacing); // Use ship length for stacking
        if (side === 'west') {
          ship.position.x -= stackOffset;
        } else if (side === 'east') {
          ship.position.x += stackOffset;
        } else if (side === 'north') {
          ship.position.z -= stackOffset;
        } else {
          ship.position.z += stackOffset;
        }

        console.log(`🚢 Ship ${idx} for dock ${dockCode} stacked at position (${ship.position.x.toFixed(2)}, ${ship.position.z.toFixed(2)})`);
      });

      // Update tracking with dock center position
      if (isAlongX) {
        shipAlongDock.set(dockCode, dockAlignPos);
      } else {
        shipAlongDock.set(dockCode, dockAlignPos);
      }
    });

    // Group cranes by dock code to handle multiple cranes per dock
    const cranesByDock = new Map<string, THREE.Object3D[]>();
    const yardCranes: THREE.Object3D[] = [];

    cranes.forEach(c => {
      const md = (c as any).userData?.['metadata'] || {};
      const craneKind = (md['craneType'] || md['type'] || md['model'] || '').toString().toLowerCase();
      const looksSTS = craneKind.includes('sts') || craneKind.includes('quay') || craneKind.includes('ship') || craneKind.includes('tower');
      const looksYard = craneKind.includes('yard') || craneKind.includes('rtg') || craneKind.includes('rmg');
      const yardKey = md['yardId'] || md['yardCode'] || undefined;
      const isYardCrane = !looksSTS && (looksYard || !!yardKey);

      if (isYardCrane) {
        yardCranes.push(c);
      } else {
        const code = String(md['dockCode'] || 'UNKNOWN');
        if (!cranesByDock.has(code)) cranesByDock.set(code, []);
        cranesByDock.get(code)!.push(c);
      }
    });

    // Place cranes: STS/tower to dock edge; yard cranes near their container yard
    cranes.forEach(c => {
      const md = (c as any).userData?.['metadata'] || {};
      const craneKind = (md['craneType'] || md['type'] || md['model'] || '').toString().toLowerCase();
      const looksSTS = craneKind.includes('sts') || craneKind.includes('quay') || craneKind.includes('ship') || craneKind.includes('tower');
      const looksYard = craneKind.includes('yard') || craneKind.includes('rtg') || craneKind.includes('rmg');
      const yardKey = md['yardId'] || md['yardCode'] || undefined; // only explicit yard refs
      const isYardCrane = !looksSTS && (looksYard || !!yardKey);
      if (isYardCrane && (yardKey || looksYard)) {
        const yard = yardById.get(String(yardKey)) || yardById.get(String((c as any).name)) || yards[0];
        if (yard) {
          const yBox = new THREE.Box3().setFromObject(yard);
          const yCenter = yBox.getCenter(new THREE.Vector3());
          // Place just outside the yard edge facing platform center
          const pCenter = platformBounds?.getCenter(new THREE.Vector3()) || new THREE.Vector3();
          const pos = c.position.clone();
          const outward = this.YARD_CRANE_YARD_OFFSET; // push further from yard edge
          if (Math.abs(pCenter.x - yCenter.x) >= Math.abs(pCenter.z - yCenter.z)) {
            // Move along X edge
            if (pCenter.x >= yCenter.x) pos.set(yBox.max.x + outward, 0, yCenter.z);
            else pos.set(yBox.min.x - outward, 0, yCenter.z);
          } else {
            // Move along Z edge
            if (pCenter.z >= yCenter.z) pos.set(yCenter.x, 0, yBox.max.z + outward);
            else pos.set(yCenter.x, 0, yBox.min.z - outward);
          }
          const cBox = new THREE.Box3().setFromObject(c);
          pos.y -= cBox.min.y;
          c.position.copy(pos);

          // Check for collisions and resolve
          const platform = this.portElements.find(o => (o as any).userData?.type === 'platform');
          const pBox = platform ? new THREE.Box3().setFromObject(platform) : null;
          let resolved = false;

          for (let attempt = 0; attempt < 30 && !resolved; attempt++) {
            const testBox = new THREE.Box3().setFromObject(c);
            let collision = null;

            for (const obs of this.portElements) {
              if (obs === c || obs === yard) continue;
              const obsType = (obs as any).userData?.type;
              if (obsType === 'water' || obsType === 'platform') continue;

              const obsBox = new THREE.Box3().setFromObject(obs);
              if (testBox.intersectsBox(obsBox)) {
                collision = obs;
                break;
              }
            }

            if (!collision) {
              resolved = true;
              break;
            }

            // Move away from collision with smaller steps
            const collBox = new THREE.Box3().setFromObject(collision);
            const collCenter = collBox.getCenter(new THREE.Vector3());
            const craneCenter = testBox.getCenter(new THREE.Vector3());
            const step = 4;

            const oldPos = c.position.clone();
            if (Math.abs(craneCenter.x - collCenter.x) > Math.abs(craneCenter.z - collCenter.z)) {
              c.position.x += (craneCenter.x > collCenter.x ? step : -step);
            } else {
              c.position.z += (craneCenter.z > collCenter.z ? step : -step);
            }

            // Check if still within platform
            if (pBox) {
              const newBox = new THREE.Box3().setFromObject(c);
              if (!pBox.containsBox(newBox)) {
                c.position.copy(oldPos);
                break;
              }
            }
          }

          return;
        }
      }

      // Default: dock crane (STS/tower) aligned to dock edge
      const code = md['dockCode'];
      const dockId = md['dockId'];
      const dockName = md['dockName'] || md['dock'];
      let dock = (code !== undefined) ? dockByCode.get(String(code)) : undefined;
      // Try infer dock from crane name like "STS-A1" -> DOCK-A1
      if (!dock) {
        const nameU = ((md['name'] as string) || (c as any).name || '').toString().toUpperCase();
        let inferred: string | undefined = undefined;
        let m = nameU.match(/STS[-\s]?([A-Z]+\d*)/);
        if (m && m[1]) inferred = `DOCK-${m[1]}`;
        if (!inferred) {
          m = nameU.match(/DOCK[-\s]?([A-Z]+\d*)/);
          if (m && m[1]) inferred = `DOCK-${m[1]}`;
        }
        if (inferred) {
          dock = dockByCode.get(inferred);
          if (dock) {
            (c as any).userData = { ...(c as any).userData, metadata: { ...md, dockCode: inferred } };
          }
        }
      }
      if (!dock && dockId !== undefined) dock = dockById.get(String(dockId));
      if (!dock && dockName !== undefined) dock = dockByName.get(String(dockName));
      if (!dock) {
        // Fallback: nearest dock to crane position
        let nearest: THREE.Object3D | undefined = undefined;
        let minD2 = Number.POSITIVE_INFINITY;
        const cPos = (c as any).getWorldPosition ? (c as any).getWorldPosition(new THREE.Vector3()) : c.position.clone();
        docks.forEach(dk => {
          const box = new THREE.Box3().setFromObject(dk);
          const ctr = box.getCenter(new THREE.Vector3());
          const d2 = ctr.distanceToSquared(cPos);
          if (d2 < minD2) { minD2 = d2; nearest = dk; }
        });
        dock = nearest;
        if (dock) {
          const dmd = (dock as any).userData?.['metadata'] || {};
          (c as any).userData = { ...(c as any).userData, metadata: { ...md, dockCode: dmd['code'] } };
        }
      }
      if (!dock) return;
      const dBox = new THREE.Box3().setFromObject(dock);
      const dCenter = dBox.getCenter(new THREE.Vector3());
      const inward = this.CRANE_DOCK_INSET;
      const pos = c.position.clone();
      const pierOffset = 80; // Pier extension length (updated)
      if (side === 'west') {
        pos.x = dBox.min.x + pierOffset + inward; // Before pier, on platform
        pos.z = shipAlongDock.get(String(code)) ?? dCenter.z;
        c.rotation.y = Math.PI / 2;
      } else if (side === 'east') {
        pos.x = dBox.max.x - pierOffset - inward; // Before pier, on platform
        pos.z = shipAlongDock.get(String(code)) ?? dCenter.z;
        c.rotation.y = -Math.PI / 2;
      } else if (side === 'north') {
        pos.z = dBox.min.z + pierOffset + inward; // Before pier, on platform
        pos.x = shipAlongDock.get(String(code)) ?? dCenter.x;
        c.rotation.y = 0;
      } else {
        pos.z = dBox.max.z - pierOffset - inward; // Before pier, on platform
        pos.x = shipAlongDock.get(String(code)) ?? dCenter.x;
        c.rotation.y = Math.PI;
      }
      const cBox = new THREE.Box3().setFromObject(c);
      pos.y -= cBox.min.y; // base at ground
      c.position.copy(pos);
    });

    // Distribute multiple cranes along the same dock to avoid overlap
    cranesByDock.forEach((cranesForDock, dockCode) => {
      if (cranesForDock.length <= 1) return; // No overlap if only one crane

      const dock = dockByCode.get(dockCode);
      if (!dock) return;

      const dBox = new THREE.Box3().setFromObject(dock);
      const dSize = dBox.getSize(new THREE.Vector3());
      const dCenter = dBox.getCenter(new THREE.Vector3());

      // Calculate spacing between cranes
      const craneSpacing = 50; // More space between cranes
      const craneSizes = cranesForDock.map(c => {
        const cBox = new THREE.Box3().setFromObject(c);
        return cBox.getSize(new THREE.Vector3());
      });

      // Distribute cranes along the dock axis
      const isAlongX = (side === 'west' || side === 'east');
      const dockLength = isAlongX ? dSize.z : dSize.x;
      const dockStart = isAlongX ? dBox.min.z : dBox.min.x;

      const totalWidth = craneSizes.reduce((sum, size) =>
        sum + (isAlongX ? size.z : size.x), 0);
      const totalSpacing = craneSpacing * (cranesForDock.length - 1);
      const availableSpace = dockLength - totalWidth - totalSpacing;

      let currentPos = dockStart + Math.max(0, availableSpace / 2);

      cranesForDock.forEach((crane, idx) => {
        const craneSize = craneSizes[idx];
        const halfSize = (isAlongX ? craneSize.z : craneSize.x) / 2;

        if (isAlongX) {
          crane.position.z = currentPos + halfSize;
        } else {
          crane.position.x = currentPos + halfSize;
        }

        currentPos += (isAlongX ? craneSize.z : craneSize.x) + craneSpacing;

        // Ensure crane stays on ground
        const cBox = new THREE.Box3().setFromObject(crane);
        crane.position.y -= cBox.min.y;
      });
    });

    // Distribute multiple yard cranes if they share the same yard
    const yardCranesByYard = new Map<string, THREE.Object3D[]>();
    yardCranes.forEach(c => {
      const md = (c as any).userData?.['metadata'] || {};
      const yardKey = md['yardId'] || md['yardCode'] || 'DEFAULT';
      if (!yardCranesByYard.has(yardKey)) yardCranesByYard.set(yardKey, []);
      yardCranesByYard.get(yardKey)!.push(c);
    });

    yardCranesByYard.forEach((cranesForYard, yardKey) => {
      if (cranesForYard.length <= 1) return;

      const yard = yardById.get(yardKey) || yards[0];
      if (!yard) return;

      const yBox = new THREE.Box3().setFromObject(yard);
      const ySize = yBox.getSize(new THREE.Vector3());
      const yCenter = yBox.getCenter(new THREE.Vector3());

      const craneSpacing = 45;
      const craneSizes = cranesForYard.map(c => {
        const cBox = new THREE.Box3().setFromObject(c);
        return cBox.getSize(new THREE.Vector3());
      });

      // Distribute along the yard edge
      const pCenter = platformBounds?.getCenter(new THREE.Vector3()) || new THREE.Vector3();
      const alongX = Math.abs(pCenter.x - yCenter.x) >= Math.abs(pCenter.z - yCenter.z);
      const yardLength = alongX ? ySize.z : ySize.x;
      const yardStart = alongX ? yBox.min.z : yBox.min.x;

      const totalWidth = craneSizes.reduce((sum, size) => sum + (alongX ? size.z : size.x), 0);
      const totalSpacing = craneSpacing * (cranesForYard.length - 1);
      const availableSpace = yardLength - totalWidth - totalSpacing;

      let currentPos = yardStart + Math.max(0, availableSpace / 2);

      cranesForYard.forEach((crane, idx) => {
        const craneSize = craneSizes[idx];
        const halfSize = (alongX ? craneSize.z : craneSize.x) / 2;

        if (alongX) {
          crane.position.z = currentPos + halfSize;
        } else {
          crane.position.x = currentPos + halfSize;
        }

        currentPos += (alongX ? craneSize.z : craneSize.x) + craneSpacing;

        // Ensure crane stays on ground
        const cBox = new THREE.Box3().setFromObject(crane);
        crane.position.y -= cBox.min.y;

        // Verify no collision with other elements
        this.ensureNoCollision(crane, [yard, ...cranesForYard.slice(0, idx)]);
      });
    });

    // Layout C: reposition trucks next to their associated dock (or first dock if none)
    if (this.currentLayoutId === 'C' && docks.length > 0) {
      const trucks = this.portElements.filter(o => (o as any).userData?.type === 'truck');
      trucks.forEach(t => {
        const md = (t as any).userData?.metadata || {};
        let dock: THREE.Object3D | undefined;
        if (md.dockCode) dock = dockByCode.get(String(md.dockCode));
        if (!dock) dock = docks[0];
        const dBox = new THREE.Box3().setFromObject(dock!);
        const tBox = new THREE.Box3().setFromObject(t);
        const tSize = tBox.getSize(new THREE.Vector3());
        const dCenter = dBox.getCenter(new THREE.Vector3());
        const offsetAlong = 0; // align with center along dock axis
        const insideOffset = Math.max(3, tSize.x * 0.3); // push slightly inland from edge so not hanging
        const pos = new THREE.Vector3();
        if (side === 'west') {
          // water at west: dock water-facing edge is dBox.max.x; place truck just inside dock (towards platform)
          pos.set(dBox.max.x + insideOffset, 0, dCenter.z + offsetAlong);
        } else if (side === 'east') {
          pos.set(dBox.min.x - insideOffset, 0, dCenter.z + offsetAlong);
        } else if (side === 'north') {
          pos.set(dCenter.x + offsetAlong, 0, dBox.max.z + insideOffset);
        } else { // south
          pos.set(dCenter.x + offsetAlong, 0, dBox.min.z - insideOffset);
        }
        t.position.copy(pos);
      });
    }

    // Layout C: pull warehouses and container yards closer to the dock edge but avoid overlaps
    if (this.currentLayoutId === 'C' && platformBounds) {
      const warehouses = this.portElements.filter(o => (o as any).userData?.['type'] === 'warehouse');
      const yards = this.portElements.filter(o => (o as any).userData?.['type'] === 'containeryard');
      const adjustGroup = [...warehouses, ...yards];

      const margin = 2; // minimal spacing between elements
      const EDGE_INSET = 18; // push elements further inland from the dock edge

      if (side === 'west' || side === 'east') {
        // First, snap X to the edge inset; then pack along Z to prevent overlap
        const snapped = adjustGroup.map(o => {
          const box = new THREE.Box3().setFromObject(o);
          const size = box.getSize(new THREE.Vector3());
          const halfZ = size.z / 2;
          const targetX = side === 'west'
            ? platformBounds.min.x + size.x / 2 + EDGE_INSET
            : platformBounds.max.x - size.x / 2 - EDGE_INSET;
          o.position.x = targetX;
          return { obj: o, halfZ, z: o.position.z };
        });
        // Sort by current Z and pack
        snapped.sort((a, b) => a.z - b.z);
        let prevCenterZ: number | null = null;
        let prevHalfZ = 0;
        for (const it of snapped) {
          let z = it.z;
          if (prevCenterZ !== null) {
            const minCenter = prevCenterZ + prevHalfZ + it.halfZ + margin;
            if (z < minCenter) z = minCenter;
          }
          // Clamp within platform bounds
          const minAllowed = platformBounds.min.z + it.halfZ + 2;
          const maxAllowed = platformBounds.max.z - it.halfZ - 2;
          it.obj.position.z = THREE.MathUtils.clamp(z, minAllowed, maxAllowed);
          prevCenterZ = it.obj.position.z;
          prevHalfZ = it.halfZ;
        }
      } else if (side === 'north' || side === 'south') {
        // Snap Z to edge inset; then pack along X to prevent overlap
        const snapped = adjustGroup.map(o => {
          const box = new THREE.Box3().setFromObject(o);
          const size = box.getSize(new THREE.Vector3());
          const halfX = size.x / 2;
          const targetZ = side === 'north'
            ? platformBounds.min.z + size.z / 2 + EDGE_INSET
            : platformBounds.max.z - size.z / 2 - EDGE_INSET;
          o.position.z = targetZ;
          return { obj: o, halfX, x: o.position.x };
        });
        snapped.sort((a, b) => a.x - b.x);
        let prevCenterX: number | null = null;
        let prevHalfX = 0;
        for (const it of snapped) {
          let x = it.x;
          if (prevCenterX !== null) {
            const minCenter = prevCenterX + prevHalfX + it.halfX + margin;
            if (x < minCenter) x = minCenter;
          }
          const minAllowed = platformBounds.min.x + it.halfX + 2;
          const maxAllowed = platformBounds.max.x - it.halfX - 2;
          it.obj.position.x = THREE.MathUtils.clamp(x, minAllowed, maxAllowed);
          prevCenterX = it.obj.position.x;
          prevHalfX = it.halfX;
        }
      }
    }
  }

  /**
   * Procedural water creation (backup)
   */
  private createProceduralWater(dimensions: Dimensions3D, color: string): THREE.Group {
    const waterGroup = new THREE.Group();
    const geometry = new THREE.PlaneGeometry(dimensions.length, dimensions.width, 100, 100);

    // Create realistic water material with waves effect
    const material = new THREE.MeshStandardMaterial({
      color: new THREE.Color(color),
      metalness: 0.95,
      roughness: 0.05,
      transparent: true,
      opacity: 0.85,
      side: THREE.DoubleSide
    });

    const mesh = new THREE.Mesh(geometry, material);
    mesh.rotation.x = -Math.PI / 2; // Make horizontal

    // Add slight wave animation through vertex manipulation
    const positionAttribute = geometry.getAttribute('position');
    if (positionAttribute) {
      for (let i = 0; i < positionAttribute.count; i++) {
        const x = positionAttribute.getX(i);
        const y = positionAttribute.getY(i);
        const wave = Math.sin(x * 0.05) * Math.cos(y * 0.05) * 0.5;
        positionAttribute.setZ(i, wave);
      }
      positionAttribute.needsUpdate = true;
      geometry.computeVertexNormals();
    }

    mesh.receiveShadow = false;
    mesh.castShadow = false;
    waterGroup.add(mesh);
    return waterGroup;
  }

  /**
   * Create port platform (concrete base structure)
   */
  private async createPlatform(dimensions: Dimensions3D, color: string): Promise<THREE.Group> {
    try {
      // Use the new ground model provided in assets
      const model = await this.loadModel('assets/models/grass_asphalt_ground.glb');
      const box = new THREE.Box3().setFromObject(model);
      const size = box.getSize(new THREE.Vector3());
      const center = box.getCenter(new THREE.Vector3());

      // Center to origin, then scale to cover specified area (X/Z). Keep Y thickness from model.
      model.position.sub(center);
      const scaleX = dimensions.length / size.x;
      const scaleZ = dimensions.width / size.z;
      model.scale.set(scaleX, 1, scaleZ);

      // Ensure ground sits at y=0
      const scaled = new THREE.Box3().setFromObject(model);
      const minY = scaled.min.y;
      model.position.y -= minY;

      console.log(`🛣️ Ground scaled to length ${dimensions.length}, width ${dimensions.width}`);
      this.applyShadowDefaults(model, { receive: true, cast: false });
      return model;
    } catch (error) {
      console.warn('Failed to load ground model, using procedural platform:', error);
      return this.createProceduralPlatform(dimensions, color);
    }
  }

  // Procedural platform fallback
  private createProceduralPlatform(dimensions: Dimensions3D, color: string): THREE.Group {
    const group = new THREE.Group();
    const geometry = new THREE.BoxGeometry(dimensions.length, dimensions.height, dimensions.width);
    const material = new THREE.MeshStandardMaterial({
      color: new THREE.Color(color),
      metalness: 0.2,
      roughness: 0.9
    });
    const mesh = new THREE.Mesh(geometry, material);
    const edges = new THREE.EdgesGeometry(geometry);
    const line = new THREE.LineSegments(edges, new THREE.LineBasicMaterial({ color: 0x666666 }));
    mesh.add(line);
    mesh.receiveShadow = true;
    mesh.castShadow = false;
    group.add(mesh);
    return group;
  }

  // Helper: apply default shadow flags to all meshes in a group
  private applyShadowDefaults(obj: THREE.Object3D, opts: { cast?: boolean; receive?: boolean }) {
    obj.traverse((child) => {
      if (child instanceof THREE.Mesh) {
        child.castShadow = !!opts.cast;
        child.receiveShadow = !!opts.receive;
      }
    });
  }

  // Align docks to the platform edge that faces the water
  private alignDocksToWaterEdge(): 'west'|'east'|'north'|'south'|undefined {
    const platform = this.portElements.find(o => (o as any).userData?.type === 'platform');
    const water = this.portElements.find(o => (o as any).userData?.type === 'water');
    if (!platform || !water) return undefined;

    const pBox = new THREE.Box3().setFromObject(platform);
    const wBox = new THREE.Box3().setFromObject(water);
    const wCenter = new THREE.Vector3();
    wBox.getCenter(wCenter);

    // Determine which platform side is closest to water center
    const sides = [
      { side: 'west',  dist: Math.abs(wCenter.x - pBox.min.x) },
      { side: 'east',  dist: Math.abs(wCenter.x - pBox.max.x) },
      { side: 'north', dist: Math.abs(wCenter.z - pBox.min.z) },
      { side: 'south', dist: Math.abs(wCenter.z - pBox.max.z) }
    ];
    sides.sort((a, b) => a.dist - b.dist);
    const nearest = sides[0].side as 'west'|'east'|'north'|'south';

    // Keep water position as-is and move platform to be at the water's edge (boundary)
    const wSize = wBox.getSize(new THREE.Vector3());
    const pSize = pBox.getSize(new THREE.Vector3());
    const oldPlatformPos = platform.position.clone();
    const newPlatformPos = platform.position.clone();

    // Position platform so its edge touches water edge with a gap, then push platform back to give ships more space
    const gap = 0.1; // small gap to prevent water overlapping asphalt
    const platformPushback = 5.0; // push platform further back from water to create space for ships
    if (nearest === 'west') {
      newPlatformPos.x = wBox.max.x + pSize.x / 2 + gap + platformPushback;
    } else if (nearest === 'east') {
      newPlatformPos.x = wBox.min.x - pSize.x / 2 - gap - platformPushback;
    } else if (nearest === 'north') {
      newPlatformPos.z = wBox.max.z + pSize.z / 2 + gap + platformPushback;
    } else {
      newPlatformPos.z = wBox.min.z - pSize.z / 2 - gap - platformPushback;
    }

    // Calculate how much the platform moved
    const platformDelta = new THREE.Vector3().subVectors(newPlatformPos, oldPlatformPos);
    platform.position.copy(newPlatformPos);

    // Move all elements on the platform (docks, warehouses, yards, cranes, buildings, etc.) by the same delta
    const elementsToMove = this.portElements.filter(o => {
      const t = (o as any).userData?.type;
      return t && t !== 'water' && t !== 'platform';
    });
    elementsToMove.forEach(elem => {
      elem.position.add(platformDelta);
    });

    // Recalculate platform bounds after moving it
    const newPBox = new THREE.Box3().setFromObject(platform);

    // Snap docks to platform edge facing water and add piers
    const docks = this.portElements.filter(o => (o as any).userData?.type === 'dock');
    docks.forEach(dock => {
      const box = new THREE.Box3().setFromObject(dock);
      const size = box.getSize(new THREE.Vector3());
      const pos = dock.position.clone();
      const epsilon = 0.0;
      if (nearest === 'west') {
        pos.x = newPBox.min.x + size.x / 2 + epsilon;
        pos.z = THREE.MathUtils.clamp(pos.z, newPBox.min.z + size.z / 2, newPBox.max.z - size.z / 2);
      } else if (nearest === 'east') {
        pos.x = newPBox.max.x - size.x / 2 - epsilon;
        pos.z = THREE.MathUtils.clamp(pos.z, newPBox.min.z + size.z / 2, newPBox.max.z - size.z / 2);
      } else if (nearest === 'north') {
        pos.z = newPBox.min.z + size.z / 2 + epsilon;
        pos.x = THREE.MathUtils.clamp(pos.x, newPBox.min.x + size.x / 2, newPBox.max.x - size.x / 2);
      } else {
        pos.z = newPBox.max.z - size.z / 2 - epsilon;
        pos.x = THREE.MathUtils.clamp(pos.x, newPBox.min.x + size.x / 2, newPBox.max.x - size.x / 2);
      }
      dock.position.copy(pos);

      // Add pier extending from dock edge into water
      const pier = this.createPierForDock(size, nearest);
      pier.userData = { type: 'pier', dockId: dock.name };

      // Position pier at dock edge
      if (nearest === 'west') {
        pier.position.set(pos.x - size.x / 2, 0, pos.z);
      } else if (nearest === 'east') {
        pier.position.set(pos.x + size.x / 2, 0, pos.z);
      } else if (nearest === 'north') {
        pier.position.set(pos.x, 0, pos.z - size.z / 2);
      } else {
        pier.position.set(pos.x, 0, pos.z + size.z / 2);
      }

      this.scene.add(pier);
      this.portElements.push(pier);
    });
    return nearest;
  }

  /**
   * Create ship using VesselService
   */
  private async createShip(dimensions: Dimensions3D, color: string, metadata?: any): Promise<THREE.Group> {
    const vessel = await this.vesselService.createVessel(
      { length: dimensions.length, width: dimensions.width, height: dimensions.height },
      color,
      metadata
    );
    vessel.userData['isVessel'] = true;
    return vessel;
  }

  /**
   * Create truck using PhysicalResourceService
   */
  private async createTruck(dimensions: Dimensions3D, color: string, metadata?: any): Promise<THREE.Group> {
    return this.physicalResourceService.createTruck(
      { width: dimensions.width, height: dimensions.height, length: dimensions.length },
      color,
      metadata
    );
  }

  /**
   * Spawn default truck near a warehouse towards docks (Layout C only)
   */
  private async addDefaultTruckNearWarehouses(side?: 'west'|'east'|'north'|'south'): Promise<void> {
    const warehouses = this.portElements.filter(o => (o as any).userData?.type === 'warehouse');
    if (warehouses.length === 0) return;
    let target = warehouses[0];
    const score = (w: THREE.Object3D) => {
      const box = new THREE.Box3().setFromObject(w); const c = box.getCenter(new THREE.Vector3());
      if (!side) return 0;
      if (side === 'west') return c.x; if (side === 'east') return -c.x; if (side === 'north') return c.z; return -c.z;
    };
    warehouses.forEach(w => { if (score(w) < score(target)) target = w; });
    const wBox = new THREE.Box3().setFromObject(target);
    const wCenter = wBox.getCenter(new THREE.Vector3());
    const dims: Dimensions3D = { length: 18, height: 6, width: 5 };
    const truck = await this.createTruck(dims, '#ffffff', { resourceType: 'truck', source: 'auto-layoutC' });
    const offset = 12;
    if (side === 'west') truck.position.set(wBox.min.x - offset, 0, wCenter.z);
    else if (side === 'east') truck.position.set(wBox.max.x + offset, 0, wCenter.z);
    else if (side === 'north') truck.position.set(wCenter.x, 0, wBox.min.z - offset);
    else if (side === 'south') truck.position.set(wCenter.x, 0, wBox.max.z + offset);
    else truck.position.copy(wCenter).add(new THREE.Vector3(0,0,15));
    this.scene.add(truck);
    this.portElements.push(truck);
    console.log('🚚 Default truck spawned (Layout C).');
  }

  /**
   * Create admin building mesh (smaller box with different texture)
   */
  private createAdminBuilding(dimensions: Dimensions3D, color: string): THREE.Mesh {
    const geometry = new THREE.BoxGeometry(dimensions.length, dimensions.height, dimensions.width);
    const material = new THREE.MeshStandardMaterial({
      color: new THREE.Color(color),
      metalness: 0.3,
      roughness: 0.6
    });
    const mesh = new THREE.Mesh(geometry, material);

    // Add edges
    const edges = new THREE.EdgesGeometry(geometry);
    const line = new THREE.LineSegments(edges, new THREE.LineBasicMaterial({ color: 0x000000 }));
    mesh.add(line);

    return mesh;
  }

  /**
   * Create a generic box for unknown element types
   */
  private createGenericBox(dimensions: Dimensions3D, color: string): THREE.Mesh {
    const geometry = new THREE.BoxGeometry(dimensions.length, dimensions.height, dimensions.width);
    const material = new THREE.MeshStandardMaterial({ color: new THREE.Color(color) });
    return new THREE.Mesh(geometry, material);
  }

  /**
   * US 4.2.4: Update visual status of vessels
   * @param statusMap Map of vessel IMO/Name to status info. If a vessel is NOT in this map, it will be hidden.
   */
  updateVesselStatuses(statusMap: Map<string, { status: string, color: string, tooltip: string }>): void {
    this.portElements.forEach(element => {
      if (element.userData['type'] === 'ship' || element.userData['type'] === 'vessel') {
        const md = element.userData['metadata'] || {};
        const imo = md.imoNumber || md.imo;
        const name = md.name || element.name;

        // Try to find status by IMO, then by Name
        const statusInfo = statusMap.get(imo) || statusMap.get(name);

        if (statusInfo) {
          // Show vessel
          element.visible = true;

          // Update metadata for tooltip
          element.userData['metadata'] = {
            ...md,
            status: statusInfo.status,
            statusTooltip: statusInfo.tooltip
          };

          // Add or update visual indicator (colored sphere above ship)
          this.addStatusIndicator(element, statusInfo.status, statusInfo.color);
        } else {
          // Hide vessel if not active in simulation
          element.visible = false;
          this.removeStatusIndicator(element);
        }
      }
    });
  }

  /**
   * Reset vessel visibility (show all) and remove status indicators
   * Called when simulation is stopped
   */
  resetVesselVisibility(): void {
    this.portElements.forEach(element => {
      if (element.userData['type'] === 'ship' || element.userData['type'] === 'vessel') {
        element.visible = true;
        this.removeStatusIndicator(element);

        // Clear status metadata
        const md = element.userData['metadata'] || {};
        if (md.status) delete md.status;
        if (md.statusTooltip) delete md.statusTooltip;
        element.userData['metadata'] = md;
      }
    });
  }

  private addStatusIndicator(element: THREE.Object3D, statusText: string, colorHex: string): void {
    // Check if indicator already exists
    let indicator = element.children.find(c => c.userData['isStatusIndicator']);

    // If indicator exists but text or color changed, remove it to recreate
    if (indicator) {
        const currentText = indicator.userData['statusText'];
        const currentColor = indicator.userData['statusColor'];

        if (currentText !== statusText || currentColor !== colorHex) {
            this.removeStatusIndicator(element);
            indicator = undefined;
        }
    }

    if (!indicator) {
      // Create canvas for text
      const canvas = document.createElement('canvas');
      const context = canvas.getContext('2d');
      if (!context) return;

      const fontSize = 48; // Reduced font size
      const fontFace = 'Arial';
      context.font = `bold ${fontSize}px ${fontFace}`;

      const metrics = context.measureText(statusText);
      const textWidth = metrics.width;
      const padding = 10; // Reduced padding

      canvas.width = textWidth + padding * 2;
      canvas.height = fontSize + padding * 2;

      // Draw background
      context.fillStyle = colorHex;
      context.fillRect(0, 0, canvas.width, canvas.height);

      // Draw border
      context.strokeStyle = 'white';
      context.lineWidth = 4;
      context.strokeRect(2, 2, canvas.width - 4, canvas.height - 4);

      // Draw text
      context.font = `bold ${fontSize}px ${fontFace}`;
      context.fillStyle = '#ffffff'; // White text
      context.textAlign = 'center';
      context.textBaseline = 'middle';
      // Add black outline for contrast
      context.strokeStyle = 'black';
      context.lineWidth = 3;
      context.strokeText(statusText, canvas.width / 2, canvas.height / 2);
      context.fillText(statusText, canvas.width / 2, canvas.height / 2);

      const texture = new THREE.CanvasTexture(canvas);
      const material = new THREE.SpriteMaterial({ map: texture, depthTest: false, depthWrite: false });

      indicator = new THREE.Sprite(material);

      // Calculate size
      // Temporarily reset rotation to get accurate local AABB
      const originalRot = element.rotation.clone();
      element.rotation.set(0, 0, 0);
      element.updateMatrixWorld();

      const box = new THREE.Box3().setFromObject(element);
      const size = box.getSize(new THREE.Vector3());

      // Restore rotation
      element.rotation.copy(originalRot);
      element.updateMatrixWorld();

      // Scale sprite to fit within ship width
      // 1. Determine ship width (smaller dimension)
      const shipWidth = Math.min(size.x, size.z);

      // 2. Calculate aspect ratio of the text canvas
      const aspectRatio = canvas.width / canvas.height;

      // 3. Set sprite width to be slightly smaller than ship width (e.g., 90%)
      // This ensures that if ships are side-by-side, the labels won't touch
      const targetWidth = Math.max(5, shipWidth * 0.9); // Minimum 5 units visibility
      const targetHeight = targetWidth / aspectRatio;

      indicator.scale.set(targetWidth, targetHeight, 1);

      // Position above ship - Centered and at fixed height
      indicator.position.set(0, size.y + 15, 0);

      indicator.userData['isStatusIndicator'] = true;
      indicator.userData['statusText'] = statusText;
      indicator.userData['statusColor'] = colorHex;

      // Ensure it renders on top
      indicator.renderOrder = 999;

      element.add(indicator);
    }
  }

  private removeStatusIndicator(element: THREE.Object3D): void {
    const indicator = element.children.find(c => c.userData['isStatusIndicator']);
    if (indicator) {
      element.remove(indicator);
      if (indicator instanceof THREE.Mesh) {
        indicator.geometry.dispose();
        (indicator.material as THREE.Material).dispose();
      }
    }
  }

  /**
   * Start vessel navigation animation
   */
  private startVesselNavigation(): void {
    this.vessels = [];

    this.portElements.forEach(element => {
      if (element.userData['isVessel'] === true) {
        const currentPos = element.position.clone();
        const startPos = currentPos.clone();

        const docks = this.portElements.filter(e => e.userData['type'] === 'dock');
        if (docks.length > 0) {
          const avgDockPos = new THREE.Vector3();
          docks.forEach(dock => {
            const dockBox = new THREE.Box3().setFromObject(dock);
            avgDockPos.add(dockBox.getCenter(new THREE.Vector3()));
          });
          avgDockPos.divideScalar(docks.length);

          const directionFromCenter = new THREE.Vector3().subVectors(currentPos, avgDockPos).normalize();
          startPos.add(directionFromCenter.multiplyScalar(800));
        } else {
          startPos.z -= 800;
        }

        this.vessels.push({
          mesh: element as THREE.Group,
          startPosition: startPos,
          endPosition: currentPos,
          progress: 0
        });

        element.position.copy(startPos);
      }
    });

    this.isNavigating = true;
    console.log(`🚢 Starting navigation for ${this.vessels.length} vessels`);
  }

  /**
   * Reset vessels to dock positions
   */
  private resetVesselPositions(): void {
    this.vessels.forEach(v => {
      v.mesh.position.copy(v.endPosition);
      v.progress = 1;
    });
    this.isNavigating = false;
    console.log('🚢 Vessels reset to docks');
  }

  /**
   * Clear all port elements from the scene
   */
  private clearPortElements(): void {
    console.log(`🧹 Clearing ${this.portElements.length} existing port elements`);

    this.portElements.forEach((element) => {
      this.scene.remove(element);

      // Recursively dispose of all geometries and materials
      element.traverse((child) => {
        if (child instanceof THREE.Mesh) {
          if (child.geometry) {
            child.geometry.dispose();
          }
          if (child.material) {
            if (Array.isArray(child.material)) {
              child.material.forEach(m => m.dispose());
            } else {
              child.material.dispose();
            }
          }
        }
      });
    });

    this.portElements = [];

    // Remove lighthouse lights from portLights array and scene
    this.portLights = this.portLights.filter(light => {
      if (light.userData && light.userData['isLighthouseLight']) {
        this.scene.remove(light);
        return false; // Remove from array
      }
      return true; // Keep in array
    });

    // Remove lighthouse spotlight
    if (this.lighthouseSpotlight) {
      this.scene.remove(this.lighthouseSpotlight);
      if (this.lighthouseSpotlight.target) {
        this.scene.remove(this.lighthouseSpotlight.target);
      }
      this.lighthouseSpotlight = null;
    }

    console.log('✅ Port elements cleared');
  }
}

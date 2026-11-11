import { Injectable } from '@angular/core';
import * as THREE from 'three';
import { GLTFLoader } from 'three/examples/jsm/loaders/GLTFLoader.js';

export interface VesselDimensions {
  length: number;
  width: number;
  height: number;
}

export interface VesselMetadata {
  imoNumber?: string;
  dockCode?: string;
  approved?: boolean;
  eta?: string;
  etd?: string;
  [key: string]: any;
}

@Injectable({
  providedIn: 'root'
})
export class VesselService {
  private gltfLoader = new GLTFLoader();
  private shipModelCache: THREE.Group | null = null;

  /**
   * Load ship 3D model from GLTF file
   */
  private async loadShipModel(modelPath: string = 'assets/models/container_ship.glb'): Promise<THREE.Group> {
    if (this.shipModelCache) {
      return this.shipModelCache.clone();
    }

    return new Promise((resolve, reject) => {
      this.gltfLoader.load(
        modelPath,
        (gltf) => {
          const model = gltf.scene;
          this.shipModelCache = model;
          resolve(model.clone());
        },
        undefined,
        (error) => {
          console.error('Error loading ship model:', error);
          reject(error);
        }
      );
    });
  }

  /**
   * Create a vessel using 3D model
   */
  async createVessel(
    dimensions: VesselDimensions,
    color: string,
    metadata?: VesselMetadata
  ): Promise<THREE.Group> {
    console.log('🚢 Creating ship:', metadata?.imoNumber || 'Unknown', 'dimensions:', dimensions, 'metadata:', metadata);

    try {
      const model = await this.loadShipModel();

      // Get original model size
      const box = new THREE.Box3().setFromObject(model);
      const size = box.getSize(new THREE.Vector3());
      const center = box.getCenter(new THREE.Vector3());

      console.log('🚢 Ship model original size:', 
        size.x.toFixed(2), '×', size.y.toFixed(2), '×', size.z.toFixed(2));

      // Center the model
      model.position.sub(center);

      // Scale uniformly based on length (x-axis)
      const scaleFactor = (dimensions.length / size.x) * 0.84375; // 1.125 * 0.75 = 84.375% of original
      model.scale.set(scaleFactor, scaleFactor, scaleFactor);

      console.log('🚢 Ship scaled uniformly to length:', dimensions.length * 0.84375);

      // Apply shadows
      this.applyShadows(model, true, true);

      // Add navigation lights
      this.addNavigationLights(model, dimensions);

      // Store metadata
      if (metadata) {
        model.userData = { ...metadata, elementType: 'ship' };
      }

      console.log('🚢 Ship created: SUCCESS');
      return model;
    } catch (error) {
      console.warn('Failed to load ship model, using procedural:', error);
      return this.createProceduralVessel(dimensions, color, metadata);
    }
  }

  /**
   * Create a simple procedural vessel (fallback)
   */
  private createProceduralVessel(
    dimensions: VesselDimensions,
    color: string,
    metadata?: VesselMetadata
  ): THREE.Group {
    const group = new THREE.Group();
    const { length, width, height } = dimensions;

    // Hull
    const hullGeometry = new THREE.BoxGeometry(length, height * 0.4, width);
    const hullMaterial = new THREE.MeshStandardMaterial({
      color: color,
      metalness: 0.5,
      roughness: 0.6
    });
    const hull = new THREE.Mesh(hullGeometry, hullMaterial);
    hull.position.y = height * 0.2;
    hull.castShadow = true;
    hull.receiveShadow = true;
    group.add(hull);

    // Superstructure
    const superGeometry = new THREE.BoxGeometry(length * 0.3, height * 0.6, width * 0.8);
    const superMaterial = new THREE.MeshStandardMaterial({
      color: 0xffffff,
      metalness: 0.3,
      roughness: 0.7
    });
    const superstructure = new THREE.Mesh(superGeometry, superMaterial);
    superstructure.position.set(-length * 0.3, height * 0.7, 0);
    superstructure.castShadow = true;
    group.add(superstructure);

    // Add navigation lights
    this.addNavigationLights(group, dimensions);

    // Store metadata
    if (metadata) {
      group.userData = { ...metadata, elementType: 'ship' };
    }

    return group;
  }

  /**
   * Apply shadows to all meshes in a vessel
   */
  private applyShadows(vessel: THREE.Group, castShadow: boolean, receiveShadow: boolean): void {
    vessel.traverse((child) => {
      if (child instanceof THREE.Mesh) {
        child.castShadow = castShadow;
        child.receiveShadow = receiveShadow;
      }
    });
  }

  /**
   * Add navigation lights to vessel (port, starboard, mast)
   */
  private addNavigationLights(vessel: THREE.Group, dimensions: VesselDimensions): void {
    const { length, width } = dimensions;
    
    // Port light (red - left side when facing bow)
    const portLight = new THREE.PointLight(0xff0000, 30, 50);
    portLight.position.set(length * 0.3, 8, -width * 0.5);
    portLight.visible = false; // Initially off
    portLight.userData = { isVesselLight: true };
    vessel.add(portLight);
    
    // Starboard light (green - right side when facing bow)
    const starboardLight = new THREE.PointLight(0x00ff00, 30, 50);
    starboardLight.position.set(length * 0.3, 8, width * 0.5);
    starboardLight.visible = false; // Initially off
    starboardLight.userData = { isVesselLight: true };
    vessel.add(starboardLight);
    
    // Mast lights (white - on superstructure)
    const mastLight1 = new THREE.PointLight(0xffffff, 40, 60);
    mastLight1.position.set(-length * 0.2, 15, 0);
    mastLight1.visible = false; // Initially off
    mastLight1.userData = { isVesselLight: true };
    vessel.add(mastLight1);
    
    const mastLight2 = new THREE.PointLight(0xffffff, 30, 50);
    mastLight2.position.set(length * 0.4, 12, 0);
    mastLight2.visible = false; // Initially off
    mastLight2.userData = { isVesselLight: true };
    vessel.add(mastLight2);
    
    console.log('💡 Added navigation lights to vessel');
  }
}

import { Injectable } from '@angular/core';
import * as THREE from 'three';
import { GLTFLoader } from 'three/examples/jsm/loaders/GLTFLoader.js';

export interface PhysicalResourceDimensions {
  width: number;
  height: number;
  length: number;
}

export interface PhysicalResourceMetadata {
  identifier?: string;
  code?: string;
  type?: string;
  assignedArea?: string;
  craneType?: string;
  model?: string;
  [key: string]: any;
}

@Injectable({
  providedIn: 'root'
})
export class PhysicalResourceService {
  private gltfLoader = new GLTFLoader();
  private modelCache: Map<string, THREE.Group> = new Map();

  /**
   * Load 3D model from file
   */
  private async loadModel(modelPath: string): Promise<THREE.Group> {
    if (this.modelCache.has(modelPath)) {
      return this.modelCache.get(modelPath)!.clone();
    }

    return new Promise((resolve, reject) => {
      this.gltfLoader.load(
        modelPath,
        (gltf) => {
          const model = gltf.scene;
          this.modelCache.set(modelPath, model);
          resolve(model.clone());
        },
        undefined,
        (error) => {
          console.error(`Error loading model ${modelPath}:`, error);
          reject(error);
        }
      );
    });
  }

  /**
   * Create a crane (STS or Yard crane)
   */
  async createCrane(
    dimensions: PhysicalResourceDimensions,
    color: string,
    metadata?: PhysicalResourceMetadata
  ): Promise<THREE.Group> {
    try {
      // Choose model based on metadata (STS vs Yard)
      const nameHint = (metadata?.['name'] || '').toString().toLowerCase();
      const craneKind = (metadata?.['craneType'] || metadata?.['type'] || metadata?.['model'] || nameHint || '').toString().toLowerCase();
      
      let modelPath = 'assets/models/crane.glb'; // default generic crane
      if (craneKind.includes('sts') || craneKind.includes('ship') || craneKind.includes('quay') || craneKind.includes('tower')) {
        // Ship-to-Shore quay crane
        modelPath = 'assets/models/tower_crane.glb';
      } else if (craneKind.includes('yard') || craneKind.includes('rtg') || craneKind.includes('rmg')) {
        modelPath = 'assets/models/gantry_crane.glb'; // yard crane model
      }

      const model = await this.loadModel(modelPath);
      
      // Get original model size and center
      const box = new THREE.Box3().setFromObject(model);
      const size = box.getSize(new THREE.Vector3());
      const center = box.getCenter(new THREE.Vector3());
      
      console.log(`🏗️ Crane model (${modelPath}) original size: ${size.x.toFixed(2)} × ${size.y.toFixed(2)} × ${size.z.toFixed(2)}`);
      
      // Center the model
      model.position.sub(center);
      
      // Scale model to match dimensions with extra multiplier for visibility
      const scaleMultiplier = 3.0; // Extra scale to make cranes much larger
      model.scale.set(
        (dimensions.length / size.x) * scaleMultiplier,
        (dimensions.height / size.y) * scaleMultiplier,
        (dimensions.width / size.z) * scaleMultiplier
      );
      
      console.log(`🏗️ Crane scaled to: ${dimensions.length * scaleMultiplier} × ${dimensions.height * scaleMultiplier} × ${dimensions.width * scaleMultiplier}`);
      
      this.applyShadows(model, true, true);
      
      // Store metadata
      if (metadata) {
        model.userData = { ...metadata, elementType: 'crane' };
      }
      
      return model;
    } catch (error) {
      console.warn('Failed to load crane model, using procedural:', error);
      return this.createProceduralCrane(dimensions, color, metadata);
    }
  }

  /**
   * Create a truck
   */
  async createTruck(
    dimensions: PhysicalResourceDimensions,
    color: string,
    metadata?: PhysicalResourceMetadata
  ): Promise<THREE.Group> {
    try {
      const model = await this.loadModel('assets/models/dav_container_truck__realistic_model.glb');
      
      // Compute original size
      const preBox = new THREE.Box3().setFromObject(model);
      const preSize = preBox.getSize(new THREE.Vector3());
      const preCenter = preBox.getCenter(new THREE.Vector3());
      console.log(`🚚 Truck model original size: ${preSize.x.toFixed(2)} × ${preSize.y.toFixed(2)} × ${preSize.z.toFixed(2)}`);

      // Reset orientation to avoid inherited rotations
      model.rotation.set(0, 0, 0);

      // Center model at origin for stable scaling/grounding
      model.position.sub(preCenter);

      // Uniformly scale by desired length to keep proportions (optionally amplify overall size)
      // Backend sends: width, height, length. We target length along Z.
      const desiredLength = dimensions.length;
      const uniformFactor = desiredLength / (preSize.z || 1);
      // Apply global x3 except for Layout C where we keep true-to-length
      const layoutId = (metadata?.['layoutId'] || '').toString().toUpperCase();
      const extraFactor = layoutId === 'C' ? 1 : 3;
      const scaleFactor = uniformFactor * extraFactor;
      model.scale.setScalar(scaleFactor);
      console.log(`🚚 Truck scaled uniformly by factor: ${scaleFactor.toFixed(3)} to length ${desiredLength}`);

      // Ground align: after scaling, shift so base sits on y=0
      const postBox = new THREE.Box3().setFromObject(model);
      const minY = postBox.min.y;
      if (isFinite(minY)) {
        model.position.y -= minY; // base touches ground
      }
      
      this.applyShadows(model, true, true);
      
      if (metadata) {
        model.userData = { ...metadata, elementType: 'truck' };
      }
      
      return model;
    } catch (error) {
      console.warn('Failed to load truck model, using procedural:', error);
      return this.createProceduralTruck(dimensions, color, metadata);
    }
  }

  /**
   * Create a forklift
   */
  async createForklift(
    dimensions: PhysicalResourceDimensions,
    color: string,
    metadata?: PhysicalResourceMetadata
  ): Promise<THREE.Group> {
    // For now, use procedural forklift (no model available)
    return this.createProceduralForklift(dimensions, color, metadata);
  }

  /**
   * Create procedural crane (fallback)
   */
  private createProceduralCrane(
    dimensions: PhysicalResourceDimensions,
    color: string,
    metadata?: PhysicalResourceMetadata
  ): THREE.Group {
    const group = new THREE.Group();
    const { width, height, length } = dimensions;

    // Base
    const baseGeometry = new THREE.BoxGeometry(width * 0.8, height * 0.1, length * 0.8);
    const material = new THREE.MeshStandardMaterial({ color, metalness: 0.8, roughness: 0.3 });
    const base = new THREE.Mesh(baseGeometry, material);
    base.position.y = height * 0.05;
    base.castShadow = true;
    base.receiveShadow = true;
    group.add(base);

    // Tower
    const towerGeometry = new THREE.BoxGeometry(width * 0.2, height * 0.8, length * 0.2);
    const tower = new THREE.Mesh(towerGeometry, material);
    tower.position.y = height * 0.5;
    tower.castShadow = true;
    group.add(tower);

    // Boom
    const boomGeometry = new THREE.BoxGeometry(width * 0.1, height * 0.1, length * 1.2);
    const boom = new THREE.Mesh(boomGeometry, material);
    boom.position.set(0, height * 0.9, length * 0.3);
    boom.castShadow = true;
    group.add(boom);

    if (metadata) {
      group.userData = { ...metadata, elementType: 'crane' };
    }

    return group;
  }

  /**
   * Create procedural truck (fallback)
   */
  private createProceduralTruck(
    dimensions: PhysicalResourceDimensions,
    color: string,
    metadata?: PhysicalResourceMetadata
  ): THREE.Group {
    const group = new THREE.Group();
    const { width, height, length } = dimensions;

    // Cabin
    const cabinGeometry = new THREE.BoxGeometry(width, height * 0.5, length * 0.3);
    const material = new THREE.MeshStandardMaterial({ color, metalness: 0.5, roughness: 0.6 });
    const cabin = new THREE.Mesh(cabinGeometry, material);
    cabin.position.set(0, height * 0.25, -length * 0.35);
    cabin.castShadow = true;
    cabin.receiveShadow = true;
    group.add(cabin);

    // Cargo bed
    const bedGeometry = new THREE.BoxGeometry(width, height * 0.4, length * 0.6);
    const bed = new THREE.Mesh(bedGeometry, material);
    bed.position.set(0, height * 0.2, length * 0.05);
    bed.castShadow = true;
    group.add(bed);

    if (metadata) {
      group.userData = { ...metadata, elementType: 'truck' };
    }

    return group;
  }

  /**
   * Create procedural forklift
   */
  private createProceduralForklift(
    dimensions: PhysicalResourceDimensions,
    color: string,
    metadata?: PhysicalResourceMetadata
  ): THREE.Group {
    const group = new THREE.Group();
    const { width, height, length } = dimensions;

    // Body
    const bodyGeometry = new THREE.BoxGeometry(width, height * 0.6, length);
    const material = new THREE.MeshStandardMaterial({ color, metalness: 0.5, roughness: 0.6 });
    const body = new THREE.Mesh(bodyGeometry, material);
    body.position.y = height * 0.3;
    body.castShadow = true;
    body.receiveShadow = true;
    group.add(body);

    // Mast
    const mastGeometry = new THREE.BoxGeometry(width * 0.2, height * 0.8, length * 0.1);
    const mast = new THREE.Mesh(mastGeometry, material);
    mast.position.set(0, height * 0.7, length * 0.4);
    mast.castShadow = true;
    group.add(mast);

    if (metadata) {
      group.userData = { ...metadata, elementType: 'forklift' };
    }

    return group;
  }

  /**
   * Apply shadows to all meshes
   */
  private applyShadows(resource: THREE.Group, castShadow: boolean, receiveShadow: boolean): void {
    resource.traverse((child) => {
      if (child instanceof THREE.Mesh) {
        child.castShadow = castShadow;
        child.receiveShadow = receiveShadow;
      }
    });
  }
}

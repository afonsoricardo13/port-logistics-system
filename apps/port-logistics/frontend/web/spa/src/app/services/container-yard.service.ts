import { Injectable } from '@angular/core';
import * as THREE from 'three';
import { GLTFLoader } from 'three/examples/jsm/loaders/GLTFLoader.js';

export interface ContainerYardDimensions {
  length: number;
  width: number;
}

export interface ContainerYardMetadata {
  identifier?: string;
  type?: string;
  maxCapacityTeu?: number;
  [key: string]: any;
}

@Injectable({
  providedIn: 'root'
})
export class ContainerYardService {
  private gltfLoader = new GLTFLoader();
  private yardModelCache: THREE.Group | null = null;

  /**
   * Load container yard 3D model
   */
  private async loadYardModel(modelPath: string = 'assets/models/low_poly_container_yard.glb'): Promise<THREE.Group> {
    if (this.yardModelCache) {
      return this.yardModelCache.clone();
    }

    return new Promise((resolve, reject) => {
      this.gltfLoader.load(
        modelPath,
        (gltf) => {
          const model = gltf.scene;
          this.yardModelCache = model;
          resolve(model.clone());
        },
        undefined,
        (error) => {
          console.error('Error loading container yard model:', error);
          reject(error);
        }
      );
    });
  }

  /**
   * Create a container yard using 3D model
   */
  async createContainerYard(
    dimensions: ContainerYardDimensions,
    color: string,
    metadata?: ContainerYardMetadata
  ): Promise<THREE.Group> {
    try {
      const model = await this.loadYardModel();

      // Get original model size
      const box = new THREE.Box3().setFromObject(model);
      const size = box.getSize(new THREE.Vector3());
      const center = box.getCenter(new THREE.Vector3());

      console.log('📦 Container Yard model original size:',
        size.x.toFixed(2), '×', size.y.toFixed(2), '×', size.z.toFixed(2));

      // Center the model
      model.position.sub(center);

      // Scale uniformly to fit within dimensions (use the smaller ratio)
      const scaleX = dimensions.length / size.x;
      const scaleZ = dimensions.width / size.z;
      const uniformScale = Math.min(scaleX, scaleZ);

      model.scale.set(uniformScale, uniformScale, uniformScale);

      console.log('📦 Container Yard scaled uniformly to fit:',
        dimensions.length, '×', dimensions.width);

      this.applyShadows(model, true, true);

      // Store metadata
      if (metadata) {
        model.userData = { ...metadata, elementType: 'containerYard' };
      }

      return model;
    } catch (error) {
      console.warn('Failed to load container yard model, using procedural:', error);
      return this.createProceduralContainerYard(dimensions, color, metadata);
    }
  }

  /**
   * Create procedural container yard (fallback)
   */
  private createProceduralContainerYard(
    dimensions: ContainerYardDimensions,
    color: string,
    metadata?: ContainerYardMetadata
  ): THREE.Group {
    const group = new THREE.Group();
    const { length, width } = dimensions;

    // Ground/pavement
    const groundGeometry = new THREE.BoxGeometry(length, 0.2, width);
    const groundMaterial = new THREE.MeshStandardMaterial({
      color: 0x505050,
      metalness: 0.1,
      roughness: 0.9
    });
    const ground = new THREE.Mesh(groundGeometry, groundMaterial);
    ground.position.y = 0.1;
    ground.receiveShadow = true;
    group.add(ground);

    // Add some container stacks for visual effect
    const containerGeometry = new THREE.BoxGeometry(2.4, 2.6, 6);
    const containerMaterials = [
      new THREE.MeshStandardMaterial({ color: 0xff0000 }),
      new THREE.MeshStandardMaterial({ color: 0x0000ff }),
      new THREE.MeshStandardMaterial({ color: 0x00ff00 }),
      new THREE.MeshStandardMaterial({ color: 0xffff00 })
    ];

    const rows = Math.floor(length / 10);
    const cols = Math.floor(width / 10);

    for (let i = 0; i < rows; i++) {
      for (let j = 0; j < cols; j++) {
        const material = containerMaterials[Math.floor(Math.random() * containerMaterials.length)];
        const container = new THREE.Mesh(containerGeometry, material);
        
        const xPos = (i / rows - 0.5) * length * 0.9;
        const zPos = (j / cols - 0.5) * width * 0.9;
        const height = Math.floor(Math.random() * 3) + 1; // 1-3 containers high
        
        container.position.set(xPos, 1.3 * height, zPos);
        container.castShadow = true;
        group.add(container);
      }
    }

    // Store metadata
    if (metadata) {
      group.userData = { ...metadata, elementType: 'containerYard' };
    }

    return group;
  }

  /**
   * Apply shadows to all meshes
   */
  private applyShadows(yard: THREE.Group, castShadow: boolean, receiveShadow: boolean): void {
    yard.traverse((child) => {
      if (child instanceof THREE.Mesh) {
        child.castShadow = castShadow;
        child.receiveShadow = receiveShadow;
      }
    });
  }
}

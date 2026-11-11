import { Injectable } from '@angular/core';
import * as THREE from 'three';
import { GLTFLoader } from 'three/examples/jsm/loaders/GLTFLoader.js';

export interface WarehouseDimensions {
  length: number;
  depth: number;
  height: number;
}

export interface WarehouseMetadata {
  identifier?: string;
  type?: string;
  maxCapacityTeu?: number;
  [key: string]: any;
}

@Injectable({
  providedIn: 'root'
})
export class WarehouseService {
  private gltfLoader = new GLTFLoader();
  private warehouseModelCache: THREE.Group | null = null;

  /**
   * Load warehouse 3D model
   */
  private async loadWarehouseModel(modelPath: string = 'assets/models/long_warehouse.glb'): Promise<THREE.Group> {
    if (this.warehouseModelCache) {
      return this.warehouseModelCache.clone();
    }

    return new Promise((resolve, reject) => {
      this.gltfLoader.load(
        modelPath,
        (gltf) => {
          const model = gltf.scene;
          this.warehouseModelCache = model;
          resolve(model.clone());
        },
        undefined,
        (error) => {
          console.error('Error loading warehouse model:', error);
          reject(error);
        }
      );
    });
  }

  /**
   * Create a warehouse using 3D model
   */
  async createWarehouse(
    dimensions: WarehouseDimensions,
    color: string,
    metadata?: WarehouseMetadata
  ): Promise<THREE.Group> {
    try {
      const model = await this.loadWarehouseModel();

      // Get original model size
      const box = new THREE.Box3().setFromObject(model);
      const size = box.getSize(new THREE.Vector3());
      const center = box.getCenter(new THREE.Vector3());

      console.log('📦 Warehouse model original size:',
        size.x.toFixed(2), '×', size.y.toFixed(2), '×', size.z.toFixed(2));

      // Center the model
      model.position.sub(center);

      // Scale to match dimensions
      model.scale.set(
        dimensions.length / size.x,
        dimensions.height / size.y,
        dimensions.depth / size.z
      );

      console.log('📦 Warehouse scaled to:',
        dimensions.length, '×', dimensions.height, '×', dimensions.depth);

      this.applyShadows(model, true, true);

      // Store metadata
      if (metadata) {
        model.userData = { ...metadata, elementType: 'warehouse' };
      }

      return model;
    } catch (error) {
      console.warn('Failed to load warehouse model, using procedural:', error);
      return this.createProceduralWarehouse(dimensions, color, metadata);
    }
  }

  /**
   * Create procedural warehouse (fallback)
   */
  private createProceduralWarehouse(
    dimensions: WarehouseDimensions,
    color: string,
    metadata?: WarehouseMetadata
  ): THREE.Group {
    const group = new THREE.Group();
    const { length, depth, height } = dimensions;

    // Main building
    const buildingGeometry = new THREE.BoxGeometry(length, height, depth);
    const buildingMaterial = new THREE.MeshStandardMaterial({
      color: color,
      metalness: 0.2,
      roughness: 0.8
    });
    const building = new THREE.Mesh(buildingGeometry, buildingMaterial);
    building.position.y = height / 2;
    building.castShadow = true;
    building.receiveShadow = true;
    group.add(building);

    // Roof
    const roofGeometry = new THREE.BoxGeometry(length * 1.05, height * 0.1, depth * 1.05);
    const roofMaterial = new THREE.MeshStandardMaterial({
      color: 0x8B4513,
      metalness: 0.1,
      roughness: 0.9
    });
    const roof = new THREE.Mesh(roofGeometry, roofMaterial);
    roof.position.y = height + height * 0.05;
    roof.castShadow = true;
    group.add(roof);

    // Store metadata
    if (metadata) {
      group.userData = { ...metadata, elementType: 'warehouse' };
    }

    return group;
  }

  /**
   * Apply shadows to all meshes
   */
  private applyShadows(warehouse: THREE.Group, castShadow: boolean, receiveShadow: boolean): void {
    warehouse.traverse((child) => {
      if (child instanceof THREE.Mesh) {
        child.castShadow = castShadow;
        child.receiveShadow = receiveShadow;
      }
    });
  }
}

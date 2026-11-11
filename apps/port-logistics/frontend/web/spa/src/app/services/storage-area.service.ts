import { Injectable } from '@angular/core';
import * as THREE from 'three';
import { WarehouseService } from './warehouse.service';
import { ContainerYardService } from './container-yard.service';

export interface StorageAreaDimensions {
  length: number;
  width: number;
  height?: number; // Optional, used only for warehouses
}

export interface StorageAreaMetadata {
  identifier?: string;
  type?: 'warehouse' | 'yard' | string; // warehouse or yard from DB
  maxCapacityTeu?: number;
  servedDocks?: string[];
  [key: string]: any;
}

@Injectable({
  providedIn: 'root'
})
export class StorageAreaService {

  constructor(
    private warehouseService: WarehouseService,
    private containerYardService: ContainerYardService
  ) {}

  /**
   * Create a storage area based on type (warehouse or yard)
   * Delegates to appropriate specialized service
   */
  async createStorageArea(
    dimensions: StorageAreaDimensions,
    color: string,
    metadata?: StorageAreaMetadata
  ): Promise<THREE.Group> {
    const type = metadata?.type?.toLowerCase();

    if (type === 'warehouse') {
      // Warehouse requires height dimension
      const height = dimensions.height || dimensions.width * 0.5; // fallback to reasonable height
      return this.warehouseService.createWarehouse(
        {
          length: dimensions.length,
          depth: dimensions.width,
          height: height
        },
        color,
        metadata
      );
    } else if (type === 'yard' || type === 'container yard' || type === 'containeryard') {
      // Container yard only needs length and width
      return this.containerYardService.createContainerYard(
        {
          length: dimensions.length,
          width: dimensions.width
        },
        color,
        metadata
      );
    } else {
      console.warn(`Unknown storage area type: ${type}, defaulting to warehouse`);
      const height = dimensions.height || dimensions.width * 0.5;
      return this.warehouseService.createWarehouse(
        {
          length: dimensions.length,
          depth: dimensions.width,
          height: height
        },
        color,
        metadata
      );
    }
  }

  /**
   * Create a warehouse (convenience method)
   */
  async createWarehouse(
    dimensions: { length: number; depth: number; height: number },
    color: string,
    metadata?: StorageAreaMetadata
  ): Promise<THREE.Group> {
    return this.warehouseService.createWarehouse(dimensions, color, {
      ...metadata,
      type: 'warehouse'
    });
  }

  /**
   * Create a container yard (convenience method)
   */
  async createContainerYard(
    dimensions: { length: number; width: number },
    color: string,
    metadata?: StorageAreaMetadata
  ): Promise<THREE.Group> {
    return this.containerYardService.createContainerYard(dimensions, color, {
      ...metadata,
      type: 'yard'
    });
  }
}

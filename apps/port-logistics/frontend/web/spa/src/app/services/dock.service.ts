import { Injectable } from '@angular/core';
import * as THREE from 'three';

export interface DockDimensions {
  length: number;
  width: number;
  height: number;
}

export interface DockMetadata {
  code?: string;
  name?: string;
  location?: string;
  [key: string]: any;
}

@Injectable({
  providedIn: 'root'
})
export class DockService {

  /**
   * Create a dock with procedural geometry
   */
  createDock(
    dimensions: DockDimensions,
    color: string,
    metadata?: DockMetadata
  ): THREE.Group {
    const group = new THREE.Group();
    
    const { length, width, height } = dimensions;

    // Main dock platform
    const platformGeometry = new THREE.BoxGeometry(length, height, width);
    const platformMaterial = new THREE.MeshStandardMaterial({
      color: color,
      metalness: 0.3,
      roughness: 0.7
    });
    const platform = new THREE.Mesh(platformGeometry, platformMaterial);
    platform.position.y = height / 2;
    platform.castShadow = true;
    platform.receiveShadow = true;
    group.add(platform);

    // Add bollards (mooring posts) along the dock
    const bollardCount = Math.max(2, Math.floor(length / 20));
    const bollardGeometry = new THREE.CylinderGeometry(0.5, 0.5, 2, 8);
    const bollardMaterial = new THREE.MeshStandardMaterial({
      color: 0x333333,
      metalness: 0.8,
      roughness: 0.2
    });

    for (let i = 0; i < bollardCount; i++) {
      const bollard = new THREE.Mesh(bollardGeometry, bollardMaterial);
      const xOffset = (i / (bollardCount - 1) - 0.5) * length * 0.9;
      bollard.position.set(xOffset, height + 1, width / 2 - 1);
      bollard.castShadow = true;
      group.add(bollard);
    }

    // Add edge markers (fenders)
    const fenderGeometry = new THREE.BoxGeometry(length * 0.95, 1, 2);
    const fenderMaterial = new THREE.MeshStandardMaterial({
      color: 0x222222,
      metalness: 0.5,
      roughness: 0.8
    });
    const fender = new THREE.Mesh(fenderGeometry, fenderMaterial);
    fender.position.set(0, height / 2, -width / 2 + 1);
    fender.castShadow = true;
    group.add(fender);

    // Store metadata
    if (metadata) {
      group.userData = { ...metadata, elementType: 'dock' };
    }

    return group;
  }

  /**
   * Apply shadows to all meshes in a dock
   */
  applyShadows(dock: THREE.Group, castShadow: boolean = true, receiveShadow: boolean = true): void {
    dock.traverse((child) => {
      if (child instanceof THREE.Mesh) {
        child.castShadow = castShadow;
        child.receiveShadow = receiveShadow;
      }
    });
  }
}

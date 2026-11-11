import { Component, OnInit, OnDestroy, ElementRef, ViewChild, AfterViewInit, HostListener } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ThreeService } from '../../services/three.service';
import { PortLayoutService } from '../../services/port-layout.service';
import { AuthService } from '../../services/auth.service';
import { VesselVisitNotificationService, VesselVisitNotificationDTO } from '../../services/vessel-visit-notification.service';
import { PhysicalResourceManagementService, PhysicalResourceDTO } from '../../services/physical-resource-management.service';
import { OperationPlanSearchService, OperationPlanDetail } from '../../services/operation-plan-search.service';

@Component({
  selector: 'app-viewer-3d',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './viewer-3d.html',
  styleUrl: './viewer-3d.scss'
})
export class Viewer3dComponent implements OnInit, AfterViewInit, OnDestroy {
  @ViewChild('canvasContainer', { static: false }) canvasContainer!: ElementRef<HTMLDivElement>;

  currentLayoutId = 'A';
  availableLayouts: string[] = [];
  isLoading = false;
  errorMessage: string | null = null;

  // Dynamic layout info
  layoutName = '';
  layoutDescription = '';
  dockCount = 0;
  warehouseCount = 0;
  yardCount = 0;

  // Element identifiers
  dockIds: string[] = [];
  warehouseIds: string[] = [];
  yardIds: string[] = [];

  // US 4.2.2: Selected object info
  selectedObjectType: string | null = null;
  selectedObjectName: string | null = null;
  selectedObjectMetadata: any = null;

  // US 4.2.3: Information overlay toggle
  showInfoOverlay = false;

  // US 4.2.6: Animation configuration
  animationDuration = 1000; // Default 1 second
  selectedEasing = 'Quadratic.InOut'; // Default easing

  // US 4.2.4: Simulation properties
  simulationDate: string = '';
  simulationTime: string = '';
  vesselVisits: VesselVisitNotificationDTO[] = [];
  cranes: PhysicalResourceDTO[] = [];
  operationPlans: OperationPlanDetail[] = [];
  isSimulationActive = false;

  // Ship arrival sound effect
  private shipArrivalSound: HTMLAudioElement | null = null;
  private previousVesselStatuses = new Map<string, string>();

  constructor(
    private threeService: ThreeService,
    private portLayoutService: PortLayoutService,
    private authService: AuthService,
    private vvnService: VesselVisitNotificationService,
    private resourceService: PhysicalResourceManagementService,
    private operationPlanService: OperationPlanSearchService
  ) {}

  /**
   * US 4.2.2: Helper to get object entries for template
   */
  objectEntries(obj: any): [string, any][] {
    return Object.entries(obj || {});
  }

  ngOnInit(): void {
    console.log('US 3.3.1 & 3.3.2 - 3D Viewer with Port Layout initialized');

    // Fetch available layouts
    this.portLayoutService.getAvailableLayouts().subscribe({
      next: (layouts) => {
        this.availableLayouts = layouts;
        console.log('Available layouts:', layouts);
      },
      error: (error) => {
        console.error('Failed to fetch available layouts:', error);
        this.errorMessage = 'Failed to load available layouts';
      }
    });

    // US 4.2.4: Fetch data for simulation
    this.fetchSimulationData();
  }

  /**
   * US 4.2.4: Fetch data for simulation
   */
  fetchSimulationData(): void {
    this.vvnService.getAll().subscribe({
      next: (vvns) => {
        this.vesselVisits = vvns;
        console.log('Loaded VVNs for simulation:', vvns.length);
      },
      error: (err) => console.error('Failed to load VVNs', err)
    });

    this.resourceService.search({ type: 'Crane' }).subscribe({
      next: (resources) => {
        this.cranes = resources;
        console.log('Loaded Cranes for simulation:', resources.length);
      },
      error: (err) => console.error('Failed to load Cranes', err)
    });

    this.operationPlanService.searchOperationPlans({}).subscribe({
      next: (plans) => {
        this.operationPlans = plans;
        console.log('Loaded Operation Plans for simulation:', plans.length);
      },
      error: (err) => console.error('Failed to load Operation Plans', err)
    });
  }

  /**
   * US 4.2.4: Update simulation based on selected time
   */
  updateSimulation(): void {
    // If date or time is missing, reset simulation (show all ships, no indicators)
    if (!this.simulationDate || !this.simulationTime) {
      this.isSimulationActive = false;
      this.threeService.resetVesselVisibility();
      return;
    }

    this.isSimulationActive = true;
    const simDateTime = new Date(`${this.simulationDate}T${this.simulationTime}`);

    // Auto Night Mode Logic (19:00 - 07:00 exclusive of 07:00)
    const hour = simDateTime.getHours();
    const shouldBeNight = hour >= 19 || hour < 7;
    const isCurrentlyNight = this.threeService.isNight();

    if (shouldBeNight !== isCurrentlyNight) {
        this.threeService.toggleNightMode();
    }

    const statusMap = new Map<string, { status: string, color: string, tooltip: string }>();

    console.log(`🕒 Simulation updated to: ${simDateTime.toLocaleString()}`);
    console.log(`📊 Total VVNs loaded: ${this.vesselVisits.length}`);

    this.vesselVisits.forEach(vvn => {
        console.log(`🔍 Checking VVN ${vvn.vesselImoNumber} - Status: ${vvn.status}, ETA: ${vvn.estimatedTimeOfArrival ? new Date(vvn.estimatedTimeOfArrival).toLocaleString() : 'N/A'}, Dock: ${vvn.assignedDockCode}`);

        if (!vvn.estimatedTimeOfArrival) {
            console.log(`⚠️ VVN ${vvn.vesselImoNumber} SKIPPED: No ETA`);
            return;
        }

        // Only consider VVNs with status 2 (Scheduled/Approved) as per business rules
        // Assuming '2' is the status code for scheduled/approved visits that have operation plans
        // Also checking for 'Approved' string as per documentation
        const s = String(vvn.status).trim();
        if (s !== '2' && s.toLowerCase() !== 'approved') {
             console.log(`⚠️ VVN ${vvn.vesselImoNumber} SKIPPED: Status not approved (${s})`);
             return;
        }

        // FIX: Filter out VVNs that are not relevant for the current simulation time
        // This prevents duplicate IMOs from old/future visits overwriting the current status
        const vvnEtaRaw = new Date(vvn.estimatedTimeOfArrival);
        // Use a generous window (e.g., +/- 7 days) to ensure we catch the right visit
        // The strict visibility check later will handle the exact display logic
        const windowStart = new Date(vvnEtaRaw.getTime() - 7 * 24 * 60 * 60 * 1000);
        const windowEnd = vvn.estimatedTimeOfDeparture
            ? new Date(new Date(vvn.estimatedTimeOfDeparture).getTime() + 7 * 24 * 60 * 60 * 1000)
            : new Date(vvnEtaRaw.getTime() + 14 * 24 * 60 * 60 * 1000);

        if (simDateTime < windowStart || simDateTime > windowEnd) {
            console.log(`⚠️ VVN ${vvn.vesselImoNumber} SKIPPED: Outside time window (${windowStart.toLocaleString()} - ${windowEnd.toLocaleString()})`);
            return;
        }

        // Try to find an operation plan for this VVN
        const plan = this.operationPlans.find(p => p.vvnId === vvn.id?.toString() || p.vesselImo === vvn.vesselImoNumber);

        // Only include vessels that have an operation plan
        if (!plan) {
            console.log(`⚠️ VVN ${vvn.vesselImoNumber} SKIPPED: No operation plan found`);
            return;
        }

        console.log(`📋 Processing VVN ${vvn.vesselImoNumber} - ETA: ${new Date(vvn.estimatedTimeOfArrival).toLocaleString()}, Dock: ${vvn.assignedDockCode}`);

        let eta = new Date(vvn.estimatedTimeOfArrival);
        let effectiveStart = eta;
        let unloadEnd: Date;
        let loadEnd: Date;
        let unloadCount = (vvn as any).totalUnloadingContainers || (vvn.unloadingManifest?.length) || 100;
        let loadCount = (vvn as any).loadingContainerCount || (vvn.loadingManifest?.length) || 100;

        if (plan && plan.startTime && plan.endTime) {
            // Use precise times from Operation Plan
            const planStart = new Date(plan.startTime);

            // If plan starts before ETA, we assume operations are delayed until arrival (ETA)
            // If plan starts after ETA, we respect the plan start (waiting at dock)
            effectiveStart = planStart > eta ? planStart : eta;

            // Use explicit loading/unloading times from plan if available (in minutes)
            if (plan.unloadingTime > 0 || plan.loadingTime > 0) {
                unloadEnd = new Date(effectiveStart.getTime() + plan.unloadingTime * 60000);
                loadEnd = new Date(unloadEnd.getTime() + plan.loadingTime * 60000);
            } else {
                // Fallback: Use the scheduled window duration and split 50/50
                const planEnd = new Date(plan.endTime);
                const opsDurationMs = planEnd.getTime() - planStart.getTime();
                const unloadDurationMs = opsDurationMs * 0.5;

                unloadEnd = new Date(effectiveStart.getTime() + unloadDurationMs);
                loadEnd = new Date(effectiveStart.getTime() + opsDurationMs);
            }
        } else {
            // Fallback to calculation if no plan
            const crane = this.cranes.find(c => c.assignedArea === vvn.assignedDockCode) || this.cranes[0];
            const capacityPerHour = crane?.capacityPerHour || 20;
            const setupTime = crane?.setupTimeMinutes || 30;

            const unloadDurationMin = (unloadCount / capacityPerHour) * 60 + setupTime;
            const loadDurationMin = (loadCount / capacityPerHour) * 60;

            unloadEnd = new Date(effectiveStart.getTime() + unloadDurationMin * 60000);
            loadEnd = new Date(unloadEnd.getTime() + loadDurationMin * 60000);
        }

        // Strict visibility check:
        // 1. Must be at or after ETA (arrived or arriving)
        // 2. Must be before or at ETD (Departure)
        // Note: loadEnd is when operations finish, but ship might stay until ETD
        let etd = vvn.estimatedTimeOfDeparture ? new Date(vvn.estimatedTimeOfDeparture) : loadEnd;

        // If ETD is missing or earlier than loadEnd (impossible), use loadEnd
        if (etd < loadEnd) etd = loadEnd;

        // Allow ship to be visible from ETA until ETD (inclusive)
        if (simDateTime.getTime() < eta.getTime() || simDateTime.getTime() > etd.getTime()) {
            console.log(`❌ VVN ${vvn.vesselImoNumber} NOT visible - SimTime: ${simDateTime.toLocaleString()}, ETA: ${eta.toLocaleString()}, ETD: ${etd.toLocaleString()}`);
            return;
        }

        console.log(`✅ VVN ${vvn.vesselImoNumber} IS visible - SimTime: ${simDateTime.toLocaleString()}, ETA: ${eta.toLocaleString()}, ETD: ${etd.toLocaleString()}`);

        let status = 'Waiting';
        let color = '#FFFF00'; // Yellow
        let tooltip = `Waiting for operations (ETA: ${eta.toLocaleTimeString()})`;

        // Define a time window for "Arriving" status
        // Ship is "Arriving" only within a small window around the exact ETA time
        const arrivalWindowMinutes = 3; // 3 minutes before and after ETA
        const arrivalWindowMs = arrivalWindowMinutes * 60 * 1000;

        // Check if current time is within the arrival window (±3 minutes from ETA)
        const timeDiffFromEta = simDateTime.getTime() - eta.getTime();
        const isArrivingNow = timeDiffFromEta >= -arrivalWindowMs && timeDiffFromEta <= arrivalWindowMs;

        // Use strict inequalities for time windows to avoid overlap issues
        if (isArrivingNow) {
             // Within arrival window -> Arriving (only shown near the exact ETA time)
            status = 'Arriving';
            color = '#800080'; // Purple
            tooltip = `Arriving at ${eta.toLocaleTimeString()}`;
        } else if (simDateTime.getTime() > eta.getTime() && simDateTime.getTime() < effectiveStart.getTime()) {
            // Arrived (past ETA) but waiting for operations to start (effectiveStart)
            status = 'Waiting';
            color = '#FFFF00'; // Yellow
            tooltip = `Waiting at dock (Ops start: ${effectiveStart.toLocaleTimeString()})`;
        } else if (simDateTime.getTime() >= effectiveStart.getTime() && simDateTime.getTime() < unloadEnd.getTime()) {
            status = 'Unloading';
            color = '#FFA500'; // Orange
            const totalDuration = unloadEnd.getTime() - effectiveStart.getTime();
            const progress = totalDuration > 0 ? Math.round(((simDateTime.getTime() - effectiveStart.getTime()) / totalDuration) * 100) : 0;
            tooltip = `Unloading: ${progress}% complete (${unloadCount} containers)`;
        } else if (simDateTime.getTime() >= unloadEnd.getTime() && simDateTime.getTime() < loadEnd.getTime()) {
            status = 'Loading';
            color = '#0000FF'; // Blue
            const totalDuration = loadEnd.getTime() - unloadEnd.getTime();
            const progress = totalDuration > 0 ? Math.round(((simDateTime.getTime() - unloadEnd.getTime()) / totalDuration) * 100) : 0;
            tooltip = `Loading: ${progress}% complete (${loadCount} containers)`;
        } else if (simDateTime.getTime() >= loadEnd.getTime() && simDateTime.getTime() < etd.getTime()) {
            // Operations finished, waiting for departure (ETD)
            status = 'Waiting';
            color = '#FFFF00'; // Yellow
            tooltip = `Operations completed. Waiting for departure (ETD: ${etd.toLocaleTimeString()})`;
        } else if (simDateTime.getTime() >= etd.getTime()) {
            status = 'Departing';
            color = '#008000'; // Green
            tooltip = `Departing at ${etd.toLocaleTimeString()}`;
        }

        console.log(`🎯 VVN ${vvn.vesselImoNumber} Status: ${status} (Color: ${color})`);

        statusMap.set(vvn.vesselImoNumber, { status, color, tooltip });

        // Check if this vessel just changed to Arriving state and trigger animation/sound
        // Only trigger if:
        // 1. Current status is "Arriving" (within ±3 minutes of ETA)
        // 2. Previous status was NOT "Arriving" (state change detection)
        const previousStatus = this.previousVesselStatuses.get(vvn.vesselImoNumber);
        if (status === 'Arriving' && previousStatus !== 'Arriving') {
            // Vessel just started arriving - trigger animation and sound
            console.log(`🚢 Vessel ${vvn.vesselImoNumber} is now arriving at ${eta.toLocaleString()}!`);
            this.playShipArrivalSound();

            // Start navigation animation automatically for this specific vessel
            if (!this.threeService.areVesselsNavigating()) {
                this.threeService.startVesselNavigationIfNeeded();
            }
        }

        // Update previous status tracking
        this.previousVesselStatuses.set(vvn.vesselImoNumber, status);
    });

    this.threeService.updateVesselStatuses(statusMap);
  }

  /**
   * Play ship arrival sound effect
   */
  private playShipArrivalSound(): void {
    if (this.shipArrivalSound) {
      // Reset audio to beginning if already playing
      this.shipArrivalSound.currentTime = 0;
      this.shipArrivalSound.play().catch(err => {
        console.warn('Could not play ship arrival sound:', err);
      });
    }
  }

  ngAfterViewInit(): void {
    // Initialize Three.js scene after view is ready
    if (this.canvasContainer) {
      this.threeService.initScene(this.canvasContainer.nativeElement);
      this.threeService.animate();

      // Fine-tune placement so ships sit nearer the sea and water hugs asphalt
      this.threeService.setPlacementOffsets({
        // push water further under asphalt and ships closer to the dock
        waterOverlap: 2.5,  // água totalmente colada ao asfalto
        shipGap: 0.005,
        craneInset: 0.35
      });

      // US 3.3.6: Setup mouse controls for camera
      this.setupCameraControls();

      // Handle window resize
      window.addEventListener('resize', this.onWindowResize.bind(this));

      // Load default port layout
      this.loadPortLayout(this.currentLayoutId);

      // Initialize ship arrival sound
      this.shipArrivalSound = new Audio('assets/audio/ship-horn-sfx-3.mp3');
      this.shipArrivalSound.volume = 0.5; // Set volume to 50%
    }
  }

  ngOnDestroy(): void {
    // Clean up Three.js resources
    this.threeService.dispose();
    window.removeEventListener('resize', this.onWindowResize.bind(this));

    // US 3.3.6: Clean up mouse event listeners
    this.cleanupCameraControls();
  }

  /**
   * US 3.3.6: Setup mouse controls for camera manipulation
   */
  private setupCameraControls(): void {
    const canvas = this.threeService.getRendererElement();
    if (!canvas) return;

    // Bind mouse events for camera control
    canvas.addEventListener('mousedown', this.onMouseDown.bind(this));
    canvas.addEventListener('mousemove', this.onMouseMove.bind(this));
    canvas.addEventListener('mouseup', this.onMouseUp.bind(this));
    canvas.addEventListener('wheel', this.onMouseWheel.bind(this), { passive: false });

    // US 4.2.2: Bind click event for object picking
    canvas.addEventListener('click', this.onCanvasClick.bind(this));

    // Bind keyboard events for WASD camera movement
    window.addEventListener('keydown', this.onKeyDown.bind(this));

    // Prevent context menu on right-click
    canvas.addEventListener('contextmenu', (e) => e.preventDefault());

    console.log('📷 Camera controls enabled: Right-click to orbit, Mouse wheel to zoom, WASD to move, Left-click to select');
  }

  /**
   * US 3.3.6: Clean up mouse event listeners
   */
  private cleanupCameraControls(): void {
    const canvas = this.threeService.getRendererElement();
    if (!canvas) return;

    canvas.removeEventListener('mousedown', this.onMouseDown.bind(this));
    canvas.removeEventListener('mousemove', this.onMouseMove.bind(this));
    canvas.removeEventListener('mouseup', this.onMouseUp.bind(this));
    canvas.removeEventListener('wheel', this.onMouseWheel.bind(this));
    canvas.removeEventListener('click', this.onCanvasClick.bind(this)); // US 4.2.2
    canvas.removeEventListener('contextmenu', (e) => e.preventDefault());

    // Clean up keyboard event listener
    window.removeEventListener('keydown', this.onKeyDown.bind(this));
  }

  /**
   * US 3.3.6: Handle mouse down event
   */
  private onMouseDown(event: MouseEvent): void {
    this.threeService.onMouseDown(event);
  }

  /**
   * US 3.3.6: Handle mouse move event
   */
  private onMouseMove(event: MouseEvent): void {
    this.threeService.onMouseMove(event);
  }

  /**
   * US 3.3.6: Handle mouse up event
   */
  private onMouseUp(event: MouseEvent): void {
    this.threeService.onMouseUp(event);
  }

  /**
   * US 4.2.2: Handle canvas click for object picking
   */
  private onCanvasClick(event: MouseEvent): void {
    // Only pick on left click (button 0)
    if (event.button !== 0) return;

    const result = this.threeService.pickObject(event);

    if (result) {
      this.selectedObjectType = result.type;
      this.selectedObjectName = result.name;
      this.selectedObjectMetadata = result.object.userData['metadata'] || {};

      console.log(`✅ Selected ${result.type}: ${result.name}`, this.selectedObjectMetadata);
    } else {
      // Clear selection
      this.clearUISelection();
    }
  }

  /**
   * Clear UI selection info
   */
  private clearUISelection(): void {
    this.selectedObjectType = null;
    this.selectedObjectName = null;
    this.selectedObjectMetadata = null;
    this.showInfoOverlay = false; // US 4.2.3: Hide overlay when clearing selection

    // Also clear selection inside ThreeService so spotlight and inspection mode are turned off
    this.threeService.clearSelection();

    console.log('❌ Selection cleared');
  }

  /**
   * US 4.2.3: Get displayable data based on user role
   * Returns filtered metadata according to role permissions
   */
  getDisplayableData(): any {
    if (!this.selectedObjectMetadata) return {};

    const userRole = this.authService.getUserRole();
    const generalData: any = {};
    const restrictedData: any = {};

    // Separate data into general and restricted
    Object.keys(this.selectedObjectMetadata).forEach(key => {
      const lowerKey = key.toLowerCase();

      // General data (always visible) - skip 'type', 'name', 'identifier' as they're redundant
      if (['description', 'code', 'imonumber', 'vesseltype', 'operator', 'length', 'depth', 'width', 'height', 'assignedarea'].includes(lowerKey)) {
        generalData[key] = this.selectedObjectMetadata[key];
      }
      // Restricted data (only for Port Authority Officer and Logistics Operator)
      else if (['status', 'eta', 'etd', 'operations', 'capacity', 'availability', 'approved'].includes(lowerKey)) {
        restrictedData[key] = this.selectedObjectMetadata[key];
      }
      // Default: treat as general (but skip redundant fields)
      else if (!['type', 'name', 'identifier', 'dockcode', 'cranetype', 'model'].includes(lowerKey)) {
        generalData[key] = this.selectedObjectMetadata[key];
      }
    });

    // Check if user has access to restricted data
    const hasRestrictedAccess =
      userRole === 'PortAuthorityOfficer' ||
      userRole === 'LogisticsOperator' ||
      userRole === 'Administrator';

    // Return combined data based on role
    if (hasRestrictedAccess) {
      return { ...generalData, ...restrictedData };
    } else {
      return generalData;
    }
  }

  /**
   * US 4.2.3: Check if user has access to restricted information
   */
  hasRestrictedAccess(): boolean {
    const userRole = this.authService.getUserRole();
    return userRole === 'PortAuthorityOfficer' ||
           userRole === 'LogisticsOperator' ||
           userRole === 'Administrator';
  }

  /**
   * US 4.2.7: Reset camera to default position
   */
  resetCamera(): void {
    // Clear UI selection first
    this.clearUISelection();

    // Then reset camera (which also calls clearSelection in service)
    this.threeService.resetCamera();
  }

  /**
   * Toggle between day and night mode
   */
  toggleNightMode(): void {
    this.threeService.toggleNightMode();
  }

  /**
   * Check if night mode is active
   */
  isNightMode(): boolean {
    return this.threeService.isNight();
  }

  /**
   * Toggle vessel navigation animation
   */
  toggleVesselNavigation(): void {
    this.threeService.toggleVesselNavigation();
  }

  /**
   * Check if vessels are navigating
   */
  areVesselsNavigating(): boolean {
    return this.threeService.areVesselsNavigating();
  }

  /**
   * US 3.3.6: Handle mouse wheel event
   */
  private onMouseWheel(event: WheelEvent): void {
    this.threeService.onMouseWheel(event);
  }

  /**
   * US 4.2.3: Handle keyboard shortcuts with HostListener
   */
  @HostListener('document:keydown', ['$event'])
  handleKeyboardEvent(event: KeyboardEvent): void {
    // US 4.2.3: Toggle info overlay with 'i' key
    if (event.key.toLowerCase() === 'i' && this.selectedObjectType) {
      this.showInfoOverlay = !this.showInfoOverlay;
      console.log(`✅ US 4.2.3: Info overlay toggled to ${this.showInfoOverlay}`);
      return;
    }

    // Call the original onKeyDown method for other keys
    this.onKeyDown(event);
  }

  /**
   * Handle keyboard input for WASD camera movement
   */
  private onKeyDown(event: KeyboardEvent): void {
    // US 4.2.7: Handle R key to reset camera in UI
    if (event.key.toLowerCase() === 'r') {
      this.clearUISelection();
    }

    // US 4.2.2: Handle ESC to clear selection in UI
    if (event.key === 'Escape' && this.selectedObjectType) {
      this.clearUISelection();
    }

    this.threeService.onKeyDown(event);
  }

  private onWindowResize(): void {
    if (this.canvasContainer) {
      const width = this.canvasContainer.nativeElement.clientWidth;
      const height = this.canvasContainer.nativeElement.clientHeight;
      this.threeService.onResize(width, height);
    }
  }

  /**
   * Load and render a specific port layout
   * US 3.3.2 - Load port layout from backend API
   * @param layoutId Layout identifier (A, B, etc.)
   */
  loadPortLayout(layoutId: string): void {
    this.isLoading = true;
    this.errorMessage = null;

    console.log(`Loading port layout: ${layoutId}`);

    this.portLayoutService.getLayoutById(layoutId).subscribe({
      next: async (layout) => {
        console.log('Port layout received:', layout);
        await this.threeService.loadPortLayout(layout);
        this.currentLayoutId = layoutId;

        // Update dynamic info from layout
        this.layoutName = layout.layoutName || '';
        this.layoutDescription = layout.description || '';

        const docks = layout.elements.filter(e => e.type.toLowerCase() === 'dock');
        const warehouses = layout.elements.filter(e => e.type.toLowerCase() === 'warehouse');
        const yards = layout.elements.filter(e => e.type.toLowerCase() === 'containeryard');

        this.dockCount = docks.length;
        this.warehouseCount = warehouses.length;
        this.yardCount = yards.length;

        // Extract identifiers from metadata
        this.dockIds = docks.map(d => d.metadata?.['code'] || d.metadata?.['name'] || d.id).filter(id => id);
        this.warehouseIds = warehouses.map(w => w.metadata?.['identifier'] || w.id).filter(id => id);
        this.yardIds = yards.map(y => y.metadata?.['identifier'] || y.id).filter(id => id);

        this.isLoading = false;
      },
      error: (error) => {
        console.error('Failed to load port layout:', error);
        this.errorMessage = `Failed to load port layout ${layoutId}`;
        this.isLoading = false;
      }
    });
  }

  /**
   * Switch between different port layouts
   * @param layoutId New layout ID
   */
  switchLayout(layoutId: string): void {
    // Always load the layout (ngModel already updated currentLayoutId)
    this.loadPortLayout(layoutId);
  }

  /**
   * US 4.2.6: Update animation duration in the ThreeService
   */
  updateAnimationDuration(): void {
    this.threeService.setAnimationDuration(this.animationDuration);
  }

  /**
   * US 4.2.6: Update animation easing function in the ThreeService
   */
  updateAnimationEasing(): void {
    const easingFunctions = this.threeService.getAvailableEasingFunctions();

    // Parse the selected easing string (e.g., "Quadratic.InOut")
    const parts = this.selectedEasing.split('.');
    if (parts.length === 2) {
      const category = parts[0];
      const type = parts[1];

      // Access the easing function safely with explicit type casting
      const categoryGroup = (easingFunctions as any)[category];
      if (categoryGroup && categoryGroup[type]) {
        this.threeService.setAnimationEasing(categoryGroup[type]);
        console.log(`✅ US 4.2.6: Easing function set to ${this.selectedEasing}`);
      }
    }
  }
}



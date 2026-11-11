import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { SchedulingService, DailyScheduleResponse, ScheduleOperation } from '../../services/scheduling.service';
import { OperationPlanningService } from '../../services/operation-planning.service';
import { DockRebalanceService, DockRebalanceProposal, VesselDockReassignment } from '../../services/dock-rebalance.service';
import { forkJoin } from 'rxjs';

/**
 * US 3.4.2 / US 3.4.4 - Daily Vessel Scheduling Component
 * 
 * Interface for logistics operators to generate daily schedules
 * for vessel loading/unloading operations
 * 
 * Features:
 * - Date selection for target scheduling day
 * - Algorithm selection: optimal, heuristic, genetic, multi_crane (US 3.4.5, US 4.3.1)
 * - Generate schedule with progress feedback
 * - Compare any two algorithms side-by-side (US 4.3.1 AC)
 * - Display results in summary table
 * - Timeline visualization (if feasible)
 * - Warnings about resource availability
 * - Dock rebalancing required before scheduling (US 4.3.3)
 */

@Component({
  selector: 'app-daily-scheduling',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './daily-scheduling.html',
  styleUrls: ['./daily-scheduling.scss']
})
export class DailySchedulingComponent implements OnInit {
  // US 3.4.2 AC-UI.a: Date selection
  selectedDate: string = '';
  minDate: string = '';
  
  // US 3.4.4: Algorithm selection - US 4.3.2: Auto is default
  selectedAlgorithm: 'optimal' | 'heuristic' | 'multi_crane' | 'genetic' | 'auto' = 'auto';
  
  // US 4.3.1 - Genetic Algorithm parameters
  gaPopulationSize: number = 20;
  gaGenerations: number = 50;
  gaCrossoverRate: number = 70;
  gaMutationRate: number = 20;
  gaTimeLimit: number = 5000;
  gaUseMultiCrane: boolean = false;
  
  // State management
  isLoading: boolean = false;
  hasResult: boolean = false;
  isComparing: boolean = false;
  
  // Schedule results
  scheduleResult: DailyScheduleResponse | null = null;
  optimalResult: DailyScheduleResponse | null = null;
  heuristicResult: DailyScheduleResponse | null = null;
  errorMessage: string = '';
  
  // US 4.3.1 AC: Algorithm comparison - any two algorithms
  compareAlgorithm1: 'optimal' | 'heuristic' | 'genetic' = 'optimal';
  compareAlgorithm2: 'optimal' | 'heuristic' | 'genetic' = 'heuristic';
  comparisonResult1: any = null;
  comparisonResult2: any = null;
  showCompareModal: boolean = false;
  
  // Service status
  serviceAvailable: boolean = true;
  saveMessages: string[] = [];
  
  // US 4.1.2 - Save operation plans
  isSaving: boolean = false;
  saveSuccess: boolean = false;
  saveError: string = '';

  // US 4.3.3 - Dock Rebalancing
  showRebalanceModal: boolean = false;
  isLoadingRebalance: boolean = false;
  isApplyingRebalance: boolean = false;
  rebalanceProposal: DockRebalanceProposal | null = null;
  rebalanceError: string = '';
  pendingReassignments: VesselDockReassignment[] = [];
  
  // AC5: Track if rebalancing was executed (required before running algorithms)
  rebalanceExecutedForDate: string | null = null;
  showRebalanceRequiredWarning: boolean = false;

  constructor(
    private schedulingService: SchedulingService, 
    private opPlanning: OperationPlanningService,
    private rebalanceService: DockRebalanceService
  ) {}

  ngOnInit(): void {
    // Set default date to today
    const today = new Date();
    this.selectedDate = this.formatDateForInput(today);
    this.minDate = this.selectedDate;
    
    // Check service health
    this.checkServiceHealth();
  }

  /**
   * US 3.4.2 AC-UI: Check if scheduling service is available
   */
  checkServiceHealth(): void {
    this.schedulingService.checkHealth().subscribe({
      next: (isHealthy) => {
        this.serviceAvailable = isHealthy;
        if (!isHealthy) {
          this.errorMessage = 'Scheduling service is not available. Please ensure Prolog server is running on port 5003.';
        }
      },
      error: () => {
        this.serviceAvailable = false;
        this.errorMessage = 'Cannot connect to scheduling service. Please start the Prolog server.';
      }
    });
  }

  /**
   * US 3.4.2 / US 3.4.4 AC-UI.a: Generate schedule for selected date
   */
  generateSchedule(): void {
    if (!this.selectedDate) {
      this.errorMessage = 'Please select a target date';
      return;
    }

    // AC5: Require rebalancing before running scheduling algorithms
    if (this.rebalanceExecutedForDate !== this.selectedDate) {
      this.showRebalanceRequiredWarning = true;
      this.errorMessage = 'Dock rebalancing must be analyzed before generating schedules. Please click "Suggest Rebalancing" first.';
      return;
    }
    this.showRebalanceRequiredWarning = false;

    // Validate genetic algorithm parameters
    if (this.selectedAlgorithm === 'genetic') {
      if (this.gaPopulationSize < 5 || this.gaPopulationSize > 100) {
        this.errorMessage = 'Population Size must be between 5 and 100';
        return;
      }
      if (this.gaGenerations < 10 || this.gaGenerations > 500) {
        this.errorMessage = 'Max Generations must be between 10 and 500';
        return;
      }
      if (this.gaCrossoverRate < 0 || this.gaCrossoverRate > 100) {
        this.errorMessage = 'Crossover Rate must be between 0 and 100';
        return;
      }
      if (this.gaMutationRate < 0 || this.gaMutationRate > 100) {
        this.errorMessage = 'Mutation Rate must be between 0 and 100';
        return;
      }
      if (this.gaTimeLimit < 1000 || this.gaTimeLimit > 60000) {
        this.errorMessage = 'Time Limit must be between 1000 and 60000 ms';
        return;
      }
    }

    // Reset state
    this.errorMessage = '';
    this.hasResult = false;
    this.isLoading = true;
    this.isComparing = false;
    this.optimalResult = null;
    this.heuristicResult = null;
    this.comparisonResult1 = null;
    this.comparisonResult2 = null;

    // Prepare genetic algorithm parameters if using genetic algorithm
    const geneticParams = this.selectedAlgorithm === 'genetic' ? {
      populationSize: this.gaPopulationSize,
      generations: this.gaGenerations,
      crossoverRate: this.gaCrossoverRate,
      mutationRate: this.gaMutationRate,
      timeLimit: this.gaTimeLimit,
      useMultiCrane: this.gaUseMultiCrane
    } : undefined;

    // Call scheduling service with selected algorithm
    this.schedulingService.generateDailySchedule(this.selectedDate, this.selectedAlgorithm, geneticParams).subscribe({
      next: (response) => {
        this.isLoading = false;
        
        if (response.success) {
          this.scheduleResult = response;
          this.hasResult = true;
          
          // US 3.4.2 AC-UI.c: Check for warnings
          if (response.warnings && response.warnings.length > 0) {
            this.errorMessage = `Warning: ${response.warnings.join(', ')}`;
          }
        } else {
          // US 3.4.2 AC-UI.c: Feedback on infeasibility
          this.errorMessage = response.message || response.error || 'Failed to generate schedule';
        }
      },
      error: (errorResponse) => {
        this.isLoading = false;
        this.errorMessage = errorResponse.error || 'An unexpected error occurred';
      }
    });
  }

  /**
   * US 3.4.2 AC-UI.b: Format time for display
   */
  formatTime(time: number | string): string {
    return this.schedulingService.formatHourToTime(time);
  }

  /**
   * US 3.4.2 AC-UI.b: Calculate duration
   */
  getDuration(operation: ScheduleOperation): string {
    return this.schedulingService.calculateDuration(operation);
  }

  /**
   * US 3.4.2 AC-UI.c: Check resource warnings
   */
  getResourceWarnings(operation: ScheduleOperation): string[] {
    return this.schedulingService.checkResourceAvailability(operation);
  }

  /**
   * Check if operation has resource issues
   */
  hasResourceIssues(operation: ScheduleOperation): boolean {
    return this.getResourceWarnings(operation).length > 0;
  }

  /**
   * Reset form and results
   */
  reset(): void {
    this.hasResult = false;
    this.scheduleResult = null;
    this.optimalResult = null;
    this.heuristicResult = null;
    this.comparisonResult1 = null;
    this.comparisonResult2 = null;
    this.isComparing = false;
    this.errorMessage = '';
    this.saveMessages = [];
    this.showRebalanceRequiredWarning = false;
    // Note: Don't reset rebalanceExecutedForDate here to preserve state
    const today = new Date();
    this.selectedDate = this.formatDateForInput(today);
  }

  /**
   * US 4.1.2: Save generated schedule as Operation Plans in OEM
   */
  saveAsOperationPlans(): void {
    if (!this.hasResult || !this.scheduleResult) {
      this.saveError = 'No schedule available to save';
      return;
    }

    this.isSaving = true;
    this.saveSuccess = false;
    this.saveError = '';
    this.saveMessages = [];

    // Convert date to ISO 8601 format
    const dateObj = new Date(this.selectedDate);
    const isoDate = dateObj.toISOString();
    const algorithm = this.scheduleResult.algorithm || this.selectedAlgorithm;

    this.opPlanning.generateAndSaveOperationPlans(isoDate, algorithm).subscribe({
      next: (response) => {
        this.isSaving = false;
        if (response.success) {
          this.saveSuccess = true;
          this.saveMessages = [
            `✓ Successfully saved ${response.plansGenerated} operation plan(s)`,
            `Algorithm: ${algorithm}`,
            `Date: ${this.selectedDate}`
          ];
          
          // Add plan details
          if (response.plans && response.plans.length > 0) {
            response.plans.forEach(plan => {
              this.saveMessages.push(`  • VVN ${plan.vvnId}`);
            });
          }
          
          // Auto-hide success message after 5 seconds
          setTimeout(() => {
            this.saveSuccess = false;
            this.saveMessages = [];
          }, 5000);
        } else {
          this.saveError = response.message || 'Failed to save operation plans';
          if (response.error) {
            this.saveMessages = [response.error];
          }
        }
      },
      error: (err) => {
        this.isSaving = false;
        this.saveError = 'Failed to save operation plans to database';
        this.saveMessages = [
          err.error?.message || err.message || 'Unknown error occurred',
          'Please ensure the OEM API is running on port 7001'
        ];
        
        // Auto-hide error message after 5 seconds
        setTimeout(() => {
          this.saveError = '';
          this.saveMessages = [];
        }, 5000);
      }
    });
  }

  /**
   * US 4.3.1 AC: Open compare algorithms modal
   */
  openCompareModal(): void {
    this.showCompareModal = true;
  }

  /**
   * US 4.3.1 AC: Close compare algorithms modal
   */
  closeCompareModal(): void {
    this.showCompareModal = false;
  }

  /**
   * US 4.3.1 AC: Compare any two algorithms side-by-side
   */
  compareAlgorithms(): void {
    if (!this.selectedDate) {
      this.errorMessage = 'Please select a target date';
      return;
    }

    // AC5: Require rebalancing before comparing algorithms
    if (this.rebalanceExecutedForDate !== this.selectedDate) {
      this.showRebalanceRequiredWarning = true;
      this.errorMessage = 'Dock rebalancing must be analyzed before comparing algorithms. Please click "Suggest Rebalancing" first.';
      return;
    }
    this.showRebalanceRequiredWarning = false;

    if (this.compareAlgorithm1 === this.compareAlgorithm2) {
      this.errorMessage = 'Please select two different algorithms to compare';
      return;
    }

    // Reset state
    this.errorMessage = '';
    this.hasResult = false;
    this.isLoading = true;
    this.isComparing = true;
    this.scheduleResult = null;
    this.showCompareModal = false;
    this.comparisonResult1 = null;
    this.comparisonResult2 = null;

    // Prepare genetic params for comparison if needed
    const geneticParams = {
      populationSize: this.gaPopulationSize,
      generations: this.gaGenerations,
      crossoverRate: this.gaCrossoverRate,
      mutationRate: this.gaMutationRate,
      timeLimit: this.gaTimeLimit,
      useMultiCrane: this.gaUseMultiCrane
    };

    // Call both algorithms in parallel
    forkJoin({
      result1: this.schedulingService.generateDailySchedule(
        this.selectedDate, 
        this.compareAlgorithm1,
        this.compareAlgorithm1 === 'genetic' ? geneticParams : undefined
      ),
      result2: this.schedulingService.generateDailySchedule(
        this.selectedDate, 
        this.compareAlgorithm2,
        this.compareAlgorithm2 === 'genetic' ? geneticParams : undefined
      )
    }).subscribe({
      next: (results) => {
        this.isLoading = false;
        this.isComparing = false;
        // Extract metrics for comparison display
        this.comparisonResult1 = this.extractComparisonMetrics(results.result1);
        this.comparisonResult2 = this.extractComparisonMetrics(results.result2);
        // Keep backwards compatibility
        if (this.compareAlgorithm1 === 'optimal') {
          this.optimalResult = results.result1;
        } else if (this.compareAlgorithm1 === 'heuristic') {
          this.heuristicResult = results.result1;
        }
        if (this.compareAlgorithm2 === 'optimal') {
          this.optimalResult = results.result2;
        } else if (this.compareAlgorithm2 === 'heuristic') {
          this.heuristicResult = results.result2;
        }
        this.hasResult = true;
      },
      error: (errorResponse) => {
        this.isLoading = false;
        this.isComparing = false;
        this.errorMessage = errorResponse.error || 'An error occurred during comparison';
      }
    });
  }

  /**
   * Extract metrics from schedule response for comparison
   */
  private extractComparisonMetrics(response: DailyScheduleResponse): any {
    if (!response.success) {
      return { error: response.message || response.error };
    }

    const schedule = response.schedule || [];
    let vesselsWithDelay = 0;
    schedule.forEach((op: any) => {
      if (op.delay && parseFloat(op.delay) > 0) {
        vesselsWithDelay++;
      }
    });

    return {
      totalDelay: response.total_delay || 0,
      totalVessels: schedule.length,
      vesselsWithDelay: vesselsWithDelay,
      computationTime: response.computation_time_ms || 0,
      maxCranes: Math.max(...(response.cranes_allocation || [1]), 1)
    };
  }

  /**
   * Get algorithm display name
   */
  getAlgorithmDisplayName(algorithm: string): string {
    const names: { [key: string]: string } = {
      'optimal': 'Optimal',
      'heuristic': 'Heuristic',
      'genetic': 'Genetic Algorithm',
      'multi_crane': 'Multi-Crane',
      'auto': 'Auto'
    };
    return names[algorithm] || algorithm;
  }

  /**
   * US 3.4.4: Calculate efficiency percentage
   * Efficiency = how close heuristic is to optimal (100% = same delay)
   */
  getEfficiency(heuristicDelay: number, optimalDelay: number): number {
    // If both have 0 delay, perfect efficiency
    if (optimalDelay === 0 && heuristicDelay === 0) return 100;
    // If optimal is 0 but heuristic has delay, 0% efficiency
    if (optimalDelay === 0) return 0;
    // If heuristic is better than optimal (shouldn't happen), cap at 100%
    if (heuristicDelay <= optimalDelay) return 100;
    // Calculate how much worse heuristic is: efficiency = optimal / heuristic * 100
    return Math.max(0, (optimalDelay / heuristicDelay) * 100);
  }

  /**
   * US 3.4.4: Get performance improvement factor
   */
  getSpeedup(optimalTime: number, heuristicTime: number): number {
    if (heuristicTime === 0) return 0;
    return optimalTime / heuristicTime;
  }

  /**
   * Format Date object to YYYY-MM-DD
   */
  private formatDateForInput(date: Date): string {
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
  }

  /**
   * US 3.4.2 AC-UI.b: Timeline visualization helpers
   */
  getTimelinePosition(time: number | string): number {
    // Convert to decimal hours if string
    const hours = typeof time === 'number' ? time : this.parseTimeToHours(time);
    // Position as percentage of 24-hour day
    return (hours / 24) * 100;
  }

  getTimelineWidth(operation: ScheduleOperation): number {
    // Get duration in hours as decimal
    const start = operation.start_time_decimal ?? (typeof operation.start_time === 'number' ? operation.start_time : this.parseTimeToHours(operation.start_time));
    const end = operation.end_time_decimal ?? (typeof operation.end_time === 'number' ? operation.end_time : this.parseTimeToHours(operation.end_time));
    const duration = end - start;
    return (duration / 24) * 100;
  }
  
  private parseTimeToHours(timeStr: string): number {
    const [hours, minutes] = timeStr.split(':').map(Number);
    return hours + (minutes / 60);
  }

  /**
   * Get color for timeline based on resource availability
   */
  getTimelineColor(operation: ScheduleOperation): string {
    if (this.hasResourceIssues(operation)) {
      return '#ef4444'; // Red for issues
    }
    return '#3b82f6'; // Blue for normal
  }

  /**
   * trackBy para vessel_id, evitando NG0955
   */
  trackByVesselId(index: number, operation: ScheduleOperation): any {
    return operation.vessel_id;
  }

  /**
   * Organize operations into rows to avoid overlap
   */
  getTimelineRows(): ScheduleOperation[][] {
    if (!this.scheduleResult?.schedule) return [];
    
    const operations = [...this.scheduleResult.schedule];
    const rows: ScheduleOperation[][] = [];
    
    operations.forEach(op => {
      const start = op.start_time_decimal ?? this.parseTimeToHours(op.start_time.toString());
      const end = op.end_time_decimal ?? this.parseTimeToHours(op.end_time.toString());
      
      // Find a row where this operation doesn't overlap
      let placed = false;
      for (const row of rows) {
        const hasOverlap = row.some(existing => {
          const existingStart = existing.start_time_decimal ?? this.parseTimeToHours(existing.start_time.toString());
          const existingEnd = existing.end_time_decimal ?? this.parseTimeToHours(existing.end_time.toString());
          return start < existingEnd && end > existingStart;
        });
        
        if (!hasOverlap) {
          row.push(op);
          placed = true;
          break;
        }
      }
      
      // If no suitable row found, create new row
      if (!placed) {
        rows.push([op]);
      }
    });
    
    return rows;
  }

  /**
   * US 3.4.5 - Get crane count for an operation
   * Returns the number of cranes allocated to this operation from cranes_allocation array
   */
  getCraneCount(index: number): number {
    if (!this.scheduleResult?.cranes_allocation || index >= this.scheduleResult.cranes_allocation.length) {
      return 1; // Default to 1 crane if no allocation info
    }
    return this.scheduleResult.cranes_allocation[index] || 1;
  }

  /**
   * US 3.4.5 - Get list of vessels using multiple cranes with crane IDs
   */
  getMultiCraneVessels(): string {
    if (!this.scheduleResult?.schedule || !this.scheduleResult?.cranes_allocation) {
      return 'None';
    }
    
    const multiCraneInfo = this.scheduleResult.schedule
      .map((op: any, i) => {
        if (this.getCraneCount(i) > 1) {
          const cranes = op.assigned_cranes && op.assigned_cranes.length > 1 
            ? op.assigned_cranes.join(', ') 
            : op.assigned_crane;
          return `${op.vessel_id} (${cranes})`;
        }
        return null;
      })
      .filter(v => v !== null);
    
    return multiCraneInfo.length > 0 ? multiCraneInfo.join(' | ') : 'None';
  }

  /**
   * US 4.3.2 - Format selection reason for display
   */
  formatSelectionReason(reason: string | undefined): string {
    if (!reason) return 'Unknown';
    
    const reasonMap: { [key: string]: string } = {
      'small_instance': 'Small problem size (≤5 vessels) - optimal search is feasible',
      'medium_instance': 'Medium problem size (6-15 vessels) - heuristic for balanced performance',
      'large_instance': 'Large problem size (>15 vessels) - genetic algorithm for exploration',
      'time_constrained': 'Tight time constraint - heuristic for fast response',
      'quality_preferred': 'Quality preference enabled - optimal despite medium size',
      'high_complexity': 'High complexity (many docks) - genetic algorithm recommended',
      'large_time_constrained': 'Large problem with time constraint - heuristic for speed',
      'default_fallback': 'Default selection - heuristic as safe choice'
    };
    
    return reasonMap[reason] || reason.replace(/_/g, ' ');
  }

  // ===============================
  // US 4.3.3 - Dock Rebalancing
  // ===============================

  /**
   * Open the dock rebalancing modal and fetch proposal
   */
  openRebalanceModal(): void {
    if (!this.selectedDate) return;

    this.showRebalanceModal = true;
    this.isLoadingRebalance = true;
    this.rebalanceError = '';
    this.rebalanceProposal = null;
    this.pendingReassignments = [];

    // Get current user ID (in real app, from auth service)
    const officerId = 'current-user'; // TODO: Replace with actual user ID from auth

    this.rebalanceService.getProposal(this.selectedDate, officerId).subscribe({
      next: (proposal) => {
        this.isLoadingRebalance = false;
        this.rebalanceProposal = proposal;
        
        // AC5: Mark rebalancing as executed for this date (allows running algorithms)
        this.rebalanceExecutedForDate = this.selectedDate;
        this.showRebalanceRequiredWarning = false;
        this.errorMessage = '';
        
        // Calculate pending reassignments (what will change)
        if (proposal.currentAllocations && proposal.proposedAllocations) {
          this.pendingReassignments = this.rebalanceService.calculateReassignments(
            proposal.currentAllocations,
            proposal.proposedAllocations
          );
        }
      },
      error: (err) => {
        this.isLoadingRebalance = false;
        console.error('Dock rebalance error:', err);
        
        if (err.status === 0) {
          this.rebalanceError = 'Cannot connect to backend. Please ensure the API is running.';
        } else if (err.status === 400) {
          this.rebalanceError = err.error?.message || err.error || 'Invalid request parameters';
        } else if (err.status === 500) {
          this.rebalanceError = err.error?.details || err.error?.error || 'Server error occurred';
        } else {
          this.rebalanceError = err.error?.message || err.message || 'Failed to fetch rebalancing proposal';
        }
      }
    });
  }

  /**
   * Close the rebalancing modal
   */
  closeRebalanceModal(): void {
    this.showRebalanceModal = false;
    this.rebalanceProposal = null;
    this.rebalanceError = '';
    this.pendingReassignments = [];
  }

  /**
   * Apply the proposed dock reassignments
   */
  applyRebalancing(): void {
    if (!this.rebalanceProposal || this.pendingReassignments.length === 0) return;

    this.isApplyingRebalance = true;
    const officerId = 'current-user'; // TODO: Replace with actual user ID

    const confirmation = {
      date: this.selectedDate,
      officerId: officerId,
      reassignments: this.pendingReassignments
    };

    this.rebalanceService.applyRebalancing(confirmation).subscribe({
      next: (result) => {
        this.isApplyingRebalance = false;
        if (result.success) {
          this.closeRebalanceModal();
          // Show success notification
          this.saveSuccess = true;
          this.saveMessages = [
            `✓ Applied ${result.reassignmentsApplied} dock reassignments`,
            ...result.messages
          ];
          setTimeout(() => {
            this.saveSuccess = false;
            this.saveMessages = [];
          }, 5000);
        } else {
          this.rebalanceError = result.messages.join(', ');
        }
      },
      error: (err) => {
        this.isApplyingRebalance = false;
        console.error('Apply rebalancing error:', err);
        
        if (err.status === 0) {
          this.rebalanceError = 'Cannot connect to backend. Please ensure the API is running.';
        } else if (err.status === 400) {
          this.rebalanceError = err.error?.message || err.error || 'Invalid request parameters';
        } else if (err.status === 500) {
          this.rebalanceError = err.error?.details || err.error?.error || 'Server error occurred';
        } else {
          this.rebalanceError = err.error?.message || err.message || 'Failed to apply rebalancing';
        }
      }
    });
  }

  /**
   * Check if rebalancing is recommended based on proposal
   */
  isRebalanceRecommended(): boolean {
    if (!this.rebalanceProposal) return false;
    return this.rebalanceProposal.recommendationReason?.toLowerCase().includes('recommended') || false;
  }

  /**
   * Calculate delay improvement percentage
   */
  getDelayImprovement(): number {
    if (!this.rebalanceProposal?.currentMetrics || !this.rebalanceProposal?.proposedMetrics) return 0;
    return this.rebalanceService.calculateImprovement(
      this.rebalanceProposal.currentMetrics,
      this.rebalanceProposal.proposedMetrics
    );
  }

  /**
   * Calculate balance score improvement
   */
  getBalanceImprovement(): number {
    if (!this.rebalanceProposal?.currentMetrics || !this.rebalanceProposal?.proposedMetrics) return 0;
    return this.rebalanceProposal.proposedMetrics.balanceScore - this.rebalanceProposal.currentMetrics.balanceScore;
  }

  /**
   * Get vessel name from VVN ID
   */
  getVesselName(vvnId: number): string {
    const allocation = this.rebalanceProposal?.currentAllocations?.find(a => a.vvnId === vvnId);
    return allocation?.vesselName || `VVN-${vvnId}`;
  }

  /**
   * Get vessel IMO from VVN ID
   */
  getVesselImo(vvnId: number): string {
    const allocation = this.rebalanceProposal?.currentAllocations?.find(a => a.vvnId === vvnId);
    return allocation?.vesselImoNumber || '';
  }

  /**
   * US 4.3.1: Determine which algorithm performed better based on total delay
   * @returns 1 if algorithm1 is better, 2 if algorithm2 is better, 0 if equal
   */
  getBetterAlgorithm(): number {
    if (!this.comparisonResult1 || !this.comparisonResult2) return 0;
    if (this.comparisonResult1.error || this.comparisonResult2.error) return 0;
    if (this.comparisonResult1.totalDelay < this.comparisonResult2.totalDelay) return 1;
    if (this.comparisonResult2.totalDelay < this.comparisonResult1.totalDelay) return 2;
    // If delays are equal, prefer faster algorithm
    if (this.comparisonResult1.computationTime < this.comparisonResult2.computationTime) return 1;
    if (this.comparisonResult2.computationTime < this.comparisonResult1.computationTime) return 2;
    return 0;
  }

  /**
   * US 4.3.1: Calculate the percentage difference in delay between algorithms
   */
  getDelayDifferencePercent(): number {
    if (!this.comparisonResult1 || !this.comparisonResult2) return 0;
    if (this.comparisonResult1.error || this.comparisonResult2.error) return 0;
    const maxDelay = Math.max(this.comparisonResult1.totalDelay, this.comparisonResult2.totalDelay);
    if (maxDelay === 0) return 0;
    const diff = Math.abs(this.comparisonResult1.totalDelay - this.comparisonResult2.totalDelay);
    return (diff / maxDelay) * 100;
  }

  /**
   * US 4.3.1: Calculate how many times faster one algorithm is than the other
   */
  getSpeedRatio(): number {
    if (!this.comparisonResult1 || !this.comparisonResult2) return 1;
    if (this.comparisonResult1.error || this.comparisonResult2.error) return 1;
    const time1 = this.comparisonResult1.computationTime || 1;
    const time2 = this.comparisonResult2.computationTime || 1;
    return Math.max(time1, time2) / Math.min(time1, time2);
  }
}

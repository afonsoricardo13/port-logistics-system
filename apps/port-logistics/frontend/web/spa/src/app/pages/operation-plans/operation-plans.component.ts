import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { SchedulingService, DailyScheduleResponse } from '../../services/scheduling.service';
import { OperationPlanningService, VvnsWithoutPlansResponse, VvnWithoutPlan } from '../../services/operation-planning.service';
import { DockRebalanceService, DockRebalanceProposal, VesselDockReassignment } from '../../services/dock-rebalance.service';
import { OperationPlansListComponent } from '../operation-plans-list/operation-plans-list.component';

/**
 * US 4.1.2 - Operation Plans Management
 * US 4.1.5 - Identify and regenerate missing plans
 * Generate and view operation plans for vessel visit notifications
 */
@Component({
  selector: 'app-operation-plans',
  standalone: true,
  imports: [CommonModule, FormsModule, OperationPlansListComponent],
  templateUrl: './operation-plans.component.html',
  styleUrls: ['./operation-plans.component.scss']
})
export class OperationPlansComponent implements OnInit {
  // Active tab
  activeTab: 'generate' | 'missing' | 'search' = 'generate';

  // Generation form
  selectedDate: string = '';
  minDate: string = '';
  selectedAlgorithm: 'optimal' | 'heuristic' | 'multi_crane' | 'genetic' | 'auto' = 'auto';
  
  // US 4.3.1 - Genetic Algorithm parameters
  gaPopulationSize: number = 20;
  gaGenerations: number = 50;
  gaCrossoverRate: number = 70;
  gaMutationRate: number = 20;
  gaTimeLimit: number = 5000;
  gaUseMultiCrane: boolean = false;
  
  // State
  isGenerating: boolean = false;
  isSaving: boolean = false;
  isLoadingMissing: boolean = false;
  isRegenerating: boolean = false;
  
  // Results
  scheduleResult: DailyScheduleResponse | null = null;
  hasResult: boolean = false;
  errorMessage: string = '';
  saveSuccess: boolean = false;
  saveError: string = '';
  saveMessages: string[] = [];

  // US 4.1.5: Missing Plans
  missingPlansData: VvnsWithoutPlansResponse | null = null;
  missingPlansError: string = '';
  showRegenerateConfirm: boolean = false;
  regenerateDate: string = '';
  regenerateAlgorithm: 'optimal' | 'heuristic' | 'multi_crane' | 'genetic' | 'auto' = 'auto';

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

  // US 4.3.1: Compare Algorithms Feature
  showCompareModal: boolean = false;
  compareAlgorithm1: 'optimal' | 'heuristic' | 'genetic' = 'optimal';
  compareAlgorithm2: 'optimal' | 'heuristic' | 'genetic' = 'heuristic';
  comparisonResult1: any = null;
  comparisonResult2: any = null;
  isComparing: boolean = false;

  constructor(
    private schedulingService: SchedulingService,
    private opPlanning: OperationPlanningService,
    private rebalanceService: DockRebalanceService
  ) {}

  ngOnInit(): void {
    const today = new Date();
    this.selectedDate = this.formatDateForInput(today);
    this.minDate = this.selectedDate;
  }

  /**
   * Generate schedule preview before saving
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

    this.errorMessage = '';
    this.hasResult = false;
    this.isGenerating = true;
    this.saveSuccess = false;
    this.saveError = '';

    // Prepare genetic algorithm parameters if using genetic algorithm
    const geneticParams = this.selectedAlgorithm === 'genetic' ? {
      populationSize: this.gaPopulationSize,
      generations: this.gaGenerations,
      crossoverRate: this.gaCrossoverRate,
      mutationRate: this.gaMutationRate,
      timeLimit: this.gaTimeLimit,
      useMultiCrane: this.gaUseMultiCrane
    } : undefined;

    this.schedulingService.generateDailySchedule(this.selectedDate, this.selectedAlgorithm, geneticParams).subscribe({
      next: (response) => {
        this.isGenerating = false;
        
        if (response.success) {
          this.scheduleResult = response;
          this.hasResult = true;
          
          if (response.warnings && response.warnings.length > 0) {
            this.errorMessage = `Warning: ${response.warnings.join(', ')}`;
          }
        } else {
          this.errorMessage = response.message || response.error || 'Failed to generate schedule';
        }
      },
      error: (errorResponse) => {
        this.isGenerating = false;
        this.errorMessage = errorResponse.error || 'An unexpected error occurred';
      }
    });
  }

  /**
   * Save generated schedule as Operation Plans
   */
  saveOperationPlans(): void {
    if (!this.hasResult || !this.scheduleResult) {
      this.saveError = 'No schedule available to save';
      return;
    }

    this.isSaving = true;
    this.saveSuccess = false;
    this.saveError = '';
    this.saveMessages = [];

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
          
          if (response.plans && response.plans.length > 0) {
            response.plans.forEach(plan => {
              this.saveMessages.push(`  • VVN ${plan.vvnId}`);
            });
          }
          
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
        
        setTimeout(() => {
          this.saveError = '';
          this.saveMessages = [];
        }, 5000);
      }
    });
  }

  reset(): void {
    this.hasResult = false;
    this.scheduleResult = null;
    this.errorMessage = '';
    this.saveMessages = [];
    this.saveSuccess = false;
    this.saveError = '';
    this.showRebalanceRequiredWarning = false;
    // Note: Don't reset rebalanceExecutedForDate here to preserve state
    const today = new Date();
    this.selectedDate = this.formatDateForInput(today);
  }

  /**
   * Switch tabs
   */
  switchToTab(tab: 'generate' | 'missing' | 'search'): void {
    this.activeTab = tab;
    if (tab === 'missing') {
      this.loadMissingPlans();
    }
  }

  /**
   * US 4.1.5: Load VVNs without Operation Plans
   */
  loadMissingPlans(): void {
    if (!this.selectedDate) {
      this.missingPlansError = 'Please select a target date';
      return;
    }

    this.isLoadingMissing = true;
    this.missingPlansError = '';
    this.missingPlansData = null;

    const dateObj = new Date(this.selectedDate);
    const isoDate = dateObj.toISOString();

    this.opPlanning.getVvnsWithoutPlans(isoDate).subscribe({
      next: (response) => {
        this.isLoadingMissing = false;
        this.missingPlansData = response;
        
        if (!response.success) {
          this.missingPlansError = response.message || 'Failed to load missing plans';
        }
      },
      error: (err) => {
        this.isLoadingMissing = false;
        this.missingPlansError = err.error?.message || 'Failed to retrieve VVNs without Operation Plans';
      }
    });
  }

  /**
   * US 4.1.5: Show regenerate confirmation dialog
   */
  promptRegenerate(): void {
    if (!this.selectedDate) {
      this.missingPlansError = 'Please select a target date';
      return;
    }

    // AC5: Require rebalancing before regenerating plans
    if (this.rebalanceExecutedForDate !== this.selectedDate) {
      this.showRebalanceRequiredWarning = true;
      this.missingPlansError = 'Dock rebalancing must be analyzed before regenerating plans. Please go to Daily Schedule tab and click "Suggest Rebalancing" first.';
      return;
    }
    this.showRebalanceRequiredWarning = false;

    this.regenerateDate = this.selectedDate;
    this.showRegenerateConfirm = true;
  }

  /**
   * US 4.1.5: Cancel regeneration
   */
  cancelRegenerate(): void {
    this.showRegenerateConfirm = false;
    this.regenerateDate = '';
  }

  /**
   * US 4.1.5: Confirm and execute regeneration
   */
  confirmRegenerate(): void {
    if (!this.regenerateDate) {
      return;
    }

    this.isRegenerating = true;
    this.missingPlansError = '';
    this.showRegenerateConfirm = false;

    const dateObj = new Date(this.regenerateDate);
    const isoDate = dateObj.toISOString();

    this.opPlanning.regenerateOperationPlans(isoDate, this.regenerateAlgorithm).subscribe({
      next: (response) => {
        this.isRegenerating = false;
        
        if (response.success) {
          // Show success message and reload missing plans
          this.saveSuccess = true;
          this.saveMessages = [
            `✓ Successfully regenerated ${response.plansGenerated} operation plan(s)`,
            response.message || '',
            `Algorithm: ${this.regenerateAlgorithm}`,
            `Date: ${this.regenerateDate}`
          ];
          
          // Reload missing plans after a short delay
          setTimeout(() => {
            this.loadMissingPlans();
            this.saveSuccess = false;
            this.saveMessages = [];
          }, 3000);
        } else {
          this.missingPlansError = response.message || 'Failed to regenerate operation plans';
          if (response.error) {
            this.missingPlansError += ': ' + response.error;
          }
        }
      },
      error: (err) => {
        this.isRegenerating = false;
        this.missingPlansError = 'Failed to regenerate operation plans';
        if (err.error?.message) {
          this.missingPlansError += ': ' + err.error.message;
        }
      }
    });
  }

  formatTime(time: number | string): string {
    return this.schedulingService.formatHourToTime(time);
  }

  getDuration(operation: any): string {
    return this.schedulingService.calculateDuration(operation);
  }

  // Multi-Crane Results Methods
  hasMultiCraneResults(): boolean {
    if (!this.scheduleResult) return false;
    
    // Check if it's multi-crane algorithm or genetic with multi-crane
    const isMultiCrane = this.scheduleResult.algorithm === 'multi_crane';
    const isGeneticMulti = this.scheduleResult.algorithm === 'genetic' && 
                          this.scheduleResult.strategy === 'multi_crane';
    
    return isMultiCrane || isGeneticMulti;
  }

  getBaselineDelay(): string {
    if (!this.scheduleResult) return '0.0';
    
    // Use baseline_delay from backend if available
    if (this.scheduleResult.baseline_delay !== undefined) {
      return this.scheduleResult.baseline_delay.toFixed(1);
    }
    
    // Fallback for multi_crane algorithm
    if (this.scheduleResult.algorithm === 'multi_crane' && this.scheduleResult.crane_hours_single) {
      // For multi-crane, baseline should be calculated differently
      // Approximate from the data we have
      return (this.scheduleResult.total_delay * 2).toFixed(1);
    }
    
    return this.scheduleResult.total_delay?.toFixed(1) || '0.0';
  }

  getOptimizedDelay(): string {
    return (this.scheduleResult?.total_delay || 0).toFixed(1);
  }

  getBaselineCraneHours(): string {
    if (this.scheduleResult?.crane_hours_single !== undefined) {
      return this.scheduleResult.crane_hours_single.toFixed(1);
    }
    
    // Estimate from schedule
    let total = 0;
    this.scheduleResult?.schedule?.forEach((op: any) => {
      const duration = parseFloat(op.duration || op.end_time - op.start_time);
      total += duration;
    });
    return total.toFixed(1);
  }

  getOptimizedCraneHours(): string {
    if (this.scheduleResult?.crane_hours_multi !== undefined) {
      return this.scheduleResult.crane_hours_multi.toFixed(1);
    }
    
    // Calculate from cranes allocation
    let total = 0;
    const cranesAlloc = this.scheduleResult?.cranes_allocation || [];
    this.scheduleResult?.schedule?.forEach((op: any, i: number) => {
      const duration = parseFloat(op.duration || (op.end_time - op.start_time));
      const cranes = cranesAlloc[i] || 1;
      total += duration * cranes;
    });
    return total.toFixed(1);
  }

  getDelayReduction(): number {
    const baseline = parseFloat(this.getBaselineDelay());
    const optimized = parseFloat(this.getOptimizedDelay());
    return Math.max(0, baseline - optimized);
  }

  getMultiCraneVessels(): string {
    const cranesAlloc = this.scheduleResult?.cranes_allocation || [];
    const vessels: string[] = [];
    
    this.scheduleResult?.schedule?.forEach((op: any, i: number) => {
      if (cranesAlloc[i] > 1) {
        const cranesList = this.getAssignedCranes(op).split(', ');
        vessels.push(`${op.vessel_id} (${cranesList.join(', ')})`);
      }
    });
    
    return vessels.join(', ') || 'None';
  }

  getCranesUsed(index: number): number {
    const cranesAlloc = this.scheduleResult?.cranes_allocation || [];
    return cranesAlloc[index] || 1;
  }

  getAssignedCranes(operation: any): string {
    // If assigned_cranes array exists (for multi-crane), join them
    if (operation.assigned_cranes && Array.isArray(operation.assigned_cranes)) {
      return operation.assigned_cranes.join(', ');
    }
    
    // Otherwise return single crane
    return operation.assigned_crane || 'N/A';
  }

  // US 4.3.2: Format selection reason for display
  formatSelectionReason(reason: string): string {
    const reasonMap: { [key: string]: string } = {
      'small_instance': 'Small problem size (≤5 vessels) - optimal search is feasible',
      'medium_instance': 'Medium problem size (6-15 vessels) - heuristic for speed',
      'large_instance': 'Large problem size (>15 vessels) - genetic algorithm for exploration',
      'time_constrained': 'Tight time constraint - heuristic for fast response',
      'large_time_constrained': 'Large instance with time constraint - heuristic for speed',
      'high_complexity': 'High complexity (many docks) - genetic algorithm recommended',
      'quality_preferred': 'Quality preferred on medium instance - optimal for best results',
      'default_fallback': 'Default selection - heuristic as safe choice'
    };
    return reasonMap[reason] || reason;
  }

  private formatDateForInput(date: Date): string {
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
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

    const officerId = 'current-user'; // TODO: Replace with actual user ID from auth

    this.rebalanceService.getProposal(this.selectedDate, officerId).subscribe({
      next: (proposal) => {
        this.isLoadingRebalance = false;
        this.rebalanceProposal = proposal;
        
        // AC5: Mark rebalancing as executed for this date (allows running algorithms)
        this.rebalanceExecutedForDate = this.selectedDate;
        this.showRebalanceRequiredWarning = false;
        this.errorMessage = '';
        
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
    const officerId = 'current-user';

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

  isRebalanceRecommended(): boolean {
    if (!this.rebalanceProposal) return false;
    return this.rebalanceProposal.recommendationReason?.toLowerCase().includes('recommended') || false;
  }

  getDelayImprovementPercent(): number {
    if (!this.rebalanceProposal?.currentMetrics || !this.rebalanceProposal?.proposedMetrics) return 0;
    return this.rebalanceService.calculateImprovement(
      this.rebalanceProposal.currentMetrics,
      this.rebalanceProposal.proposedMetrics
    );
  }

  getBalanceImprovement(): number {
    if (!this.rebalanceProposal?.currentMetrics || !this.rebalanceProposal?.proposedMetrics) return 0;
    return this.rebalanceProposal.proposedMetrics.balanceScore - this.rebalanceProposal.currentMetrics.balanceScore;
  }

  getRebalanceVesselName(vvnId: number): string {
    const allocation = this.rebalanceProposal?.currentAllocations?.find(a => a.vvnId === vvnId);
    return allocation?.vesselName || `VVN-${vvnId}`;
  }

  getRebalanceVesselImo(vvnId: number): string {
    const allocation = this.rebalanceProposal?.currentAllocations?.find(a => a.vvnId === vvnId);
    return allocation?.vesselImoNumber || '';
  }

  // ===============================
  // US 4.3.1 - Compare Algorithms
  // ===============================

  /**
   * Open the compare algorithms modal
   */
  openCompareModal(): void {
    this.showCompareModal = true;
    this.comparisonResult1 = null;
    this.comparisonResult2 = null;
  }

  /**
   * Close the compare algorithms modal
   */
  closeCompareModal(): void {
    this.showCompareModal = false;
    this.comparisonResult1 = null;
    this.comparisonResult2 = null;
  }

  /**
   * Run comparison between two selected algorithms
   */
  compareAlgorithms(): void {
    if (!this.selectedDate || this.compareAlgorithm1 === this.compareAlgorithm2) {
      return;
    }

    // Require rebalancing before comparing
    if (this.rebalanceExecutedForDate !== this.selectedDate) {
      this.showRebalanceRequiredWarning = true;
      this.errorMessage = 'Dock rebalancing must be analyzed before comparing algorithms. Please click "Suggest Rebalancing" first.';
      this.closeCompareModal();
      return;
    }

    this.isComparing = true;
    this.comparisonResult1 = null;
    this.comparisonResult2 = null;

    // Prepare genetic params
    const geneticParams = {
      populationSize: this.gaPopulationSize,
      generations: this.gaGenerations,
      crossoverRate: this.gaCrossoverRate,
      mutationRate: this.gaMutationRate,
      timeLimit: this.gaTimeLimit,
      useMultiCrane: this.gaUseMultiCrane
    };

    // Run first algorithm
    const alg1Params = this.compareAlgorithm1 === 'genetic' ? geneticParams : undefined;
    const alg2Params = this.compareAlgorithm2 === 'genetic' ? geneticParams : undefined;

    // Run both algorithms sequentially
    this.schedulingService.generateDailySchedule(this.selectedDate, this.compareAlgorithm1, alg1Params).subscribe({
      next: (result1) => {
        this.comparisonResult1 = this.extractComparisonMetrics(result1);
        
        // Run second algorithm
        this.schedulingService.generateDailySchedule(this.selectedDate, this.compareAlgorithm2, alg2Params).subscribe({
          next: (result2) => {
            this.comparisonResult2 = this.extractComparisonMetrics(result2);
            this.isComparing = false;
          },
          error: (err) => {
            this.isComparing = false;
            this.comparisonResult2 = { error: err.error?.message || 'Failed to run second algorithm' };
          }
        });
      },
      error: (err) => {
        this.isComparing = false;
        this.comparisonResult1 = { error: err.error?.message || 'Failed to run first algorithm' };
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
   * Get display name for algorithm
   */
  getAlgorithmDisplayName(algorithm: string): string {
    const names: { [key: string]: string } = {
      'optimal': 'Optimal',
      'heuristic': 'Heuristic',
      'genetic': 'Genetic Algorithm'
    };
    return names[algorithm] || algorithm;
  }

  /**
   * Determine which algorithm performed better based on total delay
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
   * Calculate the percentage difference in delay between algorithms
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
   * Calculate how many times faster one algorithm is than the other
   */
  getSpeedRatio(): number {
    if (!this.comparisonResult1 || !this.comparisonResult2) return 1;
    if (this.comparisonResult1.error || this.comparisonResult2.error) return 1;
    const time1 = this.comparisonResult1.computationTime || 1;
    const time2 = this.comparisonResult2.computationTime || 1;
    return Math.max(time1, time2) / Math.min(time1, time2);
  }
}

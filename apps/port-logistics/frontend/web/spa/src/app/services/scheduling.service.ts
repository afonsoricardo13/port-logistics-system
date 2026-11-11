import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError, map } from 'rxjs/operators';
import { environment } from '../../environments/environment';

/**
 * US 3.4.2 - Scheduling Service
 * 
 * Service to interact with the Prolog scheduling backend
 * Consumes REST API for daily vessel scheduling operations
 */

export interface ScheduleOperation {
  vessel_id: string;
  vessel_name?: string;
  start_time: number | string; // Can be decimal (1.333) or formatted ("01:20")
  end_time: number | string;
  duration?: string; // Formatted duration "04:00"
  start_time_decimal?: number; // Original decimal value
  end_time_decimal?: number;
  desired_departure?: number;
  assigned_dock: string;
  assigned_crane: string;
  assigned_cranes?: string[]; // US 3.4.5 - Multiple cranes for multi-crane operations
  assigned_staff: string;
  assigned_storage: string;
  delay?: number; // Individual operation delay in hours
  delay_hours?: number;
  early_by_hours?: number;
  completion_status?: 'EARLY' | 'ON_TIME' | 'DELAYED';
  status_message?: string;
  warnings?: string[];
}

export interface UnscheduledVessel {
  vessel_id: string;
  arrival_time: number;
  departure_time: number;
  reason: string;
}

export interface DailyScheduleRequest {
  date: string; // YYYY-MM-DD format
  algorithm?: 'optimal' | 'heuristic' | 'multi_crane' | 'genetic' | 'auto'; // US 3.4.4 & 3.4.5 & 4.3.1 & 4.3.2 - Algorithm selection
  // US 4.3.1 - Genetic Algorithm parameters
  ga_population_size?: number;
  ga_generations?: number;
  ga_crossover_rate?: number;
  ga_mutation_rate?: number;
  ga_time_limit?: number;
  ga_use_multi_crane?: boolean;
  // US 4.3.2 - Auto algorithm parameters
  time_limit_ms?: number;
  prefer_quality?: boolean;
}

export interface DailyScheduleResponse {
  success: boolean;
  date: string;
  algorithm?: string; // US 3.4.4 & 3.4.5 & 4.3.1 & 4.3.2 - Algorithm used
  vessel_count: number;
  scheduled_count: number;
  unscheduled_count: number;
  total_delay: number;
  computation_time_ms: number;
  schedule: ScheduleOperation[];
  unscheduled_vessels: UnscheduledVessel[];
  warnings: string[];
  error?: string;
  message?: string;
  
  // US 3.4.5 - Multi-crane specific fields
  strategy?: 'single_crane' | 'multi_crane'; // Strategy used by multi-crane algorithm
  baseline_delay?: number; // Delay with single crane (for multi-crane)
  crane_hours_single?: number; // Total crane-hours with single crane
  crane_hours_multi?: number; // Total crane-hours with multi-crane
  cranes_allocation?: number[]; // Number of cranes allocated per operation
  
  // US 4.3.1 - Genetic Algorithm specific fields
  ga_parameters?: {
    population_size?: number;
    max_generations?: number;
    crossover_rate?: number;
    mutation_rate?: number;
    use_multi_crane?: boolean;
  };
  
  // US 4.3.2 - Auto Algorithm Selection fields
  selected_algorithm?: string; // The algorithm that was actually selected
  selection_reason?: string; // Why this algorithm was chosen
  problem_analysis?: {
    vessel_count?: number;
    dock_count?: number;
    complexity_score?: number;
    time_limit_ms?: number;
  };
  algorithm_computation_time_ms?: number; // Time spent by the selected algorithm
}

@Injectable({
  providedIn: 'root'
})
export class SchedulingService {
  // Prolog scheduling API base URL - uses environment configuration
  private readonly API_URL = `${environment.schedulingApiUrl}/api/scheduling`;

  constructor(private http: HttpClient) {}

  /**
   * US 3.4.2 / US 3.4.4 / US 3.4.5 / US 4.3.1 / US 4.3.2 - Generate daily schedule
   * 
   * Calls the Prolog backend to compute vessel schedule for a given date
   * Supports optimal, heuristic, multi-crane, genetic, and auto algorithms
   * 
   * @param date Target date in YYYY-MM-DD format
   * @param algorithm Algorithm to use: 'auto' (default), 'optimal', 'heuristic', 'multi_crane', or 'genetic'
   * @param geneticParams Optional parameters for genetic algorithm
   * @returns Observable with schedule result
   */
  generateDailySchedule(
    date: string, 
    algorithm?: 'optimal' | 'heuristic' | 'multi_crane' | 'genetic' | 'auto',
    geneticParams?: {
      populationSize?: number;
      generations?: number;
      crossoverRate?: number;
      mutationRate?: number;
      timeLimit?: number;
      useMultiCrane?: boolean;
    }
  ): Observable<DailyScheduleResponse> {
    const request: DailyScheduleRequest = { 
      date,
      algorithm: algorithm || 'auto'
    };
    
    // Add genetic algorithm parameters if using genetic algorithm
    if (algorithm === 'genetic' && geneticParams) {
      request.ga_population_size = geneticParams.populationSize;
      request.ga_generations = geneticParams.generations;
      request.ga_crossover_rate = geneticParams.crossoverRate;
      request.ga_mutation_rate = geneticParams.mutationRate;
      request.ga_time_limit = geneticParams.timeLimit;
      request.ga_use_multi_crane = geneticParams.useMultiCrane;
    }

    return this.http.post<DailyScheduleResponse>(`${this.API_URL}/daily`, request)
      .pipe(
        map(response => {
          // Ensure response has expected structure
          if (!response.success && !response.error && !response.message) {
            throw new Error('Invalid response from scheduling service');
          }
          return response;
        }),
        catchError(error => {
          console.error('Error generating daily schedule:', error);
          
          // Return user-friendly error
          const errorResponse: DailyScheduleResponse = {
            success: false,
            date: date,
            vessel_count: 0,
            scheduled_count: 0,
            unscheduled_count: 0,
            total_delay: 0,
            computation_time_ms: 0,
            schedule: [],
            unscheduled_vessels: [],
            warnings: [],
            error: error.error?.message || error.message || 'Failed to generate schedule'
          };
          
          return throwError(() => errorResponse);
        })
      );
  }

  /**
   * Check if scheduling service is healthy
   * 
   * @returns Observable<boolean>
   */
  checkHealth(): Observable<boolean> {
    return this.http.get<{ status: string }>(`${this.API_URL}/health`)
      .pipe(
        map(response => response.status === 'ok'),
        catchError((error) => {
          console.warn('Scheduling service is not reachable', error);
          throw error;
        })
      );
  }

  /**
   * Format decimal hour to HH:MM or return string if already formatted
   */
  formatHourToTime(hour: number | string): string {
    // If already a string in HH:MM format, return as is
    if (typeof hour === 'string') {
      return hour;
    }
    
    // Convert decimal to HH:MM
    const hours = Math.floor(hour);
    const minutes = Math.round((hour - hours) * 60);
    return `${hours.toString().padStart(2, '0')}:${minutes.toString().padStart(2, '0')}`;
  }

  /**
   * Calculate operation duration
   */
  calculateDuration(operation: ScheduleOperation): string {
    // If duration is already provided as string, return it
    if (operation.duration && typeof operation.duration === 'string') {
      return operation.duration;
    }
    
    // Calculate from start and end times
    const start = typeof operation.start_time === 'number' ? operation.start_time : this.parseTimeToHours(operation.start_time as string);
    const end = typeof operation.end_time === 'number' ? operation.end_time : this.parseTimeToHours(operation.end_time as string);
    const durationHours = end - start;
    
    // Return as formatted string
    return this.formatHourToTime(durationHours);
  }
  
  /**
   * Parse HH:MM string to decimal hours
   */
  private parseTimeToHours(timeStr: string): number {
    const [hours, minutes] = timeStr.split(':').map(Number);
    return hours + (minutes / 60);
  }

  /**
   * Check if resources are available for operation
   * 
   * @param operation Schedule operation
   * @returns Warning messages if resources missing
   */
  checkResourceAvailability(operation: ScheduleOperation): string[] {
    // Use warnings from backend if available
    if (operation.warnings && operation.warnings.length > 0) {
      return operation.warnings;
    }

    // Check only actual missing resources (not cascading failures)
    const warnings: string[] = [];

    if (operation.assigned_dock === 'NO_DOCK_AVAILABLE') {
      warnings.push('No dock available');
    }

    if (operation.assigned_crane === 'NO_CRANE_AVAILABLE') {
      warnings.push('No crane available');
    }

    // Only show staff warning if crane is available
    if (operation.assigned_staff === 'NO_STAFF_AVAILABLE' && 
        operation.assigned_crane !== 'NO_CRANE_AVAILABLE') {
      warnings.push('No qualified staff available');
    }

    // Only show storage warning if both crane and staff are available
    if (operation.assigned_storage === 'NO_STORAGE_AVAILABLE' &&
        operation.assigned_crane !== 'NO_CRANE_AVAILABLE' &&
        operation.assigned_staff !== 'NO_STAFF_AVAILABLE') {
      warnings.push('No storage area available');
    }

    return warnings;
  }
}

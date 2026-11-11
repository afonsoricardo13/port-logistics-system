% ============================================================================
% Validation Tests for Scheduling Algorithms
% Tests for US 3.4.2, US 3.4.4, and US 3.4.5
% ============================================================================

:- consult(scheduling_algorithms).

% ============================================================================
% TEST 1: Optimal Scheduling (US 3.4.2)
% ============================================================================

test_optimal_no_delay :-
    format('~n=== TEST 1: Optimal Scheduling - No Delays ===~n'),
    Vessels = [
        _{id: "V001", arrival_time: 8.0, departure_time: 14.0, 
          unload_time: 2.0, load_time: 1.5, assigned_dock: "DOCK-A"},
        _{id: "V002", arrival_time: 14.0, departure_time: 20.0, 
          unload_time: 2.0, load_time: 1.5, assigned_dock: "DOCK-A"}
    ],
    
    scheduling_algorithms:compute_optimal_schedule(Vessels, Result),
    
    format('Algorithm: ~w~n', [Result.algorithm]),
    format('Total Delay: ~w hours~n', [Result.total_delay]),
    format('Computation Time: ~w ms~n', [Result.computation_time_ms]),
    
    (   Result.total_delay =:= 0
    ->  format('✓ TEST 1 PASSED: No delays as expected~n')
    ;   format('✗ TEST 1 FAILED: Expected 0 delay, got ~w~n', [Result.total_delay])
    ).

test_optimal_with_delays :-
    format('~n=== TEST 2: Optimal Scheduling - With Delays ===~n'),
    Vessels = [
        _{id: "V003", arrival_time: 8.0, departure_time: 12.0, 
          unload_time: 2.0, load_time: 2.0, assigned_dock: "DOCK-A"},
        _{id: "V004", arrival_time: 9.0, departure_time: 13.0, 
          unload_time: 2.0, load_time: 2.0, assigned_dock: "DOCK-A"}
    ],
    
    scheduling_algorithms:compute_optimal_schedule(Vessels, Result),
    
    format('Algorithm: ~w~n', [Result.algorithm]),
    format('Total Delay: ~w hours~n', [Result.total_delay]),
    
    (   Result.total_delay > 0
    ->  format('✓ TEST 2 PASSED: Delays detected as expected~n')
    ;   format('✗ TEST 2 FAILED: Expected delays, got 0~n')
    ).

% ============================================================================
% TEST 3: Heuristic Scheduling (US 3.4.4)
% ============================================================================

test_heuristic_urgency_ordering :-
    format('~n=== TEST 3: Heuristic Scheduling - Urgency Ordering ===~n'),
    Vessels = [
        _{id: "V005", arrival_time: 8.0, departure_time: 20.0, 
          unload_time: 2.0, load_time: 1.5, assigned_dock: "DOCK-A"},
        _{id: "V006", arrival_time: 9.0, departure_time: 13.0, 
          unload_time: 1.5, load_time: 1.0, assigned_dock: "DOCK-A"}
    ],
    
    scheduling_algorithms:compute_heuristic_schedule(Vessels, Result),
    
    format('Algorithm: ~w~n', [Result.algorithm]),
    format('Total Delay: ~w hours~n', [Result.total_delay]),
    format('Computation Time: ~w ms~n', [Result.computation_time_ms]),
    
    % V006 should be scheduled first (more urgent)
    Result.schedule = [(FirstVessel, _, _, _, _, _, _) | _],
    
    (   FirstVessel = "V006"
    ->  format('✓ TEST 3 PASSED: Most urgent vessel scheduled first~n')
    ;   format('✗ TEST 3 FAILED: Expected V006 first, got ~w~n', [FirstVessel])
    ).

% ============================================================================
% TEST 4: Multi-Crane Scheduling (US 3.4.5)
% ============================================================================

test_multi_crane_no_delays :-
    format('~n=== TEST 4: Multi-Crane - No Delays (Should Stay Single) ===~n'),
    Vessels = [
        _{id: "V007", arrival_time: 8.0, departure_time: 14.0, 
          unload_time: 2.0, load_time: 1.5, assigned_dock: "DOCK-A"},
        _{id: "V008", arrival_time: 14.0, departure_time: 20.0, 
          unload_time: 2.0, load_time: 1.5, assigned_dock: "DOCK-A"}
    ],
    
    scheduling_algorithms:compute_optimal_schedule(Vessels, Baseline),
    scheduling_algorithms:compute_multi_crane_schedule(Vessels, Baseline, Multi),
    
    format('Baseline Delay: ~w hours~n', [Baseline.total_delay]),
    format('Strategy: ~w~n', [Multi.strategy]),
    format('Multi-Crane Delay: ~w hours~n', [Multi.total_delay]),
    
    (   Multi.strategy = single_crane
    ->  format('✓ TEST 4 PASSED: Single crane strategy maintained~n')
    ;   format('✗ TEST 4 FAILED: Expected single_crane, got ~w~n', [Multi.strategy])
    ).

test_multi_crane_with_delays :-
    format('~n=== TEST 5: Multi-Crane - With Delays (Should Use Multi) ===~n'),
    Vessels = [
        _{id: "V009", arrival_time: 8.0, departure_time: 11.0, 
          unload_time: 2.0, load_time: 2.0, assigned_dock: "DOCK-A"},
        _{id: "V010", arrival_time: 9.0, departure_time: 13.0, 
          unload_time: 2.0, load_time: 2.0, assigned_dock: "DOCK-A"},
        _{id: "V011", arrival_time: 10.0, departure_time: 15.0, 
          unload_time: 2.0, load_time: 2.0, assigned_dock: "DOCK-A"}
    ],
    
    scheduling_algorithms:compute_optimal_schedule(Vessels, Baseline),
    scheduling_algorithms:compute_multi_crane_schedule(Vessels, Baseline, Multi),
    
    format('Baseline Delay: ~w hours~n', [Baseline.total_delay]),
    format('Strategy: ~w~n', [Multi.strategy]),
    format('Multi-Crane Delay: ~w hours~n', [Multi.total_delay]),
    format('Crane Hours (Single): ~w~n', [Multi.crane_hours_single]),
    format('Crane Hours (Multi): ~w~n', [Multi.crane_hours_multi]),
    
    (   Multi.strategy = multi_crane,
        Multi.total_delay =< Baseline.total_delay
    ->  format('✓ TEST 5 PASSED: Multi-crane reduces or maintains delays~n')
    ;   format('✗ TEST 5 FAILED: Multi-crane should reduce delays~n')
    ).

test_multi_crane_few_vessels :-
    format('~n=== TEST 6: Multi-Crane - Few Vessels (≤2) ===~n'),
    Vessels = [
        _{id: "V012", arrival_time: 8.0, departure_time: 11.0, 
          unload_time: 2.0, load_time: 2.0, assigned_dock: "DOCK-A"},
        _{id: "V013", arrival_time: 9.0, departure_time: 13.0, 
          unload_time: 2.0, load_time: 2.0, assigned_dock: "DOCK-A"}
    ],
    
    scheduling_algorithms:compute_optimal_schedule(Vessels, Baseline),
    scheduling_algorithms:compute_multi_crane_schedule(Vessels, Baseline, Multi),
    
    format('Number of Vessels: 2~n'),
    format('Strategy: ~w~n', [Multi.strategy]),
    
    (   Multi.strategy = single_crane
    ->  format('✓ TEST 6 PASSED: Single crane maintained for ≤2 vessels~n')
    ;   format('✗ TEST 6 FAILED: Expected single_crane for 2 vessels~n')
    ).

% ============================================================================
% TEST 7: Multi-Dock Scenarios
% ============================================================================

test_multi_dock :-
    format('~n=== TEST 7: Multi-Dock Scheduling ===~n'),
    Vessels = [
        _{id: "V014", arrival_time: 8.0, departure_time: 12.0, 
          unload_time: 1.5, load_time: 1.0, assigned_dock: "DOCK-A"},
        _{id: "V015", arrival_time: 8.0, departure_time: 12.0, 
          unload_time: 1.5, load_time: 1.0, assigned_dock: "DOCK-B"},
        _{id: "V016", arrival_time: 9.0, departure_time: 13.0, 
          unload_time: 1.5, load_time: 1.0, assigned_dock: "DOCK-A"}
    ],
    
    scheduling_algorithms:compute_optimal_schedule(Vessels, Result),
    
    format('Total Delay: ~w hours~n', [Result.total_delay]),
    length(Result.schedule, NumOps),
    format('Number of Operations: ~w~n', [NumOps]),
    
    (   NumOps =:= 3
    ->  format('✓ TEST 7 PASSED: All vessels scheduled~n')
    ;   format('✗ TEST 7 FAILED: Expected 3 operations, got ~w~n', [NumOps])
    ).

% ============================================================================
% TEST 8: Comparison Between Algorithms
% ============================================================================

test_algorithm_comparison :-
    format('~n=== TEST 8: Algorithm Comparison ===~n'),
    Vessels = [
        _{id: "V017", arrival_time: 8.0, departure_time: 14.0, 
          unload_time: 2.0, load_time: 1.5, assigned_dock: "DOCK-A"},
        _{id: "V018", arrival_time: 10.0, departure_time: 16.0, 
          unload_time: 2.0, load_time: 1.5, assigned_dock: "DOCK-A"},
        _{id: "V019", arrival_time: 12.0, departure_time: 18.0, 
          unload_time: 2.0, load_time: 1.5, assigned_dock: "DOCK-A"}
    ],
    
    scheduling_algorithms:compute_optimal_schedule(Vessels, OptResult),
    scheduling_algorithms:compute_heuristic_schedule(Vessels, HeurResult),
    
    format('Optimal Delay: ~w hours (Time: ~w ms)~n', 
           [OptResult.total_delay, OptResult.computation_time_ms]),
    format('Heuristic Delay: ~w hours (Time: ~w ms)~n', 
           [HeurResult.total_delay, HeurResult.computation_time_ms]),
    
    (   OptResult.total_delay =< HeurResult.total_delay
    ->  format('✓ TEST 8 PASSED: Optimal ≤ Heuristic (as expected)~n')
    ;   format('⚠ TEST 8 WARNING: Heuristic better than optimal (unusual)~n')
    ).

% ============================================================================
% TEST 9: Empty and Invalid Inputs
% ============================================================================

test_empty_input :-
    format('~n=== TEST 9: Empty Vessel List ===~n'),
    Vessels = [],
    
    (   catch(scheduling_algorithms:compute_optimal_schedule(Vessels, Result), _, true)
    ->  (   get_dict(error, Result, _)
        ->  format('✓ TEST 9 PASSED: Empty input handled correctly~n')
        ;   (   Result.total_delay =:= 0
            ->  format('✓ TEST 9 PASSED: Empty list returns zero delay~n')
            ;   format('✗ TEST 9 FAILED: Unexpected result for empty list~n')
            )
        )
    ;   format('✓ TEST 9 PASSED: Empty input caused expected failure~n')
    ).

% ============================================================================
% TEST 10: Genetic Algorithm (US 4.3.1)
% ============================================================================

test_genetic_algorithm :-
    format('~n=== TEST 10: Genetic Algorithm Basic ===~n'),
    
    % Simple vessels for testing
    Vessels = [
        _{id: v1, arrival_time: 0, departure_time: 5, unload_time: 1, load_time: 1, assigned_dock: "DOCK-A"},
        _{id: v2, arrival_time: 1, departure_time: 6, unload_time: 1, load_time: 1, assigned_dock: "DOCK-A"},
        _{id: v3, arrival_time: 2, departure_time: 8, unload_time: 2, load_time: 1, assigned_dock: "DOCK-A"}
    ],
    
    % Default parameters
    Parameters = _{
        population_size: 10,
        generations: 20,
        crossover_rate: 70,
        mutation_rate: 20,
        time_limit_ms: 3000,
        use_multi_crane: false
    },
    
    scheduling_algorithms:compute_genetic_schedule(Vessels, Parameters, Result),
    
    format('Genetic Result: ~w~n', [Result]),
    
    (   get_dict(total_delay, Result, Delay),
        get_dict(schedule, Result, Schedule),
        get_dict(algorithm, Result, "genetic"),
        is_list(Schedule),
        length(Schedule, 3)
    ->  format('✓ TEST 10 PASSED: Genetic algorithm produced valid schedule~n'),
        format('  Total delay: ~2f hours~n', [Delay])
    ;   format('✗ TEST 10 FAILED: Genetic algorithm did not produce valid result~n')
    ).

test_genetic_with_multi_crane :-
    format('~n=== TEST 11: Genetic Algorithm with Multi-Crane ===~n'),
    
    % Vessels with tight departures (need multi-crane)
    Vessels = [
        _{id: v1, arrival_time: 0, departure_time: 2, unload_time: 1.5, load_time: 1.5, assigned_dock: "DOCK-A"},
        _{id: v2, arrival_time: 0.5, departure_time: 3, unload_time: 1, load_time: 1, assigned_dock: "DOCK-A"},
        _{id: v3, arrival_time: 1, departure_time: 4, unload_time: 2, load_time: 1, assigned_dock: "DOCK-A"}
    ],
    
    % Parameters with multi-crane enabled
    Parameters = _{
        population_size: 15,
        generations: 30,
        crossover_rate: 75,
        mutation_rate: 25,
        time_limit_ms: 5000,
        use_multi_crane: true
    },
    
    scheduling_algorithms:compute_genetic_schedule(Vessels, Parameters, Result),
    
    format('Genetic Multi-Crane Result: ~w~n', [Result]),
    
    (   get_dict(strategy, Result, Strategy),
        get_dict(total_delay, Result, Delay)
    ->  format('✓ TEST 11 PASSED: Genetic with multi-crane ran successfully~n'),
        format('  Strategy: ~w, Total delay: ~2f hours~n', [Strategy, Delay])
    ;   format('✗ TEST 11 FAILED: Genetic with multi-crane did not produce valid result~n')
    ).

test_genetic_parameters :-
    format('~n=== TEST 12: Genetic Algorithm Parameters ===~n'),
    
    Vessels = [
        _{id: v1, arrival_time: 0, departure_time: 5, unload_time: 1, load_time: 1, assigned_dock: "DOCK-A"},
        _{id: v2, arrival_time: 1, departure_time: 6, unload_time: 1, load_time: 1, assigned_dock: "DOCK-A"}
    ],
    
    % Custom parameters
    Parameters = _{
        population_size: 5,
        generations: 10,
        crossover_rate: 50,
        mutation_rate: 10,
        time_limit_ms: 2000,
        use_multi_crane: false
    },
    
    scheduling_algorithms:compute_genetic_schedule(Vessels, Parameters, Result),
    
    (   get_dict(parameters, Result, ReturnedParams),
        get_dict(population_size, ReturnedParams, 5),
        get_dict(max_generations, ReturnedParams, 10)
    ->  format('✓ TEST 12 PASSED: Parameters correctly returned in response~n')
    ;   format('⚠ TEST 12 WARNING: Parameters may not be fully returned~n')
    ).

% ============================================================================
% RUN ALL TESTS
% ============================================================================

run_all_tests :-
    format('~n========================================~n'),
    format('  SCHEDULING ALGORITHMS VALIDATION~n'),
    format('========================================~n'),
    
    test_optimal_no_delay,
    test_optimal_with_delays,
    test_heuristic_urgency_ordering,
    test_multi_crane_no_delays,
    test_multi_crane_with_delays,
    test_multi_crane_few_vessels,
    test_multi_dock,
    test_algorithm_comparison,
    test_empty_input,
    test_genetic_algorithm,
    test_genetic_with_multi_crane,
    test_genetic_parameters,

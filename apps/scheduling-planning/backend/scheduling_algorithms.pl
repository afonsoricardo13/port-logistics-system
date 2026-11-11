% ============================================================================
% Port Logistics - Scheduling Algorithms (Prolog)
% US 3.4.2 - Daily Vessel Scheduling with Resource Constraints (Optimal)
% US 3.4.4 - Alternative Heuristic Scheduling Algorithm (Fast)
% ============================================================================
% Algorithms to generate daily schedules for vessel loading/unloading operations
% minimizing delays relative to desired departure times.
%
% Based on IARTI course materials (scheduling_vessels_2.pl)
% Author: LEI-ISEP Team
% Date: November 2025
% ============================================================================

:- module(scheduling_algorithms, [
    compute_optimal_schedule/2,
    compute_heuristic_schedule/2,
    sequence_temporization/2,
    sum_delays/2,
    assign_resources_to_schedule/3,
    calculate_valid_delay/2,
    compute_multi_crane_schedule/3,       % US 3.4.5
    compute_genetic_schedule/3,           % US 4.3.1 - Genetic Algorithm
    compute_auto_schedule/3               % US 4.3.2 - Auto Algorithm Selection
]).

:- dynamic shortest_delay/2.
:- dynamic shortest_delay_multi/3.      % US 3.4.5 (multi-gruas)
:- dynamic user:vessel/6.

% US 4.3.1 - Genetic Algorithm dynamic predicates
:- dynamic ga_generations/1.
:- dynamic ga_population_size/1.
:- dynamic ga_prob_crossover/1.
:- dynamic ga_prob_mutation/1.
:- dynamic ga_time_limit/1.
:- dynamic ga_target_delay/1.
:- dynamic ga_stability_generations/1.
:- dynamic ga_best_solution/2.
:- dynamic ga_use_multi_crane/1.

% ============================================================================
% OPTIMAL SCHEDULING ALGORITHM (US 3.4.2 - AC: Minimize Delays)
% ============================================================================
% Uses permutation-based search to find optimal sequence minimizing total delay
% Time Complexity: O(n!) - only feasible for small datasets (n <= 10)
% ============================================================================

% Compute optimal schedule for vessels on given date
compute_optimal_schedule(Vessels, Result) :-
    % Validate input
    (   is_list(Vessels), Vessels \= []
    ->  true
    ;   Result = _{algorithm: "optimal", error: "Invalid or empty vessel list", total_delay: 0, schedule: []},
        !
    ),
    
    % Clean previous results
    retractall(shortest_delay(_, _)),
    retractall(shortest_delay_multi(_, _, _)),
    retractall(user:vessel(_, _, _, _, _, _)),
    
    % Load vessels as dynamic facts
    load_vessels_as_facts(Vessels),
    
    % Find optimal sequence
    get_time(StartTime),
    (compute_optimal_schedule_internal ; true),
    
    % Check if solution was found
    (   retract(shortest_delay(OptimalSequence, TotalDelay))
    ->  true
    ;   OptimalSequence = [], TotalDelay = 0
    ),
    
    get_time(EndTime),
    ComputationTime is (EndTime - StartTime) * 1000,
    
    % Build result with rounded delay
    RoundedDelay is round(TotalDelay * 100) / 100,
    Result = _{
        algorithm: "optimal",
        total_delay: RoundedDelay,
        computation_time_ms: ComputationTime,
        schedule: OptimalSequence
    }.
    % Don't cleanup - needed for calculate_valid_delay later
    % retractall(user:vessel(_, _, _, _, _, _)).

% Internal optimal scheduling computation
compute_optimal_schedule_internal :-
    asserta(shortest_delay(_, 1000000)),
    findall(V, user:vessel(V, _, _, _, _, _), VesselList),
    !,
    permutation(VesselList, Sequence),
    sequence_temporization(Sequence, ScheduleTriples),
    sum_delays(ScheduleTriples, Delay),
    update_shortest_delay(ScheduleTriples, Delay),
    fail.

% Update shortest delay if current is better
update_shortest_delay(Schedule, Delay) :-
    shortest_delay(_, CurrentMin),
    (   Delay < CurrentMin
    ->  retract(shortest_delay(_, _)),
        asserta(shortest_delay(Schedule, Delay))
    ;   true
    ).

% ============================================================================
% HEURISTIC SCHEDULING ALGORITHM (US 3.4.4)
% ============================================================================
% Uses greedy strategy with priority ordering for fast computation
% Time Complexity: O(n log n) - feasible for large datasets
% Prioritizes computational efficiency over optimality
% ============================================================================

% Compute heuristic schedule using greedy approach with urgency priority
compute_heuristic_schedule(Vessels, Result) :-
    % Validate input
    (   is_list(Vessels), Vessels \= []
    ->  true
    ;   Result = _{algorithm: "heuristic_greedy_urgency", error: "Invalid or empty vessel list", total_delay: 0, schedule: []},
        !
    ),
    
    % Clean previous results
    retractall(shortest_delay(_, _)),
    retractall(shortest_delay_multi(_, _, _)),
    retractall(user:vessel(_, _, _, _, _, _)),
    
    % Load vessels as dynamic facts
    load_vessels_as_facts(Vessels),
    
    % Find heuristic sequence
    get_time(StartTime),
    compute_heuristic_schedule_internal(HeuristicSequence, TotalDelay),
    get_time(EndTime),
    ComputationTime is (EndTime - StartTime) * 1000,
    
    % Build result
    Result = _{
        algorithm: "heuristic_greedy_urgency",
        total_delay: TotalDelay,
        computation_time_ms: ComputationTime,
        schedule: HeuristicSequence
    }.
    % Don't cleanup - needed for calculate_valid_delay later
    % retractall(user:vessel(_, _, _, _, _, _)).

% Internal heuristic scheduling using urgency-based greedy strategy
compute_heuristic_schedule_internal(Schedule, TotalDelay) :-
    % Get all vessels and calculate urgency
    findall(
        Urgency-VesselId,
        (
            user:vessel(VesselId, ArrivalTime, DepartureTime, UnloadTime, LoadTime, _),
            % Urgency = departure_time - (arrival_time + operation_time)
            % Lower urgency = more urgent (tighter deadline)
            OperationTime is UnloadTime + LoadTime,
            Urgency is DepartureTime - (ArrivalTime + OperationTime)
        ),
        UrgencyPairs
    ),
    
    % Sort by urgency (most urgent first)
    keysort(UrgencyPairs, SortedPairs),
    pairs_values(SortedPairs, SortedVessels),
    
    % Create schedule with urgency-based sequence
    sequence_temporization(SortedVessels, Schedule),
    sum_delays(Schedule, TotalDelay).

% ============================================================================
% TEMPORAL SEQUENCING (US 3.4.2 - AC2.a: One vessel per dock at a time)
% ============================================================================
% Converts vessel sequence into time-scheduled operations
% Ensures no overlap: next vessel starts after previous ends
% ============================================================================

% Convert vessel sequence to timed operations
sequence_temporization(VesselList, ScheduleTriples) :-
    % Group vessels by dock and schedule each dock independently
    findall(
        Dock,
        (member(V, VesselList), user:vessel(V, _, _, _, _, Dock)),
        AllDocks
    ),
    sort(AllDocks, UniqueDocks),
    schedule_all_docks(UniqueDocks, VesselList, ScheduleTriples).

% Schedule vessels for all docks
schedule_all_docks([], _, []).
schedule_all_docks([Dock | RestDocks], AllVessels, AllSchedule) :-
    % Get vessels for this dock
    findall(V, (member(V, AllVessels), user:vessel(V, _, _, _, _, Dock)), DockVessels),
    % Schedule this dock's vessels
    sequence_temporization_internal(0, DockVessels, DockSchedule),
    % Schedule remaining docks
    schedule_all_docks(RestDocks, AllVessels, RestSchedule),
    append(DockSchedule, RestSchedule, AllSchedule).

sequence_temporization_internal(_, [], []).

sequence_temporization_internal(EndPreviousOperation, [VesselId | RestVessels], 
                                [ScheduleEntry | RestSchedule]) :-
    user:vessel(VesselId, ArrivalTime, _, UnloadTime, LoadTime, AssignedDock),
    
    % AC2.a: One vessel per dock at a time (only for same dock)
    % Start unloading at arrival or after previous vessel finishes (whichever is later)
    StartUnload is max(ArrivalTime, EndPreviousOperation),
    
    % Calculate end time (unload + load)
    TotalOperationTime is UnloadTime + LoadTime,
    EndLoad is StartUnload + TotalOperationTime,
    
    % Convert times to HH:MM format for display
    hours_to_time_string(StartUnload, StartTimeStr),
    hours_to_time_string(EndLoad, EndTimeStr),
    hours_to_time_string(TotalOperationTime, DurationStr),
    
    % Create schedule entry with formatted times and assigned dock
    ScheduleEntry = (VesselId, StartUnload, EndLoad, StartTimeStr, EndTimeStr, DurationStr, AssignedDock),
    
    % Schedule remaining vessels (only affects same dock)
    sequence_temporization_internal(EndLoad, RestVessels, RestSchedule).

% ============================================================================
% DELAY CALCULATION (US 3.4.2 - AC: Minimize Delays)
% ============================================================================
% Calculates total delay between actual completion and desired departure times
% Delay = max(0, ActualDeparture - DesiredDeparture)
% ============================================================================

% Calculate total delay for schedule (only for valid operations)
sum_delays([], 0).

sum_delays([(VesselId, _, EndLoad, _, _, _, _) | RestSchedule], TotalDelay) :-
    user:vessel(VesselId, _, DesiredDeparture, _, _, _),
    
    % Calculate delay: only if operation finishes AFTER desired departure
    % DesiredDeparture already adjusted for multi-day (e.g., 32 = next day 08:00)
    % Delay = max(0, EndLoad - DesiredDeparture)
    VesselDelay is max(0, EndLoad - DesiredDeparture),
    format(user_error, 'DEBUG DELAY: Vessel=~w, EndLoad=~w, DesiredDeparture=~w, Delay=~w~n',
           [VesselId, EndLoad, DesiredDeparture, VesselDelay]),
    
    % Sum with remaining vessels
    sum_delays(RestSchedule, RestDelay),
    TotalDelay is round((VesselDelay + RestDelay) * 100) / 100.

% ============================================================================
% RESOURCE ASSIGNMENT (US 3.4.2 - AC2.b,c,d)
% ============================================================================
% Assigns physical resources (cranes), staff, and storage to scheduled operations
% Considers operational windows and resource availability
% ============================================================================

% Check if staff is available for the operation time window
% Parse "Mon-Fri 08:00-16:00" format and validate operation fits
staff_available(StaffMember, OperationStart, OperationEnd) :-
    get_dict(operationalWindow, StaffMember, WindowStr),
    StaffId = StaffMember.mecanographicNumber,
    
    % Extract time range from format like "Mon-Fri 08:00-16:00"
    % Find the time part after space (e.g., "08:00-16:00")
    sub_string(WindowStr, SpacePos, _, _, " "),
    !,  % Commit to first space found
    AfterSpace is SpacePos + 1,
    sub_string(WindowStr, AfterSpace, _, 0, TimeRange),
    
    % Split time range into start and end (format: "HH:MM-HH:MM")
    split_string(TimeRange, "-", " ", [StartTimeStr, EndTimeStr]),
    
    % Extract hours and minutes from "HH:MM" format
    split_string(StartTimeStr, ":", " ", [StartHourStr, StartMinStr]),
    split_string(EndTimeStr, ":", " ", [EndHourStr, EndMinStr]),
    
    number_string(AvailStartHour, StartHourStr),
    number_string(AvailStartMin, StartMinStr),
    number_string(AvailEndHour, EndHourStr),
    number_string(AvailEndMin, EndMinStr),
    
    % Convert to decimal hours
    AvailStart is AvailStartHour + (AvailStartMin / 60),
    AvailEnd is AvailEndHour + (AvailEndMin / 60),
    
    % Convert operation times to time of day for comparison
    OperationStartTimeOfDay is OperationStart - (floor(OperationStart / 24) * 24),
    OperationEndTimeOfDay is OperationEnd - (floor(OperationEnd / 24) * 24),
    
    format(user_error, 'DEBUG STAFF_AVAIL: ~w window=~w-~w, op=~w-~w~n',
           [StaffId, AvailStart, AvailEnd, OperationStartTimeOfDay, OperationEndTimeOfDay]),
    
    % Validate operation fits within staff's working hours
    OperationStartTimeOfDay >= AvailStart,
    OperationEndTimeOfDay =< AvailEnd.

% Check if staff has required qualification for crane
% Handles both string format "STS_CRANE_OPERATOR" and array format ["STS_CRANE_OPERATOR"]
staff_has_qualification(StaffMember, RequiredQualification) :-
    get_dict(qualifications, StaffMember, Qualifications),
    % Convert required qualification to string for comparison
    (   atom(RequiredQualification)
    ->  atom_string(RequiredQualification, ReqStr)
    ;   ReqStr = RequiredQualification
    ),
    % Check if qualifications is a list or a string
    (   is_list(Qualifications)
    ->  % Array format: check if any qualification matches
        member(Qual, Qualifications),
        (   atom(Qual)
        ->  atom_string(Qual, QualStr)
        ;   QualStr = Qual
        ),
        sub_string(QualStr, _, _, _, ReqStr)
    ;   % String format: direct substring match
        (   atom(Qualifications)
        ->  atom_string(Qualifications, QualStr)
        ;   QualStr = Qualifications
        ),
        sub_string(QualStr, _, _, _, ReqStr)
    ).

% ============================================================================
% TIME FORMATTING UTILITIES
% ============================================================================

% Convert decimal hours to HH:MM format (time of day only)
% Example: 1.333 -> "01:20", 32.5 -> "08:30" (32 mod 24 = 8)
hours_to_time_string(DecimalHours, TimeStr) :-
    % Calculate full days and remaining hours
    TotalHours is floor(DecimalHours),
    Days is TotalHours // 24,
    TimeOfDayHours is TotalHours - (Days * 24),
    
    % Calculate minutes from decimal part
    DecimalPart is DecimalHours - floor(DecimalHours),
    Minutes is round(DecimalPart * 60),
    
    format(atom(TimeStr), '~|~`0t~d~2+:~|~`0t~d~2+', [TimeOfDayHours, Minutes]).

% Assign resources to each operation in schedule
assign_resources_to_schedule(ScheduleTriples, AvailableResources, EnrichedSchedule) :-
    % Sort by start time to assign staff to earliest operations first
    sort_by_start_time(ScheduleTriples, SortedSchedule),
    assign_resources_internal(SortedSchedule, AvailableResources, [], EnrichedSchedule).

% Sort schedule entries by start time (earliest first)
sort_by_start_time(Schedule, SortedSchedule) :-
    map_list_to_pairs(get_start_time, Schedule, Pairs),
    keysort(Pairs, SortedPairs),
    pairs_values(SortedPairs, SortedSchedule).

get_start_time((_, StartTime, _, _, _, _, _), StartTime).

% Track assigned staff with their time windows
assign_resources_internal([], _, _, []).

assign_resources_internal([(VesselId, StartTime, EndTime, StartTimeStr, EndTimeStr, DurationStr, DockCode) | RestSchedule], 
                         AvailableResources,
                         AssignedStaff,
                         [Operation | RestOperations]) :-
    % Extract resources (dock already assigned from VVN)
    AvailableResources = json([cranes=Cranes, staff=Staff, storage=Storage, docks=_]),
    
    % AC2.b: Assign crane from the SAME dock as vessel (prefer lowest setup time)
    atom_string(DockCode, DockCodeStr),
    findall(
        SetupTime-Crane,
        (
            member(Crane, Cranes),
            get_dict(assignedArea, Crane, CraneAssignedArea),
            atom_string(CraneAssignedArea, CraneAreaStr),
            CraneAreaStr = DockCodeStr,
            get_dict(status, Crane, Status),
            atom_string(Status, "Active"),
            SetupTime = Crane.get(setupTimeMinutes, 9999)
        ),
        AvailableCranes
    ),
    (   AvailableCranes \= []
    ->  sort(AvailableCranes, SortedCranes),
        % Get first crane (best setup time)
        SortedCranes = [_MinSetup-BestCrane | _],
        CraneCode = BestCrane.code,
        CranesList = [BestCrane.code],
        RequiredQualifications = BestCrane.get(qualificationRequirementIds, [])
    ;   CraneCode = "NO_CRANE_AVAILABLE",
        CranesList = [],
        RequiredQualifications = []
    ),
    
    % AC2.d: Assign qualified staff within operational windows AND not already assigned
    format(user_error, 'DEBUG STAFF: Looking for staff for vessel ~w (Start=~w, End=~w)~n', [VesselId, StartTime, EndTime]),
    (   RequiredQualifications \= [],
        member(RequiredQual, RequiredQualifications),
        format(user_error, 'DEBUG STAFF: Required qual=~w~n', [RequiredQual]),
        member(StaffMember, Staff),
        StaffMemberId = StaffMember.mecanographicNumber,
        StaffStatus = StaffMember.get(status),
        format(user_error, 'DEBUG STAFF: Checking ~w (status=~w)~n', [StaffMemberId, StaffStatus]),
        get_dict(status, StaffMember, "Available"),
        (staff_available(StaffMember, StartTime, EndTime) -> format(user_error, 'DEBUG: ~w is available~n', [StaffMemberId]) ; (format(user_error, 'DEBUG: ~w NOT available~n', [StaffMemberId]), fail)),
        (staff_has_qualification(StaffMember, RequiredQual) -> format(user_error, 'DEBUG: ~w has qual~n', [StaffMemberId]) ; (format(user_error, 'DEBUG: ~w NO qual~n', [StaffMemberId]), fail)),
        \+ staff_is_busy(StaffMemberId, StartTime, EndTime, AssignedStaff)
    ->  StaffId = StaffMemberId,
        format(user_error, 'DEBUG STAFF: Assigned ~w to vessel ~w~n', [StaffId, VesselId]),
        NewAssignedStaff = [staff(StaffId, StartTime, EndTime) | AssignedStaff]
    ;   StaffId = "NO_STAFF_AVAILABLE",
        format(user_error, 'DEBUG STAFF: NO STAFF for vessel ~w~n', [VesselId]),
        NewAssignedStaff = AssignedStaff
    ),
    
    % AC2.c: Assign storage location - CLOSEST to dock
    find_closest_storage(Storage, DockCode, StorageId),
    
    % Calculate individual vessel delay
    user:vessel(VesselId, _, DesiredDeparture, _, _, _),
    VesselDelay is max(0, EndTime - DesiredDeparture),
    RoundedDelay is round(VesselDelay * 100) / 100,
    
    % Build operation details with formatted times
    Operation = _{
        vessel_id: VesselId,
        start_time: StartTimeStr,
        end_time: EndTimeStr,
        duration: DurationStr,
        start_time_decimal: StartTime,
        end_time_decimal: EndTime,
        assigned_dock: DockCode,
        assigned_crane: CraneCode,
        assigned_cranes: CranesList,
        assigned_staff: StaffId,
        assigned_storage: StorageId,
        delay: RoundedDelay
    },
    
    % Process remaining schedule with updated assigned staff
    assign_resources_internal(RestSchedule, AvailableResources, NewAssignedStaff, RestOperations).

% Check if staff member is already busy during the time window
staff_is_busy(StaffId, StartTime, EndTime, AssignedStaff) :-
    member(staff(StaffId, AssignedStart, AssignedEnd), AssignedStaff),
    % Check for time overlap: operations overlap if one starts before the other ends
    StartTime < AssignedEnd,
    EndTime > AssignedStart.

% Calculate delay only for operations with all resources available (status OK)
% Note: This is called AFTER user:vessel facts are cleaned, so we recalculate from schedule
calculate_valid_delay(EnrichedSchedule, ValidDelay) :-
    calculate_valid_delay_internal(EnrichedSchedule, ValidDelay).

calculate_valid_delay_internal([], 0).
calculate_valid_delay_internal([Op | Rest], TotalDelay) :-
    % Only count delay if all resources are available
    get_dict(assigned_crane, Op, Crane),
    get_dict(assigned_staff, Op, Staff),
    get_dict(assigned_storage, Op, Storage),
    Crane \= "NO_CRANE_AVAILABLE",
    Staff \= "NO_STAFF_AVAILABLE",
    Storage \= "NO_STORAGE_AVAILABLE",
    !,
    % This operation is valid - count its delay
    get_dict(end_time_decimal, Op, EndTime),
    % Find corresponding vessel data from user:vessel facts
    get_dict(vessel_id, Op, VesselId),
    (   user:vessel(VesselId, _, DesiredDeparture, _, _, _)
    ->  Delay is max(0, EndTime - DesiredDeparture)
    ;   Delay = 0  % Fallback if vessel data not found
    ),
    format(user_error, 'DEBUG VALID_DELAY: Vessel=~w, EndTime=~w, DesiredDeparture=~w, Delay=~w~n',
           [VesselId, EndTime, DesiredDeparture, Delay]),
    calculate_valid_delay_internal(Rest, RestDelay),
    TotalDelay is Delay + RestDelay.

calculate_valid_delay_internal([Op | Rest], TotalDelay) :-
    % This operation has missing resources - skip it
    get_dict(vessel_id, Op, VesselId),
    format(user_error, 'DEBUG VALID_DELAY: Vessel=~w SKIPPED (missing resources)~n', [VesselId]),
    calculate_valid_delay_internal(Rest, TotalDelay).

% Find closest storage area to dock
find_closest_storage(Storage, DockCode, StorageId) :-
    % Find all storage areas that serve this dock with their distances
    findall(
        Distance-StorageArea,
        (
            member(StorageArea, Storage),
            get_dict(servedDocks, StorageArea, ServedDocks),
            member(ServedDock, ServedDocks),
            get_dict(dockCode, ServedDock, ServedDockCode),
            atom_string(ServedDockCode, ServedDockCodeStr),
            atom_string(DockCode, DockCodeStr),
            ServedDockCodeStr = DockCodeStr,
            Distance = ServedDock.get(distanceMeters, 9999)
        ),
        StorageOptions
    ),
    % Sort by distance (closest first)
    (   StorageOptions \= []
    ->  sort(StorageOptions, [_MinDistance-ClosestStorage | _]),
        StorageId = ClosestStorage.identifier
    ;   % Fallback: first available storage
        (   Storage = [StorageArea | _]
        ->  StorageId = StorageArea.identifier
        ;   StorageId = "NO_STORAGE_AVAILABLE"
        )
    ).

% ============================================================================
% UTILITY FUNCTIONS
% ============================================================================

% Load vessel data as Prolog facts for scheduling algorithms
load_vessels_as_facts([]).

load_vessels_as_facts([Vessel | RestVessels]) :-
    % Extract vessel data from JSON
    VesselId = Vessel.id,
    ArrivalTime = Vessel.arrival_time,
    DepartureTime = Vessel.departure_time,
    UnloadTime = Vessel.unload_time,
    LoadTime = Vessel.load_time,
    AssignedDock = Vessel.get(assigned_dock, "DOCK-A"),
    
    % Assert as fact
    assertz(user:vessel(VesselId, ArrivalTime, DepartureTime, UnloadTime, LoadTime, AssignedDock)),
    
    % Process remaining vessels
    load_vessels_as_facts(RestVessels).

    % ============================================================================
% MULTI-CRANE SCHEDULING (US 3.4.5)
% ============================================================================
% Objetivo:
%   - Partir do plano ótimo com 1 grua (US 3.4.2).
%   - Se ainda houver atraso e houver mais de 2 navios, tentar reduzir o atraso
%     usando 2 gruas em alguns navios (multi-crane).
%   - Minimizar também o nº de vezes que se usa 2 gruas.
%
% Interface:
%   compute_multi_crane_schedule(+Vessels, -BaselineResult, -MultiResult)
%
%   Vessels: lista de dicts já no formato de agendamento (o mesmo passado a
%            compute_optimal_schedule/2).
%
%   BaselineResult: resultado tal como devolvido por compute_optimal_schedule/2
%                   (1 grua).
%
%   MultiResult: dict com:
%     - algorithm: "multi_crane"
%     - strategy:  "single_crane" | "multi_crane"
%     - total_delay: atraso total depois da otimização
%     - computation_time_ms: tempo de cálculo do multi-crane
%     - schedule: lista de 7-tuplos (mesma estrutura dos outros algoritmos)
%     - cranes_allocation: lista com o nº de gruas por operação (achplan)
%     - crane_hours_single: métrica de crane-hours (1 grua)
%     - crane_hours_multi:  métrica de crane-hours (multi-gruas)
% ============================================================================

compute_multi_crane_schedule(Vessels, BaselineResult, MultiResult) :-
    % 1) Usar o plano ótimo já calculado
    get_time(StartTime),
    
    % Extrair campos do BaselineResult
    BaselineDelay = BaselineResult.total_delay,
    BaselineSchedule = BaselineResult.schedule,

    % 2) Construir sequências por dock com tripletos [V,Start,End]
    build_dock_sequences(BaselineSchedule, DockTripletSeqs),

    % 3) Converter para quadrupletos com 1 grua e calcular crane-hours
    convert_all_triplets_to_quads(DockTripletSeqs, DockQuadSeqsSingle),
    crane_hours_multi_from_docks(DockQuadSeqsSingle, CraneHoursSingle),

    length(Vessels, NVessels),

    % 4) Carregar navios como facts (necessário para multi-crane)
    retractall(user:vessel(_,_,_,_,_,_)),
    retractall(shortest_delay_multi(_,_,_)),
    load_vessels_as_facts(Vessels),

    % 5) Decidir se vale a pena ativar multi-gruas
    (   (BaselineDelay =:= 0 ; NVessels =< 2)
    ->  % Caso simples: não vale a pena mexer, mantemos 1 grua para todos
        Strategy         = single_crane,
        MultiDelay       = BaselineDelay,
        CraneHoursMulti  = CraneHoursSingle,
        OptimizedDockQuads = DockQuadSeqsSingle,
        extract_all_cranes_alloc(OptimizedDockQuads, LCranesAlloc),
        build_multi_crane_schedule_triples(OptimizedDockQuads, MultiScheduleTriples)
    ;   % Há atraso e mais de 2 navios → otimizar com multi-gruas
        % Otimizar dock a dock usando multi-gruas
        optimize_all_docks(DockTripletSeqs, OptimizedDockQuads, MultiDelay, CraneHoursMulti),

        % Extrair alocação de gruas e reconstruir schedule no formato 7-tuplos
        extract_all_cranes_alloc(OptimizedDockQuads, LCranesAlloc),
        build_multi_crane_schedule_triples(OptimizedDockQuads, MultiScheduleTriples),

        Strategy = multi_crane
    ),

    % Limpar apenas shortest_delay_multi (manter user:vessel para assign_resources)
    retractall(shortest_delay_multi(_,_,_)),

    get_time(EndTime),
    ComputationTime is (EndTime - StartTime) * 1000,

    MultiResult = _{
        algorithm:          "multi_crane",
        strategy:           Strategy,
        total_delay:        MultiDelay,
        computation_time_ms:ComputationTime,
        schedule:           MultiScheduleTriples,
        cranes_allocation:  LCranesAlloc,
        crane_hours_single: CraneHoursSingle,
        crane_hours_multi:  CraneHoursMulti
    }.

% ---------------------------------------------------------------
% Agrupar o schedule base por dock
% ScheduleTriples: (V, Start, End, StartStr, EndStr, DurStr, Dock)
% DockTripletSeqs: [dock(DockCode, [[V,Start,End], ...]) | ...]
% ---------------------------------------------------------------

build_dock_sequences(ScheduleTriples, DockTripletSeqs) :-
    findall(Dock,
        member((_, _, _, _, _, _, Dock), ScheduleTriples),
        AllDocks),
    sort(AllDocks, UniqueDocks),
    build_dock_sequences_for_list(UniqueDocks, ScheduleTriples, DockTripletSeqs).

build_dock_sequences_for_list([], _, []).
build_dock_sequences_for_list([Dock | Rest], ScheduleTriples,
                              [dock(Dock, Triplets) | RestDockSeqs]) :-
    findall([V, Start, End],
        member((V, Start, End, _, _, _, Dock), ScheduleTriples),
        Triplets),
    build_dock_sequences_for_list(Rest, ScheduleTriples, RestDockSeqs).

% Converter tripletos [[V,TI,TF], ...] -> quadrupletos [[V,TI,TF,N]|...]
convert_all_triplets_to_quads([], []).
convert_all_triplets_to_quads([dock(Dock, Triplets) | Rest],
                              [dock(Dock, Quads) | RestQuads]) :-
    convert_triplets_to_quadruplets(Triplets, 1, Quads),
    convert_all_triplets_to_quads(Rest, RestQuads).

% Flatten da alocação de gruas por dock
extract_all_cranes_alloc([], []).
extract_all_cranes_alloc([dock(_DockCode, Quads) | Rest], AllCr) :-
    extract_cranes_list(Quads, CrDock),
    extract_all_cranes_alloc(Rest, CrRest),
    append(CrDock, CrRest, AllCr).

% ---------------------------------------------------------------
% Otimizar todas as docks com o algoritmo multi-gruas
% ---------------------------------------------------------------

optimize_all_docks([], [], 0, 0).

optimize_all_docks([dock(Dock, Triplets) | Rest],
                   [dock(Dock, BestQuads) | RestBest],
                   TotalDelay, TotalCraneHours) :-
    % Atraso só com 1 grua (para esta dock)
    sum_delays_multi_triplets(Triplets, SingleDelayDock),

    (   SingleDelayDock =:= 0
    ->  % Sem atraso → mantém 1 grua
        convert_triplets_to_quadruplets(Triplets, 1, BestQuads),
        BestDelayDock is SingleDelayDock
    ;   % Há atraso → tentar multi-gruas
        multi_crane_optimize(Triplets, SingleDelayDock,
                             BestQuads, BestDelayDock, _LCranesAllocDock)
    ),

    crane_hours_multi(BestQuads, CraneHoursDock),
    optimize_all_docks(Rest, RestBest, DelayRest, CraneHoursRest),
    TotalDelay      is BestDelayDock  + DelayRest,
    TotalCraneHours is CraneHoursDock + CraneHoursRest.

% Atraso somado a partir de tripletos [[V, _, TEnd]|...]
sum_delays_multi_triplets([], 0).
sum_delays_multi_triplets([[V, _, TEndLoad] | Rest], TotalDelay) :-
    user_vessel_data_safe(V, _, DesiredDeparture, _, _),
    VesselDelay is max(0, TEndLoad - DesiredDeparture),
    sum_delays_multi_triplets(Rest, RestDelay),
    TotalDelay is VesselDelay + RestDelay.

% ---------------------------------------------------------------
% Núcleo da otimização multi-gruas (por dock)
% Triplets: [[V, TInUnload, TEndLoad] | ...]
% ---------------------------------------------------------------

multi_crane_optimize(SeqTriplets, SDelay1Crane,
                     SeqBetterQuadruplets, SShortestDelay, LCranesAlloc) :-
    SInitial is SDelay1Crane,
    convert_triplets_to_quadruplets(SeqTriplets, 1, QuadInitial),
    extract_cranes_list(QuadInitial, LCranesInitial),
    retractall(shortest_delay_multi(_,_,_)),
    asserta(shortest_delay_multi(QuadInitial, SInitial, LCranesInitial)),

    reverse(SeqTriplets, RevSeq),
    find_last_delayed_vessel(RevSeq, VLastDelayed),

    extract_vessels_list(SeqTriplets, SeqV),
    split_sequence_at_vessel(SeqV, VLastDelayed, SeqToOptimize, SeqFixed),

    length(SeqToOptimize, NToOptimize),

    (   generate_crane_permutations(NToOptimize, LCrOptimizable),
        length(SeqFixed, NFixed),
        generate_ones(NFixed, FixedCranes),
        append(LCrOptimizable, FixedCranes, LCrFull),
        append(SeqToOptimize, SeqFixed, SeqVFull),

        sequence_temporization_multi(SeqVFull, LCrFull, SeqQuadruplets),
        sum_delays_multi(SeqQuadruplets, S),
        compare_shortest_delay_multi(SeqQuadruplets, S, LCrFull),
        fail
    ;   true
    ),

    retract(shortest_delay_multi(SeqBetterQuadruplets, SShortestDelay, LCranesAlloc)).

% ---------------------------------------------------------------
% Temporização com N gruas (1 dock)
% ---------------------------------------------------------------

sequence_temporization_multi(LV, LCranes, SeqQuadruplets) :-
    sequence_temporization_multi1(0, LV, LCranes, SeqQuadruplets).

sequence_temporization_multi1(_, [], [], []).
sequence_temporization_multi1(EndPrevOp,
                              [V | LV],
                              [NCranes | LCranes],
                              [[V, TInUnload, TEndLoad, NCranes] | SeqQuadruplets]) :-
    user_vessel_data_safe(V, ArrivalTime, _, UnloadTime, LoadTime),
    StartUnload is max(ArrivalTime, EndPrevOp),
    TotalOperation is (UnloadTime + LoadTime) / NCranes,
    TInUnload is StartUnload,
    TEndLoad is StartUnload + TotalOperation,
    sequence_temporization_multi1(TEndLoad, LV, LCranes, SeqQuadruplets).

% Soma dos atrasos na versão multi-gruas (quadrupletos)
sum_delays_multi([], 0).
sum_delays_multi([[V, _, TEndLoad, _] | LV], S) :-
    user_vessel_data_safe(V, _, TDep, _, _),
    VesselDelay is max(0, TEndLoad - TDep),
    sum_delays_multi(LV, SLV),
    S is VesselDelay + SLV.

% ---------------------------------------------------------------
% Acesso seguro aos dados de um navio a partir de user:vessel/6
% (versão "user_" para não colidir com o que já tens noutros módulos)
% ---------------------------------------------------------------

user_vessel_data_safe(V, Arrival, Departure, Unload, Load) :-
    user:vessel(V, ArrivalRaw, DepartureRaw, UnloadRaw, LoadRaw, _Dock),
    ensure_number(ArrivalRaw,   Arrival),
    ensure_number(DepartureRaw, Departure),
    ensure_number(UnloadRaw,    Unload),
    ensure_number(LoadRaw,      Load).

ensure_number(X, N) :-
    number(X), !, N is X.
ensure_number(X, N) :-
    atom(X),   !, atom_number(X, N).
ensure_number(X, N) :-
    string(X), !, number_string(N, X).

% ---------------------------------------------------------------
% Utilitários de manipulação de listas de navios / gruas
% (alguns já existem no teu exemplo; garante que não duplicas nomes)
% ---------------------------------------------------------------

convert_triplets_to_quadruplets([], _, []).
convert_triplets_to_quadruplets([[V, TIU, TEL] | T], N,
                                [[V, TIU, TEL, N] | Q]) :-
    convert_triplets_to_quadruplets(T, N, Q).

extract_cranes_list([], []).
extract_cranes_list([[_V, _TIU, _TEL, NCranes] | T], [NCranes | L]) :-
    extract_cranes_list(T, L).

extract_vessels_list([], []).
extract_vessels_list([[V, _, _] | T], [V | L]) :-
    extract_vessels_list(T, L).

find_last_delayed_vessel([[V, _, TEndLoad] | T], VLastDelayed) :-
    user_vessel_data_safe(V, _, TDep, _, _),
    VesselDelay is max(0, TEndLoad - TDep),
    (   VesselDelay > 0
    ->  VLastDelayed = V
    ;   find_last_delayed_vessel(T, VLastDelayed)
    ).
find_last_delayed_vessel([], none) :- !.

split_sequence_at_vessel([V | T], V, [V], T) :- !.
split_sequence_at_vessel([H | T], V, [H | L1], L2) :-
    H \= V,
    split_sequence_at_vessel(T, V, L1, L2).
split_sequence_at_vessel([], _, [], []) :- !.

generate_crane_permutations(0, []) :- !.
generate_crane_permutations(N, [C | T]) :-
    N > 0,
    member(C, [1, 2]),      % 1 ou 2 gruas por navio
    N1 is N - 1,
    generate_crane_permutations(N1, T).

generate_ones(0, []) :- !.
generate_ones(N, [1 | T]) :-
    N > 0,
    N1 is N - 1,
    generate_ones(N1, T).

compare_shortest_delay_multi(SeqQuadruplets, S, LCrFull) :-
    shortest_delay_multi(_, SCurrent, LCrCurrent),
    count_cranes(LCrFull,    2, C2New),
    count_cranes(LCrCurrent, 2, C2Current),
    (   S < SCurrent
    ->  retract(shortest_delay_multi(_,_,_)),
        asserta(shortest_delay_multi(SeqQuadruplets, S, LCrFull))
    ;   S =:= SCurrent,
        C2New < C2Current
    ->  retract(shortest_delay_multi(_,_,_)),
        asserta(shortest_delay_multi(SeqQuadruplets, S, LCrFull))
    ;   true
    ).

count_cranes([], _, 0).
count_cranes([H | T], N, C) :-
    count_cranes(T, N, CT),
    (   H =:= N
    ->  C is CT + 1
    ;   C is CT
    ).

% ---------------------------------------------------------------
% Cálculo de crane-hours
% ---------------------------------------------------------------

crane_hours_multi([], 0).
crane_hours_multi([[_, Start, End, NCranes] | T], Total) :-
    Duration is End - Start,
    crane_hours_multi(T, Rest),
    Total is Rest + Duration * NCranes.

crane_hours_multi_from_docks([], 0).
crane_hours_multi_from_docks([dock(_, Quads) | Rest], Total) :-
    crane_hours_multi(Quads, CHDock),
    crane_hours_multi_from_docks(Rest, CHRest),
    Total is CHDock + CHRest.

% ---------------------------------------------------------------
% Reconstruir schedule (7-tuplos) a partir de quadrupletos por dock
% ---------------------------------------------------------------

build_multi_crane_schedule_triples([], []).
build_multi_crane_schedule_triples([dock(DockCode, Quads) | RestDocks],
                                   AllTriples) :-
    build_triples_for_dock(DockCode, Quads, TriplesDock),
    build_multi_crane_schedule_triples(RestDocks, RestTriples),
    append(TriplesDock, RestTriples, AllTriples).

build_triples_for_dock(_, [], []).
build_triples_for_dock(DockCode,
                       [[V, Start, End, _NCranes] | RestQuads],
                       [(V, Start, End, StartStr, EndStr, DurationStr, DockCode)
                        | RestTriples]) :-
    hours_to_time_string(Start, StartStr),
    hours_to_time_string(End,   EndStr),
    Duration is End - Start,
    hours_to_time_string(Duration, DurationStr),
    build_triples_for_dock(DockCode, RestQuads, RestTriples).


% ============================================================================
% GENETIC ALGORITHM SCHEDULING (US 4.3.1)
% ============================================================================
% Uses genetic algorithm approach to explore broader solution spaces
% and achieve improved or near-optimal schedules under complex conditions.
%
% Features:
%   - Configurable parameters (population size, generations, mutation rate, etc.)
%   - Multiple stopping conditions (generations, time limit, target delay, stability)
%   - Elitism: best individuals always pass to next generation
%   - Random permutation before crossover to avoid fixed pairings
%   - Non-purely elitist selection for diversity
%   - Support for single-crane and multi-crane solutions
%
% Author: LEI-ISEP Team
% Date: December 2025
% ============================================================================

% ---------------------------------------------------------------
% Main entry point for genetic algorithm scheduling
% Parameters dict contains:
%   - population_size: number of individuals (default: 20)
%   - generations: max number of generations (default: 50)
%   - crossover_rate: probability of crossover 0-100 (default: 70)
%   - mutation_rate: probability of mutation 0-100 (default: 20)
%   - time_limit_ms: max execution time in ms (default: 5000)
%   - target_delay: stop if delay reaches this value (default: 0)
%   - stability_generations: stop if stable for N generations (default: 10)
%   - use_multi_crane: boolean for multi-crane mode (default: false)
% ---------------------------------------------------------------

compute_genetic_schedule(Vessels, Parameters, Result) :-
    % Validate input
    (   is_list(Vessels), Vessels \= []
    ->  true
    ;   Result = _{
            algorithm: "genetic",
            error: "Invalid or empty vessel list",
            total_delay: 0,
            schedule: []
        },
        !
    ),
    
    % Clean previous state
    ga_cleanup,
    retractall(user:vessel(_, _, _, _, _, _)),
    
    % Initialize parameters from dict or use defaults
    ga_init_parameters(Parameters),
    
    % Load vessels as facts
    load_vessels_as_facts(Vessels),
    
    % Get vessel IDs grouped by dock
    findall(Dock-VesselId,
        (member(V, Vessels), 
         VesselId = V.id,
         AssignedDock = V.get(assigned_dock, "DOCK-A"),
         atom_string(Dock, AssignedDock)),
        DockVesselPairs),
    group_pairs_by_key(DockVesselPairs, DockGroups),
    
    % Run genetic algorithm
    get_time(StartTime),
    ga_time_limit(TimeLimit),
    TimeLimitSec is TimeLimit / 1000,
    Deadline is StartTime + TimeLimitSec,
    
    % Run GA for each dock independently
    ga_run_all_docks(DockGroups, Deadline, AllDockResults),
    
    get_time(EndTime),
    ComputationTime is (EndTime - StartTime) * 1000,
    
    % Combine results from all docks
    ga_combine_dock_results(AllDockResults, CombinedSchedule, TotalDelay, CranesAllocation),
    
    % Debug output
    length(CombinedSchedule, NumOps),
    format(user_error, '[GA] Combined schedule: ~w operations~n', [NumOps]),
    format(user_error, '[GA] Total delay: ~2f~n', [TotalDelay]),
    
    % Get parameters for response
    ga_generations(MaxGen),
    ga_population_size(PopSize),
    ga_prob_crossover(PCross),
    ga_prob_mutation(PMut),
    ga_use_multi_crane(UseMultiCrane),
    
    % Build result
    RoundedDelay is round(TotalDelay * 100) / 100,
    (UseMultiCrane = true -> Strategy = multi_crane ; Strategy = single_crane),
    
    % Calculate baseline delay: if multi-crane was used, run GA again with single crane
    (   UseMultiCrane = true
    ->  format(user_error, '[GA] Calculating baseline: running GA with single crane~n', []),
        % Temporarily disable multi-crane
        retract(ga_use_multi_crane(true)),
        asserta(ga_use_multi_crane(false)),
        
        % Run GA again with single crane to get proper baseline
        get_time(BaselineStartTime),
        ga_run_all_docks(DockGroups, Deadline, BaselineDockResults),
        get_time(BaselineEndTime),
        BaselineCompTime is (BaselineEndTime - BaselineStartTime) * 1000,
        
        % Combine baseline results
        ga_combine_dock_results(BaselineDockResults, _BaselineSchedule, BaselineDelayRaw, _BaselineAlloc),
        BaselineDelay is round(BaselineDelayRaw * 100) / 100,
        
        format(user_error, '[GA] Baseline calculated: ~2f hours (~2f ms)~n', [BaselineDelay, BaselineCompTime]),
        
        % Restore multi-crane setting
        retract(ga_use_multi_crane(false)),
        asserta(ga_use_multi_crane(true))
    ;   BaselineDelay = RoundedDelay
    ),
    
    Result = _{
        algorithm: "genetic",
        strategy: Strategy,
        total_delay: RoundedDelay,
        baseline_delay: BaselineDelay,
        computation_time_ms: ComputationTime,
        schedule: CombinedSchedule,
        cranes_allocation: CranesAllocation,
        parameters: _{
            population_size: PopSize,
            max_generations: MaxGen,
            crossover_rate: PCross,
            mutation_rate: PMut,
            use_multi_crane: UseMultiCrane
        }
    }.

% ---------------------------------------------------------------
% Initialize GA parameters from dict
% ---------------------------------------------------------------

ga_init_parameters(Parameters) :-
    % Population size (default: 10 - REDUCED)
    (get_dict(population_size, Parameters, PopSize) -> true ; PopSize = 10),
    retractall(ga_population_size(_)),
    assertz(ga_population_size(PopSize)),
    format(user_error, '[GA] Population: ~w~n', [PopSize]),
    
    % Max generations (default: 20 - REDUCED)
    (get_dict(generations, Parameters, Generations) -> true ; Generations = 20),
    retractall(ga_generations(_)),
    assertz(ga_generations(Generations)),
    format(user_error, '[GA] Max generations: ~w~n', [Generations]),
    
    % Crossover rate (default: 70%)
    (get_dict(crossover_rate, Parameters, CrossRate) -> true ; CrossRate = 70),
    PCross is CrossRate / 100,
    retractall(ga_prob_crossover(_)),
    assertz(ga_prob_crossover(PCross)),
    
    % Mutation rate (default: 20%)
    (get_dict(mutation_rate, Parameters, MutRate) -> true ; MutRate = 20),
    PMut is MutRate / 100,
    retractall(ga_prob_mutation(_)),
    assertz(ga_prob_mutation(PMut)),
    
    % Time limit in ms (default: 3000ms - REDUCED)
    (get_dict(time_limit_ms, Parameters, TimeLimit) -> true ; TimeLimit = 3000),
    retractall(ga_time_limit(_)),
    assertz(ga_time_limit(TimeLimit)),
    format(user_error, '[GA] Time limit: ~w ms~n', [TimeLimit]),
    
    % Target delay (default: 0 - optimal)
    (get_dict(target_delay, Parameters, TargetDelay) -> true ; TargetDelay = 0),
    retractall(ga_target_delay(_)),
    assertz(ga_target_delay(TargetDelay)),
    
    % Stability generations (default: 5 - REDUCED)
    (get_dict(stability_generations, Parameters, StabilityGen) -> true ; StabilityGen = 5),
    retractall(ga_stability_generations(_)),
    assertz(ga_stability_generations(StabilityGen)),
    
    % Multi-crane mode (default: false)
    (get_dict(use_multi_crane, Parameters, UseMultiCrane) -> true ; UseMultiCrane = false),
    retractall(ga_use_multi_crane(_)),
    assertz(ga_use_multi_crane(UseMultiCrane)).

% ---------------------------------------------------------------
% Cleanup GA state
% ---------------------------------------------------------------

ga_cleanup :-
    retractall(ga_generations(_)),
    retractall(ga_population_size(_)),
    retractall(ga_prob_crossover(_)),
    retractall(ga_prob_mutation(_)),
    retractall(ga_time_limit(_)),
    retractall(ga_target_delay(_)),
    retractall(ga_stability_generations(_)),
    retractall(ga_best_solution(_, _)),
    retractall(ga_use_multi_crane(_)).

% ---------------------------------------------------------------
% Run GA for all docks
% ---------------------------------------------------------------

ga_run_all_docks([], _, []).
ga_run_all_docks([Dock-VesselIds | RestDocks], Deadline, [DockResult | RestResults]) :-
    length(VesselIds, NumVessels),
    format(user_error, '[GA] Processing dock ~w with ~w vessels: ~w~n', [Dock, NumVessels, VesselIds]),
    (   NumVessels > 0
    ->  ga_run_for_dock(Dock, VesselIds, Deadline, DockResult),
        DockResult = dock_result(_, DockSchedule, DockDelay, _DockAlloc),
        length(DockSchedule, NDockOps),
        format(user_error, '[GA] Dock ~w result: ~w ops, delay=~2f~n', [Dock, NDockOps, DockDelay])
    ;   DockResult = dock_result(Dock, [], 0, []),
        format(user_error, '[GA] Dock ~w: empty (no vessels)~n', [Dock])
    ),
    ga_run_all_docks(RestDocks, Deadline, RestResults).

% ---------------------------------------------------------------
% Run GA for a single dock
% ---------------------------------------------------------------

ga_run_for_dock(Dock, VesselIds, Deadline, dock_result(Dock, BestSchedule, BestDelay, CranesAlloc)) :-
    length(VesselIds, NumVessels),
    format(user_error, '[GA] Dock ~w: ~w vessels~n', [Dock, NumVessels]),
    
    % Generate initial population
    ga_population_size(PopSize),
    format(user_error, '[GA] Generating ~w individuals...~n', [PopSize]),
    ga_generate_population(PopSize, VesselIds, NumVessels, InitialPop),
    format(user_error, '[GA] Done generating~n', []),
    
    % Evaluate initial population
    format(user_error, '[GA] Evaluating...~n', []),
    ga_evaluate_population(InitialPop, Dock, EvaluatedPop),
    
    % Sort by fitness (ascending delay)
    ga_sort_population(EvaluatedPop, SortedPop),
    SortedPop = [_*InitBest | _],
    format(user_error, '[GA] Initial best: ~2f~n', [InitBest]),
    
    % Run generations
    ga_generations(MaxGen),
    format(user_error, '[GA] Evolving (~w gen max)...~n', [MaxGen]),
    ga_run_generations(0, MaxGen, SortedPop, Dock, NumVessels, Deadline, 0, FinalPop),
    
    % Extract best solution
    FinalPop = [BestInd*BestDelay | _],
    format(user_error, '[GA] Final best: ~2f~n', [BestDelay]),
    
    % Convert to schedule format with crane allocation
    ga_use_multi_crane(UseMultiCrane),
    format(user_error, '[GA] Converting to schedule (multi_crane=~w)...~n', [UseMultiCrane]),
    ga_convert_to_schedule(BestInd, Dock, UseMultiCrane, BestSchedule, CranesAlloc),
    length(BestSchedule, NScheds),
    format(user_error, '[GA] Converted: ~w schedule entries~n', [NScheds]).

% ---------------------------------------------------------------
% Generate initial population of random permutations
% ---------------------------------------------------------------

ga_generate_population(PopSize, VesselIds, _, Pop) :-
    ga_gen_pop_loop(PopSize, VesselIds, [], Pop).

ga_gen_pop_loop(0, _, Acc, Acc) :- !.
ga_gen_pop_loop(N, VesselIds, Acc, Pop) :-
    N > 0,
    random_permutation(VesselIds, Ind),
    N1 is N - 1,
    ga_gen_pop_loop(N1, VesselIds, [Ind | Acc], Pop).

% ---------------------------------------------------------------
% Evaluate population fitness
% ---------------------------------------------------------------

ga_evaluate_population([], _, []).
ga_evaluate_population([Ind | Rest], Dock, [Ind*Delay | RestEval]) :-
    ga_evaluate_individual(Ind, Dock, Delay),
    ga_evaluate_population(Rest, Dock, RestEval).

ga_evaluate_individual(VesselSequence, Dock, TotalDelay) :-
    ga_use_multi_crane(UseMultiCrane),
    ga_evaluate_with_mode(VesselSequence, Dock, UseMultiCrane, TotalDelay).

ga_evaluate_with_mode(VesselSequence, _Dock, false, TotalDelay) :-
    % Single crane mode - simple sequential evaluation
    ga_sequence_temporization(VesselSequence, 0, ScheduleTriples),
    ga_sum_delays(ScheduleTriples, TotalDelay).

ga_evaluate_with_mode(VesselSequence, _Dock, true, TotalDelay) :-
    % Multi-crane mode - try to find optimal crane allocation
    ga_sequence_temporization(VesselSequence, 0, SingleCraneSchedule),
    ga_sum_delays(SingleCraneSchedule, SingleDelay),
    
    % If there's delay, try with 2 cranes for some vessels
    (   SingleDelay > 0
    ->  ga_optimize_cranes(VesselSequence, TotalDelay)
    ;   TotalDelay = SingleDelay
    ).

% Temporization for GA (single crane per vessel)
ga_sequence_temporization([], _, []).
ga_sequence_temporization([VesselId | Rest], EndPrevious, [(VesselId, Start, End) | RestSchedule]) :-
    user:vessel(VesselId, ArrivalTime, _, UnloadTime, LoadTime, _),
    Start is max(ArrivalTime, EndPrevious),
    TotalOp is UnloadTime + LoadTime,
    End is Start + TotalOp,
    ga_sequence_temporization(Rest, End, RestSchedule).

% Sum delays for GA schedule
ga_sum_delays([], 0).
ga_sum_delays([(VesselId, _, EndTime) | Rest], TotalDelay) :-
    user:vessel(VesselId, _, DesiredDeparture, _, _, _),
    VesselDelay is max(0, EndTime - DesiredDeparture),
    ga_sum_delays(Rest, RestDelay),
    TotalDelay is VesselDelay + RestDelay.

% Optimize crane allocation for multi-crane mode
ga_optimize_cranes(VesselSequence, BestDelay) :-
    length(VesselSequence, N),
    ga_try_crane_combinations(VesselSequence, N, 1000000, BestDelay).

ga_try_crane_combinations(VesselSequence, N, CurrentBest, BestDelay) :-
    % Generate a random crane allocation (1 or 2 cranes per vessel)
    ga_generate_crane_allocation(N, CraneAlloc),
    ga_evaluate_with_cranes(VesselSequence, CraneAlloc, Delay),
    (   Delay < CurrentBest
    ->  NewBest = Delay
    ;   NewBest = CurrentBest
    ),
    % Try a few combinations (limited for performance)
    random(0, 10, R),
    (   R < 8  % 80% chance to continue trying
    ->  ga_try_crane_combinations(VesselSequence, N, NewBest, BestDelay)
    ;   BestDelay = NewBest
    ).
ga_try_crane_combinations(_, _, CurrentBest, CurrentBest).

ga_generate_crane_allocation(0, []) :- !.
ga_generate_crane_allocation(N, [NCranes | Rest]) :-
    N > 0,
    random(1, 3, NCranes),  % 1 or 2 cranes
    N1 is N - 1,
    ga_generate_crane_allocation(N1, Rest).

ga_evaluate_with_cranes([], [], 0).
ga_evaluate_with_cranes([VesselId | RestV], [NCranes | RestC], TotalDelay) :-
    user:vessel(VesselId, _, DesiredDeparture, UnloadTime, LoadTime, _),
    TotalOp is (UnloadTime + LoadTime) / NCranes,
    % This is simplified - real implementation should track actual times
    user:vessel(VesselId, ArrivalTime, _, _, _, _),
    EndTime is ArrivalTime + TotalOp,
    VesselDelay is max(0, EndTime - DesiredDeparture),
    ga_evaluate_with_cranes(RestV, RestC, RestDelay),
    TotalDelay is VesselDelay + RestDelay.

% ---------------------------------------------------------------
% Sort population by fitness (ascending)
% ---------------------------------------------------------------

ga_sort_population(Pop, Sorted) :-
    ga_bsort(Pop, Sorted).

ga_bsort([X], [X]) :- !.
ga_bsort([X | Xs], Ys) :-
    ga_bsort(Xs, Zs),
    ga_bchange([X | Zs], Ys).

ga_bchange([X], [X]) :- !.
ga_bchange([X*VX, Y*VY | L1], [Y*VY | L2]) :-
    VX > VY, !,
    ga_bchange([X*VX | L1], L2).
ga_bchange([X | L1], [X | L2]) :-
    ga_bchange(L1, L2).

% ---------------------------------------------------------------
% Run genetic algorithm generations
% ---------------------------------------------------------------

ga_run_generations(Gen, MaxGen, Pop, _, _, _, _, Pop) :-
    Gen >= MaxGen, !,
    format(user_error, '[GA] STOP: Max gen~n', []).

ga_run_generations(Gen, _, Pop, _, _, Deadline, _, Pop) :-
    get_time(Now),
    Now >= Deadline, !,
    format(user_error, '[GA] STOP: Timeout at gen ~w~n', [Gen]).

ga_run_generations(Gen, _, Pop, _, _, _, _, Pop) :-
    Pop = [_*Delay | _],
    ga_target_delay(Target),
    Delay =< Target, !,
    format(user_error, '[GA] STOP: Target at gen ~w~n', [Gen]).

ga_run_generations(_, _, Pop, _, _, _, StableCount, Pop) :-
    ga_stability_generations(MaxStable),
    StableCount >= MaxStable, !,
    format(user_error, '[GA] STOP: Stable ~w gen~n', [StableCount]).

ga_run_generations(Gen, MaxGen, Pop, Dock, NumVessels, Deadline, StableCount, FinalPop) :-
    (Gen mod 5 =:= 0 -> format(user_error, '[GA] Gen ~w/~w~n', [Gen, MaxGen]) ; true),
    
    % Random permutation before crossover to avoid fixed pairings
    random_permutation(Pop, ShuffledPop),
    
    % Apply crossover
    ga_crossover(ShuffledPop, NumVessels, CrossedPop),
    
    % Apply mutation
    ga_mutation(CrossedPop, NumVessels, MutatedPop),
    
    % Evaluate new individuals
    ga_evaluate_population(MutatedPop, Dock, EvaluatedNew),
    
    % Combine old and new populations
    append(Pop, EvaluatedNew, CombinedPop),
    
    % Remove duplicates
    ga_remove_duplicates(CombinedPop, UniquePop),
    
    % Sort combined population
    ga_sort_population(UniquePop, SortedCombined),
    
    % Select survivors (elitism + non-elitist selection)
    ga_population_size(PopSize),
    ga_select_survivors(SortedCombined, PopSize, NewPop),
    
    % Check for stability
    Pop = [_*OldBest | _],
    NewPop = [_*NewBest | _],
    (   OldBest =:= NewBest
    ->  NewStableCount is StableCount + 1
    ;   NewStableCount = 0
    ),
    
    Gen1 is Gen + 1,
    ga_run_generations(Gen1, MaxGen, NewPop, Dock, NumVessels, Deadline, NewStableCount, FinalPop).

% ---------------------------------------------------------------
% Remove duplicate individuals
% ---------------------------------------------------------------

ga_remove_duplicates([], []).
ga_remove_duplicates([Ind*V | Rest], [Ind*V | UniqueRest]) :-
    ga_remove_ind_from_list(Ind, Rest, Filtered),
    ga_remove_duplicates(Filtered, UniqueRest).

ga_remove_ind_from_list(_, [], []).
ga_remove_ind_from_list(Ind, [Ind*_ | Rest], Filtered) :- !,
    ga_remove_ind_from_list(Ind, Rest, Filtered).
ga_remove_ind_from_list(Ind, [Other | Rest], [Other | Filtered]) :-
    ga_remove_ind_from_list(Ind, Rest, Filtered).

% ---------------------------------------------------------------
% Select survivors using elitism + probabilistic selection
% ---------------------------------------------------------------

ga_select_survivors(SortedPop, PopSize, Selected) :-
    length(SortedPop, TotalLen),
    (   TotalLen =< PopSize
    ->  Selected = SortedPop
    ;   % Keep top 20% as elite
        EliteCount is max(1, PopSize // 5),
        RemainingCount is PopSize - EliteCount,
        ga_take_n(EliteCount, SortedPop, Elite, RestPop),
        
        % Select remaining using fitness-proportional probability
        ga_probabilistic_select(RestPop, RemainingCount, Remaining),
        append(Elite, Remaining, Selected)
    ).

ga_take_n(0, L, [], L) :- !.
ga_take_n(_, [], [], []) :- !.
ga_take_n(N, [X | Rest], [X | Taken], Remaining) :-
    N > 0,
    N1 is N - 1,
    ga_take_n(N1, Rest, Taken, Remaining).

ga_probabilistic_select(_, 0, []) :- !.
ga_probabilistic_select([], _, []) :- !.
ga_probabilistic_select(Pop, N, Selected) :-
    % Assign random weight to each individual
    ga_assign_random_weights(Pop, WeightedPop),
    ga_sort_population(WeightedPop, SortedWeighted),
    ga_take_n(N, SortedWeighted, SelectedWeighted, _),
    % Convert back to original format (remove random weights)
    ga_restore_fitness(SelectedWeighted, Pop, Selected).

ga_assign_random_weights([], []).
ga_assign_random_weights([Ind*Fitness | Rest], [Ind*WeightedFitness | RestW]) :-
    random(R),
    WeightedFitness is Fitness * R,
    ga_assign_random_weights(Rest, RestW).

ga_restore_fitness([], _, []).
ga_restore_fitness([Ind*_ | Rest], OriginalPop, [Ind*OrigFitness | RestR]) :-
    member(Ind*OrigFitness, OriginalPop), !,
    ga_restore_fitness(Rest, OriginalPop, RestR).
ga_restore_fitness([_ | Rest], OriginalPop, RestR) :-
    ga_restore_fitness(Rest, OriginalPop, RestR).

% ---------------------------------------------------------------
% Crossover operation (Order Crossover - OX)
% ---------------------------------------------------------------

ga_crossover([], _, []).
ga_crossover([Ind*_], _, [Ind]).
ga_crossover([Ind1*_, Ind2*_ | Rest], NumVessels, [NewInd1, NewInd2 | RestNew]) :-
    ga_prob_crossover(PCross),
    random(P),
    (   P =< PCross
    ->  ga_generate_crossover_points(NumVessels, P1, P2),
        ga_order_crossover(Ind1, Ind2, P1, P2, NumVessels, NewInd1),
        ga_order_crossover(Ind2, Ind1, P1, P2, NumVessels, NewInd2)
    ;   NewInd1 = Ind1,
        NewInd2 = Ind2
    ),
    ga_crossover(Rest, NumVessels, RestNew).

ga_generate_crossover_points(NumVessels, P1, P2) :-
    % Use fixed crossover points to avoid infinite loops
    (   NumVessels =< 2
    ->  P1 = 1, P2 = NumVessels
    ;   P1 is max(1, NumVessels // 3),
        P2 is min(NumVessels, (NumVessels * 2) // 3)
    ).

% Order crossover implementation
ga_order_crossover(Parent1, Parent2, P1, P2, NumVessels, Child) :-
    ga_sublist_with_h(Parent1, P1, P2, SubWithH),
    R is NumVessels - P2,
    ga_rotate_right(Parent2, R, NumVessels, Rotated),
    ga_get_middle_genes(Parent1, P1, P2, MiddleGenes),
    ga_remove_genes(Rotated, MiddleGenes, Remaining),
    P3 is P2 + 1,
    ga_insert_genes(Remaining, SubWithH, P3, NumVessels, ChildWithH),
    ga_remove_h(ChildWithH, Child).

ga_sublist_with_h(List, I1, I2, Result) :-
    ga_sublist_with_h1(List, 1, I1, I2, Result).

ga_sublist_with_h1([], _, _, _, []).
ga_sublist_with_h1([X | Rest], Pos, I1, I2, [Y | ResultRest]) :-
    (   Pos >= I1, Pos =< I2
    ->  Y = X
    ;   Y = h
    ),
    Pos1 is Pos + 1,
    ga_sublist_with_h1(Rest, Pos1, I1, I2, ResultRest).

ga_get_middle_genes(List, I1, I2, Genes) :-
    ga_get_middle_genes1(List, 1, I1, I2, Genes).

ga_get_middle_genes1([], _, _, _, []).
ga_get_middle_genes1([X | Rest], Pos, I1, I2, Result) :-
    (   Pos >= I1, Pos =< I2
    ->  Result = [X | ResultRest]
    ;   Result = ResultRest
    ),
    Pos1 is Pos + 1,
    ga_get_middle_genes1(Rest, Pos1, I1, I2, ResultRest).

ga_rotate_right(L, K, N, L1) :-
    T is N - K,
    ga_rr(T, L, L1).

ga_rr(0, L, L) :- !.
ga_rr(N, [X | R], R2) :-
    N > 0,
    N1 is N - 1,
    append(R, [X], R1),
    ga_rr(N1, R1, R2).

ga_remove_genes([], _, []).
ga_remove_genes([X | R1], ToRemove, Result) :-
    (   member(X, ToRemove)
    ->  Result = R2
    ;   Result = [X | R2]
    ),
    ga_remove_genes(R1, ToRemove, R2).

ga_insert_genes([], L, _, _, L) :- !.
ga_insert_genes([X | R], L, N, NumVessels, L2) :-
    (   N > NumVessels
    ->  N1 is ((N - 1) mod NumVessels) + 1
    ;   N1 = N
    ),
    ga_insert1(X, N1, L, L1),
    N2 is N + 1,
    ga_insert_genes(R, L1, N2, NumVessels, L2).

ga_insert1(X, 1, L, [X | L]) :- !.
ga_insert1(X, N, [Y | L], [Y | L1]) :-
    N > 1,
    N1 is N - 1,
    ga_insert1(X, N1, L, L1).

ga_remove_h([], []).
ga_remove_h([h | R1], R2) :- !,
    ga_remove_h(R1, R2).
ga_remove_h([X | R1], [X | R2]) :-
    ga_remove_h(R1, R2).

% ---------------------------------------------------------------
% Mutation operation (swap two genes)
% ---------------------------------------------------------------

ga_mutation([], _, []).
ga_mutation([Ind | Rest], NumVessels, [NewInd | RestNew]) :-
    ga_prob_mutation(PMut),
    random(P),
    (   P < PMut
    ->  ga_mutate_individual(Ind, NumVessels, NewInd)
    ;   NewInd = Ind
    ),
    ga_mutation(Rest, NumVessels, RestNew).

ga_mutate_individual(Ind, NumVessels, NewInd) :-
    ga_generate_crossover_points(NumVessels, P1, P2),
    ga_swap_positions(Ind, P1, P2, NewInd).

ga_swap_positions(List, P1, P2, NewList) :-
    nth1(P1, List, E1),
    nth1(P2, List, E2),
    ga_replace_at(List, P1, E2, Temp),
    ga_replace_at(Temp, P2, E1, NewList).

ga_replace_at([_ | T], 1, X, [X | T]) :- !.
ga_replace_at([H | T], N, X, [H | T2]) :-
    N > 1,
    N1 is N - 1,
    ga_replace_at(T, N1, X, T2).

% ---------------------------------------------------------------
% Convert GA solution to schedule format
% ---------------------------------------------------------------

ga_convert_to_schedule(VesselSequence, Dock, UseMultiCrane, Schedule, CranesAlloc) :-
    (   UseMultiCrane = true
    ->  ga_convert_multi_crane(VesselSequence, Dock, Schedule, CranesAlloc)
    ;   ga_convert_single_crane(VesselSequence, Dock, Schedule, CranesAlloc)
    ).

ga_convert_single_crane(VesselSequence, Dock, Schedule, CranesAlloc) :-
    ga_convert_single_crane1(VesselSequence, Dock, 0, Schedule),
    length(VesselSequence, N),
    ga_generate_ones(N, CranesAlloc).

ga_convert_single_crane1([], _, _, []).
ga_convert_single_crane1([VesselId | Rest], Dock, EndPrev, [(VesselId, Start, End, StartStr, EndStr, DurStr, Dock) | RestSched]) :-
    user:vessel(VesselId, ArrivalTime, _, UnloadTime, LoadTime, _),
    Start is max(ArrivalTime, EndPrev),
    TotalOp is UnloadTime + LoadTime,
    End is Start + TotalOp,
    hours_to_time_string(Start, StartStr),
    hours_to_time_string(End, EndStr),
    hours_to_time_string(TotalOp, DurStr),
    ga_convert_single_crane1(Rest, Dock, End, RestSched).

ga_convert_multi_crane(VesselSequence, Dock, Schedule, CranesAlloc) :-
    length(VesselSequence, NumVessels),
    format(user_error, '[GA] Multi-crane conversion for ~w vessels~n', [NumVessels]),
    
    % First evaluate with single crane to get baseline
    ga_sequence_temporization(VesselSequence, 0, SingleSchedule),
    ga_sum_delays(SingleSchedule, SingleDelay),
    format(user_error, '[GA] Single crane delay: ~2f~n', [SingleDelay]),
    
    % If no delay with single crane, no point in multi-crane
    (   SingleDelay =:= 0
    ->  format(user_error, '[GA] No delay, using single crane~n', []),
        ga_convert_single_crane(VesselSequence, Dock, Schedule, CranesAlloc)
    ;   % Try to optimize each vessel with multi-crane (2 cranes to reduce delay)
        format(user_error, '[GA] Optimizing with multi-crane (baseline delay: ~2f)~n', [SingleDelay]),
        ga_optimize_vessels_multi_crane(VesselSequence, Dock, 0, Schedule, CranesAlloc)
    ).

% Optimize each vessel: decide whether to use 1 or 2 cranes
ga_optimize_vessels_multi_crane([], _, _, [], []).
ga_optimize_vessels_multi_crane([VesselId | Rest], Dock, EndPrev, [ScheduleEntry | RestSched], [NCranes | RestAlloc]) :-
    user:vessel(VesselId, ArrivalTime, DesiredDeparture, UnloadTime, LoadTime, _),
    Start is max(ArrivalTime, EndPrev),
    TotalOp is UnloadTime + LoadTime,
    
    % Calculate delay with 1 crane
    End1Crane is Start + TotalOp,
    Delay1Crane is max(0, End1Crane - DesiredDeparture),
    
    % Calculate delay with 2 cranes (half the time)
    End2Cranes is Start + (TotalOp / 2),
    Delay2Cranes is max(0, End2Cranes - DesiredDeparture),
    
    % Use 2 cranes if it reduces delay significantly (at least 15 minutes = 0.25h)
    (   Delay1Crane > 0.25, Delay2Cranes < Delay1Crane
    ->  NCranes = 2,
        End = End2Cranes,
        Duration is TotalOp / 2,
        format(user_error, '[GA] Vessel ~w: 2 cranes (delay ~2f -> ~2f)~n', [VesselId, Delay1Crane, Delay2Cranes])
    ;   NCranes = 1,
        End = End1Crane,
        Duration = TotalOp,
        format(user_error, '[GA] Vessel ~w: 1 crane (delay ~2f)~n', [VesselId, Delay1Crane])
    ),
    
    hours_to_time_string(Start, StartStr),
    hours_to_time_string(End, EndStr),
    hours_to_time_string(Duration, DurStr),
    ScheduleEntry = (VesselId, Start, End, StartStr, EndStr, DurStr, Dock),
    
    ga_optimize_vessels_multi_crane(Rest, Dock, End, RestSched, RestAlloc).

ga_find_best_crane_allocation(VesselSequence, Dock, Schedule, BestAlloc) :-
    length(VesselSequence, N),
    % Try several random allocations and keep the best
    ga_try_allocations(VesselSequence, Dock, N, 20, [], 1000000, Schedule, BestAlloc).

ga_try_allocations(_, _, _, 0, BestSched, _, BestSched, _) :- !.
ga_try_allocations(VesselSequence, Dock, N, Tries, _, BestDelay, BestSched, BestAlloc) :-
    ga_generate_crane_allocation(N, Alloc),
    ga_build_schedule_with_alloc(VesselSequence, Dock, Alloc, 0, Sched),
    ga_sum_delays_from_schedule(Sched, Delay),
    Tries1 is Tries - 1,
    (   Delay < BestDelay
    ->  ga_try_allocations(VesselSequence, Dock, N, Tries1, Sched, Delay, BestSched, BestAlloc),
        BestAlloc = Alloc
    ;   ga_try_allocations(VesselSequence, Dock, N, Tries1, BestSched, BestDelay, BestSched, BestAlloc)
    ).

ga_build_schedule_with_alloc([], _, [], _, []).
ga_build_schedule_with_alloc([VesselId | RestV], Dock, [NCranes | RestC], EndPrev, 
                             [(VesselId, Start, End, StartStr, EndStr, DurStr, Dock) | RestSched]) :-
    user:vessel(VesselId, ArrivalTime, _, UnloadTime, LoadTime, _),
    Start is max(ArrivalTime, EndPrev),
    TotalOp is (UnloadTime + LoadTime) / NCranes,
    End is Start + TotalOp,
    hours_to_time_string(Start, StartStr),
    hours_to_time_string(End, EndStr),
    hours_to_time_string(TotalOp, DurStr),
    ga_build_schedule_with_alloc(RestV, Dock, RestC, End, RestSched).

ga_sum_delays_from_schedule([], 0).
ga_sum_delays_from_schedule([(VesselId, _, End, _, _, _, _) | Rest], TotalDelay) :-
    user:vessel(VesselId, _, DesiredDeparture, _, _, _),
    VesselDelay is max(0, End - DesiredDeparture),
    ga_sum_delays_from_schedule(Rest, RestDelay),
    TotalDelay is VesselDelay + RestDelay.

ga_generate_ones(0, []) :- !.
ga_generate_ones(N, [1 | Rest]) :-
    N > 0,
    N1 is N - 1,
    ga_generate_ones(N1, Rest).

% ---------------------------------------------------------------
% Calculate baseline delay (single crane for all)
% ---------------------------------------------------------------

% Calculate baseline delay: recalculate schedule with single crane for ALL vessels
% This gives accurate comparison for multi-crane optimization
ga_calculate_baseline_delay(Schedule, BaselineDelay) :-
    % Extract vessel IDs in current order
    findall(VesselId, member((VesselId, _, _, _, _, _, _), Schedule), VesselIds),
    % Get dock from first entry
    (   Schedule = [(_, _, _, _, _, _, Dock) | _]
    ->  true
    ;   Dock = "DOCK-A"  % fallback
    ),
    % Recalculate with single crane from scratch
    ga_convert_single_crane1(VesselIds, Dock, 0, NewSchedule),
    ga_sum_delays_from_schedule(NewSchedule, BaselineDelay),
    format(user_error, '[GA] Baseline delay calculated: ~2f~n', [BaselineDelay]).

ga_recalc_with_single_crane([], _, []).
ga_recalc_with_single_crane([(VesselId, _, _, _, _, _, Dock) | Rest], EndPrev, 
                            [(VesselId, Start, End, StartStr, EndStr, DurStr, Dock) | RestSched]) :-
    user:vessel(VesselId, ArrivalTime, _, UnloadTime, LoadTime, _),
    Start is max(ArrivalTime, EndPrev),
    TotalOp is UnloadTime + LoadTime,  % Single crane (no division)
    End is Start + TotalOp,
    hours_to_time_string(Start, StartStr),
    hours_to_time_string(End, EndStr),
    hours_to_time_string(TotalOp, DurStr),
    ga_recalc_with_single_crane(Rest, End, RestSched).

% ---------------------------------------------------------------
% Combine results from all docks
% ---------------------------------------------------------------

ga_combine_dock_results([], [], 0, []).
ga_combine_dock_results([dock_result(Dock, Schedule, Delay, Alloc) | Rest], CombinedSchedule, TotalDelay, CombinedAlloc) :-
    length(Schedule, NScheds),
    length(Alloc, NAllocs),
    format(user_error, '[GA] Combining dock ~w: ~w scheds, ~w allocs, delay=~2f~n', [Dock, NScheds, NAllocs, Delay]),
    ga_combine_dock_results(Rest, RestSchedule, RestDelay, RestAlloc),
    append(Schedule, RestSchedule, CombinedSchedule),
    TotalDelay is Delay + RestDelay,
    append(Alloc, RestAlloc, CombinedAlloc),
    length(CombinedSchedule, NCombined),
    format(user_error, '[GA] After combining ~w: ~w total scheds~n', [Dock, NCombined]).


% ============================================================================
% AUTO ALGORITHM SELECTION (US 4.3.2)
% ============================================================================
% Automatically selects the most suitable scheduling algorithm based on:
%   - Problem size (number of vessels, operations)
%   - Available computation time
%
% Selection Policy:
%   - Small instances (≤5 vessels): Optimal algorithm (exhaustive search)
%   - Medium instances (6-15 vessels): Heuristic algorithm (fast greedy)
%   - Large instances (>15 vessels) or time-constrained: Genetic algorithm
%
% The selected algorithm is explicitly indicated in the response for traceability.
% ============================================================================

% ---------------------------------------------------------------
% Main entry point for automatic algorithm selection
% Parameters dict contains:
%   - time_limit_ms: max desired computation time (default: 5000)
%   - prefer_quality: boolean to prefer quality over speed (default: false)
% ---------------------------------------------------------------

compute_auto_schedule(Vessels, Parameters, Result) :-
    % Validate input
    (   is_list(Vessels), Vessels \= []
    ->  true
    ;   Result = _{
            algorithm: "auto",
            selected_algorithm: "none",
            error: "Invalid or empty vessel list",
            total_delay: 0,
            schedule: []
        },
        !
    ),
    
    % Analyze problem scale
    length(Vessels, NumVessels),
    
    % Get parameters
    (get_dict(time_limit_ms, Parameters, TimeLimit) -> true ; TimeLimit = 5000),
    (get_dict(prefer_quality, Parameters, PreferQuality) -> true ; PreferQuality = false),
    
    % Count unique docks (more docks = more complexity)
    findall(Dock, (member(V, Vessels), Dock = V.get(assigned_dock, "DOCK-A")), AllDocks),
    sort(AllDocks, UniqueDocks),
    length(UniqueDocks, NumDocks),
    
    % Calculate problem complexity score
    % More vessels and more docks = higher complexity
    ComplexityScore is NumVessels * NumDocks,
    
    format(user_error, '[AUTO] Analyzing problem: ~w vessels, ~w docks, complexity=~w~n', 
           [NumVessels, NumDocks, ComplexityScore]),
    format(user_error, '[AUTO] Parameters: time_limit=~w ms, prefer_quality=~w~n', 
           [TimeLimit, PreferQuality]),
    
    % Select algorithm based on policy
    auto_select_algorithm(NumVessels, ComplexityScore, TimeLimit, PreferQuality, SelectedAlgorithm, SelectionReason),
    
    format(user_error, '[AUTO] Selected algorithm: ~w (~w)~n', [SelectedAlgorithm, SelectionReason]),
    
    % Execute selected algorithm
    get_time(StartTime),
    auto_execute_algorithm(SelectedAlgorithm, Vessels, Parameters, AlgoResult),
    get_time(EndTime),
    TotalComputationTime is (EndTime - StartTime) * 1000,
    
    % Extract results from algorithm
    (get_dict(total_delay, AlgoResult, TotalDelay) -> true ; TotalDelay = 0),
    (get_dict(schedule, AlgoResult, Schedule) -> true ; Schedule = []),
    (get_dict(computation_time_ms, AlgoResult, AlgoCompTime) -> true ; AlgoCompTime = TotalComputationTime),
    
    % Build result with auto-selection metadata
    Result = _{
        algorithm: "auto",
        selected_algorithm: SelectedAlgorithm,
        selection_reason: SelectionReason,
        problem_analysis: _{
            vessel_count: NumVessels,
            dock_count: NumDocks,
            complexity_score: ComplexityScore,
            time_limit_ms: TimeLimit
        },
        total_delay: TotalDelay,
        computation_time_ms: TotalComputationTime,
        algorithm_computation_time_ms: AlgoCompTime,
        schedule: Schedule,
        original_result: AlgoResult
    },
    
    format(user_error, '[AUTO] Completed in ~2f ms (delay: ~2f h)~n', [TotalComputationTime, TotalDelay]).

% ---------------------------------------------------------------
% Algorithm selection policy
% ---------------------------------------------------------------

% Small instances (≤5 vessels): Use optimal (exhaustive search is feasible)
auto_select_algorithm(NumVessels, _, _, _, optimal, "small_instance") :-
    NumVessels =< 5,
    format(user_error, '[AUTO] Small instance (<=5 vessels): selecting optimal~n', []),
    !.

% Very tight time constraint (<1000ms): Use heuristic regardless of size
auto_select_algorithm(_, _, TimeLimit, _, heuristic, "time_constrained") :-
    TimeLimit < 1000,
    format(user_error, '[AUTO] Tight time constraint (<1s): selecting heuristic~n', []),
    !.

% Medium instances (6-15 vessels) with enough time: Use optimal if prefer quality
auto_select_algorithm(NumVessels, _, TimeLimit, true, optimal, "quality_preferred") :-
    NumVessels > 5,
    NumVessels =< 10,
    TimeLimit >= 5000,
    format(user_error, '[AUTO] Medium instance with quality preference: selecting optimal~n', []),
    !.

% Medium instances (6-15 vessels): Use heuristic for speed
auto_select_algorithm(NumVessels, _, _, _, heuristic, "medium_instance") :-
    NumVessels > 5,
    NumVessels =< 15,
    format(user_error, '[AUTO] Medium instance (6-15 vessels): selecting heuristic~n', []),
    !.

% Large instances (>15 vessels) with good time budget: Use genetic
auto_select_algorithm(NumVessels, _, TimeLimit, _, genetic, "large_instance") :-
    NumVessels > 15,
    TimeLimit >= 3000,
    format(user_error, '[AUTO] Large instance (>15 vessels): selecting genetic~n', []),
    !.

% Large instances with tight time: Use heuristic
auto_select_algorithm(NumVessels, _, _, _, heuristic, "large_time_constrained") :-
    NumVessels > 15,
    format(user_error, '[AUTO] Large instance with time constraint: selecting heuristic~n', []),
    !.

% High complexity (many docks): Use genetic if time allows
auto_select_algorithm(_, ComplexityScore, TimeLimit, _, genetic, "high_complexity") :-
    ComplexityScore > 20,
    TimeLimit >= 3000,
    format(user_error, '[AUTO] High complexity: selecting genetic~n', []),
    !.

% Default fallback: Use heuristic (safest choice)
auto_select_algorithm(_, _, _, _, heuristic, "default_fallback") :-
    format(user_error, '[AUTO] Default fallback: selecting heuristic~n', []).

% ---------------------------------------------------------------
% Execute the selected algorithm
% ---------------------------------------------------------------

auto_execute_algorithm(optimal, Vessels, _, Result) :-
    format(user_error, '[AUTO] Executing: optimal algorithm~n', []),
    compute_optimal_schedule(Vessels, Result).

auto_execute_algorithm(heuristic, Vessels, _, Result) :-
    format(user_error, '[AUTO] Executing: heuristic algorithm~n', []),
    compute_heuristic_schedule(Vessels, Result).

auto_execute_algorithm(genetic, Vessels, Parameters, Result) :-
    format(user_error, '[AUTO] Executing: genetic algorithm~n', []),
    % Use provided GA parameters or defaults optimized for auto mode
    (get_dict(time_limit_ms, Parameters, TimeLimit) -> true ; TimeLimit = 5000),
    GATimeLimit is max(1000, TimeLimit - 500),  % Reserve 500ms for overhead
    GAParameters = _{
        population_size: 15,
        generations: 30,
        crossover_rate: 70,
        mutation_rate: 20,
        time_limit_ms: GATimeLimit,
        use_multi_crane: false
    },
    compute_genetic_schedule(Vessels, GAParameters, Result).


% ============================================================================
% END OF MODULE
% ============================================================================
% ============================================================================
% ============================================================================
% Port Logistics - Planning & Scheduling Module (Prolog)
% US 3.4.1 - REST API Infrastructure for Planning & Scheduling
% US 3.4.2 - Daily Vessel Scheduling
% ============================================================================
% This module provides a REST-based API that consumes operational data from
% the existing C# backend services. It aggregates information required for
% scheduling computations without persisting any operational data.
%
% Author: LEI-ISEP Team
% Date: November 2025
% ============================================================================

% Required Libraries
:- use_module(library(http/thread_httpd)).
:- use_module(library(http/http_dispatch)).
:- use_module(library(http/http_client)).
:- use_module(library(http/http_open)).
:- use_module(library(http/http_cors)).
:- use_module(library(http/http_json)).
:- use_module(library(http/json)).
:- use_module(library(option)).

% Load configuration and scheduling modules (US 3.4.2)
:- use_module(config).
:- use_module(ssl_config).
:- consult(scheduling_algorithms).
:- use_module(data_service).

% ============================================================================
% SERVER MANAGEMENT
% ============================================================================

% Start the scheduling server with HTTP interface
start_scheduling_server :-
    config:scheduling_server_port(Port),
    % Check if server is already running on this port
    (   catch(http_current_server(Port, _), _, fail)
    ->  format('Server already running on port ~w~n', [Port]),
        format('Use stop_scheduling_server/0 first to stop the existing server.~n')
    ;   % Start the server
        catch(
            http_server(http_dispatch, [port(Port)]),
            error(permission_error(create, thread, _), _),
            (   format('Thread conflict detected. Attempting to clean up...~n'),
                % Try to stop any existing server and retry
                catch(http_stop_server(Port, []), _, true),
                sleep(1),
                http_server(http_dispatch, [port(Port)])
            )
        ),
        format('~n==============================================~n'),
        format('Planning & Scheduling API (Prolog)~n'),
        format('US 3.4.1 - Data Consumption Module~n'),
        format('US 3.4.2 - Daily Vessel Scheduling~n'),
        format('Started on port ~w~n', [Port]),
        format('Available Endpoints:~n'),
        format('  GET  /api/scheduling/health~n'),
        format('  POST /api/scheduling/generate~n'),
        format('  POST /api/scheduling/daily~n'),
        format('==============================================~n~n'),
        % Keep server running forever
        repeat,
        sleep(86400),
        fail
    ).

% Stop the scheduling server
stop_scheduling_server :-
    config:scheduling_server_port(Port),
    http_stop_server(Port, _),
    format('Scheduling server stopped.~n').

% ============================================================================
% HTTP HANDLERS - REST API ENDPOINTS
% ============================================================================

% Enable CORS for all endpoints
:- set_setting(http:cors, [*]).

% Health check endpoint
:- http_handler('/api/scheduling/health', handle_health, []).

handle_health(Request) :-
    % Descobrir o método (get ou options)
    memberchk(method(Method), Request),
    (   % Pedido OPTIONS → preflight CORS
        Method == options
    ->  cors_enable(Request, [methods([get])]),
        format('~n')                   % resposta 200 sem body
    ;   % Pedido GET normal
        cors_enable,                   % mete os headers CORS
        reply_json_dict(_{
            status:  "ok",
            message: "Planning & Scheduling API is operational",
            version: "1.0",
            module:  "planning_scheduling_prolog"
        })
    ).

% Daily scheduling endpoint (US 3.4.2)
:- http_handler('/api/scheduling/daily', handle_daily_schedule, []).

handle_daily_schedule(Request) :-
    % Descobrir o método (POST ou OPTIONS)
    memberchk(method(Method), Request),
    (   % Preflight CORS (OPTIONS)
        Method == options
    ->  % Só cabeçalhos de CORS, sem body
        cors_enable(Request, [methods([post])]),
        format('~n')           % termina a resposta (200 OK sem body)
    ;   % Pedido POST normal
        cors_enable,           % adiciona cabeçalhos CORS
        catch(
            (
                % -------- 1) Ler o JSON do body --------
                http_read_json_dict(Request, RequestBody),

                % Extrair a data (campo "date" vindo do Angular)
                (   get_dict(date, RequestBody, DateStr)
                ->  true
                ;   throw(error(missing_date_field, RequestBody))
                ),

                % Extrair algoritmo (campo "algorithm" vindo do Angular, default: "optimal")
                (   get_dict(algorithm, RequestBody, Algorithm)
                ->  true
                ;   Algorithm = "optimal"
                ),

                % Log opcional
                (   config:get_config(log_api_calls, true)
->  format(user_error, 'Computing daily schedule for: ~w using algorithm: ~w~n', [DateStr, Algorithm])
;   true
),


                                % -------- 2) Limpar facts antigos ANTES de carregar novos --------
                retractall(user:vessel(_, _, _, _, _, _)),
                retractall(shortest_delay(_, _)),
                retractall(shortest_delay_multi(_, _, _)),
                
                % -------- 3) Ir buscar navios ao backend C# --------
                data_service:fetch_vessels_for_date(DateStr, Vessels),

                % -------- 3) Se não houver navios --------
                (   Vessels = []
                ->  reply_json_dict(_{
                        success:      false,
                        message:      "No vessels scheduled for the specified date",
                        date:         DateStr,
                        vessel_count: 0
                    })

                % -------- 4) Se houver navios, corre o algoritmo selecionado --------
                ;   length(Vessels, VesselCount),

                    % --- Escolher algoritmo de agendamento ---
                    (   Algorithm == "auto"
                    ->  % US 4.3.2 - Automatic algorithm selection
                        format(user_error, '[SERVER] Using AUTO algorithm selection~n', []),
                        (get_dict(time_limit_ms, RequestBody, AutoTimeLimit) -> true ; AutoTimeLimit = 5000),
                        (get_dict(prefer_quality, RequestBody, PreferQuality) -> true ; PreferQuality = false),
                        
                        AutoParameters = _{
                            time_limit_ms: AutoTimeLimit,
                            prefer_quality: PreferQuality
                        },
                        
                        scheduling_algorithms:compute_auto_schedule(Vessels, AutoParameters, AlgoResult),
                        BaselineResult = none
                    
                    ;   Algorithm == "heuristic"
                    ->  % Algoritmo heurístico (rápido, para muitos navios)
                        scheduling_algorithms:compute_heuristic_schedule(Vessels, AlgoResult),
                        BaselineResult = none

                    ;   Algorithm == "multi_crane"
                    ->  % Novo algoritmo US 3.4.5 (multi-gruas)
                        % Primeiro, calcular baseline com 1 grua
                        scheduling_algorithms:compute_optimal_schedule(Vessels, BaselineResult),
                        % Depois, otimizar com multi-gruas
                        scheduling_algorithms:compute_multi_crane_schedule(
                            Vessels,
                            BaselineResult,   % plano 1 grua (óptimo)
                            AlgoResult        % plano multi-gruas
                        )

                    ;   Algorithm == "genetic"
                    ->  % US 4.3.1 - Genetic Algorithm
                        % Extrair parâmetros do request (com defaults)
                        (get_dict(ga_population_size, RequestBody, GAPopSize) -> true ; GAPopSize = 20),
                        (get_dict(ga_generations, RequestBody, GAGenerations) -> true ; GAGenerations = 50),
                        (get_dict(ga_crossover_rate, RequestBody, GACrossover) -> true ; GACrossover = 70),
                        (get_dict(ga_mutation_rate, RequestBody, GAMutation) -> true ; GAMutation = 20),
                        (get_dict(ga_time_limit, RequestBody, GATimeLimit) -> true ; GATimeLimit = 5000),
                        (get_dict(ga_use_multi_crane, RequestBody, GAMultiCrane) -> true ; GAMultiCrane = false),
                        
                        GAParameters = _{
                            population_size: GAPopSize,
                            generations: GAGenerations,
                            crossover_rate: GACrossover,
                            mutation_rate: GAMutation,
                            time_limit_ms: GATimeLimit,
                            use_multi_crane: GAMultiCrane
                        },
                        
                        scheduling_algorithms:compute_genetic_schedule(Vessels, GAParameters, AlgoResult),
                        BaselineResult = none

                    ;   % Default: algoritmo ótimo de 1 grua
                        scheduling_algorithms:compute_optimal_schedule(Vessels, AlgoResult),
                        BaselineResult = none
                    ),

                    % --- Recursos disponíveis ---
                    data_service:fetch_available_resources(Resources),

                    % Extrair campos do resultado
                    get_dict(schedule, AlgoResult, Schedule),
                    get_dict(computation_time_ms, AlgoResult, ComputationTime),

                    % Enriquecer schedule com recursos atribuídos
                    scheduling_algorithms:assign_resources_to_schedule(
                        Schedule, Resources, EnrichedScheduleBase
                    ),

                    % US 3.4.5 & US 4.3.1: Se multi-crane ou genetic com multi-crane, enriquecer com múltiplas gruas
                    (   Algorithm == "multi_crane"
                    ->  (get_dict(cranes_allocation, AlgoResult, CranesAlloc) -> true ; CranesAlloc = []),
                        format(user_error, '[SERVER] Multi-crane: enriching with ~w allocations~n', [CranesAlloc]),
                        enrich_multi_crane_operations(EnrichedScheduleBase, CranesAlloc, Resources, EnrichedSchedule)
                    ;   Algorithm == "genetic"
                    ->  % For genetic, check if multi-crane was used
                        (get_dict(cranes_allocation, AlgoResult, CranesAlloc) -> true ; CranesAlloc = []),
                        (get_dict(strategy, AlgoResult, Strategy) -> true ; Strategy = single_crane),
                        format(user_error, '[SERVER] Genetic strategy=~w, alloc=~w~n', [Strategy, CranesAlloc]),
                        % Check if any vessel uses > 1 crane
                        (   (member(N, CranesAlloc), N > 1)
                        ->  format(user_error, '[SERVER] Genetic: found multi-crane usage, enriching~n', []),
                            enrich_multi_crane_operations(EnrichedScheduleBase, CranesAlloc, Resources, EnrichedSchedule)
                        ;   format(user_error, '[SERVER] Genetic: no multi-crane usage~n', []),
                            EnrichedSchedule = EnrichedScheduleBase
                        )
                    ;   EnrichedSchedule = EnrichedScheduleBase
                    ),

                    % Recalcular delay apenas para operações com recursos válidos
                    scheduling_algorithms:calculate_valid_delay(EnrichedSchedule, ValidDelay),

                    % --- Construir resposta JSON final ---
                    build_daily_response(
                        Algorithm,
                        DateStr,
                        VesselCount,
                        BaselineResult,
                        AlgoResult,
                        ValidDelay,
                        ComputationTime,
                        EnrichedSchedule,
                        ResponseJson
                    ),

                    reply_json_dict(ResponseJson)
                )

            ),
            % -------- 6) Handler de erros globais --------
                    Error,
        (
            % Converte o termo de erro Prolog para string
            term_string(Error, ErrorStr),

            % Loga para a consola (user_error), NÃO para a resposta HTTP
            format(user_error, 'Error computing daily schedule: ~w~n', [ErrorStr]),

            % Resposta JSON 500 clara para o cliente
            reply_json_dict(_{
                success: false,
                error:   "Failed to compute daily schedule",
                message: ErrorStr
            }, [status(500)])
        )
        )
    ).


% Data aggregation endpoint
:- http_handler('/api/scheduling/generate', handle_aggregate_data, []).

handle_aggregate_data(Request) :-
    cors_enable(Request, [methods([post, options])]),
    (   option(method(options), Request)
    ->  true
    ;   catch(
            (
                http_read_json_dict(Request, RequestBody),
                _{date: DateStr} :< RequestBody,

                (   config:get_config(log_api_calls, true)
                ->  format('Fetching backend data for date: ~w~n', [DateStr])
                ;   true
                ),

                fetch_scheduling_data(DateStr, Result),
                reply_json(Result)
            ),
                        Error,
            (
                term_string(Error, ErrorStr),
                format(user_error, 'Error fetching backend data: ~w~n', [ErrorStr]),
                reply_json_dict(_{
                    success: false,
                    error:   "Failed to fetch backend data",
                    message: ErrorStr
                }, [status(500)])
            )

        )
    ).

% ============================================================================
% BACKEND DATA CONSUMPTION
% ============================================================================
% Functions to consume operational data from existing C# backend services
% ============================================================================

% HTTP options for backend requests
http_backend_options([
    timeout(30),
    request_header('X-User-Email'='admin@example.com'),
    cert_verify_hook(ssl_verify)
]).

% Fetch all vessel visit notifications
fetch_vessel_visits(_Date, Visits) :-
    config:backend_api_url(BaseUrl),
    atom_concat(BaseUrl, '/vessel-visits/notifications', Url),
    http_backend_options(Options),
    catch(
        (
            http_open(Url, Stream, Options),
            json_read_dict(Stream, JsonData),
            close(Stream),
            Visits = JsonData
        ),
        _Error,
        Visits = []
    ).

% Fetch active physical resources
fetch_resources(Resources) :-
    config:backend_api_url(BaseUrl),
    atom_concat(BaseUrl, '/PhysicalResources', Url),
    http_backend_options(Options),
    catch(
        (
            http_open(Url, Stream, Options),
            json_read_dict(Stream, JsonData),
            close(Stream),
            Resources = JsonData
        ),
        _Error,
        Resources = []
    ).

% Fetch available staff
fetch_staff(Staff) :-
    config:backend_api_url(BaseUrl),
    atom_concat(BaseUrl, '/OperatingStaff', Url),
    http_backend_options(Options),
    catch(
        (
            http_open(Url, Stream, Options),
            json_read_dict(Stream, JsonData),
            close(Stream),
            Staff = JsonData
        ),
        _Error,
        Staff = []
    ).

% Fetch all docks
fetch_docks(Docks) :-
    config:backend_api_url(BaseUrl),
    atom_concat(BaseUrl, '/Docks', Url),
    http_backend_options(Options),
    catch(
        (
            http_open(Url, Stream, Options),
            json_read_dict(Stream, JsonData),
            close(Stream),
            Docks = JsonData
        ),
        _Error,
        Docks = []
    ).

% Fetch all storage areas
fetch_storage_areas(StorageAreas) :-
    config:backend_api_url(BaseUrl),
    atom_concat(BaseUrl, '/storage-areas', Url),
    http_backend_options(Options),
    catch(
        (
            http_open(Url, Stream, Options),
            json_read_dict(Stream, JsonData),
            close(Stream),
            StorageAreas = JsonData
        ),
        _Error,
        StorageAreas = []
    ).

% ============================================================================
% DATA AGGREGATION
% ============================================================================
% Aggregate all required data from multiple backend endpoints
% ============================================================================

% Fetch and aggregate operational data from backend services
fetch_scheduling_data(TargetDate, Result) :-
    % Start timing
    get_time(StartTime),
    
    % Consume data from existing backend APIs
    fetch_vessel_visits(TargetDate, Visits),
    fetch_resources(Resources),
    fetch_staff(Staff),
    fetch_docks(Docks),
    fetch_storage_areas(StorageAreas),
    
    % Stop timing
    get_time(EndTime),
    ComputationTime is (EndTime - StartTime) * 1000,
    
    % Return aggregated data without persistence
    Result = json([
        success=true,
        message="Operational data retrieved from backend services",
        computation_time_ms=ComputationTime,
        data=json([
            vessel_visits=Visits,
            resources=Resources,
            staff=Staff,
            docks=Docks,
            storage_areas=StorageAreas
        ])
    ]).

% ============================================================================
% AUTO-START SERVER
% ============================================================================

% Note: Auto-start disabled to allow manual control via start_server.ps1
% Uncomment the line below if you want automatic server start on file load:
% :- initialization(start_scheduling_server, main).

% -----------------------------------------------------------------
% enrich_multi_crane_operations/4
% US 3.4.5: Enriquece operações com múltiplas gruas quando necessário
% -----------------------------------------------------------------

enrich_multi_crane_operations([], _, _, []).
enrich_multi_crane_operations([Op | Rest], [NCranes | RestAlloc], Resources, [EnrichedOp | EnrichedRest]) :-
    % Se usa mais de 1 grua, buscar gruas adicionais do mesmo dock
    (   NCranes > 1
    ->  get_dict(assigned_dock, Op, DockCode),
        Resources = json([cranes=Cranes, staff=_, storage=_, docks=_]),
        
        % Buscar gruas disponíveis do mesmo dock
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
                (get_dict(setupTimeMinutes, Crane, SetupTime) -> true ; SetupTime = 9999)
            ),
            AvailableCranes
        ),
        
        % Selecionar as N gruas com menor setup time
        sort(AvailableCranes, SortedCranes),
        take_n_cranes(NCranes, SortedCranes, SelectedCranes),
        extract_crane_codes(SelectedCranes, CraneCodes),
        
        % Atualizar operação com lista de gruas
        (   CraneCodes \= []
        ->  put_dict(assigned_cranes, Op, CraneCodes, EnrichedOp)
        ;   EnrichedOp = Op
        )
    ;   % Usa apenas 1 grua, manter como está
        EnrichedOp = Op
    ),
    enrich_multi_crane_operations(Rest, RestAlloc, Resources, EnrichedRest).

% Pegar as primeiras N gruas da lista ordenada
take_n_cranes(0, _, []) :- !.
take_n_cranes(_, [], []) :- !.
take_n_cranes(N, [Crane | Rest], [Crane | Selected]) :-
    N > 0,
    N1 is N - 1,
    take_n_cranes(N1, Rest, Selected).

% Extrair códigos das gruas
extract_crane_codes([], []).
extract_crane_codes([_SetupTime-Crane | Rest], [Code | Codes]) :-
    get_dict(code, Crane, Code),
    extract_crane_codes(Rest, Codes).

% -----------------------------------------------------------------
% build_daily_response/9
% Constrói a resposta JSON do endpoint /api/scheduling/daily
% - Para "multi_crane" adiciona informação extra (baseline, crane-hours, etc.)
% - Para os restantes algoritmos mantém compatibilidade com o formato antigo.
% -----------------------------------------------------------------

build_daily_response("multi_crane",
                     DateStr,
                     VesselCount,
                     BaselineResult,
                     MultiResult,
                     ValidDelay,
                     ComputationTime,
                     EnrichedSchedule,
                     Response) :-
    !,
    % Baseline (1 grua) pode ser usado como comparação
    (   BaselineResult == none
    ->  BaselineDelay = 0
    ;   get_dict(total_delay, BaselineResult, BaselineDelay)
    ),

    (get_dict(strategy, MultiResult, Strategy) -> true ; Strategy = single_crane),
    (get_dict(crane_hours_single, MultiResult, CraneHoursSingle) -> true ; CraneHoursSingle = 0),
    (get_dict(crane_hours_multi, MultiResult, CraneHoursMulti) -> true ; CraneHoursMulti = 0),
    (get_dict(cranes_allocation, MultiResult, CranesAlloc) -> true ; CranesAlloc = []),

    Response = _{
        success:             true,
        date:                DateStr,
        algorithm:           "multi_crane",
        vessel_count:        VesselCount,

        % Mantém o campo total_delay para compatibilidade
        total_delay:         ValidDelay,

        % Campos extra específicos da US 3.4.5:
        strategy:            Strategy,
        baseline_delay:      BaselineDelay,
        crane_hours_single:  CraneHoursSingle,
        crane_hours_multi:   CraneHoursMulti,
        cranes_allocation:   CranesAlloc,

        computation_time_ms: ComputationTime,
        schedule:            EnrichedSchedule,
        warnings:            []
    }.


% Build response for genetic algorithm (US 4.3.1)
build_daily_response("genetic",
                     DateStr,
                     VesselCount,
                     _BaselineResult,
                     GeneticResult,
                     ValidDelay,
                     ComputationTime,
                     EnrichedSchedule,
                     Response) :-
    !,
    (get_dict(strategy, GeneticResult, Strategy) -> true ; Strategy = single_crane),
    (get_dict(cranes_allocation, GeneticResult, CranesAlloc) -> true ; CranesAlloc = []),
    (get_dict(parameters, GeneticResult, Parameters) -> true ; Parameters = _{}),
    (get_dict(baseline_delay, GeneticResult, BaselineDelay) -> true ; BaselineDelay = ValidDelay),
    
    Response = _{
        success:             true,
        date:                DateStr,
        algorithm:           "genetic",
        vessel_count:        VesselCount,
        total_delay:         ValidDelay,
        baseline_delay:      BaselineDelay,
        strategy:            Strategy,
        cranes_allocation:   CranesAlloc,
        ga_parameters:       Parameters,
        computation_time_ms: ComputationTime,
        schedule:            EnrichedSchedule,
        warnings:            []
    }.


% Build response for auto algorithm selection (US 4.3.2)
build_daily_response("auto",
                     DateStr,
                     VesselCount,
                     _BaselineResult,
                     AutoResult,
                     ValidDelay,
                     ComputationTime,
                     EnrichedSchedule,
                     Response) :-
    !,
    (get_dict(selected_algorithm, AutoResult, SelectedAlgorithm) -> true ; SelectedAlgorithm = "unknown"),
    (get_dict(selection_reason, AutoResult, SelectionReason) -> true ; SelectionReason = "unknown"),
    (get_dict(problem_analysis, AutoResult, ProblemAnalysis) -> true ; ProblemAnalysis = _{}),
    (get_dict(algorithm_computation_time_ms, AutoResult, AlgoCompTime) -> true ; AlgoCompTime = ComputationTime),
    
    Response = _{
        success:             true,
        date:                DateStr,
        algorithm:           "auto",
        selected_algorithm:  SelectedAlgorithm,
        selection_reason:    SelectionReason,
        problem_analysis:    ProblemAnalysis,
        vessel_count:        VesselCount,
        total_delay:         ValidDelay,
        computation_time_ms: ComputationTime,
        algorithm_computation_time_ms: AlgoCompTime,
        schedule:            EnrichedSchedule,
        warnings:            []
    }.


build_daily_response(Algorithm,
                     DateStr,
                     VesselCount,
                     _BaselineResult,
                     _AlgoResult,
                     ValidDelay,
                     ComputationTime,
                     EnrichedSchedule,
                     Response) :-
    % Caminho "normal" para "optimal" e "heuristic"
    Response = _{
        success:             true,
        date:                DateStr,
        algorithm:           Algorithm,
        vessel_count:        VesselCount,
        total_delay:         ValidDelay,
        computation_time_ms: ComputationTime,
        schedule:            EnrichedSchedule,
        warnings:            []
    }.


% ============================================================================
% TESTING INTERFACE
% ============================================================================

% Test data consumption from backend APIs
test_data_fetch(Date) :-
    format('Fetching operational data for: ~w~n', [Date]),
    fetch_scheduling_data(Date, Result),
    format('Result: ~w~n', [Result]).

% ============================================================================
% END OF MODULE
% ============================================================================

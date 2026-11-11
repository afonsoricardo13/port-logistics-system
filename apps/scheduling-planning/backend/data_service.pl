% ============================================================================
% Port Logistics - Data Service (Prolog)
% US 3.4.2 - Backend Data Consumption for Scheduling
% ============================================================================
% Consumes operational data from C# backend APIs and transforms it for scheduling
% AC: The scheduling computation must consume data from other APIs
%
% Author: LEI-ISEP Team
% Date: November 2025
% ============================================================================

:- module(data_service, [
    fetch_vessels_for_date/2,
    fetch_available_resources/1,
    transform_vessel_to_scheduling_format/2
]).

:- use_module(library(http/http_client)).
:- use_module(library(http/http_open)).
:- use_module(library(http/json)).
:- use_module(config).
:- use_module(ssl_config).

% HTTP options for backend requests
http_backend_options([
    timeout(30),
    request_header('X-User-Email'='admin@example.com'),
    cert_verify_hook(ssl_verify)
]).

% ============================================================================
% VESSEL DATA CONSUMPTION (US 3.4.2 - AC2: Vessel arrival/departure data)
% ============================================================================

% Fetch vessels scheduled for specific date
fetch_vessels_for_date(TargetDate, VesselsForScheduling) :-
    config:backend_api_url(BaseUrl),
    atom_concat(BaseUrl, '/vessel-visits/notifications', Url),
    http_backend_options(Options),
    
    catch(
        (
            http_open(Url, Stream, Options),
            json_read_dict(Stream, JsonData),
            close(Stream),
            
            % Filter vessels by date and transform to scheduling format
            (   is_list(JsonData)
            ->  filter_vessels_by_date(JsonData, TargetDate, FilteredVessels),
                maplist(transform_vessel_to_scheduling_format, FilteredVessels, VesselsForScheduling)
            ;   VesselsForScheduling = []
            )
        ),
        Error,
        (
            format('Error fetching vessels: ~w~n', [Error]),
            VesselsForScheduling = []
        )
    ).

% Filter vessels that arrive on target date AND have status = 2 (APPROVED)
filter_vessels_by_date([], _, []).

filter_vessels_by_date([Vessel | Rest], TargetDate, [Vessel | FilteredRest]) :-
    % Check if status is APPROVED (2 or "Approved")
    get_dict(status, Vessel, Status),
    is_approved_status(Status),
    % Extract arrival date from ISO datetime
    ArrivalDatetime = Vessel.get(estimatedTimeOfArrival),
    atom_string(ArrivalDatetime, ArrivalStr),
    sub_string(ArrivalStr, 0, 10, _, DatePart),
    atom_string(TargetDate, TargetDateStr),
    sub_string(TargetDateStr, 0, 10, _, TargetDatePart),
    DatePart = TargetDatePart,
    !,
    filter_vessels_by_date(Rest, TargetDate, FilteredRest).

filter_vessels_by_date([_ | Rest], TargetDate, FilteredRest) :-
    filter_vessels_by_date(Rest, TargetDate, FilteredRest).

% Check if status represents APPROVED
is_approved_status(Status) :-
    number(Status), 
    Status =:= 2, 
    !.
is_approved_status(Status) :-
    atom(Status),
    atom_string(Status, "Approved"),
    !.
is_approved_status(Status) :-
    atom(Status),
    atom_string(Status, "2"),
    !.
is_approved_status(Status) :-
    string(Status),
    Status = "Approved",
    !.
is_approved_status(Status) :-
    string(Status),
    Status = "2".

% Transform vessel from backend format to scheduling format
% Backend: {id, imoNumber, vesselName, arrivalDate, departureDate, ...}
% Scheduling: {id, arrival_time, departure_time, unload_time, load_time}
transform_vessel_to_scheduling_format(BackendVessel, SchedulingVessel) :-
    % Extract IMO as vessel identifier
    ImoNumber = BackendVessel.get(vesselImoNumber),
    
    % Convert ISO datetimes to absolute hours (including day offset from reference date)
    ArrivalDatetime = BackendVessel.get(estimatedTimeOfArrival),
    DepartureDatetime = BackendVessel.get(estimatedTimeOfDeparture),
    datetime_to_absolute_hours(ArrivalDatetime, ArrivalHours),
    datetime_to_absolute_hours(DepartureDatetime, DepartureHours),
    
    % Calculate operation times from container counts and crane capacity
    calculate_operation_times_from_containers(BackendVessel, UnloadHours, LoadHours),
    
    % Get assigned dock (if available)
    (   get_dict(assignedDockCode, BackendVessel, AssignedDock)
    ;   get_dict('AssignedDockCode', BackendVessel, AssignedDock)
    ;   AssignedDock = "DOCK-A" % Default fallback
    ), !,
    
    % Debug logging
    format(user_error, 'DEBUG VESSEL: IMO=~w, Arrival=~w, Departure=~w, Unload=~w, Load=~w~n',
           [ImoNumber, ArrivalHours, DepartureHours, UnloadHours, LoadHours]),
    
    % Build scheduling vessel format
    SchedulingVessel = _{
        id: ImoNumber,
        vessel_name: ImoNumber,
        arrival_time: ArrivalHours,
        departure_time: DepartureHours,
        unload_time: UnloadHours,
        load_time: LoadHours,
        assigned_dock: AssignedDock
    }.

% Convert ISO datetime to absolute hours from reference date (2025-11-20 00:00)
% "2025-11-20T01:20:00" -> 1.333 hours (1h 20min)
% "2025-11-28T01:20:00" -> 193.333 hours (8 days + 1h 20min)
datetime_to_absolute_hours(Datetime, AbsoluteHours) :-
    atom_string(Datetime, DatetimeStr),
    % Extract date components: YYYY-MM-DD
    sub_string(DatetimeStr, 0, 4, _, YearStr),
    sub_string(DatetimeStr, 5, 2, _, MonthStr),
    sub_string(DatetimeStr, 8, 2, _, DayStr),
    % Extract time components: HH:MM:SS
    sub_string(DatetimeStr, 11, 2, _, HourStr),
    sub_string(DatetimeStr, 14, 2, _, MinStr),
    
    % Convert to numbers
    atom_number(YearStr, Year),
    atom_number(MonthStr, Month),
    atom_number(DayStr, Day),
    atom_number(HourStr, Hour),
    atom_number(MinStr, Min),
    
    % Reference date: 2025-11-20 (Day 0)
    % Calculate day offset from reference date
    date_time_stamp(date(2025, 11, 20, 0, 0, 0, 0, -, -), ReferenceStamp),
    date_time_stamp(date(Year, Month, Day, 0, 0, 0, 0, -, -), CurrentStamp),
    DayOffset is (CurrentStamp - ReferenceStamp) / 86400, % 86400 seconds per day
    
    % Calculate total hours: (days * 24) + hours + (minutes / 60)
    AbsoluteHours is (DayOffset * 24) + Hour + (Min / 60).

% Calculate operation times from container counts and crane capacity
% US 3.4.2 AC: Use real data from database including crane setup time
% Formula: SetupTime + (TotalContainers / CapacityPerHour)
calculate_operation_times_from_containers(Vessel, UnloadHours, LoadHours) :-
    % Try multiple field name variations (backend may use different casing)
    (   get_dict(totalUnloadingContainers, Vessel, UnloadContainers)
    ;   get_dict('TotalUnloadingContainers', Vessel, UnloadContainers)
    ;   UnloadContainers = 0
    ), !,
    
    (   get_dict(loadingContainerCount, Vessel, LoadContainers)
    ;   get_dict('LoadingContainerCount', Vessel, LoadContainers)
    ;   LoadContainers = 0
    ), !,
    
    % Get assigned dock to find matching crane
    (   get_dict(assignedDockCode, Vessel, DockCode)
    ;   get_dict('AssignedDockCode', Vessel, DockCode)
    ;   DockCode = 'DOCK-A'
    ), !,
    
    % Fetch crane for this dock and get its capacity
    get_crane_capacity_for_dock(DockCode, CraneCapacity, SetupTimeHours),
    
    % Debug logging
    format(user_error, 'DEBUG: Vessel ~w - Unload: ~w, Load: ~w, Crane Capacity: ~w/h, Setup: ~wh~n', 
           [Vessel.get(id, unknown), UnloadContainers, LoadContainers, CraneCapacity, SetupTimeHours]),
    
    % Calculate total containers
    TotalContainers is UnloadContainers + LoadContainers,
    
    % Calculate operation time: Setup + (Containers / Capacity)
    (   TotalContainers > 0
    ->  OperationHours is SetupTimeHours + (TotalContainers / CraneCapacity),
        % Split evenly between unload and load (simplified)
        UnloadHours is OperationHours * (UnloadContainers / TotalContainers),
        LoadHours is OperationHours * (LoadContainers / TotalContainers)
    ;   UnloadHours = 0,
        LoadHours = 0
    ).

% Get crane capacity for specific dock
get_crane_capacity_for_dock(DockCode, Capacity, SetupTimeHours) :-
    fetch_cranes(Cranes),
    atom_string(DockCode, DockCodeStr),
    (   member(Crane, Cranes),
        get_dict(assignedArea, Crane, AssignedArea),
        atom_string(AssignedArea, AreaStr),
        AreaStr = DockCodeStr,
        get_dict(status, Crane, Status),
        atom_string(Status, 'Active')
    ->  % Get capacityPerHour and setupTimeMinutes
        Capacity = Crane.get(capacityPerHour, 30),
        SetupTimeMinutes = Crane.get(setupTimeMinutes, 30),
        SetupTimeHours is SetupTimeMinutes / 60
    ;   % Fallback to default values
        Capacity = 30,
        SetupTimeHours = 0.5
    ), !.

% ============================================================================
% RESOURCE DATA CONSUMPTION (US 3.4.2 - AC2.b,c,d)
% ============================================================================

% Fetch available resources for scheduling
fetch_available_resources(Resources) :-
    % Fetch data in parallel from multiple endpoints
    fetch_cranes(Cranes),
    fetch_staff(Staff),
    fetch_storage_areas(Storage),
    fetch_docks(Docks),
    
    % Aggregate resources
    Resources = json([
        cranes=Cranes,
        staff=Staff,
        storage=Storage,
        docks=Docks
    ]).

% Fetch physical resources (cranes) - AC2.b
fetch_cranes(Cranes) :-
    config:backend_api_url(BaseUrl),
    atom_concat(BaseUrl, '/PhysicalResources', Url),
    http_backend_options(Options),
    
    catch(
        (
            http_open(Url, Stream, Options),
            json_read_dict(Stream, JsonData),
            close(Stream),
            (   is_list(JsonData)
            ->  filter_cranes(JsonData, Cranes)
            ;   Cranes = []
            )
        ),
        _Error,
        Cranes = []
    ).

% Filter only crane resources and active ones
filter_cranes([], []).

filter_cranes([Resource | Rest], [Resource | FilteredRest]) :-
    Type = Resource.get(type),
    Status = Resource.get(status),
    atom_string(Type, TypeStr),
    atom_string(Status, StatusStr),
    (sub_string(TypeStr, _, _, _, "Crane") ; sub_string(TypeStr, _, _, _, "CRANE")),
    StatusStr = "Active",
    !,
    filter_cranes(Rest, FilteredRest).

filter_cranes([_ | Rest], FilteredRest) :-
    filter_cranes(Rest, FilteredRest).

% Fetch operating staff - AC2.d: Qualified staff
fetch_staff(Staff) :-
    config:backend_api_url(BaseUrl),
    atom_concat(BaseUrl, '/OperatingStaff', Url),
    http_backend_options(Options),
    
    catch(
        (
            http_open(Url, Stream, Options),
            json_read_dict(Stream, JsonData),
            close(Stream),
            (   is_list(JsonData)
            ->  Staff = JsonData
            ;   Staff = []
            )
        ),
        _Error,
        Staff = []
    ).

% Fetch storage areas - AC2.c
fetch_storage_areas(StorageAreas) :-
    config:backend_api_url(BaseUrl),
    atom_concat(BaseUrl, '/storage-areas', Url),
    http_backend_options(Options),
    
    catch(
        (
            http_open(Url, Stream, Options),
            json_read_dict(Stream, JsonData),
            close(Stream),
            (   is_list(JsonData)
            ->  StorageAreas = JsonData
            ;   StorageAreas = []
            )
        ),
        _Error,
        StorageAreas = []
    ).

% Fetch docks - AC2.a: One vessel per dock
fetch_docks(Docks) :-
    config:backend_api_url(BaseUrl),
    atom_concat(BaseUrl, '/Docks', Url),
    http_backend_options(Options),
    
    catch(
        (
            http_open(Url, Stream, Options),
            json_read_dict(Stream, JsonData),
            close(Stream),
            (   is_list(JsonData)
            ->  Docks = JsonData
            ;   Docks = []
            )
        ),
        _Error,
        Docks = []
    ).

% ============================================================================
% END OF MODULE
% ============================================================================

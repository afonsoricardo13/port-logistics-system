% ============================================================================
% Configuration - Planning & Scheduling API
% US 3.4.1 - REST API Infrastructure
% ============================================================================

:- module(config, [
    scheduling_server_port/1,
    backend_api_url/1,
    enable_debug_logging/1,
    log_api_calls/1,
    http_timeout_seconds/1,
    get_config/2,
    get_config/3
]).

% Server Configuration
% Use PORT environment variable if set (Azure), otherwise default to 5003
scheduling_server_port(Port) :-
    (   getenv('PORT', PortStr)
    ->  atom_number(PortStr, Port)
    ;   Port = 5003
    ).

% Backend API URL - consumes data from existing C# services
% Using HTTPS with SSL verification disabled for development
% Check environment variable first, fallback to localhost for dev
backend_api_url(Url) :-
    (   getenv('BACKEND_API_URL', Url)
    ->  true
    ;   Url = 'https://localhost:7096/api'
    ).

% Logging Configuration
enable_debug_logging(true).
log_api_calls(true).

% HTTP Configuration
http_timeout_seconds(30).

% Helper to get configuration values
get_config(Key, Value) :- call(Key, Value).

get_config(Key, Default, Value) :-
    (call(Key, V) -> Value = V ; Value = Default).

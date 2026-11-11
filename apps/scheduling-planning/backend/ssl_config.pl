% ============================================================================
% SSL Configuration - Accept all certificates for development
% ============================================================================
% This module provides a custom SSL verification hook that accepts all
% certificates, including self-signed ones. FOR DEVELOPMENT USE ONLY.
%
% Author: LEI-ISEP Team
% Date: November 2025
% ============================================================================

:- module(ssl_config, []).

:- use_module(library(http/http_ssl_plugin)).

% Define ssl_verify hook in user module (global scope)
:- multifile user:ssl_verify/5.
:- public user:ssl_verify/5.

% Accept all SSL certificates (for development with self-signed certs)
user:ssl_verify(_SSL, _ProblemCertificate, _AllCertificates, _FirstCertificate, _Error).

% ============================================================================
% END OF MODULE
% ============================================================================

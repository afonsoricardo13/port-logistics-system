% SSL Helper for accepting self-signed certificates
% Required for communicating with development backend

:- use_module(library(ssl)).

% Custom certificate validation that accepts any certificate
% Used for development/testing with self-signed certificates
cert_accept_any(_SSL, _ProblemCertificate, _AllCertificates, _FirstCertificate, _Error) :-
    % Always accept - for development only
    true.

% Setup SSL to accept all certificates
setup_ssl_for_development :-
    % This allows HTTP client to work with self-signed certificates
    set_setting(http:ssl_verify, cert_verify_hook(cert_accept_any)).

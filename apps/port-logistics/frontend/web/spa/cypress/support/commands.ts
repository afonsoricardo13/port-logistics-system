// ***********************************************
// Custom commands for Cypress tests
// ***********************************************

/**
 * Custom command to login as a specific role
 * @param role - User role (LogisticsOperator, PortAuthorityOfficer, etc.)
 */
Cypress.Commands.add('login', (role: string = 'LogisticsOperator') => {
  cy.session(role, () => {
    // Intercept auth API
    cy.intercept('GET', '**/api/auth/me', {
      statusCode: 200,
      body: {
        email: 'test@example.com',
        role: role,
        active: true
      }
    }).as('authMe');

    // Visit to create window object
    cy.visit('/');

    // Mock tokens
    const mockIdToken = btoa(JSON.stringify({ alg: 'RS256' })) + '.' +
                        btoa(JSON.stringify({
                          sub: 'test-user-123',
                          email: 'test@example.com',
                          name: 'Test User',
                          exp: Math.floor(Date.now() / 1000) + 3600
                        })) + '.mock-signature';

    cy.window().then((win) => {
      win.sessionStorage.setItem('google_id_token', mockIdToken);
      win.sessionStorage.setItem('google_access_token', 'mock-token');
      win.sessionStorage.setItem('google_user_profile', JSON.stringify({
        sub: 'test-user-123',
        email: 'test@example.com',
        name: 'Test User'
      }));
      win.sessionStorage.setItem('user_role', role);
      win.sessionStorage.setItem('user_active', 'true');
    });
  }, {
    validate() {
      cy.window().then((win) => {
        expect(win.sessionStorage.getItem('google_id_token')).to.exist;
      });
    }
  });

  // Re-intercept after session
  cy.intercept('GET', '**/api/auth/me', {
    statusCode: 200,
    body: {
      email: 'test@example.com',
      role: role,
      active: true
    }
  }).as('authMe');
});

/**
 * Custom command to logout
 */
Cypress.Commands.add('logout', () => {
  cy.window().then((win) => {
    win.sessionStorage.clear();
  });
});

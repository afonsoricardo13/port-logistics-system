// ***********************************************************
// This support file is processed and loaded automatically before test files.
// You can change the location of this file or turn off automatically serving
// support files with the 'supportFile' configuration option.
// ***********************************************************

import './commands';

// Prevent TypeScript errors
declare global {
  namespace Cypress {
    interface Chainable {
      login(role?: string): Chainable<void>;
      logout(): Chainable<void>;
    }
  }
}

// Block Google OAuth redirects globally
beforeEach(() => {
  cy.intercept('https://accounts.google.com/**', { statusCode: 200, body: '' });
});

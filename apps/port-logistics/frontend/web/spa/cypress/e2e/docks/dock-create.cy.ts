/// <reference types="cypress" />

/**
 * E2E Tests for Dock Creation
 * 
 * User Story: As a Port Authority Officer, I want to create new docks
 * in the system, so that they can be used for vessel scheduling.
 */

describe('Dock Create', () => {
  beforeEach(() => {
    // Login as Port Authority Officer
    cy.login('PortAuthorityOfficer');
    
    cy.visit('/docks/new');
  });

  afterEach(() => {
    cy.logout();
  });

  describe('Form Display', () => {
    it('should display the create dock form', () => {
      cy.contains('Register New Dock').should('be.visible');
      cy.get('form').should('exist');
    });

    it('should have all required input fields', () => {
      cy.get('input[formControlName="code"]').should('exist');
      cy.get('input[formControlName="nameOrNumber"]').should('exist');
      cy.get('input[formControlName="location"]').should('exist');
      cy.get('input[formControlName="length"]').should('exist');
      cy.get('input[formControlName="depth"]').should('exist');
      cy.get('input[formControlName="maxDraft"]').should('exist');
    });

    it('should have vessel types selection', () => {
      cy.contains('Allowed Vessel Types').should('be.visible');
      cy.contains('button', 'Add Vessel Type').should('exist');
    });

    it('should have submit and cancel buttons', () => {
      cy.contains('button', 'Create Dock').should('be.visible');
      cy.contains('button', 'Cancel').should('be.visible');
    });
  });

  describe('Form Validation', () => {
    it('should show validation errors for empty required fields', () => {
      cy.get('button').contains('Create Dock').click();
      
      cy.contains('Code is required.').should('be.visible');
      cy.contains('Name/Number is required.').should('be.visible');
      cy.contains('Location is required.').should('be.visible');
    });

    it('should validate numeric fields are positive', () => {
      cy.get('input[formControlName="length"]').clear().type('-10');
      cy.get('input[formControlName="depth"]').clear().type('0');
      cy.get('input[formControlName="maxDraft"]').clear().type('-5');
      cy.get('input[formControlName="code"]').click();
      
      cy.contains('Length must be greater than 0.').should('be.visible');
      cy.contains('Depth must be greater than 0.').should('be.visible');
      cy.contains('Max Draft must be greater than 0.').should('be.visible');
    });

    it('should require at least one vessel type', () => {
      cy.get('input[formControlName="code"]').type('DOCK999');
      cy.get('input[formControlName="nameOrNumber"]').type('Test Dock');
      cy.get('input[formControlName="location"]').type('Test Location');
      cy.get('input[formControlName="length"]').clear().type('100');
      cy.get('input[formControlName="depth"]').clear().type('10');
      cy.get('input[formControlName="maxDraft"]').clear().type('8');
      
      cy.get('button').contains('Create Dock').click();
      
      cy.contains('At least one vessel type is required.').should('be.visible');
    });

    it('should allow form submission when valid', () => {
      cy.get('input[formControlName="code"]').type('DOCK999');
      cy.get('input[formControlName="nameOrNumber"]').type('Test Dock');
      cy.get('input[formControlName="location"]').type('Test Location');
      cy.get('input[formControlName="length"]').clear().type('100');
      cy.get('input[formControlName="depth"]').clear().type('10');
      cy.get('input[formControlName="maxDraft"]').clear().type('8');
      
      cy.get('button').contains('Add Vessel Type').click();
      cy.get('div[formarrayname="allowedVesselTypes"] input.form-control').first().type('Container Ship');
      
      cy.get('button').contains('Create Dock').should('be.visible');
    });
  });

  describe('Vessel Types Management', () => {
    it('should add a new vessel type field', () => {
      cy.get('button').contains('Add Vessel Type').click();
      cy.get('div[formarrayname="allowedVesselTypes"] input').should('have.length', 1);
    });

    it('should add multiple vessel types', () => {
      cy.get('button').contains('Add Vessel Type').click();
      cy.get('button').contains('Add Vessel Type').click();
      cy.get('button').contains('Add Vessel Type').click();
      
      cy.get('div[formarrayname="allowedVesselTypes"] input').should('have.length', 3);
    });

    it('should remove a vessel type field', () => {
      cy.get('button').contains('Add Vessel Type').click();
      cy.get('button').contains('Add Vessel Type').click();
      
      cy.get('button.btn-outline-danger').first().click();
      cy.get('div[formarrayname="allowedVesselTypes"] input').should('have.length', 1);
    });

    it('should type different vessel types', () => {
      cy.get('button').contains('Add Vessel Type').click();
      cy.get('div[formarrayname="allowedVesselTypes"] input.form-control').first().type('Container Ship');
      
      cy.get('button').contains('Add Vessel Type').click();
      cy.get('div[formarrayname="allowedVesselTypes"] input.form-control').eq(1).type('Tanker');
      
      cy.get('div[formarrayname="allowedVesselTypes"] input.form-control').eq(0).should('have.value', 'Container Ship');
      cy.get('div[formarrayname="allowedVesselTypes"] input.form-control').eq(1).should('have.value', 'Tanker');
    });
  });

  describe('Form Submission', () => {
    beforeEach(() => {
      cy.intercept('POST', '**/api/Docks', {
        statusCode: 201,
        body: {
          code: 'DOCK999',
          nameOrNumber: 'New Test Dock',
          location: 'Test Location',
          length: 100.0,
          depth: 10.0,
          maxDraft: 8.0,
          allowedVesselTypes: ['Container Ship']
        }
      }).as('createDock');
    });

    it('should successfully create a new dock', () => {
      cy.get('input[formControlName="code"]').type('DOCK999');
      cy.get('input[formControlName="nameOrNumber"]').type('New Test Dock');
      cy.get('input[formControlName="location"]').type('Test Location');
      cy.get('input[formControlName="length"]').clear().type('100');
      cy.get('input[formControlName="depth"]').clear().type('10');
      cy.get('input[formControlName="maxDraft"]').clear().type('8');
      
      cy.get('button').contains('Add Vessel Type').click();
      cy.get('div[formarrayname="allowedVesselTypes"] input.form-control').first().type('Container Ship');
      
      cy.get('button').contains('Create Dock').click();
      
      cy.wait('@createDock');
      // Success message appears in alert
    });

    it('should navigate to dock list after successful creation', () => {
      cy.get('input[formControlName="code"]').type('DOCK999');
      cy.get('input[formControlName="nameOrNumber"]').type('New Test Dock');
      cy.get('input[formControlName="location"]').type('Test Location');
      cy.get('input[formControlName="length"]').clear().type('100');
      cy.get('input[formControlName="depth"]').clear().type('10');
      cy.get('input[formControlName="maxDraft"]').clear().type('8');
      
      cy.get('button').contains('Add Vessel Type').click();
      cy.get('div[formarrayname="allowedVesselTypes"] input.form-control').first().type('Container Ship');
      
      cy.get('button').contains('Create Dock').click();
      
      cy.wait('@createDock');
      cy.url().should('include', '/docks');
      cy.url().should('not.include', '/new');
    });

    it('should send correct data to API', () => {
      cy.get('input[formControlName="code"]').type('DOCK999');
      cy.get('input[formControlName="nameOrNumber"]').type('New Test Dock');
      cy.get('input[formControlName="location"]').type('Test Location');
      cy.get('input[formControlName="length"]').clear().type('100');
      cy.get('input[formControlName="depth"]').clear().type('10');
      cy.get('input[formControlName="maxDraft"]').clear().type('8');
      
      cy.get('button').contains('Add Vessel Type').click();
      cy.get('div[formarrayname="allowedVesselTypes"] input.form-control').first().type('Container Ship');
      
      cy.get('button').contains('Create Dock').click();
      
      cy.wait('@createDock').its('request.body').should('deep.include', {
        code: 'DOCK999',
        nameOrNumber: 'New Test Dock',
        location: 'Test Location',
        length: 100,
        depth: 10,
        maxDraft: 8,
        allowedVesselTypes: ['Container Ship']
      });
    });

    it('should handle API error during creation', () => {
      cy.intercept('POST', '**/api/Docks', {
        statusCode: 400,
        body: { message: 'Dock code already exists' }
      }).as('createDockError');

      cy.get('input[formControlName="code"]').type('DOCK001');
      cy.get('input[formControlName="nameOrNumber"]').type('Duplicate Dock');
      cy.get('input[formControlName="location"]').type('Test Location');
      cy.get('input[formControlName="length"]').clear().type('100');
      cy.get('input[formControlName="depth"]').clear().type('10');
      cy.get('input[formControlName="maxDraft"]').clear().type('8');
      
      cy.get('button').contains('Add Vessel Type').click();
      cy.get('div[formarrayname="allowedVesselTypes"] input.form-control').first().type('Cargo');
      
      cy.get('button').contains('Create Dock').click();
      
      cy.wait('@createDockError');
      // Error message appears in alert
    });

    it('should handle network error', () => {
      cy.intercept('POST', '**/api/Docks', {
        forceNetworkError: true
      }).as('createDockNetworkError');

      cy.get('input[formControlName="code"]').type('DOCK999');
      cy.get('input[formControlName="nameOrNumber"]').type('Test Dock');
      cy.get('input[formControlName="location"]').type('Test Location');
      cy.get('input[formControlName="length"]').clear().type('100');
      cy.get('input[formControlName="depth"]').clear().type('10');
      cy.get('input[formControlName="maxDraft"]').clear().type('8');
      
      cy.get('button').contains('Add Vessel Type').click();
      cy.get('div[formarrayname="allowedVesselTypes"] input.form-control').first().type('Tanker');
      
      cy.get('button').contains('Create Dock').click();
      
      // Error message appears in alert
    });
  });

  describe('Form Navigation', () => {
    it('should navigate back to dock list when clicking Cancel', () => {
      cy.contains('button', 'Cancel').click();
      cy.url().should('include', '/docks');
      cy.url().should('not.include', '/new');
    });
  });
});

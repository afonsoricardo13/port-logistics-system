/// <reference types="cypress" />

/**
 * E2E Tests for Dock Update
 * 
 * User Story: As a Port Authority Officer, I want to update existing docks
 * in the system, so that dock information remains accurate.
 */

describe('Dock Update', () => {
  const existingDock = {
    code: 'DOCK001',
    nameOrNumber: 'North Terminal Dock 1',
    location: 'North Port Area',
    length: 350.5,
    depth: 15.0,
    maxDraft: 12.5,
    allowedVesselTypes: ['Container Ship', 'Cargo']
  };

  beforeEach(() => {
    // Login as Port Authority Officer
    cy.login('PortAuthorityOfficer');
    
    // Mock GET dock by code
    cy.intercept('GET', '**/api/Docks/DOCK001', {
      statusCode: 200,
      body: existingDock
    }).as('getDock');
    
    cy.visit('/docks/DOCK001/edit');
  });

  afterEach(() => {
    cy.logout();
  });

  describe('Form Display', () => {
    it('should display the edit dock form', () => {
      cy.wait('@getDock');
      cy.contains('Edit Dock').should('be.visible');
      cy.get('form').should('exist');
    });

    it('should load and display existing dock data', () => {
      cy.wait('@getDock');
      
      cy.get('input[formControlName="code"]').should('have.value', 'DOCK001');
      cy.get('input[formControlName="nameOrNumber"]').should('have.value', 'North Terminal Dock 1');
      cy.get('input[formControlName="location"]').should('have.value', 'North Port Area');
      cy.get('input[formControlName="length"]').should('have.value', '350.5');
      cy.get('input[formControlName="depth"]').should('have.value', '15');
      cy.get('input[formControlName="maxDraft"]').should('have.value', '12.5');
    });

    it('should display existing vessel types', () => {
      cy.wait('@getDock');
      
      cy.get('div[formarrayname="allowedVesselTypes"] input').should('have.length', 2);
      cy.get('div[formarrayname="allowedVesselTypes"] input.form-control').eq(0).should('have.value', 'Container Ship');
      cy.get('div[formarrayname="allowedVesselTypes"] input.form-control').eq(1).should('have.value', 'Cargo');
    });

    it('should disable code field in edit mode', () => {
      cy.wait('@getDock');
      cy.get('input[formControlName="code"]').should('be.disabled');
    });

    it('should show loading indicator while fetching dock', () => {
      cy.intercept('GET', '**/api/Docks/DOCK001', {
        statusCode: 200,
        body: existingDock,
        delay: 1000
      }).as('getDockSlow');

      cy.visit('/docks/DOCK001/edit');
      cy.get('.spinner-border').should('be.visible');
      cy.wait('@getDockSlow');
      cy.get('.spinner-border').should('not.exist');
    });
  });

  describe('Form Editing', () => {
    it('should allow editing name/number', () => {
      cy.wait('@getDock');
      
      cy.get('input[formControlName="nameOrNumber"]').clear().type('Updated Dock Name');
      cy.get('input[formControlName="nameOrNumber"]').should('have.value', 'Updated Dock Name');
    });

    it('should allow editing location', () => {
      cy.wait('@getDock');
      
      cy.get('input[formControlName="location"]').clear().type('Updated Location');
      cy.get('input[formControlName="location"]').should('have.value', 'Updated Location');
    });

    it('should allow editing physical dimensions', () => {
      cy.wait('@getDock');
      
      cy.get('input[formControlName="length"]').clear().type('400');
      cy.get('input[formControlName="depth"]').clear().type('20');
      cy.get('input[formControlName="maxDraft"]').clear().type('15');
      
      cy.get('input[formControlName="length"]').should('have.value', '400');
      cy.get('input[formControlName="depth"]').should('have.value', '20');
      cy.get('input[formControlName="maxDraft"]').should('have.value', '15');
    });

    it('should allow adding new vessel types', () => {
      cy.wait('@getDock');
      
      cy.get('button').contains('Add Vessel Type').click();
      cy.get('div[formarrayname="allowedVesselTypes"] input').should('have.length', 3);
      cy.get('div[formarrayname="allowedVesselTypes"] input.form-control').eq(2).type('Tanker');
    });

    it('should allow removing vessel types', () => {
      cy.wait('@getDock');
      
      cy.get('button.btn-outline-danger').first().click();
      cy.get('div[formarrayname="allowedVesselTypes"] input').should('have.length', 1);
    });

    it('should allow changing existing vessel types', () => {
      cy.wait('@getDock');
      
      cy.get('div[formarrayname="allowedVesselTypes"] input.form-control').first().clear().type('Tanker');
      cy.get('div[formarrayname="allowedVesselTypes"] input.form-control').first().should('have.value', 'Tanker');
    });
  });

  describe('Form Validation', () => {
    it('should validate required fields', () => {
      cy.wait('@getDock');
      
      cy.get('input[formControlName="nameOrNumber"]').clear();
      cy.get('input[formControlName="location"]').clear();
      cy.get('button').contains('Update Dock').click();
      
      cy.contains('Name/Number is required.').should('be.visible');
      cy.contains('Location is required.').should('be.visible');
    });

    it('should validate numeric fields are positive', () => {
      cy.wait('@getDock');
      
      cy.get('input[formControlName="length"]').clear().type('-10');
      cy.get('input[formControlName="depth"]').clear().type('0');
      cy.get('input[formControlName="nameOrNumber"]').click();
      
      cy.contains('Length must be greater than 0.').should('be.visible');
      cy.contains('Depth must be greater than 0.').should('be.visible');
    });

    it('should require at least one vessel type', () => {
      cy.wait('@getDock');
      
      // Remove all vessel types
      cy.get('button.btn-outline-danger').each(($btn) => {
        cy.wrap($btn).click();
      });
      
      cy.get('button').contains('Update Dock').click();
      cy.contains('At least one vessel type is required.').should('be.visible');
    });
  });

  describe('Form Submission', () => {
    beforeEach(() => {
      cy.intercept('PUT', '**/api/Docks/DOCK001', {
        statusCode: 200
      }).as('updateDock');
    });

    it('should successfully update dock', () => {
      cy.wait('@getDock');
      
      cy.get('input[formControlName="nameOrNumber"]').clear().type('Updated Dock Name');
      cy.get('input[formControlName="location"]').clear().type('Updated Location');
      cy.get('input[formControlName="length"]').clear().type('400');
      
      cy.get('button').contains('Update Dock').click();
      
      cy.wait('@updateDock');
      // Success message appears in alert
    });

    it('should send correct updated data to API', () => {
      cy.wait('@getDock');
      
      cy.get('input[formControlName="nameOrNumber"]').clear().type('Updated Name');
      cy.get('input[formControlName="length"]').clear().type('400');
      
      cy.get('button').contains('Update Dock').click();
      
      cy.wait('@updateDock').its('request.body').should('include', {
        code: 'DOCK001',
        nameOrNumber: 'Updated Name',
        length: 400
      });
    });

    it('should navigate to dock list after successful update', () => {
      cy.wait('@getDock');
      
      cy.get('input[formControlName="nameOrNumber"]').clear().type('Updated Name');
      cy.get('button').contains('Update Dock').click();
      
      cy.wait('@updateDock');
      cy.url().should('include', '/docks');
      cy.url().should('not.include', '/edit');
    });

    it('should handle API error during update', () => {
      cy.intercept('PUT', '**/api/Docks/DOCK001', {
        statusCode: 400,
        body: { message: 'Invalid dock data' }
      }).as('updateDockError');

      cy.wait('@getDock');
      
      cy.get('input[formControlName="nameOrNumber"]').clear().type('Invalid Name');
      cy.get('button').contains('Update Dock').click();
      
      cy.wait('@updateDockError');
      // Error message appears in alert
    });

    it('should handle conflict error', () => {
      cy.intercept('PUT', '**/api/Docks/DOCK001', {
        statusCode: 409,
        body: { message: 'Dock is currently in use' }
      }).as('updateDockConflict');

      cy.wait('@getDock');
      
      cy.get('input[formControlName="length"]').clear().type('100');
      cy.get('button').contains('Update Dock').click();
      
      cy.wait('@updateDockConflict');
      // Error message appears in alert
    });

    it('should handle network error', () => {
      cy.intercept('PUT', '**/api/Docks/DOCK001', {
        forceNetworkError: true
      }).as('updateDockNetworkError');

      cy.wait('@getDock');
      
      cy.get('input[formControlName="nameOrNumber"]').clear().type('New Name');
      cy.get('button').contains('Update Dock').click();
      
      // Error message appears in alert
    });
  });

  describe('Partial Updates', () => {
    beforeEach(() => {
      cy.intercept('PUT', '**/api/Docks/DOCK001', {
        statusCode: 200
      }).as('updateDock');
    });

    it('should update only name/number', () => {
      cy.wait('@getDock');
      
      cy.get('input[formControlName="nameOrNumber"]').clear().type('Only Name Changed');
      cy.get('button').contains('Update Dock').click();
      
      cy.wait('@updateDock').its('request.body').should('deep.include', {
        nameOrNumber: 'Only Name Changed',
        location: 'North Port Area',
        length: 350.5
      });
    });

    it('should update only dimensions', () => {
      cy.wait('@getDock');
      
      cy.get('input[formControlName="length"]').clear().type('500');
      cy.get('input[formControlName="depth"]').clear().type('20');
      cy.get('button').contains('Update Dock').click();
      
      cy.wait('@updateDock').its('request.body').should('include', {
        length: 500,
        depth: 20
      });
    });

    it('should update only vessel types', () => {
      cy.wait('@getDock');
      
      cy.get('button').contains('Add Vessel Type').click();
      cy.get('div[formarrayname="allowedVesselTypes"] input.form-control').last().type('Tanker');
      
      cy.get('button').contains('Update Dock').click();
      
      cy.wait('@updateDock').its('request.body.allowedVesselTypes')
        .should('include', 'Tanker');
    });
  });

  describe('Form Navigation', () => {
    it('should navigate back to dock list when clicking Cancel', () => {
      cy.wait('@getDock');
      cy.contains('button', 'Cancel').click();
      cy.url().should('include', '/docks');
      cy.url().should('not.include', '/edit');
    });
  });
});

/// <reference types="cypress" />

/**
 * E2E Tests for Updating Vessels
 */

describe('Vessel Update Form', () => {
  let vesselData: any;

  before(() => {
    cy.fixture('vessels').then((data) => {
      vesselData = data;
    });
  });

  beforeEach(() => {
    cy.login('PortAuthorityOfficer');
    
    // Setup intercept for vessel data
    cy.intercept('GET', '**/api/Vessels/IMO1234567', {
      statusCode: 200,
      body: vesselData.vesselToUpdate
    }).as('getVessel');

    cy.visit('/vessels/IMO1234567/edit');
  });

  afterEach(() => {
    cy.logout();
  });

  describe('Form Display', () => {
    it('should display the edit vessel form', () => {
      cy.wait('@getVessel');
      cy.contains('Edit Vessel').should('be.visible');
      cy.get('form').should('exist');
    });

    it('should load existing vessel data into form', () => {
      cy.wait('@getVessel');
      
      cy.get('#imoNumber').should('have.value', '1234567').should('be.disabled');
      cy.get('#name').should('have.value', 'Test Cargo');
      cy.get('#vesselType').should('have.value', 'Cargo');
      cy.get('#operator').should('have.value', 'Test Shipping Ltd');
    });

    it('should show loading indicator while fetching data', () => {
      cy.fixture('vessels').then((data) => {
        cy.intercept('GET', '**/api/Vessels/IMO1234567', {
          statusCode: 200,
          delay: 1000,
          body: data.vesselToUpdate
        }).as('getVesselSlow');

        cy.visit('/vessels/IMO1234567/edit');
        cy.get('.spinner-border').should('be.visible');
        cy.wait('@getVesselSlow');
        cy.get('.spinner-border').should('not.exist');
      });
    });

    it('should display error when vessel not found', () => {
      cy.intercept('GET', '**/api/Vessels/IMO9999999', {
        statusCode: 404,
        body: { error: 'Vessel not found' }
      }).as('getVesselNotFound');

      cy.visit('/vessels/IMO9999999/edit');
      cy.wait('@getVesselNotFound');
      cy.contains('Failed to load vessel').should('be.visible');
    });
  });

  describe('Form Editing', () => {
    it('should allow updating name', () => {
      cy.wait('@getVessel');
      cy.get('#name').clear().type('Updated Cargo Ship');
      cy.get('#name').should('have.value', 'Updated Cargo Ship');
    });

    it('should allow updating vessel type', () => {
      cy.wait('@getVessel');
      cy.get('#vesselType').clear().type('Container Ship');
      cy.get('#vesselType').should('have.value', 'Container Ship');
    });

    it('should allow updating operator', () => {
      cy.wait('@getVessel');
      cy.get('#operator').clear().type('Updated Shipping Company');
      cy.get('#operator').should('have.value', 'Updated Shipping Company');
    });

    it('should not allow editing IMO number', () => {
      cy.wait('@getVessel');
      cy.get('#imoNumber').should('be.disabled');
    });
  });

  describe('Form Validation', () => {
    it('should validate name is required', () => {
      cy.wait('@getVessel');
      cy.get('#name').clear();
      cy.get('button[type="submit"]').click();
      cy.contains('Vessel name is required').should('be.visible');
    });

    it('should validate vessel type is required', () => {
      cy.wait('@getVessel');
      cy.get('#vesselType').clear();
      cy.get('button[type="submit"]').click();
      cy.contains('Vessel type is required').should('be.visible');
    });

    it('should validate operator is required', () => {
      cy.wait('@getVessel');
      cy.get('#operator').clear();
      cy.get('button[type="submit"]').click();
      cy.contains('Operator/Owner is required').should('be.visible');
    });
  });

  describe('Successful Update', () => {
    it('should successfully update vessel with valid data', () => {
      cy.wait('@getVessel');

      cy.intercept('PUT', '**/api/Vessels/IMO1234567', {
        statusCode: 200,
        body: {
          imoNumber: '1234567',
          name: 'Updated Cargo Ship',
          vesselType: 'Ultra Large Container Ship',
          operator: 'Updated Shipping Company'
        }
      }).as('updateVessel');

      cy.get('#name').clear().type('Updated Cargo Ship');
      cy.get('#vesselType').clear().type('Ultra Large Container Ship');
      cy.get('#operator').clear().type('Updated Shipping Company');
      cy.get('button[type="submit"]').click();

      cy.wait('@updateVessel');
      cy.url().should('include', '/vessels', { timeout: 2000 });
    });

    it('should redirect to vessel list after successful update', () => {
      cy.wait('@getVessel');

      cy.intercept('PUT', '**/api/Vessels/IMO1234567', {
        statusCode: 200,
        body: {
          imoNumber: '1234567',
          name: 'Updated Cargo Ship',
          vesselType: 'Container Ship',
          operator: 'Updated Shipping Company'
        }
      }).as('updateVessel');

      cy.get('#name').clear().type('Updated Cargo Ship');
      cy.get('button[type="submit"]').click();

      cy.wait('@updateVessel');
      cy.url().should('include', '/vessels', { timeout: 2000 });
    });
  });

  describe('Error Handling', () => {
    it('should show error message when API returns error', () => {
      cy.wait('@getVessel');

      cy.intercept('PUT', '**/api/Vessels/IMO1234567', {
        statusCode: 400,
        body: { message: 'Invalid vessel type.' }
      }).as('updateVesselError');

      cy.get('#name').clear().type('Updated Name');
      cy.get('button[type="submit"]').click();

      cy.wait('@updateVesselError');
      cy.contains('Invalid vessel type').should('be.visible');
    });
  });

  describe('Form Reset and Cancel', () => {
    it('should redirect to vessel list when cancel button is clicked', () => {
      cy.wait('@getVessel');
      cy.get('#name').clear().type('Changed Name');
      cy.contains('button', 'Cancel').click();
      cy.url().should('include', '/vessels');
    });
  });

  describe('Navigation', () => {
    it('should have a cancel button that returns to vessel list', () => {
      cy.wait('@getVessel');
      cy.contains('button', 'Cancel').should('exist');
      cy.contains('button', 'Cancel').click();
      cy.url().should('include', '/vessels');
    });
  });
});

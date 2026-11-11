/// <reference types="cypress" />

/**
 * E2E Tests for Creating Vessels
 */

describe('Vessel Create Form', () => {
  beforeEach(() => {
    cy.login('PortAuthorityOfficer');
    cy.visit('/vessels/new');
  });

  afterEach(() => {
    cy.logout();
  });

  describe('Form Display', () => {
    it('should display the create vessel form', () => {
      cy.url().should('include', '/vessels/new');
      cy.get('.card-header h3', { timeout: 10000 }).should('contain', 'Register New Vessel');
      cy.get('form').should('exist');
    });

    it('should display all required form fields', () => {
      cy.get('#imoNumber').should('exist');
      cy.get('#name').should('exist');
      cy.get('#vesselType').should('exist');
      cy.get('#operator').should('exist');
    });

    it('should have empty form fields initially', () => {
      cy.get('#imoNumber').should('have.value', '');
      cy.get('#name').should('have.value', '');
      cy.get('#vesselType').should('have.value', '');
      cy.get('#operator').should('have.value', '');
    });
  });

  describe('Form Validation', () => {
    it('should show error when submitting empty form', () => {
      cy.get('button[type="submit"]').click();
      cy.contains('IMO number is required').should('be.visible');
    });

    it('should validate IMO number format', () => {
      cy.get('#imoNumber').type('123');
      cy.get('#imoNumber').blur();
      cy.contains('IMO number must be exactly 7 digits').should('be.visible');
    });

    it('should validate name is required', () => {
      cy.get('#imoNumber').type('9074729');
      cy.get('#vesselType').type('Cargo');
      cy.get('#operator').type('Test Operator');
      cy.get('button[type="submit"]').click();
      cy.contains('Vessel name is required').should('be.visible');
    });

    it('should validate vessel type is required', () => {
      cy.get('#imoNumber').type('9074729');
      cy.get('#name').type('Test Ship');
      cy.get('#operator').type('Test Operator');
      cy.get('button[type="submit"]').click();
      cy.contains('Vessel type is required').should('be.visible');
    });

    it('should validate operator is required', () => {
      cy.get('#imoNumber').type('9074729');
      cy.get('#name').type('Test Ship');
      cy.get('#vesselType').type('Cargo');
      cy.get('button[type="submit"]').click();
      cy.contains('Operator/Owner is required').should('be.visible');
    });

    it('should show valid IMO number feedback', () => {
      cy.get('#imoNumber').type('9074729');
      cy.get('#imoNumber').blur();
      cy.contains('Valid IMO number').should('be.visible');
    });
  });

  describe('Successful Creation', () => {
    it('should successfully create a vessel with valid data', () => {
      cy.fixture('vessels.json').then((data) => {
        cy.intercept('POST', '**/api/Vessels', {
          statusCode: 201,
          body: data.validVessel
        }).as('createVessel');

        cy.get('#imoNumber').type(data.validVessel.imoNumber);
        cy.get('#name').type(data.validVessel.name);
        cy.get('#vesselType').type(data.validVessel.vesselType);
        cy.get('#operator').type(data.validVessel.operator);
        cy.get('button[type="submit"]').click();

        cy.wait('@createVessel');
        cy.url().should('include', '/vessels', { timeout: 3000 });
      });
    });

    it('should redirect to vessel list after successful creation', () => {
      cy.fixture('vessels.json').then((data) => {
        cy.intercept('POST', '**/api/Vessels', {
          statusCode: 201,
          body: data.validVessel
        }).as('createVessel');

        cy.get('#imoNumber').type(data.validVessel.imoNumber);
        cy.get('#name').type(data.validVessel.name);
        cy.get('#vesselType').type(data.validVessel.vesselType);
        cy.get('#operator').type(data.validVessel.operator);
        cy.get('button[type="submit"]').click();

        cy.wait('@createVessel');
        cy.url().should('include', '/vessels', { timeout: 3000 });
      });
    });

    it('should display all vessel information in the form', () => {
      cy.get('#imoNumber').type('9074729');
      cy.get('#name').type('MSC Oscar');
      cy.get('#vesselType').type('Container Ship');
      cy.get('#operator').type('Mediterranean Shipping Company');

      cy.get('#imoNumber').should('have.value', '9074729');
      cy.get('#name').should('have.value', 'MSC Oscar');
      cy.get('#vesselType').should('have.value', 'Container Ship');
      cy.get('#operator').should('have.value', 'Mediterranean Shipping Company');
    });
  });

  describe('Error Handling', () => {
    it('should show error message when API returns error', () => {
      cy.intercept('POST', '**/api/Vessels', {
        statusCode: 400,
        body: { message: 'Vessel with this IMO number already exists.' }
      }).as('createVesselError');

      cy.get('#imoNumber').type('9074729');
      cy.get('#name').type('Test Ship');
      cy.get('#vesselType').type('Cargo');
      cy.get('#operator').type('Test Operator');
      cy.get('button[type="submit"]').click();

      cy.wait('@createVesselError');
      cy.get('.alert-danger').should('be.visible');
    });

    it('should show error when server is unavailable', () => {
      cy.intercept('POST', '**/api/Vessels', {
        statusCode: 500,
        body: { message: 'Internal Server Error' }
      }).as('serverError');

      cy.get('#imoNumber').type('9074729');
      cy.get('#name').type('Test Ship');
      cy.get('#vesselType').type('Cargo');
      cy.get('#operator').type('Test Operator');
      cy.get('button[type="submit"]').click();

      cy.wait('@serverError');
      cy.get('.alert-danger').should('be.visible');
    });
  });

  describe('Form Reset', () => {
    it('should clear form when cancel button is clicked', () => {
      cy.get('#imoNumber').type('9074729');
      cy.get('#name').type('Test Ship');
      
      cy.contains('button', 'Cancel').click();
      cy.url().should('include', '/vessels');
    });

    it('should allow creating another vessel after canceling', () => {
      cy.get('#imoNumber').type('9074729');
      cy.contains('button', 'Cancel').click();
      
      cy.visit('/vessels/new');
      cy.get('#imoNumber').should('have.value', '');
    });
  });

  describe('Navigation', () => {
    it('should have a cancel button that returns to vessel list', () => {
      cy.contains('button', 'Cancel').should('exist');
      cy.contains('button', 'Cancel').click();
      cy.url().should('include', '/vessels');
    });
  });
});

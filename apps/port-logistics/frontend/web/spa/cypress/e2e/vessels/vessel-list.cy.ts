/// <reference types="cypress" />

/**
 * E2E Tests for Vessel List
 * 
 * User Story: As a Logistics Operator, I want to manage vessels
 * (create, update, list), so that the system can accurately track
 * vessel information for scheduling and logistics operations.
 */

describe('Vessel List', () => {
  beforeEach(() => {
    // Login as Port Authority Officer
    cy.login('PortAuthorityOfficer');
    
    // Mock API responses
    cy.intercept('GET', '**/api/Vessels*', {
      statusCode: 200,
      body: [
        {
          imoNumber: '9074729',
          name: 'MSC Oscar',
          vesselType: 'Container Ship',
          operator: 'Mediterranean Shipping Company'
        },
        {
          imoNumber: '1234567',
          name: 'Test Cargo',
          vesselType: 'Cargo',
          operator: 'Test Shipping Ltd'
        },
        {
          imoNumber: '9999999',
          name: 'Pacific Explorer',
          vesselType: 'Tanker',
          operator: 'Pacific Shipping Co'
        }
      ]
    }).as('getVesselList');
    
    cy.visit('/vessels');
  });

  afterEach(() => {
    cy.logout();
  });

  describe('List View', () => {
    it('should display the vessel list page', () => {
      cy.wait('@getVesselList');
      cy.contains('Vessels Management').should('be.visible');
      cy.get('table').should('exist');
    });

    it('should display all vessels from the API', () => {
      cy.wait('@getVesselList');
      cy.get('tbody tr').should('have.length', 3);
    });

    it('should display vessel details correctly', () => {
      cy.wait('@getVesselList');
      
      // Check first vessel
      cy.get('tbody tr').first().within(() => {
        cy.contains('9074729').should('be.visible');
        cy.contains('MSC Oscar').should('be.visible');
        cy.contains('Container Ship').should('be.visible');
        cy.contains('Mediterranean Shipping Company').should('be.visible');
      });
    });

    it('should show loading indicator while fetching data', () => {
      cy.intercept('GET', '**/api/Vessels*', {
        statusCode: 200,
        body: [],
        delay: 1000
      }).as('getVesselListSlow');

      cy.visit('/vessels');
      cy.get('.spinner-border').should('be.visible');
      cy.wait('@getVesselListSlow');
      cy.get('.spinner-border').should('not.exist');
    });

    it('should display error message when API fails', () => {
      cy.intercept('GET', '**/api/Vessels*', {
        statusCode: 500,
        body: { message: 'Internal Server Error' }
      }).as('getVesselListError');

      cy.visit('/vessels');
      cy.wait('@getVesselListError');
      cy.get('.alert-danger').should('be.visible');
    });

    it('should display empty state when no vessels exist', () => {
      cy.intercept('GET', '**/api/Vessels*', {
        statusCode: 200,
        body: []
      }).as('getEmptyVesselList');

      cy.visit('/vessels');
      cy.wait('@getEmptyVesselList');
      cy.contains('No vessels found').should('be.visible');
    });
  });

  describe('Search and Filter', () => {
    it('should filter vessels by name', () => {
      cy.wait('@getVesselList');
      cy.get('#filterName').type('Oscar');
      cy.get('tbody tr').should('have.length', 1);
      cy.get('tbody tr').first().should('contain', 'MSC Oscar');
    });

    it('should filter vessels by IMO number', () => {
      cy.wait('@getVesselList');
      cy.get('#filterImo').type('9074729');
      cy.get('tbody tr').should('have.length', 1);
      cy.get('tbody tr').first().should('contain', '9074729');
    });

    it('should filter vessels by operator', () => {
      cy.wait('@getVesselList');
      cy.get('#filterOperator').type('Mediterranean');
      cy.get('tbody tr').should('have.length', 1);
      cy.get('tbody tr').first().should('contain', 'Mediterranean Shipping Company');
    });

    it('should show no results when search matches nothing', () => {
      cy.wait('@getVesselList');
      cy.get('#filterName').type('NonExistentVessel');
      cy.contains('No vessels found').should('be.visible');
    });

    it('should clear search filter', () => {
      cy.wait('@getVesselList');
      cy.get('#filterName').type('Oscar');
      cy.get('tbody tr').should('have.length', 1);
      cy.contains('Clear Filters').click();
      cy.get('tbody tr').should('have.length', 3);
    });
  });

  describe('Navigation', () => {
    it('should navigate to create vessel page', () => {
      cy.wait('@getVesselList');
      cy.contains('Register New Vessel').click();
      cy.url().should('include', '/vessels/new');
    });

    it('should navigate to edit vessel page', () => {
      cy.wait('@getVesselList');
      cy.get('tbody tr').first().within(() => {
        cy.contains('Edit').click();
      });
      cy.url().should('match', /\/vessels\/.*\/edit/);
    });
  });
});

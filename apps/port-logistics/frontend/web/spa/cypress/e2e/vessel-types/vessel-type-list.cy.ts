/// <reference types="cypress" />

/**
 * E2E Tests for Vessel Types List
 */

describe('Vessel Types List', () => {
  let vesselTypesData: any;

  before(() => {
    cy.fixture('vessel-types').then((data) => {
      vesselTypesData = data;
    });
  });

  beforeEach(() => {
    // Login as PortAuthorityOfficer
    cy.login('PortAuthorityOfficer');
    
    // Setup default intercept with fixture data
    cy.intercept('GET', '**/api/VesselTypes*', {
      statusCode: 200,
      body: vesselTypesData.vesselTypeList
    }).as('getVesselTypes');
  });

  afterEach(() => {
    cy.logout();
  });

  describe('List View', () => {
    it('should display the vessel types list page', function() {
      cy.visit('/vessel-types');
      cy.wait('@getVesselTypes');

      cy.get('.card-header h4').should('contain', 'Vessel Types');
      cy.get('table').should('exist');
    });

    it('should display vessel types in the table', function() {
      cy.visit('/vessel-types');
      cy.wait('@getVesselTypes');

      cy.get('tbody tr').should('have.length', vesselTypesData.vesselTypeList.length);
      cy.get('tbody tr').first().should('contain', vesselTypesData.vesselTypeList[0].name);
    });

    it('should show loading state while fetching data', () => {
      cy.intercept('GET', '**/api/VesselTypes*', {
        statusCode: 200,
        body: [],
        delay: 1000
      }).as('getVesselTypes');

      cy.visit('/vessel-types');
      cy.get('.spinner-border').should('be.visible');
      cy.get('.text-muted').should('contain', 'Loading vessel types...');
    });

    it('should show error message when API fails', () => {
      cy.intercept('GET', '**/api/VesselTypes*', {
        statusCode: 500,
        body: 'Internal Server Error'
      }).as('getVesselTypesFail');

      cy.visit('/vessel-types');
      cy.wait('@getVesselTypesFail');

      cy.contains('Failed to load vessel types').should('be.visible');
    });

    it('should have a button to create new vessel type', function() {
      cy.visit('/vessel-types');
      cy.wait('@getVesselTypes');

      cy.contains('button', 'New Vessel Type').should('exist');
      cy.contains('button', 'New Vessel Type').click();
      cy.url().should('include', '/vessel-types/new');
    });
  });

  describe('Search and Filter', () => {
    it('should filter vessel types by name', function() {
      cy.visit('/vessel-types');
      cy.wait('@getVesselTypes');

      cy.get('#searchName').type('Container');
      cy.get('tbody tr').should('have.length', 1);
      cy.get('tbody tr').first().should('contain', 'Container Ship');
    });

    it('should filter vessel types by description', function() {
      cy.visit('/vessel-types');
      cy.wait('@getVesselTypes');

      cy.get('#searchDescription').type('bulk');
      cy.get('tbody tr').should('have.length', 1);
      cy.get('tbody tr').first().should('contain', 'Bulk Carrier');
    });

    it('should clear filters when clear button is clicked', function() {
      cy.visit('/vessel-types');
      cy.wait('@getVesselTypes');

      cy.get('#searchName').type('Container');
      cy.get('tbody tr').should('have.length', 1);

      cy.get('button[title="Clear filters"]').click();
      cy.get('#searchName').should('have.value', '');
      cy.get('tbody tr').should('have.length', vesselTypesData.vesselTypeList.length);
    });
  });

  describe('Navigation', () => {
    it('should navigate to edit page when edit button is clicked', function() {
      cy.visit('/vessel-types');
      cy.wait('@getVesselTypes');

      cy.get('tbody tr').first().within(() => {
        cy.contains('button', 'Edit').click();
      });

      cy.url().should('include', '/vessel-types/');
      cy.url().should('include', '/edit');
    });
  });
});

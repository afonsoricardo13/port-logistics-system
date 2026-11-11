/// <reference types="cypress" />

/**
 * E2E Tests for Dock List
 * 
 * User Story: As a Port Authority Officer, I want to manage docks
 * (create, update, list), so that the system can accurately track
 * dock information for vessel operations and scheduling.
 */

describe('Dock List', () => {
  let docksData: any[];

  before(() => {
    docksData = [
      {
        code: 'DOCK001',
        nameOrNumber: 'North Terminal Dock 1',
        location: 'North Port Area',
        length: 350.5,
        depth: 15.0,
        maxDraft: 12.5,
        allowedVesselTypes: ['Container Ship', 'Cargo']
      },
      {
        code: 'DOCK002',
        nameOrNumber: 'South Terminal Dock 2',
        location: 'South Port Area',
        length: 280.0,
        depth: 12.0,
        maxDraft: 10.0,
        allowedVesselTypes: ['Tanker', 'Cargo']
      },
      {
        code: 'DOCK003',
        nameOrNumber: 'East Berth 3',
        location: 'East Port Area',
        length: 400.0,
        depth: 18.0,
        maxDraft: 14.0,
        allowedVesselTypes: ['Container Ship', 'Tanker', 'Cargo']
      }
    ];
  });

  beforeEach(() => {
    // Login as Port Authority Officer
    cy.login('PortAuthorityOfficer');
    
    // Mock API responses
    cy.intercept('GET', '**/api/Docks*', {
      statusCode: 200,
      body: docksData
    }).as('getDockList');
    
    cy.visit('/docks');
  });

  afterEach(() => {
    cy.logout();
  });

  describe('List View', () => {
    it('should display the dock list page', () => {
      cy.wait('@getDockList');
      cy.contains('Docks Management').should('be.visible');
      cy.get('table').should('exist');
    });

    it('should display all docks from the API', () => {
      cy.wait('@getDockList');
      cy.get('tbody tr').should('have.length', 3);
    });

    it('should display dock details correctly', () => {
      cy.wait('@getDockList');
      
      // Check first dock
      cy.get('tbody tr').first().within(() => {
        cy.contains('DOCK001').should('be.visible');
        cy.contains('North Terminal Dock 1').should('be.visible');
        cy.contains('North Port Area').should('be.visible');
        cy.contains('350.5').should('be.visible');
      });
    });

    it('should show loading indicator while fetching data', () => {
      cy.intercept('GET', '**/api/Docks*', {
        statusCode: 200,
        body: [],
        delay: 1000
      }).as('getDockListSlow');

      cy.visit('/docks');
      cy.get('.spinner-border').should('be.visible');
      cy.wait('@getDockListSlow');
      cy.get('.spinner-border').should('not.exist');
    });

    it('should show error message when API fails', () => {
      cy.intercept('GET', '**/api/Docks*', {
        statusCode: 500,
        body: { message: 'Internal Server Error' }
      }).as('getDockListError');

      cy.visit('/docks');
      cy.wait('@getDockListError');
      cy.contains('Failed to load docks').should('be.visible');
    });

    it('should show empty state when no docks exist', () => {
      cy.intercept('GET', '**/api/Docks*', {
        statusCode: 200,
        body: []
      }).as('getEmptyDockList');

      cy.visit('/docks');
      cy.wait('@getEmptyDockList');
      cy.contains('No docks found').should('be.visible');
    });
  });

  describe('Navigation', () => {
    it('should have a button to create new dock', () => {
      cy.wait('@getDockList');
      cy.contains('a', 'Register New Dock').should('be.visible');
    });

    it('should navigate to create dock form when clicking Create button', () => {
      cy.wait('@getDockList');
      cy.contains('a', 'Register New Dock').click();
      cy.url().should('include', '/docks/new');
    });

    it('should have edit buttons for each dock', () => {
      cy.wait('@getDockList');
      cy.get('a').contains('Edit').should('have.length.at.least', 1);
    });

    it('should navigate to edit form when clicking Edit button', () => {
      cy.wait('@getDockList');
      
      cy.get('tbody tr').first().within(() => {
        cy.contains('a', 'Edit').click();
      });
      
      cy.url().should('include', '/docks/DOCK001/edit');
    });
  });

  describe('Filtering', () => {
    it('should filter docks by name', () => {
      cy.wait('@getDockList');
      
      cy.get('input#filterName').type('North');
      
      cy.get('tbody tr').should('have.length', 1);
      cy.get('tbody tr').first().should('contain', 'North Terminal Dock 1');
    });

    it('should filter docks by location', () => {
      cy.wait('@getDockList');
      
      cy.get('input#filterLocation').type('South');
      
      cy.get('tbody tr').should('have.length', 1);
      cy.get('tbody tr').first().should('contain', 'South Port Area');
    });

    it('should filter docks by vessel type', () => {
      cy.wait('@getDockList');
      
      cy.get('input#filterVesselType').type('Container Ship');
      
      cy.get('tbody tr').should('have.length.at.least', 1);
      cy.get('tbody tr').each(($row) => {
        cy.wrap($row).should('contain.text', 'Container Ship');
      });
    });

    it('should show no results when filter matches nothing', () => {
      cy.wait('@getDockList');
      
      cy.get('input#filterName').type('NonExistentDock');
      
      cy.get('tbody tr').should('have.length', 1);
      cy.contains('No docks found').should('be.visible');
    });

    it('should clear filters and show all docks', () => {
      cy.wait('@getDockList');
      
      // Apply filter
      cy.get('input#filterName').type('North');
      cy.get('tbody tr').should('have.length', 1);
      
      // Clear filter
      cy.contains('button', 'Clear Filters').click();
      cy.get('tbody tr').should('have.length', 3);
    });

    it('should combine multiple filters', () => {
      cy.wait('@getDockList');
      
      cy.get('input#filterLocation').type('Port');
      cy.get('input#filterVesselType').type('Container Ship');
      
      cy.get('tbody tr').should('have.length.at.least', 1);
    });
  });

  describe('Dock Information Display', () => {
    it('should display allowed vessel types for each dock', () => {
      cy.wait('@getDockList');
      
      cy.get('tbody tr').first().within(() => {
        cy.contains('Container Ship').should('be.visible');
        cy.contains('Cargo').should('be.visible');
      });
    });

    it('should display physical dimensions correctly', () => {
      cy.wait('@getDockList');
      
      cy.get('tbody tr').eq(1).within(() => {
        cy.contains('280').should('be.visible'); // length
        cy.contains('12').should('be.visible'); // depth
        cy.contains('10').should('be.visible'); // maxDraft
      });
    });
  });
});

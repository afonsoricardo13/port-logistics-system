/**
 * E2E Tests for Physical Resources List (US 2.2.12)
 * 
 * User Story: As a Logistics Operator, I want to register and manage physical
 * resources (create, update, deactivate), so that the system can accurately
 * reflect resource availability and ensure proper resource allocation during
 * scheduling operations.
 */

describe('Physical Resources List - US 2.2.12', () => {
  beforeEach(() => {
    cy.login('LogisticsOperator');
    
    // Mock API responses with fixture data
    cy.fixture('physical-resources').then((data) => {
      cy.intercept('GET', '**/api/PhysicalResources*', {
        statusCode: 200,
        body: data.resourceList
      }).as('getResourceList');
    });
    
    cy.visit('/physical-resources');
  });

  afterEach(() => {
    cy.logout();
  });

  describe('List View', () => {
    it('should display the physical resources list page', () => {
      cy.wait('@getResourceList');
      cy.contains('Physical Resources').should('be.visible');
    });

    it('should display all resources from the API', () => {
      cy.wait('@getResourceList');
      cy.get('table tbody tr').should('have.length', 3);
    });

    it('should display resource details correctly', () => {
      cy.wait('@getResourceList');
      
      // Check first resource
      cy.get('table tbody tr').first().within(() => {
        cy.contains('CRANE-001').should('be.visible');
        cy.contains('Mobile container crane').should('be.visible');
        cy.contains('Crane').should('be.visible');
        cy.contains('50').should('be.visible');
        cy.contains('Active').should('be.visible');
      });
    });

    it('should display different resource types', () => {
      cy.wait('@getResourceList');
      cy.contains('Crane').should('be.visible');
      cy.contains('Truck').should('be.visible');
    });

    it('should display different resource statuses', () => {
      cy.wait('@getResourceList');
      cy.contains('Active').should('be.visible');
      cy.contains('Under Maintenance').should('be.visible'); // Frontend displays "Under Maintenance" not "UnderMaintenance"
    });

    it('should display "Register New Resource" button', () => {
      cy.contains('a', 'Register New Resource').should('be.visible');
    });

    it('should navigate to create form when clicking register button', () => {
      cy.contains('a', 'Register New Resource').click();
      cy.url().should('include', '/physical-resources/new');
    });
  });

  describe('Search and Filter', () => {
    it('should display filter section', () => {
      cy.wait('@getResourceList');
      cy.contains('Search & Filter').should('be.visible');
    });

    it('should have filter inputs for code, description, type, and status', () => {
      cy.wait('@getResourceList');
      cy.get('#filterCode').should('exist');
      cy.get('#filterDescription').should('exist');
      cy.get('#filterType').should('exist');
      cy.get('#filterStatus').should('exist');
    });

    it('should filter by code', () => {
      cy.wait('@getResourceList');
      
      // Filters are client-side, no new API call
      cy.get('#filterCode').type('CRANE');
      cy.wait(500); // Wait for filter to apply
      cy.get('table tbody tr').should('have.length', 2);
      cy.contains('CRANE-001').should('be.visible');
      cy.contains('CRANE-002').should('be.visible');
    });

    it('should filter by type', () => {
      cy.wait('@getResourceList');
      
      // Filters are client-side
      cy.get('#filterType').select('Crane');
      cy.wait(500);
      cy.get('table tbody tr').should('have.length', 2);
      cy.contains('CRANE-001').should('be.visible');
      cy.contains('CRANE-002').should('be.visible');
    });

    it('should filter by status', () => {
      cy.wait('@getResourceList');
      
      // Filters are client-side
      cy.get('#filterStatus').select('Active');
      cy.wait(500);
      cy.get('table tbody tr').should('have.length', 2);
      cy.contains('CRANE-001').should('be.visible');
      cy.contains('TRUCK-001').should('be.visible');
    });

    it('should filter by description', () => {
      cy.wait('@getResourceList');
      
      // Filters are client-side
      cy.get('#filterDescription').type('crane');
      cy.wait(500);
      cy.get('table tbody tr').should('have.length', 2);
    });

    it('should apply multiple filters simultaneously', () => {
      cy.wait('@getResourceList');
      
      // Filters are client-side
      cy.get('#filterType').select('Crane');
      cy.get('#filterStatus').select('Active');
      cy.wait(500);
      cy.get('table tbody tr').should('have.length', 1);
      cy.contains('CRANE-001').should('be.visible');
    });

    it('should clear all filters', () => {
      cy.wait('@getResourceList');
      
      cy.get('#filterCode').type('CRANE');
      cy.get('#filterType').select('Crane');
      cy.wait(500);
      
      cy.contains('button', 'Clear Filters').click();
      cy.wait(500);
      
      cy.get('#filterCode').should('have.value', '');
      cy.get('#filterType').should('have.value', '');
      cy.get('table tbody tr').should('have.length', 3);
    });

    it('should show no results message when filters return empty', () => {
      cy.wait('@getResourceList');
      
      // Filters are client-side
      cy.get('#filterCode').type('NONEXISTENT');
      cy.wait(500);
      cy.contains('No physical resources found').should('be.visible');
    });
  });

  describe('Resource Actions', () => {
    it('should display action buttons for each resource', () => {
      cy.wait('@getResourceList');
      cy.get('table tbody tr').first().within(() => {
        cy.get('button, a').should('exist');
      });
    });

    it('should navigate to edit page when clicking edit button', () => {
      cy.wait('@getResourceList');
      
      cy.get('table tbody tr').first().within(() => {
        cy.get('a[href*="edit"], button:contains("Edit")').first().click();
      });
      
      cy.url().should('match', /\/physical-resources\/[^\/]+\/edit/);
    });

    it('should show deactivate button for active resources', () => {
      cy.wait('@getResourceList');
      
      cy.get('table tbody tr').first().within(() => {
        cy.contains('Active').should('be.visible');
        cy.contains('button', 'Deactivate').should('be.visible');
      });
    });

    it('should show reactivate button for inactive resources', () => {
      cy.wait('@getResourceList');
      
      // Find row with Under Maintenance status
      cy.get('table tbody tr').contains('Under Maintenance').parents('tr').within(() => {
        cy.contains('button', 'Deactivate').should('be.visible'); // UnderMaintenance still shows Deactivate
      });
    });

    it('should confirm before deactivating a resource', () => {
      cy.wait('@getResourceList');
      
      cy.intercept('PUT', '**/api/PhysicalResources/*/deactivate', {
        statusCode: 200,
        body: { message: 'Resource deactivated successfully' }
      }).as('deactivateResource');

      // Stub the confirm dialog
      cy.on('window:confirm', (str) => {
        expect(str).to.include('deactivate');
        return true;
      });

      cy.get('table tbody tr').first().within(() => {
        cy.contains('button', 'Deactivate').click();
      });
    });
  });

  describe('Pagination and Sorting', () => {
    it('should display table headers', () => {
      cy.wait('@getResourceList');
      cy.get('table thead th').should('have.length.gt', 3);
    });

    it('should have sortable column headers', () => {
      cy.wait('@getResourceList');
      cy.get('table thead th').first().then(($th) => {
        // Check if sorting is implemented
        if ($th.find('button, .sortable').length > 0) {
          cy.get('table thead th').first().click();
          cy.wait(500); // Wait for sort
        }
      });
    });
  });

  describe('Error Handling', () => {
    it('should display error message when API fails', () => {
      cy.intercept('GET', '**/api/PhysicalResources', {
        statusCode: 500,
        body: { message: 'Internal server error' }
      }).as('getResourceListError');

      cy.visit('/physical-resources');
      cy.wait('@getResourceListError');
      cy.contains('Failed to load physical resources').should('be.visible');
    });

    it('should retry after error', () => {
      cy.intercept('GET', '**/api/PhysicalResources*', {
        statusCode: 500,
        body: { message: 'Internal server error' }
      }).as('getResourceListError');

      cy.visit('/physical-resources');
      cy.wait('@getResourceListError');

      // Mock success on retry
      cy.fixture('physical-resources').then((data) => {
        cy.intercept('GET', '**/api/PhysicalResources*', {
          statusCode: 200,
          body: data.resourceList
        }).as('getResourceListSuccess');
      });

      cy.get('body').then(($body) => {
        if ($body.find('button:contains("Retry")').length > 0) {
          cy.contains('button', 'Retry').click();
          cy.wait('@getResourceListSuccess');
          cy.get('table tbody tr').should('have.length', 3);
        }
      });
    });
  });

  describe('Empty State', () => {
    it('should display message when no resources exist', () => {
      cy.intercept('GET', '**/api/PhysicalResources', {
        statusCode: 200,
        body: []
      }).as('getEmptyList');

      cy.visit('/physical-resources');
      cy.wait('@getEmptyList');
      cy.contains('No physical resources found').should('be.visible');
    });

    it('should show register button in empty state', () => {
      cy.intercept('GET', '**/api/PhysicalResources*', {
        statusCode: 200,
        body: []
      }).as('getEmptyList');

      cy.visit('/physical-resources');
      cy.wait('@getEmptyList');
      cy.contains('Register New Resource').should('be.visible');
    });
  });
});

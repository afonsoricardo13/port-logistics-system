/**
 * E2E Tests for Operating Staff List (US 2.2.11)
 * 
 * User Story: As a Logistics Operator, I want to register and manage operating
 * staff members (create, update, deactivate), so that the system can accurately
 * reflect staff availability and ensure that only qualified personnel are assigned
 * to resources during scheduling.
 */

describe('Operating Staff List - US 2.2.11', () => {
  beforeEach(() => {
    // Login as Logistics Operator
    cy.login('LogisticsOperator');
    
    // Mock API responses
    cy.intercept('GET', '**/api/OperatingStaff*', {
      statusCode: 200,
      body: [
        {
          mecanographicNumber: 'OS001',
          name: 'John Doe',
          email: 'john.doe@port.com',
          phoneNumber: '+351912345678',
          qualifications: ['Crane Operator', 'Forklift Driver'],
          operationalWindow: 'Mon-Fri 08:00-16:00',
          status: 'Available',
          statusHistory: []
        },
        {
          mecanographicNumber: 'OS002',
          name: 'Jane Smith',
          email: 'jane.smith@port.com',
          phoneNumber: '+351923456789',
          qualifications: ['Truck Driver'],
          operationalWindow: 'Mon-Sat 09:00-17:00',
          status: 'Available',
          statusHistory: []
        },
        {
          mecanographicNumber: 'OS003',
          name: 'Bob Johnson',
          email: 'bob.johnson@port.com',
          phoneNumber: '+351934567890',
          qualifications: ['Warehouse Manager'],
          operationalWindow: 'Mon-Fri 07:00-15:00',
          status: 'Unavailable',
          statusHistory: []
        }
      ]
    }).as('getStaffList');
    
    cy.visit('/operating-staff');
  });

  afterEach(() => {
    cy.logout();
  });

  describe('List View', () => {
    it('should display the operating staff list page', () => {
      cy.wait('@getStaffList');
      cy.contains('Operating Staff').should('be.visible');
      cy.get('[data-cy=staff-list]').should('exist');
    });

    it('should display all staff members from the API', () => {
      cy.wait('@getStaffList');
      cy.get('[data-cy=staff-row]').should('have.length', 3);
    });

    it('should display staff member details correctly', () => {
      cy.wait('@getStaffList');
      
      // Check first staff member
      cy.get('[data-cy=staff-row]').first().within(() => {
        cy.contains('OS001').should('be.visible');
        cy.contains('John Doe').should('be.visible');
        cy.contains('john.doe@port.com').should('be.visible');
        cy.contains('Available').should('be.visible');
      });
    });

    it('should show loading indicator while fetching data', () => {
      cy.intercept('GET', '**/api/OperatingStaff*', {
        statusCode: 200,
        body: [],
        delay: 1000
      }).as('getStaffListSlow');

      cy.visit('/operating-staff');
      cy.get('[data-cy=loading-indicator]').should('be.visible');
      cy.wait('@getStaffListSlow');
      cy.get('[data-cy=loading-indicator]').should('not.exist');
    });

    it('should display error message when API fails', () => {
      cy.intercept('GET', '**/api/OperatingStaff*', {
        statusCode: 500,
        body: { message: 'Internal Server Error' }
      }).as('getStaffListError');

      cy.visit('/operating-staff');
      cy.wait('@getStaffListError');
      cy.contains('Failed to load operating staff').should('be.visible');
    });
  });

  describe('Search and Filter', () => {
    it('should filter staff by name using search box', () => {
      cy.wait('@getStaffList');
      
      cy.get('[data-cy=search-input]').type('John Doe');
      cy.get('[data-cy=staff-row]').should('have.length', 1);
      cy.get('[data-cy=staff-row]').first().should('contain', 'John Doe');
    });

    it('should filter staff by mecanographic number', () => {
      cy.wait('@getStaffList');
      
      cy.get('[data-cy=search-input]').type('OS002');
      cy.get('[data-cy=staff-row]').should('have.length', 1);
      cy.get('[data-cy=staff-row]').first().should('contain', 'Jane Smith');
    });

    it('should filter staff by email', () => {
      cy.wait('@getStaffList');
      
      cy.get('[data-cy=search-input]').type('bob.johnson');
      cy.get('[data-cy=staff-row]').should('have.length', 1);
      cy.get('[data-cy=staff-row]').first().should('contain', 'Bob Johnson');
    });

    it('should filter staff by qualification', () => {
      cy.wait('@getStaffList');
      
      cy.get('[data-cy=search-input]').type('Crane Operator');
      cy.get('[data-cy=staff-row]').should('have.length', 1);
      cy.get('[data-cy=staff-row]').first().should('contain', 'John Doe');
    });

    it('should filter staff by status', () => {
      cy.wait('@getStaffList');
      
      // Intercept the filtered API call
      cy.intercept('GET', '**/api/OperatingStaff?status=Unavailable', {
        statusCode: 200,
        body: [
          {
            mecanographicNumber: 'OS003',
            name: 'Bob Johnson',
            email: 'bob.johnson@port.com',
            phoneNumber: '+351934567890',
            qualifications: ['Warehouse Manager'],
            operationalWindow: 'Mon-Fri 07:00-15:00',
            status: 'Unavailable',
            statusHistory: []
          }
        ]
      }).as('getFilteredStaff');
      
      cy.get('[data-cy=status-filter]').select('Unavailable');
      cy.wait('@getFilteredStaff');
      cy.get('[data-cy=staff-row]').should('have.length', 1);
      cy.get('[data-cy=staff-row]').first().should('contain', 'Bob Johnson');
    });

    it('should show no results message when search has no matches', () => {
      cy.wait('@getStaffList');
      
      cy.get('[data-cy=search-input]').type('NonExistentName');
      cy.get('[data-cy=staff-row]').should('have.length', 0);
      cy.contains('No staff members').should('be.visible');
    });

    it('should clear filters when clear button is clicked', () => {
      cy.wait('@getStaffList');
      
      cy.get('[data-cy=search-input]').type('John');
      cy.get('[data-cy=status-filter]').select('Available');
      cy.get('[data-cy=clear-filters-btn]').click();
      
      cy.get('[data-cy=search-input]').should('have.value', '');
      cy.get('[data-cy=status-filter]').should('have.value', '');
      cy.get('[data-cy=staff-row]').should('have.length', 3);
    });
  });

  describe('Navigation', () => {
    it('should navigate to create staff form when create button is clicked', () => {
      cy.wait('@getStaffList');
      
      cy.get('[data-cy=create-staff-btn]').click();
      cy.url().should('include', '/operating-staff/create');
    });

    it('should navigate to staff details when view button is clicked', () => {
      cy.wait('@getStaffList');
      
      cy.get('[data-cy=staff-row]').first().within(() => {
        cy.get('[data-cy=view-details-btn]').click();
      });
      
      cy.url().should('include', '/operating-staff/OS001');
    });

    it('should navigate to edit form when edit button is clicked', () => {
      cy.wait('@getStaffList');
      
      cy.get('[data-cy=staff-row]').first().within(() => {
        cy.get('[data-cy=edit-staff-btn]').click();
      });
      
      cy.url().should('include', '/operating-staff/edit/OS001');
    });
  });

  describe('Deactivate Staff', () => {
    it('should show confirmation dialog when deactivate button is clicked', () => {
      cy.wait('@getStaffList');
      
      cy.get('[data-cy=staff-row]').first().within(() => {
        cy.get('[data-cy=deactivate-staff-btn]').click();
      });
      
      cy.on('window:confirm', (text) => {
        expect(text).to.contains('deactivate');
        return false;
      });
    });

    it('should deactivate staff member successfully', () => {
      cy.intercept('PATCH', '**/api/OperatingStaff/OS001/status', {
        statusCode: 200,
        body: {
          mecanographicNumber: 'OS001',
          name: 'John Doe',
          status: 'Unavailable'
        }
      }).as('deactivateStaff');

      cy.wait('@getStaffList');
      
      cy.get('[data-cy=staff-row]').first().within(() => {
        cy.get('[data-cy=deactivate-staff-btn]').click();
      });
      
      cy.on('window:confirm', () => true);
      cy.on('window:prompt', () => 'Test deactivation reason');
      
      cy.wait('@deactivateStaff');
      cy.contains('deactivated successfully').should('be.visible');
    });

    it('should display error when deactivation fails', () => {
      cy.intercept('PATCH', '**/api/OperatingStaff/OS001/status', {
        statusCode: 400,
        body: { message: 'Cannot deactivate staff member' }
      }).as('deactivateStaffError');

      cy.wait('@getStaffList');
      
      cy.get('[data-cy=staff-row]').first().within(() => {
        cy.get('[data-cy=deactivate-staff-btn]').click();
      });
      
      cy.on('window:confirm', () => true);
      cy.on('window:prompt', () => 'Test reason');
      
      cy.wait('@deactivateStaffError');
      cy.contains('Failed to deactivate').should('be.visible');
    });
  });

  describe('Reactivate Staff', () => {
    it('should reactivate staff member successfully', () => {
      cy.intercept('PATCH', '**/api/OperatingStaff/OS003/status', {
        statusCode: 200,
        body: {
          mecanographicNumber: 'OS003',
          name: 'Bob Johnson',
          status: 'Available'
        }
      }).as('reactivateStaff');

      cy.wait('@getStaffList');
      
      cy.get('[data-cy=staff-row]').contains('Bob Johnson').parent().within(() => {
        cy.get('[data-cy=reactivate-staff-btn]').click();
      });
      
      cy.on('window:confirm', () => true);
      cy.on('window:prompt', () => 'Test reactivation reason');
      
      cy.wait('@reactivateStaff');
      cy.contains('reactivated successfully').should('be.visible');
    });
  });

  describe('Permissions', () => {
    it('should hide management buttons for non-authorized users', () => {
      cy.logout();
      cy.login('ShippingAgentRepresentative');
      cy.visit('/operating-staff');
      
      cy.wait('@getStaffList');
      cy.get('[data-cy=create-staff-btn]').should('not.exist');
      cy.get('[data-cy=edit-staff-btn]').should('not.exist');
      cy.get('[data-cy=deactivate-staff-btn]').should('not.exist');
    });

    it('should show management buttons for Logistics Operator', () => {
      cy.wait('@getStaffList');
      
      cy.get('[data-cy=create-staff-btn]').should('be.visible');
      cy.get('[data-cy=edit-staff-btn]').should('exist');
      cy.get('[data-cy=deactivate-staff-btn]').should('exist');
    });
  });

  describe('Accessibility', () => {
    it('should have proper ARIA labels for buttons', () => {
      cy.wait('@getStaffList');
      
      cy.get('[data-cy=create-staff-btn]').should('have.attr', 'aria-label');
      cy.get('[data-cy=search-input]').should('have.attr', 'aria-label');
    });

    it.skip('should be keyboard navigable', () => {
      cy.wait('@getStaffList');
      
      cy.get('[data-cy=search-input]').focus().should('have.focus');
      cy.get('[data-cy=search-input]').type('{tab}');
      cy.focused().should('exist');
    });
  });
});

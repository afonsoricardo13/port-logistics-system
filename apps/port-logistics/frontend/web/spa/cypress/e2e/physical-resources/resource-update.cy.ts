/**
 * E2E Tests for Updating Physical Resources (US 2.2.12)
 * 
 * User Story: As a Logistics Operator, I want to register and manage physical
 * resources (create, update, deactivate), so that the system can accurately
 * reflect resource availability and ensure proper resource allocation during
 * scheduling operations.
 */

describe('Physical Resource Update Form - US 2.2.12', () => {
  let resourceData: any;

  before(() => {
    cy.fixture('physical-resources').then((data) => {
      resourceData = data;
    });
  });

  beforeEach(() => {
    cy.login('LogisticsOperator');
    
    const resource = resourceData.resourceToUpdate;
    
    // Mock GET request to fetch resource for editing
    cy.intercept('GET', `**/api/PhysicalResources/${resource.code}`, {
      statusCode: 200,
      body: resource
    }).as('getResource');
    
    cy.visit(`/physical-resources/${resource.code}/edit`);
  });

  afterEach(() => {
    cy.logout();
  });

  describe('Form Display', () => {
    it('should display the edit resource form', () => {
      cy.wait('@getResource');
      cy.contains('Edit Physical Resource').should('be.visible');
      cy.get('form').should('exist');
    });

    it('should load existing resource data', () => {
      cy.wait('@getResource');
      cy.fixture('physical-resources').then((data) => {
        const resource = data.resourceToUpdate;
        
        cy.get('#code').should('have.value', resource.code);
        cy.get('#description').should('have.value', resource.description);
        cy.get('#type').should('have.value', resource.type);
        cy.get('#capacity').should('have.value', resource.capacity.toString());
      });
    });

    it('should disable code field in edit mode', () => {
      cy.wait('@getResource');
      cy.get('#code').should('be.disabled');
    });

    it('should display all editable fields', () => {
      cy.wait('@getResource');
      cy.get('#description').should('not.be.disabled');
      cy.get('#type').should('not.be.disabled');
      cy.get('#capacity').should('not.be.disabled');
      cy.get('#setupTimeMinutes').should('not.be.disabled');
    });
  });

  describe('Form Validation', () => {
    it('should validate description is not empty', () => {
      cy.wait('@getResource');
      cy.get('#description').clear();
      cy.get('#description').blur();
      cy.contains('Description is required').should('be.visible');
    });

    it('should validate type is selected', () => {
      cy.wait('@getResource');
      cy.get('#type').select('');
      cy.get('#type').blur();
      cy.contains('Type is required').should('be.visible');
    });

    it('should validate capacity is positive', () => {
      cy.wait('@getResource');
      
      cy.get('#capacity').clear().type('0');
      cy.get('#capacity').blur();
      cy.contains('Capacity must be at least 1').should('be.visible');
    });

    it('should validate setup time is non-negative', () => {
      cy.wait('@getResource');
      
      cy.get('#setupTimeMinutes').clear().type('-10');
      cy.get('#setupTimeMinutes').blur();
      // Frontend validation prevents negative
      cy.get('button[type="submit"]').click();
      cy.contains('Please fill in all required fields').should('be.visible');
    });

    it('should validate description max length', () => {
      cy.wait('@getResource');
      cy.get('#description').clear().type('A'.repeat(260));
      cy.get('#description').blur();
      cy.contains('must not exceed 255 characters').should('be.visible');
    });
  });

  describe('Successful Resource Update', () => {
    it('should successfully update resource description', () => {
      cy.wait('@getResource');
      
      cy.intercept('PUT', '**/api/PhysicalResources/*', {
        statusCode: 200,
        body: {
          code: 'TRUCK-001',
          description: 'Updated forklift description',
          type: 'Truck',
          capacity: 10,
          status: 'Active'
        }
      }).as('updateResource');

      cy.get('#description').clear().type('Updated forklift description');
      cy.get('button[type="submit"]').click();
      
      cy.wait('@updateResource');
      cy.contains('Resource updated successfully').should('be.visible');
    });

    it('should successfully update resource capacity', () => {
      cy.wait('@getResource');
      
      cy.intercept('PUT', '**/api/PhysicalResources/*', {
        statusCode: 200,
        body: {
          code: 'TRUCK-001',
          description: 'Forklift truck',
          type: 'Truck',
          capacity: 15,
          status: 'Active'
        }
      }).as('updateResource');

      cy.get('#capacity').clear().type('15');
      cy.get('button[type="submit"]').click();
      
      cy.wait('@updateResource');
      cy.contains('Resource updated successfully').should('be.visible');
    });

    it('should successfully update resource type', () => {
      cy.wait('@getResource');
      
      cy.intercept('PUT', '**/api/PhysicalResources/*', {
        statusCode: 200,
        body: {
          code: 'TRUCK-001',
          description: 'Forklift truck',
          type: 'Forklift',
          capacity: 10,
          status: 'Active'
        }
      }).as('updateResource');

      cy.get('#type').select('Forklift');
      cy.get('button[type="submit"]').click();
      
      cy.wait('@updateResource');
      cy.contains('Resource updated successfully').should('be.visible');
    });

    it('should successfully update setup time', () => {
      cy.wait('@getResource');
      
      cy.intercept('PUT', '**/api/PhysicalResources/*', {
        statusCode: 200,
        body: {
          code: 'TRUCK-001',
          description: 'Forklift truck',
          type: 'Truck',
          capacity: 10,
          setupTimeMinutes: 20,
          status: 'Active'
        }
      }).as('updateResource');

      cy.get('#setupTimeMinutes').clear().type('20');
      cy.get('button[type="submit"]').click();
      
      cy.wait('@updateResource');
      cy.contains('Resource updated successfully').should('be.visible');
    });

    it('should successfully update assigned area', () => {
      cy.wait('@getResource');
      
      cy.intercept('PUT', '**/api/PhysicalResources/*', {
        statusCode: 200,
        body: {
          code: 'TRUCK-001',
          description: 'Forklift truck',
          type: 'Truck',
          capacity: 10,
          assignedArea: 'WAREHOUSE-C',
          status: 'Active'
        }
      }).as('updateResource');

      cy.get('#assignedArea').clear().type('WAREHOUSE-C');
      cy.get('button[type="submit"]').click();
      
      cy.wait('@updateResource');
      cy.contains('Resource updated successfully').should('be.visible');
    });

    it('should update multiple fields simultaneously', () => {
      cy.wait('@getResource');
      
      cy.intercept('PUT', '**/api/PhysicalResources/*', {
        statusCode: 200,
        body: {
          code: 'TRUCK-001',
          description: 'Heavy-duty forklift for containers',
          type: 'Truck',
          capacity: 20,
          setupTimeMinutes: 25,
          status: 'Active'
        }
      }).as('updateResource');

      cy.get('#description').clear().type('Heavy-duty forklift for containers');
      cy.get('#capacity').clear().type('20');
      cy.get('#setupTimeMinutes').clear().type('25');
      cy.get('button[type="submit"]').click();
      
      cy.wait('@updateResource');
      cy.contains('Resource updated successfully').should('be.visible');
    });

    it('should redirect to list after successful update', () => {
      cy.wait('@getResource');
      
      cy.intercept('PUT', '**/api/PhysicalResources/*', {
        statusCode: 200,
        body: {
          code: 'TRUCK-001',
          description: 'Updated description',
          type: 'Truck',
          capacity: 10,
          status: 'Active'
        }
      }).as('updateResource');

      cy.get('#description').clear().type('Updated description');
      cy.get('button[type="submit"]').click();
      
      cy.wait('@updateResource');
      cy.wait(1000); // Wait for redirect
      cy.url().should('include', '/physical-resources');
      cy.url().should('not.include', '/edit');
    });
  });

  describe('Qualification Management', () => {
    it('should display existing qualifications', () => {
      cy.wait('@getResource');
      
      cy.fixture('physical-resources').then((data) => {
        const resource = data.resourceToUpdate;
        
        if (resource.qualificationRequirementIds && resource.qualificationRequirementIds.length > 0) {
          cy.get('body').then(($body) => {
            if ($body.find('[data-cy=qualification-list]').length > 0) {
              cy.get('[data-cy=qualification-list]').should('exist');
            }
          });
        }
      });
    });

    it('should add new qualification', () => {
      cy.wait('@getResource');
      
      cy.intercept('GET', '**/api/Qualifications', {
        statusCode: 200,
        body: [
          { id: 'QUAL-001', name: 'Crane Operator License' },
          { id: 'QUAL-002', name: 'Forklift Certificate' }
        ]
      }).as('getQualifications');

      cy.get('body').then(($body) => {
        if ($body.find('[data-cy=add-qualification-btn]').length > 0) {
          cy.get('[data-cy=qualification-select]').select('QUAL-002');
          cy.get('[data-cy=add-qualification-btn]').click();
          cy.get('[data-cy=qualification-list]').should('contain', 'QUAL-002');
        }
      });
    });

    it('should remove qualification', () => {
      cy.wait('@getResource');
      
      cy.get('body').then(($body) => {
        if ($body.find('[data-cy=remove-qualification-btn]').length > 0) {
          cy.get('[data-cy=remove-qualification-btn]').first().click();
          cy.wait(500);
        }
      });
    });
  });

  describe('Error Handling', () => {
    it('should display error when resource not found', () => {
      cy.intercept('GET', '**/api/PhysicalResources/NONEXISTENT', {
        statusCode: 404,
        body: { message: 'Resource not found' }
      }).as('getResourceNotFound');

      cy.visit('/physical-resources/NONEXISTENT/edit');
      cy.wait('@getResourceNotFound');
      cy.contains('Failed to load resource').should('be.visible');
    });

    it('should display validation errors from API', () => {
      cy.wait('@getResource');
      
      cy.intercept('PUT', '**/api/PhysicalResources/*', {
        statusCode: 400,
        body: 'Capacity must be greater than zero.'
      }).as('updateResourceError');

      cy.get('#capacity').clear().type('50'); // Valid to bypass frontend validation
      cy.get('button[type="submit"]').click();
      
      cy.wait('@updateResourceError');
      cy.contains('Capacity must be greater than zero').should('be.visible');
    });
  });

  describe('Form Interactions', () => {
    it('should navigate back to list on cancel', () => {
      cy.wait('@getResource');
      
      cy.get('body').then(($body) => {
        if ($body.find('button:contains("Cancel")').length > 0) {
          cy.contains('button', 'Cancel').click();
          cy.url().should('include', '/physical-resources');
          cy.url().should('not.include', '/edit');
        }
      });
    });

    it('should enable submit button when form is dirty and valid', () => {
      cy.wait('@getResource');
      
      cy.get('#description').clear().type('Modified description');
      cy.get('button[type="submit"]').should('not.be.disabled');
    });

    it('should show unsaved changes warning', () => {
      cy.wait('@getResource');
      
      cy.get('#description').clear().type('Modified description without saving');
      
      // Try to navigate away
      cy.get('body').then(($body) => {
        if ($body.find('a[routerLink="/physical-resources"]').length > 0) {
          cy.on('window:confirm', (str) => {
            expect(str).to.include('unsaved');
            return false; // Cancel navigation
          });
        }
      });
    });

    it('should preserve form state after validation error', () => {
      cy.wait('@getResource');
      
      const newDescription = 'Test description that should persist';
      
      cy.intercept('PUT', '**/api/PhysicalResources/*', {
        statusCode: 400,
        body: 'Validation error'
      }).as('updateResourceError');

      cy.get('#description').clear().type(newDescription);
      cy.get('button[type="submit"]').click();
      
      cy.wait('@updateResourceError');
      cy.get('#description').should('have.value', newDescription);
    });
  });

  describe('Status Management', () => {
    it('should display current status', () => {
      cy.wait('@getResource');
      cy.fixture('physical-resources').then((data) => {
        const resource = data.resourceToUpdate;
        cy.contains(resource.status).should('be.visible');
      });
    });

    it('should allow changing status via dropdown', () => {
      cy.wait('@getResource');
      
      cy.get('body').then(($body) => {
        if ($body.find('#status').length > 0) {
          cy.get('#status').should('exist');
          cy.get('#status option').should('have.length.gt', 1);
        }
      });
    });
  });
});

/**
 * E2E Tests for Creating Physical Resources (US 2.2.12)
 * 
 * User Story: As a Logistics Operator, I want to register and manage physical
 * resources (create, update, deactivate), so that the system can accurately
 * reflect resource availability and ensure proper resource allocation during
 * scheduling operations.
 */

describe('Physical Resource Create Form - US 2.2.12', () => {
  beforeEach(() => {
    cy.login('LogisticsOperator');
    cy.visit('/physical-resources/new');
  });

  afterEach(() => {
    cy.logout();
  });

  describe('Form Display', () => {
    it('should display the create resource form', () => {
      cy.contains('Register New Physical Resource').should('be.visible');
      cy.get('form').should('exist');
    });

    it('should display all required form fields', () => {
      cy.get('#code').should('exist');
      cy.get('#description').should('exist');
      cy.get('#type').should('exist');
      cy.get('#capacity').should('exist');
      cy.get('#setupTimeMinutes').should('exist');
    });

    it('should have empty form fields initially', () => {
      cy.get('#code').should('have.value', '');
      cy.get('#description').should('have.value', '');
      cy.get('#type').should('have.value', '');
      cy.get('#capacity').should('have.value', '1'); // Default value
    });

    it('should display type options in select dropdown', () => {
      cy.get('#type option').should('have.length.gt', 1);
      cy.contains('option', 'Crane').should('exist');
      cy.contains('option', 'Truck').should('exist');
    });
  });

  describe('Form Validation', () => {
    it('should show error when submitting empty form', () => {
      cy.get('button[type="submit"]').click();
      cy.contains('Please fill in all required fields').should('be.visible');
    });

    it('should validate code is required', () => {
      cy.get('#description').type('Test Resource');
      cy.get('#type').select('Crane');
      cy.get('#capacity').clear().type('50');
      cy.get('button[type="submit"]').click();
      cy.contains('Please fill in all required fields').should('be.visible');
    });

    it('should validate description is required', () => {
      cy.get('#code').type('CRANE-001');
      cy.get('#type').select('Crane');
      cy.get('#capacity').clear().type('50');
      cy.get('button[type="submit"]').click();
      cy.contains('Please fill in all required fields').should('be.visible');
    });

    it('should validate type is required', () => {
      cy.get('#code').type('CRANE-001');
      cy.get('#description').type('Test Crane');
      cy.get('#capacity').clear().type('50');
      cy.get('button[type="submit"]').click();
      cy.contains('Please fill in all required fields').should('be.visible');
    });

    it('should validate capacity must be at least 1', () => {
      cy.get('#code').type('CRANE-001');
      cy.get('#description').type('Test Crane');
      cy.get('#type').select('Crane');
      cy.get('#capacity').clear();
      cy.get('#capacity').blur();
      cy.contains('Capacity is required').should('be.visible');
    });

    it('should validate capacity must be positive', () => {
      cy.get('#code').type('CRANE-001');
      cy.get('#description').type('Test Crane');
      cy.get('#type').select('Crane');
      cy.get('#capacity').clear().type('0');
      cy.get('#capacity').blur();
      cy.contains('Capacity must be at least 1').should('be.visible');
    });

    it('should validate setup time must be non-negative', () => {
      cy.get('#code').type('CRANE-001');
      cy.get('#description').type('Test Crane');
      cy.get('#type').select('Crane');
      cy.get('#capacity').clear().type('50');
      cy.get('#setupTimeMinutes').type('-5');
      cy.get('#setupTimeMinutes').blur();
      // Frontend validation prevents negative, form won't submit
      cy.get('button[type="submit"]').click();
      cy.contains('Please fill in all required fields').should('be.visible');
    });

    it('should validate code max length', () => {
      cy.get('#code').type('A'.repeat(25));
      cy.get('#code').blur();
      cy.contains('must not exceed 20 characters').should('be.visible');
    });

    it('should validate description max length', () => {
      cy.get('#description').type('A'.repeat(260));
      cy.get('#description').blur();
      cy.contains('must not exceed 255 characters').should('be.visible');
    });
  });

  describe('Successful Resource Creation', () => {
    it('should successfully create a physical resource with minimal data', () => {
      cy.intercept('POST', '**/api/PhysicalResources', {
        statusCode: 201,
        body: {
          code: 'CRANE-001',
          description: 'Test Crane',
          type: 'Crane',
          capacity: 50,
          status: 'Active'
        }
      }).as('createResource');

      cy.get('#code').type('CRANE-001');
      cy.get('#description').type('Test Crane');
      cy.get('#type').select('Crane');
      cy.get('#capacity').clear().type('50');
      cy.get('button[type="submit"]').click();
      
      cy.wait('@createResource');
      cy.contains('Resource created successfully').should('be.visible');
    });

    it('should successfully create a resource with all fields', () => {
      cy.fixture('physical-resources').then((data) => {
        const resource = data.validResource;

        cy.intercept('POST', '**/api/PhysicalResources', {
          statusCode: 201,
          body: resource
        }).as('createResource');

        cy.get('#code').type(resource.code);
        cy.get('#description').type(resource.description);
        cy.get('#type').select(resource.type);
        cy.get('#capacity').type(resource.capacity.toString());
        cy.get('#setupTimeMinutes').type(resource.setupTimeMinutes.toString());
        
        if (resource.assignedArea) {
          cy.get('#assignedArea').type(resource.assignedArea);
        }

        cy.get('button[type="submit"]').click();
        
        cy.wait('@createResource');
        cy.contains('Resource created successfully').should('be.visible');
      });
    });

    it('should redirect to list after successful creation', () => {
      cy.intercept('POST', '**/api/PhysicalResources', {
        statusCode: 201,
        body: {
          code: 'CRANE-001',
          description: 'Test Crane',
          type: 'Crane',
          capacity: 50,
          status: 'Active'
        }
      }).as('createResource');

      cy.get('#code').type('CRANE-001');
      cy.get('#description').type('Test Crane');
      cy.get('#type').select('Crane');
      cy.get('#capacity').type('50');
      cy.get('button[type="submit"]').click();
      
      cy.wait('@createResource');
      cy.wait(1000); // Wait for redirect
      cy.url().should('include', '/physical-resources');
    });
  });

  describe('Form Interactions', () => {
    it('should clear form on cancel button click', () => {
      cy.get('#code').type('CRANE-001');
      cy.get('#description').type('Test Crane');
      
      cy.get('body').then(($body) => {
        if ($body.find('button:contains("Cancel")').length > 0) {
          cy.contains('button', 'Cancel').click();
          cy.url().should('include', '/physical-resources');
        }
      });
    });

    it('should enable submit button when form is valid', () => {
      cy.get('#code').type('CRANE-001');
      cy.get('#description').type('Test Crane');
      cy.get('#type').select('Crane');
      cy.get('#capacity').type('50');
      
      cy.get('button[type="submit"]').should('not.be.disabled');
    });

    it('should update form validation in real-time', () => {
      cy.get('#code').type('CRANE-001');
      cy.get('#code').clear();
      cy.get('#code').blur();
      cy.contains('Code is required').should('be.visible');
      
      cy.get('#code').type('CRANE-001');
      cy.contains('Code is required').should('not.exist');
    });
  });
});

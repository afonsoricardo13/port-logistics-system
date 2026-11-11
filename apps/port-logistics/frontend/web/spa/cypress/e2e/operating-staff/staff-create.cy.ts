/**
 * E2E Tests for Creating Operating Staff (US 2.2.11)
 */

describe('Operating Staff Create Form - US 2.2.11', () => {
  beforeEach(() => {
    cy.login('LogisticsOperator');
    cy.visit('/operating-staff/create');
  });

  afterEach(() => {
    cy.logout();
  });

  describe('Form Display', () => {
    it('should display the create staff form', () => {
      cy.contains('Register Operating Staff Member').should('be.visible');
      cy.get('[data-cy=staff-form]').should('exist');
    });

    it('should display all required form fields', () => {
      cy.get('[data-cy=mecanographic-number-input]').should('exist');
      cy.get('[data-cy=name-input]').should('exist');
      cy.get('[data-cy=email-input]').should('exist');
      cy.get('[data-cy=phone-input]').should('exist');
      cy.get('[data-cy=start-time-input]').should('exist');
      cy.get('[data-cy=end-time-input]').should('exist');
    });

    it('should have empty form fields initially', () => {
      cy.get('[data-cy=mecanographic-number-input]').should('have.value', '');
      cy.get('[data-cy=name-input]').should('have.value', '');
      cy.get('[data-cy=email-input]').should('have.value', '');
    });
  });

  describe('Form Validation', () => {
    it('should show error when submitting empty form', () => {
      cy.get('[data-cy=submit-btn]').click();
      cy.contains('Name is required').should('be.visible');
    });

    it('should validate email format', () => {
      cy.intercept('POST', '**/api/OperatingStaff', {
        statusCode: 400,
        body: { errors: ['Please enter a valid email address.'] }
      }).as('createStaffError');

      cy.get('[data-cy=mecanographic-number-input]').type('OS001');
      cy.get('[data-cy=name-input]').type('John Doe');
      cy.get('[data-cy=email-input]').type('invalid-email');
      cy.get('[data-cy=phone-input]').type('+351912345678');
      cy.get('[data-cy=qualification-input]').type('Test');
      cy.get('[data-cy=add-qualification-btn]').click();
      cy.get('[data-cy=start-time-input]').type('08:00');
      cy.get('[data-cy=end-time-input]').type('16:00');
      cy.get('[data-cy=submit-btn]').click();
      
      cy.wait('@createStaffError');
      cy.contains('valid email').should('be.visible');
    });

    it('should validate mecanographic number is required', () => {
      cy.get('[data-cy=name-input]').type('John Doe');
      cy.get('[data-cy=email-input]').type('john@example.com');
      cy.get('[data-cy=phone-input]').type('+351912345678');
      cy.get('[data-cy=submit-btn]').click();
      cy.contains('Mecanographic number is required').should('be.visible');
    });

    it('should validate operational window times', () => {
      cy.intercept('POST', '**/api/OperatingStaff', {
        statusCode: 400,
        body: { errors: ['Start time must be before end time.'] }
      }).as('createStaffError');

      cy.get('[data-cy=mecanographic-number-input]').type('OS001');
      cy.get('[data-cy=name-input]').type('John Doe');
      cy.get('[data-cy=email-input]').type('john@example.com');
      cy.get('[data-cy=phone-input]').type('+351912345678');
      cy.get('[data-cy=qualification-input]').type('Test');
      cy.get('[data-cy=add-qualification-btn]').click();
      cy.get('[data-cy=start-time-input]').type('16:00');
      cy.get('[data-cy=end-time-input]').type('08:00');
      cy.get('[data-cy=submit-btn]').click();
      
      cy.wait('@createStaffError');
      cy.contains('Start time must be before end time').should('be.visible');
    });

    it('should require at least one qualification', () => {
      cy.get('[data-cy=mecanographic-number-input]').type('OS999');
      cy.get('[data-cy=name-input]').type('John Doe');
      cy.get('[data-cy=email-input]').type('john@example.com');
      cy.get('[data-cy=phone-input]').type('+351912345678');
      cy.get('[data-cy=start-time-input]').type('08:00');
      cy.get('[data-cy=end-time-input]').type('16:00');
      cy.get('[data-cy=submit-btn]').click();
      cy.contains('At least one qualification is required').should('be.visible');
    });
  });

  describe('Qualifications Management', () => {
    it('should add qualification to the list', () => {
      cy.get('[data-cy=qualification-input]').type('Crane Operator');
      cy.get('[data-cy=add-qualification-btn]').click();
      cy.get('[data-cy=qualification-item]').should('contain', 'Crane Operator');
    });

    it('should add multiple qualifications', () => {
      cy.get('[data-cy=qualification-input]').type('Crane Operator');
      cy.get('[data-cy=add-qualification-btn]').click();
      
      cy.get('[data-cy=qualification-input]').type('Forklift Driver');
      cy.get('[data-cy=add-qualification-btn]').click();
      
      cy.get('[data-cy=qualification-item]').should('have.length', 2);
    });

    it('should prevent duplicate qualifications', () => {
      cy.get('[data-cy=qualification-input]').type('Crane Operator');
      cy.get('[data-cy=add-qualification-btn]').click();
      
      cy.get('[data-cy=qualification-input]').type('Crane Operator');
      cy.get('[data-cy=add-qualification-btn]').click();
      
      cy.contains('already been added').should('be.visible');
      cy.get('[data-cy=qualification-item]').should('have.length', 1);
    });

    it('should remove qualification from the list', () => {
      cy.get('[data-cy=qualification-input]').type('Crane Operator');
      cy.get('[data-cy=add-qualification-btn]').click();
      
      cy.get('[data-cy=remove-qualification-btn]').first().click();
      cy.get('[data-cy=qualification-item]').should('have.length', 0);
    });
  });

  describe('Operational Window', () => {
    it('should display operational window preview', () => {
      cy.get('[data-cy=days-pattern-select]').select('Mon-Fri');
      cy.get('[data-cy=start-time-input]').type('08:00');
      cy.get('[data-cy=end-time-input]').type('16:00');
      
      cy.get('[data-cy=operational-window-preview]')
        .should('contain', 'Mon-Fri 08:00-16:00');
    });

    it('should allow custom days pattern', () => {
      cy.get('[data-cy=days-pattern-select]').select('Custom');
      cy.get('[data-cy=custom-days-input]').should('be.visible');
      cy.get('[data-cy=custom-days-input]').type('Mon-Wed-Fri');
      cy.get('[data-cy=start-time-input]').type('09:00');
      cy.get('[data-cy=end-time-input]').type('17:00');
      
      cy.get('[data-cy=operational-window-preview]')
        .should('contain', 'Mon-Wed-Fri 09:00-17:00');
    });
  });

  describe('Form Submission', () => {
    it('should create staff member successfully with valid data', () => {
      cy.fixture('operating-staff.json').then((data) => {
        cy.intercept('POST', '**/api/OperatingStaff', {
          statusCode: 201,
          body: data.validStaff
        }).as('createStaff');
      });

      cy.get('[data-cy=mecanographic-number-input]').type('OS001');
      cy.get('[data-cy=name-input]').type('John Doe');
      cy.get('[data-cy=email-input]').type('john.doe@port.com');
      cy.get('[data-cy=phone-input]').type('+351912345678');
      
      cy.get('[data-cy=qualification-input]').type('Crane Operator');
      cy.get('[data-cy=add-qualification-btn]').click();
      
      cy.get('[data-cy=days-pattern-select]').select('Mon-Fri');
      cy.get('[data-cy=start-time-input]').type('08:00');
      cy.get('[data-cy=end-time-input]').type('16:00');
      
      cy.get('[data-cy=submit-btn]').click();
      
      cy.wait('@createStaff');
      cy.contains('created successfully').should('be.visible');
    });

    it('should redirect to list after successful creation', () => {
      cy.fixture('operating-staff.json').then((data) => {
        cy.intercept('POST', '**/api/OperatingStaff', {
          statusCode: 201,
          body: data.validStaff
        }).as('createStaff');
      });

      cy.get('[data-cy=mecanographic-number-input]').type('OS001');
      cy.get('[data-cy=name-input]').type('John Doe');
      cy.get('[data-cy=email-input]').type('john.doe@port.com');
      cy.get('[data-cy=phone-input]').type('+351912345678');
      
      cy.get('[data-cy=qualification-input]').type('Crane Operator');
      cy.get('[data-cy=add-qualification-btn]').click();
      
      cy.get('[data-cy=start-time-input]').type('08:00');
      cy.get('[data-cy=end-time-input]').type('16:00');
      
      cy.get('[data-cy=submit-btn]').click();
      
      cy.wait('@createStaff');
      cy.url().should('include', '/operating-staff', { timeout: 3000 });
    });

    it('should display error when creation fails', () => {
      cy.intercept('POST', '**/api/OperatingStaff', {
        statusCode: 400,
        body: { errors: ['Mecanographic number already exists'] }
      }).as('createStaffError');

      cy.get('[data-cy=mecanographic-number-input]').type('OS001');
      cy.get('[data-cy=name-input]').type('John Doe');
      cy.get('[data-cy=email-input]').type('john.doe@port.com');
      cy.get('[data-cy=phone-input]').type('+351912345678');
      
      cy.get('[data-cy=qualification-input]').type('Crane Operator');
      cy.get('[data-cy=add-qualification-btn]').click();
      
      cy.get('[data-cy=start-time-input]').type('08:00');
      cy.get('[data-cy=end-time-input]').type('16:00');
      
      cy.get('[data-cy=submit-btn]').click();
      
      cy.wait('@createStaffError');
      cy.contains('already exists').should('be.visible');
    });

    it('should disable submit button while submitting', () => {
      cy.intercept('POST', '**/api/OperatingStaff', {
        statusCode: 201,
        delay: 1000,
        body: {}
      }).as('createStaffSlow');

      cy.get('[data-cy=mecanographic-number-input]').type('OS001');
      cy.get('[data-cy=name-input]').type('John Doe');
      cy.get('[data-cy=email-input]').type('john.doe@port.com');
      cy.get('[data-cy=phone-input]').type('+351912345678');
      
      cy.get('[data-cy=qualification-input]').type('Crane Operator');
      cy.get('[data-cy=add-qualification-btn]').click();
      
      cy.get('[data-cy=start-time-input]').type('08:00');
      cy.get('[data-cy=end-time-input]').type('16:00');
      
      cy.get('[data-cy=submit-btn]').click();
      cy.get('[data-cy=submit-btn]').should('be.disabled');
    });
  });

  describe('Cancel Action', () => {
    it('should show confirmation when canceling with unsaved changes', () => {
      cy.get('[data-cy=name-input]').type('John Doe');
      cy.get('[data-cy=cancel-btn]').click();
      
      cy.on('window:confirm', (text) => {
        expect(text).to.contains('unsaved changes');
        return false;
      });
    });

    it('should navigate back to list when cancel is confirmed', () => {
      cy.get('[data-cy=name-input]').type('John Doe');
      cy.get('[data-cy=cancel-btn]').click();
      
      cy.on('window:confirm', () => true);
      cy.url().should('include', '/operating-staff');
      cy.url().should('not.include', '/create');
    });
  });

  describe('Permissions', () => {
    it('should deny access to non-authorized users', () => {
      cy.logout();
      cy.login('ShippingAgentRepresentative');
      cy.visit('/operating-staff/create');
      
      // Should redirect to no-access page
      cy.url().should('include', '/no-access');
      cy.contains('Access Denied').should('be.visible');
      cy.contains("don't have permission").should('be.visible');
    });
  });
});

/**
 * E2E Tests for Updating Operating Staff (US 2.2.11)
 */

describe('Operating Staff Update Form - US 2.2.11', () => {
  beforeEach(() => {
    cy.fixture('operating-staff.json').then((data) => {
      cy.intercept('GET', '**/api/OperatingStaff/mecanographic/OS002', {
        statusCode: 200,
        body: data.staffToUpdate
      }).as('getStaff');
    });

    cy.visit('/');
    cy.login('LogisticsOperator');
    cy.visit('/operating-staff/edit/OS002');
  });

  afterEach(() => {
    cy.logout();
  });

  describe('Form Display', () => {
    it('should display the edit staff form', () => {
      cy.wait('@getStaff');
      cy.contains('Edit Operating Staff Member').should('be.visible');
      cy.get('[data-cy=staff-form]').should('exist');
    });

    it('should load existing staff data into form', () => {
      cy.wait('@getStaff');
      
      cy.get('[data-cy=mecanographic-number-input]')
        .should('have.value', 'OS002')
        .should('be.disabled');
      cy.get('[data-cy=name-input]').should('have.value', 'Jane Smith');
      cy.get('[data-cy=email-input]').should('have.value', 'jane.smith@port.com');
      cy.get('[data-cy=phone-input]').should('have.value', '+351923456789');
    });

    it('should load existing qualifications', () => {
      cy.wait('@getStaff');
      cy.get('[data-cy=qualification-item]').should('contain', 'Truck Driver');
    });

    it('should parse and display operational window correctly', () => {
      cy.wait('@getStaff');
      cy.get('[data-cy=days-pattern-select]').should('have.value', 'Mon-Sat');
      cy.get('[data-cy=start-time-input]').should('have.value', '09:00');
      cy.get('[data-cy=end-time-input]').should('have.value', '17:00');
    });

    it('should show loading indicator while fetching data', () => {
      cy.intercept('GET', '**/api/OperatingStaff/mecanographic/OS002', {
        statusCode: 200,
        delay: 2000,
        body: {
          mecanographicNumber: 'OS002',
          name: 'Jane Smith',
          email: 'jane.smith@port.com',
          phoneNumber: '+351923456789',
          qualifications: ['Truck Driver'],
          operationalWindow: 'Mon-Sat 09:00-17:00',
          status: 'Available'
        }
      }).as('getStaffSlow');

      cy.visit('/operating-staff/edit/OS002');
      cy.get('[data-cy=loading-indicator]').should('be.visible');
      cy.wait('@getStaffSlow');
      cy.get('[data-cy=loading-indicator]').should('not.exist');
    });

    it('should display error when staff not found', () => {
      cy.intercept('GET', '**/api/OperatingStaff/mecanographic/OS999', {
        statusCode: 404,
        body: { message: 'Staff member not found' }
      }).as('getStaffNotFound');

      cy.visit('/operating-staff/edit/OS999');
      cy.wait('@getStaffNotFound');
      cy.wait(500); // Wait for error to render
      cy.get('.alert').should('be.visible');
    });
  });

  describe('Form Editing', () => {
    it('should allow updating name', () => {
      cy.wait('@getStaff');
      cy.get('[data-cy=name-input]').clear().type('Jane Updated Smith');
      cy.get('[data-cy=name-input]').should('have.value', 'Jane Updated Smith');
    });

    it('should allow updating email', () => {
      cy.wait('@getStaff');
      cy.get('[data-cy=email-input]').clear().type('jane.updated@port.com');
      cy.get('[data-cy=email-input]').should('have.value', 'jane.updated@port.com');
    });

    it('should allow updating phone number', () => {
      cy.wait('@getStaff');
      cy.get('[data-cy=phone-input]').clear().type('+351999888777');
      cy.get('[data-cy=phone-input]').should('have.value', '+351999888777');
    });

    it('should allow adding new qualifications', () => {
      cy.wait('@getStaff');
      
      cy.get('[data-cy=qualification-input]').type('Crane Operator');
      cy.get('[data-cy=add-qualification-btn]').click();
      
      cy.get('[data-cy=qualification-item]').should('have.length', 2);
      cy.get('[data-cy=qualification-item]').should('contain', 'Crane Operator');
    });

    it('should allow removing existing qualifications', () => {
      cy.wait('@getStaff');
      
      cy.get('[data-cy=remove-qualification-btn]').first().click();
      cy.get('[data-cy=qualification-item]').should('have.length', 0);
    });

    it('should allow updating operational window', () => {
      cy.wait('@getStaff');
      
      cy.get('[data-cy=days-pattern-select]').select('Mon-Fri');
      cy.get('[data-cy=start-time-input]').clear().type('08:00');
      cy.get('[data-cy=end-time-input]').clear().type('16:00');
      
      cy.get('[data-cy=operational-window-preview]')
        .should('contain', 'Mon-Fri 08:00-16:00');
    });

    it('should not allow editing mecanographic number', () => {
      cy.wait('@getStaff');
      cy.get('[data-cy=mecanographic-number-input]').should('be.disabled');
    });
  });

  describe('Form Validation', () => {
    it('should validate required fields on update', () => {
      cy.wait('@getStaff');
      
      cy.get('[data-cy=name-input]').clear();
      cy.get('[data-cy=submit-btn]').click();
      cy.contains('Name is required').should('be.visible');
    });

    it('should validate email format on update', () => {
      cy.intercept('PUT', '**/api/OperatingStaff/OS002', {
        statusCode: 400,
        body: { errors: ['Please enter a valid email address.'] }
      }).as('updateStaffError');
      
      cy.wait('@getStaff');
      
      cy.get('[data-cy=email-input]').clear().type('invalid-email');
      cy.get('[data-cy=submit-btn]').click();
      cy.wait('@updateStaffError');
      cy.contains('valid email').should('be.visible');
    });

    it('should validate operational window times on update', () => {
      cy.intercept('PUT', '**/api/OperatingStaff/OS002', {
        statusCode: 400,
        body: { errors: ['Start time must be before end time.'] }
      }).as('updateStaffError');
      
      cy.wait('@getStaff');
      
      cy.get('[data-cy=start-time-input]').clear().type('18:00');
      cy.get('[data-cy=end-time-input]').clear().type('09:00');
      cy.get('[data-cy=submit-btn]').click();
      cy.wait('@updateStaffError');
      cy.contains('Start time must be before end time').should('be.visible');
    });

    it('should require at least one qualification', () => {
      cy.wait('@getStaff');
      
      cy.get('[data-cy=remove-qualification-btn]').first().click();
      cy.get('[data-cy=submit-btn]').click();
      cy.contains('At least one qualification is required').should('be.visible');
    });
  });

  describe('Form Submission', () => {
    it('should update staff member successfully', () => {
      cy.fixture('operating-staff.json').then((data) => {
        const updatedStaff = { ...data.staffToUpdate, name: 'Jane Updated' };
        
        cy.intercept('PUT', '**/api/OperatingStaff/OS002', {
          statusCode: 200,
          body: updatedStaff
        }).as('updateStaff');
      });

      cy.wait('@getStaff');
      
      cy.get('[data-cy=name-input]').clear().type('Jane Updated');
      cy.get('[data-cy=submit-btn]').click();
      
      cy.wait('@updateStaff');
      cy.contains('updated successfully').should('be.visible');
    });

    it('should send correct data to API', () => {
      cy.intercept('PUT', '**/api/OperatingStaff/OS002', (req) => {
        expect(req.body).to.have.property('name');
        expect(req.body).to.have.property('email');
        expect(req.body).to.have.property('phoneNumber');
        expect(req.body).to.have.property('qualifications');
        expect(req.body).to.have.property('operationalWindow');
        expect(req.body).not.to.have.property('mecanographicNumber');
        
        req.reply({ statusCode: 200, body: req.body });
      }).as('updateStaff');

      cy.wait('@getStaff');
      
      cy.get('[data-cy=name-input]').clear().type('Jane Updated');
      cy.get('[data-cy=submit-btn]').click();
      
      cy.wait('@updateStaff');
    });

    it('should redirect to list after successful update', () => {
      cy.fixture('operating-staff.json').then((data) => {
        cy.intercept('PUT', '**/api/OperatingStaff/OS002', {
          statusCode: 200,
          body: data.staffToUpdate
        }).as('updateStaff');
      });

      cy.wait('@getStaff');
      
      cy.get('[data-cy=name-input]').clear().type('Jane Updated');
      cy.get('[data-cy=submit-btn]').click();
      
      cy.wait('@updateStaff');
      cy.url().should('include', '/operating-staff', { timeout: 3000 });
      cy.url().should('not.include', '/edit');
    });

    it('should display error when update fails', () => {
      cy.intercept('PUT', '**/api/OperatingStaff/OS002', {
        statusCode: 400,
        body: { errors: ['Invalid email format'] }
      }).as('updateStaffError');

      cy.wait('@getStaff');
      
      cy.get('[data-cy=email-input]').clear().type('invalid@');
      cy.get('[data-cy=submit-btn]').click();
      
      cy.wait('@updateStaffError');
      cy.contains('Invalid email format').should('be.visible');
    });

    it('should disable submit button while submitting', () => {
      cy.intercept('PUT', '**/api/OperatingStaff/OS002', {
        statusCode: 200,
        delay: 1000,
        body: {}
      }).as('updateStaffSlow');

      cy.wait('@getStaff');
      
      cy.get('[data-cy=name-input]').clear().type('Jane Updated');
      cy.get('[data-cy=submit-btn]').click();
      cy.get('[data-cy=submit-btn]').should('be.disabled');
    });
  });

  describe('Cancel Action', () => {
    it('should show confirmation when canceling with unsaved changes', () => {
      cy.wait('@getStaff');
      
      cy.get('[data-cy=name-input]').clear().type('Jane Modified');
      cy.get('[data-cy=cancel-btn]').click();
      
      cy.on('window:confirm', (text) => {
        expect(text).to.contains('unsaved changes');
        return false;
      });
    });

    it('should navigate back to list when cancel is confirmed', () => {
      cy.wait('@getStaff');
      
      cy.get('[data-cy=name-input]').clear().type('Jane Modified');
      cy.get('[data-cy=cancel-btn]').click();
      
      cy.on('window:confirm', () => true);
      cy.url().should('include', '/operating-staff');
      cy.url().should('not.include', '/edit');
    });
  });

  describe('Permissions', () => {
    it('should deny access to non-authorized users', () => {
      cy.logout();
      cy.login('ShippingAgentRepresentative');
      cy.visit('/operating-staff/edit/OS002');
      
      cy.url().should('include', '/no-access');
      cy.contains('Access Denied').should('be.visible');
    });
  });
});

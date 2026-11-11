/// <reference types="cypress" />

/**
 * E2E Tests for Approving and Rejecting Vessel Visit Notifications
 * US 2.2.7 - Port Authority Officer reviews and decides on VVNs
 */

describe('VVN Approve/Reject - US 2.2.7', () => {
  let vvnData: any;

  before(() => {
    cy.fixture('vvn').then((data) => {
      vvnData = data;
    });
  });

  beforeEach(() => {
    cy.login('PortAuthorityOfficer');
    
    // Setup default docks intercept
    cy.intercept('GET', '**/api/Docks*', {
      statusCode: 200,
      body: []
    }).as('getDocks');
  });

  afterEach(() => {
    cy.logout();
  });

  describe('Approve Notification', () => {
    it('should display approve and reject buttons for submitted notifications', () => {
      cy.intercept('GET', `**/api/vessel-visits/notifications/${vvnData.submittedNotification.id}`, {
        statusCode: 200,
        body: vvnData.submittedNotification
      }).as('getNotification');

      cy.visit(`/vessel-visits/${vvnData.submittedNotification.id}/review`);
      cy.wait('@getNotification');

      cy.contains('Submitted').should('be.visible');
      cy.contains('button', 'Approve').should('be.visible');
      cy.contains('button', 'Reject').should('be.visible');
    });

    it('should open approve modal when clicking approve button', () => {
      cy.intercept('GET', `**/api/vessel-visits/notifications/${vvnData.submittedNotification.id}`, {
        statusCode: 200,
        body: vvnData.submittedNotification
      }).as('getNotification');

      cy.intercept('GET', '**/api/Docks*', {
        statusCode: 200,
        body: vvnData.docks
      }).as('getDocksData');

      cy.visit(`/vessel-visits/${vvnData.submittedNotification.id}/review`);
      cy.wait('@getNotification');

      cy.contains('button', 'Approve').click();
      cy.contains('Approve Notification').should('be.visible');
      cy.get('select#assignedDockCode').should('be.visible');
    });

    it('should successfully approve notification with dock assignment', () => {
      cy.intercept('GET', `**/api/vessel-visits/notifications/${vvnData.submittedNotification.id}`, {
        statusCode: 200,
        body: vvnData.submittedNotification
      }).as('getNotification');

      cy.intercept('GET', '**/api/Docks*', {
        statusCode: 200,
        body: vvnData.docks
      }).as('getDocksData');

      cy.intercept('GET', `**/api/vessel-visits/notifications/${vvnData.submittedNotification.id}/audit-trail`, {
        statusCode: 200,
        body: []
      }).as('getAuditTrail');

      cy.intercept('PATCH', '**/api/vessel-visits/notifications/*/decision*', {
        statusCode: 200,
        body: { ...vvnData.submittedNotification, status: 'Approved', assignedDockCode: 'DOCK-A' }
      }).as('approveNotification');

      cy.visit(`/vessel-visits/${vvnData.submittedNotification.id}/review`);
      cy.wait('@getNotification');

      cy.contains('button', 'Approve').click();
      cy.get('select#assignedDockCode').select('DOCK-A');
      cy.get('.modal.show .modal-footer button[type="submit"]').click();

      cy.wait('@approveNotification');
      cy.contains('approved successfully').should('be.visible');
    });

    it('should require dock code when approving', () => {
      cy.intercept('GET', `**/api/vessel-visits/notifications/${vvnData.submittedNotification.id}`, {
        statusCode: 200,
        body: vvnData.submittedNotification
      }).as('getNotification');

      cy.intercept('GET', '**/api/Docks*', {
        statusCode: 200,
        body: vvnData.docks
      }).as('getDocksData');

      cy.intercept('GET', `**/api/vessel-visits/notifications/${vvnData.submittedNotification.id}/audit-trail`, {
        statusCode: 200,
        body: []
      }).as('getAuditTrail');

      cy.visit(`/vessel-visits/${vvnData.submittedNotification.id}/review`);
      cy.wait('@getNotification');

      cy.contains('button', 'Approve').click();
      
      // Clear any pre-filled value
      cy.get('select#assignedDockCode').select('');
      
      // Submit button should be disabled when form is invalid
      cy.get('.modal.show .modal-footer button[type="submit"]').should('be.disabled');
      
      // Select should show as invalid when touched
      cy.get('select#assignedDockCode').focus().blur();
      cy.get('select#assignedDockCode').should('have.class', 'ng-invalid');
    });

    it('should show error when approve API fails', () => {
      cy.intercept('GET', `**/api/vessel-visits/notifications/${vvnData.submittedNotification.id}`, {
        statusCode: 200,
        body: vvnData.submittedNotification
      }).as('getNotification');

      cy.intercept('GET', '**/api/Docks*', {
        statusCode: 200,
        body: vvnData.docks
      }).as('getDocksData');

      cy.intercept('GET', `**/api/vessel-visits/notifications/${vvnData.submittedNotification.id}/audit-trail`, {
        statusCode: 200,
        body: []
      }).as('getAuditTrail');

      cy.intercept('PATCH', '**/api/vessel-visits/notifications/*/decision*', {
        statusCode: 400,
        body: { errors: ['Dock is not available'] }
      }).as('approveNotificationFail');

      cy.visit(`/vessel-visits/${vvnData.submittedNotification.id}/review`);
      cy.wait('@getNotification');

      cy.contains('button', 'Approve').click();
      cy.get('select#assignedDockCode').select('DOCK-A');
      cy.get('.modal.show .modal-footer button[type="submit"]').click();

      cy.wait('@approveNotificationFail');
      cy.contains('Failed to approve').should('be.visible');
    });
  });

  describe('Reject Notification', () => {
    it('should open reject modal when clicking reject button', () => {
      cy.intercept('GET', `**/api/vessel-visits/notifications/${vvnData.submittedNotification.id}`, {
        statusCode: 200,
        body: vvnData.submittedNotification
      }).as('getNotification');

      cy.intercept('GET', `**/api/vessel-visits/notifications/${vvnData.submittedNotification.id}/audit-trail`, {
        statusCode: 200,
        body: []
      }).as('getAuditTrail');

      cy.visit(`/vessel-visits/${vvnData.submittedNotification.id}/review`);
      cy.wait('@getNotification');

      cy.contains('button', 'Reject').click();
      cy.contains('Reject Notification').should('be.visible');
      cy.get('textarea#rejectionReason').should('be.visible');
    });

    it('should successfully reject notification with reason', () => {
      cy.intercept('GET', `**/api/vessel-visits/notifications/${vvnData.submittedNotification.id}`, {
        statusCode: 200,
        body: vvnData.submittedNotification
      }).as('getNotification');

      cy.intercept('GET', `**/api/vessel-visits/notifications/${vvnData.submittedNotification.id}/audit-trail`, {
        statusCode: 200,
        body: []
      }).as('getAuditTrail');

      cy.intercept('PATCH', '**/api/vessel-visits/notifications/*/decision*', {
        statusCode: 200,
        body: { 
          ...vvnData.submittedNotification, 
          status: 'Rejected', 
          rejectionReason: 'Missing required documentation' 
        }
      }).as('rejectNotification');

      cy.visit(`/vessel-visits/${vvnData.submittedNotification.id}/review`);
      cy.wait('@getNotification');

      cy.contains('button', 'Reject').click();
      cy.get('textarea#rejectionReason').type('Missing required documentation');
      cy.get('.modal.show .modal-footer button[type="submit"]').click();

      cy.wait('@rejectNotification');
      cy.contains('rejected successfully').should('be.visible');
    });

    it('should require rejection reason with minimum length', () => {
      cy.intercept('GET', `**/api/vessel-visits/notifications/${vvnData.submittedNotification.id}`, {
        statusCode: 200,
        body: vvnData.submittedNotification
      }).as('getNotification');

      cy.intercept('GET', `**/api/vessel-visits/notifications/${vvnData.submittedNotification.id}/audit-trail`, {
        statusCode: 200,
        body: []
      }).as('getAuditTrail');

      cy.visit(`/vessel-visits/${vvnData.submittedNotification.id}/review`);
      cy.wait('@getNotification');

      cy.contains('button', 'Reject').click();
      
      // Submit button should be disabled when form is invalid (empty)
      cy.get('.modal.show .modal-footer button[type="submit"]').should('be.disabled');

      // Try with too short reason
      cy.get('textarea#rejectionReason').type('Short');
      // Button still disabled because minlength not met
      cy.get('.modal.show .modal-footer button[type="submit"]').should('be.disabled');
      
      // Trigger touched state to show validation message
      cy.get('textarea#rejectionReason').clear().blur();
      cy.contains('minimum 10 characters').should('be.visible');
    });

    it('should show error when reject API fails', () => {
      cy.intercept('GET', `**/api/vessel-visits/notifications/${vvnData.submittedNotification.id}`, {
        statusCode: 200,
        body: vvnData.submittedNotification
      }).as('getNotification');

      cy.intercept('GET', `**/api/vessel-visits/notifications/${vvnData.submittedNotification.id}/audit-trail`, {
        statusCode: 200,
        body: []
      }).as('getAuditTrail');

      cy.intercept('PATCH', '**/api/vessel-visits/notifications/*/decision*', {
        statusCode: 500,
        body: { error: 'Internal server error' }
      }).as('rejectNotificationFail');

      cy.visit(`/vessel-visits/${vvnData.submittedNotification.id}/review`);
      cy.wait('@getNotification');

      cy.contains('button', 'Reject').click();
      cy.get('textarea#rejectionReason').type('Missing required documentation');
      cy.get('.modal.show .modal-footer button[type="submit"]').click();

      cy.wait('@rejectNotificationFail');
      cy.contains('Failed to reject').should('be.visible');
    });
  });

  describe('Decision Permissions', () => {
    it('should not show approve/reject buttons for already approved notifications', () => {
      cy.intercept('GET', `**/api/vessel-visits/notifications/${vvnData.approvedNotification.id}`, {
        statusCode: 200,
        body: vvnData.approvedNotification
      }).as('getNotification');

      cy.intercept('GET', `**/api/vessel-visits/notifications/${vvnData.approvedNotification.id}/audit-trail`, {
        statusCode: 200,
        body: []
      }).as('getAuditTrail');

      cy.visit(`/vessel-visits/${vvnData.approvedNotification.id}/review`);
      cy.wait('@getNotification');

      cy.get('.card-header.bg-warning').should('not.exist');
      cy.contains('Approved').should('be.visible');
    });

    it('should not show approve/reject buttons for rejected notifications', () => {
      cy.intercept('GET', `**/api/vessel-visits/notifications/${vvnData.rejectedNotification.id}`, {
        statusCode: 200,
        body: vvnData.rejectedNotification
      }).as('getNotification');

      cy.intercept('GET', `**/api/vessel-visits/notifications/${vvnData.rejectedNotification.id}/audit-trail`, {
        statusCode: 200,
        body: []
      }).as('getAuditTrail');

      cy.visit(`/vessel-visits/${vvnData.rejectedNotification.id}/review`);
      cy.wait('@getNotification');

      cy.get('.card-header.bg-warning').should('not.exist');
      cy.contains('Rejected').should('be.visible');
    });

    it('should not show approve/reject buttons for InProgress notifications', () => {
      cy.intercept('GET', `**/api/vessel-visits/notifications/${vvnData.inProgressNotification.id}`, {
        statusCode: 200,
        body: vvnData.inProgressNotification
      }).as('getNotification');

      cy.intercept('GET', `**/api/vessel-visits/notifications/${vvnData.inProgressNotification.id}/audit-trail`, {
        statusCode: 200,
        body: []
      }).as('getAuditTrail');

      cy.visit(`/vessel-visits/${vvnData.inProgressNotification.id}/review`);
      cy.wait('@getNotification');

      cy.get('.card-header.bg-warning').should('not.exist');
      cy.contains('InProgress').should('be.visible');
    });
  });
});

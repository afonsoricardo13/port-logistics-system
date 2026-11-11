describe('VVN List - US 2.2.10', () => {
  beforeEach(() => {
    cy.intercept('GET', '**/api/vessel-visits/notifications/organization-notifications', {
      statusCode: 200,
      body: [
        {
          id: 1,
          vesselImoNumber: 'IMO1234567',
          estimatedTimeOfArrival: '2025-06-15T10:00:00Z',
          estimatedTimeOfDeparture: '2025-06-16T18:00:00Z',
          status: 'InProgress',
          submittedByRepresentativeId: 1,
          containerIds: ['ABCD1234567'],
          organizationIdentifier: 'ORG001'
        },
        {
          id: 2,
          vesselImoNumber: 'IMO7654321',
          estimatedTimeOfArrival: '2025-06-20T08:00:00Z',
          estimatedTimeOfDeparture: '2025-06-21T16:00:00Z',
          status: 'Submitted',
          submittedByRepresentativeId: 1,
          containerIds: ['EFGH7654321'],
          organizationIdentifier: 'ORG001'
        },
        {
          id: 3,
          vesselImoNumber: 'IMO1111111',
          estimatedTimeOfArrival: '2025-06-25T12:00:00Z',
          estimatedTimeOfDeparture: '2025-06-26T20:00:00Z',
          status: 'Approved',
          assignedDockCode: 'DOCK-A',
          submittedByRepresentativeId: 1,
          containerIds: ['MNOP2222222'],
          organizationIdentifier: 'ORG001'
        },
        {
          id: 4,
          vesselImoNumber: 'IMO2222222',
          estimatedTimeOfArrival: '2025-06-30T14:00:00Z',
          estimatedTimeOfDeparture: '2025-07-01T22:00:00Z',
          status: 'Rejected',
          rejectionReason: 'Missing cargo manifest',
          submittedByRepresentativeId: 1,
          containerIds: [],
          organizationIdentifier: 'ORG001'
        }
      ]
    }).as('getNotifications');

    cy.login('ShippingAgentRepresentative');
    cy.visit('/vessel-visits', { failOnStatusCode: false });
  });

  afterEach(() => {
    cy.logout();
  });

  it('should display notifications list', () => {
    cy.wait('@getNotifications');
    cy.contains('Vessel Visit Notifications').should('be.visible');
  });

  it('should display all notifications', () => {
    cy.wait('@getNotifications');
    cy.get('tbody tr').should('have.length', 4);
  });

  it('should display InProgress status', () => {
    cy.wait('@getNotifications');
    cy.contains('IMO1234567').should('be.visible');
    cy.contains('In Progress').should('be.visible');
  });

  it('should display Approved status with dock', () => {
    cy.wait('@getNotifications');
    cy.contains('IMO1111111').should('be.visible');
    cy.contains('Approved').should('be.visible');
    cy.contains('DOCK-A').should('be.visible');
  });

  it('should display Rejected status', () => {
    cy.wait('@getNotifications');
    cy.contains('IMO2222222').should('be.visible');
    cy.contains('Rejected').should('be.visible');
  });

  it('should filter by vessel IMO', () => {
    cy.wait('@getNotifications');
    cy.get('input[placeholder*="IMO"]').type('IMO1234567');
    cy.get('tbody tr').should('have.length', 1);
  });

  it('should filter by status', () => {
    cy.wait('@getNotifications');
    cy.get('select#status').select('Approved');
    cy.get('tbody tr').should('have.length', 1);
    cy.contains('DOCK-A').should('be.visible');
  });

  it('should clear filters', () => {
    cy.wait('@getNotifications');
    cy.get('input[placeholder*="IMO"]').type('IMO1234567');
    cy.get('select#status').select('Approved');
    cy.contains('Clear Filters').click();
    cy.get('input[placeholder*="IMO"]').should('have.value', '');
    cy.get('tbody tr').should('have.length', 4);
  });

  it('should navigate to details', () => {
    cy.wait('@getNotifications');
    cy.get('tbody tr').first().within(() => {
      cy.contains('View').click();
    });
    cy.url().should('include', '/vessel-visits/');
  });
});

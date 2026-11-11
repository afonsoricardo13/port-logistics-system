describe('Missing Operation Plans - US 4.1.5', () => {
  beforeEach(() => {
    cy.intercept('GET', '**/api/Incidents*', { statusCode: 200, body: [] });
    cy.intercept('GET', '**/api/OperationPlans/missing-plans*', {
      statusCode: 200,
      body: [
        {
          id: 1,
          vesselImoNumber: 'IMO1234567',
          estimatedTimeOfArrival: '2025-06-15T10:00:00Z',
          estimatedTimeOfDeparture: '2025-06-16T18:00:00Z',
          status: 'Approved',
          assignedDockCode: 'DOCK-A'
        }
      ]
    }).as('getMissingPlans');

    cy.login('LogisticsOperator');
    cy.visit('/operations');
  });

  afterEach(() => {
    cy.logout();
  });

  it('should display operations page', () => {
    cy.contains('Operations').should('be.visible');
  });

  it('should have Operation Plans tab', () => {
    cy.contains('Operation Plans').should('be.visible');
  });

  it('should navigate to Missing Plans tab', () => {
    cy.contains('Operation Plans').click();
    cy.contains('Missing Plans').should('be.visible');
  });

  it('should display missing plans section', () => {
    cy.contains('Operation Plans').click();
    cy.contains('Missing Plans').click();
    cy.contains('Identify VVNs without Operation Plans').should('be.visible');
  });

  it('should have target date selector', () => {
    cy.contains('Operation Plans').click();
    cy.contains('Missing Plans').click();
    cy.contains('Target Date').should('be.visible');
  });

  it('should have check missing plans button', () => {
    cy.contains('Operation Plans').click();
    cy.contains('Missing Plans').click();
    cy.contains('Check Missing Plans').should('be.visible');
  });

  it('should call API when checking missing plans', () => {
    cy.contains('Operation Plans').click();
    cy.contains('Missing Plans').click();
    cy.contains('Check Missing Plans').click();
    cy.wait('@getMissingPlans');
  });

  it('should support date selection', () => {
    cy.contains('Operation Plans').click();
    cy.contains('Missing Plans').click();
    cy.get('input[type="date"]').should('exist');
  });

  it('should have REST endpoint for missing plans', () => {
    cy.contains('Operation Plans').click();
    cy.contains('Missing Plans').click();
    cy.contains('Check Missing Plans').click();
    cy.wait('@getMissingPlans').its('response.statusCode').should('eq', 200);
  });

  it('should support plan regeneration concept', () => {
    cy.contains('Operation Plans').click();
    cy.contains('Generate Plans').should('exist');
  });
});

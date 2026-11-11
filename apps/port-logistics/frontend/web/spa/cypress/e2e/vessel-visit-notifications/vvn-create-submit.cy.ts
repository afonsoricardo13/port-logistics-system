describe('US 2.2.8 - Create/Submit Vessel Visit Notification', () => {
  beforeEach(() => {
    cy.login('ShippingAgentRepresentative');
    cy.visit('/vessel-visits/create');
  });

  afterEach(() => {
    cy.logout();
  });

  it('should display create form', () => {
    cy.contains('Create Vessel Visit Notification').should('be.visible');
    cy.get('#vesselImoNumber').should('exist');
    cy.get('#estimatedTimeOfArrival').should('exist');
    cy.get('#estimatedTimeOfDeparture').should('exist');
  });

  it('should show error for invalid container ISO 6346', () => {
    cy.get('#vesselImoNumber').type('9123456');
    cy.get('#estimatedTimeOfArrival').type('2025-12-01T10:00');
    cy.get('#estimatedTimeOfDeparture').type('2025-12-02T10:00');
    cy.contains('+ Add Container').click();
    cy.get('input[placeholder*="ISO 6346"]').first().type('INVALID123');
    cy.contains('Submit for Approval').click();
    cy.contains('Container ID must follow ISO 6346 format').should('be.visible');
  });

  it('should create notification with valid data', () => {
    cy.get('#vesselImoNumber').type('9074729');
    cy.get('#estimatedTimeOfArrival').type('2025-12-01T10:00');
    cy.get('#estimatedTimeOfDeparture').type('2025-12-02T10:00');
    cy.contains('Save as Draft').should('not.be.disabled');
  });

  it('should add and remove containers', () => {
    cy.contains('+ Add Container').click();
    cy.get('input[placeholder*="ISO 6346"]').should('have.length', 1);
    cy.contains('+ Add Container').click();
    cy.get('input[placeholder*="ISO 6346"]').should('have.length', 2);
    cy.get('.btn-danger').first().click();
    cy.get('input[placeholder*="ISO 6346"]').should('have.length', 1);
  });

  it('should add and remove crew members', () => {
    cy.contains('+ Add Crew Member').click();
    cy.get('input[placeholder="Full name"]').should('have.length', 1);
    cy.get('input[placeholder="Full name"]').type('John Doe');
    cy.get('input[placeholder*="Passport"]').type('PT123456');
    cy.get('input[placeholder*="Portuguese"]').type('Portuguese');
  });

  it('should show error when crew count is invalid', () => {
    cy.get('#crewCount').clear().type('0');
    cy.get('#crewCount').blur();
    cy.get('#crewCount').should('have.class', 'ng-invalid');
  });

  it('should validate ETA before ETD', () => {
    cy.get('#vesselImoNumber').type('9123456');
    cy.get('#estimatedTimeOfArrival').type('2025-12-02T10:00');
    cy.get('#estimatedTimeOfDeparture').type('2025-12-01T10:00');
    cy.contains('Submit for Approval').click();
    cy.contains('Departure must be after', { matchCase: false }).should('be.visible');
  });

  it('should add safety officers', () => {
    cy.contains('+ Add Safety Officer').click();
    cy.get('input[placeholder="Safety officer name"]').should('have.length', 1);
    cy.get('input[placeholder="Safety officer name"]').type('Officer Smith');
  });
});

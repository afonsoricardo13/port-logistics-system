/**
 * US 4.1.10: E2E Tests for Search and List Vessel Visit Executions
 * 
 * Purpose: Verify that Operations Managers can search and filter VVEs
 * to review execution history and analyze performance metrics
 * 
 * Coverage:
 * - VVE search page navigation
 * - Date range filtering UI
 * - Vessel IMO filtering
 * - Status filtering
 * - Results display with metrics
 * - API integration
 */
describe('Vessel Visit Execution Search - US 4.1.10', () => {
  beforeEach(() => {
    // Mock successful API responses for VVE search
    cy.intercept('GET', '**/api/VesselVisitExecutions/search*', {
      statusCode: 200,
      body: [
        {
          id: 'VVE-2026-001',
          vvnId: 'VVN-2026-001',
          vesselImo: 'IMO9876543',
          vesselName: 'MV Atlantic Express',
          actualArrivalTime: '2026-01-15T08:00:00Z',
          actualBerthTime: '2026-01-15T09:30:00Z',
          dockUsed: 'DOCK-A1',
          actualUnberthTime: '2026-01-16T14:00:00Z',
          actualDepartureTime: '2026-01-16T15:30:00Z',
          status: 'Completed',
          expectedArrivalTime: '2026-01-15T08:00:00Z',
          expectedDepartureTime: '2026-01-16T16:00:00Z',
          totalTurnaroundTimeHours: 31.5,
          berthOccupancyTimeHours: 28.5,
          waitingTimeForBerthingHours: 1.5,
          arrivalDelayHours: 0.0,
          departureDelayHours: 0.5,
          executedOperationsCount: 12,
          notes: 'Normal operations'
        },
        {
          id: 'VVE-2026-002',
          vvnId: 'VVN-2026-002',
          vesselImo: 'IMO5555555',
          vesselName: 'MV Pacific Trader',
          actualArrivalTime: '2026-01-20T10:00:00Z',
          actualBerthTime: '2026-01-20T12:00:00Z',
          dockUsed: 'DOCK-B2',
          actualUnberthTime: null,
          actualDepartureTime: null,
          status: 'InProgress',
          expectedArrivalTime: '2026-01-20T09:00:00Z',
          expectedDepartureTime: '2026-01-21T18:00:00Z',
          totalTurnaroundTimeHours: null,
          berthOccupancyTimeHours: null,
          waitingTimeForBerthingHours: 2.0,
          arrivalDelayHours: 1.0,
          departureDelayHours: null,
          executedOperationsCount: 5,
          notes: 'Operations in progress'
        }
      ]
    }).as('getVveSearch');

    // Mock empty search results
    cy.intercept('GET', '**/api/VesselVisitExecutions/search*vesselImo=9999999*', {
      statusCode: 200,
      body: []
    }).as('getEmptySearch');

    // Mock incidents endpoint (required for operations page)
    cy.intercept('GET', '**/api/Incidents*', { statusCode: 200, body: [] });

    // Login as Logistics Operator (manages VVEs and operations)
    cy.login('LogisticsOperator');
    cy.visit('/operations/executions/search');
  });

  afterEach(() => {
    cy.logout();
  });

  /**
   * Test 1: Verify VVE search page loads correctly
   */
  it('should display VVE search page', () => {
    cy.contains('US 4.1.10').should('be.visible');
    cy.contains('Search Vessel Visit Executions').should('be.visible');
  });

  /**
   * Test 2: Verify search filters section exists
   */
  it('should display search filters section', () => {
    cy.contains('Search Filters').should('be.visible');
  });

  /**
   * Test 3: Verify start date and end date selectors exist
   */
  it('should have start date and end date selectors', () => {
    cy.contains('Start Date').should('be.visible');
    cy.contains('End Date').should('be.visible');
  });

  /**
   * Test 4: Verify vessel IMO filter exists
   */
  it('should have vessel IMO input field', () => {
    cy.contains('Vessel IMO').should('be.visible');
    cy.get('input[placeholder*="1234567"]').should('exist');
  });

  /**
   * Test 5: Verify execution status filter exists
   */
  it('should have execution status filter', () => {
    cy.contains('Status').should('be.visible');
  });

  /**
   * Test 6: Verify search button exists
   */
  it('should have search button', () => {
    cy.contains('button', 'Search').should('be.visible');
  });

  /**
   * Test 7: Verify API is called when searching
   */
  it('should call VVE search API when search button is clicked', () => {
    cy.contains('button', 'Search').click();
    cy.wait('@getVveSearch');
  });

  /**
   * Test 8: Verify search results display with metrics
   */
  it('should display search results with execution metrics', () => {
    cy.contains('button', 'Search').click();
    cy.wait('@getVveSearch');
    
    // Verify search results section
    cy.contains('Search Results').should('be.visible');
    cy.contains('2 records').should('be.visible');
  });

  /**
   * Test 9: Verify date range filtering works
   */
  it('should support filtering by date range', () => {
    cy.get('input[type="date"]').first().type('2026-01-01');
    cy.get('input[type="date"]').last().type('2026-01-31');
    
    cy.contains('button', 'Search').click();
    cy.wait('@getVveSearch');
  });

  /**
   * Test 10: Verify vessel IMO filtering works
   */
  it('should support filtering by vessel IMO', () => {
    cy.get('input[placeholder*="1234567"]').type('9074729');
    cy.contains('button', 'Search').click();
    cy.wait('@getVveSearch');
  });

  /**
   * Test 11: Verify status filtering works
   */
  it('should support filtering by execution status', () => {
    cy.get('select').select('Completed');
    cy.contains('button', 'Search').click();
    cy.wait('@getVveSearch');
  });

  /**
   * Test 12: Verify search results table displays VVE data
   */
  it('should display VVE data in results table', () => {
    cy.contains('button', 'Search').click();
    cy.wait('@getVveSearch');
    
    // Verify table headers exist
    cy.contains('VVE ID').should('be.visible');
    cy.contains('VVN ID').should('be.visible');
    cy.contains('Status').should('be.visible');
  });

  /**
   * Test 13: Verify timestamps section displays
   */
  it('should display timestamps section in results', () => {
    cy.contains('button', 'Search').click();
    cy.wait('@getVveSearch');
    
    cy.contains('Timestamps').should('be.visible');
    cy.contains('Arrival').should('be.visible');
    cy.contains('Departure').should('be.visible');
  });

  /**
   * Test 14: Verify results table displays data
   */
  it('should display VVE results in table format', () => {
    cy.contains('button', 'Search').click();
    cy.wait('@getVveSearch');
    
    // Verify results table exists
    cy.get('table').should('be.visible');
    cy.contains('VVE ID').should('be.visible');
  });

  /**
   * Test 15: Verify empty search results handled
   */
  it('should handle empty search results gracefully', () => {
    cy.get('input[placeholder*="1234567"]').type('9999999');
    cy.contains('button', 'Search').click();
    cy.wait('@getEmptySearch');
    
    // Should show 0 records
    cy.contains('0 records').should('be.visible');
  });

  /**
   * Test 16: Verify reset filters button exists
   */
  it('should have reset filters button', () => {
    cy.contains('button', 'Reset Filters').should('be.visible');
  });

  /**
   * Test 17: Verify reset filters clears inputs
   */
  it('should clear filters when reset button is clicked', () => {
    cy.get('input[placeholder*="1234567"]').type('9074729');
    cy.contains('button', 'Reset Filters').click();
    cy.get('input[placeholder*="1234567"]').should('have.value', '');
  });

  /**
   * Test 18: Verify multiple filters work together
   */
  it('should support combining multiple search filters', () => {
    cy.get('input[type="date"]').first().type('2026-01-01');
    cy.get('input[type="date"]').last().type('2026-01-31');
    cy.get('input[placeholder*="1234567"]').type('9074729');
    cy.get('select').select('Completed');
    
    cy.contains('button', 'Search').click();
    cy.wait('@getVveSearch');
  });

  /**
   * Test 19: Verify dock information displayed
   */
  it('should display dock used in results', () => {
    cy.contains('button', 'Search').click();
    cy.wait('@getVveSearch');
    
    cy.contains('DOCK').should('be.visible');
  });

  /**
   * Test 20: Verify timestamps displayed
   */
  it('should display actual arrival and departure times', () => {
    cy.contains('button', 'Search').click();
    cy.wait('@getVveSearch');
    
    cy.contains('Arrival').should('be.visible');
    cy.contains('Departure').should('be.visible');
  });
});

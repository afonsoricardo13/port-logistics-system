/// <reference types="cypress" />

/**
 * E2E Tests for Updating Vessel Types
 */

describe('Vessel Type Update Form', () => {
  let fixtureData: any;

  before(() => {
    cy.fixture('vessel-types').then((data) => {
      fixtureData = data;
    });
  });

  beforeEach(() => {
    cy.login('PortAuthorityOfficer');
  });

  afterEach(() => {
    cy.logout();
  });

  describe('Form Display', () => {
    it('should display the edit vessel type form', () => {
      cy.intercept('GET', '**/api/VesselTypes/Bulk%20Carrier', {
        statusCode: 200,
        body: fixtureData.vesselTypeToUpdate
      }).as('getVesselType');

      cy.visit('/vessel-types/Bulk%20Carrier/edit');
      cy.wait('@getVesselType');

      cy.get('.card-header h4').should('contain', 'Edit Vessel Type');
      cy.get('form').should('exist');
    });

    it('should load existing vessel type data', () => {
      cy.intercept('GET', '**/api/VesselTypes/Bulk%20Carrier', {
        statusCode: 200,
        body: fixtureData.vesselTypeToUpdate
      }).as('getVesselType');

      cy.visit('/vessel-types/Bulk%20Carrier/edit');
      cy.wait('@getVesselType');

      cy.get('#name').should('be.disabled');
      cy.get('#name').should('have.value', fixtureData.vesselTypeToUpdate.name);
      cy.get('#capacity').should('have.value', fixtureData.vesselTypeToUpdate.capacity.toString());
      cy.get('#description').should('have.value', fixtureData.vesselTypeToUpdate.description);
      cy.get('#maxRows').should('have.value', fixtureData.vesselTypeToUpdate.maxRows.toString());
      cy.get('#maxBays').should('have.value', fixtureData.vesselTypeToUpdate.maxBays.toString());
      cy.get('#maxTiers').should('have.value', fixtureData.vesselTypeToUpdate.maxTiers.toString());
    });

    it('should disable name field in edit mode', () => {
      cy.intercept('GET', '**/api/VesselTypes/Bulk%20Carrier', {
        statusCode: 200,
        body: fixtureData.vesselTypeToUpdate
      }).as('getVesselType');

      cy.visit('/vessel-types/Bulk%20Carrier/edit');
      cy.wait('@getVesselType');

      cy.get('#name').should('be.disabled');
      cy.contains('Name cannot be changed in edit mode').should('be.visible');
    });

    it('should show error when vessel type not found', () => {
      cy.intercept('GET', '**/api/VesselTypes/NonExistent', {
        statusCode: 404,
        body: 'Vessel type not found'
      }).as('getVesselTypeFail');

      cy.visit('/vessel-types/NonExistent/edit');
      cy.wait('@getVesselTypeFail');

      cy.contains('Failed to load vessel type').should('be.visible');
    });
  });

  describe('Form Editing', () => {
    beforeEach(() => {
      cy.fixture('vessel-types').as('vesselTypeData');
    });

    it('should allow editing capacity', () => {
      cy.intercept('GET', '**/api/VesselTypes/Bulk%20Carrier', {
        statusCode: 200,
        body: fixtureData.vesselTypeToUpdate
      }).as('getVesselType');

      cy.visit('/vessel-types/Bulk%20Carrier/edit');
      cy.wait('@getVesselType');

      cy.get('#capacity').clear().type('3500');
      cy.get('#capacity').should('have.value', '3500');
    });

    it('should allow editing description', () => {
      cy.intercept('GET', '**/api/VesselTypes/Bulk%20Carrier', {
        statusCode: 200,
        body: fixtureData.vesselTypeToUpdate
      }).as('getVesselType');

      cy.visit('/vessel-types/Bulk%20Carrier/edit');
      cy.wait('@getVesselType');

      const newDescription = 'Updated vessel description for bulk carrier';
      cy.get('#description').clear().type(newDescription);
      cy.get('#description').should('have.value', newDescription);
    });

    it('should allow editing operational constraints', () => {
      cy.intercept('GET', '**/api/VesselTypes/Bulk%20Carrier', {
        statusCode: 200,
        body: fixtureData.vesselTypeToUpdate
      }).as('getVesselType');

      cy.visit('/vessel-types/Bulk%20Carrier/edit');
      cy.wait('@getVesselType');

      cy.get('#maxRows').clear().type('10');
      cy.get('#maxBays').clear().type('18');
      cy.get('#maxTiers').clear().type('7');

      cy.get('#maxRows').should('have.value', '10');
      cy.get('#maxBays').should('have.value', '18');
      cy.get('#maxTiers').should('have.value', '7');
    });
  });

  describe('Form Validation', () => {
    beforeEach(() => {
      cy.fixture('vessel-types').as('vesselTypeData');
    });

    it('should validate capacity is required', () => {
      cy.intercept('GET', '**/api/VesselTypes/Bulk%20Carrier', {
        statusCode: 200,
        body: fixtureData.vesselTypeToUpdate
      }).as('getVesselType');

      cy.visit('/vessel-types/Bulk%20Carrier/edit');
      cy.wait('@getVesselType');

      cy.get('#capacity').clear().blur();
      cy.get('#capacity').should('have.class', 'is-invalid');
    });

    it('should validate description is required', () => {
      cy.intercept('GET', '**/api/VesselTypes/Bulk%20Carrier', {
        statusCode: 200,
        body: fixtureData.vesselTypeToUpdate
      }).as('getVesselType');

      cy.visit('/vessel-types/Bulk%20Carrier/edit');
      cy.wait('@getVesselType');

      cy.get('#description').clear().blur();
      cy.get('#description').should('have.class', 'is-invalid');
    });

    it('should validate operational constraints', () => {
      cy.intercept('GET', '**/api/VesselTypes/Bulk%20Carrier', {
        statusCode: 200,
        body: fixtureData.vesselTypeToUpdate
      }).as('getVesselType');

      cy.visit('/vessel-types/Bulk%20Carrier/edit');
      cy.wait('@getVesselType');

      cy.get('#maxRows').clear().blur();
      cy.get('#maxRows').should('have.class', 'is-invalid');

      cy.get('#maxBays').clear().blur();
      cy.get('#maxBays').should('have.class', 'is-invalid');

      cy.get('#maxTiers').clear().blur();
      cy.get('#maxTiers').should('have.class', 'is-invalid');
    });
  });

  describe('Successful Update', () => {
    beforeEach(() => {
      cy.fixture('vessel-types').as('vesselTypeData');
    });

    it('should successfully update vessel type', () => {
      cy.intercept('GET', '**/api/VesselTypes/Bulk%20Carrier', {
        statusCode: 200,
        body: fixtureData.vesselTypeToUpdate
      }).as('getVesselType');

      const updatedVesselType = {
        ...fixtureData.vesselTypeToUpdate,
        capacity: 3500,
        description: 'Updated description'
      };

      cy.intercept('PUT', '**/api/VesselTypes/Bulk%20Carrier', {
        statusCode: 200,
        body: updatedVesselType
      }).as('updateVesselType');

      cy.visit('/vessel-types/Bulk%20Carrier/edit');
      cy.wait('@getVesselType');

      cy.get('#capacity').clear().type('3500');
      cy.get('#description').clear().type('Updated description');

      cy.get('button[type="submit"]').click();

      cy.wait('@updateVesselType');
      cy.url().should('include', '/vessel-types');
    });

    it('should redirect to vessel type list after successful update', () => {
      cy.intercept('GET', '**/api/VesselTypes/Bulk%20Carrier', {
        statusCode: 200,
        body: fixtureData.vesselTypeToUpdate
      }).as('getVesselType');

      cy.intercept('PUT', '**/api/VesselTypes/Bulk%20Carrier', {
        statusCode: 200,
        body: fixtureData.vesselTypeToUpdate
      }).as('updateVesselType');

      cy.visit('/vessel-types/Bulk%20Carrier/edit');
      cy.wait('@getVesselType');

      cy.get('#capacity').clear().type('3500');

      cy.get('button[type="submit"]').click();

      cy.wait('@updateVesselType');
      cy.url().should('eq', Cypress.config().baseUrl + '/vessel-types');
    });
  });

  describe('Error Handling', () => {
    beforeEach(() => {
      cy.fixture('vessel-types').as('vesselTypeData');
    });

    it('should show error message when update fails', () => {
      cy.intercept('GET', '**/api/VesselTypes/Bulk%20Carrier', {
        statusCode: 200,
        body: fixtureData.vesselTypeToUpdate
      }).as('getVesselType');

      cy.intercept('PUT', '**/api/VesselTypes/Bulk%20Carrier', {
        statusCode: 400,
        body: ['Invalid vessel type data']
      }).as('updateVesselTypeFail');

      cy.visit('/vessel-types/Bulk%20Carrier/edit');
      cy.wait('@getVesselType');

      cy.get('#capacity').clear().type('3500');

      cy.get('button[type="submit"]').click();

      cy.wait('@updateVesselTypeFail');
      cy.contains('Invalid vessel type data').should('be.visible');
    });
  });

  describe('Navigation', () => {
    beforeEach(() => {
      cy.fixture('vessel-types').as('vesselTypeData');
    });

    it('should have a cancel button that returns to vessel type list', () => {
      cy.intercept('GET', '**/api/VesselTypes/Bulk%20Carrier', {
        statusCode: 200,
        body: fixtureData.vesselTypeToUpdate
      }).as('getVesselType');

      cy.intercept('GET', '**/api/VesselTypes*', {
        statusCode: 200,
        body: []
      }).as('getVesselTypes');

      cy.visit('/vessel-types/Bulk%20Carrier/edit');
      cy.wait('@getVesselType');

      cy.contains('button', 'Cancel').click();
      cy.url().should('include', '/vessel-types');
    });
  });
});



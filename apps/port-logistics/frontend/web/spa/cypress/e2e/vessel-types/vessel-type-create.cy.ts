/// <reference types="cypress" />

/**
 * E2E Tests for Creating Vessel Types
 */

describe('Vessel Type Create Form', () => {
  beforeEach(() => {
    cy.login('PortAuthorityOfficer');
    cy.visit('/vessel-types/new');
  });

  afterEach(() => {
    cy.logout();
  });

  describe('Form Display', () => {
    it('should display the create vessel type form', () => {
      cy.url().should('include', '/vessel-types/new');
      cy.get('.card-header h4').should('contain', 'Create New Vessel Type');
      cy.get('form').should('exist');
    });

    it('should display all required form fields', () => {
      cy.get('#name').should('exist');
      cy.get('#capacity').should('exist');
      cy.get('#description').should('exist');
      cy.get('#maxRows').should('exist');
      cy.get('#maxBays').should('exist');
      cy.get('#maxTiers').should('exist');
    });

    it('should have empty form fields initially', () => {
      cy.get('#name').should('have.value', '');
      cy.get('#capacity').should('have.value', '0');
      cy.get('#description').should('have.value', '');
      cy.get('#maxRows').should('have.value', '0');
      cy.get('#maxBays').should('have.value', '0');
      cy.get('#maxTiers').should('have.value', '0');
    });
  });

  describe('Form Validation', () => {
    it('should show error when submitting empty form', () => {
      cy.get('button[type="submit"]').click();
      cy.get('.is-invalid').should('have.length.greaterThan', 0);
    });

    it('should validate name is required', () => {
      cy.get('#name').focus().blur();
      cy.get('#name').should('have.class', 'is-invalid');
      cy.get('.invalid-feedback').should('contain', 'Name is required');
    });

    it('should validate name minimum length', () => {
      cy.get('#name').type('A').blur();
      cy.get('#name').should('have.class', 'is-invalid');
      cy.get('.invalid-feedback').should('contain', 'Name must be at least 2 characters');
    });

    it('should validate capacity is required', () => {
      cy.get('#capacity').clear().blur();
      cy.get('#capacity').should('have.class', 'is-invalid');
      cy.get('.invalid-feedback').should('contain', 'Capacity is required');
    });

    it('should validate capacity minimum value', () => {
      cy.get('#capacity').clear().type('0').blur();
      cy.get('#capacity').should('have.class', 'is-invalid');
      cy.get('.invalid-feedback').should('contain', 'Capacity must be at least 1');
    });

    it('should validate description is required', () => {
      cy.get('#description').focus().blur();
      cy.get('#description').should('have.class', 'is-invalid');
      cy.get('.invalid-feedback').should('contain', 'Description is required');
    });

    it('should validate description minimum length', () => {
      cy.get('#description').type('Test').blur();
      cy.get('#description').should('have.class', 'is-invalid');
      cy.get('.invalid-feedback').should('contain', 'Description must be at least 5 characters');
    });

    it('should validate maxRows is required and minimum value', () => {
      cy.get('#maxRows').clear().blur();
      cy.get('#maxRows').should('have.class', 'is-invalid');
    });

    it('should validate maxBays is required and minimum value', () => {
      cy.get('#maxBays').clear().blur();
      cy.get('#maxBays').should('have.class', 'is-invalid');
    });

    it('should validate maxTiers is required and minimum value', () => {
      cy.get('#maxTiers').clear().blur();
      cy.get('#maxTiers').should('have.class', 'is-invalid');
    });
  });

  describe('Successful Creation', () => {
    beforeEach(() => {
      cy.fixture('vessel-types').as('vesselTypeData');
    });

    it('should successfully create a vessel type with valid data', function() {
      cy.intercept('POST', '**/api/VesselTypes', {
        statusCode: 201,
        body: this['vesselTypeData'].validVesselType
      }).as('createVesselType');

      cy.get('#name').type(this['vesselTypeData'].validVesselType.name);
      cy.get('#capacity').clear().type(this['vesselTypeData'].validVesselType.capacity.toString());
      cy.get('#description').type(this['vesselTypeData'].validVesselType.description);
      cy.get('#maxRows').clear().type(this['vesselTypeData'].validVesselType.maxRows.toString());
      cy.get('#maxBays').clear().type(this['vesselTypeData'].validVesselType.maxBays.toString());
      cy.get('#maxTiers').clear().type(this['vesselTypeData'].validVesselType.maxTiers.toString());

      cy.get('button[type="submit"]').click();

      cy.wait('@createVesselType');
      cy.url().should('include', '/vessel-types');
    });

    it('should redirect to vessel type list after successful creation', function() {
      cy.intercept('POST', '**/api/VesselTypes', {
        statusCode: 201,
        body: this['vesselTypeData'].validVesselType
      }).as('createVesselType');

      cy.get('#name').type(this['vesselTypeData'].validVesselType.name);
      cy.get('#capacity').clear().type(this['vesselTypeData'].validVesselType.capacity.toString());
      cy.get('#description').type(this['vesselTypeData'].validVesselType.description);
      cy.get('#maxRows').clear().type(this['vesselTypeData'].validVesselType.maxRows.toString());
      cy.get('#maxBays').clear().type(this['vesselTypeData'].validVesselType.maxBays.toString());
      cy.get('#maxTiers').clear().type(this['vesselTypeData'].validVesselType.maxTiers.toString());

      cy.get('button[type="submit"]').click();

      cy.wait('@createVesselType');
      cy.url().should('eq', Cypress.config().baseUrl + '/vessel-types');
    });

    it('should display all vessel type information in the form', function() {
      cy.get('#name').type(this['vesselTypeData'].validVesselType.name);
      cy.get('#name').should('have.value', this['vesselTypeData'].validVesselType.name);
      
      cy.get('#capacity').clear().type(this['vesselTypeData'].validVesselType.capacity.toString());
      cy.get('#capacity').should('have.value', this['vesselTypeData'].validVesselType.capacity.toString());
      
      cy.get('#description').type(this['vesselTypeData'].validVesselType.description);
      cy.get('#description').should('have.value', this['vesselTypeData'].validVesselType.description);
      
      cy.get('#maxRows').clear().type(this['vesselTypeData'].validVesselType.maxRows.toString());
      cy.get('#maxRows').should('have.value', this['vesselTypeData'].validVesselType.maxRows.toString());
      
      cy.get('#maxBays').clear().type(this['vesselTypeData'].validVesselType.maxBays.toString());
      cy.get('#maxBays').should('have.value', this['vesselTypeData'].validVesselType.maxBays.toString());
      
      cy.get('#maxTiers').clear().type(this['vesselTypeData'].validVesselType.maxTiers.toString());
      cy.get('#maxTiers').should('have.value', this['vesselTypeData'].validVesselType.maxTiers.toString());
    });
  });

  describe('Error Handling', () => {
    beforeEach(() => {
      cy.fixture('vessel-types').as('vesselTypeData');
    });

    it('should show error message when API returns error', function() {
      cy.intercept('POST', '**/api/VesselTypes', {
        statusCode: 400,
        body: ['Invalid vessel type data']
      }).as('createVesselTypeFail');

      cy.get('#name').type(this['vesselTypeData'].validVesselType.name);
      cy.get('#capacity').clear().type(this['vesselTypeData'].validVesselType.capacity.toString());
      cy.get('#description').type(this['vesselTypeData'].validVesselType.description);
      cy.get('#maxRows').clear().type(this['vesselTypeData'].validVesselType.maxRows.toString());
      cy.get('#maxBays').clear().type(this['vesselTypeData'].validVesselType.maxBays.toString());
      cy.get('#maxTiers').clear().type(this['vesselTypeData'].validVesselType.maxTiers.toString());

      cy.get('button[type="submit"]').click();

      cy.wait('@createVesselTypeFail');
      cy.contains('Invalid vessel type data').should('be.visible');
    });
  });

  describe('Navigation', () => {
    it('should have a cancel button that returns to vessel type list', () => {
      cy.intercept('GET', '**/api/VesselTypes*', {
        statusCode: 200,
        body: []
      }).as('getVesselTypes');

      cy.contains('button', 'Cancel').click();
      cy.url().should('include', '/vessel-types');
    });
  });
});

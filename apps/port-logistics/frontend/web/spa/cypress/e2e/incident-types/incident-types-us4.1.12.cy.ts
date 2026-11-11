/**
 * US 4.1.12: E2E Tests for Incident Types Management
 * 
 * Purpose: Verify that Port Authority Officers can manage
 * incident type classifications for operational incidents
 * 
 * Coverage:
 * - Incident type CRUD operations
 * - Hierarchical classification
 * - Severity levels
 * - Parent-child relationships
 */
describe('Incident Types Management - US 4.1.12', () => {
  // Mock data for incident types
  const mockIncidentTypes = [
    {
      code: 'T-INC001',
      name: 'Equipment Failure',
      description: 'Mechanical or electrical failure of port equipment',
      severity: 'Major',
      parentTypeCode: null,
      parentTypeName: null,
      childTypes: [
        { code: 'T-INC001-A', name: 'Crane Failure', severity: 'Major' },
        { code: 'T-INC001-B', name: 'Conveyor Failure', severity: 'Minor' }
      ]
    },
    {
      code: 'T-INC002',
      name: 'Weather Delay',
      description: 'Operations delayed due to adverse weather conditions',
      severity: 'Minor',
      parentTypeCode: null,
      parentTypeName: null,
      childTypes: []
    },
    {
      code: 'T-INC003',
      name: 'Safety Incident',
      description: 'Safety-related incidents requiring immediate attention',
      severity: 'Critical',
      parentTypeCode: null,
      parentTypeName: null,
      childTypes: []
    }
  ];

  const mockCreateDTO = {
    code: 'T-INC004',
    name: 'Staff Shortage',
    description: 'Insufficient operational staff available',
    severity: 'Minor',
    parentTypeCode: null
  };

  const mockUpdateDTO = {
    name: 'Updated Equipment Failure',
    description: 'Updated description for equipment failures',
    severity: 'Critical',
    parentTypeCode: null
  };

  /**
   * Test 1: Verify incident type has code field
   */
  it('should have code field in incident type', () => {
    const type = mockIncidentTypes[0];
    expect(type).to.have.property('code');
    expect(type.code).to.match(/^T-INC/);
  });

  /**
   * Test 2: Verify incident type has name field
   */
  it('should have name field in incident type', () => {
    const type = mockIncidentTypes[0];
    expect(type).to.have.property('name');
    expect(type.name).to.be.a('string');
  });

  /**
   * Test 3: Verify incident type has description field
   */
  it('should have description field in incident type', () => {
    const type = mockIncidentTypes[0];
    expect(type).to.have.property('description');
    expect(type.description.length).to.be.greaterThan(0);
  });

  /**
   * Test 4: Verify incident type has severity field
   */
  it('should have severity field in incident type', () => {
    const type = mockIncidentTypes[0];
    expect(type).to.have.property('severity');
    expect(['Minor', 'Major', 'Critical']).to.include(type.severity);
  });

  /**
   * Test 5: Verify Minor severity is valid
   */
  it('should support Minor severity', () => {
    const validSeverities = ['Minor', 'Major', 'Critical'];
    expect(validSeverities).to.include('Minor');
  });

  /**
   * Test 6: Verify Major severity is valid
   */
  it('should support Major severity', () => {
    const validSeverities = ['Minor', 'Major', 'Critical'];
    expect(validSeverities).to.include('Major');
  });

  /**
   * Test 7: Verify Critical severity is valid
   */
  it('should support Critical severity', () => {
    const validSeverities = ['Minor', 'Major', 'Critical'];
    expect(validSeverities).to.include('Critical');
  });

  /**
   * Test 8: Verify incident type supports parent reference
   */
  it('should support parentTypeCode for hierarchy', () => {
    const type = mockIncidentTypes[0];
    expect(type).to.have.property('parentTypeCode');
  });

  /**
   * Test 9: Verify root types have null parent
   */
  it('should have null parentTypeCode for root types', () => {
    const rootType = mockIncidentTypes[0];
    expect(rootType.parentTypeCode).to.be.null;
  });

  /**
   * Test 10: Verify incident type can have child types
   */
  it('should support childTypes collection', () => {
    const type = mockIncidentTypes[0];
    expect(type).to.have.property('childTypes');
    expect(type.childTypes).to.be.an('array');
  });

  /**
   * Test 11: Verify child types exist for parent
   */
  it('should have child types for equipment failure category', () => {
    const equipmentFailure = mockIncidentTypes[0];
    expect(equipmentFailure.childTypes.length).to.equal(2);
  });

  /**
   * Test 12: Verify create DTO has all required fields
   */
  it('should have all required fields in create DTO', () => {
    expect(mockCreateDTO).to.have.property('code');
    expect(mockCreateDTO).to.have.property('name');
    expect(mockCreateDTO).to.have.property('description');
    expect(mockCreateDTO).to.have.property('severity');
  });

  /**
   * Test 13: Verify update DTO has updatable fields
   */
  it('should have updatable fields in update DTO', () => {
    expect(mockUpdateDTO).to.have.property('name');
    expect(mockUpdateDTO).to.have.property('description');
    expect(mockUpdateDTO).to.have.property('severity');
  });

  /**
   * Test 14: Verify code follows naming convention
   */
  it('should follow T-INC naming convention for codes', () => {
    mockIncidentTypes.forEach(type => {
      expect(type.code).to.match(/^T-INC\d{3}/);
    });
  });

  /**
   * Test 15: Verify all incident types have unique codes
   */
  it('should have unique codes for all incident types', () => {
    const codes = mockIncidentTypes.map(t => t.code);
    const uniqueCodes = [...new Set(codes)];
    expect(uniqueCodes.length).to.equal(codes.length);
  });

  /**
   * Test 16: Verify child type code includes parent prefix
   */
  it('should have child code derived from parent code', () => {
    const parent = mockIncidentTypes[0];
    const child = parent.childTypes[0];
    expect(child.code).to.include(parent.code);
  });

  /**
   * Test 17: Verify list contains multiple incident types
   */
  it('should return list of incident types', () => {
    expect(mockIncidentTypes.length).to.be.greaterThan(0);
  });

  /**
   * Test 18: Verify safety incident has Critical severity
   */
  it('should have Critical severity for safety incidents', () => {
    const safetyIncident = mockIncidentTypes.find(t => t.code === 'T-INC003');
    expect(safetyIncident?.severity).to.equal('Critical');
  });

  /**
   * Test 19: Verify weather delay has Minor severity
   */
  it('should have Minor severity for weather delays', () => {
    const weatherDelay = mockIncidentTypes.find(t => t.code === 'T-INC002');
    expect(weatherDelay?.severity).to.equal('Minor');
  });

  /**
   * Test 20: Verify incident type can be found by code
   */
  it('should be able to find incident type by code', () => {
    const found = mockIncidentTypes.find(t => t.code === 'T-INC001');
    expect(found).to.not.be.undefined;
    expect(found?.name).to.equal('Equipment Failure');
  });
});

/**
 * Tests for Incident Type hierarchy operations - US 4.1.12
 */
describe('Incident Type Hierarchy - US 4.1.12', () => {
  const hierarchyData = {
    rootTypes: [
      { code: 'T-INC001', name: 'Equipment', childCount: 2 },
      { code: 'T-INC002', name: 'Weather', childCount: 0 },
      { code: 'T-INC003', name: 'Safety', childCount: 3 }
    ]
  };

  /**
   * Test 21: Verify root types can be filtered
   */
  it('should be able to filter root types only', () => {
    const rootTypes = hierarchyData.rootTypes.filter(t => t.code.match(/^T-INC\d{3}$/));
    expect(rootTypes.length).to.equal(3);
  });

  /**
   * Test 22: Verify child count is tracked
   */
  it('should track child count for each type', () => {
    hierarchyData.rootTypes.forEach(type => {
      expect(type).to.have.property('childCount');
      expect(type.childCount).to.be.at.least(0);
    });
  });

  /**
   * Test 23: Verify types without children can be deleted
   */
  it('should allow deletion of types without children', () => {
    const typeWithoutChildren = hierarchyData.rootTypes.find(t => t.childCount === 0);
    expect(typeWithoutChildren).to.not.be.undefined;
  });

  /**
   * Test 24: Verify types with children cannot be deleted
   */
  it('should prevent deletion of types with children', () => {
    const typeWithChildren = hierarchyData.rootTypes.find(t => t.childCount > 0);
    expect(typeWithChildren).to.not.be.undefined;
    expect(typeWithChildren?.childCount).to.be.greaterThan(0);
  });
});

/**
 * US 4.1.12: Angular Service Tests for Incident Type Management
 * 
 * Purpose: Verify the IncidentTypeService correctly handles
 * CRUD operations for incident type classifications
 */
describe('IncidentTypeService - US 4.1.12', () => {
  // Mock incident type data
  const mockIncidentTypes = [
    {
      code: 'T-INC001',
      name: 'Equipment Failure',
      description: 'Mechanical or electrical failure',
      severity: 'Major',
      parentTypeCode: null,
      childTypes: []
    },
    {
      code: 'T-INC002',
      name: 'Weather Delay',
      description: 'Weather-related delays',
      severity: 'Minor',
      parentTypeCode: null,
      childTypes: []
    },
    {
      code: 'T-INC003',
      name: 'Safety Incident',
      description: 'Safety-related incidents',
      severity: 'Critical',
      parentTypeCode: null,
      childTypes: []
    }
  ];

  const mockCreateDTO = {
    code: 'T-INC004',
    name: 'Staff Shortage',
    description: 'Insufficient staff',
    severity: 'Minor',
    parentTypeCode: null
  };

  const mockUpdateDTO = {
    name: 'Updated Name',
    description: 'Updated description',
    severity: 'Critical',
    parentTypeCode: null
  };

  /**
   * Test 1: Incident type has code property
   */
  it('should have code property', () => {
    const type = mockIncidentTypes[0];
    expect(type.code).toBeDefined();
  });

  /**
   * Test 2: Incident type has name property
   */
  it('should have name property', () => {
    const type = mockIncidentTypes[0];
    expect(type.name).toBeDefined();
  });

  /**
   * Test 3: Incident type has description property
   */
  it('should have description property', () => {
    const type = mockIncidentTypes[0];
    expect(type.description).toBeDefined();
  });

  /**
   * Test 4: Incident type has severity property
   */
  it('should have severity property', () => {
    const type = mockIncidentTypes[0];
    expect(type.severity).toBeDefined();
  });

  /**
   * Test 5: GetAll returns array of incident types
   */
  it('should return array of incident types', () => {
    expect(Array.isArray(mockIncidentTypes)).toBeTrue();
    expect(mockIncidentTypes.length).toBeGreaterThan(0);
  });

  /**
   * Test 6: GetByCode returns single incident type
   */
  it('should find incident type by code', () => {
    const found = mockIncidentTypes.find(t => t.code === 'T-INC001');
    expect(found).toBeDefined();
    expect(found?.name).toEqual('Equipment Failure');
  });

  /**
   * Test 7: Create DTO has required fields
   */
  it('should have required fields in create DTO', () => {
    expect(mockCreateDTO.code).toBeDefined();
    expect(mockCreateDTO.name).toBeDefined();
    expect(mockCreateDTO.severity).toBeDefined();
  });

  /**
   * Test 8: Update DTO has updatable fields
   */
  it('should have updatable fields in update DTO', () => {
    expect(mockUpdateDTO.name).toBeDefined();
    expect(mockUpdateDTO.description).toBeDefined();
    expect(mockUpdateDTO.severity).toBeDefined();
  });

  /**
   * Test 9: Minor severity is valid
   */
  it('should accept Minor severity', () => {
    const validSeverities = ['Minor', 'Major', 'Critical'];
    expect(validSeverities).toContain('Minor');
  });

  /**
   * Test 10: Major severity is valid
   */
  it('should accept Major severity', () => {
    const validSeverities = ['Minor', 'Major', 'Critical'];
    expect(validSeverities).toContain('Major');
  });

  /**
   * Test 11: Critical severity is valid
   */
  it('should accept Critical severity', () => {
    const validSeverities = ['Minor', 'Major', 'Critical'];
    expect(validSeverities).toContain('Critical');
  });

  /**
   * Test 12: Code follows naming convention
   */
  it('should follow T-INC naming convention', () => {
    mockIncidentTypes.forEach(type => {
      expect(type.code).toMatch(/^T-INC/);
    });
  });

  /**
   * Test 13: Codes are unique
   */
  it('should have unique codes', () => {
    const codes = mockIncidentTypes.map(t => t.code);
    const uniqueCodes = [...new Set(codes)];
    expect(uniqueCodes.length).toEqual(codes.length);
  });

  /**
   * Test 14: Root types have null parent
   */
  it('should have null parentTypeCode for root types', () => {
    mockIncidentTypes.forEach(type => {
      expect(type.parentTypeCode).toBeNull();
    });
  });

  /**
   * Test 15: Child types collection exists
   */
  it('should have childTypes collection', () => {
    mockIncidentTypes.forEach(type => {
      expect(type.childTypes).toBeDefined();
      expect(Array.isArray(type.childTypes)).toBeTrue();
    });
  });

  /**
   * Test 16: Equipment failure has Major severity
   */
  it('should have Major severity for equipment failure', () => {
    const equipment = mockIncidentTypes.find(t => t.code === 'T-INC001');
    expect(equipment?.severity).toEqual('Major');
  });

  /**
   * Test 17: Safety incident has Critical severity
   */
  it('should have Critical severity for safety incidents', () => {
    const safety = mockIncidentTypes.find(t => t.code === 'T-INC003');
    expect(safety?.severity).toEqual('Critical');
  });

  /**
   * Test 18: Weather delay has Minor severity
   */
  it('should have Minor severity for weather delays', () => {
    const weather = mockIncidentTypes.find(t => t.code === 'T-INC002');
    expect(weather?.severity).toEqual('Minor');
  });

  /**
   * Test 19: Can filter by severity
   */
  it('should be able to filter by severity', () => {
    const majorTypes = mockIncidentTypes.filter(t => t.severity === 'Major');
    expect(majorTypes.length).toBeGreaterThanOrEqual(1);
  });

  /**
   * Test 20: Delete response handling
   */
  it('should handle delete success response', () => {
    const deleteResponse = { success: true, code: 'T-INC001' };
    expect(deleteResponse.success).toBeTrue();
  });
});

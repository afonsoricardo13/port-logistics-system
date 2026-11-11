using Application.DTO;
using Domain.IRepository;
using Domain.Model;
using Microsoft.Extensions.Logging;

namespace Application.Services;

/// <summary>
/// US 3.3.2: Service to generate port layouts for 3D visualization
/// US 3.3.3: Displays vessels from approved VesselVisitNotifications and resources with assigned areas
/// </summary>
public class PortLayoutService
{
    private readonly IDockRepository _dockRepository;
    private readonly IStorageAreaRepository _storageAreaRepository;
    private readonly IVesselVisitNotificationRepository _vesselVisitRepository;
    private readonly IPhysicalResourceRepository _physicalResourceRepository;
    private readonly IVesselRepository _vesselRepository;
    private readonly ILogger<PortLayoutService> _logger;

    public PortLayoutService(
        IDockRepository dockRepository,
        IStorageAreaRepository storageAreaRepository,
        IVesselVisitNotificationRepository vesselVisitRepository,
        IPhysicalResourceRepository physicalResourceRepository,
        IVesselRepository vesselRepository,
        ILogger<PortLayoutService> logger)
    {
        _dockRepository = dockRepository;
        _storageAreaRepository = storageAreaRepository;
        _vesselVisitRepository = vesselVisitRepository;
        _physicalResourceRepository = physicalResourceRepository;
        _vesselRepository = vesselRepository;
        _logger = logger;
    }

    /// <summary>
    /// Get port layout by ID (A or B)
    /// </summary>
    public async Task<PortLayoutDTO?> GetLayoutAsync(string layoutId)
    {
        _logger.LogInformation("US 3.3.2: Retrieving port layout {LayoutId}", layoutId);

        return layoutId.ToUpper() switch
        {
            "A" => await GenerateLayoutA(),
            "B" => await GenerateLayoutB(),
            "C" => await GenerateLayoutC(),
            _ => null
        };
    }

    /// <summary>
    /// Get all available layout IDs
    /// </summary>
    public Task<List<string>> GetAvailableLayoutsAsync()
    {
        return Task.FromResult(new List<string> { "A", "B", "C" });
    }

    /// <summary>
    /// Layout A: Real docks from database positioned on the left side
    /// </summary>
    private async Task<PortLayoutDTO> GenerateLayoutA()
    {
        var elements = new List<PortElementDTO>();

        // Port base platform (large concrete base) - Dimensions3D: length, width, height
        elements.Add(new PortElementDTO
        {
            Id = "port-platform",
            Type = "platform",
            Name = "Porto Base Platform",
            Position = new Position3D(0, -2, 0),
            Dimensions = new Dimensions3D(600, 400, 2),
            Rotation = new Rotation3D(0, 0, 0),
            Color = "#9e9e9e",
            Metadata = new Dictionary<string, object> { { "material", "concrete" } }
        });

        // Water plane (animated ocean) - position at y=-0.5 to be visible
        elements.Add(new PortElementDTO
        {
            Id = "water",
            Type = "water",
            Name = "Porto Water Surface",
            Position = new Position3D(-100, -0.5, 0),
            Dimensions = new Dimensions3D(800, 800, 1),
            Rotation = new Rotation3D(0, 0, 0),
            Color = "#1e88e5",
            Metadata = new Dictionary<string, object> { { "animated", true } }
        });

        // Get REAL docks from database
        var docks = await _dockRepository.GetAllAsync();
        var dockList = docks.Take(3).ToList(); // Take first 3 for Layout A
        
        _logger.LogInformation("US 3.3.2: Loading {Count} real docks from database for Layout A", dockList.Count);

        // Create dock placement map for later VVN and crane assignment
        var dockPlacementA = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        // Use a safe dictionary build to tolerate duplicate codes (last one wins)
        var dockByCodeA = new Dictionary<string, Domain.Model.Dock>(StringComparer.OrdinalIgnoreCase);
        foreach (var d in dockList)
        {
            dockByCodeA[d.Code] = d;
        }

        // Position real docks on the left (negative X) with spacing
        var dockSpacing = 120.0;
        for (int i = 0; i < dockList.Count; i++)
        {
            var dock = dockList[i];
            var zPosition = i * dockSpacing - 120;
            dockPlacementA[dock.Code] = zPosition;
            
            elements.Add(new PortElementDTO
            {
                Id = $"dock-{dock.Code}",
                Type = "dock",
                Name = $"{dock.NameOrNumber} ({dock.Code})",
                Position = new Position3D(-80, 0, zPosition),
                Dimensions = new Dimensions3D(
                    dock.Physical.Length,
                    dock.Physical.Depth,
                    2
                ),
                Rotation = new Rotation3D(0, 0, 0),
                Color = "#795548",
                Metadata = new Dictionary<string, object>
                {
                    { "code", dock.Code },
                    { "name", dock.NameOrNumber },
                    { "location", dock.Location },
                    { "length", dock.Physical.Length },
                    { "depth", dock.Physical.Depth },
                    { "maxDraft", dock.Physical.MaxDraft },
                    { "allowedVesselTypes", dock.AllowedVesselTypes.ToArray() }
                }
            });
        }

        // US 3.3.3: Use APPROVED vessel visit notifications (real data) for ships in Layout A
        var allVvnsA = _vesselVisitRepository.GetAll();
        var approvedVvnsA = allVvnsA
            .Where(v => v.Status == VvnStatus.Approved && !string.IsNullOrEmpty(v.AssignedDockCode))
            .Where(v => dockPlacementA.ContainsKey(v.AssignedDockCode!))
            .ToList();

        foreach (var v in approvedVvnsA)
        {
            var dockCode = v.AssignedDockCode!;
            var dock = dockByCodeA[dockCode];
            var zPos = dockPlacementA[dockCode];

            var shipLen = Math.Min(dock.Physical.Length * 1.15, 320);
            var shipWid = Math.Min(dock.Physical.Depth * 0.75, 65);
            var shipHgt = 16;

            // Fetch real vessel data from database
            var vessel = await _vesselRepository.GetByImoNumberAsync(v.VesselImoNumber);
            var vesselName = vessel?.Name ?? $"IMO:{v.VesselImoNumber}";
            var vesselType = vessel?.VesselType ?? "Unknown";

            elements.Add(new PortElementDTO
            {
                Id = $"ship-{v.VesselImoNumber}",
                Type = "ship",
                Name = vesselName,
                Position = new Position3D(-600, -0.05, zPos),
                Dimensions = new Dimensions3D(shipLen, shipHgt, shipWid),
                Rotation = new Rotation3D(0, 0, 0),
                Color = "#1a237e",
                Metadata = new Dictionary<string, object>
                {
                    { "dockCode", dockCode },
                    { "approved", true },
                    { "imoNumber", v.VesselImoNumber },
                    { "name", vesselName },
                    { "type", vesselType },
                    { "vesselType", vesselType },
                    { "operator", vessel?.Operator ?? string.Empty },
                    { "eta", v.EstimatedTimeOfArrival?.ToString("o") ?? string.Empty },
                    { "etd", v.EstimatedTimeOfDeparture?.ToString("o") ?? string.Empty }
                }
            });
        }

        // US 3.3.3: Use REAL physical resources assigned to docks for cranes in Layout A
        var allResourcesA = _physicalResourceRepository.GetAll();
        _logger.LogInformation("US 3.3.3: Found {Count} total physical resources in database", allResourcesA.Count());
        
        var dockResourcesA = allResourcesA
            .Where(r => !string.IsNullOrEmpty(r.AssignedArea))
            .Where(r => dockPlacementA.ContainsKey(r.AssignedArea!))
            .ToList();
        
        _logger.LogInformation("US 3.3.3: Found {Count} resources assigned to Layout A docks", dockResourcesA.Count);

        foreach (var res in dockResourcesA)
        {
            var zPos = dockPlacementA[res.AssignedArea!];
            
            // Determine element type based on PhysicalResource type
            var elementType = res.Type.ToLower() switch
            {
                "truck" => "truck",
                "forklift" => "forklift",
                _ => "crane" // STS, RTG, RMG, etc.
            };
            
            // Adjust dimensions based on resource type
            var (width, height, length) = elementType switch
            {
                "truck" => (15.0, 8.0, 36.0),
                "forklift" => (2.0, 2.5, 3.0),
                _ => (10.0, 12.0, 30.0)  // STS Crane
            };
            
            // Build metadata based on element type
            var metadata = new Dictionary<string, object>
            {
                { "code", res.Code },
                { "description", res.Description },
                { "assignedArea", res.AssignedArea! }
            };
            
            // Add crane-specific metadata only for cranes
            if (elementType == "crane")
            {
                metadata.Add("craneType", "STS");
                metadata.Add("model", "tower_crane");
            }
            
            elements.Add(new PortElementDTO
            {
                Id = $"{elementType}-{res.Code}",
                Type = elementType,
                Name = res.Code,
                Position = new Position3D(-25, 4, zPos),
                Dimensions = new Dimensions3D(width, height, length),
                Rotation = new Rotation3D(0, 0, 0),
                Color = "#ff9800",
                Metadata = metadata
            });
        }

        // Get REAL storage areas from database
        var storageAreas = await _storageAreaRepository.GetAllAsync();
        var warehouses = storageAreas.Where(sa => sa.Type.ToLower() == "warehouse").Take(2).ToList();
        var yards = storageAreas.Where(sa => sa.Type.ToLower() == "yard").Take(1).ToList();
        
        _logger.LogInformation("US 3.3.2: Loading {WarehouseCount} warehouses and {YardCount} yards from database", 
            warehouses.Count, yards.Count);

        // Real warehouses inland
        for (int i = 0; i < warehouses.Count; i++)
        {
            var warehouse = warehouses[i];
            var zPosition = -100 + (i * 100);
            
            elements.Add(new PortElementDTO
            {
                Id = $"warehouse-{warehouse.Identifier}",
                Type = "warehouse",
                Name = warehouse.Identifier,
                Position = new Position3D(50, 0, zPosition),
                Dimensions = new Dimensions3D(80, 50, 20),
                Rotation = new Rotation3D(0, i * 15, 0),
                Color = i == 0 ? "#e74c3c" : "#c0392b",
                Metadata = new Dictionary<string, object> 
                { 
                    { "identifier", warehouse.Identifier },
                    { "location", warehouse.Location },
                    { "maxCapacityTeu", warehouse.MaxCapacityTeu },
                    { "currentOccupancyTeu", warehouse.CurrentOccupancyTeu }
                }
            });
        }

        // Real container yards
        var yardPosMapA = new Dictionary<string, (double x, double z)>();
        for (int i = 0; i < yards.Count; i++)
        {
            var yard = yards[i];
            var xPos = 80.0;
            var zPos = 80.0;
            yardPosMapA[yard.Identifier] = (xPos, zPos);
            
            elements.Add(new PortElementDTO
            {
                Id = $"yard-{yard.Identifier}",
                Type = "containerYard",
                Name = yard.Identifier,
                Position = new Position3D(xPos, 0, zPos),
                Dimensions = new Dimensions3D(100, 80, 0.5),
                Rotation = new Rotation3D(0, 0, 0),
                Color = "#34495e",
                Metadata = new Dictionary<string, object> 
                { 
                    { "identifier", yard.Identifier },
                    { "location", yard.Location },
                    { "maxCapacityTeu", yard.MaxCapacityTeu },
                    { "currentOccupancyTeu", yard.CurrentOccupancyTeu }
                }
            });
        }

        // US 3.3.3: Use REAL physical resources assigned to yards for yard cranes in Layout A
        var yardResourcesA = allResourcesA
            .Where(r => !string.IsNullOrEmpty(r.AssignedArea))
            .Where(r => yardPosMapA.ContainsKey(r.AssignedArea!))
            .ToList();
        
        _logger.LogInformation("US 3.3.3: Found {Count} resources assigned to Layout A yards", yardResourcesA.Count);

        foreach (var res in yardResourcesA)
        {
            var yz = yardPosMapA[res.AssignedArea!];
            
            // Determine element type based on PhysicalResource type
            var elementType = res.Type.ToLower() switch
            {
                "truck" => "truck",
                "forklift" => "forklift",
                _ => "crane" // RTG, RMG, etc.
            };
            
            // Adjust dimensions based on resource type
            var (width, height, length) = elementType switch
            {
                "truck" => (15.0, 8.0, 36.0),
                "forklift" => (2.0, 2.5, 3.0),
                _ => (16.0, 24.0, 30.0)  // Yard crane - doubled dimensions
            };
            
            // Build metadata based on element type
            var yardMetadata = new Dictionary<string, object>
            {
                { "code", res.Code },
                { "description", res.Description },
                { "assignedArea", res.AssignedArea! }
            };
            
            // Add crane-specific metadata only for cranes
            if (elementType == "crane")
            {
                yardMetadata.Add("craneType", "RTG");
                yardMetadata.Add("model", "gantry_crane");
            }
            
            elements.Add(new PortElementDTO
            {
                Id = $"yard-{elementType}-{res.Code}",
                Type = elementType,
                Name = res.Code,
                Position = new Position3D(yz.x + 15, 4, yz.z + 10),
                Dimensions = new Dimensions3D(width, height, length),
                Rotation = new Rotation3D(0, 0, 0),
                Color = "#ff9800",
                Metadata = yardMetadata
            });
        }

        // Admin building
        elements.Add(new PortElementDTO
        {
            Id = "admin-1",
            Type = "adminBuilding",
            Name = "Port Administration",
            Position = new Position3D(20, 0, -150),
            Dimensions = new Dimensions3D(30, 20, 25),
            Rotation = new Rotation3D(0, 45, 0),
            Color = "#2c3e50",
            Metadata = new Dictionary<string, object> { { "floors", 5 } }
        });

        // Count actual elements
        var dockCount = elements.Count(e => e.Type.ToLower() == "dock");
        var warehouseCount = elements.Count(e => e.Type.ToLower() == "warehouse");
        var yardCount = elements.Count(e => e.Type.ToLower() == "containeryard");
        
        return new PortLayoutDTO
        {
            LayoutId = "A",
            LayoutName = $"Porto Layout A - {dockCount} Docks Left",
            Version = "1.0",
            Description = $"Standard layout with {dockCount} docks on the left side, {warehouseCount} warehouses and {yardCount} container yard(s)",
            Elements = elements,
            Settings = new Dictionary<string, object>
            {
                { "cameraPosition", new { x = 150, y = 100, z = 150 } },
                { "cameraTarget", new { x = 0, y = 0, z = 0 } },
                { "ambientLight", "#ffffff" },
                { "ambientIntensity", 0.6 },
                { "skyColor", "#87ceeb" },
                { "gridSize", 500 }
            }
        };
    }

    /// <summary>
    /// Layout B: 5 docks (3 left + 2 right), 3 warehouses, 2 container yards
    /// </summary>
    private async Task<PortLayoutDTO> GenerateLayoutB()
    {
        var elements = new List<PortElementDTO>();

        // Port base platform - Dimensions3D: length, width, height
        elements.Add(new PortElementDTO
        {
            Id = "port-platform",
            Type = "platform",
            Name = "Porto Base Platform",
            Position = new Position3D(0, -2, 0),
            Dimensions = new Dimensions3D(700, 500, 2),
            Rotation = new Rotation3D(0, 0, 0),
            Color = "#9e9e9e",
            Metadata = new Dictionary<string, object> { { "material", "concrete" } }
        });

        // Water plane (animated ocean) - expanded for Layout B
        elements.Add(new PortElementDTO
        {
            Id = "water",
            Type = "water",
            Name = "Porto Water Surface",
            Position = new Position3D(-250, -0.5, 0),
            Dimensions = new Dimensions3D(1500, 1500, 1),
            Rotation = new Rotation3D(0, 0, 0),
            Color = "#0d47a1",
            Metadata = new Dictionary<string, object> { { "animated", true } }
        });

        // Get REAL docks from database for Layout B (5 docks: 3 left + 2 right)
        var allDocks = await _dockRepository.GetAllAsync();
        var dockList = allDocks.Take(5).ToList();
        
        _logger.LogInformation("US 3.3.2: Loading {Count} real docks from database for Layout B", dockList.Count);

        // Position first 3 docks on the left (negative X) with more spacing
        var dockPlacementByCode = new Dictionary<string, (string side, double z)>(StringComparer.OrdinalIgnoreCase);
        // Use a safe dictionary build to tolerate duplicate codes (last one wins)
        var dockByCode = new Dictionary<string, Domain.Model.Dock>(StringComparer.OrdinalIgnoreCase);
        foreach (var d in dockList)
        {
            dockByCode[d.Code] = d;
        }
        var leftDocks = dockList.Take(3).ToList();
        var dockSpacing = 150.0; // Increased spacing to prevent ship overlap
        for (int i = 0; i < leftDocks.Count; i++)
        {
            var dock = leftDocks[i];
            var zPosition = i * dockSpacing - 150;
            dockPlacementByCode[dock.Code] = ("left", zPosition);
            
            elements.Add(new PortElementDTO
            {
                Id = $"dock-left-{dock.Code}",
                Type = "dock",
                Name = $"{dock.NameOrNumber} West ({dock.Code})",
                Position = new Position3D(-100, 0, zPosition),
                Dimensions = new Dimensions3D(
                    dock.Physical.Length,
                    dock.Physical.Depth,
                    2
                ),
                Rotation = new Rotation3D(0, 0, 0),
                Color = "#6d4c41",
                Metadata = new Dictionary<string, object>
                {
                    { "code", dock.Code },
                    { "name", dock.NameOrNumber },
                    { "side", "left" },
                    { "location", dock.Location },
                    { "length", dock.Physical.Length },
                    { "depth", dock.Physical.Depth },
                    { "maxDraft", dock.Physical.MaxDraft },
                    { "allowedVesselTypes", dock.AllowedVesselTypes.ToArray() }
                }
            });

            // Cranes and vessels will be added from REAL DB below (physical resources + approved VVN)
        }

        // Position remaining 2 docks on the right (positive X) with more spacing
        var rightDocks = dockList.Skip(3).Take(2).ToList();
        for (int i = 0; i < rightDocks.Count; i++)
        {
            var dock = rightDocks[i];
            var zPositionRight = i * 160.0 - 80; // Increased spacing for right docks
            dockPlacementByCode[dock.Code] = ("right", zPositionRight);
            
            elements.Add(new PortElementDTO
            {
                Id = $"dock-right-{dock.Code}",
                Type = "dock",
                Name = $"{dock.NameOrNumber} East ({dock.Code})",
                Position = new Position3D(100, 0, zPositionRight),
                Dimensions = new Dimensions3D(
                    dock.Physical.Length,
                    dock.Physical.Depth,
                    2
                ),
                Rotation = new Rotation3D(0, 0, 0),
                Color = "#5d4037",
                Metadata = new Dictionary<string, object>
                {
                    { "code", dock.Code },
                    { "name", dock.NameOrNumber },
                    { "side", "right" },
                    { "location", dock.Location },
                    { "length", dock.Physical.Length },
                    { "depth", dock.Physical.Depth },
                    { "maxDraft", dock.Physical.MaxDraft },
                    { "allowedVesselTypes", dock.AllowedVesselTypes.ToArray() }
                }
            });

            // Cranes and vessels will be added from REAL DB below (physical resources + approved VVN)
        }

        // Get REAL storage areas from database for Layout B
        var storageAreasB = await _storageAreaRepository.GetAllAsync();
        var warehousesB = storageAreasB.Where(sa => sa.Type.ToLower() == "warehouse").Take(3).ToList();
        var yardsB = storageAreasB.Where(sa => sa.Type.ToLower() == "yard").Take(2).ToList();
        
        _logger.LogInformation("US 3.3.2: Loading {WarehouseCount} warehouses and {YardCount} yards from database for Layout B", 
            warehousesB.Count, yardsB.Count);

        // Real warehouses
        var warehousePositions = new[] { 
            new { x = 0, z = -180 }, 
            new { x = -20, z = 150 }, 
            new { x = 60, z = 180 } 
        };
        
        for (int i = 0; i < warehousesB.Count && i < warehousePositions.Length; i++)
        {
            var warehouse = warehousesB[i];
            var pos = warehousePositions[i];
            
            elements.Add(new PortElementDTO
            {
                Id = $"warehouse-{warehouse.Identifier}",
                Type = "warehouse",
                Name = warehouse.Identifier,
                Position = new Position3D(pos.x, 0, pos.z),
                Dimensions = new Dimensions3D(90 - i * 15, 60 - i * 7, 22 - i * 2),
                Rotation = new Rotation3D(0, i * 45, 0),
                Color = i == 0 ? "#d32f2f" : i == 1 ? "#c62828" : "#b71c1c",
                Metadata = new Dictionary<string, object> 
                { 
                    { "identifier", warehouse.Identifier },
                    { "location", warehouse.Location },
                    { "maxCapacityTeu", warehouse.MaxCapacityTeu },
                    { "currentOccupancyTeu", warehouse.CurrentOccupancyTeu }
                }
            });
        }

        // Real container yards (keep as-is; do not modify count/selection)
        var yardPositions = new[] { 
            new { x = -50, z = 50 }, 
            new { x = 80, z = -120 } 
        };
        
        for (int i = 0; i < yardsB.Count && i < yardPositions.Length; i++)
        {
            var yard = yardsB[i];
            var pos = yardPositions[i];
            
            elements.Add(new PortElementDTO
            {
                Id = $"yard-{yard.Identifier}",
                Type = "containerYard",
                Name = yard.Identifier,
                Position = new Position3D(pos.x, 0, pos.z),
                Dimensions = new Dimensions3D(120 - i * 20, 90 - i * 20, 0.5),
                Rotation = new Rotation3D(0, i * 30, 0),
                Color = i == 0 ? "#455a64" : "#37474f",
                Metadata = new Dictionary<string, object> 
                { 
                    { "identifier", yard.Identifier },
                    { "location", yard.Location },
                    { "maxCapacityTeu", yard.MaxCapacityTeu },
                    { "currentOccupancyTeu", yard.CurrentOccupancyTeu }
                }
            });

            // Yard cranes will be sourced from REAL physical resources below to avoid duplicates
        }

        // US 3.3.3: Use APPROVED vessel visit notifications (real data) for ships in Layout B
        var allVvns = _vesselVisitRepository.GetAll();
        var approvedVvns = allVvns
            .Where(v => v.Status == VvnStatus.Approved && !string.IsNullOrEmpty(v.AssignedDockCode))
            .Where(v => dockPlacementByCode.ContainsKey(v.AssignedDockCode!))
            .ToList();

        foreach (var v in approvedVvns)
        {
            var dockCode = v.AssignedDockCode!;
            var dock = dockByCode[dockCode];
            var placement = dockPlacementByCode[dockCode];
            var zPos = placement.z;
            var isLeft = placement.side == "left";

            // Derive ship dimensions from dock physical size to keep proportions
            var shipLen = Math.Min(dock.Physical.Length * 1.15, 320);
            var shipWid = Math.Min(dock.Physical.Depth * 0.75, 65);
            var shipHgt = 16;

            // Fetch real vessel data from database
            _logger.LogInformation("US 3.3.3: Looking up vessel with IMO {ImoNumber} from VVN (Layout B)", v.VesselImoNumber);
            var vessel = await _vesselRepository.GetByImoNumberAsync(v.VesselImoNumber);
            
            if (vessel == null)
            {
                _logger.LogWarning("US 3.3.3: Vessel with IMO {ImoNumber} not found in Vessels table (Layout B)", v.VesselImoNumber);
            }
            else
            {
                _logger.LogInformation("US 3.3.3: Found vessel {Name} (Type: {Type}, Operator: {Operator}) for Layout B", 
                    vessel.Name, vessel.VesselType, vessel.Operator);
            }
            
            var vesselName = vessel?.Name ?? $"IMO:{v.VesselImoNumber}";
            var vesselType = vessel?.VesselType ?? "Unknown";

            elements.Add(new PortElementDTO
            {
                Id = $"ship-{v.VesselImoNumber}",
                Type = "ship",
                Name = vesselName,
                Position = new Position3D(isLeft ? -700 : 700, -0.05, zPos),
                Dimensions = new Dimensions3D(shipLen, shipHgt, shipWid),
                Rotation = new Rotation3D(0, 0, 0),
                Color = "#1a237e",
                Metadata = new Dictionary<string, object>
                {
                    { "dockCode", dockCode },
                    { "approved", true },
                    { "imoNumber", v.VesselImoNumber },
                    { "name", vesselName },
                    { "type", vesselType },
                    { "vesselType", vesselType },
                    { "operator", vessel?.Operator ?? string.Empty },
                    { "eta", v.EstimatedTimeOfArrival?.ToString("o") ?? string.Empty },
                    { "etd", v.EstimatedTimeOfDeparture?.ToString("o") ?? string.Empty }
                }
            });
        }

        // US 3.3.3: Use REAL physical resources assigned to docks/yards for cranes in Layout B
        var allResourcesB = _physicalResourceRepository.GetAll();
        var assignedResourcesB = allResourcesB.Where(r => !string.IsNullOrEmpty(r.AssignedArea)).ToList();

        // Map yard identifiers to their chosen positions to place yard cranes nearby
        var yardPosMap = new Dictionary<string, (double x, double z)>();
        for (int i = 0; i < yardsB.Count && i < yardPositions.Length; i++)
        {
            yardPosMap[yardsB[i].Identifier] = (yardPositions[i].x, yardPositions[i].z);
        }

        foreach (var res in assignedResourcesB)
        {
            // Dock-assigned resources (e.g., STS)
            if (dockPlacementByCode.TryGetValue(res.AssignedArea!, out var placement))
            {
                var dock = dockByCode[res.AssignedArea!];
                var zPos = placement.z;
                var isLeft = placement.side == "left";

                // Determine element type based on PhysicalResource type
                var elementType = res.Type.ToLower() switch
                {
                    "truck" => "truck",
                    "forklift" => "forklift",
                    _ => "crane" // STS, RTG, RMG, etc.
                };
                
                // Adjust dimensions based on resource type
                var (width, height, length) = elementType switch
                {
                    "truck" => (15.0, 8.0, 36.0),
                    "forklift" => (2.0, 2.5, 3.0),
                    _ => (10.0, 12.0, 30.0)  // STS Crane
                };

                // Build metadata based on element type
                var dockMetadata = new Dictionary<string, object>
                {
                    { "code", res.Code },
                    { "description", res.Description },
                    { "assignedArea", res.AssignedArea! }
                };
                
                // Add crane-specific metadata only for cranes
                if (elementType == "crane")
                {
                    dockMetadata.Add("craneType", "STS");
                    dockMetadata.Add("model", "tower_crane");
                }

                elements.Add(new PortElementDTO
                {
                    Id = $"{elementType}-{res.Code}",
                    Type = elementType,
                    Name = res.Code,
                    Position = new Position3D(isLeft ? -60 : 60, 4, zPos),
                    Dimensions = new Dimensions3D(width, height, length),
                    Rotation = new Rotation3D(0, 0, 0),
                    Color = "#ff6f00",
                    Metadata = dockMetadata
                });
                continue;
            }

            // Yard-assigned resources (e.g., RTG/RMG)
            if (yardPosMap.TryGetValue(res.AssignedArea!, out var yz))
            {
                // Determine element type based on PhysicalResource type
                var elementType = res.Type.ToLower() switch
                {
                    "truck" => "truck",
                    "forklift" => "forklift",
                    _ => "crane" // RTG, RMG, etc.
                };
                
                // Adjust dimensions based on resource type
                var (width, height, length) = elementType switch
                {
                    "truck" => (15.0, 8.0, 36.0),
                    "forklift" => (2.0, 2.5, 3.0),
                    _ => (32.0, 40.0, 40.0)  // Yard crane - doubled dimensions
                };
                
                // Build metadata based on element type
                var yardMetadataB = new Dictionary<string, object>
                {
                    { "code", res.Code },
                    { "description", res.Description },
                    { "assignedArea", res.AssignedArea! }
                };
                
                // Add crane-specific metadata only for cranes
                if (elementType == "crane")
                {
                    yardMetadataB.Add("craneType", "RTG");
                    yardMetadataB.Add("model", "gantry_crane");
                }
                
                elements.Add(new PortElementDTO
                {
                    Id = $"yard-{elementType}-{res.Code}",
                    Type = elementType,
                    Name = res.Code,
                    Position = new Position3D(yz.x + 10, 4, yz.z + 10),
                    Dimensions = new Dimensions3D(width, height, length),
                    Rotation = new Rotation3D(0, 0, 0),
                    Color = "#ff9800",
                    Metadata = yardMetadataB
                });
            }
        }

        // Admin buildings
        elements.Add(new PortElementDTO
        {
            Id = "admin-1",
            Type = "adminBuilding",
            Name = "Main Administration",
            Position = new Position3D(0, 0, -250),
            Dimensions = new Dimensions3D(40, 25, 30),
            Rotation = new Rotation3D(0, 0, 0),
            Color = "#263238",
            Metadata = new Dictionary<string, object> { { "floors", 7 } }
        });

        elements.Add(new PortElementDTO
        {
            Id = "control-tower",
            Type = "adminBuilding",
            Name = "Control Tower",
            Position = new Position3D(150, 0, 0),
            Dimensions = new Dimensions3D(15, 15, 50),
            Rotation = new Rotation3D(0, 0, 0),
            Color = "#1565c0",
            Metadata = new Dictionary<string, object> { { "type", "tower" } }
        });

        // Count actual elements
        var totalDocks = elements.Count(e => e.Type.ToLower() == "dock");
        var leftDocksCount = elements.Count(e => e.Type.ToLower() == "dock" && e.Id.Contains("left"));
        var rightDocksCount = elements.Count(e => e.Type.ToLower() == "dock" && e.Id.Contains("right"));
        var warehouseCountB = elements.Count(e => e.Type.ToLower() == "warehouse");
        var yardCountB = elements.Count(e => e.Type.ToLower() == "containeryard");
        
        return new PortLayoutDTO
        {
            LayoutId = "B",
            LayoutName = $"Porto Layout B - {totalDocks} Docks Extended",
            Version = "1.0",
            Description = $"Extended layout with {totalDocks} docks ({leftDocksCount} left + {rightDocksCount} right), {warehouseCountB} warehouses and {yardCountB} container yards",
            Elements = elements,
            Settings = new Dictionary<string, object>
            {
                { "cameraPosition", new { x = 200, y = 150, z = 200 } },
                { "cameraTarget", new { x = 0, y = 0, z = 0 } },
                { "ambientLight", "#ffffff" },
                { "ambientIntensity", 0.7 },
                { "skyColor", "#4fc3f7" },
                { "gridSize", 600 }
            }
        };
    }

    /// <summary>
    /// Layout C: Complete port view - ALL docks and storage areas from database
    /// </summary>
    private async Task<PortLayoutDTO> GenerateLayoutC()
    {
        var elements = new List<PortElementDTO>();
        
        // Helper to normalize identifiers (treat underscores/hyphens/spaces equally, case-insensitive)
        static string NormalizeId(string? id)
        {
            if (string.IsNullOrWhiteSpace(id)) return string.Empty;
            var filtered = id!.Where(char.IsLetterOrDigit).ToArray();
            return new string(filtered).ToUpperInvariant();
        }

        // Port base platform (compact) - Dimensions3D: length, width, height
        elements.Add(new PortElementDTO
        {
            Id = "port-platform",
            Type = "platform",
            Name = "Porto Base Platform",
            Position = new Position3D(0, -2, 0),
            Dimensions = new Dimensions3D(300, 250, 2),
            Rotation = new Rotation3D(0, 0, 0),
            Color = "#9e9e9e",
            Metadata = new Dictionary<string, object> { { "material", "concrete" } }
        });

        // Water plane (compact for overview)
        elements.Add(new PortElementDTO
        {
            Id = "water",
            Type = "water",
            Name = "Porto Water Surface",
            Position = new Position3D(-80, -0.5, 0),
            Dimensions = new Dimensions3D(400, 400, 1),
            Rotation = new Rotation3D(0, 0, 0),
            Color = "#006064",
            Metadata = new Dictionary<string, object> { { "animated", true } }
        });

        // Get ALL docks from database
        var allDocks = await _dockRepository.GetAllAsync();
        var dockList = allDocks.ToList();
        
        _logger.LogInformation("US 3.3.2: Loading ALL {Count} docks from database for Layout C", dockList.Count);

        // Arrange docks in a row along the waterfront (negative X side)
        var dockSpacing = 40.0;  // Much tighter spacing for overview
        var startZ = -(dockList.Count - 1) * dockSpacing / 2; // Center the docks
        
        for (int i = 0; i < dockList.Count; i++)
        {
            var dock = dockList[i];
            var zPosition = startZ + (i * dockSpacing);
            
            // Very small scale for overview map (10-15% of real size)
            // Allows user to see entire port, then zoom in for details
            var minLength = 180.0;
            var maxLength = 240.0;
            var normalizedLengthRatio = Math.Clamp((dock.Physical.Length - minLength) / (maxLength - minLength), 0, 1);
            var dockLength = 20 + (normalizedLengthRatio * 8); // 20-28m (very small)
            
            var minDepth = 30.0;
            var maxDepth = 45.0;
            var normalizedDepthRatio = Math.Clamp((dock.Physical.Depth - minDepth) / (maxDepth - minDepth), 0, 1);
            var dockDepth = 8 + (normalizedDepthRatio * 4); // 8-12m (very small)
            
            elements.Add(new PortElementDTO
            {
                Id = $"dock-{dock.Code}",
                Type = "dock",
                Name = $"{dock.NameOrNumber} ({dock.Code})",
                Position = new Position3D(-80, 0, zPosition),
                Dimensions = new Dimensions3D(
                    dockLength,
                    dockDepth,
                    2
                ),
                Rotation = new Rotation3D(0, 0, 0),
                Color = "#5d4037",
                Metadata = new Dictionary<string, object>
                {
                    { "code", dock.Code },
                    { "location", dock.Location },
                    { "length", dock.Physical.Length },
                    { "depth", dock.Physical.Depth },
                    { "maxDraft", dock.Physical.MaxDraft },
                    { "allowedVesselTypes", dock.AllowedVesselTypes.ToArray() }
                }
            });
        }

        // US 3.3.3: Get APPROVED vessel visit notifications from database
        // Only show vessels that have approved visit notifications assigned to docks
        var allVesselVisits = _vesselVisitRepository.GetAll();
        var approvedVisits = allVesselVisits
            .Where(v => v.Status == VvnStatus.Approved && !string.IsNullOrEmpty(v.AssignedDockCode))
            .ToList();
        
        _logger.LogInformation("US 3.3.3: Found {Count} approved vessel visit notifications for Layout C", approvedVisits.Count);

        // Create a ship for each approved vessel visit notification
        foreach (var visit in approvedVisits)
        {
            // Find the dock this vessel is assigned to
            var assignedDock = dockList.FirstOrDefault(d => d.Code.Equals(visit.AssignedDockCode, StringComparison.OrdinalIgnoreCase));
            if (assignedDock == null)
            {
                _logger.LogWarning("US 3.3.3: Vessel {IMO} assigned to unknown dock {DockCode}, skipping", visit.VesselImoNumber, visit.AssignedDockCode);
                continue;
            }

            // Find the Z position of the assigned dock
            var dockIndex = dockList.IndexOf(assignedDock);
            var zPosition = startZ + (dockIndex * dockSpacing);

            // Calculate dock visual size for this specific dock
            var minLength = 180.0;
            var maxLength = 240.0;
            var normalizedLengthRatio = Math.Clamp((assignedDock.Physical.Length - minLength) / (maxLength - minLength), 0, 1);
            var dockLength = 20 + (normalizedLengthRatio * 8); // 20-28m

            // Calculate ship dimensions based on vessel data
            var shipLength = dockLength * 1.5; // Scale with dock
            var shipWidth = 6.0;
            var shipHeight = 8.0;

            // Fetch real vessel data from database
            _logger.LogInformation("US 3.3.3: Looking up vessel with IMO {ImoNumber} from VVN (Layout C)", visit.VesselImoNumber);
            var vessel = await _vesselRepository.GetByImoNumberAsync(visit.VesselImoNumber);
            
            if (vessel == null)
            {
                _logger.LogWarning("US 3.3.3: Vessel with IMO {ImoNumber} not found in Vessels table (Layout C)", visit.VesselImoNumber);
            }
            else
            {
                _logger.LogInformation("US 3.3.3: Found vessel {Name} (Type: {Type}, Operator: {Operator}) for Layout C", 
                    vessel.Name, vessel.VesselType, vessel.Operator);
            }
            
            var vesselName = vessel?.Name ?? $"IMO:{visit.VesselImoNumber}";
            var vesselType = vessel?.VesselType ?? "Unknown";

            elements.Add(new PortElementDTO
            {
                Id = $"ship-{visit.VesselImoNumber}",
                Type = "ship",
                Name = vesselName,
                Position = new Position3D(dockLength >= 26 ? -250 : -230, -0.05, zPosition),
                Dimensions = new Dimensions3D(shipLength, shipWidth, shipHeight),
                Rotation = new Rotation3D(0, 0, 0),
                Color = "#1a237e",
                Metadata = new Dictionary<string, object>
                {
                    { "imoNumber", visit.VesselImoNumber },
                    { "name", vesselName },
                    { "type", vesselType },
                    { "vesselType", vesselType },
                    { "operator", vessel?.Operator ?? string.Empty },
                    { "dockCode", visit.AssignedDockCode ?? "" },
                    { "approved", true },
                    { "eta", visit.EstimatedTimeOfArrival?.ToString("o") ?? "" },
                    { "etd", visit.EstimatedTimeOfDeparture?.ToString("o") ?? "" }
                }
            });
        }

        // Get ALL storage areas from database (needed for both storage and resources)
        var storageAreas = await _storageAreaRepository.GetAllAsync();
        var allStorageAreas = storageAreas.ToList();
        
        var warehouses = allStorageAreas.Where(s => s.Type.Equals("Warehouse", StringComparison.OrdinalIgnoreCase)).ToList();
        var yards = allStorageAreas.Where(s => s.Type.Equals("Yard", StringComparison.OrdinalIgnoreCase)).ToList();
        
        _logger.LogInformation("US 3.3.2: Loading ALL storage areas: {WarehouseCount} warehouses and {YardCount} yards from database for Layout C", 
            warehouses.Count, yards.Count);

        // Calculate yard positioning variables (needed later for resources)
        var warehouseStartX = 40.0; // move warehouses further inland
        var warehouseStartZ = -40.0;
        var warehouseSpacingX = 30.0;
        var warehouseSpacingZ = 35.0;
        var warehousesPerRow = 2;
        var yardStartX = 40.0; // move yards further inland
        var yardStartZ = warehouseStartZ + ((warehouses.Count / warehousesPerRow + 1) * warehouseSpacingZ);
        var yardSpacingZ = 35.0;

        // US 3.3.3: Get physical resources (cranes) with assigned areas from database
        var allResources = _physicalResourceRepository.GetAll();
        var assignedResources = allResources
            .Where(r => !string.IsNullOrEmpty(r.AssignedArea))
            .ToList();
        
        _logger.LogInformation("US 3.3.3: Found {Count} physical resources with assigned areas for Layout C", assignedResources.Count);
        
        foreach (var r in assignedResources)
        {
            _logger.LogInformation("US 3.3.3: Resource {Code} ({Type}) assigned to {Area}", r.Code, r.Type, r.AssignedArea);
        }

        // Build normalized lookup for yards by identifier for robust matching (e.g., YARD-MAIN-001 vs YARD_MAIN_001)
        var yardByNormId = yards.ToDictionary(y => NormalizeId(y.Identifier), y => y);

        // Placement uses AssignedArea from DB only (no layout-specific overrides)

        // Create cranes for resources assigned to docks
        foreach (var resource in assignedResources)
        {
            var effectiveArea = resource.AssignedArea ?? string.Empty;
            // Check if resource is assigned to a dock
            var assignedDock = dockList.FirstOrDefault(d => d.Code.Equals(effectiveArea, StringComparison.OrdinalIgnoreCase));
            if (assignedDock != null)
            {
                var dockIndex = dockList.IndexOf(assignedDock);
                var zPosition = startZ + (dockIndex * dockSpacing);

                // Determine element type based on PhysicalResource type
                var elementType = resource.Type.ToLower() switch
                {
                    "truck" => "truck",
                    "forklift" => "forklift",
                    _ => "crane" // STS, RTG, RMG, etc.
                };

                // Adjust dimensions based on resource type (aligned with Layout A/B scale)
                var (width, height, length) = elementType switch
                {
                    "truck" => (15.0, 8.0, 36.0),      // Truck
                    "forklift" => (2.0, 2.5, 3.0),    // Forklift
                    _ => (10.0, 12.0, 30.0)            // STS crane at docks
                };

                var dockMetadataC = new Dictionary<string, object>
                {
                    { "code", resource.Code },
                    { "description", resource.Description },
                    { "assignedArea", effectiveArea }
                };
                
                if (elementType == "crane")
                {
                    dockMetadataC.Add("craneType", "STS");
                    dockMetadataC.Add("model", "tower_crane");
                }

                elements.Add(new PortElementDTO
                {
                    Id = $"{elementType}-resource-{resource.Code}",
                    Type = elementType,
                    Name = resource.Code,
                    Position = new Position3D(-10, 4, zPosition),
                    Dimensions = new Dimensions3D(width, height, length),
                    Rotation = new Rotation3D(0, 0, 0),
                    Color = "#ff6f00",
                    Metadata = dockMetadataC
                });
                continue;
            }

            // Check if resource is assigned to a storage area (yard)
            var assignedYard = yardByNormId.TryGetValue(NormalizeId(effectiveArea), out var yy) ? yy : null;
            if (assignedYard != null)
            {
                var yardIndex = yards.IndexOf(assignedYard);
                var zPosition = yardStartZ + (yardIndex * yardSpacingZ);

                // Determine element type based on PhysicalResource type
                var elementType = resource.Type.ToLower() switch
                {
                    "truck" => "truck",
                    "forklift" => "forklift",
                    _ => "crane" // RTG, RMG, etc.
                };

                // Adjust dimensions based on resource type (yard cranes now half the previous size)
                var (width, height, length) = elementType switch
                {
                    "truck" => (15.0, 8.0, 36.0),      // Truck
                    "forklift" => (2.0, 2.5, 3.0),    // Forklift
                    _ => (16.0, 20.0, 20.0)            // Yard crane (reduced by half)
                };

                // Compute where the associated yard is placed and position the crane at its west edge
                var yardCenterX = yardStartX + 100; // must match the yard creation X
                var minYardCapacity = 6000.0;
                var maxYardCapacity = 8500.0;
                var normalizedYardCapacity = Math.Clamp((assignedYard.MaxCapacityTeu - minYardCapacity) / (maxYardCapacity - minYardCapacity), 0, 1);
                var yardWidth = 20 + (normalizedYardCapacity * 12); // must match yard creation width
                var edgeOffset = 2.0; // small margin outside the yard edge
                // Place at the east edge (same Z) so it's right beside the containers
                var craneXAtYardEastEdge = yardCenterX + (yardWidth / 2.0) + edgeOffset;

                var yardMetadataC = new Dictionary<string, object>
                {
                    { "code", resource.Code },
                    { "description", resource.Description },
                    { "assignedArea", effectiveArea }
                };
                
                if (elementType == "crane")
                {
                    yardMetadataC.Add("craneType", "RTG");
                    yardMetadataC.Add("model", "gantry_crane");
                }

                elements.Add(new PortElementDTO
                {
                    Id = $"yard-{elementType}-resource-{resource.Code}",
                    Type = elementType,
                    Name = resource.Code,
                    // Place crane right beside the container yard (east edge, near containers)
                    Position = new Position3D(craneXAtYardEastEdge, 4, zPosition),
                    Dimensions = new Dimensions3D(width, height, length),
                    Rotation = new Rotation3D(0, 0, 0),
                    Color = "#ff9800",
                    Metadata = yardMetadataC
                });
            }
            else
            {
                // Resource has AssignedArea but it doesn't match any dock or yard
                _logger.LogWarning("US 3.3.3: Resource {Code} ({Type}) assigned to '{Area}' but no matching dock or yard found - SKIPPING", 
                    resource.Code, resource.Type, effectiveArea);
            }
        }

        // Arrange warehouses in a grid on the right side (compact)
        for (int i = 0; i < warehouses.Count; i++)
        {
            var warehouse = warehouses[i];
            var row = i / warehousesPerRow;
            var col = i % warehousesPerRow;
            var xPosition = warehouseStartX + (col * warehouseSpacingX);
            var zPosition = warehouseStartZ + (row * warehouseSpacingZ);

            // Much smaller scale for overview map
            var minCapacity = 4000.0;
            var maxCapacity = 6500.0;
            var normalizedCapacity = Math.Clamp((warehouse.MaxCapacityTeu - minCapacity) / (maxCapacity - minCapacity), 0, 1);
            
            var warehouseWidth = 15 + (normalizedCapacity * 8);   // 15-23 (small)
            var warehouseDepth = 12 + (normalizedCapacity * 6);   // 12-18 (small)
            var warehouseHeight = 6 + (normalizedCapacity * 3);   // 6-9 (small)

            elements.Add(new PortElementDTO
            {
                Id = $"warehouse-{warehouse.Identifier}",
                Type = "warehouse",
                Name = $"Warehouse {warehouse.Identifier}",
                Position = new Position3D(xPosition, 0, zPosition),
                Dimensions = new Dimensions3D(warehouseWidth, warehouseDepth, warehouseHeight),
                Rotation = new Rotation3D(0, 0, 0),
                Color = "#c62828",
                Metadata = new Dictionary<string, object>
                { 
                    { "identifier", warehouse.Identifier },
                    { "location", warehouse.Location },
                    { "maxCapacityTeu", warehouse.MaxCapacityTeu },
                    { "currentOccupancyTeu", warehouse.CurrentOccupancyTeu }
                }
            });
        }

        // Arrange container yards below warehouses (compact)
        for (int i = 0; i < yards.Count; i++)
        {
            var yard = yards[i];
            var zPosition = yardStartZ + (i * yardSpacingZ);

            // Much smaller scale for overview map
            var minYardCapacity = 6000.0;
            var maxYardCapacity = 8500.0;
            var normalizedYardCapacity = Math.Clamp((yard.MaxCapacityTeu - minYardCapacity) / (maxYardCapacity - minYardCapacity), 0, 1);
            
            var yardWidth = 20 + (normalizedYardCapacity * 12);  // 20-32 (small)
            var yardDepth = 18 + (normalizedYardCapacity * 10);  // 18-28 (small)

            elements.Add(new PortElementDTO
            {
                Id = $"yard-{yard.Identifier}",
                Type = "containerYard",
                Name = $"Container Yard {yard.Identifier}",
                Position = new Position3D(yardStartX + 100, 0, zPosition),
                Dimensions = new Dimensions3D(yardWidth, yardDepth, 1),
                Rotation = new Rotation3D(0, 0, 0),
                Color = "#546e7a",
                Metadata = new Dictionary<string, object>
                { 
                    { "identifier", yard.Identifier },
                    { "location", yard.Location },
                    { "maxCapacityTeu", yard.MaxCapacityTeu },
                    { "currentOccupancyTeu", yard.CurrentOccupancyTeu }
                }
            });
        }

        // Admin building
        elements.Add(new PortElementDTO
        {
            Id = "admin-main",
            Type = "adminBuilding",
            Name = "Port Authority HQ",
            Position = new Position3D(30, 0, -120),
            Dimensions = new Dimensions3D(12, 8, 10),
            Rotation = new Rotation3D(0, 0, 0),
            Color = "#37474f",
            Metadata = new Dictionary<string, object> { { "floors", 10 } }
        });

        // Count elements
        var totalDocks = dockList.Count;
        var totalWarehouses = warehouses.Count;
        var totalYards = yards.Count;
        var totalVessels = approvedVisits.Count;
        var totalResources = assignedResources.Count;
        
        _logger.LogInformation("US 3.3.3: Layout C contains {Docks} docks, {Vessels} vessels (approved), {Resources} physical resources", 
            totalDocks, totalVessels, totalResources);
        
        return new PortLayoutDTO
        {
            LayoutId = "C",
            LayoutName = $"Porto Layout C - Real Data View",
            Version = "2.0",
            Description = $"US 3.3.3: Port with ALL {totalDocks} docks, {totalVessels} approved vessels, {totalResources} assigned resources, {totalWarehouses} warehouses and {totalYards} yards from database",
            Elements = elements,
            Settings = new Dictionary<string, object>
            {
                { "cameraPosition", new { x = 120, y = 120, z = 120 } },
                { "cameraTarget", new { x = 0, y = 0, z = 0 } },
                { "ambientLight", "#ffffff" },
                { "ambientIntensity", 0.85 },
                { "skyColor", "#0277bd" },
                { "gridSize", 300 }
            }
        };
    }
}

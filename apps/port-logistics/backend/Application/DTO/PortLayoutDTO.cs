namespace Application.DTO;

/// <summary>
/// US 3.3.2: 3D position in world space
/// </summary>
public class Position3D
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Z { get; set; }

    public Position3D() { }

    public Position3D(double x, double y, double z)
    {
        X = x;
        Y = y;
        Z = z;
    }
}

/// <summary>
/// US 3.3.2: 3D dimensions (length, width, height)
/// </summary>
public class Dimensions3D
{
    public double Length { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }

    public Dimensions3D() { }

    public Dimensions3D(double length, double width, double height)
    {
        Length = length;
        Width = width;
        Height = height;
    }
}

/// <summary>
/// US 3.3.2: 3D rotation (Euler angles in degrees)
/// </summary>
public class Rotation3D
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Z { get; set; }

    public Rotation3D() { }

    public Rotation3D(double x, double y, double z)
    {
        X = x;
        Y = y;
        Z = z;
    }
}

/// <summary>
/// US 3.3.2: Port element (dock, warehouse, container yard, etc.)
/// </summary>
public class PortElementDTO
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // "dock", "warehouse", "containerYard", "crane", "building"
    public string Name { get; set; } = string.Empty;
    public Position3D Position { get; set; } = new();
    public Dimensions3D Dimensions { get; set; } = new();
    public Rotation3D Rotation { get; set; } = new();
    public string Color { get; set; } = "#808080"; // Hex color for rendering
    public Dictionary<string, object>? Metadata { get; set; } // Additional properties (capacity, status, etc.)
}

/// <summary>
/// US 3.3.2: Complete port layout for 3D visualization
/// </summary>
public class PortLayoutDTO
{
    public string LayoutId { get; set; } = string.Empty;
    public string LayoutName { get; set; } = string.Empty;
    public string Version { get; set; } = "1.0";
    public string Description { get; set; } = string.Empty;
    public List<PortElementDTO> Elements { get; set; } = new();
    public Dictionary<string, object>? Settings { get; set; } // Camera, lighting, environment settings
}

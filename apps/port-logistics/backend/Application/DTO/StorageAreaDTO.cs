namespace Application.DTO;

using Domain.Model;

public class StorageAreaDTO
{
    public string Identifier { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // Yard, Warehouse
    public string Location { get; set; } = string.Empty;
    public int MaxCapacityTeu { get; set; }
    public int CurrentOccupancyTeu { get; set; }
    public List<ServedDockDTO> ServedDocks { get; set; } = new();

    public static StorageAreaDTO ToDTO(StorageArea s)
        => new StorageAreaDTO
        {
            Identifier = s.Identifier,
            Type = s.Type,
            Location = s.Location,
            MaxCapacityTeu = s.MaxCapacityTeu,
            CurrentOccupancyTeu = s.CurrentOccupancyTeu,
            ServedDocks = s.ServedDocks.Select(d => new ServedDockDTO { DockCode = d.DockCode, DistanceMeters = d.DistanceMeters }).ToList()
        };

    public static IEnumerable<StorageAreaDTO> ToDTO(IEnumerable<StorageArea> items) => items.Select(ToDTO);

    public static StorageArea ToDomain(StorageAreaDTO dto)
    {
        var s = new StorageArea(dto.Identifier, dto.Type, dto.Location, dto.MaxCapacityTeu, dto.CurrentOccupancyTeu);
        if (dto.ServedDocks != null && dto.ServedDocks.Count > 0)
            s.SetServedDocks(dto.ServedDocks.Select(d => new ServedDock(d.DockCode, d.DistanceMeters)));
        return s;
    }

    public static void UpdateToDomain(StorageArea domain, StorageAreaDTO dto)
    {
        domain.Update(dto.Type, dto.Location, dto.MaxCapacityTeu, dto.CurrentOccupancyTeu);
        if (dto.ServedDocks != null)
            domain.SetServedDocks(dto.ServedDocks.Select(d => new ServedDock(d.DockCode, d.DistanceMeters)));
    }
}

public class ServedDockDTO
{
    public string DockCode { get; set; } = string.Empty;
    public double DistanceMeters { get; set; }
}

using Application.DTO;
using Domain.IRepository;
using Domain.Model;
using System.Text.Json;

namespace Application.Services;

public class IncidentService
{
    private readonly IIncidentRepository _incidentRepository;
    private readonly IIncidentTypeRepository _incidentTypeRepository;
    private readonly IVesselVisitExecutionRepository _vveRepository;

    public IncidentService(
        IIncidentRepository incidentRepository,
        IIncidentTypeRepository incidentTypeRepository,
        IVesselVisitExecutionRepository vveRepository)
    {
        _incidentRepository = incidentRepository;
        _incidentTypeRepository = incidentTypeRepository;
        _vveRepository = vveRepository;
    }

    /// <summary>
    /// Get all incidents with optional filtering
    /// </summary>
    public async Task<IEnumerable<IncidentDTO>> GetAllAsync(IncidentFilterDTO? filter = null)
    {
        IEnumerable<Incident> incidents;

        // Apply primary filter
        if (filter?.ActiveOnly == true)
        {
            incidents = await _incidentRepository.GetActiveIncidentsAsync();
        }
        else if (filter?.Status != null && Enum.TryParse<IncidentStatus>(filter.Status, true, out var status))
        {
            incidents = await _incidentRepository.GetByStatusAsync(status);
        }
        else if (filter?.Severity != null && Enum.TryParse<IncidentSeverity>(filter.Severity, true, out var severity))
        {
            incidents = await _incidentRepository.GetBySeverityAsync(severity);
        }
        else if (filter?.StartDate.HasValue == true && filter?.EndDate.HasValue == true)
        {
            incidents = await _incidentRepository.GetByDateRangeAsync(filter.StartDate.Value, filter.EndDate.Value);
        }
        else
        {
            incidents = await _incidentRepository.GetAllAsync();
        }

        // Additional client-side filtering
        if (filter != null)
        {
            if (filter.Severity != null && Enum.TryParse<IncidentSeverity>(filter.Severity, true, out var sev))
                incidents = incidents.Where(i => i.Severity == sev);

            if (filter.StartDate.HasValue)
                incidents = incidents.Where(i => i.StartTime >= filter.StartDate.Value);

            if (filter.EndDate.HasValue)
                incidents = incidents.Where(i => i.StartTime <= filter.EndDate.Value);

            // Filter by specific vessel visit
            if (!string.IsNullOrEmpty(filter.VesselVisitId))
            {
                incidents = incidents.Where(i =>
                {
                    var affectedVVEs = DeserializeAffectedVVEs(i.AffectedVVEs);
                    return affectedVVEs.Contains(filter.VesselVisitId) || affectedVVEs.Contains("*");
                });
            }
        }

        return incidents.Select(MapToDTO);
    }

    /// <summary>
    /// Get incident by ID
    /// </summary>
    public async Task<IncidentDTO?> GetByIdAsync(Guid id)
    {
        var incident = await _incidentRepository.GetByIdAsync(id);
        return incident == null ? null : MapToDTO(incident);
    }

    /// <summary>
    /// Create a new incident
    /// </summary>
    public async Task<IncidentDTO> CreateAsync(CreateIncidentDTO dto)
    {
        // Validate incident type exists
        if (!await _incidentTypeRepository.ExistsAsync(dto.IncidentTypeCode))
            throw new InvalidOperationException($"Incident type with code {dto.IncidentTypeCode} not found");

        // Parse and validate severity
        if (!Enum.TryParse<IncidentSeverity>(dto.Severity, true, out var severity))
            throw new ArgumentException($"Invalid severity: {dto.Severity}. Valid values: Minor, Major, Critical");

        // Process affected VVEs
        var affectedVVEIds = dto.AffectedVVEIds ?? new List<string>();
        
        // If no VVEs specified, it affects all ongoing VVEs
        if (affectedVVEIds.Count == 0)
        {
            affectedVVEIds = new List<string> { "*" };
        }
        else
        {
            // Validate that specified VVEs exist
            foreach (var vveId in affectedVVEIds.Where(id => id != "*"))
            {
                if (!await _vveRepository.ExistsAsync(vveId))
                    throw new InvalidOperationException($"Vessel visit execution with ID {vveId} not found");
            }
        }

        var incident = new Incident
        {
            Id = Guid.NewGuid(),
            IncidentTypeCode = dto.IncidentTypeCode,
            StartTime = dto.StartTime,
            Severity = severity,
            Description = dto.Description,
            AffectedVVEs = SerializeAffectedVVEs(affectedVVEIds),
            Status = IncidentStatus.Active,
            CreatedBy = dto.CreatedBy,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _incidentRepository.AddAsync(incident);
        return MapToDTO(created);
    }

    /// <summary>
    /// Update an existing incident
    /// </summary>
    public async Task<IncidentDTO> UpdateAsync(Guid id, UpdateIncidentDTO dto)
    {
        var incident = await _incidentRepository.GetByIdAsync(id);
        if (incident == null)
            throw new KeyNotFoundException($"Incident with ID {id} not found");

        // Update incident type if provided
        if (!string.IsNullOrEmpty(dto.IncidentTypeCode))
        {
            if (!await _incidentTypeRepository.ExistsAsync(dto.IncidentTypeCode))
                throw new InvalidOperationException($"Incident type with code {dto.IncidentTypeCode} not found");
            incident.IncidentTypeCode = dto.IncidentTypeCode;
        }

        // Update severity if provided
        if (!string.IsNullOrEmpty(dto.Severity))
        {
            if (!Enum.TryParse<IncidentSeverity>(dto.Severity, true, out var severity))
                throw new ArgumentException($"Invalid severity: {dto.Severity}. Valid values: Minor, Major, Critical");
            incident.Severity = severity;
        }

        // Update status if provided
        if (!string.IsNullOrEmpty(dto.Status))
        {
            if (!Enum.TryParse<IncidentStatus>(dto.Status, true, out var status))
                throw new ArgumentException($"Invalid status: {dto.Status}. Valid values: Active, Resolved");
            
            // If marking as resolved, set end time and calculate duration
            if (status == IncidentStatus.Resolved && incident.Status != IncidentStatus.Resolved)
            {
                incident.EndTime = dto.EndTime ?? DateTime.UtcNow;
                incident.Status = status;
            }
            else
            {
                incident.Status = status;
            }
        }

        // Update end time if provided (for manual adjustment)
        if (dto.EndTime.HasValue)
        {
            incident.EndTime = dto.EndTime.Value;
        }

        // Update description if provided
        if (!string.IsNullOrEmpty(dto.Description))
        {
            incident.Description = dto.Description;
        }

        // Update affected VVEs if provided
        if (dto.AffectedVVEIds != null)
        {
            // Validate VVEs exist
            foreach (var vveId in dto.AffectedVVEIds.Where(id => id != "*"))
            {
                if (!await _vveRepository.ExistsAsync(vveId))
                    throw new InvalidOperationException($"Vessel visit execution with ID {vveId} not found");
            }
            incident.AffectedVVEs = SerializeAffectedVVEs(dto.AffectedVVEIds);
        }

        var updated = await _incidentRepository.UpdateAsync(incident);
        return MapToDTO(updated);
    }

    /// <summary>
    /// Mark an incident as resolved
    /// </summary>
    public async Task<IncidentDTO> ResolveAsync(Guid id, DateTime? endTime = null)
    {
        var incident = await _incidentRepository.GetByIdAsync(id);
        if (incident == null)
            throw new KeyNotFoundException($"Incident with ID {id} not found");

        if (incident.Status == IncidentStatus.Resolved)
            throw new InvalidOperationException("Incident is already resolved");

        incident.Status = IncidentStatus.Resolved;
        incident.EndTime = endTime ?? DateTime.UtcNow;

        var updated = await _incidentRepository.UpdateAsync(incident);
        return MapToDTO(updated);
    }

    /// <summary>
    /// Associate or detach VVEs from an incident
    /// </summary>
    public async Task<IncidentDTO> UpdateAffectedVVEsAsync(Guid id, List<string> vveIds)
    {
        var incident = await _incidentRepository.GetByIdAsync(id);
        if (incident == null)
            throw new KeyNotFoundException($"Incident with ID {id} not found");

        // Validate VVEs exist
        foreach (var vveId in vveIds.Where(id => id != "*"))
        {
            if (!await _vveRepository.ExistsAsync(vveId))
                throw new InvalidOperationException($"Vessel visit execution with ID {vveId} not found");
        }

        incident.AffectedVVEs = SerializeAffectedVVEs(vveIds);
        var updated = await _incidentRepository.UpdateAsync(incident);
        return MapToDTO(updated);
    }

    /// <summary>
    /// Delete an incident
    /// </summary>
    public async Task<bool> DeleteAsync(Guid id)
    {
        return await _incidentRepository.DeleteAsync(id);
    }

    /// <summary>
    /// Get active incidents affecting a specific VVE
    /// </summary>
    public async Task<IEnumerable<IncidentDTO>> GetActiveIncidentsForVVEAsync(string vveId)
    {
        var activeIncidents = await _incidentRepository.GetActiveIncidentsAsync();
        
        var filtered = activeIncidents.Where(i =>
        {
            var affectedVVEs = DeserializeAffectedVVEs(i.AffectedVVEs);
            return affectedVVEs.Contains(vveId) || affectedVVEs.Contains("*");
        });

        return filtered.Select(MapToDTO);
    }

    // Helper methods for mapping
    private IncidentDTO MapToDTO(Incident incident)
    {
        return new IncidentDTO
        {
            Id = incident.Id,
            IncidentTypeCode = incident.IncidentTypeCode,
            IncidentTypeName = incident.IncidentType?.Name ?? string.Empty,
            StartTime = incident.StartTime,
            EndTime = incident.EndTime,
            Severity = incident.Severity.ToString(),
            Description = incident.Description,
            AffectedVVEIds = DeserializeAffectedVVEs(incident.AffectedVVEs),
            Status = incident.Status.ToString(),
            CreatedBy = incident.CreatedBy,
            CreatedAt = incident.CreatedAt,
            ResolvedAt = incident.ResolvedAt,
            DurationMinutes = incident.DurationMinutes
        };
    }

    private static string SerializeAffectedVVEs(List<string> vveIds)
    {
        return JsonSerializer.Serialize(vveIds);
    }

    private static List<string> DeserializeAffectedVVEs(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }
}

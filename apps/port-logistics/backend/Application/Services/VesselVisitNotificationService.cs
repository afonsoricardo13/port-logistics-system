namespace Application.Services;

using Application.DTO;
using Domain.IRepository;
using Domain.Model;

public class VesselVisitNotificationService
{
    private readonly IVesselVisitNotificationRepository _repo;
    private readonly IDockRepository _docks;
    private readonly IVesselRepository _vessels;
    private readonly IRepresentativeRepository _representativeRepo;
    private readonly IOrganizationRepository _organizationRepo;

    public VesselVisitNotificationService(
        IVesselVisitNotificationRepository repo, 
        IDockRepository docks, 
        IVesselRepository vessels,
        IRepresentativeRepository representativeRepo,
        IOrganizationRepository organizationRepo)
    {
        _repo = repo;
        _docks = docks;
        _vessels = vessels;
        _representativeRepo = representativeRepo;
        _organizationRepo = organizationRepo;
    }

    private async Task PopulateOrganizationAsync(VesselVisitNotificationDTO dto)
    {
        if (dto.SubmittedByRepresentativeId.HasValue && dto.SubmittedByRepresentativeId.Value > 0)
        {
            var rep = await _representativeRepo.GetByIdAsync(dto.SubmittedByRepresentativeId.Value);
            if (rep != null)
            {
                var org = await _organizationRepo.GetByIdAsync(rep.OrganizationId);
                dto.OrganizationIdentifier = org?.Identifier;
            }
        }
    }

    public async Task<VesselVisitNotificationDTO?> GetById(long id)
    {
        var vvn = await _repo.GetByIdAsync(id);
        if (vvn == null) return null;
        
        var dto = VesselVisitNotificationDTO.ToDTO(vvn);
        
        // Load additional fields from DataModel
        var additionalFields = await _repo.GetAdditionalFieldsAsync(id);
        dto.CaptainName = additionalFields.CaptainName;
        dto.CrewCount = additionalFields.CrewCount;
        dto.SafetyOfficers = additionalFields.SafetyOfficers;
        dto.PreferredDockCode = additionalFields.PreferredDockCode;
        dto.UnloadingManifest = additionalFields.UnloadingManifest;
        dto.LoadingManifest = additionalFields.LoadingManifest;
        
        // US 2.2.10: Get rejection reason from decision logs
        if (vvn.Status == Domain.Model.VvnStatus.Rejected)
        {
            var rejectionLog = vvn.DecisionLogs
                .Where(log => log.Outcome == Domain.Model.DecisionOutcome.Rejected)
                .OrderByDescending(log => log.Timestamp)
                .FirstOrDefault();
            
            dto.RejectionReason = rejectionLog?.Reason;
        }
        
        await PopulateOrganizationAsync(dto);
        return dto;
    }

    public async Task<VesselVisitNotificationDTO?> Create(VesselVisitNotificationDTO dto, List<string> errors, long representativeId)
    {
        // Accept incomplete data; only validate basic Vessel IMO presence
        if (string.IsNullOrWhiteSpace(dto.VesselImoNumber))
        {
            errors.Add("Vessel IMO number is required");
            return null;
        }

        if (representativeId <= 0)
        {
            errors.Add("Representative ID is required");
            return null;
        }

        // US 2.2.8/2.2.9: Validate that vessel exists in the database
        var vessel = await _vessels.GetByImoNumberAsync(dto.VesselImoNumber);
        if (vessel == null)
        {
            errors.Add($"Vessel with IMO number {dto.VesselImoNumber} does not exist. Please register the vessel first.");
            return null;
        }

        // Validate crew count
        if (dto.CrewCount.HasValue && dto.CrewCount.Value < 1)
        {
            errors.Add("Crew count must be at least 1");
            return null;
        }

        // Validate preferred dock exists if provided
        if (!string.IsNullOrWhiteSpace(dto.PreferredDockCode))
        {
            var dockExists = (await _docks.GetAllAsync()).Any(d => d.Code.Equals(dto.PreferredDockCode, StringComparison.OrdinalIgnoreCase));
            if (!dockExists)
            {
                errors.Add($"Preferred dock '{dto.PreferredDockCode}' does not exist");
                return null;
            }
        }

        var crew = dto.Crew.Select(c => new CrewMember(c.Name, c.CitizenId, c.Nationality));
        
        // Validate containers BEFORE creating domain object to avoid partial saves
        if (dto.ContainerIds != null && dto.ContainerIds.Any())
        {
            foreach (var cid in dto.ContainerIds)
            {
                if (!string.IsNullOrWhiteSpace(cid) && !Iso6346.IsValid(cid))
                {
                    errors.Add($"Invalid container ID (ISO 6346): {cid}");
                }
            }
            if (errors.Count > 0) return null;
        }
        
        var domain = new VesselVisitNotification(
            dto.VesselImoNumber,
            dto.EstimatedTimeOfArrival,
            dto.EstimatedTimeOfDeparture,
            dto.ContainerIds,
            crew,
            representativeId);

        // If status is Submitted, call Submit() on the domain object before saving
        if (!string.IsNullOrWhiteSpace(dto.Status) && 
            string.Equals(dto.Status, VvnStatus.Submitted.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            domain.Submit(errors);
            if (errors.Count > 0) return null;
        }

        var saved = await _repo.Add(domain);
        
        // Update with additional fields (captain, crew count, safety officers, manifests, preferred dock)
        // These are stored in DataModel but not in Domain
        await UpdateAdditionalFields(saved.Id, dto);
        
        // Fetch again to get complete data
        var result = await GetById(saved.Id);
        return result;
    }
    
    private async Task UpdateAdditionalFields(long vvnId, VesselVisitNotificationDTO dto)
    {
        // This is a workaround: we directly update the DataModel with additional fields
        // In a clean architecture, these should be in the Domain model
        var vvnDataModel = await _repo.GetByIdAsync(vvnId);
        if (vvnDataModel != null)
        {
            // The repository returns Domain, we need to update DataModel directly
            // This will be handled via Update method
            await _repo.UpdateAdditionalFields(vvnId, dto.CaptainName, dto.CrewCount, 
                dto.SafetyOfficers, dto.PreferredDockCode, 
                dto.UnloadingManifest, dto.LoadingManifest);
        }
    }

    public async Task<VesselVisitNotificationDTO?> Submit(long id, List<string> errors)
    {
        var vvn = await _repo.GetByIdAsync(id);
        if (vvn == null)
        {
            errors.Add("Vessel Visit Notification not found");
            return null;
        }
        vvn.Submit(errors);
        if (errors.Count > 0) return null;

        var updated = await _repo.Update(vvn, errors);
        return updated == null ? null : VesselVisitNotificationDTO.ToDTO(updated);
    }

    // US 2.2.7
    public async Task<VesselVisitNotificationDTO?> Approve(long id, string officerId, ApproveNotificationDTO dto, List<string> errors)
    {
        var vvn = await _repo.GetByIdAsync(id);
        if (vvn == null)
        {
            errors.Add("Vessel Visit Notification not found");
            return null;
        }

        // Validate dock exists
        var dockExists = (await _docks.GetAllAsync()).Any(d => d.Code.Equals(dto.AssignedDockCode, StringComparison.OrdinalIgnoreCase));
        if (!dockExists)
        {
            errors.Add("Assigned dock does not exist");
            return null;
        }

        vvn.Approve(officerId, dto.AssignedDockCode, errors);
        if (errors.Count > 0) return null;

        var saved = await _repo.Update(vvn, errors);
        return saved == null ? null : VesselVisitNotificationDTO.ToDTO(saved);
    }

    public async Task<VesselVisitNotificationDTO?> Reject(long id, string officerId, RejectNotificationDTO dto, List<string> errors)
    {
        var vvn = await _repo.GetByIdAsync(id);
        if (vvn == null)
        {
            errors.Add("Vessel Visit Notification not found");
            return null;
        }

        vvn.Reject(officerId, dto.Reason, errors);
        if (errors.Count > 0) return null;

        var saved = await _repo.Update(vvn, errors);
        return saved == null ? null : VesselVisitNotificationDTO.ToDTO(saved);
    }

    public async Task<VesselVisitNotificationDTO?> Update(long id, VesselVisitNotificationDTO dto, List<string> errors)
{
    var vvn = await _repo.GetByIdAsync(id);
    if (vvn == null)
    {
        errors.Add("Vessel Visit Notification not found");
        return null;
    }

    // Somente pode ser atualizado se o status for "In Progress"
    if (vvn.Status != VvnStatus.InProgress)
    {
        errors.Add("Only notifications in 'In Progress' can be updated.");
        return null;
    }

    // US 2.2.9: IMO number cannot be changed after creation
    if (!string.IsNullOrWhiteSpace(dto.VesselImoNumber) && dto.VesselImoNumber != vvn.VesselImoNumber)
    {
        errors.Add("Vessel IMO number cannot be changed after creation.");
        return null;
    }

    // Validate crew count
    if (dto.CrewCount.HasValue && dto.CrewCount.Value < 1)
    {
        errors.Add("Crew count must be at least 1");
        return null;
    }

    // Validate preferred dock exists if provided
    if (!string.IsNullOrWhiteSpace(dto.PreferredDockCode))
    {
        var dockExists = (await _docks.GetAllAsync()).Any(d => d.Code.Equals(dto.PreferredDockCode, StringComparison.OrdinalIgnoreCase));
        if (!dockExists)
        {
            errors.Add($"Preferred dock '{dto.PreferredDockCode}' does not exist");
            return null;
        }
    }

    // Atualizar os detalhes básicos (ETA, ETD)
    vvn.UpdateDetails(dto.EstimatedTimeOfArrival, dto.EstimatedTimeOfDeparture, errors);
    if (errors.Count > 0) return null;

    // Atualizar o manifesto de carga se fornecido
    if (dto.ContainerIds != null && dto.ContainerIds.Any())
    {
        vvn.AddOrReplaceCargoManifest(dto.ContainerIds);
    }

    // Atualizar a tripulação se fornecida
    if (dto.Crew != null && dto.Crew.Any())
    {
        vvn.AddOrReplaceCrew(dto.Crew.Select(c => new CrewMember(c.Name, c.CitizenId, c.Nationality)));
    }

    // Salvar no repositório
    var updated = await _repo.Update(vvn, errors);
    if (updated == null) return null;

    // US 2.2.8: Update additional fields (stored only in DataModel)
    await _repo.UpdateAdditionalFields(id, dto.CaptainName, dto.CrewCount, 
        dto.SafetyOfficers, dto.PreferredDockCode, 
        dto.UnloadingManifest, dto.LoadingManifest);

    return VesselVisitNotificationDTO.ToDTO(updated);
}

    public async Task<IEnumerable<VesselVisitNotificationDTO>> GetFiltered(VesselVisitNotificationFilterDTO filter)
    {
        VvnStatus? status = null;
        if (!string.IsNullOrWhiteSpace(filter.Status) && Enum.TryParse<VvnStatus>(filter.Status, true, out var parsedStatus))
        {
            status = parsedStatus;
        }

        var list = await _repo.GetFilteredAsync(
            filter.VesselImoNumber,
            status,
            filter.DateFrom,
            filter.DateTo,
            filter.RepresentativeId,
            filter.OrganizationId);

        var dtos = VesselVisitNotificationDTO.ToDTO(list).ToList();
        foreach (var dto in dtos) await PopulateOrganizationAsync(dto);
        return dtos;
    }

    // US 2.2.10: View notifications submitted by representatives from same organization
    public async Task<IEnumerable<VesselVisitNotificationDTO>> GetByRepresentativeIdAsync(long representativeId)
    {
        var list = await _repo.GetByRepresentativeIdAsync(representativeId);
        var dtos = VesselVisitNotificationDTO.ToDTO(list).ToList();
        foreach (var dto in dtos) await PopulateOrganizationAsync(dto);
        return dtos;
    }

    public async Task<IEnumerable<VesselVisitNotificationDTO>> GetByOrganizationIdAsync(long organizationId)
    {
        var list = await _repo.GetByOrganizationIdAsync(organizationId);
        var dtos = VesselVisitNotificationDTO.ToDTO(list).ToList();
        foreach (var dto in dtos) await PopulateOrganizationAsync(dto);
        return dtos;
    }

    public async Task<List<DecisionLogDTO>> GetAudit(long id)
    {
        var vvn = await _repo.GetByIdAsync(id);
        if (vvn == null) return new List<DecisionLogDTO>();
        
        return vvn.DecisionLogs.Select(log => new DecisionLogDTO
        {
            OfficerId = log.OfficerId,
            Outcome = log.Outcome.ToString(),
            Timestamp = log.Timestamp,
            AssignedDockCode = log.AssignedDockCode,
            Reason = log.Reason
        }).ToList();
    }

    public async Task<bool> Delete(long id, List<string> errors)
    {
        var vvn = await _repo.GetByIdAsync(id);
        if (vvn == null)
        {
            errors.Add("Vessel Visit Notification not found");
            return false;
        }

        // Only allow deletion if status is InProgress
        if (vvn.Status != VvnStatus.InProgress)
        {
            errors.Add("Only notifications with 'InProgress' status can be deleted");
            return false;
        }

        return await _repo.Delete(id);
    }
}

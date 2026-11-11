namespace Domain.IRepository;

using Domain.Model;

public interface IVesselVisitNotificationRepository : IGenericRepository<VesselVisitNotification>
{
    Task<VesselVisitNotification?> GetByIdAsync(long id);
    new Task<VesselVisitNotification> Add(VesselVisitNotification vvn);
    Task<VesselVisitNotification?> Update(VesselVisitNotification vvn, List<string> errorMessages);
    Task<IEnumerable<VesselVisitNotification>> GetByStatusAsync(VvnStatus status);
    
    Task<IEnumerable<VesselVisitNotification>> GetFilteredAsync(
        string? vesselImoNumber = null,
        VvnStatus? status = null,
        DateTime? dateFrom = null,
        DateTime? dateTo = null,
        long? representativeId = null,
        long? organizationId = null);
    
    Task<bool> Delete(long id);
    
    // US 2.2.10: Get notifications by representative
    Task<IEnumerable<VesselVisitNotification>> GetByRepresentativeIdAsync(long representativeId);
    
    // US 2.2.10: Get notifications by organization
    Task<IEnumerable<VesselVisitNotification>> GetByOrganizationIdAsync(long organizationId);
    
    // US 2.2.8: Update additional fields stored in DataModel only
    Task UpdateAdditionalFields(long vvnId, string? captainName, int? crewCount, 
        List<string>? safetyOfficers, string? preferredDockCode, 
        object? unloadingManifest, object? loadingManifest);
    
    // US 2.2.8: Get additional fields from DataModel
    Task<(string? CaptainName, int? CrewCount, List<string>? SafetyOfficers, string? PreferredDockCode, object? UnloadingManifest, object? LoadingManifest)> GetAdditionalFieldsAsync(long id);
}

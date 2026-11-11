namespace DataModel.Repository;

using Microsoft.EntityFrameworkCore;

using DataModel.Model;

public interface IAbsanteeContext
{
	DbSet<DockDataModel> Docks { get; set; }
	DbSet<OperatingStaffDataModel> OperatingStaff { get; set; }
	DbSet<VesselDataModel> Vessels { get; set; }
	DbSet<VesselVisitNotificationDataModel> VesselVisitNotifications { get; set; }
	DbSet<PhysicalResourceDataModel> PhysicalResources { get; set; }
	DbSet<OrganizationDataModel> Organizations { get; set; }
	DbSet<RepresentativeDataModel> Representatives { get; set; }
	
	Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
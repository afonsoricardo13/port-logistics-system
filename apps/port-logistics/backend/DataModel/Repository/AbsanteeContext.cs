namespace DataModel.Repository;

using DataModel.Model;
using Domain.Model;
using Microsoft.EntityFrameworkCore;

using Microsoft.Extensions.Configuration;

public class AbsanteeContext : DbContext
{
	protected readonly IConfiguration? Configuration;

	//public AbsanteeContext() {}
	public AbsanteeContext(DbContextOptions<AbsanteeContext> options)
		: base(options)
	{
		// Database initialization is handled by the application or test setup
		// Removed Database.EnsureCreated() to avoid conflicts with integration tests
	}

    public virtual DbSet<DockDataModel> Docks { get; set; } = null!;
	public virtual DbSet<OperatingStaffDataModel> OperatingStaff { get; set; } = null!;
	public virtual DbSet<QualificationDataModel> Qualifications { get; set; } = null!;
	public virtual DbSet<VesselDataModel> Vessels { get; set; } = null!;
	public virtual DbSet<VesselVisitNotificationDataModel> VesselVisitNotifications { get; set; } = null!;
	public virtual DbSet<ContainerItemDataModel> ContainerItems { get; set; } = null!;
	public virtual DbSet<CrewMemberDataModel> CrewMembers { get; set; } = null!;
	public virtual DbSet<PhysicalResourceDataModel> PhysicalResources { get; set; } = null!;
    public virtual DbSet<VesselTypeDataModel> VesselTypes { get; set; } = null!;
	public virtual DbSet<StorageAreaDataModel> StorageAreas { get; set; } = null!;
	public virtual DbSet<StorageAreaServedDockDataModel> StorageAreaServedDocks { get; set; } = null!;
	public virtual DbSet<SystemUserDataModel> SystemUsers { get; set; } = null!;
	public virtual DbSet<OrganizationDataModel> Organizations { get; set; } = null!;
	public virtual DbSet<RepresentativeDataModel> Representatives { get; set; } = null!;
	public virtual DbSet<PrivacyPolicy> PrivacyPolicies { get; set; } = null!;
	public virtual DbSet<DataRightsRequest> DataRightsRequests { get; set; } = null!;


	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{

		// necessário se Domain.Model.Colaborator fosse usado para persistência, e se pretendesse que os atributos/propriedades fossem privadas

		// var property = typeof(Colaborator).GetProperty("Name", BindingFlags.NonPublic | BindingFlags.Instance);
		// if (property != null)
		// 	modelBuilder.Entity<Colaborator>()
		// 		.Property("_strName")
		// 		.HasColumnName("Name");

		// modelBuilder.Entity<Colaborator>()
        // 	.HasKey(c => c.Id);
			
		// modelBuilder.Entity<Colaborator>()
        // 	.HasKey(c => c.Email);
		modelBuilder.Entity<VesselVisitNotificationDataModel>()
		    .HasMany(v => v.CargoManifest)
		    .WithOne()
		    .HasForeignKey(ci => ci.VesselVisitNotificationId)
		    .OnDelete(DeleteBehavior.Cascade);

		modelBuilder.Entity<VesselVisitNotificationDataModel>()
		    .HasMany(v => v.Crew)
		    .WithOne()
		    .HasForeignKey(cm => cm.VesselVisitNotificationId)
		    .OnDelete(DeleteBehavior.Cascade);

		// Enforce business-identity uniqueness at the database level
		modelBuilder.Entity<VesselTypeDataModel>()
			.HasIndex(v => v.Name)
			.IsUnique();

		modelBuilder.Entity<DockDataModel>()
			.HasIndex(d => d.Code)
			.IsUnique();

		// Storage Areas
		modelBuilder.Entity<StorageAreaDataModel>()
			.HasIndex(s => s.Identifier)
			.IsUnique();

		modelBuilder.Entity<StorageAreaDataModel>()
			.HasMany(s => s.ServedDocks)
			.WithOne()
			.HasForeignKey(sd => sd.StorageAreaId)
			.OnDelete(DeleteBehavior.Cascade);

		// System Users - US 3.2.5
		modelBuilder.Entity<SystemUserDataModel>()
			.HasKey(u => u.Email);

		modelBuilder.Entity<SystemUserDataModel>()
			.HasIndex(u => u.Email)
			.IsUnique();

		modelBuilder.Entity<SystemUserDataModel>()
			.HasIndex(u => u.ActivationToken)
			.IsUnique()
			.HasFilter("ActivationToken IS NOT NULL");

		// Organizations and Representatives - US 2.2.5 and 2.2.6
		modelBuilder.Entity<OrganizationDataModel>()
			.HasIndex(o => o.Identifier)
			.IsUnique();

		modelBuilder.Entity<OrganizationDataModel>()
			.HasIndex(o => o.TaxNumber)
			.IsUnique();

		modelBuilder.Entity<RepresentativeDataModel>()
			.HasIndex(r => r.Email)
			.IsUnique();

		modelBuilder.Entity<RepresentativeDataModel>()
			.HasIndex(r => r.CitizenId)
			.IsUnique();

		modelBuilder.Entity<RepresentativeDataModel>()
			.HasOne(r => r.Organization)
			.WithMany(o => o.Representatives)
			.HasForeignKey(r => r.OrganizationId)
			.OnDelete(DeleteBehavior.Restrict);
	}
}
using Domain.Model;
using Microsoft.EntityFrameworkCore;

namespace DataModel;

public class OEMDbContext : DbContext
{
    public OEMDbContext(DbContextOptions<OEMDbContext> options) : base(options)
    {
    }

    public DbSet<OperationPlan> OperationPlans { get; set; }
    public DbSet<VesselVisitExecution> VesselVisitExecutions { get; set; }
    public DbSet<ExecutedOperation> ExecutedOperations { get; set; }
    public DbSet<IncidentType> IncidentTypes { get; set; }
    public DbSet<Incident> Incidents { get; set; }
    public DbSet<ComplementaryTaskCategory> ComplementaryTaskCategories { get; set; }
    public DbSet<ComplementaryTask> ComplementaryTasks { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // OperationPlan configuration
        modelBuilder.Entity<OperationPlan>(entity =>
        {
            entity.ToTable("OperationPlans");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.VvnId).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Date).IsRequired();
            entity.Property(e => e.Operations).IsRequired();
            entity.Property(e => e.AssignedResources).IsRequired();
            entity.Property(e => e.TimeWindows).IsRequired();
            entity.Property(e => e.CreatedBy).IsRequired().HasMaxLength(100);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.AlgorithmUsed).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => e.VvnId);
            entity.HasIndex(e => e.Date);
        });

        // VesselVisitExecution configuration
        modelBuilder.Entity<VesselVisitExecution>(entity =>
        {
            entity.ToTable("VesselVisitExecutions");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(50);
            entity.Property(e => e.VvnId).IsRequired().HasMaxLength(50);
            entity.Property(e => e.DockUsed).HasMaxLength(50);
            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.CreatedBy).IsRequired().HasMaxLength(100);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.HasIndex(e => e.VvnId);
            entity.HasIndex(e => e.Status);
        });

        // ExecutedOperation configuration (US 4.1.9)
        modelBuilder.Entity<ExecutedOperation>(entity =>
        {
            entity.ToTable("ExecutedOperations");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.VveId).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Type).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.UpdatedBy).IsRequired().HasMaxLength(100);
            entity.Property(e => e.UpdatedAt).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            
            entity.HasIndex(e => e.VveId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.OperationPlanId);
        });

        // IncidentType configuration
        modelBuilder.Entity<IncidentType>(entity =>
        {
            entity.ToTable("IncidentTypes");
            entity.HasKey(e => e.Code);
            entity.Property(e => e.Code).HasMaxLength(20);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Severity).IsRequired();
            entity.Property(e => e.ParentTypeCode).HasMaxLength(20);
            
            entity.HasOne(e => e.ParentType)
                .WithMany(e => e.ChildTypes)
                .HasForeignKey(e => e.ParentTypeCode)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Incident configuration
        modelBuilder.Entity<Incident>(entity =>
        {
            entity.ToTable("Incidents");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.IncidentTypeCode).IsRequired().HasMaxLength(20);
            entity.Property(e => e.StartTime).IsRequired();
            entity.Property(e => e.Severity).IsRequired();
            entity.Property(e => e.Description).IsRequired();
            entity.Property(e => e.AffectedVVEs).IsRequired();
            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.CreatedBy).IsRequired().HasMaxLength(100);
            entity.Property(e => e.CreatedAt).IsRequired();
            
            entity.HasOne(e => e.IncidentType)
                .WithMany()
                .HasForeignKey(e => e.IncidentTypeCode)
                .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.StartTime);
        });

        // ComplementaryTaskCategory configuration
        modelBuilder.Entity<ComplementaryTaskCategory>(entity =>
        {
            entity.ToTable("ComplementaryTaskCategories");
            entity.HasKey(e => e.Code);
            entity.Property(e => e.Code).HasMaxLength(20);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).IsRequired().HasMaxLength(500);
            entity.Property(e => e.ExpectedImpact).HasMaxLength(200);
        });

        // ComplementaryTask configuration
        modelBuilder.Entity<ComplementaryTask>(entity =>
        {
            entity.ToTable("ComplementaryTasks");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CategoryCode).IsRequired().HasMaxLength(20);
            entity.Property(e => e.VveId).IsRequired().HasMaxLength(50);
            entity.Property(e => e.ResponsibleTeam).IsRequired().HasMaxLength(100);
            entity.Property(e => e.StartTime).IsRequired();
            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.ImpactType).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            
            entity.HasOne(e => e.Category)
                .WithMany()
                .HasForeignKey(e => e.CategoryCode)
                .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasOne(e => e.VesselVisitExecution)
                .WithMany()
                .HasForeignKey(e => e.VveId)
                .OnDelete(DeleteBehavior.Restrict);
            
            entity.HasIndex(e => e.VveId);
            entity.HasIndex(e => e.Status);
        });
    }
}

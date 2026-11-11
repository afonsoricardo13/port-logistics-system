using System.Collections.Generic;
using System.Linq;
using Domain.Model;
using DataModel.Model;

namespace DataModel.Mapper
{
    public class PhysicalResourceMapper
    {
        public PhysicalResource ToDomain(PhysicalResourceDataModel dm)
        {
            if (dm == null) return null!;

            var qualificationIds = string.IsNullOrWhiteSpace(dm.QualificationRequirementIdsCsv)
                ? new List<string>()
                : dm.QualificationRequirementIdsCsv.Split(',').Select(s => s.Trim()).Where(s => s.Length > 0).ToList();

            Domain.Model.PhysicalResourceStatus status = Domain.Model.PhysicalResourceStatus.Active;
            if (!string.IsNullOrWhiteSpace(dm.Status) && System.Enum.TryParse<Domain.Model.PhysicalResourceStatus>(dm.Status, out var parsed))
                status = parsed;

            var domain = new PhysicalResource(dm.Code, dm.Description, dm.Type, dm.Capacity, dm.AssignedArea, status, dm.SetupTimeMinutes, qualificationIds,
                dm.OperationalWindow, dm.CapacityPerHour, dm.AverageSpeedKmH);
            // hydrate audit info
            domain.ChangedAt = dm.ChangedAt;
            domain.ChangedBy = dm.ChangedBy;
            domain.OperationType = dm.OperationType;

            return domain;
        }

        public IEnumerable<PhysicalResource> ToDomainList(IEnumerable<PhysicalResourceDataModel> list)
        {
            return list?.Select(ToDomain) ?? Enumerable.Empty<PhysicalResource>();
        }

        public PhysicalResourceDataModel ToDataModel(PhysicalResource domain)
        {
            if (domain == null) return null!;

            var dm = new PhysicalResourceDataModel(domain)
            {
                Id = domain.Id,
                QualificationRequirementIdsCsv = domain.QualificationRequirementIds != null ? string.Join(',', domain.QualificationRequirementIds) : null,
                ChangedAt = domain.ChangedAt,
                ChangedBy = domain.ChangedBy,
                OperationType = domain.OperationType
            };

            return dm;
        }

        public void UpdateDataModel(PhysicalResourceDataModel dm, PhysicalResource domain)
        {
            dm.Code = domain.Code;
            dm.Description = domain.Description;
            dm.Type = domain.Type;
            dm.Capacity = domain.Capacity;
            dm.AssignedArea = domain.AssignedArea;
            dm.Status = domain.Status.ToString();
            dm.SetupTimeMinutes = domain.SetupTimeMinutes;
            dm.QualificationRequirementIdsCsv = domain.QualificationRequirementIds != null ? string.Join(',', domain.QualificationRequirementIds) : null;
            dm.OperationalWindow = domain.OperationalWindow;
            dm.CapacityPerHour = domain.CapacityPerHour;
            dm.AverageSpeedKmH = domain.AverageSpeedKmH;
            dm.ChangedAt = domain.ChangedAt;
            dm.ChangedBy = domain.ChangedBy;
            dm.OperationType = domain.OperationType;
        }
    }
}

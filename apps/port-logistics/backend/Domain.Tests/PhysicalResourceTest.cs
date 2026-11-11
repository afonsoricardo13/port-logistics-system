using System;
using System.Collections.Generic;
using Domain.Model;
using Xunit;

namespace Domain.Tests
{
    public class PhysicalResourceTests
    {
        // Testa a criação de um PhysicalResource com parâmetros válidos
        [Fact]
        public void Create_WithValidParameters_CreatesPhysicalResource()
        {
            // Arrange
            var resource = new PhysicalResource(
                "PR-001",
                "big",
                "truck",
                50,
                "dock1"
            );

            // Assert
            Assert.Equal("PR-001", resource.Code);
            Assert.Equal("big", resource.Description);
            Assert.Equal("truck", resource.Type);
            Assert.Equal(50, resource.Capacity);
            Assert.Equal("dock1", resource.AssignedArea);
            Assert.Equal(PhysicalResourceStatus.Active, resource.Status);
        }

        // Testa a exceção quando a capacidade é negativa
        [Fact]
        public void Create_WithNegativeCapacity_ThrowsArgumentException()
        {
            // Assert
            Assert.Throws<ArgumentException>(() => new PhysicalResource(
                "PR-002",
                "small",
                "truck",
                -1,
                "dock2"
            ));
        }

        // Testa a atualização de um PhysicalResource
        [Fact]
        public void Update_WithValidParameters_UpdatesPhysicalResource()
        {
            // Arrange
            var resource = new PhysicalResource(
                "PR-003",
                "big",
                "truck",
                30
            );

            // Act
            resource.Update(
                description: "big",
                type: "Meeting truck",
                capacity: 40,
                assignedArea: "dock3",
                status: PhysicalResourceStatus.Inactive,
                setupTimeMinutes: 10,
                qualificationRequirementIds: new[] { "QUAL-001", "QUAL-002" },
                operationalWindow: null,
                capacityPerHour: null,
                averageSpeedKmH: null
            );

            // Assert
            Assert.Equal("big", resource.Description);
            Assert.Equal("Meeting truck", resource.Type);
            Assert.Equal(40, resource.Capacity);
            Assert.Equal("dock3", resource.AssignedArea);
            Assert.Equal(PhysicalResourceStatus.Inactive, resource.Status);
            Assert.Equal(10, resource.SetupTimeMinutes);
            Assert.Contains("QUAL-001", resource.QualificationRequirementIds);
        }

        // Testa a desativação de um PhysicalResource
        [Fact]
        public void Deactivate_SetsStatusToInactive()
        {
            // Arrange
            var resource = new PhysicalResource(
                "PR-004",
                "large",
                "Classtruck",
                25
            );

            // Act
            resource.Deactivate(changedBy: "admin");

            // Assert
            Assert.Equal(PhysicalResourceStatus.Inactive, resource.Status);
            Assert.NotNull(resource.ChangedAt);
            Assert.Equal("admin", resource.ChangedBy);
            Assert.Equal("Deactivate", resource.OperationType);
        }

        // Testa a reativação de um PhysicalResource
        [Fact]
        public void Reactivate_SetsStatusToActive()
        {
            // Arrange
            var resource = new PhysicalResource(
                "PR-005",
                "big",
                "truck",
                100,
                status: PhysicalResourceStatus.Inactive
            );

            // Act
            resource.Reactivate(changedBy: "admin");

            // Assert
            Assert.Equal(PhysicalResourceStatus.Active, resource.Status);
            Assert.NotNull(resource.ChangedAt);
            Assert.Equal("admin", resource.ChangedBy);
            Assert.Equal("Reactivate", resource.OperationType);
        }

        // Testa que não é possível atualizar a capacidade com valor negativo
        [Fact]
        public void Update_WithNegativeCapacity_ThrowsArgumentException()
        {
            // Arrange
            var resource = new PhysicalResource(
                "PR-006",
                "big",
                "truck",
                150
            );

            // Assert
            Assert.Throws<ArgumentException>(() => resource.Update(
                description: null,
                type: null,
                capacity: -5,
                assignedArea: null,
                status: null,
                setupTimeMinutes: null,
                qualificationRequirementIds: null,
                operationalWindow: null,
                capacityPerHour: null,
                averageSpeedKmH: null
            ));
        }
    }
}

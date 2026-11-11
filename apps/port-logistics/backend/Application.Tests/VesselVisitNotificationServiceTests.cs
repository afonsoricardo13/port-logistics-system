using Application.DTO;
using Application.Services;
using Domain.IRepository;
using Domain.Model;
using Moq;
using Xunit;

namespace Application.Tests;

/// <summary>
/// Unit tests for VesselVisitNotificationService
/// US 2.2.10: Shipping Agent Representative views status of submitted Vessel Visit Notifications
/// Tests validate service layer logic with isolated dependencies
/// </summary>
public class VesselVisitNotificationServiceTests
{
    private readonly Mock<IVesselVisitNotificationRepository> _mockVvnRepo;
    private readonly Mock<IDockRepository> _mockDockRepo;
    private readonly Mock<IVesselRepository> _mockVesselRepo;
    private readonly Mock<IRepresentativeRepository> _mockRepRepo;
    private readonly Mock<IOrganizationRepository> _mockOrgRepo;
    private readonly VesselVisitNotificationService _service;

    public VesselVisitNotificationServiceTests()
    {
        _mockVvnRepo = new Mock<IVesselVisitNotificationRepository>();
        _mockDockRepo = new Mock<IDockRepository>();
        _mockVesselRepo = new Mock<IVesselRepository>();
        _mockRepRepo = new Mock<IRepresentativeRepository>();
        _mockOrgRepo = new Mock<IOrganizationRepository>();
        
        _service = new VesselVisitNotificationService(
            _mockVvnRepo.Object,
            _mockDockRepo.Object,
            _mockVesselRepo.Object,
            _mockRepRepo.Object,
            _mockOrgRepo.Object);
    }

    #region US 2.2.10 - GetByRepresentativeIdAsync Tests

    [Fact]
    public async Task GetByRepresentativeIdAsync_WhenRepresentativeHasNotifications_ShouldReturnAll()
    {
        // Arrange
        long representativeId = 1;
        var notifications = new List<VesselVisitNotification>
        {
            CreateVvn("IMO1234567", representativeId, VvnStatus.InProgress),
            CreateVvn("IMO7654321", representativeId, VvnStatus.Submitted),
            CreateVvn("IMO1111111", representativeId, VvnStatus.Approved)
        };

        _mockVvnRepo.Setup(r => r.GetByRepresentativeIdAsync(representativeId))
            .ReturnsAsync(notifications);

        // Act
        var result = await _service.GetByRepresentativeIdAsync(representativeId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count());
        _mockVvnRepo.Verify(r => r.GetByRepresentativeIdAsync(representativeId), Times.Once);
    }

    [Fact]
    public async Task GetByRepresentativeIdAsync_WhenRepresentativeHasNoNotifications_ShouldReturnEmpty()
    {
        // Arrange
        long representativeId = 1;
        _mockVvnRepo.Setup(r => r.GetByRepresentativeIdAsync(representativeId))
            .ReturnsAsync(new List<VesselVisitNotification>());

        // Act
        var result = await _service.GetByRepresentativeIdAsync(representativeId);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
        _mockVvnRepo.Verify(r => r.GetByRepresentativeIdAsync(representativeId), Times.Once);
    }

    [Fact]
    public async Task GetByRepresentativeIdAsync_ShouldIncludeAllStatuses()
    {
        // Arrange
        long representativeId = 1;
        var notifications = new List<VesselVisitNotification>
        {
            CreateVvn("IMO1234567", representativeId, VvnStatus.InProgress),
            CreateVvn("IMO7654321", representativeId, VvnStatus.Submitted),
            CreateVvn("IMO1111111", representativeId, VvnStatus.Approved),
            CreateVvn("IMO2222222", representativeId, VvnStatus.Rejected)
        };

        _mockVvnRepo.Setup(r => r.GetByRepresentativeIdAsync(representativeId))
            .ReturnsAsync(notifications);

        // Act
        var result = await _service.GetByRepresentativeIdAsync(representativeId);

        // Assert
        var dtos = result.ToList();
        Assert.Equal(4, dtos.Count);
        Assert.Contains(dtos, d => d.Status == VvnStatus.InProgress.ToString());
        Assert.Contains(dtos, d => d.Status == VvnStatus.Submitted.ToString());
        Assert.Contains(dtos, d => d.Status == VvnStatus.Approved.ToString());
        Assert.Contains(dtos, d => d.Status == VvnStatus.Rejected.ToString());
    }

    #endregion

    #region US 2.2.10 - GetByOrganizationIdAsync Tests

    [Fact]
    public async Task GetByOrganizationIdAsync_WhenOrganizationHasNotifications_ShouldReturnAll()
    {
        // Arrange
        long organizationId = 1;
        var notifications = new List<VesselVisitNotification>
        {
            CreateVvn("IMO1234567", 1, VvnStatus.Submitted),
            CreateVvn("IMO7654321", 2, VvnStatus.Approved),
            CreateVvn("IMO1111111", 3, VvnStatus.InProgress)
        };

        _mockVvnRepo.Setup(r => r.GetByOrganizationIdAsync(organizationId))
            .ReturnsAsync(notifications);

        // Act
        var result = await _service.GetByOrganizationIdAsync(organizationId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count());
        _mockVvnRepo.Verify(r => r.GetByOrganizationIdAsync(organizationId), Times.Once);
    }

    [Fact]
    public async Task GetByOrganizationIdAsync_WhenOrganizationHasNoNotifications_ShouldReturnEmpty()
    {
        // Arrange
        long organizationId = 1;
        _mockVvnRepo.Setup(r => r.GetByOrganizationIdAsync(organizationId))
            .ReturnsAsync(new List<VesselVisitNotification>());

        // Act
        var result = await _service.GetByOrganizationIdAsync(organizationId);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
        _mockVvnRepo.Verify(r => r.GetByOrganizationIdAsync(organizationId), Times.Once);
    }

    [Fact]
    public async Task GetByOrganizationIdAsync_ShouldIncludeNotificationsFromDifferentRepresentatives()
    {
        // Arrange
        long organizationId = 1;
        var notifications = new List<VesselVisitNotification>
        {
            CreateVvn("IMO1234567", 1, VvnStatus.Submitted),
            CreateVvn("IMO7654321", 2, VvnStatus.Submitted),
            CreateVvn("IMO1111111", 3, VvnStatus.Approved)
        };

        _mockVvnRepo.Setup(r => r.GetByOrganizationIdAsync(organizationId))
            .ReturnsAsync(notifications);

        // Act
        var result = await _service.GetByOrganizationIdAsync(organizationId);

        // Assert
        var dtos = result.ToList();
        Assert.Equal(3, dtos.Count);
        Assert.Equal(3, dtos.Select(d => d.SubmittedByRepresentativeId).Distinct().Count());
    }

    #endregion

    #region US 2.2.10 - GetById with Status and Rejection Reason Tests

    [Fact]
    public async Task GetById_WhenNotificationIsInProgress_ShouldReturnCorrectStatus()
    {
        // Arrange
        var vvn = CreateVvn("IMO1234567", 1, VvnStatus.InProgress);
        _mockVvnRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(vvn);
        _mockVvnRepo.Setup(r => r.GetAdditionalFieldsAsync(1))
            .ReturnsAsync((null, null, null, null, null, null));

        // Act
        var result = await _service.GetById(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(VvnStatus.InProgress.ToString(), result.Status);
        Assert.Null(result.AssignedDockCode);
        Assert.Null(result.RejectionReason);
    }

    [Fact]
    public async Task GetById_WhenNotificationIsSubmitted_ShouldReturnCorrectStatus()
    {
        // Arrange
        var vvn = CreateVvn("IMO1234567", 1, VvnStatus.Submitted);
        _mockVvnRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(vvn);
        _mockVvnRepo.Setup(r => r.GetAdditionalFieldsAsync(1))
            .ReturnsAsync((null, null, null, null, null, null));

        // Act
        var result = await _service.GetById(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(VvnStatus.Submitted.ToString(), result.Status);
        Assert.Null(result.AssignedDockCode);
    }

    [Fact]
    public async Task GetById_WhenNotificationIsApproved_ShouldReturnStatusAndDock()
    {
        // Arrange
        var vvn = CreateVvn("IMO1234567", 1, VvnStatus.Approved);
        vvn.ForceAssignedDock("DOCK-A");
        
        _mockVvnRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(vvn);
        _mockVvnRepo.Setup(r => r.GetAdditionalFieldsAsync(1))
            .ReturnsAsync((null, null, null, null, null, null));

        // Act
        var result = await _service.GetById(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(VvnStatus.Approved.ToString(), result.Status);
        Assert.Equal("DOCK-A", result.AssignedDockCode);
        Assert.Null(result.RejectionReason);
    }

    [Fact]
    public async Task GetById_WhenNotificationIsRejected_ShouldReturnStatusAndReason()
    {
        // Arrange
        var vvn = CreateVvn("IMO1234567", 1, VvnStatus.Rejected);
        var rejectionLog = DecisionLog.Rejected("OFFICER-001", "Missing cargo manifest");
        vvn.ForceDecisionLogs(new[] { rejectionLog });
        
        _mockVvnRepo.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(vvn);
        _mockVvnRepo.Setup(r => r.GetAdditionalFieldsAsync(1))
            .ReturnsAsync((null, null, null, null, null, null));

        // Act
        var result = await _service.GetById(1);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(VvnStatus.Rejected.ToString(), result.Status);
        Assert.Equal("Missing cargo manifest", result.RejectionReason);
    }

    [Fact]
    public async Task GetById_WhenNotificationDoesNotExist_ShouldReturnNull()
    {
        // Arrange
        _mockVvnRepo.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((VesselVisitNotification?)null);

        // Act
        var result = await _service.GetById(999);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region US 2.2.10 - GetFiltered Tests (Search and Filter)

    [Fact]
    public async Task GetFiltered_ByVesselImoNumber_ShouldReturnMatchingNotifications()
    {
        // Arrange
        var filter = new VesselVisitNotificationFilterDTO { VesselImoNumber = "IMO1234567" };
        var notifications = new List<VesselVisitNotification>
        {
            CreateVvn("IMO1234567", 1, VvnStatus.Submitted)
        };

        _mockVvnRepo.Setup(r => r.GetFilteredAsync("IMO1234567", null, null, null, null, null))
            .ReturnsAsync(notifications);

        // Act
        var result = await _service.GetFiltered(filter);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("IMO1234567", result.First().VesselImoNumber);
    }

    [Fact]
    public async Task GetFiltered_ByStatus_ShouldReturnMatchingNotifications()
    {
        // Arrange
        var filter = new VesselVisitNotificationFilterDTO { Status = "Approved" };
        var notifications = new List<VesselVisitNotification>
        {
            CreateVvn("IMO1234567", 1, VvnStatus.Approved),
            CreateVvn("IMO7654321", 1, VvnStatus.Approved)
        };

        _mockVvnRepo.Setup(r => r.GetFilteredAsync(null, VvnStatus.Approved, null, null, null, null))
            .ReturnsAsync(notifications);

        // Act
        var result = await _service.GetFiltered(filter);

        // Assert
        var dtos = result.ToList();
        Assert.Equal(2, dtos.Count);
        Assert.All(dtos, d => Assert.Equal(VvnStatus.Approved.ToString(), d.Status));
    }

    [Fact]
    public async Task GetFiltered_ByRepresentativeId_ShouldReturnMatchingNotifications()
    {
        // Arrange
        var filter = new VesselVisitNotificationFilterDTO { RepresentativeId = 1 };
        var notifications = new List<VesselVisitNotification>
        {
            CreateVvn("IMO1234567", 1, VvnStatus.Submitted),
            CreateVvn("IMO7654321", 1, VvnStatus.Approved)
        };

        _mockVvnRepo.Setup(r => r.GetFilteredAsync(null, null, null, null, 1, null))
            .ReturnsAsync(notifications);

        // Act
        var result = await _service.GetFiltered(filter);

        // Assert
        var dtos = result.ToList();
        Assert.Equal(2, dtos.Count);
        Assert.All(dtos, d => Assert.Equal(1, d.SubmittedByRepresentativeId));
    }

    [Fact]
    public async Task GetFiltered_ByDateRange_ShouldReturnMatchingNotifications()
    {
        // Arrange
        var dateFrom = new DateTime(2025, 1, 1);
        var dateTo = new DateTime(2025, 12, 31);
        var filter = new VesselVisitNotificationFilterDTO { DateFrom = dateFrom, DateTo = dateTo };
        
        var notifications = new List<VesselVisitNotification>
        {
            CreateVvn("IMO1234567", 1, VvnStatus.Submitted, new DateTime(2025, 6, 15))
        };

        _mockVvnRepo.Setup(r => r.GetFilteredAsync(null, null, dateFrom, dateTo, null, null))
            .ReturnsAsync(notifications);

        // Act
        var result = await _service.GetFiltered(filter);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
    }

    [Fact]
    public async Task GetFiltered_WithMultipleFilters_ShouldReturnMatchingNotifications()
    {
        // Arrange
        var filter = new VesselVisitNotificationFilterDTO 
        { 
            VesselImoNumber = "IMO1234567",
            Status = "Submitted",
            RepresentativeId = 1
        };
        
        var notifications = new List<VesselVisitNotification>
        {
            CreateVvn("IMO1234567", 1, VvnStatus.Submitted)
        };

        _mockVvnRepo.Setup(r => r.GetFilteredAsync("IMO1234567", VvnStatus.Submitted, null, null, 1, null))
            .ReturnsAsync(notifications);

        // Act
        var result = await _service.GetFiltered(filter);

        // Assert
        var dto = result.Single();
        Assert.Equal("IMO1234567", dto.VesselImoNumber);
        Assert.Equal(VvnStatus.Submitted.ToString(), dto.Status);
        Assert.Equal(1, dto.SubmittedByRepresentativeId);
    }

    [Fact]
    public async Task GetFiltered_WithNoMatches_ShouldReturnEmpty()
    {
        // Arrange
        var filter = new VesselVisitNotificationFilterDTO { VesselImoNumber = "IMO9999999" };
        _mockVvnRepo.Setup(r => r.GetFilteredAsync("IMO9999999", null, null, null, null, null))
            .ReturnsAsync(new List<VesselVisitNotification>());

        // Act
        var result = await _service.GetFiltered(filter);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetFiltered_WithInvalidStatus_ShouldIgnoreStatusFilter()
    {
        // Arrange
        var filter = new VesselVisitNotificationFilterDTO { Status = "InvalidStatus" };
        var notifications = new List<VesselVisitNotification>
        {
            CreateVvn("IMO1234567", 1, VvnStatus.Submitted)
        };

        _mockVvnRepo.Setup(r => r.GetFilteredAsync(null, null, null, null, null, null))
            .ReturnsAsync(notifications);

        // Act
        var result = await _service.GetFiltered(filter);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
    }

    #endregion

    #region US 2.2.7 - Approve Tests

    [Fact]
    public async Task Approve_WithValidData_ShouldApproveNotification()
    {
        // Arrange
        long id = 1;
        string officerId = "officer@port.com";
        var dto = new ApproveNotificationDTO { AssignedDockCode = "DOCK001" };
        var vvn = CreateVvn("IMO1234567", 1, VvnStatus.Submitted);
        var docks = new List<Dock> { new Dock("DOCK001", "Main Dock", "North Port", 100.0, 15.0, 12.0, new List<string> { "Container Ship" }) };

        _mockVvnRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(vvn);
        _mockDockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(docks);
        _mockVvnRepo.Setup(r => r.Update(It.IsAny<VesselVisitNotification>(), It.IsAny<List<string>>()))
            .ReturnsAsync(vvn);

        var errors = new List<string>();

        // Act
        var result = await _service.Approve(id, officerId, dto, errors);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(errors);
        Assert.Equal("DOCK001", result.AssignedDockCode);
        _mockVvnRepo.Verify(r => r.Update(It.IsAny<VesselVisitNotification>(), It.IsAny<List<string>>()), Times.Once);
    }

    [Fact]
    public async Task Approve_WhenNotificationNotFound_ShouldReturnNullAndAddError()
    {
        // Arrange
        long id = 999;
        string officerId = "officer@port.com";
        var dto = new ApproveNotificationDTO { AssignedDockCode = "DOCK001" };

        _mockVvnRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((VesselVisitNotification?)null);

        var errors = new List<string>();

        // Act
        var result = await _service.Approve(id, officerId, dto, errors);

        // Assert
        Assert.Null(result);
        Assert.Single(errors);
        Assert.Contains("not found", errors[0]);
        _mockVvnRepo.Verify(r => r.Update(It.IsAny<VesselVisitNotification>(), It.IsAny<List<string>>()), Times.Never);
    }

    [Fact]
    public async Task Approve_WithInvalidDock_ShouldReturnNullAndAddError()
    {
        // Arrange
        long id = 1;
        string officerId = "officer@port.com";
        var dto = new ApproveNotificationDTO { AssignedDockCode = "INVALID_DOCK" };
        var vvn = CreateVvn("IMO1234567", 1, VvnStatus.Submitted);
        var docks = new List<Dock> { new Dock("DOCK001", "Main Dock", "North Port", 100.0, 15.0, 12.0, new List<string> { "Container Ship" }) };

        _mockVvnRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(vvn);
        _mockDockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(docks);

        var errors = new List<string>();

        // Act
        var result = await _service.Approve(id, officerId, dto, errors);

        // Assert
        Assert.Null(result);
        Assert.Single(errors);
        Assert.Contains("dock does not exist", errors[0]);
        _mockVvnRepo.Verify(r => r.Update(It.IsAny<VesselVisitNotification>(), It.IsAny<List<string>>()), Times.Never);
    }

    [Fact]
    public async Task Approve_WhenNotInSubmittedStatus_ShouldReturnNullAndAddError()
    {
        // Arrange
        long id = 1;
        string officerId = "officer@port.com";
        var dto = new ApproveNotificationDTO { AssignedDockCode = "DOCK001" };
        var vvn = CreateVvn("IMO1234567", 1, VvnStatus.Approved); // Already approved
        var docks = new List<Dock> { new Dock("DOCK001", "Main Dock", "North Port", 100.0, 15.0, 12.0, new List<string> { "Container Ship" }) };

        _mockVvnRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(vvn);
        _mockDockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(docks);

        var errors = new List<string>();

        // Act
        var result = await _service.Approve(id, officerId, dto, errors);

        // Assert
        Assert.Null(result);
        Assert.NotEmpty(errors);
        _mockVvnRepo.Verify(r => r.Update(It.IsAny<VesselVisitNotification>(), It.IsAny<List<string>>()), Times.Never);
    }

    [Fact]
    public async Task Approve_WhenRepositoryUpdateFails_ShouldReturnNull()
    {
        // Arrange
        long id = 1;
        string officerId = "officer@port.com";
        var dto = new ApproveNotificationDTO { AssignedDockCode = "DOCK001" };
        var vvn = CreateVvn("IMO1234567", 1, VvnStatus.Submitted);
        var docks = new List<Dock> { new Dock("DOCK001", "Main Dock", "North Port", 100.0, 15.0, 12.0, new List<string> { "Container Ship" }) };

        _mockVvnRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(vvn);
        _mockDockRepo.Setup(r => r.GetAllAsync()).ReturnsAsync(docks);
        _mockVvnRepo.Setup(r => r.Update(It.IsAny<VesselVisitNotification>(), It.IsAny<List<string>>()))
            .ReturnsAsync((VesselVisitNotification?)null);

        var errors = new List<string>();

        // Act
        var result = await _service.Approve(id, officerId, dto, errors);

        // Assert
        Assert.Null(result);
    }

    #endregion

    #region US 2.2.7 - Reject Tests

    [Fact]
    public async Task Reject_WithValidData_ShouldRejectNotification()
    {
        // Arrange
        long id = 1;
        string officerId = "officer@port.com";
        var dto = new RejectNotificationDTO { Reason = "Safety concerns identified during review process" };
        var vvn = CreateVvn("IMO1234567", 1, VvnStatus.Submitted);

        _mockVvnRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(vvn);
        _mockVvnRepo.Setup(r => r.Update(It.IsAny<VesselVisitNotification>(), It.IsAny<List<string>>()))
            .Callback<VesselVisitNotification, List<string>>((v, e) => 
            {
                // The Reject method modifies the vvn in place
            })
            .ReturnsAsync((VesselVisitNotification v, List<string> e) => v);

        var errors = new List<string>();

        // Act
        var result = await _service.Reject(id, officerId, dto, errors);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(errors);
        // The rejection reason is populated from DecisionLogs after the domain method is called
        _mockVvnRepo.Verify(r => r.Update(It.IsAny<VesselVisitNotification>(), It.IsAny<List<string>>()), Times.Once);
    }

    [Fact]
    public async Task Reject_WhenNotificationNotFound_ShouldReturnNullAndAddError()
    {
        // Arrange
        long id = 999;
        string officerId = "officer@port.com";
        var dto = new RejectNotificationDTO { Reason = "Test rejection reason" };

        _mockVvnRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync((VesselVisitNotification?)null);

        var errors = new List<string>();

        // Act
        var result = await _service.Reject(id, officerId, dto, errors);

        // Assert
        Assert.Null(result);
        Assert.Single(errors);
        Assert.Contains("not found", errors[0]);
        _mockVvnRepo.Verify(r => r.Update(It.IsAny<VesselVisitNotification>(), It.IsAny<List<string>>()), Times.Never);
    }

    [Fact]
    public async Task Reject_WithShortReason_ShouldStillReject()
    {
        // Arrange
        long id = 1;
        string officerId = "officer@port.com";
        var dto = new RejectNotificationDTO { Reason = "Short" }; // Less than 10 characters but still valid in domain
        var vvn = CreateVvn("IMO1234567", 1, VvnStatus.Submitted);

        _mockVvnRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(vvn);
        _mockVvnRepo.Setup(r => r.Update(It.IsAny<VesselVisitNotification>(), It.IsAny<List<string>>()))
            .ReturnsAsync(vvn);

        var errors = new List<string>();

        // Act
        var result = await _service.Reject(id, officerId, dto, errors);

        // Assert - Domain doesn't validate minimum length, only that reason is not empty
        Assert.NotNull(result);
        Assert.Empty(errors);
        _mockVvnRepo.Verify(r => r.Update(It.IsAny<VesselVisitNotification>(), It.IsAny<List<string>>()), Times.Once);
    }

    [Fact]
    public async Task Reject_WhenNotInSubmittedStatus_ShouldReturnNullAndAddError()
    {
        // Arrange
        long id = 1;
        string officerId = "officer@port.com";
        var dto = new RejectNotificationDTO { Reason = "Valid rejection reason here" };
        var vvn = CreateVvn("IMO1234567", 1, VvnStatus.InProgress); // Not submitted

        _mockVvnRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(vvn);

        var errors = new List<string>();

        // Act
        var result = await _service.Reject(id, officerId, dto, errors);

        // Assert
        Assert.Null(result);
        Assert.NotEmpty(errors);
        _mockVvnRepo.Verify(r => r.Update(It.IsAny<VesselVisitNotification>(), It.IsAny<List<string>>()), Times.Never);
    }

    [Fact]
    public async Task Reject_WhenRepositoryUpdateFails_ShouldReturnNull()
    {
        // Arrange
        long id = 1;
        string officerId = "officer@port.com";
        var dto = new RejectNotificationDTO { Reason = "Valid rejection reason here" };
        var vvn = CreateVvn("IMO1234567", 1, VvnStatus.Submitted);

        _mockVvnRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(vvn);
        _mockVvnRepo.Setup(r => r.Update(It.IsAny<VesselVisitNotification>(), It.IsAny<List<string>>()))
            .ReturnsAsync((VesselVisitNotification?)null);

        var errors = new List<string>();

        // Act
        var result = await _service.Reject(id, officerId, dto, errors);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task Reject_WithEmptyReason_ShouldReturnNullAndAddError()
    {
        // Arrange
        long id = 1;
        string officerId = "officer@port.com";
        var dto = new RejectNotificationDTO { Reason = "" };
        var vvn = CreateVvn("IMO1234567", 1, VvnStatus.Submitted);

        _mockVvnRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(vvn);
        _mockVvnRepo.Setup(r => r.Update(It.IsAny<VesselVisitNotification>(), It.IsAny<List<string>>()))
            .ReturnsAsync((VesselVisitNotification?)null);

        var errors = new List<string>();

        // Act
        var result = await _service.Reject(id, officerId, dto, errors);

        // Assert
        Assert.Null(result);
        Assert.NotEmpty(errors);
        _mockVvnRepo.Verify(r => r.Update(It.IsAny<VesselVisitNotification>(), It.IsAny<List<string>>()), Times.Never);
    }

    [Fact]
    public async Task Reject_WithValidLongReason_ShouldRejectNotification()
    {
        // Arrange
        long id = 1;
        string officerId = "officer@port.com";
        var dto = new RejectNotificationDTO 
        { 
            Reason = "This is a very detailed rejection reason explaining all the safety and regulatory concerns that were identified during the thorough review process of this vessel visit notification." 
        };
        var vvn = CreateVvn("IMO1234567", 1, VvnStatus.Submitted);

        _mockVvnRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(vvn);
        _mockVvnRepo.Setup(r => r.Update(It.IsAny<VesselVisitNotification>(), It.IsAny<List<string>>()))
            .ReturnsAsync(vvn);

        var errors = new List<string>();

        // Act
        var result = await _service.Reject(id, officerId, dto, errors);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(errors);
    }

    #endregion

    #region Helper Methods

    private VesselVisitNotification CreateVvn(string imoNumber, long representativeId, VvnStatus status, DateTime? eta = null)
    {
        var vvn = new VesselVisitNotification(
            imoNumber,
            eta ?? DateTime.UtcNow.AddDays(7),
            DateTime.UtcNow.AddDays(8),
            new List<string> { "ABCD1234567" },
            new List<CrewMember> { new CrewMember("Captain", "ID123", "PT") },
            representativeId);

        vvn.ForceStatus(status);
        return vvn;
    }

    #endregion
}

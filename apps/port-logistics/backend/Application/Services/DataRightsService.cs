using Application.DTO;
using DataModel.Model;
using DataModel.Repository;
using Domain.IRepository;
using Domain.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Application.Services
{
    /// <summary>
    /// Service interface for managing GDPR data rights requests.
    /// US 4.5.3 - Data Rights for System Users
    /// </summary>
    public interface IDataRightsService
    {
        Task<DataRightsRequestDTO> CreateAccessRequestAsync(string userEmail, CreateDataAccessRequestDTO dto);
        Task<DataRightsRequestDTO> CreateRectificationRequestAsync(string userEmail, CreateDataRectificationRequestDTO dto);
        Task<DataRightsRequestDTO> CreateDeletionRequestAsync(string userEmail, CreateDataDeletionRequestDTO dto);
        Task<IEnumerable<DataRightsRequestDTO>> GetUserRequestsAsync(string userEmail);
        Task<IEnumerable<DataRightsRequestDTO>> GetAllRequestsAsync();
        Task<IEnumerable<DataRightsRequestDTO>> GetPendingRequestsAsync();
        Task<DataRightsRequestDTO?> GetRequestByIdAsync(int id);
        Task<DataRightsRequestDTO> ProcessRequestAsync(string adminEmail, ProcessDataRightsRequestDTO dto);
        Task<UserDataExportDTO> ExportUserDataAsync(string userEmail, string format);
    }

    /// <summary>
    /// Service implementation for GDPR data rights management.
    /// US 4.5.3 - Data Rights for System Users
    /// </summary>
    public class DataRightsService : IDataRightsService
    {
        private readonly IDataRightsRequestRepository _repository;
        private readonly AbsanteeContext _context;
        private readonly ILogger<DataRightsService> _logger;

        public DataRightsService(
            IDataRightsRequestRepository repository,
            AbsanteeContext context,
            ILogger<DataRightsService> logger)
        {
            _repository = repository;
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Creates a data access request (export personal data).
        /// </summary>
        public async Task<DataRightsRequestDTO> CreateAccessRequestAsync(string userEmail, CreateDataAccessRequestDTO dto)
        {
            var user = await GetUserAsync(userEmail);
            if (user == null)
            {
                throw new InvalidOperationException($"User with email {userEmail} not found");
            }

            var request = new DataRightsRequest
            {
                UserEmail = userEmail,
                RequestType = 0, // Access
                Status = 0, // Pending
                ExportFormat = dto.PreferredFormat ?? "json",
                CreatedAt = DateTime.UtcNow
            };

            var created = await _repository.CreateAsync(request);
            _logger.LogInformation("Data access request created for user {Email}, ID: {Id}", userEmail, created.Id);
            
            return MapToDTO(created);
        }

        /// <summary>
        /// Creates a data rectification request (correct personal data).
        /// </summary>
        public async Task<DataRightsRequestDTO> CreateRectificationRequestAsync(string userEmail, CreateDataRectificationRequestDTO dto)
        {
            var user = await GetUserAsync(userEmail);
            if (user == null)
            {
                throw new InvalidOperationException($"User with email {userEmail} not found");
            }

            if (dto.FieldsToRectify == null || !dto.FieldsToRectify.Any())
            {
                throw new InvalidOperationException("At least one field to rectify must be specified");
            }

            var request = new DataRightsRequest
            {
                UserEmail = userEmail,
                RequestType = 1, // Rectification
                Status = 0, // Pending
                RequestDetails = dto.Reason,
                ProposedChanges = JsonSerializer.Serialize(dto.FieldsToRectify),
                CreatedAt = DateTime.UtcNow
            };

            var created = await _repository.CreateAsync(request);
            _logger.LogInformation("Data rectification request created for user {Email}, ID: {Id}", userEmail, created.Id);
            
            return MapToDTO(created);
        }

        /// <summary>
        /// Creates a data deletion request (right to be forgotten).
        /// </summary>
        public async Task<DataRightsRequestDTO> CreateDeletionRequestAsync(string userEmail, CreateDataDeletionRequestDTO dto)
        {
            var user = await GetUserAsync(userEmail);
            if (user == null)
            {
                throw new InvalidOperationException($"User with email {userEmail} not found");
            }

            var request = new DataRightsRequest
            {
                UserEmail = userEmail,
                RequestType = 2, // Deletion
                Status = 0, // Pending
                RequestDetails = dto.Reason,
                CreatedAt = DateTime.UtcNow
            };

            var created = await _repository.CreateAsync(request);
            _logger.LogInformation("Data deletion request created for user {Email}, ID: {Id}", userEmail, created.Id);
            
            return MapToDTO(created);
        }

        /// <summary>
        /// Gets all requests for a specific user.
        /// </summary>
        public async Task<IEnumerable<DataRightsRequestDTO>> GetUserRequestsAsync(string userEmail)
        {
            var requests = await _repository.GetByUserEmailAsync(userEmail);
            return requests.Select(MapToDTO);
        }

        /// <summary>
        /// Gets all requests (admin only).
        /// </summary>
        public async Task<IEnumerable<DataRightsRequestDTO>> GetAllRequestsAsync()
        {
            var requests = await _repository.GetAllAsync();
            return requests.Select(MapToDTO);
        }

        /// <summary>
        /// Gets pending requests (admin only).
        /// </summary>
        public async Task<IEnumerable<DataRightsRequestDTO>> GetPendingRequestsAsync()
        {
            var requests = await _repository.GetPendingAsync();
            return requests.Select(MapToDTO);
        }

        /// <summary>
        /// Gets a specific request by ID.
        /// </summary>
        public async Task<DataRightsRequestDTO?> GetRequestByIdAsync(int id)
        {
            var request = await _repository.GetByIdAsync(id);
            return request != null ? MapToDTO(request) : null;
        }

        /// <summary>
        /// Processes a data rights request (admin action).
        /// </summary>
        public async Task<DataRightsRequestDTO> ProcessRequestAsync(string adminEmail, ProcessDataRightsRequestDTO dto)
        {
            var request = await _repository.GetByIdAsync(dto.RequestId);
            if (request == null)
            {
                throw new InvalidOperationException($"Request with ID {dto.RequestId} not found");
            }

            if (request.Status == 2 || request.Status == 3) // Already Completed or Rejected
            {
                throw new InvalidOperationException("This request has already been processed");
            }

            if (dto.NewStatus < 1 || dto.NewStatus > 3)
            {
                throw new InvalidOperationException("Invalid status. Use 1=InProgress, 2=Completed, 3=Rejected");
            }

            request.Status = dto.NewStatus;
            request.ProcessedAt = DateTime.UtcNow;
            request.ProcessedBy = adminEmail;
            request.AdminResponse = dto.AdminResponse;

            var updated = await _repository.UpdateAsync(request);
            _logger.LogInformation("Data rights request {Id} processed by {Admin}, new status: {Status}", 
                request.Id, adminEmail, dto.NewStatus);
            
            return MapToDTO(updated);
        }

        /// <summary>
        /// Exports user data for GDPR data portability.
        /// </summary>
        public async Task<UserDataExportDTO> ExportUserDataAsync(string userEmail, string format)
        {
            var user = await GetUserAsync(userEmail);
            if (user == null)
            {
                throw new InvalidOperationException($"User with email {userEmail} not found");
            }

            var requests = await _repository.GetByUserEmailAsync(userEmail);

            var export = new UserDataExportDTO
            {
                Email = user.Email,
                Name = user.Name,
                Role = user.Role,
                CreatedAt = user.CreatedAt,
                LastLoginAt = null, // Not tracked in current system
                ExportGeneratedAt = DateTime.UtcNow,
                ExportFormat = format,
                DataRightsRequestHistory = requests.Select(MapToDTO).ToList()
            };

            _logger.LogInformation("Data export generated for user {Email} in format {Format}", userEmail, format);
            
            return export;
        }

        /// <summary>
        /// Gets user information from the database.
        /// </summary>
        private async Task<SystemUserDataModel?> GetUserAsync(string userEmail)
        {
            return await _context.SystemUsers
                .FirstOrDefaultAsync(u => u.Email.ToLower() == userEmail.ToLower());
        }

        /// <summary>
        /// Maps a domain entity to DTO.
        /// </summary>
        private static DataRightsRequestDTO MapToDTO(DataRightsRequest request)
        {
            return new DataRightsRequestDTO
            {
                Id = request.Id,
                UserEmail = request.UserEmail,
                RequestType = request.RequestType,
                Status = request.Status,
                RequestDetails = request.RequestDetails,
                ProposedChanges = request.ProposedChanges,
                ExportFormat = request.ExportFormat,
                ExportFilePath = request.ExportFilePath,
                CreatedAt = request.CreatedAt,
                ProcessedAt = request.ProcessedAt,
                ProcessedBy = request.ProcessedBy,
                AdminResponse = request.AdminResponse
            };
        }
    }
}

using Application.DTO;
using Domain.IRepository;
using Domain.Model;

namespace Application.Services;

public class ComplementaryTaskService
{
    private readonly IComplementaryTaskRepository _taskRepository;
    private readonly IComplementaryTaskCategoryRepository _categoryRepository;
    private readonly IVesselVisitExecutionRepository _vveRepository;

    public ComplementaryTaskService(
        IComplementaryTaskRepository taskRepository,
        IComplementaryTaskCategoryRepository categoryRepository,
        IVesselVisitExecutionRepository vveRepository)
    {
        _taskRepository = taskRepository;
        _categoryRepository = categoryRepository;
        _vveRepository = vveRepository;
    }

    /// <summary>
    /// Get all complementary tasks with optional filtering
    /// </summary>
    public async Task<IEnumerable<ComplementaryTaskDTO>> GetAllAsync(ComplementaryTaskFilterDTO? filter = null)
    {
        IEnumerable<ComplementaryTask> tasks;

        // Apply primary filter
        if (filter?.OngoingOnly == true)
        {
            tasks = await _taskRepository.GetOngoingTasksAsync();
        }
        else if (filter?.Status != null && Enum.TryParse<ComplementaryTaskStatus>(filter.Status, true, out var status))
        {
            tasks = await _taskRepository.GetByStatusAsync(status);
        }
        else if (!string.IsNullOrEmpty(filter?.VveId))
        {
            tasks = await _taskRepository.GetByVveIdAsync(filter.VveId);
        }
        else if (filter?.StartDate.HasValue == true && filter?.EndDate.HasValue == true)
        {
            tasks = await _taskRepository.GetByDateRangeAsync(filter.StartDate.Value, filter.EndDate.Value);
        }
        else
        {
            tasks = await _taskRepository.GetAllAsync();
        }

        // Additional client-side filtering
        if (filter != null)
        {
            if (filter.ImpactType != null && Enum.TryParse<TaskImpactType>(filter.ImpactType, true, out var impact))
                tasks = tasks.Where(t => t.ImpactType == impact);

            if (filter.StartDate.HasValue)
                tasks = tasks.Where(t => t.StartTime >= filter.StartDate.Value);

            if (filter.EndDate.HasValue)
                tasks = tasks.Where(t => t.StartTime <= filter.EndDate.Value);
        }

        return tasks.Select(MapToDTO);
    }

    /// <summary>
    /// Get complementary task by ID
    /// </summary>
    public async Task<ComplementaryTaskDTO?> GetByIdAsync(Guid id)
    {
        var task = await _taskRepository.GetByIdAsync(id);
        return task == null ? null : MapToDTO(task);
    }

    /// <summary>
    /// Get ongoing tasks that are currently impacting operations
    /// </summary>
    public async Task<IEnumerable<ComplementaryTaskDTO>> GetOngoingTasksAsync()
    {
        var tasks = await _taskRepository.GetOngoingTasksAsync();
        return tasks.Select(MapToDTO);
    }

    /// <summary>
    /// Get tasks by VVE ID
    /// </summary>
    public async Task<IEnumerable<ComplementaryTaskDTO>> GetByVveIdAsync(string vveId)
    {
        var tasks = await _taskRepository.GetByVveIdAsync(vveId);
        return tasks.Select(MapToDTO);
    }

    /// <summary>
    /// Create a new complementary task
    /// </summary>
    public async Task<ComplementaryTaskDTO> CreateAsync(CreateComplementaryTaskDTO dto)
    {
        // Validate category exists
        if (!await _categoryRepository.ExistsAsync(dto.CategoryCode))
            throw new InvalidOperationException($"Complementary task category with code {dto.CategoryCode} not found");

        // Validate VVE exists
        var vve = await _vveRepository.GetByIdAsync(dto.VveId);
        if (vve == null)
            throw new InvalidOperationException($"Vessel visit execution with ID {dto.VveId} not found");

        // Validate VVE is in progress
        if (vve.Status != VesselVisitExecutionStatus.InProgress)
            throw new InvalidOperationException($"Cannot add complementary task to VVE {dto.VveId}. VVE status must be 'InProgress', current status is '{vve.Status}'");

        // Parse and validate impact type
        if (!Enum.TryParse<TaskImpactType>(dto.ImpactType, true, out var impactType))
            throw new ArgumentException($"Invalid impact type: {dto.ImpactType}. Valid values: Parallel, Blocking");

        var task = new ComplementaryTask
        {
            Id = Guid.NewGuid(),
            CategoryCode = dto.CategoryCode,
            VveId = dto.VveId,
            ResponsibleTeam = dto.ResponsibleTeam,
            StartTime = dto.StartTime,
            Status = ComplementaryTaskStatus.Ongoing,
            ImpactType = impactType,
            Notes = dto.Notes
        };

        var created = await _taskRepository.AddAsync(task);
        
        // Reload to get navigation properties
        var result = await _taskRepository.GetByIdAsync(created.Id);
        return MapToDTO(result!);
    }

    /// <summary>
    /// Update an existing complementary task
    /// </summary>
    public async Task<ComplementaryTaskDTO> UpdateAsync(Guid id, UpdateComplementaryTaskDTO dto)
    {
        var task = await _taskRepository.GetByIdAsync(id);
        if (task == null)
            throw new InvalidOperationException($"Complementary task with ID {id} not found");

        // Validate category if changed
        if (!string.IsNullOrEmpty(dto.CategoryCode) && dto.CategoryCode != task.CategoryCode)
        {
            if (!await _categoryRepository.ExistsAsync(dto.CategoryCode))
                throw new InvalidOperationException($"Complementary task category with code {dto.CategoryCode} not found");
            task.CategoryCode = dto.CategoryCode;
        }

        // Update fields if provided
        if (!string.IsNullOrEmpty(dto.ResponsibleTeam))
            task.ResponsibleTeam = dto.ResponsibleTeam;

        if (dto.StartTime.HasValue)
            task.StartTime = dto.StartTime.Value;

        if (dto.EndTime.HasValue)
        {
            task.EndTime = dto.EndTime.Value;
            
            // Validate end time is after start time
            if (task.EndTime.Value < task.StartTime)
                throw new ArgumentException("End time must be after start time");
        }

        if (!string.IsNullOrEmpty(dto.Status))
        {
            if (!Enum.TryParse<ComplementaryTaskStatus>(dto.Status, true, out var status))
                throw new ArgumentException($"Invalid status: {dto.Status}. Valid values: Ongoing, Completed");
            
            task.Status = status;
            
            // Auto-set end time if completing without explicit end time
            if (status == ComplementaryTaskStatus.Completed && !task.EndTime.HasValue && !dto.EndTime.HasValue)
                task.EndTime = DateTime.UtcNow;
        }

        if (!string.IsNullOrEmpty(dto.ImpactType))
        {
            if (!Enum.TryParse<TaskImpactType>(dto.ImpactType, true, out var impactType))
                throw new ArgumentException($"Invalid impact type: {dto.ImpactType}. Valid values: Parallel, Blocking");
            task.ImpactType = impactType;
        }

        if (dto.Notes != null)
            task.Notes = dto.Notes;

        var updated = await _taskRepository.UpdateAsync(task);
        
        // Reload to get navigation properties
        var result = await _taskRepository.GetByIdAsync(updated.Id);
        return MapToDTO(result!);
    }

    /// <summary>
    /// Delete a complementary task
    /// </summary>
    public async Task<bool> DeleteAsync(Guid id)
    {
        if (!await _taskRepository.ExistsAsync(id))
            throw new InvalidOperationException($"Complementary task with ID {id} not found");

        return await _taskRepository.DeleteAsync(id);
    }

    /// <summary>
    /// Map domain model to DTO
    /// </summary>
    private ComplementaryTaskDTO MapToDTO(ComplementaryTask task)
    {
        return new ComplementaryTaskDTO
        {
            Id = task.Id,
            CategoryCode = task.CategoryCode,
            CategoryName = task.Category?.Name ?? string.Empty,
            VveId = task.VveId,
            VesselName = string.Empty, // VesselName not stored in VVE, would need to fetch from Port Logistics API
            ResponsibleTeam = task.ResponsibleTeam,
            StartTime = task.StartTime,
            EndTime = task.EndTime,
            Status = task.Status.ToString(),
            ImpactType = task.ImpactType.ToString(),
            Notes = task.Notes,
            CreatedAt = task.CreatedAt,
            UpdatedAt = task.UpdatedAt
        };
    }
}

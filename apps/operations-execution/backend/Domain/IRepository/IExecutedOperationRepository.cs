using Domain.Model;

namespace Domain.IRepository;

/// <summary>
/// US 4.1.9: Repository interface for ExecutedOperation entity
/// </summary>
public interface IExecutedOperationRepository
{
    /// <summary>
    /// Get executed operation by ID
    /// </summary>
    Task<ExecutedOperation?> GetByIdAsync(Guid id);
    
    /// <summary>
    /// Get all executed operations for a specific VVE
    /// </summary>
    Task<IEnumerable<ExecutedOperation>> GetByVveIdAsync(string vveId);
    
    /// <summary>
    /// Get executed operations by status
    /// </summary>
    Task<IEnumerable<ExecutedOperation>> GetByStatusAsync(ExecutionStatus status);
    
    /// <summary>
    /// Get executed operations for a specific operation plan
    /// </summary>
    Task<IEnumerable<ExecutedOperation>> GetByOperationPlanIdAsync(Guid operationPlanId);
    
    /// <summary>
    /// Add a new executed operation
    /// </summary>
    Task<ExecutedOperation> AddAsync(ExecutedOperation executedOperation);
    
    /// <summary>
    /// Update an existing executed operation
    /// </summary>
    Task<ExecutedOperation> UpdateAsync(ExecutedOperation executedOperation);
    
    /// <summary>
    /// Delete an executed operation
    /// </summary>
    Task<bool> DeleteAsync(Guid id);
    
    /// <summary>
    /// Check if an executed operation exists
    /// </summary>
    Task<bool> ExistsAsync(Guid id);
    
    /// <summary>
    /// Get all executed operations
    /// </summary>
    Task<IEnumerable<ExecutedOperation>> GetAllAsync();
}

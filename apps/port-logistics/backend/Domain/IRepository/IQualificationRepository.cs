namespace Domain.IRepository;

using Domain.Model;

/// <summary>
/// Repository interface for Qualification entity
/// US 2.2.13: Register and manage qualifications (create, update)
/// </summary>
public interface IQualificationRepository : IGenericRepository<Qualification>
{
    /// <summary>
    /// Gets all qualifications
    /// </summary>
    Task<List<Qualification>> GetAllAsync();

    /// <summary>
    /// Adds a new qualification
    /// </summary>
    Task<Qualification> AddAsync(Qualification qualification);

    /// <summary>
    /// Updates an existing qualification
    /// </summary>
    Task<Qualification?> UpdateAsync(Qualification qualification);

    /// <summary>
    /// Gets a qualification by its name (unique identifier)
    /// </summary>
    Task<Qualification?> GetByQualificationNameAsync(string qualificationName);

    /// <summary>
    /// Searches qualifications by name pattern (partial match, case-insensitive)
    /// </summary>
    Task<List<Qualification>> SearchAsync(string? namePattern = null);

    /// <summary>
    /// Checks if a qualification with the given name already exists
    /// </summary>
    Task<bool> ExistsByQualificationNameAsync(string qualificationName);
}

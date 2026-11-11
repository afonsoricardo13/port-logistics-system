namespace Domain.IRepository;

using Domain.Model;

public interface IPhysicalResourceRepository : IGenericRepository<PhysicalResource>
{
    Task<IEnumerable<PhysicalResource>> GetAllAsync();
    Task<PhysicalResource?> GetByCodeAsync(string code);
    new Task<PhysicalResource> Add(PhysicalResource resource);
    Task<PhysicalResource?> Update(PhysicalResource resource, List<string> errorMessages);
    Task<bool> Exists(string code);

    Task<IEnumerable<PhysicalResource>> SearchAsync(string? code, string? description, string? type, string? status);
}

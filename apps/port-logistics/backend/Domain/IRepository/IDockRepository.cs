namespace Domain.IRepository;

using Domain.Model;

public interface IDockRepository : IGenericRepository<Dock>
{
    Task<IEnumerable<Dock>> GetAllAsync();
    Task<Dock?> GetByCodeAsync(string code);
    new Task<Dock> Add(Dock dock);
    Task<Dock?> Update(Dock dock, List<string> errorMessages);
    Task<bool> Exists(string code);

    Task<IEnumerable<Dock>> SearchAsync(string? name, string? vesselType, string? location);
}

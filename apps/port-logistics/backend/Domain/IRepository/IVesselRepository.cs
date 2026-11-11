namespace Domain.IRepository;

using Domain.Model;

public interface IVesselRepository : IGenericRepository<Vessel>
{
    Task<IEnumerable<Vessel>> GetAllAsync();
    Task<Vessel?> GetByImoNumberAsync(string imoNumber);
    new Task<Vessel> Add(Vessel vessel);
    Task<Vessel?> Update(Vessel vessel, List<string> errorMessages);
    Task<bool> Exists(string imoNumber);

    Task<IEnumerable<Vessel>> SearchAsync(string? imoNumber, string? name, string? operatorName);
}

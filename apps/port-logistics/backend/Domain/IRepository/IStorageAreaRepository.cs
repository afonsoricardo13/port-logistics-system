namespace Domain.IRepository;

using Domain.Model;

public interface IStorageAreaRepository : IGenericRepository<StorageArea>
{
    Task<IEnumerable<StorageArea>> GetAllAsync();
    Task<StorageArea?> GetByIdentifierAsync(string identifier);
    Task<bool> Exists(string identifier);
    new Task<StorageArea> Add(StorageArea area);
    Task<StorageArea?> Update(StorageArea area, List<string> errorMessages);
}

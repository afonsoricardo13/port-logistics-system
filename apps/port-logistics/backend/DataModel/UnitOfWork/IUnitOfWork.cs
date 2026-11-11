using Domain.IRepository;

namespace UnitOfWork;

public interface IUnitOfWork : IDisposable
{
    IGenericRepository Generic { get; }

    Task<int> CompleteAsync();
}

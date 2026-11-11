namespace DataModel.Mapper;

using DataModel.Model;
using Domain.Factory;
using Domain.Model;

public class VesselMapper
{
    private readonly IVesselFactory _factory;

    public VesselMapper(IVesselFactory factory)
    {
        _factory = factory;
    }

    public Vessel? ToDomain(VesselDataModel dm)
    {
        try
        {
            var v = _factory.NewVessel(dm.ImoNumber, dm.Name, dm.VesselType, dm.Operator);
            v.Id = dm.Id;
            return v;
        }
        catch (ArgumentException ex)
        {
            // Log e retornar null para vessels com dados inválidos
            Console.WriteLine($"Warning: Invalid vessel data for ID {dm.Id}: {ex.Message}");
            return null;
        }
    }

    public IEnumerable<Vessel> ToDomain(IEnumerable<VesselDataModel> items)
        => items.Select(ToDomain).Where(v => v != null).Select(v => v!);

    public VesselDataModel ToDataModel(Vessel v) => new VesselDataModel(v);

    public void UpdateDataModel(VesselDataModel dm, Vessel v)
    {
        dm.Name = v.Name;
        dm.VesselType = v.VesselType;
        dm.Operator = v.Operator;
    }
}

namespace Domain.Factory;

using Domain.Model;

public interface IVesselTypeFactory
{
    VesselType NewVesselType(string name, string description, int capacity, int maxRows, int maxBays, int maxTiers);
}

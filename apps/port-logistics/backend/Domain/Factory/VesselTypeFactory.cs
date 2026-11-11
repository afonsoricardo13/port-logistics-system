namespace Domain.Factory;

using Domain.Model;

public class VesselTypeFactory : IVesselTypeFactory
{
    public VesselType NewVesselType(string name, string description, int capacity, int maxRows, int maxBays, int maxTiers)
        => new VesselType(name, description, capacity, maxRows, maxBays, maxTiers);
}

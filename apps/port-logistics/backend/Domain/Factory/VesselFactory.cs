namespace Domain.Factory;

using Domain.Model;

public class VesselFactory : IVesselFactory
{
    public Vessel NewVessel(string imoNumber, string name, string vesselType, string operatorName)
    {
        return new Vessel(imoNumber, name, vesselType, operatorName);
    }
}

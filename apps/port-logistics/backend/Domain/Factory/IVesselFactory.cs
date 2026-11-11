namespace Domain.Factory;

using Domain.Model;

public interface IVesselFactory
{
    Vessel NewVessel(string imoNumber, string name, string vesselType, string operatorName);
}

namespace Domain.Factory;

using Domain.Model;

public interface IDockFactory
{
    Dock NewDock(string code, string nameOrNumber, string location,
                 double length, double depth, double maxDraft,
                 IEnumerable<string> allowedVesselTypes);
}

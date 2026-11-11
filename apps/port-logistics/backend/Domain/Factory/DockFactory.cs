namespace Domain.Factory;

using Domain.Model;

public class DockFactory : IDockFactory
{
    public Dock NewDock(string code, string nameOrNumber, string location,
                        double length, double depth, double maxDraft,
                        IEnumerable<string> allowedVesselTypes)
    {
        return new Dock(code, nameOrNumber, location, length, depth, maxDraft, allowedVesselTypes);
    }
}

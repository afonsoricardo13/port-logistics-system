using DataModel.Model;
using DataModel.Repository;
using Domain.Model;

namespace WebApi.IntegrationTests.Helpers;

public static class Utilities
{
    public static void InitializeDbForTests(AbsanteeContext db)
    {
        // Database initialized for tests
        db.SaveChanges();
    }

    public static void ReinitializeDbForTests(AbsanteeContext db)
    {
        db.Docks.RemoveRange(db.Docks);
        db.OperatingStaff.RemoveRange(db.OperatingStaff);
        db.Vessels.RemoveRange(db.Vessels);
        db.VesselVisitNotifications.RemoveRange(db.VesselVisitNotifications);
        InitializeDbForTests(db);
    }
}

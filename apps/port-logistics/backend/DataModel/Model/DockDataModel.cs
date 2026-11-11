namespace DataModel.Model;

using Domain.Model;

public class DockDataModel
{
    public long Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string NameOrNumber { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public double Length { get; set; }
    public double Depth { get; set; }
    public double MaxDraft { get; set; }
    public string AllowedVesselTypesCsv { get; set; } = string.Empty; // CSV for simplicity

    public DockDataModel() { }

    public DockDataModel(Dock d)
    {
        Id = d.Id;
        Code = d.Code; NameOrNumber = d.NameOrNumber; Location = d.Location;
        Length = d.Physical.Length; Depth = d.Physical.Depth; MaxDraft = d.Physical.MaxDraft;
        AllowedVesselTypesCsv = string.Join(',', d.AllowedVesselTypes);
    }
}

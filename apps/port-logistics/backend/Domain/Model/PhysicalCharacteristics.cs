namespace Domain.Model;

public class PhysicalCharacteristics
{
    public double Length { get; private set; }
    public double Depth { get; private set; }
    public double MaxDraft { get; private set; }

    private PhysicalCharacteristics() { }

    public PhysicalCharacteristics(double length, double depth, double maxDraft)
    {
        if (length <= 0 || depth <= 0 || maxDraft <= 0)
            throw new ArgumentException("Physical characteristics must be positive");

        Length = length;
        Depth = depth;
        MaxDraft = maxDraft;
    }

    public bool Update(double length, double depth, double maxDraft)
    {
        if (length <= 0 || depth <= 0 || maxDraft <= 0) return false;
        Length = length; Depth = depth; MaxDraft = maxDraft; return true;
    }
}

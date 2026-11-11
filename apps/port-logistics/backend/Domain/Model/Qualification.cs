namespace Domain.Model;

using System;
using System.ComponentModel.DataAnnotations;

/// <summary>
/// US 2.2.13: Staff qualification entity
/// QualificationName serves as primary key, RequiredNumber indicates how many staff members need this qualification
/// </summary>
public class Qualification
{
    [Key]
    private string _qualificationName = string.Empty;
    public string QualificationName
    {
        get { return _qualificationName; }
    }

    private int _requiredNumber;
    public int RequiredNumber
    {
        get { return _requiredNumber; }
    }

    // EF Core
    private Qualification()
    {
    }

    public Qualification(string qualificationName, int requiredNumber)
    {
        if (!IsValidParameters(qualificationName, requiredNumber))
        {
            throw new ArgumentException("Invalid qualification parameters");
        }

        // Store in uppercase for case-insensitive comparison
        _qualificationName = qualificationName.Trim().ToUpper();
        _requiredNumber = requiredNumber;
    }

    // BR1: name is unique (PK), BR2: 2-100 chars, BR3: requiredNumber >= 0, BR4: max 999
    private bool IsValidParameters(string qualificationName, int requiredNumber)
    {
        if (string.IsNullOrWhiteSpace(qualificationName))
            return false;

        var trimmedName = qualificationName.Trim();
        if (trimmedName.Length < 2 || trimmedName.Length > 100)
            return false;

        if (requiredNumber < 0 || requiredNumber > 999)
            return false;

        return true;
    }

    // US 2.2.13: only requiredNumber can be updated (name is PK)
    public void UpdateRequiredNumber(int requiredNumber)
    {
        if (requiredNumber < 0)
        {
            throw new ArgumentException("Required number must be non-negative");
        }

        if (requiredNumber > 999)
        {
            throw new ArgumentException("Required number cannot exceed 999");
        }

        _requiredNumber = requiredNumber;
    }

    public string GetQualificationName() => _qualificationName;
    public int GetRequiredNumber() => _requiredNumber;
    public override bool Equals(object? obj)
    {
        if (obj == null || GetType() != obj.GetType())
            return false;

        Qualification other = (Qualification)obj;
        return _qualificationName.Equals(other._qualificationName, StringComparison.OrdinalIgnoreCase);
    }

    public override int GetHashCode()
    {
        return _qualificationName.ToLower().GetHashCode();
    }

    public override string ToString()
    {
        return $"Qualification '{_qualificationName}' (Required: {_requiredNumber})";
    }
}

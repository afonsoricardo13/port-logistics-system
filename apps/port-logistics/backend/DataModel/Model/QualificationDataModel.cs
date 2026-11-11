namespace DataModel.Model;

using Domain.Model;
using System.ComponentModel.DataAnnotations;

/// <summary>
/// Data model for Qualification entity persistence
/// US 2.2.13: Register and manage qualifications
/// </summary>
public class QualificationDataModel
{
    /// <summary>
    /// Qualification name - primary key and unique identifier
    /// </summary>
    [Key]
    [Required]
    [MaxLength(100)]
    public string QualificationName { get; set; } = string.Empty;

    /// <summary>
    /// Required number of staff with this qualification
    /// </summary>
    [Required]
    public int RequiredNumber { get; set; }

    /// <summary>
    /// Parameterless constructor for EF Core
    /// </summary>
    public QualificationDataModel()
    {
    }

    /// <summary>
    /// Constructor from domain model
    /// </summary>
    public QualificationDataModel(Qualification qualification)
    {
        QualificationName = qualification.QualificationName;
        RequiredNumber = qualification.RequiredNumber;
    }
}

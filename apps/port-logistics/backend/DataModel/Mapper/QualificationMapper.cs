namespace DataModel.Mapper;

using DataModel.Model;
using Domain.Model;

/// <summary>
/// Mapper for converting between Qualification domain and data models
/// US 2.2.13: Register and manage qualifications
/// </summary>
public class QualificationMapper
{
    /// <summary>
    /// Convert data model to domain model
    /// </summary>
    public static Qualification? ToDomain(QualificationDataModel dataModel)
    {
        if (dataModel == null)
            return null;

        return new Qualification(
            dataModel.QualificationName,
            dataModel.RequiredNumber
        );
    }

    /// <summary>
    /// Convert domain model to data model
    /// </summary>
    public static QualificationDataModel? ToDataModel(Qualification domain)
    {
        if (domain == null)
            return null;

        return new QualificationDataModel(domain);
    }

    /// <summary>
    /// Convert list of data models to domain models
    /// </summary>
    public static List<Qualification> ToDomainList(List<QualificationDataModel> dataModels)
    {
        if (dataModels == null) 
            return new List<Qualification>();
        
        var result = new List<Qualification>();
        foreach (var dm in dataModels)
        {
            var domain = ToDomain(dm);
            if (domain != null) 
                result.Add(domain);
        }
        return result;
    }

    /// <summary>
    /// Convert list of domain models to data models
    /// </summary>
    public static List<QualificationDataModel> ToDataModelList(List<Qualification> domains)
    {
        if (domains == null) 
            return new List<QualificationDataModel>();
        
        var result = new List<QualificationDataModel>();
        foreach (var d in domains)
        {
            var dataModel = ToDataModel(d);
            if (dataModel != null) 
                result.Add(dataModel);
        }
        return result;
    }
}

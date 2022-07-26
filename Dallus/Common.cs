using System.Reflection;

namespace Dallus
{
    #region Interfaces

    /// <summary>
    ///
    /// </summary>
    public interface IRepo {/* Not sure if needed just yet */}

    /// <summary>
    /// Implement this interface on Model or ViewModel classes to use the Repo
    /// </summary>
    public interface IRepoModel {/* Type Marker Interface */}

    /// <summary>
    /// Currently Not Used
    /// </summary>
    public interface ITableColumn { /* For specific list overrides of ToString()*/}

    /// <summary>
    /// Implement this interface to provide custom member mapping
    /// </summary>
    public interface IMemberMap
    {
        // Summary: Source DataReader column name
        string ColumnName { get; }

        // Summary: Target field
        FieldInfo Field { get; }

        // Summary: Target member type
        Type MemberType { get; }

        // Summary: Target constructor parameter
        ParameterInfo Parameter { get; }

        // Summary: Target property
        PropertyInfo Property { get; }
    }

    #endregion Interfaces

    #region Enumerations

    public enum InsertScriptType
    {
        PkId,
        FullModel
    }

    public enum ModelDetail
    {
        InsertScript,
        UpdateScript,
        UpdateWhereScript,
        DeleteScript,
        DeleteByIdScript,
        DeleteWhereScript,
        ExistsScript,
        ExistsWhereScript,
        PageScript,
        PageWhereScript,
        TopScript,
        TopWhereScript,
        CountScript,
        CountWhereScript,
        SelectAll,
        SelectById,
        SelectWhereScript,
        TableName,
        TableColumns,
        PkName,
        PkIsNumeric,
        PropItems
    }

    #endregion Enumerations
}
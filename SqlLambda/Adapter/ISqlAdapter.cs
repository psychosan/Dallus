﻿/* License: http://www.apache.org/licenses/LICENSE-2.0 */

namespace SqlLambda.Adapter
{
    /// <summary>
    /// SQL adapter provides db specific functionality related to db specific SQL syntax
    /// </summary>
    interface ISqlAdapter
    {
        string QueryString(string selection, string source, string conditions, string order, string grouping, string having);
        string QueryStringPage(string selection, string source, string conditions, string order, int pageSize, int pageNumber);
        string QueryStringPage(string selection, string source, string conditions, string order, int pageSize);
        string Table(string tableName);
        string Field(string tableName, string fieldName);
        string Parameter(string parameterId);
    }
}

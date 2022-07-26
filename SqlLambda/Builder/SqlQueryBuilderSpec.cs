/* License: http://www.apache.org/licenses/LICENSE-2.0 */

using SqlLambda.ValueObjects;

namespace SqlLambda.Builder
{
    /// <summary>
    /// Implements the SQL building for JOIN, ORDER BY, SELECT, and GROUP BY
    /// </summary>
    partial class SqlQueryBuilder
    {
        public void Join(string originalTableName, string joinTableName, string leftField, string rightField)
        {
            var joinString = $"JOIN {Adapter.Table(joinTableName)} ON {Adapter.Field(originalTableName, leftField)} = {Adapter.Field(joinTableName, rightField)}";
            _tableNames.Add(joinTableName);
            _joinExpressions.Add(joinString);
            _splitColumns.Add(rightField);
        }

        public void OrderBy(string tableName, string fieldName, bool desc = false)
        {
            var order = Adapter.Field(tableName, fieldName);
            
            if (desc) order += " DESC";     // TODO: Validate that we get an 'ASC' if not provided.. suspicious

            _sortList.Add(order);            
        }

        public void Select(string tableName)
        {
            var selectionString = $"{Adapter.Table(tableName)}.*";
            _selectionList.Add(selectionString);
        }

        public void Select(string tableName, string fieldName) => _selectionList.Add(Adapter.Field(tableName, fieldName));

        public void Select(string tableName, string fieldName, SelectFunction selectFunction)
        {
            var selectionString = $"{selectFunction.ToString()}({Adapter.Field(tableName, fieldName)})";
            _selectionList.Add(selectionString);
        }

        public void GroupBy(string tableName, string fieldName) => _groupingList.Add(Adapter.Field(tableName, fieldName));
    }
}

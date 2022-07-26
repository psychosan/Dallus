/* License: http://www.apache.org/licenses/LICENSE-2.0 */

using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using SqlLambda.Adapter;

namespace SqlLambda.Builder
{
    /// <summary>
    /// Implements the whole SQL building logic. Continually adds and stores the SQL parts as the requests come. 
    /// When requested to return the QueryString, the parts are combined and returned as a single query string.
    /// The query parameters are stored in a dictionary implemented by an ExpandoObject that can be requested by QueryParameters.
    /// </summary>
    public partial class SqlQueryBuilder
    {
        internal ISqlAdapter Adapter { get; set; }

        private const string PARAMETER_PREFIX = "Param";

        private readonly List<string> _tableNames = new List<string>();
        private readonly List<string> _joinExpressions = new List<string>();
        private readonly List<string> _selectionList = new List<string>();
        private readonly List<string> _conditions = new List<string>();
        private readonly List<string> _sortList = new List<string>();
        private readonly List<string> _groupingList = new List<string>();
        private readonly List<string> _havingConditions = new List<string>();
        private readonly List<string> _splitColumns = new List<string>();
        private int _paramIndex;

        public List<string> TableNames => _tableNames;
        public List<string> JoinExpressions => _joinExpressions;
        public List<string> SelectionList => _selectionList;
        public List<string> WhereConditions => _conditions;
        public List<string> OrderByList => _sortList;
        public List<string> GroupByList => _groupingList;
        public List<string> HavingConditions => _havingConditions;
        public List<string> SplitColumns => _splitColumns;
        public int CurrentParamIndex => _paramIndex;
        public IDictionary<string, object> Parameters { get; private set; }


        public string QueryString => Adapter.QueryString(Selection, Source, Conditions, Grouping, Having, Order);

        private string Source
        {
            get
            {
                var joinExpression = string.Join(" ", _joinExpressions);
                return $"{Adapter.Table(_tableNames.First())} {joinExpression}";
            }
        }

        private string Selection
        {
            get
            {
                if (_selectionList.Count == 0)
                    return $"{Adapter.Table(_tableNames.First())}.*";
                
                // else
                return string.Join(", ", _selectionList);
            }
        }

        private string Conditions
        {
            get
            {
                if (_conditions.Count == 0)
                    return "";
                
                // else
                return "WHERE " + string.Join("", _conditions);
            }
        }

        private string Order
        {
            get
            {
                if (_sortList.Count == 0)
                    return "";

                // else
                return "ORDER BY " + string.Join(", ", _sortList);
            }
        }

        private string Grouping
        {
            get
            {
                if (_groupingList.Count == 0)
                    return "";
                
                // else
                return "GROUP BY " + string.Join(", ", _groupingList);
            }
        }

        private string Having
        {
            get
            {
                if (_havingConditions.Count == 0)
                    return "";

                // else
                return "HAVING " + string.Join(" ", _havingConditions);
            }
        }

        public string QueryStringPage(int pageSize, int? pageNumber = null)
        {
            if (pageNumber.HasValue)
            {
                if (_sortList.Count == 0)
                    throw new Exception("Pagination requires the ORDER BY statement to be specified");

                return Adapter.QueryStringPage(Source, Selection, Conditions, Order, pageSize, pageNumber.Value);
            }
            
            return Adapter.QueryStringPage(Source, Selection, Conditions, Order, pageSize);
        }

        internal SqlQueryBuilder(string tableName, ISqlAdapter adapter)
        {
            _tableNames.Add(tableName);
            Adapter = adapter;
            Parameters = new ExpandoObject();
            _paramIndex = 0;
        }

        #region helpers
        private string NextParamId()
        {
            ++_paramIndex;
            return PARAMETER_PREFIX + _paramIndex.ToString(CultureInfo.InvariantCulture);
        }

        private void AddParameter(string key, object value)
        {
            if(!Parameters.ContainsKey(key))
                Parameters.Add(key, value);
        }
        #endregion
    }
}

using System.Reflection;
using System.Text;

namespace Dallus
{
    /*-------------------------------------------------------------------------------------------------
     * Author: Felix De Herrera  -- *~FXD>
     *   Date: Mid-Late 2017
     *
     *   There are two types of usage for these generated scripts:
     *   1) Full Models - Scripts that handle a full model passed in to the repo
     *   2) PrimaryKey Ops - Scripts that are primarily built for data ops via a pk
     *
     *   Primary Keys - Required!
     *     Table PK Conventions:  {TableName}Id, {TableName}_Id, Id, or a field/prop decorated with [DapperPk]
     *
     *     When passing a pkIdValue the default matching queryString parameter is '@pkVal'
     *     Example:
     *        A delete by id would look like the following:
     *        'Delete From [Table Name] Where [Table Pk Name] = @pkVal' <= So the argument you pass in
     *         should have the variable name of pkVal or be passed in as 'new{pkval}'
     *         or 'new{pkVal = yourVariable_or_value}
     *
     *   WHERE Clauses
     *   1) All WHERE clauses clean incoming 'where' statements of the 'where' word
     *   2) Expect a parameter variable named '@whereCondition'
     *      - The where condition should just be text with matching arguments passed in.
     *      - The system will execute a user specified 'where clause' with values included in the
     *        submitted script as well.
     *
     --------------------------------------------------------------------------------------------------*/

    public class ModelParser<T> where T : class
    {
        #region Public Properties

        /// <summary>
        /// Database Table (Model) name
        /// </summary>
        public string TableName { get; set; }

        /// <summary>
        /// Name of the Primary Key field
        /// </summary>
        public string PkName { get; set; }

        /// <summary>
        /// Is the primary key a numeric type
        /// </summary>
        public bool PkIsNumeric { get; set; }

        /// <summary>
        /// List of PropItems for all fields except those decorated with the [DapperIgnore] attribute
        /// </summary>
        public IList<PropItem> PropItems { get; set; }

        /// <summary>
        /// Returns full set of table columns, including the primary key. e.g. "[field1], [field2], [etc...]"
        /// </summary>
        public string TableColumns { get; set; }

        #endregion Public Properties

        #region ctor and Main class parsers

        public ModelParser()
        {
            InitializeClass();
        }

        private void InitializeClass()
        {
            bool isIgnored = false;
            var model = typeof(T);
            var modelProperties = model.GetProperties();
            PropItems = new List<PropItem>();

            var propNames = modelProperties.Select(k => k.Name).ToList();
            TableName = model.Name;

            // Get ignored class properties
            var ignoredProps = modelProperties.Where(k => Attribute.IsDefined(k, typeof(DapperIgnoreAttribute))).Select(x => x.Name).ToList();
            bool hasIgnore = ignoredProps.Any();

            // Find Pk
            PkName = FindPkName(modelProperties, model.Name);

            if (string.IsNullOrWhiteSpace(PkName))
                throw new ArgumentOutOfRangeException("PkName", "Primary Key Not Found!\nModel Classes Must Contain a Primary Key");

            // Check pk type //<< Note: GetProperty() call is case sensitive
            PkIsNumeric = model.GetProperty(PkName).PropertyType.IsValueType;

            // Build out class metadata
            foreach (var propName in propNames)
            {
                if (hasIgnore)
                    isIgnored = ignoredProps.Any(k => k.Equals(propName, StringComparison.CurrentCulture));

                PropItems.Add(new PropItem
                {
                    Name = propName,
                    IsIgnored = isIgnored,
                    IsPrimaryKey = propName == PkName
                });

                isIgnored = false;
            }

            // Build out table column list
            TableColumns = PropItems.ToColumnString();
        }

        private string FindPkName(PropertyInfo[] modelProps, string tableName)
        {
            // Pk Follows Standard PK Naming Conventions for db tables
            var pkName = modelProps.Where(k => Attribute.IsDefined(k, typeof(DapperPkAttribute))).Select(x => x.Name).FirstOrDefault();

            if (!string.IsNullOrWhiteSpace(pkName))
                return pkName;

            var props = modelProps.Select(k => k.Name);

            if (props.Any())
            {
                var pkSearchName = "id";           //<< This is for you Mark :)
                pkName = props.FirstOrDefault(k => k.Equals(pkSearchName, StringComparison.CurrentCultureIgnoreCase));

                if (pkName != null)
                    return pkName;

                pkSearchName = $"{tableName}Id";
                pkName = props.FirstOrDefault(k => k.Equals(pkSearchName, StringComparison.CurrentCultureIgnoreCase));

                if (pkName != null)
                    return pkName;

                pkSearchName = $"{tableName}_Id";
                pkName = props.FirstOrDefault(k => k.Equals(pkSearchName, StringComparison.CurrentCultureIgnoreCase));

                if (pkName != null)
                    return pkName;
            }

            // Otherwise handle a crazy person..
            throw new ArgumentOutOfRangeException("PkName", "Primary Key Not Found!\nModel Classes Must Contain a Primary Key\n -->m:FindPkName returned null");
        }

        private string GetUpdateColumnSet()
        {
            StringBuilder qry = new StringBuilder();

            foreach (var prop in PropItems)
                qry.Append(prop.ToUpdateSegment());

            return qry.ToString().TrimEnd(',');
        }

        public Tuple<string, string> GetInsertColumnSet()
        {
            StringBuilder insertCols = new StringBuilder();
            StringBuilder updateColValues = new StringBuilder();

            foreach (var prop in PropItems)
            {
                insertCols.Append(prop.ToInsertArgument());
                updateColValues.Append(prop.ToInsertValue());
            }

            var listCols = insertCols.ToString().TrimEnd(',');
            var listColVals = updateColValues.ToString().TrimEnd(',');
            return new Tuple<string, string>(listCols, listColVals);
        }

        #endregion ctor and Main class parsers

        #region Public Methods

        public string GetUpdateScript()
        {
            /*
                Returns string in following format for usage with Dapper:
                UPDATE table_name
                SET column1=value1,column2=value2,...
                WHERE some_column=some_value;
             */

            var updateCols = GetUpdateColumnSet();
            return $"UPDATE {TableName} SET {updateCols} WHERE [{PkName}] = @{PkName}";
        }

        public string GetInsertScript()
        {
            //TODO: What If PK is Non-Numeric???

            /*  Returns string in following format for usage with Dapper:
                Insert Into table_name (column1, column2)
                Values (@column1, @column2);
                SELECT CAST(SCOPE_IDENTITY() as int)  */

            var insertColSet = GetInsertColumnSet();
            var insertScript =
                $"INSERT INTO {TableName} ({insertColSet.Item1}) VALUES ({insertColSet.Item2}); SELECT CAST(SCOPE_IDENTITY() AS INT);";

            return insertScript;
        }

        public string GetExistsScript()
        {
            return $"IF EXISTS (SELECT {PkName} FROM {TableName} WHERE [{PkName}] = @{PkName}) Select 1 ELSE Select 0";
        }

        public string GetDeleteScript()
        {
            return $"Delete From {TableName} Where [{PkName}] = @pkVal; Select @@ROWCOUNT as 'Success'";
        }

        public string GetPagingScript()
        {
            /*
                SELECT [a_bunch_of_columns]
                FROM dbo.[some_table]
                ORDER BY [some_column_or_columns]
                OFFSET @PageSize * (@Page - 1) ROWS
                FETCH NEXT @PageSize ROWS ONLY;
             */

            StringBuilder sb = new StringBuilder();
            sb.AppendLine(@"SELECT {0}");
            sb.AppendLine(@"FROM {1}");
            sb.AppendLine(@"ORDER BY {2}");
            sb.AppendLine(@"OFFSET @pageSize * (@page - 1) ROWS");
            sb.AppendLine(@"FETCH NEXT @pageSize ROWS ONLY;");

            string pagingScript = string.Format(sb.ToString(), TableColumns, TableName, TableColumns);

            return pagingScript;
        }

        public string GetPagingWhereScript()
        {
            /*
                SELECT [a_bunch_of_columns]
                FROM dbo.[some_table]
                ORDER BY [some_column_or_columns]
                OFFSET @PageSize * (@Page - 1) ROWS
                FETCH NEXT @PageSize ROWS ONLY;
             */

            StringBuilder sb = new StringBuilder();
            sb.AppendLine(@"SELECT {0}");
            sb.AppendLine(@"FROM {1}");
            sb.AppendLine(@"WHERE @whereCondition");
            sb.AppendLine(@"ORDER BY {2}");
            sb.AppendLine(@"OFFSET @pageSize * (@page - 1) ROWS");
            sb.AppendLine(@"FETCH NEXT @pageSize ROWS ONLY;");

            string pagingScript = string.Format(sb.ToString(), TableColumns, TableName, TableColumns);

            return pagingScript;
        }

        public string GetTopScript()
        {
            return $"SELECT TOP(@topCount) {TableColumns} FROM [{TableName}]";
        }

        public string GetTopScriptWhere()
        {
            return $"SELECT TOP(@topCount) {TableColumns} FROM [{TableName}] WHERE @whereCondition";
        }

        private string GetUpdateWhereScript()
        {
            /*
                Returns string in following format for usage with Dapper:
                UPDATE table_name
                SET column1=@value1,column2=@value2,...
                WHERE @whereCondition;
             */

            var updateCols = GetUpdateColumnSet();
            return $"UPDATE [{TableName}] SET {updateCols} WHERE @whereCondition";
        }

        private string GetTopWhereScript()
        {
            return $"SELECT TOP(@topCount) {TableColumns} FROM [{TableName}] WHERE @whereCondition";
        }

        private string GetExistsWhereScript()
        {
            return $"SELECT {TableColumns} FROM [{TableName}] WHERE @whereCondition";
        }

        private string GetDeleteWhereScript()
        {
            return $"DELETE FROM [{TableName}] WHERE @whereCondition; Select @@ROWCOUNT as 'Success'";
        }

        private string GetDeleteByIdScript()
        {
            return $"DELETE FROM [{TableName}] WHERE [{PkName}] = @{PkName}; Select @@ROWCOUNT as 'Success'";
        }

        private string GetCountWhereScript()
        {
            return $"SELECT COUNT(*) FROM [{TableName}] WHERE @whereCondition";
        }

        private string GetCountScript()
        {
            return $"SELECT COUNT(*) FROM [{TableName}]";
        }

        private string GetSelectAllScript()
        {
            return $"SELECT {TableColumns} FROM [{TableName}]";
        }

        private string GetSelectWhereScript()
        {
            return $"SELECT {TableColumns} FROM [{TableName}] WHERE @whereCondition";
        }

        private string GetSelectByIdScript()
        {
            return $"SELECT {TableColumns} FROM [{TableName}] WHERE [{PkName}] = @pkVal";
        }

        private string GetPageWhereScript()
        {
            /*
                SELECT [a_bunch_of_columns]
                FROM dbo.[some_table]
                ORDER BY [some_column_or_columns]
                OFFSET @PageSize * (@Page - 1) ROWS
                FETCH NEXT @PageSize ROWS ONLY;
             */

            StringBuilder sb = new StringBuilder();
            sb.AppendLine(@"SELECT {0}");
            sb.AppendLine(@"FROM {1}");
            sb.AppendLine(@"WHERE @whereCondition");
            sb.AppendLine(@"ORDER BY {2}");
            sb.AppendLine(@"OFFSET @pageSize * (@page - 1) ROWS");
            sb.AppendLine(@"FETCH NEXT @pageSize ROWS ONLY;");

            string pagingScript = string.Format(sb.ToString(), TableColumns, TableName, TableColumns);

            return pagingScript;
        }

        internal ModelInfo ModelInfo()
        {
            return new ModelInfo
            {
                InsertScript = GetInsertScript(),
                UpdateScript = GetUpdateScript(),
                UpdateWhereScript = GetUpdateWhereScript(),
                DeleteScript = GetDeleteScript(),
                DeleteByIdScript = GetDeleteByIdScript(),
                DeleteWhereScript = GetDeleteWhereScript(),
                ExistsScript = GetExistsScript(),
                ExistsWhereScript = GetExistsWhereScript(),
                PageScript = GetPagingScript(),
                PageWhereScript = GetPageWhereScript(),
                TopScript = GetTopScript(),
                TopWhereScript = GetTopWhereScript(),
                CountScript = GetCountScript(),
                CountWhereScript = GetCountWhereScript(),
                SelectAllScript = GetSelectAllScript(),
                SelectByIdScript = GetSelectByIdScript(),
                SelectWhereScript = GetSelectWhereScript(),

                TableName = TableName,
                PkName = PkName,
                PkIsNumeric = PkIsNumeric,
                PropItems = PropItems,
                TableColumns = TableColumns
            };
        }

        #endregion Public Methods
    }
}
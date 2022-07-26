using System.Collections.Concurrent;
using System.Data;
using System.Data.SqlClient;
using System.Linq.Expressions;
using Dapper;
using SqlLambda;

namespace Dallus
{
    /// <summary>
    /// Static Version of Repo.
    /// <para>IMPORTANT: Initialize method MUST be called before accessing any static methods</para>
    /// <para>IMPORTANT: Or add code to initialize from app/web config file</para>
    /// </summary>
    public static class Repo
    {
        // TODO: Change the implementation of this to be fluent and force user to initialize first before using

        #region Initialization

        private static string _connection;

        internal static ConcurrentDictionary<string, ModelInfo> ModelStore = new ConcurrentDictionary<string, ModelInfo>();

        public static void Initialize(string connectionString)
        {
            _connection = connectionString;
        }

        #endregion Initialization

        #region Repo Implementations

        #region Saves 

        /// <summary>
        /// If inbound model contains a PK an update is performed
        /// otherwise and insert is processed
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="model"></param>
        /// <param name="modInfo"></param>
        /// <returns>Supplied model</returns>
        public static T Save<T>(T model, ModelInfo modInfo = null) where T : class, IRepoModel
        {
            string pkName = GetModelDetail<T>(ModelDetail.PkName, modInfo);

            // Get PkId column value
            var pkVal = GetPropValue(model, pkName);

            // Update if we find a PkValue

            if (NotNullEmptyOrZero(pkVal))
                return Update(model, out _, modInfo);

            return Insert(model, modInfo);
        }

        /// <summary>
        /// </summary>
        /// <param name="models"></param>
        /// <returns></returns>
        public static IEnumerable<T> SaveAll<T>(IEnumerable<T> models) where T : class, IRepoModel
        {
            var modInfo = TryGetCacheItem<T>();

            foreach (var model in models)
                Save(model, modInfo);

            return models;
        }

        #endregion Saves 

        #region Inserts 

        /// <summary>
        /// Inserts the supplied populated model and returns the newly inserted record with the PkId populated
        /// </summary>
        /// <param name="model"></param>
        /// <param name="modInfo"></param>
        /// <returns></returns>
        public static T Insert<T>(T model, ModelInfo? modInfo = null) where T : class, IRepoModel
        {
            // If called from [Insert/Update/Save]All,
            // no need to hit the cache a bunch of times for the same model.

            var mi = modInfo ?? TryGetCacheItem<T>();
            var qryScript = mi.InsertScript;
            var pkName = mi.PkName;

            var newId = WithConnection<long>(k => k.QuerySingle<long>(qryScript, model, commandType: CommandType.Text));
            model.SetPkId<T>(pkName, newId);

            return model;
        }

        /// <summary>
        /// </summary>
        /// <param name="models"></param>
        /// <returns></returns>
        public static IEnumerable<T> InsertAll<T>(IEnumerable<T> models) where T : class, IRepoModel
        {
            // TODO: Verify that this actually works.. modifying referenced item in 4 loop? or create new list to return?
            var modInfo = TryGetCacheItem<T>();
            //var modelSet = models.ToList();

            foreach (T model in models)
                Insert(model, modInfo);

            return models;
        }

        #endregion Inserts 

        #region Updates

        /// <summary>
        /// Update a record
        /// </summary>
        /// <param name="model"></param>
        /// <returns>
        /// True for success, False for failure
        /// </returns>
        public static bool Update<T>(T model, ModelInfo modelInfo = null) where T : class, IRepoModel
        {
            string updateScript = GetModelDetail<T>(ModelDetail.UpdateScript, modelInfo);
            int result = WithConnection<int>(k => k.Execute(updateScript, new { model }, commandType: CommandType.Text));

            return result > 0;
        }

        public static T Update<T>(T model, out bool success, ModelInfo modelInfo = null) where T : class, IRepoModel
        {
            string updateScript = GetModelDetail<T>(ModelDetail.UpdateScript, modelInfo);
            int result = WithConnection<int>(k => k.Execute(updateScript, model, commandType: CommandType.Text));

            // Set our return variable
            success = result > 0;

            return model;
        }

        /// <summary>
        /// </summary>
        /// <param name="models"></param>
        /// <returns></returns>
        public static IEnumerable<T> UpdateAll<T>(IEnumerable<T> models) where T : class, IRepoModel
        {
            var modInfo = TryGetCacheItem<T>();

            foreach (var model in models)
                Update(model, modInfo);

            return models;
        }

        public static bool UpdateWhere<T>(string whereCondition, T model) where T : class, IRepoModel
        {
            whereCondition = CleanWhereClause(whereCondition);
            string updateScript = GetModelDetail<T>(ModelDetail.UpdateWhereScript);
            updateScript = updateScript.Replace("@whereCondition", whereCondition);

            int result = WithConnection<int>(k => k.Execute(updateScript, model, commandType: CommandType.Text));

            return result > 0;
        }

        public static bool UpdateWhere<T>(Expression<Func<T, bool>> predicate, T model) where T : class, IRepoModel
        {
            string updateScript = GetModelDetail<T>(ModelDetail.UpdateScript);
            //int result = WithConnection<int>(k => k.Execute(updateScript, model, commandType: CommandType.Text));

            var sql = new SqlLam<T>(predicate);
            string whereCond = LambdaExtractSqlWhere(sql);
            string qryScript = GetModelDetail<T>(ModelDetail.UpdateWhereScript);

            qryScript = qryScript.Replace("@whereCondition", whereCond);
            sql.QueryParameters.Add("model", model);
            return WithConnection<bool>(k => k.QuerySingle<T>(qryScript, new { model, sql.QueryParameters }, commandType: CommandType.Text) != null);
        }

        #endregion Updates

        #region Deletes

        public static bool Delete<T>(T model) where T : class, IRepoModel
        {
            var mi = GetActionQueryPkg(model, ModelDetail.DeleteScript);
            var pkVal = mi.pkVal;

            return WithConnection<bool>(k => k.QuerySingle<bool>(mi.qryScript, new { pkVal }, commandType: CommandType.Text));
        }

        public static bool Delete<T>(dynamic pkId) where T : class, IRepoModel
        {
            ModelInfo modInfo = TryGetCacheItem<T>();
            var pkVal = SimplePkCast(pkId, modInfo.PkIsNumeric);
            return WithConnection<bool>(k => k.QuerySingle<int>(modInfo.DeleteScript, new { pkVal }, commandType: CommandType.Text) > 0);
        }

        public static bool DeleteWhere<T>(string whereCondition, object parameters = null) where T : class, IRepoModel
        {
            string qryScript = GetModelDetail<T>(ModelDetail.DeleteWhereScript);
            whereCondition = CleanWhereClause(whereCondition);
            qryScript = qryScript.Replace("@whereCondition", whereCondition);

            return WithConnection<bool>(k => k.QuerySingle<T>(qryScript, parameters, commandType: CommandType.Text) != null);
        }

        public static bool DeleteWhere<T>(Expression<Func<T, bool>> predicate) where T : class, IRepoModel
        {
            var sql = new SqlLam<T>(predicate);
            string whereCondition = LambdaExtractSqlWhere(sql);
            string deleteWhereScript = GetModelDetail<T>(ModelDetail.DeleteWhereScript);

            deleteWhereScript = deleteWhereScript.Replace("@whereCondition", whereCondition);
            return WithConnection<bool>(k => k.QuerySingle<T>(deleteWhereScript, sql.QueryParameters, commandType: CommandType.Text) != null);
        }

        #endregion Deletes

        #region Get Methods

        /// <summary>
        /// Get a model using the PkId
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="pkVal"></param>
        /// <returns></returns>
        public static T GetById<T>(dynamic pkVal) where T : class, IRepoModel
        {
            string selectByIdScript = GetModelDetail<T>(ModelDetail.SelectById);
            return WithConnection<T>(k => k.QuerySingle<T>(selectByIdScript, new { pkVal }, commandType: CommandType.Text));
        }

        /// <summary>
        /// Exactly the same as GetById
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="pkVal"></param>
        /// <returns></returns>
        public static T FindById<T>(dynamic pkVal) where T : class, IRepoModel
        {
            string selectByIdScript = GetModelDetail<T>(ModelDetail.SelectById);
            return WithConnection<T>(k => k.QuerySingle<T>(selectByIdScript, new { pkVal }, commandType: CommandType.Text));
        }

        public static T GetSingle<T>(dynamic pkId) where T : class, IRepoModel
        {
            string selectByIdScript = GetModelDetail<T>(ModelDetail.SelectById);
            return WithConnection<T>(k => k.QuerySingle<T>(selectByIdScript, new { pkId }, commandType: CommandType.Text));
        }

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="whereCondition"></param>
        /// <returns></returns>
        public static T GetSingleWhere<T>(string whereCondition, object parameters = null) where T : class, IRepoModel
        {
            string qryScript = GetModelDetail<T>(ModelDetail.SelectWhereScript);
            whereCondition = CleanWhereClause(whereCondition);
            qryScript = qryScript.Replace("@whereCondition", whereCondition);

            return WithConnection<T>(k => k.QuerySingle<T>(qryScript, parameters, commandType: CommandType.Text));
        }

        /// <summary>
        /// </summary>
        /// <param name="predicate"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T GetSingleWhere<T>(Expression<Func<T, bool>> predicate) where T : class, IRepoModel
        {
            var sql = new SqlLam<T>(predicate);
            string whereCond = LambdaExtractSqlWhere(sql);
            string qryScript = GetModelDetail<T>(ModelDetail.SelectWhereScript);
            qryScript = qryScript.Replace("@whereCondition", whereCond);

            return WithConnection<T>(k => k.QuerySingle<T>(qryScript, sql.QueryParameters, commandType: CommandType.Text));
        }

        /// <summary>
        /// </summary>
        /// <param name="topCount"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static IEnumerable<T> GetTop<T>(int topCount) where T : class, IRepoModel
        {
            string topScript = GetModelDetail<T>(ModelDetail.TopScript);
            return WithConnection<IEnumerable<T>>(k => k.Query<T>(topScript, new { topCount }, commandType: CommandType.Text));
        }

        public static IEnumerable<T> GetTopWhere<T>(int topCount, string whereCondition) where T : class, IRepoModel
        {
            whereCondition = CleanWhereClause(whereCondition);
            string qryScript = GetModelDetail<T>(ModelDetail.TopWhereScript);
            qryScript = qryScript.Replace("@whereCondition", whereCondition);

            return WithConnection<IEnumerable<T>>(k => k.Query<T>(qryScript, new { topCount }, commandType: CommandType.Text));
        }

        public static IEnumerable<T> GetTopWhere<T>(int topCount, Expression<Func<T, bool>> predicate) where T : class, IRepoModel
        {
            var sql = new SqlLam<T>(predicate);
            sql.QueryParameters.Add("topCount", topCount);
            string whereCond = LambdaExtractSqlWhere(sql);

            string qryScript = GetModelDetail<T>(ModelDetail.TopWhereScript);
            qryScript = qryScript.Replace("@whereCondition", whereCond);

            return WithConnection<IEnumerable<T>>(k => k.Query<T>(qryScript, sql.QueryParameters, commandType: CommandType.Text));
        }

        /// <summary>
        /// </summary>
        /// <param name="pageSize"></param>
        /// <param name="page"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static IEnumerable<T> GetPage<T>(int page = 1, int pageSize = 15) where T : class, IRepoModel
        {
            page = page < 1 ? 1 : page;
            pageSize = pageSize < 1 ? 1 : pageSize;

            string pageScript = GetModelDetail<T>(ModelDetail.PageScript);
            return WithConnection<IEnumerable<T>>(k => k.Query<T>(pageScript, new { page, pageSize }, commandType: CommandType.Text));
        }

        public static IEnumerable<T> GetPageWhere<T>(string whereCondition, int page, int pageSize) where T : class, IRepoModel
        {
            page = page < 1 ? 1 : page;
            pageSize = pageSize < 1 ? 1 : pageSize;

            whereCondition = CleanWhereClause(whereCondition);
            string qryScript = GetModelDetail<T>(ModelDetail.PageWhereScript);
            qryScript = qryScript.Replace("@whereCondition", whereCondition);
            return WithConnection<IEnumerable<T>>(k => k.Query<T>(qryScript, new { page, pageSize }, commandType: CommandType.Text));
        }

        public static IEnumerable<T> GetPageWhere<T>(Expression<Func<T, bool>> predicate, int page, int pageSize) where T : class, IRepoModel
        {
            page = page < 1 ? 1 : page;
            pageSize = pageSize < 1 ? 1 : pageSize;

            var sql = new SqlLam<T>(predicate);
            string whereCondition = LambdaExtractSqlWhere(sql);
            string qryScript = GetModelDetail<T>(ModelDetail.PageWhereScript);

            qryScript = qryScript.Replace("@whereCondition", whereCondition);

            sql.QueryParameters.Add(new KeyValuePair<string, object>("page", page));
            sql.QueryParameters.Add(new KeyValuePair<string, object>("pageSize", pageSize));

            return WithConnection<IEnumerable<T>>(k => k.Query<T>(qryScript, sql.QueryParameters, commandType: CommandType.Text));
        }

        /// <summary>
        /// </summary>
        /// <returns></returns>
        public static IEnumerable<T> GetList<T>() where T : class, IRepoModel
        {
            string sql = GetModelDetail<T>(ModelDetail.SelectAll);
            return WithConnection<IEnumerable<T>>(k => k.Query<T>(sql, commandType: CommandType.Text));
        }

        /// <summary>
        ///
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="whereCondition"></param>
        /// <returns></returns>
        public static IEnumerable<T> GetListWhere<T>(string whereCondition) where T : class, IRepoModel
        {
            string sqlScript = GetModelDetail<T>(ModelDetail.SelectWhereScript);
            whereCondition = CleanWhereClause(whereCondition);
            sqlScript = sqlScript.Replace("@whereCondition", whereCondition);

            return WithConnection<IEnumerable<T>>(k => k.Query<T>(sqlScript, whereCondition, commandType: CommandType.Text));
        }

        /// <summary>
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public static IEnumerable<T> GetListWhere<T>(Expression<Func<T, bool>> predicate) where T : class, IRepoModel
        {
            var sql = new SqlLam<T>(predicate);
            string whereCondition = LambdaExtractSqlWhere(sql);
            string selectWhereScript = GetModelDetail<T>(ModelDetail.SelectWhereScript);

            selectWhereScript = selectWhereScript.Replace("@whereCondition", whereCondition);

            return WithConnection<IEnumerable<T>>(k => k.Query<T>(selectWhereScript, sql.QueryParameters, commandType: CommandType.Text));
        }

        //public static object GetListWhere<T>(Func<T, bool> func) where T : class, IModel
        //{
        //    var sql = new SqlLam<T>(func);
        //    string whereCondition = LamdaExtractSqlWhere(sql);
        //    string selectWhereScript = GetModelDetail<T>(ModelDetail.SelectWhereScript);

        //    selectWhereScript = selectWhereScript.Replace("@whereCondition", whereCondition);

        //    return WithConnection<IEnumerable<T>>(k => k.Query<T>(selectWhereScript, sql.QueryParameters, commandType: CommandType.Text));
        //}

        #endregion Get Methods

        #region Count Operations

        public static int Count<T>() where T : class, IRepoModel
        {
            try
            {
                string countScript = GetModelDetail<T>(ModelDetail.CountScript);
                return WithConnection<int>(k => k.QuerySingle<int>(countScript, commandType: CommandType.Text));
            }
            catch (Exception ex)
            {
                var x = ex;
            }
            return 0;
        }

        public static int CountWhere<T>(string whereCondition, object parameters = null) where T : class, IRepoModel
        {
            try
            {
                whereCondition = CleanWhereClause(whereCondition);
                string qryScript = GetModelDetail<T>(ModelDetail.CountWhereScript);
                qryScript = qryScript.Replace("@whereCondition", whereCondition);

                return WithConnection<int>(k => k.QuerySingle<int>(qryScript, parameters, commandType: CommandType.Text));
            }
            catch (Exception)
            {
                //ConfigLogger.Instance.LogError(ex);
            }
            return 0;
        }

        public static int CountWhere<T>(Expression<Func<T, bool>> predicate) where T : class, IRepoModel
        {
            var sql = new SqlLam<T>(predicate);
            string whereCondition = LambdaExtractSqlWhere(sql);
            string qryScript = GetModelDetail<T>(ModelDetail.CountWhereScript);

            qryScript = qryScript.Replace("@whereCondition", whereCondition);
            return WithConnection<int>(k => k.QuerySingle<int>(qryScript, sql.QueryParameters, commandType: CommandType.Text));
        }

        #endregion Count Operations

        #region Execute Operations

        /// <summary>
        /// </summary>
        /// <param name="sprocName"></param>
        /// <param name="parameters"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static IEnumerable<T> ExecSproc<T>(string sprocName, object? parameters = null) where T : class, IRepoModel
        {
            return WithConnection<IEnumerable<T>>(k => k.Query<T>(sprocName, parameters));
        }

        public static T ExecSprocSingle<T>(string sprocName, object? parameters = null) where T : class, IRepoModel
        {
            return WithConnection<T>(k => k.QuerySingle<T>(sprocName, parameters));
        }

        public static bool ExecAction<T>(string sqlScript, object? parameters = null, CommandType commandType = CommandType.Text) where T : class, IRepoModel
        {
            return WithConnection<bool>(k => k.Execute(sqlScript, parameters, commandType: commandType) > 0);
        }

        public static IEnumerable<T> ExecAny<T>(string sqlScript, object? parameters = null, CommandType commandType = CommandType.Text) where T : class, IRepoModel
        {
            return WithConnection<IEnumerable<T>>(k => k.Query<T>(sqlScript, parameters, commandType: commandType));
        }

        public static T ExecAnySingle<T>(string sqlScript, object? parameters = null, CommandType commandType = CommandType.Text) //This can be anything.. so no => where T : class, IModel
        {
            return WithConnection<T>(k => k.QuerySingle<T>(sqlScript, parameters, commandType: commandType));
        }

        #endregion Execute Operations

        #endregion Repo Implementations

        #region Wrapped and Adjusted Dapper Implementations

        // Summary: Execute parameterized SQL
        // Returns: Number of rows affected
        public static int Execute(CommandDefinition command)
        {
            try
            {
                return WithConnection<int>(k => k.Execute(command));
            }
            catch (Exception ex)
            {
                var x = ex;
            }

            return 0;
        }

        // Summary: Execute parameterized SQL
        // Returns: Number of rows affected
        public static int Execute(string sql, object? param = null, IDbTransaction? transaction = null, int? commandTimeout = default, CommandType? commandType = default)
        {
            try
            {
                return WithConnection<int>(k => k.Execute(sql, param, transaction, commandTimeout, commandType));
            }
            catch (Exception ex)
            {
                var x = ex;
            }

            return 0;
        }

        // Summary: Execute a command asynchronously using .NET 4.5 Task.
        public static Task<int> ExecuteAsync(CommandDefinition command)
        {
            try
            {
                return WithConnectionAsync<int>(k => k.ExecuteAsync(command));
            }
            catch (Exception ex)
            {
                var x = ex;
            }

            return Task.FromResult(0);
        }

        // Summary: Execute a command asynchronously using .NET 4.5 Task.
        public static Task<int> ExecuteAsync(string sql, object? param = null, IDbTransaction? transaction = null, int? commandTimeout = default, CommandType? commandType = default)
        {
            try
            {
                return WithConnectionAsync<int>(k => k.ExecuteAsync(sql, param, transaction, commandTimeout, commandType));
            }
            catch (Exception ex)
            {
                var x = ex;
            }

            return Task.FromResult(0);
        }

        // Summary: Execute parameterized SQL and return an System.Data.IDataReader
        // Returns: An System.Data.IDataReader that can be used to iterate over the results of the
        // SQL query.
        // Remarks: This is typically used when the results of a query are not processed by Dapper,
        // for example, used to fill a System.Data.DataTable or DataSet.
        public static IDataReader? ExecuteReader(CommandDefinition command)
        {
            try
            {
                WithConnection<IDataReader>(k => k.ExecuteReader(command));
            }
            catch (Exception ex)
            {
                var x = ex;
            }

            return null;
        }

        // Summary: Execute parameterized SQL and return an System.Data.IDataReader
        // Returns: An System.Data.IDataReader that can be used to iterate over the results of the
        // SQL query.
        // Remarks: This is typically used when the results of a query are not processed by Dapper,
        // for example, used to fill a System.Data.DataTable or DataSet.
        public static IDataReader? ExecuteReader(CommandDefinition command, CommandBehavior commandBehavior)
        {
            try
            {
                return WithConnection(k => ExecuteReader(command, commandBehavior));
            }
            catch (Exception ex)
            {
                var x = ex;
            }

            return null;
        }

        // Summary: Execute parameterized SQL and return an System.Data.IDataReader
        // Returns: An System.Data.IDataReader that can be used to iterate over the results of the
        // SQL query.
        // Remarks: This is typically used when the results of a query are not processed by Dapper,
        // for example, used to fill a System.Data.DataTable or DataSet.
        public static IDataReader? ExecuteReader(string sql, object? param = null, IDbTransaction? transaction = null, int? commandTimeout = default, CommandType? commandType = default)
        {
            try
            {
                return WithConnection(k => ExecuteReader(sql, param, transaction, commandTimeout, commandType));
            }
            catch (Exception ex)
            {
                var x = ex;
            }

            return null;
        }

        // Summary: Execute parameterized SQL and return an System.Data.IDataReader
        //
        // Returns: An System.Data.IDataReader that can be used to iterate over the results of the
        // SQL query.
        //
        // Remarks: This is typically used when the results of a query are not processed by Dapper,
        // for example, used to fill a System.Data.DataTable or DataSet.
        public static Task<IDataReader>? ExecuteReaderAsync(CommandDefinition command)
        {
            try
            {
                return WithConnectionAsync<IDataReader>(k => k.ExecuteReaderAsync(command));
            }
            catch (Exception ex)
            {
                var x = ex;
            }

            return null; //Task.FromResult<IDataReader>(null);
        }

        // Summary: Execute parameterized SQL and return an System.Data.IDataReader
        //
        // Returns: An System.Data.IDataReader that can be used to iterate over the results of the
        // SQL query.
        //
        // Remarks: This is typically used when the results of a query are not processed by Dapper,
        // for example, used to fill a System.Data.DataTable or DataSet.
        public static Task<IDataReader>? ExecuteReaderAsync(string sql, object param = null, IDbTransaction transaction = null,
                                                           int? commandTimeout = default, CommandType? commandType = default)
        {
            try
            {
                return WithConnectionAsync<IDataReader>(k => k.ExecuteReaderAsync(sql, param, transaction, commandTimeout, commandType));
            }
            catch (Exception ex)
            {
                var x = ex;
            }

            return null; // Task.FromResult<IDataReader>(null);
        }

        // Summary: Execute parameterized SQL that selects a single value
        // Returns: The first cell selected
        public static object? ExecuteScalar(CommandDefinition command)
        {
            try
            {
                return WithConnection<object>(k => k.ExecuteScalar(command));
            }
            catch (Exception ex)
            {
                var x = ex;
            }

            return null;
        }

        // Summary: Execute parameterized SQL that selects a single value
        // Returns: The first cell selected
        public static object? ExecuteScalar(string sql, object? param = null, IDbTransaction? transaction = null,
            int? commandTimeout = default, CommandType? commandType = default)
        {
            try
            {
                return WithConnection<object>(k => k.ExecuteScalar(sql, param, transaction, commandTimeout, commandType));
            }
            catch (Exception ex)
            {
                var x = ex;
            }

            return null;
        }

        // Summary: Execute parameterized SQL that selects a single value
        // Returns: The first cell selected
        public static T? ExecuteScalar<T>(CommandDefinition command)
        {
            try
            {
                return WithConnection<T>(k => k.ExecuteScalar<T>(command));
            }
            catch (Exception ex)
            {
                var x = ex;
            }

            return default(T);
        }

        // Summary: Execute parameterized SQL that selects a single value
        // Returns: The first cell selected
        public static T? ExecuteScalar<T>(string sql, object? param = null, IDbTransaction? transaction = null,
            int? commandTimeout = default, CommandType? commandType = default)
        {
            try
            {
                return WithConnection<T>(k => k.ExecuteScalar<T>(sql, param, transaction, commandTimeout, commandType));
            }
            catch (Exception ex)
            {
                var x = ex;
            }

            return default;
        }

        // Summary: Execute parameterized SQL that selects a single value
        // Returns: The first cell selected
        public static Task<object>? ExecuteScalarAsync(CommandDefinition command)
        {
            try
            {
                return WithConnectionAsync<object>(k => k.ExecuteScalarAsync(command));
            }
            catch (Exception ex)
            {
                var x = ex;
            }

            return null;
        }

        // Summary: Execute parameterized SQL that selects a single value
        // Returns: The first cell selected
        public static Task<object>? ExecuteScalarAsync(string sql, object? param = null, IDbTransaction? transaction = null,
            int? commandTimeout = default, CommandType? commandType = default)
        {
            try
            {
                return WithConnectionAsync<object>(k => k.ExecuteScalarAsync(sql, param, transaction, commandTimeout, commandType));
            }
            catch (Exception ex)
            {
                var x = ex;
            }

            return null;
        }

        // Summary: Execute parameterized SQL that selects a single value
        // Returns: The first cell selected
        public static Task<T>? ExecuteScalarAsync<T>(CommandDefinition command)
        {
            try
            {
                return WithConnectionAsync<T>(k => k.ExecuteScalarAsync<T>(command));
            }
            catch (Exception ex)
            {
                var x = ex;
            }

            return null;
        }

        // Summary: Execute parameterized SQL that selects a single value
        // Returns: The first cell selected
        public static Task<T>? ExecuteScalarAsync<T>(string sql, object? param = null, IDbTransaction? transaction = null,
            int? commandTimeout = default, CommandType? commandType = default)
        {
            try
            {
                return WithConnectionAsync<T>(k => k.ExecuteScalarAsync<T>(sql, param, transaction, commandTimeout, commandType));
            }
            catch (Exception ex)
            {
                var x = ex;
            }

            return null;
        }

        public static IEnumerable<dynamic>? Query(string sql, object? param = null, IDbTransaction? transaction = null,
            bool buffered = true, int? commandTimeout = default, CommandType? commandType = default)
        {
            try
            {
                return WithConnection<dynamic>(k => k.Query(sql, param, transaction, buffered, commandTimeout, commandType));
            }
            catch (Exception ex)
            {
                var x = ex;
            }

            return null;
        }

        // Summary: Executes a single-row query, returning the data typed as per the Type suggested
        // Returns: A sequence of data of the supplied type{} if a basic type (int, string, etc) is
        // queried then the data from the first column in assumed, otherwise an instance is created
        // per row, and a direct column-name===member-name mapping is assumed (case insensitive).
        public static IEnumerable<object>? Query(Type type, string sql, object? param = null, IDbTransaction? transaction = null,
            bool buffered = true, int? commandTimeout = default, CommandType? commandType = default)
        {
            try
            {
                return WithConnection<IEnumerable<object>>(k => k.Query(type, sql, param, transaction, buffered, commandTimeout, commandType));
            }
            catch (Exception ex)
            {
                var x = ex;
            }

            return null;
        }

        // Summary: Executes a query, returning the data typed as per T
        //
        // Returns: A sequence of data of the supplied type{} if a basic type (int, string, etc) is
        // queried then the data from the first column in assumed, otherwise an instance is created
        // per row, and a direct column-name===member-name mapping is assumed (case insensitive).
        //
        // Remarks: the dynamic param may seem a bit odd, but this works around a major usability
        // issue in vs, if it is Object vs completion gets annoying. Eg type new [space] get new object
        public static IEnumerable<T> Query<T>(CommandDefinition command)
        {
            try
            {
                return WithConnection<IEnumerable<T>>(k => k.Query<T>(command));
            }
            catch (Exception ex)
            {
                var x = ex;
            }

            return null;
        }

        // Summary: Executes a query, returning the data typed as per T
        //
        // Returns: A sequence of data of the supplied type{} if a basic type (int, string, etc) is
        // queried then the data from the first column in assumed, otherwise an instance is created
        // per row, and a direct column-name===member-name mapping is assumed (case insensitive).
        public static IEnumerable<T> Query<T>(string sql, object param = null, IDbTransaction transaction = null,
            bool buffered = true, int? commandTimeout = default, CommandType? commandType = default)
        {
            try
            {
                return WithConnection<IEnumerable<T>>(k => k.Query<T>(sql, param, transaction, buffered, commandTimeout, commandType));
            }
            catch (Exception ex)
            {
                var x = ex;
            }

            return null;
        }

        // Summary: Perform a multi mapping query with arbitrary input parameters
        // Parameters: cnn: sql:
        // types: array of types in the record set map: param: transaction: buffered:
        // splitOn: The Field we should split and read the second object from (default: id)
        // commandTimeout: Number of seconds before command execution timeout
        // commandType: Is it a stored proc or a batch? Type parameters: TReturn: The return type
        public static IEnumerable<TReturn> Query<TReturn>(string sql, Type[] types, Func<object[], TReturn> map,
            object param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id",
            int? commandTimeout = default, CommandType? commandType = default)
        {
            try
            {
                return WithConnection<IEnumerable<TReturn>>(k => k.Query<TReturn>(sql, types, map, param, transaction, buffered, splitOn, commandTimeout, commandType));
            }
            catch (Exception ex)
            {
                var x = ex;
            }

            return null;
        }

        // Summary: Maps a query to objects
        // Parameters: cnn: sql: map: param: transaction: buffered:
        // splitOn: The Field we should split and read the second object from (default: id)
        // commandTimeout: Number of seconds before command execution timeout
        // commandType: Is it a stored proc or a batch? Type parameters: TFirst: The first type in
        // the record set
        // TSecond: The second type in the record set
        // TReturn: The return type
        public static IEnumerable<TReturn> Query<TFirst, TSecond, TReturn>(string sql, Func<TFirst, TSecond, TReturn> map,
            object param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id",
            int? commandTimeout = default, CommandType? commandType = default)
        {
            try
            {
                return WithConnection<IEnumerable<TReturn>>(k => k.Query<TFirst, TSecond, TReturn>(sql, map, param, transaction, buffered, splitOn, commandTimeout, commandType));
            }
            catch (Exception ex)
            {
                var x = ex;
            }

            return null;
        }

        // Summary: Maps a query to objects
        // Parameters: cnn: sql: map: param: transaction: buffered:
        // splitOn: The Field we should split and read the second object from (default: id)
        // commandTimeout: Number of seconds before command execution timeout commandType: Type
        // parameters: TFirst: TSecond: TThird: TReturn:
        public static IEnumerable<TReturn> Query<TFirst, TSecond, TThird, TReturn>(string sql,
            Func<TFirst, TSecond, TThird, TReturn> map, object param = null, IDbTransaction transaction = null,
            bool buffered = true, string splitOn = "Id", int? commandTimeout = default,
            CommandType? commandType = default)
        {
            try
            {
                return WithConnection<IEnumerable<TReturn>>(k => k.Query<TFirst, TSecond, TThird, TReturn>(sql, map, param, transaction, buffered, splitOn, commandTimeout, commandType));
            }
            catch (Exception ex)
            {
                var x = ex;
            }

            return null;
        }

        // Summary: Perform a multi mapping query with 4 input parameters
        // Parameters: cnn: sql: map: param: transaction: buffered: splitOn: commandTimeout:
        // commandType: Type parameters: TFirst: TSecond: TThird: TFourth: TReturn:
        public static IEnumerable<TReturn> Query<TFirst, TSecond, TThird, TFourth, TReturn>(string sql,
            Func<TFirst, TSecond, TThird, TFourth, TReturn> map, object param = null, IDbTransaction transaction = null,
            bool buffered = true, string splitOn = "Id", int? commandTimeout = default,
            CommandType? commandType = default)
        {
            try
            {
                return WithConnection<IEnumerable<TReturn>>(k => k.Query<TFirst, TSecond, TThird, TFourth, TReturn>(sql, map, param, transaction, buffered, splitOn, commandTimeout, commandType));
            }
            catch (Exception ex)
            {
                var x = ex;
            }

            return null;
        }

        // Summary: Perform a multi mapping query with 5 input parameters
        // Parameters: cnn: sql: map: param: transaction: buffered: splitOn: commandTimeout:
        // commandType: Type parameters: TFirst: TSecond: TThird: TFourth: TFifth: TReturn:
        public static IEnumerable<TReturn> Query<TFirst, TSecond, TThird, TFourth, TFifth, TReturn>(string sql,
            Func<TFirst, TSecond, TThird, TFourth, TFifth, TReturn> map, object param = null,
            IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id",
            int? commandTimeout = default, CommandType? commandType = default)
        {
            try
            {
                return WithConnection<IEnumerable<TReturn>>(k => k.Query<TFirst, TSecond, TThird, TFourth, TFifth, TReturn>(sql, map, param, transaction, buffered, splitOn, commandTimeout, commandType));
            }
            catch (Exception ex)
            {
                var x = ex;
            }

            return null;
        }

        // Summary: Perform a multi mapping query with 6 input parameters
        // Parameters: cnn: sql: map: param: transaction: buffered: splitOn: commandTimeout:
        // commandType: Type parameters: TFirst: TSecond: TThird: TFourth: TFifth: TSixth: TReturn:
        public static IEnumerable<TReturn> Query<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TReturn>(string sql,
            Func<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TReturn> map, object param = null,
            IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id",
            int? commandTimeout = default, CommandType? commandType = default)
        {
            try
            {
                return WithConnection<IEnumerable<TReturn>>(k => k.Query<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TReturn>(sql, map, param, transaction, buffered, splitOn, commandTimeout, commandType));
            }
            catch (Exception ex)
            {
                var x = ex;
            }

            return null;
        }

        // Summary: Perform a multi mapping query with 7 input parameters
        // Parameters: cnn: sql: map: param: transaction: buffered: splitOn: commandTimeout:
        // commandType: Type parameters: TFirst: TSecond: TThird: TFourth: TFifth: TSixth: TSeventh: TReturn:
        public static IEnumerable<TReturn> Query<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TReturn>(
            string sql, Func<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TReturn> map,
            object param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id",
            int? commandTimeout = default, CommandType? commandType = default)
        {
            try
            {
                return WithConnection<IEnumerable<TReturn>>(k => k.Query<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TReturn>(sql, map, param, transaction, buffered, splitOn, commandTimeout, commandType));
            }
            catch (Exception ex)
            {
                var x = ex;
            }

            return null;
        }

        // Summary: Execute a query asynchronously using .NET 4.5 Task. Remarks:
        // Note: each row can be accessed via "dynamic", or by casting to an IDictionary<string,object>
        public static Task<IEnumerable<dynamic>> QueryAsync(CommandDefinition command)
        {
            try
            {
                return WithConnectionAsync<IEnumerable<dynamic>>(k => k.QueryAsync(command));
            }
            catch (Exception ex)
            {
                var x = ex;
            }

            return Task.FromResult<IEnumerable<dynamic>>(null);
        }

        // Summary: Execute a query asynchronously using .NET 4.5 Task.
        public static Task<IEnumerable<object>> QueryAsync(Type type, CommandDefinition command)
        {
            try
            {
                return WithConnectionAsync<IEnumerable<object>>(k => k.QueryAsync(type, command));
            }
            catch (Exception ex)
            {
                var x = ex;
            }

            return Task.FromResult<IEnumerable<object>>(null);
        }

        // Summary: Execute a query asynchronously using .NET 4.5 Task. Remarks:
        // Note: each row can be accessed via "dynamic", or by casting to an IDictionary<string,object>
        public static Task<IEnumerable<dynamic>> QueryAsync(string sql, object param = null, IDbTransaction transaction = null,
            int? commandTimeout = default, CommandType? commandType = default)
        {
            try
            {
                return WithConnectionAsync<IEnumerable<dynamic>>(k => k.QueryAsync(sql, param, transaction, commandTimeout, commandType));
            }
            catch (Exception ex)
            {
                var x = ex;
            }

            return Task.FromResult<IEnumerable<dynamic>>(null);
        }

        // Summary: Execute a query asynchronously using .NET 4.5 Task.
        public static Task<IEnumerable<object>> QueryAsync(Type type, string sql, object param = null,
            IDbTransaction transaction = null, int? commandTimeout = default,
            CommandType? commandType = default)
        {
            try
            {
                return WithConnectionAsync<IEnumerable<object>>(k => k.QueryAsync(type, sql, param, transaction, commandTimeout, commandType));
            }
            catch (Exception ex)
            {
                var x = ex;
            }

            return Task.FromResult<IEnumerable<object>>(null);
        }

        // Summary: Execute a query asynchronously using .NET 4.5 Task.
        public static Task<IEnumerable<T>> QueryAsync<T>(CommandDefinition command)
        {
            try
            {
                return WithConnectionAsync<IEnumerable<T>>(k => k.QueryAsync<T>(command));
            }
            catch (Exception ex)
            {
                var x = ex;
            }

            return Task.FromResult<IEnumerable<T>>(null);
        }

        // Summary: Execute a query asynchronously using .NET 4.5 Task.
        public static Task<IEnumerable<T>> QueryAsync<T>(string sql, object param = null, IDbTransaction transaction = null,
            int? commandTimeout = default, CommandType? commandType = default)
        {
            try
            {
                return WithConnectionAsync<IEnumerable<T>>(k => k.QueryAsync<T>(sql, param, transaction, commandTimeout, commandType));
            }
            catch (Exception ex)
            {
                var x = ex;
            }

            return Task.FromResult<IEnumerable<T>>(null);
        }

        // Summary: Perform a multi mapping query with arbitrary input parameters
        // Parameters: cnn: sql:
        // types: array of types in the recordset map: param: transaction: buffered:
        // splitOn: The Field we should split and read the second object from (default: id)
        // commandTimeout: Number of seconds before command execution timeout
        // commandType: Is it a stored proc or a batch? Type parameters: TReturn: The return type
        public static Task<IEnumerable<TReturn>> QueryAsync<TReturn>(string sql, Type[] types, Func<object[], TReturn> map,
            object param = null, IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id",
            int? commandTimeout = default, CommandType? commandType = default)
        {
            try
            {
                return WithConnectionAsync<IEnumerable<TReturn>>(k => k.QueryAsync<TReturn>(sql, types, map, param, transaction, buffered, splitOn, commandTimeout, commandType));
            }
            catch (Exception ex)
            {
                var x = ex;
            }

            return Task.FromResult<IEnumerable<TReturn>>(null);
        }

        // Summary: Maps a query to objects
        // Parameters: cnn:
        // splitOn: The field we should split and read the second object from (default: id)
        // command: The command to execute map: Type parameters: TFirst: The first type in the recordset
        // TSecond: The second type in the recordset
        // TReturn: The return type
        public static Task<IEnumerable<TReturn>> QueryAsync<TFirst, TSecond, TReturn>(CommandDefinition command,
            Func<TFirst, TSecond, TReturn> map, string splitOn = "Id")
        {
            try
            {
                return WithConnectionAsync<IEnumerable<TReturn>>(k => k.QueryAsync<TFirst, TSecond, TReturn>(command, map, splitOn));
            }
            catch (Exception ex)
            {
                var x = ex;
            }

            return Task.FromResult<IEnumerable<TReturn>>(null);
        }

        // Summary: Maps a query to objects
        // Parameters: cnn: sql: map: param: transaction: buffered:
        // splitOn: The field we should split and read the second object from (default: id)
        // commandTimeout: Number of seconds before command execution timeout
        // commandType: Is it a stored proc or a batch? Type parameters: TFirst: The first type in
        // the recordset
        // TSecond: The second type in the recordset
        // TReturn: The return type
        public static Task<IEnumerable<TReturn>> QueryAsync<TFirst, TSecond, TReturn>(string sql,
            Func<TFirst, TSecond, TReturn> map, object param = null, IDbTransaction transaction = null,
            bool buffered = true, string splitOn = "Id", int? commandTimeout = default,
            CommandType? commandType = default)
        {
            try
            {
                return WithConnectionAsync<IEnumerable<TReturn>>(k => k.QueryAsync<TFirst, TSecond, TReturn>(sql, map, param, transaction, buffered, splitOn, commandTimeout, commandType));
            }
            catch (Exception ex)
            {
                var x = ex;
            }

            return Task.FromResult<IEnumerable<TReturn>>(null);
        }

        // Summary: Maps a query to objects
        // Parameters: cnn:
        // splitOn: The field we should split and read the second object from (default: id)
        // command: The command to execute map: Type parameters: TFirst: TSecond: TThird: TReturn:
        public static Task<IEnumerable<TReturn>> QueryAsync<TFirst, TSecond, TThird, TReturn>(CommandDefinition command,
            Func<TFirst, TSecond, TThird, TReturn> map, string splitOn = "Id")
        {
            try
            {
                return WithConnectionAsync<IEnumerable<TReturn>>(k => k.QueryAsync<TFirst, TSecond, TThird, TReturn>(command, map, splitOn));
            }
            catch (Exception ex)
            {
                var x = ex;
            }

            return Task.FromResult<IEnumerable<TReturn>>(null);
        }

        // Summary: Maps a query to objects
        // Parameters: cnn: sql: map: param: transaction: buffered:
        // splitOn: The Field we should split and read the second object from (default: id)
        // commandTimeout: Number of seconds before command execution timeout commandType: Type
        // parameters: TFirst: TSecond: TThird: TReturn:
        public static Task<IEnumerable<TReturn>> QueryAsync<TFirst, TSecond, TThird, TReturn>(string sql,
            Func<TFirst, TSecond, TThird, TReturn> map, object param = null, IDbTransaction transaction = null,
            bool buffered = true, string splitOn = "Id", int? commandTimeout = default,
            CommandType? commandType = default)
        {
            try
            {
                return WithConnectionAsync<IEnumerable<TReturn>>(k => k.QueryAsync<TFirst, TSecond, TThird, TReturn>(sql, map, param, transaction, buffered, splitOn, commandTimeout, commandType));
            }
            catch (Exception ex)
            {
                var x = ex;
            }

            return Task.FromResult<IEnumerable<TReturn>>(null);
        }

        // Summary: Perform a multi mapping query with 4 input parameters
        // Parameters: cnn:
        // splitOn: The field we should split and read the second object from (default: id)
        // command: The command to execute map: Type parameters: TFirst: TSecond: TThird: TFourth: TReturn:
        public static Task<IEnumerable<TReturn>> QueryAsync<TFirst, TSecond, TThird, TFourth, TReturn>(
            CommandDefinition command, Func<TFirst, TSecond, TThird, TFourth, TReturn> map, string splitOn = "Id")
        {
            try
            {
                return WithConnectionAsync<IEnumerable<TReturn>>(k => k.QueryAsync<TFirst, TSecond, TThird, TFourth, TReturn>(command, map, splitOn));
            }
            catch (Exception ex)
            {
                var x = ex;
            }

            return Task.FromResult<IEnumerable<TReturn>>(null);
        }

        // Summary: Perform a multi mapping query with 4 input parameters
        // Parameters: cnn: sql: map: param: transaction: buffered: splitOn: commandTimeout:
        // commandType: Type parameters: TFirst: TSecond: TThird: TFourth: TReturn:
        public static Task<IEnumerable<TReturn>> QueryAsync<TFirst, TSecond, TThird, TFourth, TReturn>(string sql,
            Func<TFirst, TSecond, TThird, TFourth, TReturn> map, object param = null, IDbTransaction transaction = null,
            bool buffered = true, string splitOn = "Id", int? commandTimeout = default,
            CommandType? commandType = default)
        {
            try
            {
                return WithConnectionAsync<IEnumerable<TReturn>>(k => k.QueryAsync<TFirst, TSecond, TThird, TFourth, TReturn>(sql, map, param, transaction, buffered, splitOn, commandTimeout, commandType));
            }
            catch (Exception ex)
            {
                var x = ex;
            }

            return Task.FromResult<IEnumerable<TReturn>>(null);
        }

        // Summary: Perform a multi mapping query with 5 input parameters
        public static Task<IEnumerable<TReturn>> QueryAsync<TFirst, TSecond, TThird, TFourth, TFifth, TReturn>(
            CommandDefinition command, Func<TFirst, TSecond, TThird, TFourth, TFifth, TReturn> map,
            string splitOn = "Id")
        {
            try
            {
                return WithConnectionAsync<IEnumerable<TReturn>>(k => k.QueryAsync<TFirst, TSecond, TThird, TFourth, TFifth, TReturn>(command, map, splitOn));
            }
            catch (Exception ex)
            {
                var x = ex;
            }

            return Task.FromResult<IEnumerable<TReturn>>(null);
        }

        // Summary: Perform a multi mapping query with 5 input parameters
        public static Task<IEnumerable<TReturn>> QueryAsync<TFirst, TSecond, TThird, TFourth, TFifth, TReturn>(string sql,
            Func<TFirst, TSecond, TThird, TFourth, TFifth, TReturn> map, object param = null,
            IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id",
            int? commandTimeout = default, CommandType? commandType = default)
        {
            try
            {
                return WithConnectionAsync<IEnumerable<TReturn>>(k => k.QueryAsync<TFirst, TSecond, TThird, TFourth, TFifth, TReturn>(sql, map, param, transaction, buffered, splitOn, commandTimeout, commandType));
            }
            catch (Exception ex)
            {
                var x = ex;
            }

            return Task.FromResult<IEnumerable<TReturn>>(null);
        }

        // Summary: Perform a multi mapping query with 6 input parameters
        public static Task<IEnumerable<TReturn>> QueryAsync<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TReturn>(
            CommandDefinition command, Func<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TReturn> map,
            string splitOn = "Id")
        {
            try
            {
                return WithConnectionAsync<IEnumerable<TReturn>>(k => k.QueryAsync<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TReturn>(command, map, splitOn));
            }
            catch (Exception ex)
            {
                var x = ex;
            }

            return Task.FromResult<IEnumerable<TReturn>>(null);
        }

        // Summary: Perform a multi mapping query with 6 input parameters
        public static Task<IEnumerable<TReturn>> QueryAsync<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TReturn>(
            string sql, Func<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TReturn> map, object param = null,
            IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id",
            int? commandTimeout = default, CommandType? commandType = default)
        {
            try
            {
                return WithConnectionAsync<IEnumerable<TReturn>>(k => k.QueryAsync<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TReturn>(sql, map, param, transaction, buffered, splitOn, commandTimeout, commandType));
            }
            catch (Exception ex)
            {
                var x = ex;
            }

            return Task.FromResult<IEnumerable<TReturn>>(null);
        }

        // Summary: Perform a multi mapping query with 7 input parameters
        public static Task<IEnumerable<TReturn>> QueryAsync
            <TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TReturn>(CommandDefinition command,
                Func<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TReturn> map, string splitOn = "Id")
        {
            try
            {
                return WithConnectionAsync<IEnumerable<TReturn>>(k => k.QueryAsync<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TReturn>(command, map, splitOn));
            }
            catch (Exception ex)
            {
                var x = ex;
            }

            return Task.FromResult<IEnumerable<TReturn>>(null);
        }

        // Summary: Perform a multi mapping query with 7 input parameters
        public static Task<IEnumerable<TReturn>> QueryAsync
            <TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TReturn>(string sql,
                Func<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TReturn> map, object param = null,
                IDbTransaction transaction = null, bool buffered = true, string splitOn = "Id",
                int? commandTimeout = default, CommandType? commandType = default)
        {
            try
            {
                return WithConnectionAsync<IEnumerable<TReturn>>(k => k.QueryAsync<TFirst, TSecond, TThird, TFourth, TFifth, TSixth, TSeventh, TReturn>(sql, map, param, transaction, buffered, splitOn, commandTimeout, commandType));
            }
            catch (Exception ex)
            {
                var x = ex;
            }

            return Task.FromResult<IEnumerable<TReturn>>(null);
        }

        // Summary: Return a dynamic object with properties matching the columns Remarks:
        // Note: the row can be accessed via "dynamic", or by casting to an IDictionary<string,object>
        public static dynamic QueryFirst(string sql, object param = null, IDbTransaction transaction = null,
            int? commandTimeout = default, CommandType? commandType = default)
        {
            try
            {
                return WithConnection<dynamic>(k => k.QueryFirst(sql, param, transaction, commandTimeout, commandType));
            }
            catch (Exception ex)
            {
                var x = ex;
            }

            return null;
        }

        // Summary: Executes a single-row query, returning the data typed as per the Type suggested
        // Returns: A sequence of data of the supplied type{} if a basic type (int, string, etc) is
        // queried then the data from the first column in assumed, otherwise an instance is created
        // per row, and a direct column-name===member-name mapping is assumed (case insensitive).
        public static object QueryFirst(Type type, string sql, object param = null, IDbTransaction transaction = null,
            int? commandTimeout = default, CommandType? commandType = default)
        {
            try
            {
                return WithConnection<object>(k => k.QueryFirst(type, sql, param, transaction, commandTimeout, commandType));
            }
            catch (Exception ex)
            {
                var x = ex;
            }

            return null;
        }

        // Summary: Executes a query, returning the data typed as per T
        // Returns: A single instance or null of the supplied type{} if a basic type (int, string,
        // etc) is queried then the data from the first column in assumed, otherwise an instance is
        //      created per row, and a direct column-name===member-name mapping is assumed (case insensitive).
        //
        // Remarks: the dynamic param may seem a bit odd, but this works around a major usability
        // issue in vs, if it is Object vs completion gets annoying. Eg type new [space] get new object
        public static T QueryFirst<T>(CommandDefinition command)
        {
            try
            {
                return WithConnection<T>(k => k.QueryFirst<T>(command));
            }
            catch (Exception ex)
            {
                var x = ex;
            }

            return default;
        }

        // Summary: Executes a single-row query, returning the data typed as per T
        //
        // Returns: A sequence of data of the supplied type{} if a basic type (int, string, etc) is
        // queried then the data from the first column in assumed, otherwise an instance is created
        // per row, and a direct column-name===member-name mapping is assumed (case insensitive).
        public static T QueryFirst<T>(string sql, object param = null, IDbTransaction transaction = null,
            int? commandTimeout = default, CommandType? commandType = default)
        {
            try
            {
                return WithConnection<T>(k => k.QueryFirst<T>(sql, param, transaction, commandTimeout, commandType));
            }
            catch (Exception ex)
            {
                var x = ex;
            }

            return default;
        }

        // Summary: Execute a single-row query asynchronously using .NET 4.5 Task.
        //
        // Remarks:
        // Note: the row can be accessed via "dynamic", or by casting to an IDictionary<string,object>
        public static Task<dynamic> QueryFirstAsync(CommandDefinition command)
        {
            try
            {
                return WithConnectionAsync<dynamic>(k => k.QueryFirstAsync(command));
            }
            catch (Exception ex)
            {
                var x = ex;
            }

            return Task.FromResult<dynamic>(null);
        }

        // Summary: Execute a single-row query asynchronously using .NET 4.5 Task.
        public static Task<object> QueryFirstAsync(Type type, CommandDefinition command)
        {
            try
            {
                return WithConnectionAsync<object>(k => k.QueryFirstAsync(type, command));
            }
            catch (Exception ex)
            {
                var x = ex;
            }

            return Task.FromResult<object>(null);
        }

        // Summary: Execute a single-row query asynchronously using .NET 4.5 Task.
        public static Task<object> QueryFirstAsync(Type type, string sql, object param = null,
            IDbTransaction transaction = null, int? commandTimeout = default,
            CommandType? commandType = default)
        {
            try
            {
                return WithConnectionAsync<object>(k => k.QueryFirstAsync(type, sql, param, transaction, commandTimeout, commandType));
            }
            catch (Exception ex)
            {
                var x = ex;
            }

            return Task.FromResult<object>(null);
        }

        // Summary: Execute a single-row query asynchronously using .NET 4.5 Task.
        public static Task<T> QueryFirstAsync<T>(string sql, object param = null, IDbTransaction transaction = null,
            int? commandTimeout = default, CommandType? commandType = default)
        {
            try
            {
                return WithConnectionAsync<T>(k => k.QueryFirstAsync<T>(sql, param, transaction, commandTimeout, commandType));
            }
            catch (Exception ex)
            {
                var x = ex;
            }

            return Task.FromResult<T>(default);
        }

        // Summary: Return a dynamic object with properties matching the columns Remarks:
        // Note: the row can be accessed via "dynamic", or by casting to an IDictionary<string,object>
        public static dynamic QueryFirstOrDefault(string sql, object param = null, IDbTransaction transaction = null,
            int? commandTimeout = default, CommandType? commandType = default)
        {
            try
            {
                return WithConnection<dynamic>(k => k.QueryFirstOrDefault(sql, param, transaction, commandTimeout, commandType));
            }
            catch (Exception ex)
            {
                var x = ex;
            }

            return null;
        }

        // Summary: Executes a single-row query, returning the data typed as per the Type suggested
        //
        // Returns: A sequence of data of the supplied type{} if a basic type (int, string, etc) is
        // queried then the data from the first column in assumed, otherwise an instance is created
        // per row, and a direct column-name===member-name mapping is assumed (case insensitive).
        public static object QueryFirstOrDefault(Type type, string sql, object param = null, IDbTransaction transaction = null,
            int? commandTimeout = default, CommandType? commandType = default)
        {
            try
            {
                return WithConnection<object>(k => k.QueryFirstOrDefault(type, sql, param, transaction, commandTimeout, commandType));
            }
            catch (Exception ex)
            {
                var x = ex;
            }

            return null;
        }

        // Summary: Executes a query, returning the data typed as per T
        //
        // Returns: A single or null instance of the supplied type{} if a basic type (int, string,
        // etc) is queried then the data from the first column in assumed, otherwise an instance is
        //      created per row, and a direct column-name===member-name mapping is assumed (case insensitive).
        //
        // Remarks: the dynamic param may seem a bit odd, but this works around a major usability
        // issue in vs, if it is Object vs completion gets annoying. Eg type new [space] get new object
        public static T QueryFirstOrDefault<T>(CommandDefinition command)
        {
            try
            {
                return WithConnection<T>(k => k.QueryFirstOrDefault<T>(command));
            }
            catch (Exception ex)
            {
                var x = ex;
            }

            return default;
        }

        // Summary: Executes a single-row query, returning the data typed as per T
        //
        // Returns: A sequence of data of the supplied type{} if a basic type (int, string, etc) is
        // queried then the data from the first column in assumed, otherwise an instance is created
        // per row, and a direct column-name===member-name mapping is assumed (case insensitive).
        public static T QueryFirstOrDefault<T>(string sql, object param = null, IDbTransaction transaction = null,
            int? commandTimeout = default, CommandType? commandType = default)
        {
            try
            {
                return WithConnection<T>(k => k.QueryFirstOrDefault<T>(sql, param, transaction, commandTimeout, commandType));
            }
            catch (Exception ex)
            {
                var x = ex;
            }

            return default;
        }

        // Summary: Execute a single-row query asynchronously using .NET 4.5 Task. Remarks:
        // Note: the row can be accessed via "dynamic", or by casting to an IDictionary<string,object>
        public static Task<dynamic> QueryFirstOrDefaultAsync(CommandDefinition command)
        {
            try
            {
                return WithConnectionAsync<dynamic>(k => k.QueryFirstOrDefaultAsync(command));
            }
            catch (Exception ex)
            {
                var x = ex;
            }

            return Task.FromResult<dynamic>(null);
        }

        // Summary: Execute a single-row query asynchronously using .NET 4.5 Task.
        public static Task<object> QueryFirstOrDefaultAsync(Type type, CommandDefinition command)
        {
            try
            {
                return WithConnectionAsync<object>(k => k.QueryFirstOrDefaultAsync(type, command));
            }
            catch (Exception ex)
            {
                var x = ex;
            }

            return Task.FromResult<object>(null);
        }

        // Summary: Execute a single-row query asynchronously using .NET 4.5 Task.
        public static Task<object> QueryFirstOrDefaultAsync(Type type, string sql, object param = null,
            IDbTransaction transaction = null, int? commandTimeout = default,
            CommandType? commandType = default)
        {
            try
            {
                return WithConnectionAsync<object>(k => k.QueryFirstOrDefaultAsync(type, sql, param, transaction, commandTimeout, commandType));
            }
            catch (Exception ex)
            {
                var x = ex;
            }

            return Task.FromResult<object>(null);
        }

        // Summary: Execute a single-row query asynchronously using .NET 4.5 Task.
        public static Task<T> QueryFirstOrDefaultAsync<T>(string sql, object param = null, IDbTransaction transaction = null,
            int? commandTimeout = default, CommandType? commandType = default)
        {
            try
            {
                return WithConnectionAsync<T>(k => k.QueryFirstOrDefaultAsync<T>(sql, param, transaction, commandTimeout, commandType));
            }
            catch (Exception ex)
            {
                var x = ex;
            }

            return Task.FromResult<T>(default);
        }

        // Summary: Execute a command that returns multiple result sets, and access each in turn
        public static SqlMapper.GridReader QueryMultiple(CommandDefinition command)
        {
            try
            {
                return WithConnection<SqlMapper.GridReader>(k => k.QueryMultiple(command));
            }
            catch (Exception ex)
            {
                var x = ex;
            }

            return null;
        }

        // Summary: Execute a command that returns multiple result sets, and access each in turn
        public static SqlMapper.GridReader QueryMultiple(string sql, object param = null, IDbTransaction transaction = null,
            int? commandTimeout = default, CommandType? commandType = default)
        {
            try
            {
                return WithConnection<SqlMapper.GridReader>(k => k.QueryMultiple(sql, param, transaction, commandTimeout, commandType));
            }
            catch (Exception ex)
            {
                var x = ex;
            }

            return null;
        }

        // Summary: Execute a command that returns multiple result sets, and access each in turn
        public static Task<SqlMapper.GridReader> QueryMultipleAsync(CommandDefinition command)
        {
            try
            {
                return WithConnectionAsync<SqlMapper.GridReader>(k => k.QueryMultipleAsync(command));
            }
            catch (Exception ex)
            {
                var x = ex;
            }

            return Task.FromResult<SqlMapper.GridReader>(null);
        }

        // Summary: Execute a command that returns multiple result sets, and access each in turn
        public static Task<SqlMapper.GridReader> QueryMultipleAsync(string sql, object param = null,
            IDbTransaction transaction = null, int? commandTimeout = default,
            CommandType? commandType = default)
        {
            try
            {
                return WithConnectionAsync<SqlMapper.GridReader>(k => k.QueryMultipleAsync(sql, param, transaction, commandTimeout, commandType));
            }
            catch (Exception ex)
            {
                var x = ex;
            }

            return Task.FromResult<SqlMapper.GridReader>(null);
        }

        // Summary: Return a dynamic object with properties matching the columns Remarks:
        // Note: the row can be accessed via "dynamic", or by casting to an IDictionary<string,object>
        public static dynamic QuerySingle(string sql, object param = null, IDbTransaction transaction = null,
            int? commandTimeout = default, CommandType? commandType = default)
        {
            try
            {
                return WithConnection<dynamic>(k => k.QuerySingle(sql, param, transaction, commandTimeout, commandType));
            }
            catch (Exception ex)
            {
                var x = ex;
            }

            return null;
        }

        // Summary: Executes a single-row query, returning the data typed as per the Type suggested
        //
        // Returns: A sequence of data of the supplied type{} if a basic type (int, string, etc) is
        // queried then the data from the first column in assumed, otherwise an instance is created
        // per row, and a direct column-name===member-name mapping is assumed (case insensitive).
        public static object QuerySingle(Type type, string sql, object param = null, IDbTransaction transaction = null,
            int? commandTimeout = default, CommandType? commandType = default)
        {
            try
            {
                return WithConnection<object>(k => k.QuerySingle(type, sql, param, transaction, commandTimeout, commandType));
            }
            catch (Exception ex)
            {
                var x = ex;
            }

            return null;
        }

        // Summary: Executes a query, returning the data typed as per T
        //
        // Returns: A single instance of the supplied type{} if a basic type (int, string, etc) is
        // queried then the data from the first column in assumed, otherwise an instance is created
        // per row, and a direct column-name===member-name mapping is assumed (case insensitive).
        //
        // Remarks: the dynamic param may seem a bit odd, but this works around a major usability
        // issue in vs, if it is Object vs completion gets annoying. Eg type new [space] get new object
        public static T QuerySingle<T>(CommandDefinition command)
        {
            try
            {
                return WithConnection<T>(k => k.QuerySingle<T>(command));
            }
            catch (Exception ex)
            {
                var x = ex;
            }

            return default;
        }

        // Summary: Executes a single-row query, returning the data typed as per T
        //
        // Returns: A sequence of data of the supplied type{} if a basic type (int, string, etc) is
        // queried then the data from the first column in assumed, otherwise an instance is created
        // per row, and a direct column-name===member-name mapping is assumed (case insensitive).
        public static T QuerySingle<T>(string sql, object param = null, IDbTransaction transaction = null,
            int? commandTimeout = default, CommandType? commandType = default)
        {
            try
            {
                return WithConnection<T>(k => k.QuerySingle<T>(sql, param, transaction, commandTimeout, commandType));
            }
            catch (Exception ex)
            {
                var x = ex;
            }

            return default;
        }

        // Summary: Execute a single-row query asynchronously using .NET 4.5 Task.
        //
        // Remarks:
        // Note: the row can be accessed via "dynamic", or by casting to an IDictionary<string,object>
        public static Task<dynamic> QuerySingleAsync(CommandDefinition command)
        {
            try
            {
                return WithConnectionAsync<dynamic>(k => k.QuerySingleAsync(command));
            }
            catch (Exception ex)
            {
                var x = ex;
            }

            return Task.FromResult<dynamic>(null);
        }

        // Summary: Execute a single-row query asynchronously using .NET 4.5 Task.
        public static Task<object> QuerySingleAsync(Type type, CommandDefinition command)
        {
            try
            {
                return WithConnectionAsync<object>(k => k.QuerySingleAsync(type, command));
            }
            catch (Exception ex)
            {
                var x = ex;
            }

            return Task.FromResult<object>(null);
        }

        // Summary: Execute a single-row query asynchronously using .NET 4.5 Task.
        public static Task<object> QuerySingleAsync(Type type, string sql, object param = null,
            IDbTransaction transaction = null, int? commandTimeout = default,
            CommandType? commandType = default)
        {
            try
            {
                return WithConnectionAsync<object>(k => k.QuerySingleAsync(type, sql, param, transaction, commandTimeout, commandType));
            }
            catch (Exception ex)
            {
                var x = ex;
            }

            return Task.FromResult<object>(null);
        }

        // Summary: Execute a single-row query asynchronously using .NET 4.5 Task.
        public static Task<T> QuerySingleAsync<T>(string sql, object param = null, IDbTransaction transaction = null,
            int? commandTimeout = default, CommandType? commandType = default)
        {
            try
            {
                return WithConnectionAsync<T>(k => k.QuerySingleAsync<T>(sql, param, transaction, commandTimeout, commandType));
            }
            catch (Exception ex)
            {
                var x = ex;
            }

            return Task.FromResult<T>(default);
        }

        // Summary: Return a dynamic object with properties matching the columns
        //
        // Remarks:
        // Note: the row can be accessed via "dynamic", or by casting to an IDictionary<string,object>
        public static dynamic QuerySingleOrDefault(string sql, object param = null, IDbTransaction transaction = null,
            int? commandTimeout = default, CommandType? commandType = default)
        {
            try
            {
                return WithConnection<dynamic>(k => k.QuerySingleOrDefault(sql, param, transaction, commandTimeout, commandType));
            }
            catch (Exception ex)
            {
                var x = ex;
            }

            return null;
        }

        // Summary: Executes a single-row query, returning the data typed as per the Type suggested
        //
        // Returns: A sequence of data of the supplied type{} if a basic type (int, string, etc) is
        // queried then the data from the first column in assumed, otherwise an instance is created
        // per row, and a direct column-name===member-name mapping is assumed (case insensitive).
        public static object QuerySingleOrDefault(Type type, string sql, object param = null, IDbTransaction transaction = null,
            int? commandTimeout = default, CommandType? commandType = default)
        {
            try
            {
                return WithConnection<object>(k => k.QuerySingleOrDefault(type, sql, param, transaction, commandTimeout, commandType));
            }
            catch (Exception ex)
            {
                var x = ex;
            }

            return null;
        }

        // Summary: Executes a query, returning the data typed as per T
        //
        // Returns: A single instance of the supplied type{} if a basic type (int, string, etc) is
        // queried then the data from the first column in assumed, otherwise an instance is created
        // per row, and a direct column-name===member-name mapping is assumed (case insensitive).
        //
        // Remarks: the dynamic param may seem a bit odd, but this works around a major usability
        // issue in vs, if it is Object vs completion gets annoying. Eg type new [space] get new object
        public static T QuerySingleOrDefault<T>(CommandDefinition command)
        {
            try
            {
                return WithConnection<T>(k => k.QuerySingleOrDefault<T>(command));
            }
            catch (Exception ex)
            {
                var x = ex;
            }

            return default;
        }

        // Summary: Executes a single-row query, returning the data typed as per T
        //
        // Returns: A sequence of data of the supplied type{} if a basic type (int, string, etc) is
        // queried then the data from the first column in assumed, otherwise an instance is created
        // per row, and a direct column-name===member-name mapping is assumed (case insensitive).
        public static T QuerySingleOrDefault<T>(string sql, object param = null, IDbTransaction transaction = null,
            int? commandTimeout = default, CommandType? commandType = default)
        {
            try
            {
                return WithConnection<T>(k => k.QuerySingleOrDefault<T>(sql, param, transaction, commandTimeout, commandType));
            }
            catch (Exception ex)
            {
                var x = ex;
            }

            return default;
        }

        // Summary: Execute a single-row query asynchronously using .NET 4.5 Task.
        //
        // Remarks:
        // Note: the row can be accessed via "dynamic", or by casting to an IDictionary<string,object>
        public static Task<dynamic> QuerySingleOrDefaultAsync(CommandDefinition command)
        {
            try
            {
                return WithConnectionAsync<dynamic>(k => k.QuerySingleOrDefaultAsync(command));
            }
            catch (Exception ex)
            {
                var x = ex;
            }

            return Task.FromResult<dynamic>(null);
        }

        // Summary: Execute a single-row query asynchronously using .NET 4.5 Task.
        public static Task<object> QuerySingleOrDefaultAsync(Type type, CommandDefinition command)
        {
            try
            {
                return WithConnectionAsync<object>(k => k.QuerySingleOrDefaultAsync(command));
            }
            catch (Exception ex)
            {
                var x = ex;
            }

            return Task.FromResult<object>(null);
        }

        // Summary: Execute a single-row query asynchronously using .NET 4.5 Task.
        public static Task<object> QuerySingleOrDefaultAsync(Type type, string sql, object param = null,
            IDbTransaction transaction = null, int? commandTimeout = default,
            CommandType? commandType = default)
        {
            try
            {
                return WithConnectionAsync<object>(k => k.QuerySingleOrDefaultAsync(type, sql, param, transaction, commandTimeout, commandType));
            }
            catch (Exception ex)
            {
                var x = ex;
            }

            return Task.FromResult<object>(null);
        }

        // Summary: Execute a single-row query asynchronously using .NET 4.5 Task.
        public static Task<T> QuerySingleOrDefaultAsync<T>(string sql, object param = null, IDbTransaction transaction = null,
            int? commandTimeout = default, CommandType? commandType = default)
        {
            try
            {
                return WithConnectionAsync<T>(k => k.QuerySingleOrDefaultAsync<T>(sql, param, transaction, commandTimeout, commandType));
            }
            catch (Exception ex)
            {
                var x = ex;
            }

            return Task.FromResult<T>(default);
        }

        private static dynamic Execute(Func<SqlConnection, SqlTransaction, dynamic> action)
        {
            using (var con = new SqlConnection(_connection))
            {
                con.Open();
                var trans = con.BeginTransaction();
                var result = action(con, trans);
                trans.Commit();
                return result;
            }
        }

        private static dynamic Execute(Func<SqlConnection, dynamic> action)
        {
            using (var con = new SqlConnection(_connection))
            {
                con.Open();
                var result = action(con);
                return result;
            }
        }

        private static Task<dynamic> ExecuteAsync(Func<SqlConnection, SqlTransaction, dynamic> action)
        {
            using (var con = new SqlConnection(_connection))
            {
                con.Open();
                var trans = con.BeginTransaction();
                var result = action(con, trans);
                trans.Commit();
                return result;
            }
        }

        private static Task<dynamic> ExecuteAsync(Func<SqlConnection, dynamic> action)
        {
            using (var con = new SqlConnection(_connection))
            {
                con.Open();
                var result = action(con);
                return result;
            }
        }

        #endregion Wrapped and Adjusted Dapper Implementations

        #region WithConnection System

        internal static T WithConnection<T>(Func<IDbConnection, T> getData)
        {
            try
            {
                using (var connection = new SqlConnection(_connection))
                {
                    connection.Open();
                    return getData(connection);
                }
            }
            catch (TimeoutException ex)
            {
                throw new Exception($"WithConnection() SQL Timeout -Static.T \n{ex}");
            }
            catch (SqlException ex)
            {
                throw new Exception($"WithConnection() SQL Exception (not a Timeout) -Static.T \n{ex}");
            }
        }

        // Use for buffered queries that return a type
        internal static async Task<T> WithConnectionAsync<T>(Func<IDbConnection, Task<T>> getData)
        {
            try
            {
                using (var connection = new SqlConnection(_connection))
                {
                    await connection.OpenAsync().ConfigureAwait(false);
                    return await getData(connection).ConfigureAwait(false);
                }
            }
            catch (TimeoutException ex)
            {
                throw new Exception($"WithConnectionAsync() SQL Timeout -Static.Task<T> \n{ex}");
            }
            catch (SqlException ex)
            {
                throw new Exception($"WithConnectionAsync() SQL Exception (not a Timeout) -Static.Task<T> \n{ex}");
            }
        }

        // Use for buffered queries that do not return a type
        internal static async Task WithConnectionAsync(Func<IDbConnection, Task> getData)
        {
            try
            {
                using (var connection = new SqlConnection(_connection))
                {
                    await connection.OpenAsync().ConfigureAwait(false);
                    await getData(connection).ConfigureAwait(false);
                }
            }
            catch (TimeoutException ex)
            {
                throw new Exception($"WithConnectionAsync() SQL Timeout -Static.Task \n{ex}");
            }
            catch (SqlException ex)
            {
                throw new Exception($"WithConnectionAsync() SQL Exception (not a Timeout) -Static.Task \n{ex}");
            }
        }

        // Use for non-buffered queries that return a type
        internal static async Task<TResult> WithConnectionAsync<TRead, TResult>(Func<IDbConnection, Task<TRead>> getData, Func<TRead, Task<TResult>> process)
        {
            try
            {
                using (var connection = new SqlConnection(_connection))
                {
                    await connection.OpenAsync().ConfigureAwait(false);
                    var data = await getData(connection).ConfigureAwait(false);
                    return await process(data).ConfigureAwait(false);
                }
            }
            catch (TimeoutException ex)
            {
                throw new Exception($"WithConnectionAsync() SQL Timeout -Static.Task<TResult> \n{ex}");
            }
            catch (SqlException ex)
            {
                throw new Exception($"WithConnectionAsync() SQL Exception (not a Timeout) -Static.Task<TResult> \n{ex}");
            }
        }

        #endregion WithConnection System

        #region Local Utilities

        internal static void FlushCacheItem(string modelName)
        {
            var isOk = ModelStore.TryRemove(modelName, out _);
        }

        internal static void FlushCache()
        {
            ModelStore.Clear();
        }

        internal static void RefreshCacheItem<T>() where T : class, IRepoModel
        {
            FlushCacheItem(typeof(T).Name);
            ModelInfo mi = new ModelParser<T>().ModelInfo();
            var isOk = ModelStore.TryAdd(mi.TableName, mi);
        }

        private static string LambdaExtractSqlWhere<T>(SqlLam<T> sqlamda) where T : class, IRepoModel
        {
            // Extract a 'WHERE' statement out of the SqlLam object
            var qryString = sqlamda.SqlBuilder.QueryString;
            int start = qryString.IndexOf("Where", StringComparison.InvariantCultureIgnoreCase);
            string whereStmt = qryString.Substring(start).Trim();
            return CleanWhereClause(whereStmt);
        }

        private static string LamdaExtractSqlFrom<T>(SqlLam<T> sqlLambda) where T : class, IRepoModel
        {
            // Extract a 'FROM' statement out of the SqlLam object
            var qryString = sqlLambda.SqlBuilder.QueryString;
            int start = qryString.IndexOf("From", StringComparison.InvariantCultureIgnoreCase);
            string fromStmt = qryString.Substring(start).Trim();
            return fromStmt;
        }

        private static dynamic GetModelDetail<T>(ModelDetail modelItem, ModelInfo? modelInfo = null) where T : class, IRepoModel
        {
            var mi = modelInfo ?? TryGetCacheItem<T>();

            if (mi == null) throw new ArgumentOutOfRangeException();

            switch (modelItem)
            {
                case ModelDetail.TableName:
                    return mi.TableName;

                case ModelDetail.PkName:
                    return mi.PkName;

                case ModelDetail.InsertScript:
                    return mi.InsertScript;

                case ModelDetail.UpdateScript:
                    return mi.UpdateScript;

                case ModelDetail.UpdateWhereScript:
                    return mi.UpdateWhereScript;

                case ModelDetail.DeleteScript:
                    return mi.DeleteScript;

                case ModelDetail.DeleteByIdScript:
                    return mi.DeleteByIdScript;

                case ModelDetail.DeleteWhereScript:
                    return mi.DeleteWhereScript;

                case ModelDetail.ExistsScript:
                    return mi.ExistsScript;

                case ModelDetail.ExistsWhereScript:
                    return mi.ExistsWhereScript;

                case ModelDetail.PageScript:
                    return mi.PageScript;

                case ModelDetail.PageWhereScript:
                    return mi.PageWhereScript;

                case ModelDetail.TopScript:
                    return mi.TopScript;

                case ModelDetail.TopWhereScript:
                    return mi.TopWhereScript;

                case ModelDetail.CountScript:
                    return mi.CountScript;

                case ModelDetail.CountWhereScript:
                    return mi.CountWhereScript;

                case ModelDetail.SelectAll:
                    return mi.SelectAllScript;

                case ModelDetail.SelectById:
                    return mi.SelectByIdScript;

                case ModelDetail.SelectWhereScript:
                    return mi.SelectWhereScript;

                case ModelDetail.PkIsNumeric:
                    return mi.PkIsNumeric;

                case ModelDetail.TableColumns:
                    return mi.TableColumns;

                case ModelDetail.PropItems:
                    return mi.PropItems;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Try to get the cached model item.
        /// <para>If not in cache, the model is decomposed and added to the cache.</para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        private static ModelInfo TryGetCacheItem<T>() where T : class, IRepoModel
        {
            var modelName = typeof(T).Name;

            // Check the dictionary for the object
            ModelStore.TryGetValue(modelName, out var modelVal);

            // Return cached model
            if (modelVal != null)
                return modelVal;

            // Otherwise...
            // Process Model and Add to Dictionary
            var cInfo = new ModelParser<T>();

            modelVal = cInfo.ModelInfo();
            ModelStore.TryAdd(modelName, modelVal);

            // Return model
            return modelVal;
        }

        private static string CleanWhereClause(string whereCondition)
        {
            if (string.IsNullOrWhiteSpace(whereCondition))
                return string.Empty;

            var data = whereCondition.Trim().Split();

            if (data.First().Trim().ToLower() != "where")
                return whereCondition;

            // Remove the first element and return joined string
            return string.Join(" ", data.Skip(1));
        }

        public static dynamic GetPropValue(object model, string propName)
        {
            return model.GetType().GetProperty(propName)?.GetValue(model);
        }

        public static dynamic SimpleCast(dynamic obj, Type castTo)
        {
            return Convert.ChangeType(obj, castTo);
        }

        private static ModelQueryPkg GetActionQueryPkg<T>(T model, ModelDetail scriptItem) where T : class, IRepoModel
        {
            ModelInfo modInfo = TryGetCacheItem<T>();
            string qry = GetModelDetail<T>(scriptItem, modInfo);
            var pkIdVal = SimplePkCast(model.GetPropValue<T>(modInfo.PkName), modInfo.PkIsNumeric);

            return new ModelQueryPkg
            {
                pkVal = pkIdVal,
                qryScript = qry
            };
        }

        /// <summary>
        /// Returns a long or a string value
        /// </summary>
        /// <param name="objValue"></param>
        /// <param name="isNumeric"></param>
        /// <returns></returns>
        public static dynamic SimplePkCast(dynamic objValue, bool isNumeric)
        {
            if (isNumeric)
                return Convert.ChangeType(objValue, typeof(long));

            return Convert.ChangeType(objValue, typeof(string));
        }

        public static bool IsNullEmptyOrZero(dynamic value)
        {
            // If null just bail
            if (value == null) return true;

            // If we have a value type..
            if (value.GetType().BaseType.IsValueType || value.GetType().BaseType.Name == "ValueType")
                return value == 0;

            // Try to convert to a string..
            if (Convert.ToString(value) != null)
                return string.IsNullOrWhiteSpace(value.ToString());

            return false;
        }

        public static bool NotNullEmptyOrZero(dynamic value)
        {
            return !IsNullEmptyOrZero(value);
        }

        internal static object Query<T>(Action<object> p)
        {
            throw new NotImplementedException();
        }

        #endregion Local Utilities

    }
}
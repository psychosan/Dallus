using System.Collections.Concurrent;
using System.Data;
using System.Reflection;
using Dapper;
using SqlLambda.ValueObjects;

namespace Dallus
{
    internal class PocoTest:IRepoModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    internal class Scratch
    {
        public void DoSomething1()
        {
            
        }

        public void DlinkExp()
        {
            
            var x = new PocoTest();

            var rpo = new Repox("dls");
            rpo.ExecuteReader("");
            rpo.Delete(x);
            rpo.GetById<PocoTest>(3);
            rpo.GetPage<PocoTest>(3, 121);
            rpo.GetListWhere<PocoTest>(k => k.Name == "bob" && k.Id == 5);
            rpo.InsertWithChildren<PocoTest>(x);

            Repo.GetPageWhere<PocoTest>(k => k.Id == 2 && k.Name == "bob", 1, 35);
            Repo.ExecSproc<PocoTest>("sprocName", new { id = 1, name = 3 });

            var fxr = RepoFluent.CreateConnection("");
            fxr.ExecuteScalarAsync<PocoTest>("");
            var q = fxr.CountWhere<PocoTest>("blah");
            var q1 = fxr.IsNullEmptyOrZero(q);
            var a2 = fxr.GetPageWhere<PocoTest>(k => k.Id == 5);

            var rpf = new RepoFluent();
            rpf.GetPageWhere<PocoTest>(kk => kk.Id == 5);
            
        }
    }

    public static class DapperExtensions1
    {
        public static int InsertWithChildren<T>(this IDbConnection connection, T model)
        {
            var properties = typeof(T).GetProperties();
            var sql = BuildInsertSql<T>(model, properties);

            using var multi = connection.QueryMultiple(sql, model);
            var result = multi.Read().Single();
            var id = (int)result.id;

            foreach (var property in properties)
            {
                var childModel = property.GetValue(model);
                if (childModel == null) continue;

                var childType = childModel.GetType();
                var childProperties = childType.GetProperties();
                var childSql = BuildInsertSql(childModel, childProperties, id);

                connection.Execute(childSql, childModel);
            }

            return id;
        }

        private static string BuildInsertSql<T>(T model, PropertyInfo[] properties, int parentId = 0)
        {
            var tableName = typeof(T).Name;
            var cols = string.Join(",", properties.Select(p => p.Name));
            var vals = string.Join(",", properties.Select(p => "@" + p.Name));

            var sql = $"INSERT INTO {tableName} ({cols}) VALUES ({vals}); SELECT CAST(SCOPE_IDENTITY() as int) as id";
            if (parentId > 0)
            {
                sql = $"DECLARE @tmpTable TABLE (id int); {sql} INSERT INTO {tableName}_{tableName} (ParentId, ChildId) VALUES ({parentId}, (SELECT id FROM @tmpTable));";
            }
            return sql;
        }
    }

    public static class DapperExtensions2
    {
        public static int InsertWithChildren<T>(this IDbConnection connection, T model)
        {
            var properties = typeof(T).GetProperties();
            var sql = BuildInsertSql<T>(model, properties);

            using var multi = connection.QueryMultiple(sql, model);
            var result = multi.Read().Single();
            var id = (int)result.id;

            foreach (var property in properties)
            {
                var childModel = property.GetValue(model);
                if (childModel == null) continue;

                var childType = childModel.GetType();
                if (childType.IsClass && childType != typeof(string)) 
                    InsertWithChildren(connection, childModel, id);
            }

            return id;
        }

        private static void InsertWithChildren<T>(IDbConnection connection, T model, int parentId)
        {
            var properties = typeof(T).GetProperties();
            var sql = BuildInsertSql<T>(model, properties, parentId);

            using var multi = connection.QueryMultiple(sql, model);
            var result = multi.Read().Single();
            var id = (int)result.id;

            foreach (var property in properties)
            {
                var childModel = property.GetValue(model);
                if (childModel == null) continue;

                var childType = childModel.GetType();
                if (childType.IsClass && childType != typeof(string)) 
                    InsertWithChildren(connection, childModel, id);
            }
        }

        private static string BuildInsertSql<T>(T model, PropertyInfo[] properties, int parentId = 0)
        {
            var tableName = typeof(T).Name;
            var cols = string.Join(",", properties.Select(p => p.Name));
            var vals = string.Join(",", properties.Select(p => "@" + p.Name));
            var sql = $"INSERT INTO {tableName} ({cols}) VALUES ({vals}); SELECT CAST(SCOPE_IDENTITY() as int) as id";
            
            if (parentId > 0)
                sql =
                    $"DECLARE @tmpTable TABLE (id int); {sql} INSERT INTO {tableName}_{tableName} (ParentId, ChildId) VALUES ({parentId}, (SELECT id FROM @tmpTable));";
            
            return sql;
        }
    }

    public static class DapperExtensions3
    {
        public static int InsertWithChildren<T>(this IDbConnection connection, T model)
        {
            var properties = typeof(T).GetProperties();
            var sql = BuildInsertSql<T>(model, properties);

            using var multi = connection.QueryMultiple(sql, model);
            var result = multi.Read().Single();
            var id = (int)result.id;

            foreach (var property in properties)
            {
                var childModel = property.GetValue(model);
                if (childModel == null) continue;

                var childType = childModel.GetType();
                if (childType.IsClass && childType != typeof(string)) 
                    InsertWithChildren(connection, childModel, id);
            }

            return id;
        }

        private static void InsertWithChildren<T>(IDbConnection connection, T model, int parentId)
        {
            var properties = typeof(T).GetProperties();
            var sql = BuildInsertSql<T>(model, properties, parentId);

            using var multi = connection.QueryMultiple(sql, model);
            var result = multi.Read().Single();
            var id = (int)result.id;

            foreach (var property in properties)
            {
                var childModel = property.GetValue(model);
                if (childModel == null) continue;

                var childType = childModel.GetType();
                if (childType.IsClass && childType != typeof(string)) 
                    InsertWithChildren(connection, childModel, id);
            }
        }

        private static string BuildInsertSql<T>(T model, PropertyInfo[] properties, int parentId = 0)
        {
            var tableName = typeof(T).Name;
            var cols = string.Join(",", properties.Select(p => p.Name));
            var vals = string.Join(",", properties.Select(p => "@" + p.Name));
            var sql = $"INSERT INTO {tableName} ({cols}) VALUES ({vals}); SELECT CAST(SCOPE_IDENTITY() as int) as id";

            if (parentId > 0)
                sql = $"{sql}; INSERT INTO {tableName}_{tableName} (ParentId, ChildId) VALUES ({parentId}, SCOPE_IDENTITY());";
            
            return sql;
        }
    }

    public static class DapperExtensions4
    {
        public static T InsertWithChildren<T>(this IDbConnection connection, T model)
        {
            var properties = typeof(T).GetProperties();
            var sql = BuildInsertSql<T>(model, properties);

            using (var multi = connection.QueryMultiple(sql, model))
            {
                var result = multi.Read().Single();
                var id = (int)result.id;

                var idProperty = GetIdProperty(typeof(T));
                idProperty.SetValue(model, id);

                foreach (var property in properties)
                {
                    var childModel = property.GetValue(model);
                    if (childModel == null) continue;

                    var childType = childModel.GetType();
                    if (childType.IsClass && childType != typeof(string))
                    {
                        InsertWithChildren(connection, childModel, id);
                    }
                }

                return model;
            }
        }

        private static void InsertWithChildren<T>(IDbConnection connection, T model, int parentId)
        {
            var properties = typeof(T).GetProperties();
            var sql = BuildInsertSql<T>(model, properties, parentId);

            using (var multi = connection.QueryMultiple(sql, model))
            {
                var result = multi.Read().Single();
                var id = (int)result.id;

                var idProperty = GetIdProperty(typeof(T));
                idProperty.SetValue(model, id);

                foreach (var property in properties)
                {
                    var childModel = property.GetValue(model);
                    if (childModel == null) continue;

                    var childType = childModel.GetType();
                    if (childType.IsClass && childType != typeof(string))
                    {
                        InsertWithChildren(connection, childModel, id);
                    }
                }
            }
        }

        private static PropertyInfo GetIdProperty(Type modelType)
        {
            var idProperty = modelType.GetProperty("Id", BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
            
            if (idProperty != null) 
                return idProperty;

            var idName = $"{modelType.Name}Id";
            idProperty = modelType.GetProperty(idName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);

            return idProperty;
        }

        private static string BuildInsertSql<T>(T model, PropertyInfo[] properties, int parentId = 0)
        {
            var tableName = typeof(T).Name;
            var cols = string.Join(",", properties.Select(p => p.Name));
            var vals = string.Join(",", properties.Select(p => "@" + p.Name));
            var sql = $"INSERT INTO {tableName} ({cols}) VALUES ({vals}); SELECT CAST(SCOPE_IDENTITY() as int) as id";

            if (parentId > 0)
            {
                sql = $"{sql}; DECLARE @ParentId INT = {parentId};" +
                      $"UPDATE {tableName} SET ParentId = @ParentId WHERE Id = SCOPE_IDENTITY();";
            }

            return sql;
        }
    }

    public static class DapperExtensions5
    {
        public static T InsertWithChildren<T>(this IDbConnection connection, T model)
        {
            var properties = typeof(T).GetProperties();
            var sql = BuildInsertSql<T>(model, properties);

            using (var multi = connection.QueryMultiple(sql, model))
            {
                var result = multi.Read().Single();
                var id = (int)result.id;

                var idProperty = GetIdProperty(typeof(T));
                idProperty.SetValue(model, id);

                foreach (var property in properties)
                {
                    var childModel = property.GetValue(model);
                    if (childModel == null) continue;

                    var childType = childModel.GetType();
                    if (childType.IsClass && childType != typeof(string))
                    {
                        InsertWithChildren(connection, childModel, id);
                    }
                }

                return model;
            }
        }

        private static void InsertWithChildren<T>(IDbConnection connection, T model, int parentId)
        {
            var properties = typeof(T).GetProperties();
            var sql = BuildInsertSql<T>(model, properties, parentId);

            using (var multi = connection.QueryMultiple(sql, model))
            {
                var result = multi.Read().Single();
                var id = (int)result.id;

                var idProperty = GetIdProperty(typeof(T));
                idProperty.SetValue(model, id);

                foreach (var property in properties)
                {
                    var childModel = property.GetValue(model);
                    if (childModel == null) continue;

                    var childType = childModel.GetType();
                    if (childType.IsClass && childType != typeof(string))
                    {
                        InsertWithChildren(connection, childModel, id);
                    }
                }
            }
        }

        private static PropertyInfo GetIdProperty(Type modelType)
        {
            var idProperty = modelType.GetProperty("Id", BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);

            if (idProperty != null)
                return idProperty;

            var idName = $"{modelType.Name}Id";
            idProperty = modelType.GetProperty(idName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);

            return idProperty;
        }

        private static string BuildInsertSql<T>(T model, PropertyInfo[] properties, int parentId = 0)
        {
            var tableName = typeof(T).Name;
            var cols = string.Join(",", properties.Select(p => p.Name));
            var vals = string.Join(",", properties.Select(p => "@" + p.Name));
            var sql = $"INSERT INTO {tableName} ({cols}) VALUES ({vals}); SELECT CAST(SCOPE_IDENTITY() as int) as id";

            if (parentId > 0)
            {
                sql = $"{sql}; DECLARE @ParentId INT = {parentId};" +
                      $"UPDATE {tableName} SET ParentId = @ParentId WHERE Id = SCOPE_IDENTITY();";
            }

            return sql;
        }
    }


    public static class ClassExtensions
    {
        public static bool ImplementsInterface<T>(this Type type)
        {
            return type.GetInterfaces().Contains(typeof(T));
        }
    }


}

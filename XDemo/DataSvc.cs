using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dallus;

namespace XDemo
{
    internal static class DataSvc
    {
        public static IEnumerable<T> GetData<T>(this T model) where T: class, IRepoModel, new()
        {
            var repo = new Repox("Data Source=localhost;Database=northwind;trusted_connection=true;");
            var data = repo.GetList<T>();
            return data;
        }
    }
}

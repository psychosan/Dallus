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

}

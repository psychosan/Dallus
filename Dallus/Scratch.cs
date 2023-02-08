using System.Collections.Concurrent;
using System.Data;
using System.Reflection;
using Dapper;
using SqlLambda.ValueObjects;

namespace Dallus
{
    internal class Customer : IRepoModel
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string FullName { get; set; }
        public string Description { get; set; }
        public PocoTest2 PokoTest2 { get; set; }
    }

    internal class PocoTest : IRepoModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    internal class PocoTest2 : IRepoModel
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }

    internal class Scratch
    {
        public void DoSomething1()
        {
            var pco = new Customer()
            {
                Description = "Nice Gury",
                FirstName = "Bobb",
                FullName = "Bubba Jones",
                Id = 7,
                LastName = "Jones",
                PokoTest2 = new PocoTest2()
                {
                    Description = "pk2 description",
                    Name = "chubby boy bob",
                    Id = 3
                }
            };

            var rpo = new Repox("connection");
            rpo.InsertWithChildren<Customer>(pco);

        }

        public void DlinkExp()
        {
            var pcoTest = new PocoTest();

            var rpo = new Repox("connectionString");
            var pageSet = rpo.GetPageWhere<PocoTest2>(k => k.Description == "Hi", 3, 25);

            var paramVal = new { PricePoint = 200 };

            rpo.ExecuteReader("Select * from Customers Where LastPurchase > @pricePoint", paramVal);
            rpo.Delete(pcoTest);
            rpo.GetById<PocoTest>(3);
            rpo.GetPage<PocoTest>(3, 121);
            rpo.GetListWhere<PocoTest>(k => k.Name == "bob" && k.Id == 5);
            rpo.InsertWithChildren<PocoTest>(pcoTest);

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

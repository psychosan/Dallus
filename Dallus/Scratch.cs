namespace Repox
{
    internal class PocoTest:IRepoModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    internal class Scratch
    {
        public void DlinkExp()
        {
            var x = new PocoTest();

            Repo.GetPageWhere<PocoTest>(k => k.Id == 2 && k.Name == "bob", 1, 35);
            Repo.ExecSproc<PocoTest>("sprocName", new { id = 1, name = 3 });
            
            
        }
    }

}

namespace Dallus
{
    /// <summary>
    /// Breaking all the rules on this one.
    /// This class basically facilitates getting a pk and the requested query to run
    /// </summary>
    internal class ModelQueryPkg
    {
        public dynamic? PkVal { get; set; }
        public string? QryScript { get; set; }
    }
}
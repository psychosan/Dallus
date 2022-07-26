namespace Repox
{
    /// <summary>
    /// Breaking all the rules on this one.
    /// This class basically facilitates getting a pk and the requested query to run
    /// </summary>
    internal class ModelQueryPkg
    {
        public dynamic pkVal { get; set; }
        public string qryScript { get; set; }
    }
}
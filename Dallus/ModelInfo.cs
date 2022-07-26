namespace Dallus
{
    [Serializable]
    public class ModelInfo 
    {
        /// <summary>
        /// Model Class/Table Name
        /// </summary>
        public string TableName { get; internal set; }

        /// <summary>
        /// Returns full set of table columns, including the primary key. e.g. "[field1], [field2], [etc...]"
        /// </summary>
        public string TableColumns { get; set; }

        public string PkName { get; internal set; }

        /// <summary>
        /// Is the primary key a numeric type
        /// </summary>
        public bool PkIsNumeric { get; set; }

        public string InsertScript { get; set; }
        public string UpdateScript { get; set; }
        public string UpdateWhereScript { get; set; }
        public string DeleteScript { get; set; }
        public string DeleteByIdScript { get; set; }
        public string DeleteWhereScript { get; set; }
        public string ExistsScript { get; set; }
        public string ExistsWhereScript { get; set; }
        public string PageScript { get; set; }
        public string PageWhereScript { get; internal set; }
        public string TopScript { get; set; }
        public string TopWhereScript { get; set; }
        public string CountScript { get; internal set; }
        public string CountWhereScript { get; internal set; }
        public string SelectAllScript { get; internal set; }
        public string SelectByIdScript { get; internal set; }
        public string SelectWhereScript { get; internal set; }

        /// <summary>
        /// List of PropItems for all fields except those decorated with the [DapperIgnore] attribute
        /// </summary>
        public IList<PropItem> PropItems { get; set; }
    }
}
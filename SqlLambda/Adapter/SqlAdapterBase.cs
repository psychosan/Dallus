/* License: http://www.apache.org/licenses/LICENSE-2.0 */

namespace SqlLambda.Adapter
{
    /// <summary>
    /// Provides functionality common to all supported databases
    /// </summary>
    class SqlAdapterBase
    {
        public string QueryString(string selection, string source, string conditions, string order, string grouping, string having) 
                       => $"SELECT {selection} FROM {source} {conditions} {order} {grouping} {having}";
    }
}

using System.Reflection;

namespace Repox
{
    public class PropItem
    {
        public string Name { get; set; }
        public bool IsPrimaryKey { get; set; }
        public bool IsIgnored { get; set; }
        public PropertyInfo PropInfoSet { get; set; }

        /// <summary>
        /// Returns PropItem.Name as [PropertyName] = @PropertyName
        /// </summary>
        /// <returns>string: [PropertyName] = @PropertyName</returns>
        public string ToUpdateSegment()
        {
            if (IsPrimaryKey || IsIgnored) return string.Empty;

            return $"[{Name}] = @{Name} ,";
        }

        /// <summary>
        /// Returns PropItem.Name as [PropertyName]
        /// </summary>
        /// <returns>string: [PropertyName]</returns>
        public string ToInsertArgument()
        {
            if (IsPrimaryKey || IsIgnored) return string.Empty;

            return $"[{Name}] ,";
        }

        /// <summary>
        /// Returns PropItem.Name as @PropertyName
        /// </summary>
        /// <returns>string: @PropertyName</returns>
        public string ToInsertValue()
        {
            if (IsPrimaryKey || IsIgnored) return string.Empty;

            return $"@{Name} ,";
        }

        /// <summary>
        /// Incudes all fields, including Pk, that are not decorated with [DapperIgnored]
        /// </summary>
        /// <returns></returns>
        public string ToColumnItem()
        {
            if (IsIgnored) return string.Empty;

            return $"[{Name}],";
        }
    }
}
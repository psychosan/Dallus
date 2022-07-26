using System.Reflection;
using System.Text;

namespace Repox
{
    public static class DataExt
    {
        internal static bool IsNumeric(this string stringVal)
        {
            return long.TryParse(stringVal, out _);
        }

        internal static string ToColumnString(this IList<PropItem> propList, bool ignorePk = false)
        {
            var sb = new StringBuilder();

            foreach (var item in propList.Where(item => !item.IsIgnored && (!ignorePk || !item.IsPrimaryKey)))
            {
                sb.Append($"[{item.Name}], ");
            }

            string colSet = sb.ToString().Trim().TrimEnd(',');
            return colSet;
        }

        internal static dynamic GetPropValue<T>(object model, string propName) where T : class, IRepoModel
        {
            return model.GetType().GetProperty(propName)?.GetValue(model);
        }

        // N2M From: http://technico.qnownow.com/how-to-set-property-value-using-reflection-in-c/
        // Moded by Me *~FXD>
        internal static void SetValue(object inputObject, string propertyName, object propertyVal)
        {
            //find out the type
            Type type = inputObject.GetType();

            //get the property information based on the type
            PropertyInfo propertyInfo = type.GetProperty(propertyName);

            //find the property type
            Type propertyType = propertyInfo.PropertyType;

            //Convert.ChangeType does not handle conversion to nullable types
            //if the property type is nullable, we need to get the underlying type of the property
            var targetType = IsNullableType(propertyInfo.PropertyType) ? Nullable.GetUnderlyingType(propertyInfo.PropertyType) : propertyInfo.PropertyType;

            //Returns an System.Object with the specified System.Type and whose value is
            //equivalent to the specified object.
            propertyVal = Convert.ChangeType(propertyVal, targetType);

            //Set the value of the property
            propertyInfo.SetValue(inputObject, propertyVal, null);
        }

        internal static bool IsNullableType(Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
        }
    }
}
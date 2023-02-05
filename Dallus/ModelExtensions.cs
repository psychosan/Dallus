using System.Reflection;

namespace Dallus
{
    internal static class ModelExtensions
    {
        public static bool ImplementsInterface<T>(this Type type)
        {
            return type.GetInterfaces().Contains(typeof(T));
        }

        internal static T SetPkId<T>(this IRepoModel model, string pkFieldName, dynamic pkIdValue) where T : class, IRepoModel
        {
            SetValue(model, pkFieldName, pkIdValue);
            return (T)model;
        }

        internal static T SetPropValue<T>(this IRepoModel model, string propName, dynamic propValue) where T : class, IRepoModel
        {
            SetValue(model, propName, propValue);
            return (T)model;
        }

        internal static dynamic GetPropValue<T>(this IRepoModel model, string propName) where T : class, IRepoModel
        {
            return model.GetType().GetProperty(propName)?.GetValue(model);
        }

        //N2M Based on: http://technico.qnownow.com/how-to-set-property-value-using-reflection-in-c/
        internal static void SetValue(this IRepoModel inputObject, string propertyName, object propertyVal)
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
            return type.IsGenericType && type.GetGenericTypeDefinition().Equals(typeof(Nullable<>));
        }
    }
}
using System;
using System.Linq;

namespace GuruOps.ES.LinqToDsl.DAL
{
    public static class TypeHelper
    {
        public static bool IsInstanceOf(this Type type, Type genericType)
        {
            bool result = type.IsAssignableFrom(genericType) ||
                         type.GetInterfaces()
                             .Any(x => x == genericType || genericType.IsGenericType && x.GetGenericTypeDefinition() == genericType);
            return result;
        }
    }
}

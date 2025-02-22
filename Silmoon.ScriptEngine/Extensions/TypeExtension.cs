using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Silmoon.ScriptEngine.Extensions
{
    public static class TypeExtension
    {
        public static PropertyInfo[] GetParameters<T>(this Type type) where T : Attribute
        {
            return [.. type.GetProperties().Where(x => x.GetCustomAttribute<T>() is not null)];
        }
        public static T[] GetParameterAttributes<T>(this Type type) where T : Attribute
        {
            var properties = GetParameters<T>(type);
            return [.. properties.Select(x => x.GetCustomAttribute<T>())];
        }
    }
}

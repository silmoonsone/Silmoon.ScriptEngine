using Silmoon;
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
        public static object? Invoke(this Type type, object instance, MethodExecuteInfo methodExecuteInfo) => type.GetMethod(methodExecuteInfo.Name)?.Invoke(instance, methodExecuteInfo.Parameters);
    }
}

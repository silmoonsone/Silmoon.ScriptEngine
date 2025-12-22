using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Silmoon.ScriptEngine
{
    public class MethodExecuteInfo
    {
        public string Name { get; set; }
        public object?[]? Parameters { get; set; }

        public static MethodExecuteInfo Create(string name, object?[]? parameters)
        {
            return new MethodExecuteInfo
            {
                Name = name,
                Parameters = parameters
            };
        }
    }
}

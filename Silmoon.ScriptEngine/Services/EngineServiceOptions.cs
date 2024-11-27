using Silmoon.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Silmoon.ScriptEngine.Services
{
    public class EngineServiceOptions
    {
        public List<string> ScriptFiles { get; set; } = [];
        public List<string> ReferrerAssemblyNames { get; set; } = [];
        public List<string> ReferrerAssemblyPaths { get; set; } = [];

        public string StartTypeFullName { get; set; }
        public MethodExecuteInfo[] StartExecuteMethods { get; set; } = [];
        public MethodExecuteInfo[] StopExecuteMethods { get; set; } = [];
    }
}

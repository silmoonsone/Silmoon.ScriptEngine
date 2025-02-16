using Silmoon.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Silmoon.ScriptEngine
{
    public class EngineInstanceOptions
    {
        public string AssemblyName { get; set; } = null;
        public List<string> ScriptFiles { get; set; } = [];
        public List<string> ReferrerAssemblyNames { get; set; } = [];
        public List<string> ReferrerAssemblyPaths { get; set; } = [];

        public string StartTypeFullName { get; set; }
    }
}

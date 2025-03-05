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
        public string AssemblyLoadContextName { get; set; } = null;
        public List<string> ScriptFiles { get; set; } = [];
        public List<string> ReferrerAssemblyNames { get; set; } = [];
        public List<string> ReferrerAssemblyPaths { get; set; } = [];

        public string MainTypeFullName { get; set; }


        public void AddCoreReferrer()
        {
            ReferrerAssemblyNames.Add("netstandard");
            ReferrerAssemblyNames.Add("System.Console");
            ReferrerAssemblyNames.Add("System.Runtime");
            ReferrerAssemblyNames.Add("System.Collections");
            ReferrerAssemblyNames.Add("System.Private.CoreLib");
            ReferrerAssemblyNames.Add("System.Linq");
        }
    }
}
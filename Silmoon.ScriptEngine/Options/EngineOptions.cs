using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Silmoon.ScriptEngine.Options
{
    public class EngineOptions
    {
        public string AssemblyLoadContextName { get; set; } = null;
        public List<string> ReferrerAssemblyNames { get; set; } = [];
        public List<string> ReferrerAssemblyPaths { get; set; } = [];

        public string EntryTypeFullName { get; set; }


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
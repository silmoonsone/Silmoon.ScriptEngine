using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Silmoon.ScriptEngine.Options
{
    public class EngineCompilerOptions : EngineOptions
    {
        public string AssemblyName { get; set; } = null;

        public List<string> ScriptFiles { get; set; } = [];
    }
}
using Silmoon.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Silmoon.ScriptEngine.Services
{
    public class EngineServiceOptions : EngineInstanceOptions
    {
        public MethodExecuteInfo[] StartExecuteMethods { get; set; } = [];
        public MethodExecuteInfo[] StopExecuteMethods { get; set; } = [];
    }
}

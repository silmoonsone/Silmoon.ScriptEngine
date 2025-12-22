using Silmoon.ScriptEngine.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Silmoon.ScriptEngine.ServiceHostTesting
{
    public class EngineServiceOptions : EngineCompilerOptions
    {
        public MethodExecuteInfo StartExecuteMethod { get; set; } = null;
        public MethodExecuteInfo StopExecuteMethod { get; set; } = null;
    }
}

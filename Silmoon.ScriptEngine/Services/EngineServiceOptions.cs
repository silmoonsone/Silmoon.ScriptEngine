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
        public MethodExecuteInfo StartExecuteMethod { get; set; } = null;
        public MethodExecuteInfo StopExecuteMethod { get; set; } = null;
    }
}

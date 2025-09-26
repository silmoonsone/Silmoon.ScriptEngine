using Silmoon.ScriptEngine.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Silmoon.ScriptEngine
{
    public class EngineExecuteContext
    {
        public EngineOptions Options { get; set; }
        public byte[] AssemblyBinary { get; set; }
    }
}
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
        public List<string> AdditionAssemblyNames { get; set; } = [];

        public string MainTypeFullName { get; set; }
        public string StartMethod { get; set; }
        public object?[]? StartMethodParameter { get; set; }
        public string StopMethod { get; set; }
        public object?[]? StopMethodParameter { get; set; }
    }
}

using Silmoon.Extension;
using Silmoon.ScriptEngine.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Silmoon.ScriptEngine.Models
{
    public class EngineExecuteContext
    {
        public EngineOptions Options { get; set; }
        public byte[] AssemblyBinary { get; set; }
        public byte[] GetEngineExecuteModelBinary()
        {
            var compressedData = this.ToJsonString().GetBytes().Compress();
            return compressedData;
        }
    }
}
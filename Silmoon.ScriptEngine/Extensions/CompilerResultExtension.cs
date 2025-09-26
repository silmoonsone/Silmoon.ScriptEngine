using Silmoon.Extension;
using Silmoon.Runtime;
using Silmoon.ScriptEngine.Models;
using Silmoon.ScriptEngine.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Silmoon.ScriptEngine.Extensions
{
    public static class CompilerResultExtension
    {
        public static EngineExecuteContext GetEngineExecuteModel(this CompilerResult compilerResult, EngineCompilerOptions options)
        {
            if (compilerResult.Success)
            {
                EngineExecuteContext engineExecuteContext = new EngineExecuteContext()
                {
                    AssemblyBinary = compilerResult.Binary,
                    Options = options,
                };
                return engineExecuteContext;
            }
            else throw new Exception("Compiler result is not success.");
        }
        public static byte[] GetEngineExecuteModelBinary(this CompilerResult compilerResult, EngineCompilerOptions options)
        {
            var engineExecuteContext = GetEngineExecuteModel(compilerResult, options);
            var compressedData = engineExecuteContext.ToJsonString().GetBytes().Compress();
            return compressedData;
        }
    }
}

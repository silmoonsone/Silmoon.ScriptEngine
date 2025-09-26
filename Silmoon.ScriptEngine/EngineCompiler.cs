using Silmoon.Extension;
using Silmoon.Models;
using Silmoon.Runtime;
using Silmoon.ScriptEngine.Models;
using Silmoon.ScriptEngine.Options;
using Silmoon.Secure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Silmoon.ScriptEngine
{
    public class EngineCompiler
    {
        public event EngineOutputCallback OnOutput;
        public event EngineErrorCallback OnError;
        public EngineCompilerOptions Options { get; private set; } = null;
        Compiler Compiler { get; set; } = new Compiler();

        public List<FileInfo> CheckedFiles { get; private set; } = [];
        public byte[] AssemblyBinary { get; private set; } = null;


        public EngineCompiler(EngineCompilerOptions engineInstanceOptions)
        {
            Options = engineInstanceOptions;
        }

        public EngineOptions Preprocess()
        {
            for (int i = 0; i < Options.ScriptFiles.Count; i++)
            {
                Options.ScriptFiles[i] = Path.GetFullPath(Options.ScriptFiles[i]);
            }
            for (int i = 0; i < Options.ReferrerAssemblyPaths.Count; i++)
            {
                Options.ReferrerAssemblyPaths[i] = Path.GetFullPath(Options.ReferrerAssemblyPaths[i]);
            }

            List<string> files = [];
            string assemblyName = Options.AssemblyName;
            foreach (var item in Options.ScriptFiles)
            {
                if (!File.Exists(item)) continue;
                string[] lines = File.ReadAllLines(item);
                foreach (var line in lines)
                {
                    if (line.StartsWith("#pragma dep"))
                    {
                        var lineArray = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        if (lineArray.Length == 3)
                            if (!Options.ReferrerAssemblyNames.Contains(lineArray[2].Trim())) Options.ReferrerAssemblyNames.Add(lineArray[2].Trim());
                    }

                    if (line.StartsWith("#pragma ref"))
                    {
                        var lineArray = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        if (lineArray.Length == 3)
                        {
                            var path = Path.GetFullPath(lineArray[2].Trim());
                            if (!Options.ReferrerAssemblyPaths.Contains(path)) Options.ReferrerAssemblyPaths.Add(path);
                        }
                    }

                    if (line.StartsWith("#pragma csf"))
                    {
                        var lineArray = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        if (lineArray.Length == 3) files.Add(Path.GetFullPath(lineArray[2].Trim()));
                    }

                    if (line.StartsWith("#pragma assemblyName"))
                    {
                        var lineArray = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        if (lineArray.Length == 3) assemblyName = lineArray[2].Trim();
                    }
                }
            }

            Options.AssemblyName = assemblyName;

            foreach (var item in files)
            {
                if (!Options.ScriptFiles.Contains(item)) Options.ScriptFiles.Add(item);
            }

            return Options;
        }
        public StateSet<bool, List<FileInfo>> CheckFiles()
        {
            bool scriptFileIsNotExist = false;
            List<FileInfo> scriptFiles = [];
            Options.ScriptFiles.Each(file =>
            {
                var info = new FileInfo(file);
                if (!info.Exists)
                {
                    scriptFileIsNotExist = true;
                    OnError?.Invoke($"Script file {info.FullName} not found");
                }
                else
                    scriptFiles.Add(info);
            });

            if (scriptFileIsNotExist)
                return false.ToStateSet(scriptFiles, "Some script file(s) do not exist.");
            else
            {
                CheckedFiles = scriptFiles;
                return true.ToStateSet(scriptFiles);
            }
        }
        public async Task<CompilerResult> Compile()
        {
            if (!CheckedFiles.IsNullOrEmpty())
            {
                if (!CheckedFiles.IsNullOrEmpty()) OnOutput?.Invoke($"Start compiling {CheckedFiles.Count} files including {CheckedFiles[0].Name}..");

                var result = await Compiler.CompileSourceFilesAsync(Options.AssemblyName, CheckedFiles.Select(x => x.FullName), null, [.. Options.ReferrerAssemblyPaths], [.. Options.ReferrerAssemblyNames], false);
                if (result.Success)
                {
                    AssemblyBinary = result.Binary;
                    OnOutput?.Invoke($"Compilation success. assembly binary size {result.Binary.Length}. md5 hash is {result.Binary.GetMD5Hash().ToHexString()}.");
                }
                else
                    result.Diagnostics.Each(diagnostic => OnError?.Invoke($"{diagnostic}"));
                return result;
            }
            else
            {
                throw new FileLoadException("No script files to compile. Please check files.");
            }
        }

        public EngineExecuter NewExecuter()
        {
            return new EngineExecuter(new EngineExecuteContext()
            {
                AssemblyBinary = AssemblyBinary,
                Options = Options,
            });
        }
        public EngineExecuter<T> NewExecuter<T>() where T : class
        {
            return new EngineExecuter<T>(new EngineExecuteContext()
            {
                AssemblyBinary = AssemblyBinary,
                Options = Options,
            });
        }
    }
    public delegate void EngineOutputCallback(string message);
    public delegate void EngineErrorCallback(string message, Exception exception = null);
}

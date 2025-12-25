using Silmoon.Extension;
using Silmoon.Models;
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
        public EngineCompilerOptions Options { get; private set; }
        Compiler Compiler = new Compiler();
        public byte[] AssemblyBinary { get; private set; } = null;

        public EngineCompiler(EngineCompilerOptions engineInstanceOptions)
        {
            Options = engineInstanceOptions;
        }

        EngineOptions Preprocess()
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
                string sourceCodeBaseDirectory = Path.GetDirectoryName(item);

                if (!File.Exists(item)) continue;
                string[] lines = File.ReadAllLines(item);
                foreach (var line in lines)
                {
                    if (line.StartsWith("#pragma d"))
                    {
                        var lineArray = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        if (lineArray.Length == 3 && lineArray[2].StartsWith('"') && lineArray[2].EndsWith('"'))
                            if (!Options.ReferrerAssemblyNames.Contains(lineArray[2].Trim('"')))
                                Options.ReferrerAssemblyNames.Add(lineArray[2].Trim());
                    }

                    if (line.StartsWith("#pragma r"))
                    {
                        var lineArray = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        if (lineArray.Length == 3 && lineArray[2].StartsWith('"') && lineArray[2].EndsWith('"'))
                        {
                            var path = lineArray[2].Trim('"');

                            if (Path.IsPathRooted(path)) path = Path.GetFullPath(path);
                            else path = Path.GetFullPath(Path.Combine(sourceCodeBaseDirectory, path));


                            if (!Options.ReferrerAssemblyPaths.Contains(path)) Options.ReferrerAssemblyPaths.Add(path);
                        }
                    }

                    if (line.StartsWith("#pragma f"))
                    {
                        var lineArray = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        if (lineArray.Length == 3 && lineArray[2].StartsWith('"') && lineArray[2].EndsWith('"'))
                        {
                            var path = lineArray[2].Trim('"');

                            if (Path.IsPathRooted(path)) path = Path.GetFullPath(path);
                            else path = Path.GetFullPath(Path.Combine(sourceCodeBaseDirectory, path));

                            if (!files.Contains(path)) files.Add(path);
                        }
                    }

                    if (line.StartsWith("#pragma assemblyName"))
                    {
                        var lineArray = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        if (lineArray.Length == 3)
                            assemblyName = lineArray[2].Trim('"');
                    }
                }
            }

            Options.AssemblyName = assemblyName;

            foreach (var item in files)
            {
                if (!Options.ScriptFiles.Contains(item))
                    Options.ScriptFiles.Add(item);
            }

            return Options;
        }
        StateSet<bool, List<FileInfo>> CheckFiles()
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
                return true.ToStateSet(scriptFiles);
            }
        }
        public async Task<StateSet<bool, CompilerResult>> Compile()
        {
            Preprocess();
            var checkResult = CheckFiles();
            if (checkResult.State)
            {
                if (!checkResult.Data.IsNullOrEmpty())
                {
                    if (!checkResult.Data.IsNullOrEmpty()) OnOutput?.Invoke($"Start compiling {checkResult.Data.Count} files including {checkResult.Data[0].Name}..");
                    var result = await Compiler.CompileSourceFilesAsync(Options.AssemblyName, checkResult.Data.Select(x => x.FullName), null, [.. Options.ReferrerAssemblyPaths], [.. Options.ReferrerAssemblyNames], false);
                    if (result.Success)
                        OnOutput?.Invoke($"Compilation success. assembly binary size {result.Binary.Length}. md5 hash is {result.Binary.GetMD5Hash().ToHexString()}.");
                    else
                        result.Diagnostics.Each(diagnostic => OnError?.Invoke($"{diagnostic}"));
                    return result.Success.ToStateSet(result);
                }
                else
                    return false.ToStateSet<CompilerResult>(null, "No script files to compile. Please check files.");
            }
            else return false.ToStateSet<CompilerResult>(null, checkResult.Message);
        }

        public EngineExecuter GetEngineExecuter(byte[] assemblyBinary, EngineCompilerOptions options)
        {
            return new EngineExecuter(new EngineExecuteContext()
            {
                AssemblyBinary = assemblyBinary,
                Options = options,
            });
        }
        public EngineExecuter<T> GetEngineExecuter<T>(byte[] assemblyBinary, EngineCompilerOptions options) where T : class
        {
            return new EngineExecuter<T>(new EngineExecuteContext()
            {
                AssemblyBinary = assemblyBinary,
                Options = options,
            });
        }
    }
    public delegate void EngineOutputCallback(string message);
    public delegate void EngineErrorCallback(string message, Exception exception = null);
}

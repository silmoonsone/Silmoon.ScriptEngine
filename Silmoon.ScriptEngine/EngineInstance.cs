using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Silmoon.Extension;
using Silmoon.Models;
using Silmoon.Runtime;
using Silmoon.Runtime.Extensions;
using Silmoon.Secure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Silmoon.ScriptEngine
{
    public class EngineInstance<T> : IDisposable where T : class
    {
        public event EngineOutputCallback OnOutput;
        public event EngineErrorCallback OnError;
        Compiler Compiler { get; set; } = new Compiler();
        public AssemblyLoadContextEx Context { get; set; } = null;
        public EngineInstanceOptions Options { get; private set; } = null;
        public T Instance { get; set; } = default;
        public Type Type { get; set; } = null;

        public List<FileInfo> CheckedFiles { get; private set; } = [];
        public CompilerResult CompilerResult { get; private set; } = null;

        public EngineInstance()
        {
            Options = new EngineInstanceOptions();
        }
        public EngineInstance(EngineInstanceOptions engineInstanceOptions) : this()
        {
            Options = engineInstanceOptions;
        }

        public EngineInstanceOptions ProcessInstanceOptions()
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
            List<string> refs = [];
            foreach (var item in Options.ScriptFiles)
            {
                string[] lines = File.ReadAllLines(item);
                foreach (var line in lines)
                {
                    if (line.StartsWith("//dep:"))
                    {
                        var lineArray = line.Split(":");
                        if (lineArray.Length == 2)
                        {
                            if (!Options.ReferrerAssemblyNames.Contains(lineArray[1])) Options.ReferrerAssemblyNames.Add(lineArray[1]);
                        }
                    }

                    if (line.StartsWith("//ref:"))
                    {
                        var lineArray = line.Split(":");
                        if (lineArray.Length == 2)
                        {
                            refs.Add(Path.GetFullPath(lineArray[1]));
                        }
                    }

                    if (line.StartsWith("//csf:"))
                    {
                        var lineArray = line.Split(":");
                        if (lineArray.Length == 2)
                        {
                            files.Add(Path.GetFullPath(lineArray[1]));
                        }
                    }
                }
            }

            List<string> refs2 = [];
            foreach (var item in refs)
            {
                if (!Options.ReferrerAssemblyPaths.Contains(item)) refs2.Add(item);
            }

            List<string> files2 = [];
            foreach (var item in files)
            {
                if (!Options.ScriptFiles.Contains(item)) files2.Add(item);
            }

            foreach (var item in refs2)
            {
                Options.ReferrerAssemblyPaths.Add(item);
            }
            foreach (var item in files2)
            {
                Options.ScriptFiles.Add(item);
            }

            return Options;
        }
        public StateSet<bool, List<FileInfo>> CheckScriptFiles()
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
        public async Task<CompilerResult> CompileScript()
        {
            OnOutput?.Invoke($"Start compiling {CheckedFiles.Count} files including {CheckedFiles[0].Name}..");

            var result = await Compiler.CompileSourceFilesAsync("ScriptContext", CheckedFiles.Select(x => x.FullName), null, [.. Options.ReferrerAssemblyPaths], [.. Options.ReferrerAssemblyNames], false);
            if (result.Success)
            {
                CompilerResult = result;
                OnOutput?.Invoke($"Compilation success. assembly binary size {result.Binary.Length}. md5 hash is {result.Binary.GetMD5Hash().ToHexString()}.");
            }
            else
                result.Diagnostics.Each(diagnostic => OnError?.Invoke($"{diagnostic}"));
            return result;
        }


        public StateSet<bool> LoadAssembly()
        {
            if (Context is not null) return false.ToStateSet("Assembly context is not null, maybe has assembly is running.");
            try
            {
                OnOutput?.Invoke($"Assembly({Options.AssemblyName}) loaded.");
                Context = new AssemblyLoadContextEx(Options.AssemblyName, Options.ReferrerAssemblyNames, Options.ReferrerAssemblyPaths, true);
                using var codeStream = CompilerResult.Binary.GetStream();
                var assembly = Context.LoadFromStream(codeStream);

                Type = assembly.GetType(Options.MainTypeFullName);
                OnOutput?.Invoke($"Get main type({Type.FullName}) ok.");
                return true.ToStateSet();
            }
            catch (Exception ex)
            {
                return false.ToStateSet("Assembly load failed(" + ex.Message + ").");
            }
        }
        public void CreateInstance()
        {
            if (Type is not null)
            {
                Instance = (T)Activator.CreateInstance(Type);
                OnOutput?.Invoke($"Instance({Options.AssemblyName}::{Type.FullName}) created.");
            }

        }
        public void UnloadAssembly()
        {
            if (Context is not null)
            {
                try
                {
                    if (Instance is IDisposable disposable) disposable?.Dispose();
                }
                catch { }
                finally
                {
                    Context.Unload();
                    Context = null;
                    OnOutput?.Invoke($"Assembly({Options.AssemblyName}) unloaded.");
                }
            }
        }
        public StateSet<bool, Type> GetMainAssemblyType(CompilerResult compilerResult)
        {
            try
            {
                var context = new AssemblyLoadContextEx(Options.AssemblyName, Options.ReferrerAssemblyNames, Options.ReferrerAssemblyPaths, true);
                using var codeStream = compilerResult.Binary.GetStream();
                var assembly = context.LoadFromStream(codeStream);

                Type = assembly.GetType(Options.MainTypeFullName);
                context.Unload();
                return true.ToStateSet(Type);
            }
            catch (Exception ex)
            {
                return false.ToStateSet<Type>(null, "Assembly load failed(" + ex.Message + ").");
            }
        }

        public EngineExecuteModel GetEngineExecuteModel(CompilerResult compilerResult)
        {
            if (compilerResult.Success)
            {
                EngineExecuteModel csjModel = new EngineExecuteModel()
                {
                    CompilerResult = compilerResult,
                    Options = Options,
                };
                return csjModel;
            }
            else throw new Exception("Compiler result is not success.");
        }
        public StateSet<bool> LoadEngineExecuteModel(EngineExecuteModel engineExecuteModel)
        {
            Options = engineExecuteModel.Options;
            return LoadAssembly();
        }
        public byte[] GetEngineExecuteModelBinary(CompilerResult compilerResult)
        {
            var csjModel = GetEngineExecuteModel(compilerResult);
            var compressedData = csjModel.ToJsonString().GetBytes().Compress();
            return compressedData;
        }
        public StateSet<bool> LoadEngineExecuteModelBinary(byte[] csjData)
        {
            var json = csjData.Decompress().GetString();
            var csjModel = JsonConvert.DeserializeObject<EngineExecuteModel>(json);
            return LoadEngineExecuteModel(csjModel);
        }
        public void Dispose()
        {
            UnloadAssembly();
            OnOutput = null;
            OnError = null;
            Context = null;
            Options = null;
            Instance = null;
            Type = null;
        }
    }
    public class EngineInstance : EngineInstance<object>
    {
        public EngineInstance()
        {
        }
        public EngineInstance(EngineInstanceOptions engineInstanceOptions) : base(engineInstanceOptions)
        {
        }
    }
    public delegate void EngineOutputCallback(string message);
    public delegate void EngineErrorCallback(string message, Exception exception = null);
}

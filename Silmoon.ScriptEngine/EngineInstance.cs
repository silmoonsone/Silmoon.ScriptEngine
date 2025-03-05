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
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Silmoon.ScriptEngine
{
    public class EngineInstance<T> : IDisposable where T : class
    {
        public event EngineOutputCallback OnOutput;
        public event EngineErrorCallback OnError;
        public EngineInstanceOptions Options { get; private set; } = null;
        Compiler Compiler { get; set; } = new Compiler();
        public AssemblyLoadContextEx Context { get; set; } = null;
        public Assembly InstanceAssembly { get; set; } = null;
        public T Instance { get; set; } = default;
        public Type Type { get; set; } = null;

        public List<FileInfo> CheckedFiles { get; private set; } = [];
        public byte[] AssemblyBinary { get; private set; } = null;


        public EngineInstance()
        {
            Options = new EngineInstanceOptions();
        }
        public EngineInstance(EngineInstanceOptions engineInstanceOptions) : this()
        {
            Options = engineInstanceOptions;
        }

        public EngineInstanceOptions Preprocess()
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
            OnOutput?.Invoke($"Start compiling {CheckedFiles.Count} files including {CheckedFiles[0].Name}..");

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


        public StateSet<bool> LoadAssembly()
        {
            if (Context is not null) return false.ToStateSet("Assembly context is not null, maybe has assembly is running.");
            try
            {
                Context = new AssemblyLoadContextEx(Options.AssemblyLoadContextName, Options.ReferrerAssemblyNames, Options.ReferrerAssemblyPaths, true);
                using var codeStream = AssemblyBinary.GetStream();
                InstanceAssembly = Context.LoadFromStream(codeStream);
                OnOutput?.Invoke($"AssemblyLoadContext{(Context.Name.IsNullOrEmpty() ? "(unname!)" : $"({Context.Name})")}, Assembly({InstanceAssembly.GetName().Name}) loaded.");

                Type = InstanceAssembly.GetType(Options.MainTypeFullName);
                if (Type is null)
                {
                    OnError?.Invoke("Main type is null. Check main type name.");
                    return false.ToStateSet("Main type is null. Check main type name.");
                }
                else
                {
                    OnOutput?.Invoke($"Get main type({Type.FullName}) ok.");
                    return true.ToStateSet();
                }
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
                OnOutput?.Invoke($"Instance({InstanceAssembly.GetName().Name}::{Type.FullName}){(Options.AssemblyLoadContextName.IsNullOrEmpty() ? string.Empty : $" created on {Options.AssemblyLoadContextName}")}.");
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
                finally
                {
                    Context.Unload();
                    Context = null;
                    OnOutput?.Invoke($"Assembly({Options.AssemblyName}) unloaded.");
                }
            }
            Instance = null;
            InstanceAssembly = null;
            Type = null;
        }


        public EngineExecuteModel GetEngineExecuteModel(CompilerResult compilerResult)
        {
            if (compilerResult.Success)
            {
                EngineExecuteModel csjModel = new EngineExecuteModel()
                {
                    AssemblyBinary = compilerResult.Binary,
                    Options = Options,
                };
                return csjModel;
            }
            else throw new Exception("Compiler result is not success.");
        }
        public StateSet<bool> LoadEngineExecuteModel(EngineExecuteModel engineExecuteModel)
        {
            Options = engineExecuteModel.Options;
            AssemblyBinary = engineExecuteModel.AssemblyBinary;
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
            Options = null;
            OnOutput = null;
            OnError = null;
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

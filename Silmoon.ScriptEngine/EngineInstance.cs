using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
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
    public class EngineInstance : IDisposable
    {
        public event EngineOutputCallback OnOutput;
        public event EngineErrorCallback OnError;
        Compiler Compiler { get; set; } = new Compiler();
        public AssemblyLoadContextEx Context { get; set; } = null;
        EngineInstanceOptions EngineInstanceOptions { get; set; } = null;
        public object Instance { get; set; } = null;
        public Type Type { get; set; } = null;

        public EngineInstance()
        {

        }
        public EngineInstance(EngineInstanceOptions engineInstanceOptions) : this()
        {
            EngineInstanceOptions = engineInstanceOptions;
        }

        public StateSet<bool, List<FileInfo>> CheckScriptFiles()
        {
            bool scriptFileIsNotExist = false;
            List<FileInfo> scriptFiles = [];
            EngineInstanceOptions.ScriptFiles.Each(file =>
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
                return true.ToStateSet(scriptFiles);
        }
        public async Task<CompilerResult> CompileScript(List<FileInfo> scriptFiles)
        {
            OnOutput?.Invoke($"Start compiling {scriptFiles.Count} files including {scriptFiles[0].Name}..");

            var result = await Compiler.CompileSourceFilesAsync("ScriptContext", scriptFiles.Select(x => x.FullName), null, [.. EngineInstanceOptions.ReferrerAssemblyPaths], [.. EngineInstanceOptions.ReferrerAssemblyNames], false);
            if (result.Success)
                OnOutput?.Invoke($"Compilation success. assembly binary size {result.Binary.Length}. md5 hash is {result.Binary.GetMD5Hash().ToHexString()}.");
            else
                result.Diagnostics.Each(diagnostic => OnError?.Invoke($"{diagnostic.ToString()}"));
            return result;
        }
        public void LoadAssembly(CompilerResult compilerResult)
        {
            if (Context is not null) throw new InvalidOperationException("Context is not null. Please unload the assembly first.");
            OnOutput?.Invoke($"Assembly({EngineInstanceOptions.AssemblyName}) loaded.");
            Context = new AssemblyLoadContextEx(EngineInstanceOptions.AssemblyName, EngineInstanceOptions.ReferrerAssemblyNames, EngineInstanceOptions.ReferrerAssemblyPaths, true);
            using var codeStream = compilerResult.Binary.GetStream();
            var assembly = Context.LoadFromStream(codeStream);

            Type = assembly.GetType(EngineInstanceOptions.StartTypeFullName);
            Instance = Activator.CreateInstance(Type);
            OnOutput?.Invoke($"Assembly({EngineInstanceOptions.AssemblyName}) running.");
        }
        public void UnloadAssembly()
        {
            if (Context is not null)
            {
                Context.Unload();
                Context = null;
                OnOutput?.Invoke($"Assembly({EngineInstanceOptions.AssemblyName}) unloaded.");
            }
        }

        public void Dispose()
        {
            try
            {
                UnloadAssembly();
            }
            catch { }
            finally
            {
                OnOutput = null;
                OnError = null;
            }
        }
    }
    public delegate void EngineOutputCallback(string message);
    public delegate void EngineErrorCallback(string message, Exception exception = null);
}

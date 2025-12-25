using Newtonsoft.Json;
using Silmoon.Extension;
using Silmoon.Models;
using Silmoon.ScriptEngine.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Silmoon.ScriptEngine
{
    public class EngineExecuter<T> : IDisposable where T : class
    {
        public event EngineOutputCallback OnOutput;
        public event EngineErrorCallback OnError;

        public AssemblyLoadContextEx Context { get; set; } = null;
        public Assembly InstanceAssembly { get; set; } = null;
        public T Instance { get; set; } = default;
        public Type Type { get; set; } = null;
        public EngineExecuteContext EngineExecuteContext { get; private set; } = null;

        public EngineExecuter(EngineExecuteContext engineExecuteModel)
        {
            EngineExecuteContext = engineExecuteModel;
        }
        public EngineExecuter(byte[] engineExecuteModelBytes)
        {
            var jsonString = engineExecuteModelBytes.Decompress().GetString();
            var engineExecuteModel = JsonConvert.DeserializeObject<EngineExecuteContext>(jsonString);
            EngineExecuteContext = engineExecuteModel;
        }

        public StateSet<bool> LoadAssembly()
        {
            if (Context is not null) return false.ToStateSet("Assembly context is not null, maybe has assembly is running.");
            try
            {
                Context = new AssemblyLoadContextEx(EngineExecuteContext.Options.AssemblyLoadContextName, EngineExecuteContext.Options.ReferrerAssemblyNames, EngineExecuteContext.Options.ReferrerAssemblyPaths, true);
                using var codeStream = EngineExecuteContext.AssemblyBinary.GetStream();
                InstanceAssembly = Context.LoadFromStream(codeStream);
                Type = InstanceAssembly.GetType(EngineExecuteContext.Options.EntryTypeFullName);

                OnOutput?.Invoke($"AssemblyLoadContext{(Context.Name.IsNullOrEmpty() ? "(unnamed assembly)" : $"({Context.Name})")}, Assembly({InstanceAssembly.GetName().Name}) loaded.");

                if (Type is null)
                {
                    OnError?.Invoke("Entry type is null. Check entry type name.");
                    return false.ToStateSet("Entry type is null. Check entry type name.");
                }
                else
                {
                    OnOutput?.Invoke($"Get entry type({Type.FullName}) ok.");
                    return true.ToStateSet();
                }
            }
            catch (Exception ex)
            {
                return false.ToStateSet("Assembly load failed(" + ex.Message + ").");
            }
        }
        public StateSet<bool, T> CreateInstance()
        {
            if (Type is not null)
            {
                Instance = (T)Activator.CreateInstance(Type);
                OnOutput?.Invoke($"Instance({InstanceAssembly.GetName().Name}::{Type.FullName}){(EngineExecuteContext.Options.AssemblyLoadContextName.IsNullOrEmpty() ? string.Empty : $" created on {EngineExecuteContext.Options.AssemblyLoadContextName}")}.");
                return true.ToStateSet(Instance);
            }
            else return false.ToStateSet(Instance, "Type is null, can not create instance.");
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
                    OnOutput?.Invoke($"Assembly({InstanceAssembly.GetName().Name}) unloaded.");
                }
            }
            Instance = null;
            InstanceAssembly = null;
            Type = null;
        }



        public void Dispose()
        {
            UnloadAssembly();
            OnOutput = null;
            OnError = null;
        }
    }
    public class EngineExecuter : EngineExecuter<object>
    {
        public EngineExecuter(EngineExecuteContext engineExecuteModel) : base(engineExecuteModel)
        {
        }
        public EngineExecuter(byte[] engineExecuteModelBytes) : base(engineExecuteModelBytes)
        {
        }
    }
}

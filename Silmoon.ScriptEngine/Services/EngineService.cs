using Microsoft.CodeAnalysis.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Silmoon.Extension;
using Silmoon.Runtime;
using Silmoon.Runtime.Extensions;
using Silmoon.Secure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Silmoon.ScriptEngine.Services
{
    public class EngineService : IHostedService
    {
        ILogger<EngineService> _logger;
        EngineServiceOptions Options;
        IHostApplicationLifetime HostApplicationLifetime;

        Compiler compiler = new Compiler();
        object instance = null;
        Type type = null;

        public EngineService(ILogger<EngineService> logger, IOptions<EngineServiceOptions> _options, IHostApplicationLifetime hostApplicationLifetime)
        {
            _logger = logger;
            Options = _options.Value;
            HostApplicationLifetime = hostApplicationLifetime;
        }
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("EngineService started");
            HostApplicationLifetime.ApplicationStarted.Register(async () =>
            {
                var fileInfos = CheckScriptFiles();
                var complierResult = await CompilerScript(fileInfos);
                if (complierResult.Success) _ = RunAssembly(complierResult);
                else HostApplicationLifetime.StopApplication();
            });
            await Task.CompletedTask;
        }
        public async Task StopAsync(CancellationToken cancellationToken)
        {
            if (instance is not null) await StopScript();

            _logger.LogInformation("EngineService stopped");
            await Task.CompletedTask;
        }
        public async Task StopScript()
        {
            Options.StopExecuteMethods.Each(method => type.Invoke(instance, method));

            Console.WriteLine(ConsoleHelper.WarpStringANSIColor("RUNNING STOP...", null, ConsoleColor.Green));

            _logger.LogInformation("Stopping script");
            await Task.CompletedTask;
        }


        public List<FileInfo> CheckScriptFiles()
        {
            bool scriptFileIsNotExist = false;
            List<FileInfo> scriptFiles = [];
            Options.ScriptFiles.Each(file =>
            {
                var info = new FileInfo(file);
                if (!info.Exists)
                {
                    scriptFileIsNotExist = true;
                    _logger.LogError($"Script file {info.FullName} not found");
                }
                else
                    scriptFiles.Add(info);
            });

            if (scriptFileIsNotExist) HostApplicationLifetime.StopApplication();
            return scriptFiles;
        }
        public async Task<CompilerResult> CompilerScript(List<FileInfo> scriptFiles)
        {
            _logger.LogInformation($"Start compiling {scriptFiles.Count} files including {scriptFiles[0].Name}..");

            var result = await compiler.CompileSourceFilesAsync("ScriptContext", scriptFiles.Select(x => x.FullName), null, [.. Options.ReferrerAssemblyPaths], [.. Options.ReferrerAssemblyNames], false);
            if (result.Success)
                _logger.LogInformation($"Compilation success. assembly binary size {result.Binary.Length}. md5 hash is {result.Binary.GetMD5Hash().ToHexString()}.");
            else
                result.Diagnostics.Each(diagnostic => _logger.LogError($"{diagnostic.ToString()}"));
            return result;
        }
        public Task RunAssembly(CompilerResult compilerResult)
        {
            _logger.LogInformation($"Starting script.");
            Console.WriteLine(ConsoleHelper.WarpStringANSIColor("RUNNING START...", null, ConsoleColor.Green));

            return Task.Run(() =>
            {
                var context = new AssemblyLoadContextEx("ScriptContextAssembly", Options.ReferrerAssemblyNames, Options.ReferrerAssemblyPaths, true);
                using var codeStream = compilerResult.Binary.GetStream();
                var assembly = context.LoadFromStream(codeStream);

                type = assembly.GetType(Options.StartTypeFullName);
                instance = Activator.CreateInstance(type);

                Options.StartExecuteMethods.Each(method => type.Invoke(instance, method));
            });
        }
    }
}

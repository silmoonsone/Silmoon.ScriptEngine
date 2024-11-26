using Microsoft.CodeAnalysis.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Silmoon.Extension;
using Silmoon.Runtime;
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
        object Instance = null;
        Type Type = null;

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
                if (complierResult.Success) _ = RunBinary(complierResult);
                else HostApplicationLifetime.StopApplication();
            });
            await Task.CompletedTask;
        }
        public async Task StopAsync(CancellationToken cancellationToken)
        {
            if (Instance is not null) await StopScript();

            _logger.LogInformation("EngineService stopped");
            await Task.CompletedTask;
        }
        public async Task StopScript()
        {
            if (!Options.StopMethod.IsNullOrEmpty()) Type.GetMethod(Options.StopMethod)?.Invoke(Instance, Options.StopMethodParameter);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("RUNNING STOP...");
            Console.WriteLine();
            Console.ResetColor();

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

            var result = await compiler.CompileSourceFilesAsync("ScriptContext", scriptFiles.Select(x => x.FullName), null, null, [.. Options.AdditionAssemblyNames], false);
            if (result.Success)
                _logger.LogInformation($"Compilation success. assembly binary size {result.Binary.Length}. md5 hash is {result.Binary.GetMD5Hash().ToHexString()}.");
            else result.Diagnostics.Each(diagnostic => _logger.LogError($"{diagnostic.ToString()}"));
            return result;
        }
        public Task RunBinary(CompilerResult compilerResult)
        {
            _logger.LogInformation($"Starting script.");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine();
            Console.WriteLine("RUNNING START...");
            Console.ResetColor();

            return Task.Run(() =>
            {
                var context = new AssemblyLoadContextEx("ScriptContextAssembly", true);
                using var codeStream = compilerResult.Binary.GetStream();
                var assembly = context.LoadFromStream(codeStream);

                Type = assembly.GetType(Options.MainTypeFullName);
                Instance = Activator.CreateInstance(Type);

                if (!Options.StartMethod.IsNullOrEmpty())
                    Type.GetMethod(Options.StartMethod)?.Invoke(Instance, Options.StartMethodParameter);
            });
        }
    }
}

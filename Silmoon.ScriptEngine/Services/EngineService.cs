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

        EngineInstance EngineInstance { get; set; } = null;

        public EngineService(ILogger<EngineService> logger, IOptions<EngineServiceOptions> _options, IHostApplicationLifetime hostApplicationLifetime)
        {
            _logger = logger;
            HostApplicationLifetime = hostApplicationLifetime;
            Options = _options.Value;
        }
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("EngineService started");
            HostApplicationLifetime.ApplicationStarted.Register(async () => await StartScript());
            await Task.CompletedTask;
        }
        public async Task StopAsync(CancellationToken cancellationToken)
        {
            if (EngineInstance.Instance is not null) await StopScript();

            _logger.LogInformation("EngineService stopped");
            await Task.CompletedTask;
        }
        async Task StartScript()
        {
            EngineInstance?.Dispose();
            EngineInstance = new EngineInstance(Options);
            EngineInstance.OnOutput += (s) => _logger.LogInformation(s);
            EngineInstance.OnError += (s, e) => _logger.LogError(s);
            EngineInstance.Preprocess();
            var fileInfos = EngineInstance.CheckFiles();
            if (fileInfos.State)
            {
                _logger.LogInformation("Script files loaded successfully");
                var complierResult = await EngineInstance.Compile();
                if (complierResult.Success)
                {
                    _logger.LogInformation("Script compiled successfully");
                    EngineInstance.LoadAssembly();
                    EngineInstance.CreateInstance();
                    EngineInstance.Type.Invoke(EngineInstance.Instance, Options.StartExecuteMethod);
                }
                else
                {
                    foreach (var item in complierResult.Diagnostics)
                    {
                        _logger.LogError(item.GetMessage());
                    }
                    _logger.LogError("Script compiled failed");
                    HostApplicationLifetime.StopApplication();
                }
            }
            else
            {
                _logger.LogError(fileInfos.Message);
                HostApplicationLifetime.StopApplication();
            }

        }
        public async Task StopScript()
        {
            EngineInstance.Type.Invoke(EngineInstance.Instance, Options.StopExecuteMethod);
            EngineInstance?.Dispose();
            _logger.LogInformation("Stopping script");
            await Task.CompletedTask;
        }
    }
}

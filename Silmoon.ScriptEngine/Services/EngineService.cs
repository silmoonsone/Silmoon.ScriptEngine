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
            EngineInstance = new EngineInstance(Options);
        }
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("EngineService started");
            HostApplicationLifetime.ApplicationStarted.Register(async () =>
            {
                var fileInfos = EngineInstance.CheckScriptFiles();
                if (fileInfos.State)
                {
                    var complierResult = await EngineInstance.CompileScript(fileInfos.Data);
                    if (complierResult.Success)
                    {
                        EngineInstance.LoadAssembly(complierResult);
                        Options.StartExecuteMethods.Each(method => EngineInstance.Type.Invoke(EngineInstance.Instance, method));
                    }
                    else HostApplicationLifetime.StopApplication();
                }
                else HostApplicationLifetime.StopApplication();
            });
            await Task.CompletedTask;
        }
        public async Task StopAsync(CancellationToken cancellationToken)
        {
            if (EngineInstance.Instance is not null) await StopScript();

            _logger.LogInformation("EngineService stopped");
            await Task.CompletedTask;
        }
        public async Task StopScript()
        {
            Options.StopExecuteMethods.Each(method => EngineInstance.Type.Invoke(EngineInstance.Instance, method));
            _logger.LogInformation("Stopping script");
            await Task.CompletedTask;
        }


    }
}

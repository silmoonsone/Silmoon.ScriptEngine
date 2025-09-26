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

namespace Silmoon.ScriptEngine.ServiceHostTesting
{
    public class EngineService : IHostedService
    {
        ILogger<EngineService> _logger;
        EngineServiceOptions Options;
        IHostApplicationLifetime HostApplicationLifetime;

        EngineCompiler EngineCompiler { get; set; } = null;
        EngineExecuter<object> EngineExecuter { get; set; } = null;
        public EngineService(ILogger<EngineService> logger, IOptions<EngineServiceOptions> _options, IHostApplicationLifetime hostApplicationLifetime)
        {
            _logger = logger;
            HostApplicationLifetime = hostApplicationLifetime;
            Options = _options.Value;
        }
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("EngineService started");
            HostApplicationLifetime.ApplicationStarted.Register(async () =>
            {
                EngineCompiler = new EngineCompiler(Options);
                EngineCompiler.OnOutput += (s) => _logger.LogInformation(s);
                EngineCompiler.OnError += (s, e) => _logger.LogError(s);
                EngineCompiler.Preprocess();
                var fileInfos = EngineCompiler.CheckFiles();
                if (fileInfos.State)
                {
                    _logger.LogInformation("Script files loaded successfully");
                    var complierResult = await EngineCompiler.Compile();
                    if (complierResult.Success)
                    {
                        _logger.LogInformation("Script compiled successfully");

                        EngineExecuter = EngineCompiler.NewExecuter<object>();
                        EngineExecuter.LoadAssembly();
                        EngineExecuter.CreateInstance();
                        EngineExecuter.Type.Invoke(EngineExecuter.Instance, Options.StartExecuteMethod);
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
            });
            await Task.CompletedTask;
        }
        public async Task StopAsync(CancellationToken cancellationToken)
        {
            if (EngineExecuter?.Instance is not null)
            {
                EngineExecuter.Type.Invoke(EngineExecuter.Instance, Options.StopExecuteMethod);
                EngineExecuter?.Dispose();
                _logger.LogInformation("Stopping script");
            }

            _logger.LogInformation("EngineService stopped");
            await Task.CompletedTask;
        }
    }
}

using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoTradingScripts
{
    public class ScriptProgram
    {
        EAScript1 script;

        CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        Random Random = new Random();
        public ScriptProgram()
        {
            script = new EAScript1();
        }
        public void StartScript()
        {
            Console.WriteLine("!!!ScriptProgram.StartScript");
            script.StartScript();
            SimluateTick();
        }

        public void StopScript()
        {
            cancellationTokenSource.Cancel();
            Console.WriteLine("!!!ScriptProgram.StopScript");
            script.StopScript();
        }

        void SimluateTick()
        {
            Task.Run(async () =>
            {
                double price = 2700;
                while (!cancellationTokenSource.Token.IsCancellationRequested)
                {
                    await Task.Delay(Random.Next(100, 1000));
                    price += Random.Next(-10, 10);
                    script.OnTick("XAUUSD", price);
                }
            });
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoTradingFrameworks
{
    public abstract class AutoTradingScript : IAutoTradingScript
    {
        public Dictionary<string, string> Symbols { get; set; } = [];
        public bool StartScript()
        {
            return true;
        }
        public void StopScript()
        {

        }

        public virtual void OnTick(string symbol, double price)
        {

        }

        public bool SendOrder(string symbol, double price, int lots)
        {
            Console.WriteLine($"SEND ORDER: {symbol}, {price}, {lots}");
            return true;
        }
    }
}

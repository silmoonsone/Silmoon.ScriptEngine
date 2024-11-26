using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoTradingFrameworks
{
    public interface IAutoTradingScript
    {
        bool StartScript();
        void StopScript();
        Dictionary<string, string> Symbols { get; set; }
        abstract void OnTick(string symbol, double price);
        bool SendOrder(string symbol, double price, int quantity);
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoTradingFrameworks
{
    public interface IScriptProgram
    {
        void StartScript();
        void StopScript();
    }
}

//ref: ../../../../../../AutoTradingFrameworks/bin/Debug/net8.0/AutoTradingFrameworks.dll
//dep: System.Console
//dep: System.Runtime
//dep: System.Collections
//dep: System.Private.CoreLib
//dep: System.Linq
//dep: Silmoon.ScriptEngine
//csf: ../../../../../../AuthTradingScripts/ScriptProgram.cs


using System;
using AutoTradingFrameworks;

namespace AuthTradingScripts
{
    public class EAScript1 : AutoTradingScript
    {
        int downTimes = 0;
        double beforePrice = 0;
        public override void OnTick(string symbol, double price)
        {
            if (beforePrice == 0) beforePrice = price;
            Console.WriteLine($"*** OnTick {symbol}, price {price}");

            if (price < beforePrice)
                downTimes++;
            else downTimes = 0;

            beforePrice = price;

            if (downTimes >= 2)
                SendOrder(symbol, -1, 1);

            base.OnTick(symbol, price);
        }
    }
}

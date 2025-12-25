//assemblyName: "EAScriptTestAssembly_ScriptProgram"
#pragma r "../AutoTradingFrameworks/bin/Debug/net10.0/AutoTradingFrameworks.dll"
#pragma d "System.Console"
#pragma d "System.Runtime"
#pragma d "System.Collections"
#pragma d "System.Private.CoreLib"
#pragma d "System.Linq"
#pragma d "Silmoon.ScriptEngine"
#pragma f "ScriptProgram.cs"

using System;
using AutoTradingFrameworks;

namespace AutoTradingScripts
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

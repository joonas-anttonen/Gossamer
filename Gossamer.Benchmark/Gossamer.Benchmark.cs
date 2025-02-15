﻿using Gossamer.Frontend;
using Gossamer.Logging;
using Gossamer.Utilities;

namespace Gossamer.Benchmark
{
    public class Benchmark
    {
        public static int Main(string[] args)
        {
            using Gossamer engine = new(Gossamer.Parameters.FromArgs(args));
            
            return engine.Run();
        }
    }
}
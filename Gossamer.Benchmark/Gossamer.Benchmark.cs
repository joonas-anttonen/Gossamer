using Gossamer.Frontend;
using Gossamer.Logging;
using Gossamer.Utilities;

namespace Gossamer.Benchmark
{
    public class Benchmark
    {
        public static int Main(string[] args)
        {
            using Gossamer engine = new(Gossamer.Parameters.FromArgs(args));

            try
            {
                engine.Log.AddConsoleListener();

                engine.Run();
            }
            catch (Exception ex)
            {
                engine.Log.Append(Log.Level.Error, ex.ToString(), DateTime.Now);
                return 1;
            }

            return 0;
        }
    }
}
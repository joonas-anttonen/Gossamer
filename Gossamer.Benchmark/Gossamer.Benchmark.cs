using Gossamer.Frontend;
using Gossamer.Utilities;

namespace Gossamer.Benchmark
{
    public class Benchmark
    {
        public static int Main(string[] args)
        {
            using GossamerLog log = new();
            log.AddDebugListener();

            using Gossamer gossamer = new(log, new GossamerParameters());

            try
            {
                gossamer.Run();
            }
            catch (Exception ex)
            {
                log.Append(GossamerLog.Level.Error, ex.ToString(), DateTime.Now);
                return 1;
            }

            return 0;
        }
    }
}
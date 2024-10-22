using Gossamer.FrontEnd;
using Gossamer.Utilities;

namespace Gossamer.Benchmark
{
    public class Benchmark
    {
        public static int Main(string[] args)
        {
            Gossamer gossamer = new(new GossamerParameters());
            gossamer.Run();

            return 0;
        }
    }
}
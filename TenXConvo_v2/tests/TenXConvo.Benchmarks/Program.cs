using BenchmarkDotNet.Running;
using TenXConvo.Benchmarks;

public class Program
{
    public static void Main(string[] args)
    {
        var summary = BenchmarkRunner.Run<ConsultantAndUserServiceBenchmarks>();
    }
}

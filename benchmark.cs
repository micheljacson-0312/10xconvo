using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

public class Program
{
    public static async Task Main()
    {
        var tokens = Enumerable.Range(0, 100).ToList();

        // Baseline
        var sw = Stopwatch.StartNew();
        int sent = 0;
        foreach (var token in tokens)
        {
            var (ok, _) = await SendWebPushAsync(token);
            if (ok) sent++;
        }
        sw.Stop();
        Console.WriteLine($"Baseline (Sequential): {sw.ElapsedMilliseconds} ms for {sent} items");

        // Optimized
        sw.Restart();
        var tasks = tokens.Select(token => SendWebPushAsync(token)).ToList();
        var results = await Task.WhenAll(tasks);
        int sentOpt = results.Count(r => r.ok);
        sw.Stop();
        Console.WriteLine($"Optimized (Parallel): {sw.ElapsedMilliseconds} ms for {sentOpt} items");
    }

    private static async Task<(bool ok, string error)> SendWebPushAsync(int token)
    {
        // Simulate network delay
        await Task.Delay(10);
        return (true, null);
    }
}

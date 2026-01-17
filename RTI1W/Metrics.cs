using System.Diagnostics;

namespace RTI1W;

public static class Metrics
{
    private static readonly Stopwatch stopwatch = Stopwatch.StartNew();
    private static readonly List<MetricTime> metricTimers = [];

    public static bool ActiveEvents { get; set; }

    private static long numRaySphere = 0;
    public static void EventRaySphere()
    {
        if (!ActiveEvents) return;
        Interlocked.Increment(ref numRaySphere);
    }

    private static long numRayBox = 0;
    public static void EventRayBox()
    {
        if (!ActiveEvents) return;
        Interlocked.Increment(ref numRayBox);
    }

    private static long numRayTriangle = 0;
    public static void EventRayTriangle()
    {
        if (!ActiveEvents) return;
        Interlocked.Increment(ref numRayTriangle);
    }

    private class MetricTime
    {
        public required string Name { get; set; }
        public required TimeSpan Start { get; set; }
        public required TimeSpan Stop { get; set; }
        public TimeSpan Elapsed => Stop - Start;
    }

    public static void StartTimer(string name)
    {
        var elapsed = stopwatch.Elapsed;
        MetricTime metric = new()
        {
            Name = name,
            Start = elapsed,
            Stop = elapsed
        };
        metricTimers.Add(metric);
    }

    public static void StopTimer()
    {
        metricTimers.Last().Stop = stopwatch.Elapsed;
    }

    public static void Display()
    {
        if (ActiveEvents)
        {
            WriteLine("Events:");
            WriteLine($"- Ray-Sphere: {numRaySphere:N0}");
            WriteLine($"- Ray-Box: {numRayBox:N0}");
            WriteLine($"- Ray-Triangle: {numRayTriangle:N0}");
        }
        WriteLine("Timers: ");
        foreach (var metric in metricTimers)
        {
            WriteLine($"- {metric.Name}: {metric.Elapsed.TotalSeconds:F2} secs");
        }
    }
}

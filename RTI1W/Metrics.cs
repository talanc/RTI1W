using System.Diagnostics;

namespace RTI1W;

public static class Metrics
{
    private static readonly Stopwatch stopwatch = Stopwatch.StartNew();
    private static readonly List<MetricTime> metricTimers = new();

    private static int numRaySphere = 0;
    public static void EventRaySphere()
    {
        Interlocked.Increment(ref numRaySphere);
    }

    private static int numRayBox = 0;
    public static void EventRayBox()
    {
        Interlocked.Increment(ref numRayBox);
    }

    private class MetricTime
    {
        public string Name { get; set; } = "";
        public TimeSpan Start { get; set; }
        public TimeSpan Stop { get; set; }
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
        Error.WriteLine("Events:");
        Error.WriteLine($"- Ray-Sphere: {numRaySphere:N0}");
        Error.WriteLine($"- Ray-Box: {numRayBox:N0}");
        Error.WriteLine("Timers: ");
        foreach (var metric in metricTimers)
        {
            Error.WriteLine($"- {metric.Name}: {metric.Elapsed.TotalSeconds:F2} secs");
        }
    }
}

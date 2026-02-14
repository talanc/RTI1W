using System.Diagnostics;

namespace RTI1W;

public static class Metrics
{
    private static readonly List<MetricTime> metricTimers = [];

    private static bool activeEvents = false;

    public static void ActivateEvents()
    {
        activeEvents = true;
    }

    private static long numRayBvh = 0;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void EventRayBvh()
    {
        if (!activeEvents) return;
        Interlocked.Increment(ref numRayBvh);
    }

    private static long numRaySphere = 0;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void EventRaySphere()
    {
        if (!activeEvents) return;
        Interlocked.Increment(ref numRaySphere);
    }

    private static long numRayTriangle = 0;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void EventRayTriangle()
    {
        if (!activeEvents) return;
        Interlocked.Increment(ref numRayTriangle);
    }

    private class MetricTime
    {
        public required string Name { get; set; }
        public required long Start { get; set; }
        public TimeSpan Elapsed { get; set; }
    }

    public static void StartTimer(string name)
    {
        MetricTime metric = new()
        {
            Name = name,
            Start = Stopwatch.GetTimestamp(),
        };
        metricTimers.Add(metric);
    }

    public static void StopTimer()
    {
        metricTimers.Last().Elapsed = Stopwatch.GetElapsedTime(metricTimers.Last().Start);
    }

    public static void Display()
    {
        if (activeEvents)
        {
            WriteLine("Events:");
            WriteLine($"- Ray-BVH: {numRayBvh:N0}");
            WriteLine($"- Ray-Sphere: {numRaySphere:N0}");
            WriteLine($"- Ray-Triangle: {numRayTriangle:N0}");
        }
        WriteLine("Timers:");
        foreach (var metric in metricTimers)
        {
            WriteLine($"- {metric.Name}: {metric.Elapsed.TotalSeconds:F3} secs");
        }
    }
}

using System.Globalization;
using System.Text;

namespace LoadTestRunner;

/// <summary>
/// A single measured sync operation.
/// </summary>
public readonly record struct OperationSample(
    int ClientIndex,
    int OperationIndex,
    double ElapsedMillisecondsSinceStart,
    double LatencyMilliseconds,
    int OperationCount,
    bool Success,
    string Outcome);

/// <summary>
/// Writes per-operation samples to CSV and computes the summary statistics the report
/// requires (mean, standard deviation, and percentiles rather than a bare average).
/// </summary>
public static class ResultsWriter
{
    public static string WriteRaw(RunOptions options, IReadOnlyList<OperationSample> samples)
    {
        Directory.CreateDirectory(options.OutputDirectory);
        var path = Path.Combine(options.OutputDirectory, options.ResultFileName());

        var builder = new StringBuilder();
        builder.AppendLine("client_index,operation_index,elapsed_ms,latency_ms,operation_count,success,outcome");

        foreach (var sample in samples)
        {
            builder.Append(sample.ClientIndex).Append(',')
                   .Append(sample.OperationIndex).Append(',')
                   .Append(sample.ElapsedMillisecondsSinceStart.ToString("F3", CultureInfo.InvariantCulture)).Append(',')
                   .Append(sample.LatencyMilliseconds.ToString("F3", CultureInfo.InvariantCulture)).Append(',')
                   .Append(sample.OperationCount).Append(',')
                   .Append(sample.Success ? "true" : "false").Append(',')
                   .Append(Escape(sample.Outcome))
                   .AppendLine();
        }

        File.WriteAllText(path, builder.ToString());
        return path;
    }

    private static string Escape(string value)
        => value.Contains(',') || value.Contains('"')
            ? "\"" + value.Replace("\"", "\"\"") + "\""
            : value;

    public static RunSummary Summarise(RunOptions options, IReadOnlyList<OperationSample> samples, TimeSpan wallClock)
    {
        var successful = samples.Where(s => s.Success).ToList();
        var latencies = successful.Select(s => s.LatencyMilliseconds).OrderBy(v => v).ToArray();

        double mean = latencies.Length > 0 ? latencies.Average() : double.NaN;

        // Sample standard deviation (n-1): these are repeated measurements of a process,
        // not an exhaustive population.
        double stdDev = latencies.Length > 1
            ? Math.Sqrt(latencies.Sum(v => (v - mean) * (v - mean)) / (latencies.Length - 1))
            : double.NaN;

        return new RunSummary
        {
            Configuration = options.DescribeConfiguration(),
            Clients = options.Clients,
            NoteSize = options.NoteSize,
            Repetition = options.Repetition,
            TotalRequests = samples.Count,
            SuccessfulRequests = successful.Count,
            FailedRequests = samples.Count - successful.Count,
            TotalOperations = successful.Sum(s => s.OperationCount),
            WallClockSeconds = wallClock.TotalSeconds,
            MeanLatencyMs = mean,
            StdDevLatencyMs = stdDev,
            MinLatencyMs = latencies.Length > 0 ? latencies[0] : double.NaN,
            P50LatencyMs = Percentile(latencies, 50),
            P95LatencyMs = Percentile(latencies, 95),
            P99LatencyMs = Percentile(latencies, 99),
            MaxLatencyMs = latencies.Length > 0 ? latencies[^1] : double.NaN
        };
    }

    /// <summary>
    /// Linear-interpolation percentile over a pre-sorted array.
    /// </summary>
    public static double Percentile(double[] sorted, double percentile)
    {
        if (sorted.Length == 0) return double.NaN;
        if (sorted.Length == 1) return sorted[0];

        double rank = (percentile / 100.0) * (sorted.Length - 1);
        int lower = (int)Math.Floor(rank);
        int upper = (int)Math.Ceiling(rank);

        if (lower == upper) return sorted[lower];

        double weight = rank - lower;
        return sorted[lower] * (1 - weight) + sorted[upper] * weight;
    }

    /// <summary>
    /// Appends the run summary to a shared CSV so the full matrix accumulates in one file.
    /// </summary>
    public static string AppendSummary(RunOptions options, RunSummary summary)
    {
        Directory.CreateDirectory(options.OutputDirectory);
        var path = Path.Combine(options.OutputDirectory, $"{options.Label}_summary.csv");

        bool needsHeader = !File.Exists(path);
        var builder = new StringBuilder();

        if (needsHeader)
        {
            builder.AppendLine("timestamp_utc,clients,note_size,repetition,total_requests," +
                               "successful_requests,failed_requests,total_operations,wall_clock_s," +
                               "throughput_ops_s,mean_latency_ms,stddev_latency_ms,min_ms,p50_ms,p95_ms,p99_ms,max_ms");
        }

        var inv = CultureInfo.InvariantCulture;
        builder.Append(DateTime.UtcNow.ToString("O", inv)).Append(',')
               .Append(summary.Clients).Append(',')
               .Append(summary.NoteSize).Append(',')
               .Append(summary.Repetition).Append(',')
               .Append(summary.TotalRequests).Append(',')
               .Append(summary.SuccessfulRequests).Append(',')
               .Append(summary.FailedRequests).Append(',')
               .Append(summary.TotalOperations).Append(',')
               .Append(summary.WallClockSeconds.ToString("F3", inv)).Append(',')
               .Append(summary.ThroughputOperationsPerSecond.ToString("F3", inv)).Append(',')
               .Append(summary.MeanLatencyMs.ToString("F3", inv)).Append(',')
               .Append(summary.StdDevLatencyMs.ToString("F3", inv)).Append(',')
               .Append(summary.MinLatencyMs.ToString("F3", inv)).Append(',')
               .Append(summary.P50LatencyMs.ToString("F3", inv)).Append(',')
               .Append(summary.P95LatencyMs.ToString("F3", inv)).Append(',')
               .Append(summary.P99LatencyMs.ToString("F3", inv)).Append(',')
               .Append(summary.MaxLatencyMs.ToString("F3", inv))
               .AppendLine();

        File.AppendAllText(path, builder.ToString());
        return path;
    }
}

public sealed class RunSummary
{
    public required string Configuration { get; init; }
    public int Clients { get; init; }
    public int NoteSize { get; init; }
    public int Repetition { get; init; }
    public int TotalRequests { get; init; }
    public int SuccessfulRequests { get; init; }
    public int FailedRequests { get; init; }
    public int TotalOperations { get; init; }
    public double WallClockSeconds { get; init; }
    public double MeanLatencyMs { get; init; }
    public double StdDevLatencyMs { get; init; }
    public double MinLatencyMs { get; init; }
    public double P50LatencyMs { get; init; }
    public double P95LatencyMs { get; init; }
    public double P99LatencyMs { get; init; }
    public double MaxLatencyMs { get; init; }

    public double ThroughputOperationsPerSecond
        => WallClockSeconds > 0 ? TotalOperations / WallClockSeconds : double.NaN;

    public override string ToString()
    {
        var inv = CultureInfo.InvariantCulture;
        return $"""
            Configuration : {Configuration}
            Requests      : {TotalRequests} ({SuccessfulRequests} ok, {FailedRequests} failed)
            Operations    : {TotalOperations} over {WallClockSeconds.ToString("F2", inv)}s
            Throughput    : {ThroughputOperationsPerSecond.ToString("F1", inv)} ops/s
            Latency (ms)  : mean {MeanLatencyMs.ToString("F2", inv)}  sd {StdDevLatencyMs.ToString("F2", inv)}
                            min {MinLatencyMs.ToString("F2", inv)}  p50 {P50LatencyMs.ToString("F2", inv)}  p95 {P95LatencyMs.ToString("F2", inv)}  p99 {P99LatencyMs.ToString("F2", inv)}  max {MaxLatencyMs.ToString("F2", inv)}
            """;
    }
}

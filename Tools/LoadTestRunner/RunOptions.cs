namespace LoadTestRunner;

/// <summary>
/// Command line options for the load test runner.
///
/// Matches the interface documented in the MSc report:
///     LoadTestRunner --clients N --duration Ns
/// with additional parameters needed to sweep the benchmark matrix.
/// </summary>
public sealed class RunOptions
{
    public int Clients { get; init; } = 1;
    public TimeSpan Duration { get; init; } = TimeSpan.FromSeconds(30);
    public int NoteSize { get; init; } = 500;
    public int Repetition { get; init; } = 1;
    public int BatchSize { get; init; } = 20;
    public TimeSpan WarmUp { get; init; } = TimeSpan.FromSeconds(5);
    public string BaseUrl { get; init; } = "http://localhost:5266";
    public string OutputDirectory { get; init; } = "Documentation/Benchmarks/raw";
    public string Label { get; init; } = "run";
    public bool DryRun { get; init; }

    public string DescribeConfiguration() =>
        $"clients={Clients} duration={Duration.TotalSeconds:0}s noteSize={NoteSize} " +
        $"batchSize={BatchSize} warmUp={WarmUp.TotalSeconds:0}s rep={Repetition}";

    /// <summary>
    /// Filename used for the per-operation CSV. Encoding the parameters in the name keeps
    /// each raw file self-describing, which matters when the results are cited in the report.
    /// </summary>
    public string ResultFileName() =>
        $"{Label}_c{Clients:D3}_n{NoteSize:D5}_d{Duration.TotalSeconds:0}s_r{Repetition:D2}.csv";

    public static RunOptions Parse(string[] args)
    {
        var options = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            if (!arg.StartsWith("--", StringComparison.Ordinal))
            {
                throw new ArgumentException($"Unexpected argument '{arg}'. Expected an option starting with '--'.");
            }

            var key = arg[2..];

            if (key.Equals("dry-run", StringComparison.OrdinalIgnoreCase))
            {
                options[key] = "true";
                continue;
            }

            if (i + 1 >= args.Length)
            {
                throw new ArgumentException($"Option '--{key}' requires a value.");
            }

            options[key] = args[++i];
        }

        var duration = ParseDuration(Get(options, "duration", "30s"));
        var warmUp = ParseDuration(Get(options, "warmup", "5s"));

        var parsed = new RunOptions
        {
            Clients = int.Parse(Get(options, "clients", "1")),
            Duration = duration,
            NoteSize = int.Parse(Get(options, "note-size", "500")),
            Repetition = int.Parse(Get(options, "repetition", "1")),
            BatchSize = int.Parse(Get(options, "batch-size", "20")),
            WarmUp = warmUp,
            BaseUrl = Get(options, "base-url", "http://localhost:5266").TrimEnd('/'),
            OutputDirectory = Get(options, "output", "Documentation/Benchmarks/raw"),
            Label = Get(options, "label", "run"),
            DryRun = Get(options, "dry-run", "false").Equals("true", StringComparison.OrdinalIgnoreCase)
        };

        Validate(parsed);
        return parsed;
    }

    private static string Get(IDictionary<string, string> options, string key, string fallback)
        => options.TryGetValue(key, out var value) ? value : fallback;

    private static void Validate(RunOptions options)
    {
        if (options.Clients < 1)
            throw new ArgumentException("--clients must be at least 1.");
        if (options.Duration <= TimeSpan.Zero)
            throw new ArgumentException("--duration must be positive.");
        if (options.NoteSize < 1)
            throw new ArgumentException("--note-size must be at least 1.");
        if (options.BatchSize < 1)
            throw new ArgumentException("--batch-size must be at least 1.");
        if (options.WarmUp < TimeSpan.Zero)
            throw new ArgumentException("--warmup cannot be negative.");
        if (options.Repetition < 1)
            throw new ArgumentException("--repetition must be at least 1.");
    }

    /// <summary>
    /// Accepts "30s", "500ms", "2m", or a bare number interpreted as seconds.
    /// </summary>
    private static TimeSpan ParseDuration(string value)
    {
        value = value.Trim();

        if (value.EndsWith("ms", StringComparison.OrdinalIgnoreCase))
            return TimeSpan.FromMilliseconds(double.Parse(value[..^2]));
        if (value.EndsWith("s", StringComparison.OrdinalIgnoreCase))
            return TimeSpan.FromSeconds(double.Parse(value[..^1]));
        if (value.EndsWith("m", StringComparison.OrdinalIgnoreCase))
            return TimeSpan.FromMinutes(double.Parse(value[..^1]));

        return TimeSpan.FromSeconds(double.Parse(value));
    }

    public static string Usage => """
        LoadTestRunner - benchmark harness for the CRDT notes server.

        Usage:
          dotnet run --project Tools/LoadTestRunner -- --clients N --duration 30s [options]

        Options:
          --clients N        Number of concurrent simulated clients.       (default 1)
          --duration Ns      Measurement window, e.g. 30s / 500ms / 2m.    (default 30s)
          --note-size N      Characters pre-seeded into each client note.  (default 500)
          --batch-size N     CRDT operations per sync request.            (default 20)
          --warmup Ns        Unmeasured warm-up window.                    (default 5s)
          --repetition N     Repetition index, recorded in the CSV name.   (default 1)
          --base-url URL     Server base URL.        (default http://localhost:5266)
          --output DIR       Output directory for the raw CSV.
          --label NAME       Prefix for the CSV filename.                  (default run)
          --dry-run          Validate connectivity and exit without measuring.

        The harness requires a running server. Start one with:
          dotnet run --project Server --launch-profile http
        """;
}

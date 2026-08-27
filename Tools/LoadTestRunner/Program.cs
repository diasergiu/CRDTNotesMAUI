using System.Diagnostics;
using LoadTestRunner;

if (args.Contains("--help") || args.Contains("-h"))
{
    Console.WriteLine(RunOptions.Usage);
    return 0;
}

RunOptions options;
try
{
    options = RunOptions.Parse(args);
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Argument error: {ex.Message}");
    Console.Error.WriteLine();
    Console.Error.WriteLine(RunOptions.Usage);
    return 2;
}

Console.WriteLine($"LoadTestRunner  {options.DescribeConfiguration()}");
Console.WriteLine($"Target          {options.BaseUrl}");

var handler = new SocketsHttpHandler
{
    // The default connection limit would itself become the bottleneck at 100 clients,
    // which would make the results a measurement of the harness, not the server.
    MaxConnectionsPerServer = Math.Max(64, options.Clients * 2),
    PooledConnectionLifetime = TimeSpan.FromMinutes(10)
};

using var http = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(100) };

// --- Connectivity check -------------------------------------------------------
try
{
    // RequestValidationMiddleware rejects any non-/api/user route without an x-user-id
    // header, so the probe must supply one even though the endpoint ignores it.
    var probeRequest = new HttpRequestMessage(HttpMethod.Get, $"{options.BaseUrl}/api/test");
    probeRequest.Headers.Add("x-user-id", Guid.Empty.ToString());

    var probe = await http.SendAsync(probeRequest);
    if (!probe.IsSuccessStatusCode)
    {
        Console.Error.WriteLine($"Server probe returned {(int)probe.StatusCode}. Is the server healthy?");
        return 3;
    }
    Console.WriteLine("Server          reachable");
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Cannot reach server at {options.BaseUrl}: {ex.Message}");
    Console.Error.WriteLine("Start it with: dotnet run --project Server --launch-profile http");
    return 3;
}

if (options.DryRun)
{
    Console.WriteLine("Dry run requested - connectivity verified, exiting without measuring.");
    return 0;
}

// --- Set up clients -----------------------------------------------------------
Console.WriteLine($"Provisioning    {options.Clients} client(s), seeding {options.NoteSize} characters each...");

var clients = Enumerable.Range(0, options.Clients)
    .Select(i => new SimulatedClient(i, options, http))
    .ToList();

try
{
    await Task.WhenAll(clients.Select(c => c.SetUpAsync(CancellationToken.None)));
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Set-up failed: {ex.Message}");
    return 4;
}

Console.WriteLine("Provisioning    complete");

// --- Warm-up ------------------------------------------------------------------
var sink = new ConcurrentSampleSink();
var clock = Stopwatch.StartNew();

double measurementStartMs;

if (options.WarmUp > TimeSpan.Zero)
{
    Console.WriteLine($"Warm-up         {options.WarmUp.TotalSeconds:0}s (discarded)");
    using var warmUpCts = new CancellationTokenSource(options.WarmUp);
    await Task.WhenAll(clients.Select(c => c.RunAsync(clock, sink, warmUpCts.Token)));
}

// Everything recorded so far belongs to the warm-up and is excluded below.
measurementStartMs = clock.Elapsed.TotalMilliseconds;
int warmUpSampleCount = sink.Snapshot().Count;

// --- Measurement --------------------------------------------------------------
Console.WriteLine($"Measuring       {options.Duration.TotalSeconds:0}s...");

var measurementStopwatch = Stopwatch.StartNew();
using (var runCts = new CancellationTokenSource(options.Duration))
{
    await Task.WhenAll(clients.Select(c => c.RunAsync(clock, sink, runCts.Token)));
}
measurementStopwatch.Stop();

var measured = sink.Snapshot().Skip(warmUpSampleCount).ToList();

if (measured.Count == 0)
{
    Console.Error.WriteLine("No samples were collected. Increase --duration or check server health.");
    return 5;
}

// --- Results ------------------------------------------------------------------
var rawPath = ResultsWriter.WriteRaw(options, measured);
var summary = ResultsWriter.Summarise(options, measured, measurementStopwatch.Elapsed);
var summaryPath = ResultsWriter.AppendSummary(options, summary);

Console.WriteLine();
Console.WriteLine(summary);
Console.WriteLine();
Console.WriteLine($"Raw samples     {rawPath}");
Console.WriteLine($"Summary row     {summaryPath}");

if (summary.FailedRequests > 0)
{
    Console.WriteLine();
    Console.WriteLine($"WARNING: {summary.FailedRequests} request(s) failed. Failure breakdown:");
    foreach (var group in measured.Where(s => !s.Success)
                                  .GroupBy(s => s.Outcome)
                                  .OrderByDescending(g => g.Count()))
    {
        Console.WriteLine($"  {group.Key}: {group.Count()}");
    }
    Console.WriteLine("These must be reported alongside the latency figures, not discarded.");
}

return 0;

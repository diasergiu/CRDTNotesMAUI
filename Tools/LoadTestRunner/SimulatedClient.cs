using System.Diagnostics;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using DatabaseLibrary.Entities;

namespace LoadTestRunner;

/// <summary>
/// Simulates one client: registers a user, creates a note, seeds it to the configured
/// size, then issues CRDT sync batches for the duration of the measurement window.
/// </summary>
public sealed class SimulatedClient
{
    private readonly int _index;
    private readonly RunOptions _options;
    private readonly HttpClient _http;
    private readonly string _siteId;

    private Guid _userId;
    private Guid _noteId;
    private int _sequence;

    public SimulatedClient(int index, RunOptions options, HttpClient http)
    {
        _index = index;
        _options = options;
        _http = http;
        _siteId = $"c{index}";
    }

    /// <summary>
    /// Registers a dedicated user and note. Each client owns its own note so that the
    /// measurement reflects concurrent load rather than contention on a single row,
    /// unless the note is deliberately shared by a future contention scenario.
    /// </summary>
    public async Task SetUpAsync(CancellationToken cancellationToken)
    {
        var username = $"bench_{_options.Label}_{Guid.NewGuid():N}"[..24];
        const string password = "benchmark-password";

        var registerResponse = await _http.PostAsJsonAsync(
            $"{_options.BaseUrl}/api/user/register",
            new { Name = $"Benchmark Client {_index}", Username = username, Password = password },
            cancellationToken);

        if (!registerResponse.IsSuccessStatusCode)
        {
            var body = await registerResponse.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"Client {_index}: registration failed ({(int)registerResponse.StatusCode}): {body}");
        }

        var loginResponse = await _http.PostAsync(
            $"{_options.BaseUrl}/api/user/login?username={Uri.EscapeDataString(username)}&password={Uri.EscapeDataString(password)}",
            content: null,
            cancellationToken);

        loginResponse.EnsureSuccessStatusCode();
        _userId = ExtractGuid(await loginResponse.Content.ReadAsStringAsync(cancellationToken), "idUser");

        _noteId = Guid.NewGuid();
        var createRequest = NewRequest(HttpMethod.Post, "/api/notes");
        createRequest.Content = JsonContent.Create(new
        {
            IdNote = _noteId,
            Title = $"Benchmark note {_index}",
            Content = string.Empty,
            CreationDate = DateTime.UtcNow.ToString("O"),
            LastUpdate = DateTime.UtcNow.ToString("O"),
            Version = 1,
            DirtyFlagChangesMade = false
        });

        var createResponse = await _http.SendAsync(createRequest, cancellationToken);
        if (!createResponse.IsSuccessStatusCode)
        {
            var body = await createResponse.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"Client {_index}: note creation failed ({(int)createResponse.StatusCode}): {body}");
        }

        // The server assigns the note id, so read it back rather than assuming ours was used.
        var createdBody = await createResponse.Content.ReadAsStringAsync(cancellationToken);
        var assigned = TryExtractGuid(createdBody, "idNote");
        if (assigned.HasValue) _noteId = assigned.Value;

        await SeedNoteAsync(cancellationToken);
    }

    /// <summary>
    /// Pre-loads the note to the configured character count so that latency is measured
    /// against a realistic document size rather than an empty one.
    /// </summary>
    private async Task SeedNoteAsync(CancellationToken cancellationToken)
    {
        const int seedBatch = 500;
        var remaining = _options.NoteSize;

        while (remaining > 0 && !cancellationToken.IsCancellationRequested)
        {
            var count = Math.Min(seedBatch, remaining);
            var batch = BuildBatch(count);

            var request = NewRequest(HttpMethod.Put, "/api/notes/SendCRDTChangestoServer");
            request.Content = JsonContent.Create(batch);

            var response = await _http.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new InvalidOperationException(
                    $"Client {_index}: seeding failed ({(int)response.StatusCode}): {body}");
            }

            remaining -= count;
        }
    }

    /// <summary>
    /// Issues sync batches until the token is cancelled, recording one sample per request.
    /// Samples produced during warm-up are discarded by the caller.
    /// </summary>
    public async Task RunAsync(
        Stopwatch clock,
        ConcurrentSampleSink sink,
        CancellationToken cancellationToken)
    {
        int operationIndex = 0;

        while (!cancellationToken.IsCancellationRequested)
        {
            var batch = BuildBatch(_options.BatchSize);
            var request = NewRequest(HttpMethod.Put, "/api/notes/SendCRDTChangestoServer");
            request.Content = JsonContent.Create(batch);

            double startedAt = clock.Elapsed.TotalMilliseconds;
            var sw = Stopwatch.StartNew();
            bool success;
            string outcome;

            try
            {
                var response = await _http.SendAsync(request, cancellationToken);
                sw.Stop();
                success = response.IsSuccessStatusCode;
                outcome = success ? "ok" : $"http_{(int)response.StatusCode}";
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // The measurement window closed mid-request; this is not a server failure
                // and must not be recorded as one.
                break;
            }
            catch (Exception ex)
            {
                sw.Stop();
                success = false;
                outcome = ex.GetType().Name;
            }

            sink.Add(new OperationSample(
                _index,
                operationIndex++,
                startedAt,
                sw.Elapsed.TotalMilliseconds,
                batch.Count,
                success,
                outcome));
        }
    }

    private List<CRDTCharacter> BuildBatch(int count)
    {
        var batch = new List<CRDTCharacter>(count);
        var now = DateTime.UtcNow.ToString("O");

        for (int i = 0; i < count; i++)
        {
            _sequence++;
            batch.Add(new CRDTCharacter
            {
                IdCharacter = $"({_sequence}),({_siteId})",
                IdNote = _noteId,
                Character = (char)('a' + (_sequence % 26)),
                Operation = "insert",
                ClockDateTime = DateTime.Parse(now),
                Tombstone = false
            });
        }

        return batch;
    }

    private HttpRequestMessage NewRequest(HttpMethod method, string path)
    {
        var request = new HttpRequestMessage(method, $"{_options.BaseUrl}{path}");
        request.Headers.Add("x-user-id", _userId.ToString());
        return request;
    }

    private static Guid ExtractGuid(string json, string propertyName)
        => TryExtractGuid(json, propertyName)
           ?? throw new InvalidOperationException($"Could not find '{propertyName}' in response: {json}");

    /// <summary>
    /// The API wraps payloads in ApiResponse, so search the document rather than assuming
    /// a fixed shape.
    /// </summary>
    private static Guid? TryExtractGuid(string json, string propertyName)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            return FindGuid(document.RootElement, propertyName);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static Guid? FindGuid(JsonElement element, string propertyName)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    if (property.NameEquals(propertyName)
                        && property.Value.ValueKind == JsonValueKind.String
                        && Guid.TryParse(property.Value.GetString(), out var value))
                    {
                        return value;
                    }

                    var nested = FindGuid(property.Value, propertyName);
                    if (nested.HasValue) return nested;
                }
                break;

            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    var nested = FindGuid(item, propertyName);
                    if (nested.HasValue) return nested;
                }
                break;
        }

        return null;
    }
}

/// <summary>
/// Thread-safe collector for samples produced by concurrent clients.
/// </summary>
public sealed class ConcurrentSampleSink
{
    private readonly List<OperationSample> _samples = new();
    private readonly Lock _gate = new();

    public void Add(OperationSample sample)
    {
        lock (_gate)
        {
            _samples.Add(sample);
        }
    }

    public IReadOnlyList<OperationSample> Snapshot()
    {
        lock (_gate)
        {
            return _samples.ToList();
        }
    }
}

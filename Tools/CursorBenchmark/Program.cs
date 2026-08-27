using System.Diagnostics;
using DatabaseLibrary.Cursor;

// Simple performance script for the extracted CRDT cursor library.
// Usage: dotnet run --project Tools\CursorBenchmark -c Release [charCount]

int charCount = args.Length > 0 && int.TryParse(args[0], out var n) ? n : 10_000;
var clientId = Guid.NewGuid();

Console.WriteLine($"CRDT Cursor benchmark - inserting {charCount:N0} characters (client {clientId})");
Console.WriteLine(new string('-', 60));

// Warm up the JIT so the measured run is representative.
RunInsertPass(Math.Min(1_000, charCount), clientId, report: false);

// 1) Sequential append at the end of the document.
{
    var cursor = new NoteCursor(string.Empty, clientId);
    var inserted = new List<DatabaseLibrary.Entities.Client.CRDTCharacterClient>(charCount);
    var sw = Stopwatch.StartNew();
    for (int i = 0; i < charCount; i++)
        inserted.Add(cursor.InsertCharacter(i, (char)('a' + (i % 26))));
    sw.Stop();
    Report("Append at end", charCount, sw);
    Console.WriteLine($"    final length: {cursor.GetString().Length:N0}");

    // 2) Merge the produced operations into a fresh cursor on another client.
    var remote = new NoteCursor(string.Empty, Guid.NewGuid());
    var mergeSw = Stopwatch.StartNew();
    foreach (var op in inserted)
        remote.MergeCharacter(op);
    mergeSw.Stop();
    Report("Merge remote ops", charCount, mergeSw);
    Console.WriteLine($"    merged length: {remote.GetString().Length:N0}");
}

// 3) GetString reconstruction cost on a populated document.
{
    var cursor = BuildCursor(charCount, clientId);
    var sw = Stopwatch.StartNew();
    var text = cursor.GetString();
    sw.Stop();
    Console.WriteLine($"GetString ({charCount:N0} chars): {sw.Elapsed.TotalMilliseconds:N2} ms, length {text.Length:N0}");
}

static void RunInsertPass(int count, Guid clientId, bool report, string label = "")
{
    var cursor = new NoteCursor(string.Empty, clientId);
    var sw = Stopwatch.StartNew();
    for (int i = 0; i < count; i++)
        cursor.InsertCharacter(i, (char)('a' + (i % 26)));
    sw.Stop();
    if (report)
    {
        Report(label, count, sw);
        Console.WriteLine($"    final length: {cursor.GetString().Length:N0}");
    }
}

static NoteCursor BuildCursor(int count, Guid clientId)
{
    var cursor = new NoteCursor(string.Empty, clientId);
    for (int i = 0; i < count; i++)
        cursor.InsertCharacter(i, (char)('a' + (i % 26)));
    return cursor;
}

static void Report(string label, int count, Stopwatch sw)
{
    double ms = sw.Elapsed.TotalMilliseconds;
    double perOp = sw.Elapsed.TotalMilliseconds * 1000.0 / count; // microseconds/op
    Console.WriteLine($"{label,-18}: {ms,10:N2} ms total  |  {perOp,8:N2} us/op  |  {count / sw.Elapsed.TotalSeconds,12:N0} ops/s");
}

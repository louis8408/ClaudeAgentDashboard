using ClaudeAgentDashboard.Infrastructure.Transcripts;

namespace ClaudeAgentDashboard.Infrastructure.IntegrationTests;

public class JsonlTranscriptReaderTests
{
    [Fact]
    public void ReadRecentEntries_Returns_The_Last_N_Lines_From_A_Real_File()
    {
        var path = TempTranscriptPath();
        try
        {
            File.WriteAllLines(path,
            [
                """{"type":"user","message":{"content":"first"}}""",
                """{"type":"assistant","message":{"content":"second"}}""",
                """{"type":"assistant","message":{"content":"third"}}""",
            ]);

            var reader = new JsonlTranscriptReader();
            var entries = reader.ReadRecentEntries(path, maxEntries: 2);

            Assert.Equal(2, entries.Count);
            Assert.Contains(entries, e => e.Contains("second", StringComparison.Ordinal));
            Assert.Contains(entries, e => e.Contains("third", StringComparison.Ordinal));
        }
        finally
        {
            TryDelete(path);
        }
    }

    [Fact]
    public void ReadRecentEntries_Skips_Unparseable_Lines_Without_Throwing()
    {
        var path = TempTranscriptPath();
        try
        {
            File.WriteAllLines(path,
            [
                "not valid json at all",
                """{"type":"assistant","message":{"content":"a real entry"}}""",
            ]);

            var reader = new JsonlTranscriptReader();
            var entries = reader.ReadRecentEntries(path, maxEntries: 10);

            Assert.Contains(entries, e => e.Contains("a real entry", StringComparison.Ordinal));
        }
        finally
        {
            TryDelete(path);
        }
    }

    [Fact]
    public void ReadRecentEntries_Returns_Empty_For_A_NonExistent_File()
    {
        var reader = new JsonlTranscriptReader();

        var entries = reader.ReadRecentEntries(@"C:\this\file\does\not\exist.jsonl", maxEntries: 5);

        Assert.Empty(entries);
    }

    private static string TempTranscriptPath() => Path.Combine(Path.GetTempPath(), $"transcript-{Guid.NewGuid()}.jsonl");

    private static void TryDelete(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}

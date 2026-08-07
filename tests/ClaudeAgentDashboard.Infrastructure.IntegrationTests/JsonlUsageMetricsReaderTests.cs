using ClaudeAgentDashboard.Infrastructure.Transcripts;

namespace ClaudeAgentDashboard.Infrastructure.IntegrationTests;

public class JsonlUsageMetricsReaderTests
{
    [Fact]
    public void TryReadLatestUsage_Sums_Output_Tokens_And_Uses_The_Latest_Entrys_Context_Fields()
    {
        var path = TempTranscriptPath();
        try
        {
            File.WriteAllLines(path,
            [
                """{"type":"user","message":{"content":"hi"}}""",
                """{"type":"assistant","message":{"content":"first reply","usage":{"input_tokens":100,"output_tokens":40,"cache_creation_input_tokens":0,"cache_read_input_tokens":0}}}""",
                """{"type":"assistant","message":{"content":"second reply","usage":{"input_tokens":150,"output_tokens":60,"cache_creation_input_tokens":10,"cache_read_input_tokens":20}}}""",
            ]);

            var reader = new JsonlUsageMetricsReader();
            var snapshot = reader.TryReadLatestUsage(path);

            Assert.NotNull(snapshot);
            Assert.Equal(100, snapshot!.TokensUsed); // 40 + 60 across both assistant turns
            Assert.Equal(180, snapshot.ContextWindowTokensInUse); // 150 + 10 + 20 from the latest turn only
        }
        finally
        {
            TryDelete(path);
        }
    }

    [Fact]
    public void TryReadLatestUsage_Returns_Null_For_A_NonExistent_File()
    {
        var reader = new JsonlUsageMetricsReader();

        var snapshot = reader.TryReadLatestUsage(@"C:\this\file\does\not\exist.jsonl");

        Assert.Null(snapshot);
    }

    [Fact]
    public void TryReadLatestUsage_Returns_Null_When_No_Assistant_Turn_Has_A_Usage_Block_Yet()
    {
        var path = TempTranscriptPath();
        try
        {
            File.WriteAllLines(path,
            [
                """{"type":"user","message":{"content":"hi"}}""",
            ]);

            var reader = new JsonlUsageMetricsReader();
            var snapshot = reader.TryReadLatestUsage(path);

            Assert.Null(snapshot);
        }
        finally
        {
            TryDelete(path);
        }
    }

    [Fact]
    public void TryReadLatestUsage_Skips_Unparseable_Lines_Without_Throwing()
    {
        var path = TempTranscriptPath();
        try
        {
            File.WriteAllLines(path,
            [
                "not valid json at all",
                """{"type":"assistant","message":{"content":"ok","usage":{"input_tokens":5,"output_tokens":7,"cache_creation_input_tokens":0,"cache_read_input_tokens":0}}}""",
            ]);

            var reader = new JsonlUsageMetricsReader();
            var snapshot = reader.TryReadLatestUsage(path);

            Assert.NotNull(snapshot);
            Assert.Equal(7, snapshot!.TokensUsed);
        }
        finally
        {
            TryDelete(path);
        }
    }

    private static string TempTranscriptPath() => Path.Combine(Path.GetTempPath(), $"usage-transcript-{Guid.NewGuid()}.jsonl");

    private static void TryDelete(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}

using ClaudeAgentDashboard.Infrastructure.Transcripts;

namespace ClaudeAgentDashboard.Infrastructure.IntegrationTests;

public class JsonlAgentTitleReaderTests
{
    [Fact]
    public void ReadLatestTitle_Returns_The_Title_From_An_AiTitle_Line()
    {
        var path = TempTranscriptPath();
        try
        {
            File.WriteAllLines(path, ["""{"type":"ai-title","aiTitle":"Debug PreToolUse hook errors","sessionId":"abc"}"""]);

            var reader = new JsonlAgentTitleReader();
            var title = reader.ReadLatestTitle(path);

            Assert.Equal("Debug PreToolUse hook errors", title);
        }
        finally
        {
            TryDelete(path);
        }
    }

    [Fact]
    public void ReadLatestTitle_Uses_The_Most_Recent_Line_When_The_Title_Changed()
    {
        var path = TempTranscriptPath();
        try
        {
            File.WriteAllLines(path,
            [
                """{"type":"ai-title","aiTitle":"First title","sessionId":"abc"}""",
                """{"type":"ai-title","aiTitle":"Second title","sessionId":"abc"}""",
            ]);

            var reader = new JsonlAgentTitleReader();
            var title = reader.ReadLatestTitle(path);

            Assert.Equal("Second title", title);
        }
        finally
        {
            TryDelete(path);
        }
    }

    [Fact]
    public void ReadLatestTitle_Returns_Null_For_A_NonExistent_File()
    {
        var reader = new JsonlAgentTitleReader();

        var title = reader.ReadLatestTitle(@"C:\this\file\does\not\exist.jsonl");

        Assert.Null(title);
    }

    [Fact]
    public void ReadLatestTitle_Returns_Null_When_No_AiTitle_Line_Exists_Yet()
    {
        var path = TempTranscriptPath();
        try
        {
            File.WriteAllLines(path, ["""{"type":"user","message":{"content":"hi"}}"""]);

            var reader = new JsonlAgentTitleReader();
            var title = reader.ReadLatestTitle(path);

            Assert.Null(title);
        }
        finally
        {
            TryDelete(path);
        }
    }

    [Fact]
    public void ReadLatestTitle_Skips_Unparseable_Lines_Without_Throwing()
    {
        var path = TempTranscriptPath();
        try
        {
            File.WriteAllLines(path,
            [
                "not valid json at all",
                """{"type":"ai-title","aiTitle":"A real title","sessionId":"abc"}""",
            ]);

            var reader = new JsonlAgentTitleReader();
            var title = reader.ReadLatestTitle(path);

            Assert.Equal("A real title", title);
        }
        finally
        {
            TryDelete(path);
        }
    }

    private static string TempTranscriptPath() => Path.Combine(Path.GetTempPath(), $"title-transcript-{Guid.NewGuid()}.jsonl");

    private static void TryDelete(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}

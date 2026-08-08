using ClaudeAgentDashboard.Domain;
using ClaudeAgentDashboard.Infrastructure.Transcripts;

namespace ClaudeAgentDashboard.Infrastructure.IntegrationTests;

public class JsonlTranscriptReaderTests
{
    [Fact]
    public void ReadRecentEntries_Returns_The_Last_N_Real_Conversational_Turns_Oldest_First()
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

            Assert.Equal(
                [new TranscriptEntry("assistant", "second"), new TranscriptEntry("assistant", "third")],
                entries);
        }
        finally
        {
            TryDelete(path);
        }
    }

    [Fact]
    public void ReadRecentEntries_Excludes_NonChat_Lines_Like_Hook_Events_And_Metadata()
    {
        var path = TempTranscriptPath();
        try
        {
            File.WriteAllLines(path,
            [
                """{"type":"assistant","message":{"content":"a real reply"}}""",
                """{"type":"hook_success","hookName":"PreToolUse:PowerShell"}""",
                """{"type":"last-prompt","lastPrompt":"something"}""",
                """{"type":"attachment","attachment":{"type":"total_tokens_reminder"}}""",
            ]);

            var reader = new JsonlTranscriptReader();
            var entries = reader.ReadRecentEntries(path, maxEntries: 10);

            Assert.Equal([new TranscriptEntry("assistant", "a real reply")], entries);
        }
        finally
        {
            TryDelete(path);
        }
    }

    [Fact]
    public void ReadRecentEntries_Excludes_User_Turns_With_No_Extractable_Text_Such_As_Tool_Results()
    {
        var path = TempTranscriptPath();
        try
        {
            File.WriteAllLines(path,
            [
                """{"type":"user","message":{"role":"user","content":[{"tool_use_id":"abc","type":"tool_result"}]}}""",
                """{"type":"user","message":{"content":"a real question"}}""",
            ]);

            var reader = new JsonlTranscriptReader();
            var entries = reader.ReadRecentEntries(path, maxEntries: 10);

            Assert.Equal([new TranscriptEntry("user", "a real question")], entries);
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

            Assert.Equal([new TranscriptEntry("assistant", "a real entry")], entries);
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

using ClaudeAgentDashboard.Domain;
using ClaudeAgentDashboard.Infrastructure.Transcripts;

namespace ClaudeAgentDashboard.Infrastructure.IntegrationTests;

public class JsonlPermissionModeReaderTests
{
    [Theory]
    [InlineData("default", PermissionMode.Manual)]
    [InlineData("acceptEdits", PermissionMode.AcceptEdits)]
    [InlineData("plan", PermissionMode.Plan)]
    [InlineData("auto", PermissionMode.Auto)]
    public void ReadLatestPermissionMode_Maps_Each_Known_Raw_Value(string raw, PermissionMode expected)
    {
        var path = TempTranscriptPath();
        try
        {
            File.WriteAllLines(path, [$$"""{"type":"permission-mode","permissionMode":"{{raw}}","sessionId":"abc"}"""]);

            var reader = new JsonlPermissionModeReader();
            var mode = reader.ReadLatestPermissionMode(path);

            Assert.Equal(expected, mode);
        }
        finally
        {
            TryDelete(path);
        }
    }

    [Fact]
    public void ReadLatestPermissionMode_Uses_The_Most_Recent_Line_When_Mode_Changed()
    {
        var path = TempTranscriptPath();
        try
        {
            File.WriteAllLines(path,
            [
                """{"type":"permission-mode","permissionMode":"default","sessionId":"abc"}""",
                """{"type":"permission-mode","permissionMode":"plan","sessionId":"abc"}""",
            ]);

            var reader = new JsonlPermissionModeReader();
            var mode = reader.ReadLatestPermissionMode(path);

            Assert.Equal(PermissionMode.Plan, mode);
        }
        finally
        {
            TryDelete(path);
        }
    }

    [Fact]
    public void ReadLatestPermissionMode_Returns_Unknown_For_An_Unrecognized_Raw_Value()
    {
        var path = TempTranscriptPath();
        try
        {
            File.WriteAllLines(path, ["""{"type":"permission-mode","permissionMode":"somethingNew","sessionId":"abc"}"""]);

            var reader = new JsonlPermissionModeReader();
            var mode = reader.ReadLatestPermissionMode(path);

            Assert.Equal(PermissionMode.Unknown, mode);
        }
        finally
        {
            TryDelete(path);
        }
    }

    [Fact]
    public void ReadLatestPermissionMode_Returns_Unknown_For_A_NonExistent_File()
    {
        var reader = new JsonlPermissionModeReader();

        var mode = reader.ReadLatestPermissionMode(@"C:\this\file\does\not\exist.jsonl");

        Assert.Equal(PermissionMode.Unknown, mode);
    }

    [Fact]
    public void ReadLatestPermissionMode_Returns_Unknown_When_No_PermissionMode_Line_Exists_Yet()
    {
        var path = TempTranscriptPath();
        try
        {
            File.WriteAllLines(path, ["""{"type":"user","message":{"content":"hi"}}"""]);

            var reader = new JsonlPermissionModeReader();
            var mode = reader.ReadLatestPermissionMode(path);

            Assert.Equal(PermissionMode.Unknown, mode);
        }
        finally
        {
            TryDelete(path);
        }
    }

    private static string TempTranscriptPath() => Path.Combine(Path.GetTempPath(), $"mode-transcript-{Guid.NewGuid()}.jsonl");

    private static void TryDelete(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}

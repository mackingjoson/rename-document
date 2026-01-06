using Xunit;
using System.IO;

public class UtilitiesTests
{
    [Fact]
    public void SanitizeFileName_RemovesInvalidChars()
    {
        var input = "SP: No/ 06110*MIN?";
        var outp = Utilities.SanitizeFileName(input);
        Assert.DoesNotContain(Path.GetInvalidFileNameChars(), outp);
    }

    [Fact]
    public void MakeIncrementalPath_CreatesNewPathIfExists()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "RenameDocumentTests");
        Directory.CreateDirectory(tempDir);
        var basePath = Path.Combine(tempDir, "SP No. 06110-MIN.pdf");
        File.WriteAllText(basePath, "x");
        var p1 = Utilities.MakeIncrementalPath(basePath);
        Assert.True(p1.EndsWith("SP No. 06110-MIN (1).pdf"));
        // cleanup
        File.Delete(basePath);
        Directory.Delete(tempDir);
    }
}
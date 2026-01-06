using Xunit;

public class RegexTests
{
    [Theory]
    [InlineData("CA-G.R. SP No. 06110-MIN", "SP No. 06110-MIN")]
    [InlineData("CA-G.R. SP No.06110-MIN", "SP No. 06110-MIN")]
    [InlineData("CA.G.R. SP No. 06110 - MIN", "SP No. 06110-MIN")]
    [InlineData("Random text CA-G.R. SP No. 12345-XYZ More", "SP No. 12345-XYZ")]
    [InlineData("CA-G.R.\nSP No.\n06110-MIN", "SP No. 06110-MIN")]
    public void FindCaseIdentifier_CapturesDifferentVariants(string input, string expected)
    {
        var result = PdfRenamer.FindCaseIdentifier(input);
        Assert.Equal(expected, result);
    }
}
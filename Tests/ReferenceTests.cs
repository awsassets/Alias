using Xunit;

public class ReferenceTests
{
    [Theory]
    [InlineData(@"C:\Source\Catel.Core\output\Catel.Core.dll", "Catel.Core.dll")]
    [InlineData(@"C:\Source\Catel.Core\output\nl\Catel.Core.resources.dll", "nl/Catel.Core.resources.dll")]
    public void RelativePath(string input, string expectedOutput)
    {
        var reference = new Reference(input);

        Assert.Equal(expectedOutput, reference.RelativeFileName);
    }
}
namespace Shadow.FunkyGibbon.Tests;

public class FunctionTests
{
    [Fact]
    public void Function_ShouldExist()
    {
        // This is a placeholder test to verify test infrastructure works
        // Add actual function tests as you develop
        Assert.True(true);
    }

    [Theory]
    [InlineData("test@example.com")]
    [InlineData("user@domain.com")]
    public void EmailValidation_ShouldAcceptValidEmails(string email)
    {
        // Example of data-driven test
        Assert.Contains("@", email);
        Assert.Contains(".", email);
    }
}

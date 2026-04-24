using CsvImporter.Core.Services;

namespace CsvImporter.Tests;

public class CredentialServiceTests
{
    [Fact]
    public void Encrypt_ThenDecrypt_RoundTripsCorrectly()
    {
        var svc       = new CredentialService();
        var plaintext = "S3cr3tP@ssword!";
        var cipher    = svc.Encrypt(plaintext);
        var decrypted = svc.Decrypt(cipher);

        Assert.NotEqual(plaintext, cipher);
        Assert.Equal(plaintext, decrypted);
    }

    [Fact]
    public void Encrypt_ProducesBase64String()
    {
        var svc    = new CredentialService();
        var cipher = svc.Encrypt("test");

        // Should not throw
        var bytes = Convert.FromBase64String(cipher);
        Assert.NotEmpty(bytes);
    }

    [Theory]
    [InlineData("")]
    [InlineData("short")]
    [InlineData("A long password with special chars: äöü @#$%!")]
    public void Encrypt_Decrypt_Various_Inputs(string input)
    {
        var svc       = new CredentialService();
        var decrypted = svc.Decrypt(svc.Encrypt(input));
        Assert.Equal(input, decrypted);
    }
}

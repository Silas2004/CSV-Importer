using System.Security.Cryptography;
using System.Text;

namespace CsvImporter.Core.Services;

public class CredentialService
{
    private static readonly byte[] Entropy = "CsvImporter.v1"u8.ToArray();

    public string Encrypt(string plaintext)
    {
        var bytes     = Encoding.UTF8.GetBytes(plaintext);
        var encrypted = ProtectedData.Protect(bytes, Entropy, DataProtectionScope.CurrentUser);
        return Convert.ToBase64String(encrypted);
    }

    public string Decrypt(string ciphertext)
    {
        var bytes     = Convert.FromBase64String(ciphertext);
        var decrypted = ProtectedData.Unprotect(bytes, Entropy, DataProtectionScope.CurrentUser);
        return Encoding.UTF8.GetString(decrypted);
    }
}

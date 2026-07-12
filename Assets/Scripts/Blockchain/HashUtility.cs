using System.Security.Cryptography;
using System.Text;

public static class HashUtility
{
    public static string Sha256(string input)
    {
        using var sha256 = SHA256.Create();
        byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));

        var builder = new StringBuilder(bytes.Length * 2);
        foreach (byte b in bytes)
            builder.Append(b.ToString("x2"));

        return builder.ToString();
    }
}

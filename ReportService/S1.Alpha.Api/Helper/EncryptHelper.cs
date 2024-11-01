using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace S1.Alpha.Api.Helper;

public static class EncryptHelper
{
    public static async ValueTask<byte[]> EncryptMessage<T>(string key, string iv, T data)
    {
        var value = JsonSerializer.Serialize(data, new JsonSerializerOptions
        {
            WriteIndented = false
        });

        using Aes aes = Aes.Create();
        aes.Key = Encoding.UTF8.GetBytes(key);
        aes.IV = Encoding.UTF8.GetBytes(iv);
        aes.Padding = PaddingMode.PKCS7;

        using MemoryStream output = new();
        using CryptoStream cryptoStream = new(output, aes.CreateEncryptor(), CryptoStreamMode.Write);
        await cryptoStream.WriteAsync(Encoding.Unicode.GetBytes(value));
        await cryptoStream.FlushFinalBlockAsync();

        return output.ToArray();
    }

    public static async ValueTask<T?> DecryptMessage<T>(string key, string iv, string message)
    {
        using Aes aes = Aes.Create();
        aes.Key = Encoding.UTF8.GetBytes(key);
        aes.IV = Encoding.UTF8.GetBytes(iv);
        aes.Padding = PaddingMode.PKCS7;

        using MemoryStream output = new();
        using CryptoStream cryptoStream = new(output, aes.CreateDecryptor(), CryptoStreamMode.Write);
        await cryptoStream.WriteAsync(Convert.FromHexString(message));
        await cryptoStream.FlushFinalBlockAsync();

        return JsonSerializer.Deserialize<T>((Encoding.UTF8.GetString(output.ToArray()).Replace("\0", "")), new JsonSerializerOptions
        {
            WriteIndented = false,
            
        });
    }
}

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
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using MemoryStream output = new();
        using CryptoStream cryptoStream = new(output, aes.CreateEncryptor(), CryptoStreamMode.Write);

        var bytes = Encoding.UTF8.GetBytes(value);
        await cryptoStream.WriteAsync(bytes.AsMemory(0, bytes.Length));
        await cryptoStream.FlushFinalBlockAsync();

        return output.ToArray();
    }

    public static async ValueTask<T?> DecryptMessage<T>(string key, string iv, string message)
    {
        using Aes aes = Aes.Create();
        aes.Key = Encoding.UTF8.GetBytes(key);
        aes.IV = Encoding.UTF8.GetBytes(iv);
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using MemoryStream output = new();
        using CryptoStream cryptoStream = new(output, aes.CreateDecryptor(), CryptoStreamMode.Write);

        var bytes = Convert.FromHexString(message);
        await cryptoStream.WriteAsync(bytes.AsMemory(0, bytes.Length));
        await cryptoStream.FlushFinalBlockAsync();

        return JsonSerializer.Deserialize<T>((Encoding.UTF8.GetString(output.ToArray())), new JsonSerializerOptions
        {
            WriteIndented = false,
            
        });
    }
}

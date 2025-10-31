using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using MQR.Services.Model;

namespace MQR.Services.Credentials;

public interface ICredentialEncryptor
{
    Task<string> Encrypt(string plaintextPassword);
    Task<string> Decrypt(string cipherText);
}

public class TripleDesEncryptor(IOptions<MqrConfig> config) : ICredentialEncryptor
{
    public Task<string> Encrypt(string plaintextPassword)
    {
        using var des = TripleDES.Create();
        
        des.Key = GetKey(des.KeySize / 8);
        des.IV = new byte[des.BlockSize / 8];
        des.Mode = CipherMode.CBC;
        des.Padding = PaddingMode.PKCS7;

        using var encryptor = des.CreateEncryptor();
        var plainBytes = Encoding.UTF8.GetBytes(plaintextPassword);
        var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
        var base64String = Convert.ToBase64String(cipherBytes);

        return Task.FromResult(base64String);
    }

    public Task<string> Decrypt(string cipherText)
    {
        using var des = TripleDES.Create();

        des.Key = GetKey(des.KeySize / 8);
        des.IV = new byte[des.BlockSize / 8];
        des.Mode = CipherMode.CBC;
        des.Padding = PaddingMode.PKCS7;

        using var decryptor = des.CreateDecryptor();
        var cipherBytes = Convert.FromBase64String(cipherText);
        var plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
        var result = Encoding.UTF8.GetString(plainBytes);

        return Task.FromResult(result);
    }

    private byte[] GetKey(int length)
    {
        var key = config.Value.EncryptionKey;
        
        var keyBytes = Encoding.UTF8.GetBytes(key);
        Array.Resize(ref keyBytes, length);
        
        return keyBytes;
    }
}
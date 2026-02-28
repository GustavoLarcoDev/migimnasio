using System.Security.Cryptography;

namespace Gimnasio.Helpers;

/// <summary>
/// Cifrado AES-256-GCM para proteger contraseñas de certificados .p12.
/// La clave maestra se almacena en appsettings.Production.json (nunca en git).
/// </summary>
public static class CertificadoEncryptionHelper
{
    private const int NonceSize = 12;  // AES-GCM nonce estándar
    private const int TagSize = 16;    // AES-GCM tag de autenticación

    /// <summary>
    /// Cifra un texto plano usando AES-256-GCM.
    /// Retorna Base64 de: nonce(12) + ciphertext(N) + tag(16).
    /// </summary>
    public static string Encrypt(string plainText, string base64Key)
    {
        var key = Convert.FromBase64String(base64Key);
        var plainBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
        var nonce = new byte[NonceSize];
        RandomNumberGenerator.Fill(nonce);

        var cipherText = new byte[plainBytes.Length];
        var tag = new byte[TagSize];

        using var aes = new AesGcm(key, TagSize);
        aes.Encrypt(nonce, plainBytes, cipherText, tag);

        // Formato: nonce + ciphertext + tag
        var result = new byte[NonceSize + cipherText.Length + TagSize];
        Buffer.BlockCopy(nonce, 0, result, 0, NonceSize);
        Buffer.BlockCopy(cipherText, 0, result, NonceSize, cipherText.Length);
        Buffer.BlockCopy(tag, 0, result, NonceSize + cipherText.Length, TagSize);

        return Convert.ToBase64String(result);
    }

    /// <summary>
    /// Descifra un texto cifrado con AES-256-GCM.
    /// Espera Base64 de: nonce(12) + ciphertext(N) + tag(16).
    /// </summary>
    public static string Decrypt(string encryptedBase64, string base64Key)
    {
        var key = Convert.FromBase64String(base64Key);
        var combined = Convert.FromBase64String(encryptedBase64);

        var nonce = new byte[NonceSize];
        var tag = new byte[TagSize];
        var cipherTextLength = combined.Length - NonceSize - TagSize;
        var cipherText = new byte[cipherTextLength];

        Buffer.BlockCopy(combined, 0, nonce, 0, NonceSize);
        Buffer.BlockCopy(combined, NonceSize, cipherText, 0, cipherTextLength);
        Buffer.BlockCopy(combined, NonceSize + cipherTextLength, tag, 0, TagSize);

        var plainBytes = new byte[cipherTextLength];
        using var aes = new AesGcm(key, TagSize);
        aes.Decrypt(nonce, cipherText, tag, plainBytes);

        return System.Text.Encoding.UTF8.GetString(plainBytes);
    }
}

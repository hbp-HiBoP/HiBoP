using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace CRNL.HiBoP.Spikes.P06;

public static class CertificateIdentity
{
    public static X509Certificate2 CreateEphemeral(string? advertisedAddress = null)
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var request = new CertificateRequest(
            "CN=HiBoP P06 Transport Spike",
            key,
            HashAlgorithmName.SHA256);
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, true));
        request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, true));
        request.CertificateExtensions.Add(
            new X509EnhancedKeyUsageExtension(
                new OidCollection { new("1.3.6.1.5.5.7.3.1") },
                true));

        var san = new SubjectAlternativeNameBuilder();
        san.AddDnsName("localhost");
        san.AddIpAddress(IPAddress.Loopback);
        if (!string.IsNullOrWhiteSpace(advertisedAddress))
        {
            if (IPAddress.TryParse(advertisedAddress, out var address))
            {
                san.AddIpAddress(address);
            }
            else
            {
                san.AddDnsName(advertisedAddress);
            }
        }

        request.CertificateExtensions.Add(san.Build());
        var now = DateTimeOffset.UtcNow;
        using var generated = request.CreateSelfSigned(now.AddMinutes(-5), now.AddDays(7));
        var storageFlags = OperatingSystem.IsWindows()
            ? X509KeyStorageFlags.UserKeySet | X509KeyStorageFlags.Exportable
            : X509KeyStorageFlags.EphemeralKeySet | X509KeyStorageFlags.Exportable;
        return X509CertificateLoader.LoadPkcs12(
            generated.Export(X509ContentType.Pfx),
            null,
            storageFlags);
    }

    public static byte[] ComputeSpkiPin(X509Certificate2 certificate)
    {
        using var publicKey = certificate.GetECDsaPublicKey()
            ?? throw new CryptographicException("The P06 certificate does not contain an ECDSA public key.");
        return SHA256.HashData(publicKey.ExportSubjectPublicKeyInfo());
    }

    public static string ComputeShortAuthenticationString(X509Certificate2 certificate) =>
        ComputeShortAuthenticationString(ComputeSpkiPin(certificate));

    public static string ComputeShortAuthenticationString(ReadOnlySpan<byte> pin)
    {
        if (pin.Length != SHA256.HashSizeInBytes)
        {
            throw new ArgumentException("An SPKI SHA-256 pin must contain 32 bytes.", nameof(pin));
        }

        var firstTwentyBits = ((pin[0] << 12) | (pin[1] << 4) | (pin[2] >> 4)) % 1_000_000;
        return firstTwentyBits.ToString("D6", System.Globalization.CultureInfo.InvariantCulture);
    }

    public static bool PinsMatch(ReadOnlySpan<byte> left, ReadOnlySpan<byte> right) =>
        left.Length == right.Length && CryptographicOperations.FixedTimeEquals(left, right);
}

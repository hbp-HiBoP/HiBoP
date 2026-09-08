using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Asn1.Pkcs;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Operators;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.X509;
using X509Certificate = System.Security.Cryptography.X509Certificates.X509Certificate;

namespace HBP.Transfer.Transport
{
    /// <summary>Per-run TLS identity. No key is persisted or exported.</summary>
    public static class TransportIdentity
    {
        public static X509Certificate2 Create()
        {
            var random = new SecureRandom();
            var generator = new RsaKeyPairGenerator();
            generator.Init(new KeyGenerationParameters(random, 2048));
            var keyPair = generator.GenerateKeyPair();
            var certificate = new X509V3CertificateGenerator();
            var name = new X509Name("CN=HiBoP-Transport-Probe");
            certificate.SetSerialNumber(new BigInteger(128, random).Add(BigInteger.One));
            certificate.SetIssuerDN(name);
            certificate.SetSubjectDN(name);
            certificate.SetNotBefore(DateTime.UtcNow.AddMinutes(-1));
            certificate.SetNotAfter(DateTime.UtcNow.AddHours(1));
            certificate.SetPublicKey(keyPair.Public);
            certificate.AddExtension(X509Extensions.BasicConstraints, true, new BasicConstraints(false));
            certificate.AddExtension(X509Extensions.KeyUsage, true, new KeyUsage(KeyUsage.DigitalSignature | KeyUsage.KeyEncipherment));
            certificate.AddExtension(X509Extensions.ExtendedKeyUsage, true, new ExtendedKeyUsage(KeyPurposeID.id_kp_serverAuth));
            var signed = certificate.Generate(new Asn1SignatureFactory("SHA256WITHRSA", keyPair.Private, random));
            // Mono's PKCS#12 importer requires legacy PBE. This container stays in
            // memory only: it does not select or weaken the network TLS cipher.
            var store = new Pkcs12StoreBuilder().SetCertAlgorithm(PkcsObjectIdentifiers.PbeWithShaAnd3KeyTripleDesCbc).SetKeyAlgorithm(PkcsObjectIdentifiers.PbeWithShaAnd3KeyTripleDesCbc).SetUseDerEncoding(true).Build();
            store.SetKeyEntry("identity", new AsymmetricKeyEntry(keyPair.Private), new[] { new X509CertificateEntry(signed) });
            // In-memory PKCS#12 bridges the standard library to Unity's TLS provider.
            // Neither the certificate's private key nor this temporary password is saved.
            byte[] passwordBytes = new byte[32];
            random.NextBytes(passwordBytes);
            string password = Convert.ToBase64String(passwordBytes);
            using var buffer = new MemoryStream();
            store.Save(buffer, password.ToCharArray(), random);
            byte[] pfx = buffer.ToArray();
            try
            {
                return new X509Certificate2(pfx, password, X509KeyStorageFlags.EphemeralKeySet);
            }
            finally
            {
                Array.Clear(pfx, 0, pfx.Length);
            }
        }

        public static X509Certificate2 CreateWithRuntimeApi()
        {
            using var key = RSA.Create(2048);
            var request = new CertificateRequest("CN=HiBoP-Transport-Probe", key, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, true));
            request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, true));
            var usage = new OidCollection { new Oid("1.3.6.1.5.5.7.3.1") };
            request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(usage, true));
            return request.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddHours(1));
        }

        public static byte[] Hash(byte[] bytes)
        {
            using var sha = SHA256.Create();
            return sha.ComputeHash(bytes);
        }

        public static bool Equal(byte[] left, byte[] right)
        {
            if (left == null || right == null || left.Length != right.Length) return false;
            int difference = 0;
            for (int i = 0; i < left.Length; i++) difference |= left[i] ^ right[i];
            return difference == 0;
        }

        public static bool Matches(X509Certificate certificate, byte[] expectedPin)
        {
            return certificate != null && expectedPin != null && expectedPin.Length == 32 && Equal(Hash(certificate.GetRawCertData()), expectedPin);
        }
    }
}

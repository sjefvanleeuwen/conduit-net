using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace ConduitNet.Tools.CertGen
{
    class Program
    {
        static void Main(string[] args)
        {
            string outputDir = args.Length > 0 ? args[0] : "certs";
            string password = args.Length > 1 ? args[1] : "conduit";

            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            Console.WriteLine($"Generating certificates in '{outputDir}'...");

            // 1. Create CA
            using var rsaCa = RSA.Create(4096);
            var caReq = new CertificateRequest("CN=ConduitCA", rsaCa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            
            caReq.CertificateExtensions.Add(new X509BasicConstraintsExtension(true, false, 0, true));
            caReq.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.KeyCertSign | X509KeyUsageFlags.CrlSign, true));

            using var caCert = caReq.CreateSelfSigned(DateTimeOffset.Now, DateTimeOffset.Now.AddYears(5));
            
            // Export CA (Public Key)
            File.WriteAllBytes(Path.Combine(outputDir, "ca.crt"), caCert.Export(X509ContentType.Cert));
            Console.WriteLine("Created ca.crt");

            // 2. Create Node Certificate
            using var rsaNode = RSA.Create(2048);
            var nodeReq = new CertificateRequest("CN=ConduitNode", rsaNode, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            
            nodeReq.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, false));
            nodeReq.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment, false));
            nodeReq.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(new OidCollection { new Oid("1.3.6.1.5.5.7.3.1"), new Oid("1.3.6.1.5.5.7.3.2") }, false)); // Server & Client Auth

            // Add SAN for localhost
            var sanBuilder = new SubjectAlternativeNameBuilder();
            sanBuilder.AddDnsName("localhost");
            sanBuilder.AddIpAddress(System.Net.IPAddress.Loopback);
            sanBuilder.AddIpAddress(System.Net.IPAddress.IPv6Loopback);
            nodeReq.CertificateExtensions.Add(sanBuilder.Build());

            // Sign with CA
            // Note: We need to copy the CA cert to a new object with the private key to sign
            // But here we have the key in rsaCa.
            // CreateSelfSigned is easy, but signing with another key requires more work in .NET Core.
            // Actually, we can use Create() on the request with the issuer cert and key.
            
            // We need to construct the serial number
            byte[] serialNumber = new byte[8];
            RandomNumberGenerator.Fill(serialNumber);

            using var nodeCert = nodeReq.Create(caCert, DateTimeOffset.Now, DateTimeOffset.Now.AddYears(1), serialNumber);
            
            // We need to pair the public cert with the private key
            using var nodeCertWithKey = nodeCert.CopyWithPrivateKey(rsaNode);

            // Export PFX
            File.WriteAllBytes(Path.Combine(outputDir, "node.pfx"), nodeCertWithKey.Export(X509ContentType.Pfx, password));
            Console.WriteLine($"Created node.pfx (Password: {password})");
            
            // Export Client PFX (same as node for now, but could be different)
            File.WriteAllBytes(Path.Combine(outputDir, "client.pfx"), nodeCertWithKey.Export(X509ContentType.Pfx, password));
            Console.WriteLine($"Created client.pfx (Password: {password})");
        }
    }
}

using System.Net.Security;
using System.Net.WebSockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace ConduitNet.Tools.MtlsTester;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Conduit mTLS Tester");
        
        var certPath = FindCertPath();
        var clientCertPath = Path.Combine(certPath, "client.pfx");
        var caCertPath = Path.Combine(certPath, "ca.crt");

        if (!File.Exists(clientCertPath) || !File.Exists(caCertPath))
        {
            Console.WriteLine($"Error: Certificates not found in {certPath}");
            return;
        }

        Console.WriteLine($"Loading Client Cert: {clientCertPath}");
        Console.WriteLine($"Loading CA Cert: {caCertPath}");

        // Load Certs
        // Note: In .NET 9+ we should use X509CertificateLoader, but for now X509Certificate2 constructor is fine (with warning)
        var clientCert = new X509Certificate2(clientCertPath, "conduit");
        var caCert = new X509Certificate2(caCertPath);

        var url = "wss://localhost:5000/conduit";
        Console.WriteLine($"Connecting to {url}...");

        using var ws = new ClientWebSocket();
        ws.Options.ClientCertificates.Add(clientCert);
        
        // Custom validation to trust our CA
        ws.Options.RemoteCertificateValidationCallback = (sender, cert, chain, sslPolicyErrors) =>
        {
            if (sslPolicyErrors == SslPolicyErrors.None) return true;

            // If the only error is RemoteCertificateChainErrors, check if it's signed by our CA
            if (sslPolicyErrors == SslPolicyErrors.RemoteCertificateChainErrors)
            {
                // In a real scenario, we would build the chain with our CA.
                // For this simple test, we just check if the issuer matches our CA's subject.
                // This is NOT secure for production but proves the cert is being presented.
                // A better check is to verify the chain.
                
                // Simple check:
                if (cert?.Issuer == caCert.Subject)
                {
                    Console.WriteLine("Server certificate issuer matches our CA.");
                    return true;
                }
            }

            Console.WriteLine($"SSL Error: {sslPolicyErrors}");
            return false;
        };

        try
        {
            await ws.ConnectAsync(new Uri(url), CancellationToken.None);
            Console.WriteLine("Successfully connected via mTLS!");
            
            // Send a ping or something?
            // For now, just close.
            await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Test Complete", CancellationToken.None);
            Console.WriteLine("Connection closed gracefully.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Connection failed: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"Inner: {ex.InnerException.Message}");
            }
        }
    }

    static string FindCertPath()
    {
        var current = Directory.GetCurrentDirectory();
        for (int i = 0; i < 5; i++)
        {
            var path = Path.Combine(current, "certs");
            if (Directory.Exists(path)) return path;
            var parent = Directory.GetParent(current);
            if (parent == null) break;
            current = parent.FullName;
        }
        return "certs";
    }
}

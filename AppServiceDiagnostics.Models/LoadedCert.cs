using System.Security.Cryptography.X509Certificates;

namespace AppServiceDiagnostics.Models
{
    public class LoadedCert
    {
        public string? Thumbprint { get; set; }
        public bool HasPrivateKey { get; set; }
        public string? SerialNumber { get; set; }
        public string? IssuerName { get; set; }
        public string? SubjectName { get; set; }
    }
}

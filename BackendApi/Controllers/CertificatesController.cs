using AppServiceDiagnostics.Models;
using Azure;
using Azure.Security.KeyVault.Certificates;
using Azure.Security.KeyVault.Secrets;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography.X509Certificates;

namespace BackendApi.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class CertificatesController : ControllerBase
    {
        private CertificateClient _certificateClient;
        private SecretClient _secretClient;

        public CertificatesController(CertificateClient certificateClient, SecretClient secretClient)
        {
            _certificateClient = certificateClient;
            _secretClient = secretClient;
        }

        [HttpGet]
        public async Task<IActionResult> Get() 
        {
            var certificates = new List<string>();
            await foreach (var certProp in _certificateClient.GetPropertiesOfCertificatesAsync())
            {
                certificates.Add(certProp.Name);
            }

            return Ok(certificates);
        }


        [HttpGet("{certName}")]
        public async Task<IActionResult> GetCertificate(string certName)
        {

            KeyVaultCertificateWithPolicy certificateWithPolicy = await _certificateClient.GetCertificateAsync(certName);
            X509Certificate2 x509Certificate = await _certificateClient.DownloadCertificateAsync(certificateWithPolicy.Name);         
            var loadedCert = new LoadedCert()
            {
                Thumbprint = x509Certificate.Thumbprint,
                SubjectName =x509Certificate.SubjectName.Name,
                IssuerName = x509Certificate.IssuerName.Name,
                SerialNumber = x509Certificate.SerialNumber,
                HasPrivateKey = x509Certificate.HasPrivateKey
            };
            return Ok(loadedCert);
        }
    }
}

//6:46 PM 9/14/2018 Rajib
namespace Sow.Framework.Security.LetsEncrypt {
    using System.Security.Cryptography.X509Certificates;
    public class Certificate : ICertificate {
        public string cert_dir { get; set; }
        public string cert_path { get; set; }
        public X509Certificate2 Cert { get; set; }
        public bool isExpired { get; set; }
        public bool status { get; set; }
        public string errorDescription { get; set; }
    }
}

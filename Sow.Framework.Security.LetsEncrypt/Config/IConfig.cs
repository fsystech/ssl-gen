
namespace Sow.Framework.Security.LetsEncrypt {
    using System.Collections.Generic;
    using Certes;

    public class AppSettings {
        public WebServerEnum webServerEnum { get; set; }
        public string CopyTo { get; set; }
        public string[] ExportType { get; set; }
        public string WebServerName { get; set; }
        public string[] AppPool { get; set; }
        public string[] Site { get; set; }
    }
    public interface IDomain {
        string CF_DNS_ZONE { get; set; }
        string CertEmail { get; set; }
        string ZoneName { get; set; }
        string DomainName { get; set; }
        bool IsWildcard { get; set; }
        bool IsSelfHost { get; set; }
        bool StoreCertificate { get; set; }
        CsrInfo Csr { get; set; }
        AppSettings appSettings { get; set; }
    }
    public class Domain : IDomain {
        public string CF_DNS_ZONE { get; set; }
        public string CertEmail { get; set; }
        public bool StoreCertificate { get; set; }
        public string ZoneName { get; set; }
        public string DomainName { get; set; }
        public bool IsWildcard { get; set; }
        public bool IsSelfHost { get; set; }
        public CsrInfo Csr { get; set; }
        public AppSettings appSettings { get; set; }
    };
    public interface IConfig {
        string CF_AUTH_KEY { get; set; }
        string CF_AUTH_EMAIL { get; set; }
        string CertEmail { get; set; }
        string Dir { get; set; }
        List<Domain> Domain { get; set; }
    }
    public class Config : IConfig {
        public string CF_AUTH_KEY { get; set; }
        public string CF_AUTH_EMAIL { get; set; }
        public string CertEmail { get; set; }
        public string Dir { get; set; }
        public List<Domain> Domain { get; set; }
    }
    public interface IWinConfig {
        string WinUser { get; set; }
        string WinPassword { get; set; }
    }
    public class WinConfig: IWinConfig {
        public string WinUser { get; set; }
        public string WinPassword { get; set; }
    }
    public interface IGConfig {
        string CF_API { get; set; }
        string CF_URI { get; set; }
        WinConfig winConfig { get; set; }
        List<Config> config { get; set; }
        object SmtpSettings { get; set; }
    }
    public class GConfig: IGConfig {
        public string CF_API { get; set; }
        public string CF_URI { get; set; }
        public WinConfig winConfig { get; set; }
        public List<Config> config { get; set; }
        public object SmtpSettings { get; set; }
    }
}

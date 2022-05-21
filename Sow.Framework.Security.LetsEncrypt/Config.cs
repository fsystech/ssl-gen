/**
* Copyright (c) 2018, Sow ( https://safeonline.world, https://www.facebook.com/safeonlineworld). (https://github.com/RKTUXYN) All rights reserved.
* Copyrights licensed under the New BSD License.
* See the accompanying LICENSE file for terms.
*/
// 7:50 PM 9/15/2018
// Rajib Chy
using Certes;
using System.Collections.Generic;
namespace Sow.Framework.Security.LetsEncrypt;
public class WebAppSettings {
    public WebServerEnum WebServer { get; set; }
    public string CopyTo { get; set; }
    public string[] ExportType { get; set; }
    public string WebServerName { get; set; }
    public string[] AppPool { get; set; }
    public string[] Site { get; set; }
}
public interface IDomainConfig {
    string CloudflareDNSZone { get; set; }
    string ZoneName { get; set; }
    string DomainName { get; set; }
    bool IsWildcard { get; set; }
    bool IsSelfHost { get; set; }
    bool StoreCertificate { get; set; }
    CsrInfo Csr { get; set; }
    WebAppSettings AppSettings { get; set; }
}
public class DomainConfig : IDomainConfig {
    public string CloudflareDNSZone { get; set; }
    public bool StoreCertificate { get; set; }
    public string ZoneName { get; set; }
    public string DomainName { get; set; }
    public bool IsWildcard { get; set; }
    public bool IsSelfHost { get; set; }
    public CsrInfo Csr { get; set; }
    public WebAppSettings AppSettings { get; set; }
};
public interface IAppConfig {
    string CloudflareAuthKey { get; set; }
    string CloudflareAuthEmail { get; set; }
    string CertEmail { get; set; }
    string Dir { get; set; }
    List<DomainConfig> Domain { get; set; }
}
public class AppConfig : IAppConfig {
    public string CloudflareAuthKey { get; set; }
    public string CloudflareAuthEmail { get; set; }
    public string CertEmail { get; set; }
    public string Dir { get; set; }
    public List<DomainConfig> Domain { get; set; }
}
public interface IWinConfig {
    string WinUser { get; set; }
    string WinPassword { get; set; }
}
public interface IGAppConfig {
    string ConfigKey { get; set; }
    string CloudflareAPI { get; set; }
    string CloudflareUrl { get; set; }
    List<AppConfig> Config { get; set; }
    string CertPassword { get; set; }
    object SmtpSettings { get; set; }
}
public class GAppConfig : IGAppConfig {
    [Newtonsoft.Json.JsonIgnore]
    public string ConfigKey { get; set; }
    public string CloudflareAPI { get; set; }
    public string CloudflareUrl { get; set; }
    public List<AppConfig> Config { get; set; }
    public object SmtpSettings { get; set; }
    public string CertPassword { get; set; }
}
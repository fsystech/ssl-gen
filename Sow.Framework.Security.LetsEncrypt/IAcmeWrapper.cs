/**
* Copyright (c) 2018, Sow ( https://safeonline.world, https://www.facebook.com/safeonlineworld). (https://github.com/RKTUXYN) All rights reserved.
* Copyrights licensed under the New BSD License.
* See the accompanying LICENSE file for terms.
*/
// 7:50 PM 9/15/2018
// Rajib Chy
using System;
using System.Threading.Tasks;
namespace Sow.Framework.Security.LetsEncrypt;
public enum CertEnum {
    PFX = 1,
    PRIVATE = 2,
    FULL_CHAIN = 3,
    DER = 4
}
public enum WebServerEnum {
    NONE = 0,
    NGINX = 1,
    IIS = 2
}
public interface IAcmeWrapper : IDisposable {
    int MAX_TRY { get; }
    string DomainDir { get; }
    string CertDir { get; }
    IGAppConfig AppConfig { get; }
    IDomainConfig DomainInfo { get; }
    bool IsDisposed { get; }
    void CopyTo( );
    Task<bool> RemoveDnsTextRecord( );
    WebServerEnum ServerType { get; }
    string GetServerName( WebServerEnum webServerEnum = WebServerEnum.NONE );
    string CreateCertAbsPath( string zoneName, CertEnum certTyp, string certDir = null );
    (bool, string) IsValidConfig( bool logging = false );
    Task<IOrderResult> CreateOrRenewCert( int rec = 0, bool forceRenew = false );
    Task<ICertificate> CreateCertificate( );
    ICertificate GetCertificate( );
    bool ExistsCertificate( CertEnum certEnum = CertEnum.PFX );
}
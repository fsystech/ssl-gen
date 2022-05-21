namespace Sow.Framework.Security.LetsEncrypt {
    using System;
    using System.Threading;
    using System.Threading.Tasks;
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
    public interface IAcmeWrapper: IDisposable {
        int MAX_TRY { get; set; }
        string domainDir { get; set; }
        string certDir { get; set; }
        IGConfig gConfig { get; set; }
        IDomain domain { get; set; }
        void CopyTo( );
        Task<bool> RemoveDnsTextRecord( );
        WebServerEnum GetServerType( );
        string GetServerName( WebServerEnum webServerEnum = WebServerEnum.NONE );
        string CreateCertAbsPath( string zoneName, CertEnum certTyp, string certDir = null );
        (bool, string) IsValidConfig( bool logging = false );
        Task<IOrderResult> CreateOrRenewCert( int rec = 0, bool forceRenew = false );
        Task<ICertificate> CreateCertificate();
        ICertificate GetCertificate();
        bool ExistsCertificate( CertEnum certEnum = CertEnum.PFX );
        void ReInit( string web );
        void Init( string web, IConfig config = null, ILogger logger = null, int mxtry = 10, CancellationToken ct = default( CancellationToken ) );
    }
}

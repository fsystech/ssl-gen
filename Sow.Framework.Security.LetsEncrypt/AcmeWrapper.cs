/**
* Copyright (c) 2018, Sow ( https://safeonline.world, https://www.facebook.com/safeonlineworld). (https://github.com/RKTUXYN) All rights reserved.
* Copyrights licensed under the New BSD License.
* See the accompanying LICENSE file for terms.
*/
// 7:50 PM 9/15/2018
// Rajib Chy
using System;
using Certes;
using Certes.Acme;
using Certes.Pkcs;
using System.Linq;
using Newtonsoft.Json;
using System.Threading;
using Sow.Framework.Files;
using Certes.Acme.Resource;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Sow.Framework.Security.CloudflareWrapper;
using System.Security.Cryptography.X509Certificates;
//https://github.com/fszlin/certes
namespace Sow.Framework.Security.LetsEncrypt;
public class AcmeWrapper : IAcmeWrapper {
    private ICFDNS _cfDns;
    private long _isDisposed = 0;
    public bool IsDisposed => Interlocked.Read( ref _isDisposed ) != 0;
    private IAcmeCtx _contex { get; set; }
    private IAppConfig _config { get; set; }
    private IOrderContext _orderContext { get; set; }
    private readonly CancellationToken _token;
    private readonly ILogger _logger;
    private readonly IDomainConfig _domain;
    public IDomainConfig DomainInfo => _domain;
    public int MAX_TRY { get; set; }
    private string _domainDir { get; set; }
    public string DomainDir => _domainDir;
    private string _orderAbsolute { get; set; }
    private string _certDir { get; set; }
    public string CertDir => _certDir;
    private readonly string _acmeApiServer;
    public IGAppConfig AppConfig => Util.GConfig;
    public static string DefaultAcmeApiServer => "LetsEncryptV2";
    public WebServerEnum ServerType => _domain.AppSettings.WebServer;
    public AcmeWrapper( 
        IAppConfig config, string web, ILogger logger,
        CancellationToken token, string acmeApiServer = "LetsEncryptV2", int mxtry = 10 
     ) {
        _acmeApiServer = acmeApiServer;
        _config = config; _token = token; _logger = logger;
        _config.Dir = Util.RegisterNewDirectory( _config.CertEmail );
        _logger.Write( "Working for {0}", _config.CertEmail );
        _logger.FlushMemory( );
        if ( _domain != null ) {
            if ( _domain.ZoneName == web ) return;
        }
        _domain = _config.Domain.Find( a => a.DomainName == web );
        if ( _domain == null ) {
            string msg = string.Format( "No data found for domain {0} against Email {1}!!!", web, _config.CertEmail );
            _logger.Write( msg ); _logger.FlushMemory( );
            throw new Exception( msg );
        }
        _logger.Write( "Working for {0}", web );
        _logger.FlushMemory( );
        _domainDir = Util.RegisterNewDirectory( _domain.ZoneName, _config.Dir );
        Util.RegisterNewDirectory( "order", _domainDir );
        _orderAbsolute = string.Format( @"{0}/order/new_order.bin", _domainDir );
        _certDir = string.Format( @"{0}/cert/", _domainDir );
        FileWorker.CreateDirectory( _certDir );
        _cfDns = new CFDNS( new CFConfig {
            CF_API = Util.GConfig.CloudflareAPI,
            CF_URI = Util.GConfig.CloudflareUrl,
            CF_AUTH_KEY = config.CloudflareAuthKey,
            CF_AUTH_EMAIL = config.CloudflareAuthEmail ?? config.CertEmail,
            CF_DNS_ZONE = _domain.CloudflareDNSZone
        }, _logger );
        MAX_TRY = mxtry;
    }
    public (bool, string) IsValidConfig( bool logging = false ) {
        var (status, resp) = (true, "Success");
        if ( _domain == null ) {
            (status, resp) = (false, "No Domain Config Found!!!");
            goto RETURN;
        }
        if ( string.IsNullOrEmpty( _domain.DomainName ) ) {
            (status, resp) = (false, "Invalid Config Defined. Domain Name Required!!!");
            goto RETURN;
        }
        if ( string.IsNullOrEmpty( _domain.ZoneName ) ) {
            (status, resp) = (false, string.Format( "Invalid Config Defined. Zone Name Required for domain {0}!!!", _domain.DomainName ));
            goto RETURN;
        }
        if ( string.IsNullOrEmpty( _domain.CloudflareDNSZone ) ) {
            (status, resp) = (false, string.Format( "Cloudflare Dns Zone required for {0}!!!", _domain.DomainName ));
            goto RETURN;
        }
        if ( _domain.Csr == null ) {
            (status, resp) = (false, string.Format( "CSR required for {0}!!!", _domain.DomainName ));
            goto RETURN;
        }
        if ( _domain.IsSelfHost && _domain.StoreCertificate ) {
            if ( _domain.AppSettings == null ) {
                (status, resp) = (false, string.Format( "IIS Settings required for {0}!!!", _domain.DomainName ));
                goto RETURN;
            }
            if ( _domain.AppSettings.Site == null ) {
                (status, resp) = (false, string.Format( "Site(s) required in IIS Settings for {0}!!!", _domain.DomainName ));
                goto RETURN;
            }
        }
        RETURN:
        if ( !status && logging ) {
            _logger.Write( resp );
        }
        return (status, resp);
    }
    private Uri GetAcmeServer( ) {
        return _acmeApiServer switch {
            "LetsEncryptV2" => WellKnownServers.LetsEncryptV2,
            "LetsEncryptStagingV2" => WellKnownServers.LetsEncryptStagingV2,
            _ => throw new Exception( "Invalid Acme Api Server!!" ),
        };
    }
    private async Task PrepareContext( ) {
        if ( _contex != null ) return;
        try {
            _contex = new AcmeCtx( );
            string pemKey = FileWorker.Read( $@"{_config.Dir}/{_acmeApiServer}_account.pem" );
            if ( string.IsNullOrEmpty( pemKey ) ) {
                _logger.Write( $"Creating account for {_config.CertEmail}" );
                _contex.Ctx = new AcmeContext( GetAcmeServer( ) );
                _contex.ACtx = await _contex.Ctx.NewAccount( email: _config.CertEmail, termsOfServiceAgreed: true );
                pemKey = _contex.Ctx.AccountKey.ToPem( );
                FileWorker.WriteFile( pemKey, $@"{_config.Dir}/{_acmeApiServer}_account.pem" );
                _logger.Write( $"New registration created successfully for :: {_config.CertEmail}" );
                return;
            }
            _logger.Write( $"Using old PEM for account {_config.CertEmail}" );
            IKey accountKey = KeyFactory.FromPem( pemKey );
            _contex.Ctx = new AcmeContext( GetAcmeServer( ), accountKey );
            _contex.ACtx = await _contex.Ctx.Account( );
            _logger.Write( $"Re-authenticated :: {_config.CertEmail}" );
        } catch ( Exception ex ) {
            _logger.Write( ex );
            throw new Exception( "We are unable to create AcmeContext" );
        }
        return;
    }
    public static IAppConfig GetConfig( string email, string configKey ) {
        if ( Util.GConfig == null ) {
            Util.Load( $@"{App.Dir}/config/{configKey}.config.json", configKey );
        }
        return Util.GConfig.Config.Find( a => a.CertEmail == email );
    }
    private void SaveNewOrder( ) {
        _logger.Write( $"Order location {_orderContext.Location}" );
        FileWorker.WriteFile( JsonConvert.SerializeObject( _orderContext.Location ), _orderAbsolute );
    }
    public bool LoadOrder( ) {
        string fstar = FileWorker.Read( _orderAbsolute );
        if ( string.IsNullOrEmpty( fstar ) ) return false;
        Uri loc = JsonConvert.DeserializeObject<Uri>( fstar );
        if ( loc == null ) return false;
        _orderContext = _contex.Ctx.Order( loc );
        return true;
    }
    async Task<IChalageStatus> ValidateChalage( IChallengeContext challengeCtx ) {
        // Now let's ping the ACME service to validate the challenge token
        try {
            // We sleep 5 seconds between each request, to leave time to ACME service to refresh
            if ( WaitHandler.Wait( 30000, _token ) ) return new ChalageStatus { status = false, errorDescription = "Task cancelled" };
            Challenge challenge = await challengeCtx.Validate( );
            if ( challenge.Status == ChallengeStatus.Invalid ) {
                _logger.Write( "Error occured while validating acme challenge to {0} :: error==>{1}", _domain.ZoneName, challenge.Error.Detail );
                return new ChalageStatus {
                    status = false,
                    errorDescription = challenge.Error.Detail
                };
            }
            // We need to loop, because ACME service might need some time to validate the challenge token
            int retry = 0;
            while ( ( ( challenge.Status == ChallengeStatus.Pending )
                || ( challenge.Status == ChallengeStatus.Processing ) ) && ( retry < 30 ) ) {
                // We sleep 5 seconds between each request, to leave time to ACME service to refresh
                if ( WaitHandler.Wait( 15000, _token ) ) return new ChalageStatus { status = false, errorDescription = "Task cancelled" };
                // We refresh the challenge object from ACME service
                challenge = await challengeCtx.Resource( );
                retry++;
            }
            if ( challenge.Status == ChallengeStatus.Invalid ) {
                _logger.Write( "Error occured while validating acme challenge to {0} :: error==>{1}", _domain.ZoneName, challenge.Error.Detail );
                return new ChalageStatus {
                    status = false,
                    errorDescription = challenge.Error.Detail
                };
            }
            return new ChalageStatus {
                status = true
            };
        } catch ( Exception e ) {
            _logger.Write( "Error occured while validating acme challenge to {0} :: error==>{1}", _domain.ZoneName, e.Message );
            _logger.Write( e );
            return new ChalageStatus {
                status = false,
                errorDescription = e.Message
            };
        }
    }
    public string GetServerName( WebServerEnum webServerEnum = WebServerEnum.NONE ) {
        if ( webServerEnum == WebServerEnum.NONE ) {
            return GetServerName( ServerType );
        }
        if ( webServerEnum == WebServerEnum.IIS ) return "IIS";
        if ( webServerEnum == WebServerEnum.NGINX ) return "NGINX";
        throw new Exception( "Invalid Cert Enum..." );
    }
    public string CreateCertAbsPath(
        string zoneName, CertEnum certTyp, string certDir = null
    ) {
        if ( certTyp == CertEnum.PFX )
            return string.Format( @"{0}{1}.pfx", certDir ?? _certDir, zoneName );
        if ( certTyp == CertEnum.PRIVATE )
            return string.Format( @"{0}privkey_{1}.pem", certDir ?? _certDir, zoneName );
        if ( certTyp == CertEnum.FULL_CHAIN )
            return string.Format( @"{0}fullchain_{1}.pem", certDir ?? _certDir, zoneName );
        if ( certTyp == CertEnum.DER )
            return string.Format( @"{0}{1}.der", certDir ?? _certDir, zoneName );
        throw new Exception( "Invalid Cert Enum..." );
    }
    private static string CertEnumToString( CertEnum certTyp ) {
        if ( certTyp == CertEnum.PFX ) return "PFX";
        if ( certTyp == CertEnum.PRIVATE ) return "PRIVATE";
        if ( certTyp == CertEnum.FULL_CHAIN ) return "FULL_CHAIN";
        if ( certTyp == CertEnum.DER ) return "DER";
        throw new Exception( "Invalid Cert Enum..." );
    }
    private void CopyTo( CertEnum certEnum, string toDir ) {
        string fromPath = CreateCertAbsPath( _domain.ZoneName, certEnum );
        string toPath = CreateCertAbsPath( _domain.ZoneName, certEnum, toDir );
        try {
            if ( System.IO.File.Exists( fromPath ) ) {
                System.IO.File.Copy( fromPath, toPath, true );
            }
        } catch ( Exception e ) {
            _logger.Write( $"Unable to copy {CertEnumToString( certEnum )} form {fromPath} to {toPath}" );
            _logger.Write( e );
        }
    }
    public void CopyTo( ) {
        if ( string.IsNullOrEmpty( _domain.AppSettings.CopyTo ) ) return;
        string absPath = System.IO.Path.Combine( _domain.AppSettings.CopyTo, _domain.ZoneName ) + "\\";
        _logger.Write( $"Copy cert to {absPath}" );
        FileWorker.CreateDirectory( absPath );
        CopyTo( CertEnum.PFX, absPath );
        CopyTo( CertEnum.PRIVATE, absPath );
        CopyTo( CertEnum.FULL_CHAIN, absPath );
        CopyTo( CertEnum.DER, absPath );
    }
    public async Task<ICertificate> CreateCertificate( ) {
        try {
            _logger.Write( "Generating certificate for {0}", _domain.ZoneName );
            IKey privateKey = KeyFactory.NewKey( KeyAlgorithm.ES256 );
            _domain.Csr.CommonName = _domain.DomainName;
            CertificateChain cert = await _orderContext.Generate( _domain.Csr, privateKey );
            PfxBuilder pfxBuilder = cert.ToPfx( privateKey );
            byte[] pfx = pfxBuilder.Build( _domain.ZoneName, AppConfig.CertPassword );
            foreach ( string type in _domain.AppSettings.ExportType ) {
                switch ( type ) {
                    case "PFX":
                        FileWorker.WriteFile( pfx, CreateCertAbsPath( _domain.ZoneName, CertEnum.PFX ) );
                        break;
                    case "PRIVATE":
                        FileWorker.WriteFile( privateKey.ToPem( ), CreateCertAbsPath( _domain.ZoneName, CertEnum.PRIVATE ) );
                        break;
                    case "DER":
                        FileWorker.WriteFile( cert.Certificate.ToDer( ), CreateCertAbsPath( _domain.ZoneName, CertEnum.DER ) );
                        break;
                    case "FULL_CHAIN":
                        FileWorker.WriteFile( cert.ToPem( certKey: privateKey ), CreateCertAbsPath( _domain.ZoneName, CertEnum.FULL_CHAIN ) );
                        break;
                }
            }
            return new Certificate {
                status = true,
                cert_dir = _certDir,
                Cert = GetX509Certificate2( pfx, AppConfig.CertPassword ),
                isExpired = false
            };
        } catch ( Exception e ) {
            _logger.Write( "Error occured while generating certificate for {0} :: error==>{1}", _domain.ZoneName, e.Message );
            _logger.Write( e );
            return new Certificate {
                status = false,
                errorDescription = e.Message
            };
        }
    }
    private static string GetJPropertyValue( JProperty jProperty ) {
        if ( jProperty != null ) {
            return jProperty.Value.ToString( );
        }
        return null;
    }
    private static X509Certificate2 GetX509Certificate2( byte[] pfx, string password ) {
        X509Certificate2 cert = new X509Certificate2( pfx, password, X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet );
        return cert;
    }
    public bool ExistsCertificate( CertEnum certEnum = CertEnum.PFX ) {
        return FileWorker.ExistsFile( CreateCertAbsPath( _domain.ZoneName, certEnum ) );
    }
    public ICertificate GetCertificate( ) {
        try {
            string certPath = string.Format( "{0}{1}.pfx", _certDir, _domain.ZoneName );
            byte[] pfx = FileWorker.ReadAllByte( certPath );
            if ( pfx == null ) {
                return new Certificate {
                    status = false,
                    errorDescription = string.Format( "No existing Certificate for {0}", _domain.ZoneName )
                };
            }
            X509Certificate2 cert = GetX509Certificate2( pfx, AppConfig.CertPassword );
            int result = DateTime.Compare( cert.NotAfter, DateTime.Now );
            return new Certificate {
                status = true,
                Cert = cert,
                isExpired = result < 0,
                cert_dir = _certDir,
                cert_path = certPath
            };
        } catch ( Exception e ) {
            _logger.Write( $"Error occured while read certificate for {_domain.ZoneName}" );
            _logger.Write( e );
            return new Certificate {
                status = false,
                errorDescription = e.Message
            };
        }
    }
    public async Task<IOrderResult> CreateOrRenewCert( int rec = 0, bool forceRenew = false ) {
        try {
            var (status, result) = IsValidConfig( );
            if ( !status ) {
                _logger.Write( result );
                return new OrderResult {
                    success = false,
                    taskType = TaskType.NOTHING,
                    errorDescription = result
                };
            }
            _logger.Write( "Create or Renew Certificate for {0}", _domain.ZoneName );
            ICertificate oldCertificate = GetCertificate( );
            if ( oldCertificate.status ) {
                if ( !oldCertificate.isExpired && forceRenew == false ) {
                    _logger.Write( "Certificate not expired for {0}", _domain.ZoneName );
                    return new OrderResult {
                        success = true,
                        taskType = TaskType.INSTALL_CERT,
                        oldCertificate = oldCertificate.Cert
                    };
                }
            }
            await this.PrepareContext( );
            if ( forceRenew == false && oldCertificate.status && !oldCertificate.isExpired && LoadOrder( ) ) {
                return new OrderResult {
                    success = true,
                    taskType = TaskType.INSTALL_CERT,
                    oldCertificate = oldCertificate.Cert
                };
            }
            IOrderResult orderResult = new OrderResult { success = true };
            if ( oldCertificate.status ) {
                if ( oldCertificate.isExpired == false ) {
                    _logger.Write( "Revoke Certificate for {0} & expired on {1} and serial {2}", _domain.ZoneName, oldCertificate.Cert.NotAfter.ToShortDateString( ), oldCertificate.Cert.SerialNumber );
                    try {
                        await _contex.Ctx.RevokeCertificate( oldCertificate.Cert.RawData, RevocationReason.Unspecified );
                    } catch ( Exception r ) {
                        FileWorker.DeleteFile( oldCertificate.cert_path );
                        _logger.Write( "Error occured while Revoke Certificate for {0}; Error=>{1}", _domain.ZoneName, r.Message );
                        _logger.Write( r );
                        _logger.Write( "Whatever we are trying to create new certificate." );
                    }
                } else {
                    _logger.Write( "This old Certificate expired on {0} and serial {1}", oldCertificate.Cert.NotAfter.ToShortDateString( ), oldCertificate.Cert.SerialNumber );
                }
            }
            if ( _domain.IsWildcard ) {
                _orderContext = await _contex.Ctx.NewOrder( new List<string> { _domain.ZoneName, _domain.DomainName } );
            } else {
                _orderContext = await _contex.Ctx.NewOrder( new List<string> { _domain.DomainName } );
            }
            SaveNewOrder( );
            List<IChallengeCtx> challengeCtxs = new List<IChallengeCtx>( );
            List<DnsTxtStore> dnsTxt = this.GetDnsTXT( );
            if ( _domain.IsWildcard ) {
                if ( dnsTxt == null ) dnsTxt = new List<DnsTxtStore>( );
                List<DnsTxtStore> writeDnsTxt = new List<DnsTxtStore>( );
                _logger.Write( "Defined acme challenge type DNS for Wildcard(*) {0}", _domain.ZoneName );
                _logger.Write( "Get Authorization Context for {0}", _domain.ZoneName );
                IEnumerable<IAuthorizationContext> authCtx = await _orderContext.Authorizations( );
                _logger.Write( "Authorization Context found for {0}", _domain.ZoneName );
                foreach ( IAuthorizationContext authz in authCtx ) {
                    // var hctx = await authz.Http( );
                    IChallengeContext challengeCtx = await authz.Dns( );
                    string txt = _contex.Ctx.AccountKey.DnsTxt( challengeCtx.Token );
                    DnsTxtStore dnsTxtStore = dnsTxt.FirstOrDefault( a => a.content == txt );
                    if ( dnsTxtStore != null ) {
                        challengeCtxs.Add( new ChallengeCtx {
                            ctx = challengeCtx, txtName = dnsTxtStore.name
                        } );
                        dnsTxt.Remove( dnsTxtStore );
                        continue;
                    }
                    ICFAPIResponse cFAPIResponse = await _cfDns.AddRecord( new QueryConfig {
                        DOMAIN_NAME = _domain.ZoneName,
                        RECORD_TYPE = CFRecordType.TXT,
                        RECORD_NAME = "_acme-challenge",
                        RECORD_CONTENT = txt,
                        NAME = string.Concat( "_acme-challenge.", _domain.ZoneName )
                    } );
                    if ( !cFAPIResponse.success ) {
                        orderResult.success = false;
                        orderResult.errorDescription = JsonConvert.SerializeObject( cFAPIResponse.messages, _cfDns.JsonConfig );
                        _logger.Write( $"We are unable to add text record at {_domain.ZoneName}", LogLevel.ERROR );
                        _logger.Write( $"Cloudflare message {cFAPIResponse.messages ?? JsonConvert.SerializeObject( cFAPIResponse.errors, _cfDns.JsonConfig )}", LogLevel.WARNING );
                        break;
                    }
                    IChallengeCtx cCtx = new ChallengeCtx {
                        ctx = challengeCtx
                    };
                    dnsTxtStore = new DnsTxtStore {
                        content = txt
                    };
                    string txtName = string.Empty;
                    if ( cFAPIResponse.result is JObject ) {
                        JObject jObject = ( JObject )cFAPIResponse.result;
                        dnsTxtStore.id = GetJPropertyValue( jObject.Property( "id" ) );
                        txtName = GetJPropertyValue( jObject.Property( "name" ) );
                    } else {
                        txtName = string.Concat( "_acme-challenge.", _domain.ZoneName );
                    }
                    dnsTxtStore.name = txtName;
                    writeDnsTxt.Add( dnsTxtStore );
                    cCtx.txtName = txtName;
                    challengeCtxs.Add( cCtx );
                }
                if ( orderResult.success == false ) {
                    return orderResult;
                };
                if ( writeDnsTxt.Count > 0 ) {
                    this.WriteDnsTXT( writeDnsTxt ); writeDnsTxt.Clear( );
                }

            } else {
                throw new NotImplementedException( "Not Implemented!!!" );
            }
            foreach ( IChallengeCtx cCtx in challengeCtxs ) {
                if ( WaitHandler.Wait( 10000, _token ) ) {
                    orderResult.success = false;
                    orderResult.errorDescription = "Task cancelled";
                    return orderResult;
                }
                _logger.Write( "Validating acme-challenge => {0} for Domain {1}", cCtx.txtName, _domain.ZoneName );
                IChalageStatus chalageStatus = await this.ValidateChalage( cCtx.ctx );
                if ( chalageStatus.status == false ) {
                    orderResult.success = false;
                    orderResult.errorDescription = chalageStatus.errorDescription;
                    break;
                }

            }
            if ( _domain.IsWildcard ) {
                foreach ( DnsTxtStore txt in dnsTxt ) {
                    await _cfDns.RemoveRecord( new QueryConfig {
                        DOMAIN_NAME = _domain.ZoneName,
                        RECORD_TYPE = CFRecordType.TXT,
                        RECORD_NAME = "_acme-challenge",
                        RECORD_CONTENT = txt.content,
                        NAME = txt.name,
                        RECORD_ID = txt.id
                    } );
                }
            }

            if ( !orderResult.success ) {
                _logger.Write( "Error occured while creating order request for {0} . Error=>{1}", _domain.ZoneName, orderResult.errorDescription );
                return orderResult;
            }

            orderResult.taskType = TaskType.DOWNLOAD_CERT;
            orderResult.oldCertificate = oldCertificate.Cert;
            return orderResult;
        } catch ( Exception e ) {
            _logger.Write( $"Error occured while creating order request for {_domain.ZoneName}" );
            _logger.Write( e );
            return new OrderResult {
                taskType = TaskType.NOTHING,
                errorDescription = e.Message,
                success = false
            };
        }
    }
    private List<DnsTxtStore> GetDnsTXT( ) {
        try {
            string fstar = FileWorker.Read( string.Format( @"{0}_acme-challenge.dat", _domainDir ) );
            if ( string.IsNullOrEmpty( fstar ) ) return null;
            return JsonConvert.DeserializeObject<List<DnsTxtStore>>( fstar ); ;
        } catch ( Exception e ) {
            _logger.Write( e );
            return null;
        }
    }
    public async Task<bool> RemoveDnsTextRecord( ) {
        _logger.Write( $"Remove existing CloudFlare acme-challenge text record for {_domain.ZoneName}" );
        List<DnsTxtStore> dnsTxtStores = GetDnsTXT( );
        if ( dnsTxtStores != null && dnsTxtStores.Count > 0 ) {
            List<DnsTxtStore> pendingDnsTxtStores = new List<DnsTxtStore>( );
            foreach ( DnsTxtStore textRecord in dnsTxtStores ) {
                ICFAPIResponse resp = await _cfDns.RemoveRecord( new QueryConfig {
                    DOMAIN_NAME = _domain.ZoneName,
                    RECORD_TYPE = CFRecordType.TXT,
                    RECORD_NAME = "_acme-challenge",
                    RECORD_CONTENT = textRecord.content,
                    NAME = textRecord.name,
                    RECORD_ID = textRecord.id
                } );
                if ( resp.success == false ) {
                    pendingDnsTxtStores.Add( textRecord );
                }
            }
            if ( pendingDnsTxtStores.Count > 0 ) {
                if ( pendingDnsTxtStores.Count != dnsTxtStores.Count ) {
                    WriteDnsTXT( pendingDnsTxtStores );
                }
            } else {
                dnsTxtStores.Clear( );
                FileWorker.DeleteFile( string.Format( @"{0}_acme-challenge.dat", _domainDir ) );
            }
        }
        _logger.Write( $"End Remove existing CloudFlare acme-challenge text record for {_domain.ZoneName}" );
        return true;
    }
    private void WriteDnsTXT( List<DnsTxtStore> data ) => FileWorker.WriteFile( JsonConvert.SerializeObject( data ), string.Format( @"{0}_acme-challenge.dat", _domainDir ) );
    public void Dispose( ) {
        if ( IsDisposed ) return;
        _ = Interlocked.Increment( ref _isDisposed );
        _contex = null;
        _config = null;
        _orderContext = null;
        GC.SuppressFinalize( this );
        GC.Collect( 0, GCCollectionMode.Optimized );
    }
}
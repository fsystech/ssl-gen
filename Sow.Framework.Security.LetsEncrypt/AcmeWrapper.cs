﻿using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Certes;
using Certes.Acme;
using Certes.Acme.Resource;
using Certes.Pkcs;
//https://github.com/fszlin/certes
namespace Sow.Framework.Security.LetsEncrypt {
    using System.Linq;
    using Newtonsoft.Json;
    using Sow.Framework.Files;
    using Newtonsoft.Json.Linq;
    using Sow.Framework.Security.CloudflareWrapper;
    public class AcmeWrapper : IAcmeWrapper {
        private JsonSerializerSettings jsonSettings = new JsonSerializerSettings {
            NullValueHandling = NullValueHandling.Ignore,
            Formatting = Formatting.Indented
        };
        public IGConfig gConfig { get { return Global.gConfig; } set { throw new Exception( "readonly" ); } }
        private ICFDNS _cfDns;
        private IAcmeCtx _acmeCtx { get; set; }
        private IConfig _config { get; set; }
        private IOrderContext _orderContext { get; set; }
        CancellationToken _appct { get; set; }
        CancellationTokenSource _cancellationTokenSource { get; set; }
        CancellationToken _ct { get; set; }
        ILogger _logger { get; set; }
        public IDomain domain { get; set; }
        public int MAX_TRY { get; set; }
        private string _domainDir { get; set; }
        public string domainDir { get { return _domainDir; } set { throw new Exception( "readonly" ); } }
        private string _orderAbsolute { get; set; }
        private string _txtAbsolute { get; set; }
        private string _certDir { get; set; }
        public string certDir { get { return _certDir; } set { throw new Exception( "readonly" ); } }
        private bool _isValid { get; set; }
        private string _acmeApiServer { get; set; }
        public static bool IS_WINDOWS = Environment.OSVersion.Platform == PlatformID.Win32NT;
        public static string DefaultAcmeApiServer {
            get { return "LetsEncryptV2"; }
            set { throw new Exception( "readonly" ); }
        }
        public void Init( string web, IConfig config = null, ILogger logger = null, int mxtry = 10, CancellationToken ct = default( CancellationToken ) ) {
            if ( config != null ) {
                if ( _config == null || ( _config.CertEmail != config.CertEmail ) ) {
                    _config = config;
                    _config.Dir = Global.RegisterNewDirectory( _config.CertEmail );
                    if ( logger != null ) {
                        _logger = logger;
                    } else {
                        _logger = new Logger( isMs: true, write_cache: true );
                        _logger.Open( string.Format( @"{0}/log/{1}_{2}_{3}.log", Global.APP_DIR, DateTime.Now.ToString( "yyyy'-'MM'-'dd" ), _config.CertEmail.Replace( "@", "" ).Replace( "_", "" ).Replace( ".", "" ), Guid.NewGuid( ).ToString( "N" ) ) );
                    }
                    _logger.Write( "Working for {0}", _config.CertEmail );
                    _logger.FlushMemory( );
                }
            }
            if ( domain != null ) {
                if ( domain.ZoneName == web ) return;
            }
            domain = _config.Domain.Find( a => a.DomainName == web );
            if ( domain == null ) {
                string msg = string.Format( "No data found for domain {0} against Email {1}!!!", web, _config.CertEmail );
                _logger.Write( msg ); _logger.FlushMemory( );
                throw new Exception( msg );
            }
            _logger.Write( "Working for {0}", web );
            _logger.FlushMemory( );
            _domainDir = Global.RegisterNewDirectory( domain.ZoneName, _config.Dir );
            Global.RegisterNewDirectory( "order", _domainDir );
            _orderAbsolute = string.Format( @"{0}/order/new_order.bin", _domainDir );
            _txtAbsolute = string.Format( @"{0}/order/_acme-challenge.txt", _domainDir );
            _certDir = string.Format( @"{0}/cert/", _domainDir );
            FileWorker.CreateDirectory( _certDir );
            _cfDns = new CFDNS( new CFConfig {
                CF_API = Global.gConfig.CF_API,
                CF_URI = Global.gConfig.CF_URI,
                CF_AUTH_KEY = config.CF_AUTH_KEY,
                CF_AUTH_EMAIL = config.CF_AUTH_EMAIL ?? config.CertEmail,
                CF_DNS_ZONE = domain.CF_DNS_ZONE
            }, _logger ); ;
            _cancellationTokenSource = new CancellationTokenSource( );
            MAX_TRY = mxtry;
            _appct = _cancellationTokenSource.Token;
            _ct = ct;

        }
        public AcmeWrapper( IConfig config, string web, string acmeApiServer = "LetsEncryptV2", ILogger logger = null, int mxtry = 10, CancellationToken ct = default( CancellationToken ) ) {
            _isValid = false;
            _acmeApiServer = acmeApiServer;
            Init( config: config, web: web, logger: logger, mxtry: mxtry, ct: ct );
        }
        public void ReInit( string web ) {
            domain = null;
            _orderContext = null;
            _cfDns.Dispose( );
            _isValid = false;
            this.Init( web: web );
        }
        public (bool, string) IsValidConfig( bool logging = false ) {
            var (status, resp) = (true, "Success");
            if ( domain == null ) {
                (status, resp) = (false, "No Domain Config Found!!!");
                goto RETURN;
            }
            if ( string.IsNullOrEmpty( domain.DomainName ) ) {
                (status, resp) = (false, "Invalid Config Defined. Domain Name Required!!!");
                goto RETURN;
            }
            if ( string.IsNullOrEmpty( domain.ZoneName ) ) {
                (status, resp) = (false, string.Format( "Invalid Config Defined. Zone Name Required for domain {0}!!!", domain.DomainName ));
                goto RETURN;
            }
            if ( string.IsNullOrEmpty( domain.CF_DNS_ZONE ) ) {
                (status, resp) = (false, string.Format( "Cloudflare Dns Zone required for {0}!!!", domain.DomainName ));
                goto RETURN;
            }
            if ( domain.Csr == null ) {
                (status, resp) = (false, string.Format( "CSR required for {0}!!!", domain.DomainName ));
                goto RETURN;
            }
            if ( domain.IsSelfHost && domain.StoreCertificate ) {
                if ( domain.appSettings == null ) {
                    (status, resp) = (false, string.Format( "IIS Settings required for {0}!!!", domain.DomainName ));
                    goto RETURN;
                }
                if ( domain.appSettings.Site == null ) {
                    (status, resp) = (false, string.Format( "Site(s) required in IIS Settings for {0}!!!", domain.DomainName ));
                    goto RETURN;
                }
            }
        RETURN:
            _isValid = status;
            if ( !status && logging ) {
                _logger.Write( resp );
            }
            return (status, resp);
        }
        private Uri GetAcmeServer( ) {
            switch ( _acmeApiServer ) {
                case "LetsEncrypt": return WellKnownServers.LetsEncrypt;
                case "LetsEncryptV2": return WellKnownServers.LetsEncryptV2;
                case "LetsEncryptStaging": return WellKnownServers.LetsEncryptStaging;
                case "LetsEncryptStagingV2": return WellKnownServers.LetsEncryptStagingV2;
            }
            throw new Exception( "Invalid Acme Api Server!!" );
        }
        private async Task PrepareContext( ) {
            if ( _acmeCtx != null ) return;
            _acmeCtx = new AcmeCtx( );
            string pemKey = FileWorker.Read( string.Format( @"{0}/{1}_account.pem", _config.Dir, _acmeApiServer ) );
            if ( string.IsNullOrEmpty( pemKey ) ) {
                _acmeCtx.Ctx = new AcmeContext( GetAcmeServer( ) );
                _acmeCtx.ACtx = await _acmeCtx.Ctx.NewAccount( email: _config.CertEmail, termsOfServiceAgreed: true );
                pemKey = _acmeCtx.Ctx.AccountKey.ToPem( );
                FileWorker.WriteFile( pemKey, string.Format( @"{0}/{1}_account.pem", _config.Dir, _acmeApiServer ) );
                _logger.Write( "New registration created successfully for :: {0}", _config.CertEmail );
                return;
            }
            IKey accountKey = KeyFactory.FromPem( pemKey );
            _acmeCtx.Ctx = new AcmeContext( GetAcmeServer( ), accountKey );
            _acmeCtx.ACtx = await _acmeCtx.Ctx.Account( );
            _logger.Write( "Re-authenticated :: {0}", _config.CertEmail );
            return;
        }
        public static IConfig GetConfig( string email, string dir = null ) {
            if ( !string.IsNullOrEmpty( dir ) ) {
                Global.APP_DIR = dir;
            }
            if ( Global.gConfig == null ) {
                Global.Load( string.Format( @"{0}/AcmeWrapper/config.json", Global.APP_DIR ) );
            }
            return Global.gConfig.config.Find( a => a.CertEmail == email );
        }
        public static IWinConfig GetWinConfig( ) {
            if ( Global.gConfig == null ) {
                throw new Exception( "Config not initilized yet!!!" );
            }
            return Global.gConfig.winConfig;
        }
        private void SaveNewOrder( ) {
            _logger.Write( $"Order location {_orderContext.Location}" );
            FileWorker.WriteBinary( _orderContext.Location, _orderAbsolute );
        }
        public bool LoadOrder( ) {
            object loc = FileWorker.ReadBinary( _orderAbsolute );
            if ( loc == null ) return false;
            _orderContext = _acmeCtx.Ctx.Order( ( Uri )loc );
            return true;
        }
        async Task<IChalageStatus> ValidateChalage( IChallengeContext challengeCtx ) {
            // Now let's ping the ACME service to validate the challenge token
            try {
                // We sleep 5 seconds between each request, to leave time to ACME service to refresh
                Thread.Sleep( 5000 );
                Challenge challenge = await challengeCtx.Validate( );
                if ( challenge.Status == ChallengeStatus.Invalid ) {
                    _logger.Write( "Error occured while validating acme challenge to {0} :: error==>{1}", domain.ZoneName, challenge.Error.Detail );
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
                    Thread.Sleep( 5000 );
                    // We refresh the challenge object from ACME service
                    challenge = await challengeCtx.Resource( );
                    retry++;
                }
                if ( challenge.Status == ChallengeStatus.Invalid ) {
                    _logger.Write( "Error occured while validating acme challenge to {0} :: error==>{1}", domain.ZoneName, challenge.Error.Detail );
                    return new ChalageStatus {
                        status = false,
                        errorDescription = challenge.Error.Detail
                    };
                }
                return new ChalageStatus {
                    status = true
                };
            } catch ( Exception e ) {
                _logger.Write( "Error occured while validating acme challenge to {0} :: error==>{1}", domain.ZoneName, e.Message );
                _logger.Write( e.StackTrace );
                return new ChalageStatus {
                    status = false,
                    errorDescription = e.Message
                };
            }
        }
        public WebServerEnum GetServerType( ) {
            return this.domain.appSettings.webServerEnum;
        }
        public string GetServerName( WebServerEnum webServerEnum = WebServerEnum.NONE ) {
            if( webServerEnum == WebServerEnum.NONE ) {
                return GetServerName( this.GetServerType() );
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
        private string CertEnumToString( CertEnum certTyp ) {
            if ( certTyp == CertEnum.PFX ) return "PFX";
            if ( certTyp == CertEnum.PRIVATE ) return "PRIVATE";
            if ( certTyp == CertEnum.FULL_CHAIN ) return "FULL_CHAIN";
            if ( certTyp == CertEnum.DER ) return "DER";
            throw new Exception( "Invalid Cert Enum..." );
        }
        private void CopyTo( CertEnum certEnum, string toDir ) {
            string fromPath = CreateCertAbsPath( domain.ZoneName, certEnum );
            string toPath = CreateCertAbsPath( domain.ZoneName, certEnum, toDir );
            try {
                if ( System.IO.File.Exists( fromPath ) ) {
                    System.IO.File.Copy( fromPath, toPath, true );
                }
            } catch ( Exception e ) {
                _logger.Write( $"Unable to copy {CertEnumToString( certEnum )} form {fromPath} to {toPath}" );
                _logger.Write( $"Eror: {e.StackTrace}" );
            }
        }
        public void CopyTo( ) {
            if ( string.IsNullOrEmpty( domain.appSettings.CopyTo ) ) return;
            string absPath = $"{domain.appSettings.CopyTo}{domain.ZoneName}//";
            FileWorker.CreateDirectory( absPath );
            CopyTo( CertEnum.PFX, absPath );
            CopyTo( CertEnum.PRIVATE, absPath );
            CopyTo( CertEnum.FULL_CHAIN, absPath );
            CopyTo( CertEnum.DER, absPath );
        }
        public async Task<ICertificate> CreateCertificate( ) {
            try {
                _logger.Write( "Generating certificate for {0}", domain.ZoneName );
                IKey privateKey = KeyFactory.NewKey( KeyAlgorithm.ES256 );
                domain.Csr.CommonName = domain.DomainName;
                CertificateChain cert = await _orderContext.Generate( domain.Csr, privateKey );
                PfxBuilder pfxBuilder = cert.ToPfx( privateKey );
                
                byte[] pfx = pfxBuilder.Build( domain.ZoneName, "sow_le_pkey" );
                foreach ( string type in domain.appSettings.ExportType ) {
                    switch ( type ) {
                        case "PFX":
                            FileWorker.WriteFile( pfx, CreateCertAbsPath( domain.ZoneName, CertEnum.PFX ) );
                            break;
                        case "PRIVATE":
                            FileWorker.WriteFile( privateKey.ToPem( ), CreateCertAbsPath( domain.ZoneName, CertEnum.PRIVATE ) );
                            break;
                        case "DER":
                            FileWorker.WriteFile( cert.Certificate.ToDer( ), CreateCertAbsPath( domain.ZoneName, CertEnum.DER ) );
                            break;
                        case "FULL_CHAIN":
                            FileWorker.WriteFile( cert.ToPem( certKey: privateKey ), CreateCertAbsPath( domain.ZoneName, CertEnum.FULL_CHAIN ) );
                            break;
                    }
                }
                //FileWorker.WriteFile( pfx, CreateCertAbsPath( domain.ZoneName, CertEnum.PFX ) );
                //FileWorker.WriteFile( privateKey.ToPem( ), CreateCertAbsPath( domain.ZoneName, CertEnum.PRIVATE ) );
                //FileWorker.WriteFile( cert.ToPem( certKey: privateKey ), CreateCertAbsPath( domain.ZoneName, CertEnum.FULL_CHAIN ) );
                //FileWorker.WriteFile( cert.Certificate.ToDer( ), CreateCertAbsPath( domain.ZoneName, CertEnum.DER ) );
                return new Certificate {
                    status = true,
                    cert_dir = _certDir,
                    Cert = this.GetX509Certificate2( pfx ),
                    isExpired = false
                };
            } catch ( Exception e ) {
                _logger.Write( "Error occured while generating certificate for {0} :: error==>{1}", domain.ZoneName, e.Message );
                _logger.Write( e.StackTrace );
                return new Certificate {
                    status = false,
                    errorDescription = e.Message
                };
            }
        }
        private string GetJPropertyValue( JProperty jProperty ) {
            if ( jProperty != null ) {
                return jProperty.Value.ToString( );
            }
            return null;
        }
        private X509Certificate2 GetX509Certificate2( byte[] pfx ) {
            X509Certificate2 cert = new X509Certificate2( pfx, "sow_le_pkey", X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet );
            return cert;
        }
        public bool ExistsCertificate( CertEnum certEnum = CertEnum.PFX ) {
            return FileWorker.ExistsFile( CreateCertAbsPath( domain.ZoneName, certEnum ) );
        }
        public ICertificate GetCertificate( ) {
            try {
                string cert_path = string.Format( "{0}{1}.pfx", _certDir, domain.ZoneName );
                byte[] pfx = FileWorker.ReadAllByte( string.Format( "{0}{1}.pfx", _certDir, domain.ZoneName ) );
                if ( pfx == null ) {
                    return new Certificate {
                        status = false,
                        errorDescription = string.Format( "No existing Certificate for {0}", domain.ZoneName )
                    };
                }
                X509Certificate2 cert = this.GetX509Certificate2( pfx );
                int result = DateTime.Compare( cert.NotAfter, DateTime.Now );
                return new Certificate {
                    status = true,
                    Cert = cert,
                    isExpired = result < 0,
                    cert_dir = _certDir,
                    cert_path= cert_path
                };
            } catch ( Exception e ) {
                _logger.Write( "Error occured while read certificate for {0} :: error==>{1}", domain.ZoneName, e.Message );
                _logger.Write( e.StackTrace );
                return new Certificate {
                    status = false,
                    errorDescription = e.Message
                };
            }
        }
        private void RemoveOldOrder( ) {
            // _orderContext.Location
        }
        public async Task<IOrderResult> CreateOrRenewCert( int rec = 0, bool forceRenew = false ) {
            try {
                {
                    var (status, result) = IsValidConfig( );
                    if ( !status ) {
                        _logger.Write( result );
                        return new OrderResult {
                            success = false,
                            taskType = TaskType.NOTHING,
                            errorDescription = result
                        };
                    }
                }
                _logger.Write( "Create or Renew Certificate for {0}", domain.ZoneName );
                ICertificate oldCertificate = GetCertificate( );
                if ( oldCertificate.status ) {
                    if ( !oldCertificate.isExpired && forceRenew == false ) {
                        _logger.Write( "Certificate not expired for {0}", domain.ZoneName );
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
                        _logger.Write( "Revoke Certificate for {0} & expired on {1} and serial {2}", domain.ZoneName, oldCertificate.Cert.NotAfter.ToShortDateString( ), oldCertificate.Cert.SerialNumber );
                        try {
                            await _acmeCtx.Ctx.RevokeCertificate( oldCertificate.Cert.RawData, RevocationReason.Unspecified );
                        } catch ( Exception r ) {
                            FileWorker.DeleteFile( oldCertificate.cert_path );
                            _logger.Write( "Error occured while Revoke Certificate for {0}; Error=>{1}", domain.ZoneName, r.Message );
                            _logger.Write( r.StackTrace );
                            _logger.Write( "Whatever we are trying to create new certificate." );
                            /*if (r.Message.IndexOf( "alreadyRevoked" ) <= -1) {
                                return new OrderResult {
                                    taskType = TaskType.NOTHING,
                                    errorDescription = r.Message,
                                    success = false
                                };
                            }*/
                        }
                    } else {
                        _logger.Write( "This old Certificate expired on {0} and serial {1}", oldCertificate.Cert.NotAfter.ToShortDateString( ), oldCertificate.Cert.SerialNumber );
                    }
                }
                if ( domain.IsWildcard ) {
                    _orderContext = await _acmeCtx.Ctx.NewOrder( new List<string> { domain.ZoneName, domain.DomainName } );
                } else {
                    _orderContext = await _acmeCtx.Ctx.NewOrder( new List<string> { domain.DomainName } );
                }
                SaveNewOrder( );
                List<IChallengeCtx> challengeCtxs = new List<IChallengeCtx>( );
                List<IDnsTxtStore> dnsTxt = this.GetDnsTXT( );
                if ( domain.IsWildcard ) {
                    if ( dnsTxt == null ) dnsTxt = new List<IDnsTxtStore>( );
                    List<IDnsTxtStore> writeDnsTxt = new List<IDnsTxtStore>( );
                    _logger.Write( "Defined acme challenge type DNS for Wildcard(*) {0}", domain.ZoneName );
                    _logger.Write( "Get Authorization Context for {0}", domain.ZoneName );
                    IEnumerable<IAuthorizationContext> authCtx = await _orderContext.Authorizations( );
                    _logger.Write( "Authorization Context found for {0}", domain.ZoneName );
                    foreach ( IAuthorizationContext authz in authCtx ) {
                        IChallengeContext challengeCtx = await authz.Dns( );
                        string txt = _acmeCtx.Ctx.AccountKey.DnsTxt( challengeCtx.Token );
                        IDnsTxtStore dnsTxtStore = dnsTxt.FirstOrDefault( a => a.content == txt );
                        if ( dnsTxtStore != null ) {
                            challengeCtxs.Add( new ChallengeCtx {
                                ctx = challengeCtx, txtName = dnsTxtStore.name
                            } );
                            dnsTxt.Remove( dnsTxtStore );
                            continue;
                        }
                        ICFAPIResponse cFAPIResponse = await _cfDns.AddRecord( new QueryConfig {
                            DOMAIN_NAME = domain.ZoneName,
                            RECORD_TYPE = CFRecordType.TXT,
                            RECORD_NAME = "_acme-challenge",
                            RECORD_CONTENT = txt,
                            NAME = string.Concat( "_acme-challenge.", domain.ZoneName )
                        } );
                        if ( !cFAPIResponse.success ) {
                            orderResult.success = false;
                            orderResult.errorDescription = JsonConvert.SerializeObject( cFAPIResponse.messages, _cfDns.jsonSettings );
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
                            txtName = string.Concat( "_acme-challenge.", domain.ZoneName );
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
                    Thread.Sleep( 5000 );//Wait for 5s
                    _logger.Write( "Validating acme-challenge => {0} for Domain {1}", cCtx.txtName, domain.ZoneName );
                    IChalageStatus chalageStatus = await this.ValidateChalage( cCtx.ctx );
                    if ( chalageStatus.status == false ) {
                        orderResult.success = false;
                        orderResult.errorDescription = chalageStatus.errorDescription;
                        break;
                    }

                }
                if ( domain.IsWildcard ) {
                    foreach ( IDnsTxtStore txt in dnsTxt ) {
                        await _cfDns.RemoveRecord( new QueryConfig {
                            DOMAIN_NAME = domain.ZoneName,
                            RECORD_TYPE = CFRecordType.TXT,
                            RECORD_NAME = "_acme-challenge",
                            RECORD_CONTENT = txt.content,
                            NAME = txt.name,
                            RECORD_ID = txt.id
                        } );
                    }
                }

                if ( !orderResult.success ) {
                    _logger.Write( "Error occured while creating order request for {0} . Error=>{1}", domain.ZoneName, orderResult.errorDescription );
                    return orderResult;
                }

                orderResult.taskType = TaskType.DOWNLOAD_CERT;
                orderResult.oldCertificate = oldCertificate.Cert;
                return orderResult;
            } catch ( Exception e ) {
                if ( rec >= MAX_TRY ) {
                    _logger.Write( "Error occured while creating order request for {0} . Error=>{1}", domain.ZoneName, e.Message );
                    _logger.Write( e.StackTrace );
                    return new OrderResult {
                        taskType = TaskType.NOTHING,
                        errorDescription = e.Message,
                        success = false
                    };
                }
                return await CreateOrRenewCert( rec++ );
            }
        }
        private List<IDnsTxtStore> GetDnsTXT( ) {
            try {
                object obj = FileWorker.ReadBinary( string.Format( @"{0}_acme-challenge.dat", _domainDir ) );
                if ( obj == null ) return null;
                return ( List<IDnsTxtStore> )obj;
            } catch ( Exception e ) {
                _logger.Write( e.Message );
                _logger.Write( e.StackTrace );
                return null;
            }
        }
        public async Task<bool> RemoveDnsTextRecord( ) {
            _logger.Write( $"Remove existing CloudFlare acme-challenge text record for {domain.ZoneName}" );
            List<IDnsTxtStore> dnsTxtStores = GetDnsTXT( );
            if ( dnsTxtStores != null && dnsTxtStores.Count > 0 ) {
                List<IDnsTxtStore> pendingDnsTxtStores = new List<IDnsTxtStore>( );
                foreach ( IDnsTxtStore textRecord in dnsTxtStores ) {
                    ICFAPIResponse resp = await _cfDns.RemoveRecord( new QueryConfig {
                        DOMAIN_NAME = domain.ZoneName,
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
            _logger.Write( $"End Remove existing CloudFlare acme-challenge text record for {domain.ZoneName}" );
            return true;
        }
        private void WriteDnsTXT( List<IDnsTxtStore> data ) {
            FileWorker.WriteBinary( data, string.Format( @"{0}_acme-challenge.dat", _domainDir ) );
            return;
        }
        public void Dispose( ) {
            _acmeCtx = null;
            _config = null;
            domain = null;
            _orderContext = null;
            GC.SuppressFinalize( this );
            GC.Collect( 0, GCCollectionMode.Optimized );
        }
    }
}

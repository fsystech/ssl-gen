/**
* Copyright (c) 2018, Sow ( https://safeonline.world, https://www.facebook.com/safeonlineworld). (https://github.com/RKTUXYN) All rights reserved.
* Copyrights licensed under the New BSD License.
* See the accompanying LICENSE file for terms.
*/
// 11:26 AM 5/21/2022
// Rajib Chy
using System;
using Sow.Framework;
using Sow.Framework.Files;
using Sow.Framework.Security;
using Sow.Framework.Security.LetsEncrypt;
using System.Security.Cryptography.X509Certificates;
namespace Sow.WCartGen;
internal class Win : WCartGenBase {
    public Win( Arguments arguments ) : base( arguments ) { }
    protected override async void StartWork( object state ) {
        try {
            //6:10 AM 9/14/2018 Rajib
            //IConfig config = AcmeWrapper.GetConfig( "mssclang@outlook.com" );
            //IAcmeWrapper acmeWrapper = new AcmeWrapper( config, "*.tripecosys.com", CancellationToken.None );
            using ( IAcmeWrapper acmeWrapper = new AcmeWrapper( config: _config, web: _arguments.web, acmeApiServer: _arguments.acmeApiServer ?? AcmeWrapper.DefaultAcmeApiServer, logger: _logger, ct: _tokenSource.Token ) ) {
                X509Certificate2 cert = null;
                IIISWrapper iISWrapper = AcmeWrapper.IS_WINDOWS == false ? null : new IISWrapper( );
                if ( _arguments.forceRenew == false ) {
                    _logger.Write( "Check if valid Certificate exists for {0}", acmeWrapper.domain.ZoneName );
                    if ( acmeWrapper.ExistsCertificate( ) ) {
                        await acmeWrapper.RemoveDnsTextRecord( );
                        ICertificate cer = acmeWrapper.GetCertificate( );
                        if ( !cer.isExpired && cer.status ) {
                            var (status, resp) = acmeWrapper.IsValidConfig( logging: true );
                            if ( !status ) {
                                goto EXIT;
                            }
                            cert = cer.Cert;
                            _logger.Write( "A valid Certificate found for {0}; Cert Serial:: {1}", acmeWrapper.domain.ZoneName, cert.SerialNumber );
                        }
                    }
                } else {
                    await acmeWrapper.RemoveDnsTextRecord( );
                }
                if ( cert == null ) {
                    IOrderResult orderResult = await acmeWrapper.CreateOrRenewCert( forceRenew: _arguments.forceRenew, rec: acmeWrapper.MAX_TRY );
                    if ( orderResult.success != true ) {
                        goto EXIT;
                    }
                    if ( orderResult.taskType == TaskType.DOWNLOAD_CERT ) {
                        ICertificate certificate = await acmeWrapper.CreateCertificate( );
                        if ( certificate.status == false ) goto EXIT;
                        acmeWrapper.CopyTo( );
                        cert = certificate.Cert;
                        if ( AcmeWrapper.IS_WINDOWS && acmeWrapper.domain.StoreCertificate == true ) {
                            if ( !iISWrapper.InstallCertificate( certificate: cert, zoneName: acmeWrapper.domain.ZoneName, oldCertificate: orderResult.oldCertificate, storeName: StoreName.My, logger: _logger ) )
                                goto EXIT;
                        }

                    } else if ( orderResult.taskType == TaskType.INSTALL_CERT ) {
                        cert = orderResult.oldCertificate;
                        if ( AcmeWrapper.IS_WINDOWS ) {
                            if ( !iISWrapper.InstallCertificate( certificate: cert, zoneName: acmeWrapper.domain.ZoneName, storeName: StoreName.My, logger: _logger ) )
                                goto EXIT;
                        }
                    } else {
                        goto EXIT;
                    }
                    orderResult = null;
                } else {
                    if ( acmeWrapper.domain.StoreCertificate == true ) {
                        if ( AcmeWrapper.IS_WINDOWS ) {
                            if ( !iISWrapper.InstallCertificate( certificate: cert, zoneName: acmeWrapper.domain.ZoneName, storeName: StoreName.My, logger: _logger ) )
                                goto EXIT;
                        }
                    }
                }
                if ( AcmeWrapper.IS_WINDOWS ) {
                    if ( acmeWrapper.GetServerType( ) == WebServerEnum.IIS ) {
                        if ( acmeWrapper.domain.StoreCertificate == true ) {
                            _logger.Write( "Bind Certificate for {0}", acmeWrapper.domain.ZoneName );
                            IIISWrapperResponse iSWrapperResponse = iISWrapper.BindCertificate( AppSettings: new IISWrapperSettings {
                                SiteName = acmeWrapper.domain.ZoneName,
                                CertificateStoreName = "My",
                                AppPool = acmeWrapper.domain.appSettings.AppPool,
                                CertificateHash = cert.GetCertHash( ),
                                Site = acmeWrapper.domain.appSettings.Site,
                                ZoneName = acmeWrapper.domain.ZoneName
                            }, logger: _logger, token: _tokenSource.Token );
                            if ( iSWrapperResponse.Error ) {
                                _logger.Write( iSWrapperResponse.ErrorDescription );
                            }
                        }
                    }
                    _logger.Write( "Certificate process completed for {0}", acmeWrapper.domain.ZoneName );
                    IWinConfig winConfig = acmeWrapper.gConfig.winConfig;
                    string batAbsolute = string.Concat( acmeWrapper.domainDir, "ScheduleRunner.bat" );
                    if ( FileWorker.ExistsFile( batAbsolute ) ) {
                        FileWorker.DeleteFile( batAbsolute );
                    }
                    _logger.Write( "Creating .bat file for Schedule runner for {0}", acmeWrapper.domain.ZoneName );
                    string bat = string.Format( "rem Auto Generated on {0}\r\n", DateTime.Now.ToString( ) );
                    bat += string.Format( "cd {0} \r\n", App.Dir );
                    bat += string.Format( "dotnet wcert_gen.dll -e {0} -w {1} -fr Y", _config.CertEmail, acmeWrapper.domain.DomainName );
                    if ( acmeWrapper.domain.appSettings.webServerEnum == WebServerEnum.NGINX ) {
                        bat += "\r\nnet stop nginx";
                        bat += "\r\nnet start nginx";
                    }
                    FileWorker.WriteFile( bat, batAbsolute );
                    _logger.Write( ".bat file creation completed for Schedule runner for {0}", acmeWrapper.domain.ZoneName );
                    _logger.Write( "Register new Task Schedule for Renew the Certificate for {0}", acmeWrapper.domain.ZoneName );
                    TaskSchedule.Create( settings: new ScheduleSettings {
                        TaskName = string.Format( @"\LetsEncryptWrapper\SSL\{0}", acmeWrapper.domain.ZoneName ),
                        TriggerDateTime = cert.NotAfter,/**DateTime.Now.AddDays( 1 ),*/
                        Description = string.Format( "Register new Task Schedule for Renew the Certificate for {0}; Cert Serail::{1}", acmeWrapper.domain.ZoneName, cert.SerialNumber ),
                        ActionPath = batAbsolute,//@"dotnet wcert_gen.dll",
                        Arguments = null,//string.Format( "-e {0} -w {1} -fy Y", config.Email, acmeWrapper.domain.DomainName ),
                        UserName = winConfig.WinUser,
                        Password = winConfig.WinPassword,
                        StartIn = App.Dir
                    }, logger: _logger );
                    _logger.Write( "Task Schedule Registration completed for {0}", acmeWrapper.domain.ZoneName );
                }
            }
            goto EXIT;
        } catch ( Exception e ) {
            _logger.Write( e.Message );
            _logger.Write( e.StackTrace );
            goto EXIT;
        }
        EXIT:
        Exit( );
    }
}
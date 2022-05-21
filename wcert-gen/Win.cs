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
            using ( IAcmeWrapper acmeWrapper = new AcmeWrapper( config: _config, web: _arguments.Web, acmeApiServer: _arguments.AcmeApiServer ?? AcmeWrapper.DefaultAcmeApiServer, logger: _logger, token: _tokenSource.Token ) ) {
                X509Certificate2 cert = null;
                IIISWrapper iISWrapper = App.IsWindows == false ? null : new IISWrapper( );
                if ( _arguments.ForceRenew == false ) {
                    _logger.Write( "Check if valid Certificate exists for {0}", acmeWrapper.DomainInfo.ZoneName );
                    if ( acmeWrapper.ExistsCertificate( ) ) {
                        await acmeWrapper.RemoveDnsTextRecord( );
                        ICertificate cer = acmeWrapper.GetCertificate( );
                        if ( !cer.isExpired && cer.status ) {
                            var (status, resp) = acmeWrapper.IsValidConfig( logging: true );
                            if ( !status ) {
                                goto EXIT;
                            }
                            cert = cer.Cert;
                            _logger.Write( "A valid Certificate found for {0}; Cert Serial:: {1}", acmeWrapper.DomainInfo.ZoneName, cert.SerialNumber );
                        }
                    }
                } else {
                    await acmeWrapper.RemoveDnsTextRecord( );
                }
                if ( cert == null ) {
                    IOrderResult orderResult = await acmeWrapper.CreateOrRenewCert( forceRenew: _arguments.ForceRenew, rec: acmeWrapper.MAX_TRY );
                    if ( orderResult.success != true ) {
                        goto EXIT;
                    }
                    if ( orderResult.taskType == TaskType.DOWNLOAD_CERT ) {
                        ICertificate certificate = await acmeWrapper.CreateCertificate( );
                        if ( certificate.status == false ) goto EXIT;
                        cert = certificate.Cert;
                        if ( acmeWrapper.ServerType == WebServerEnum.NGINX ) {
                            acmeWrapper.CopyTo( );
                        } else if ( App.IsWindows && acmeWrapper.ServerType == WebServerEnum.IIS ) {
                            if ( acmeWrapper.DomainInfo.StoreCertificate == true ) {
                                if ( !iISWrapper.InstallCertificate( certificate: cert, zoneName: acmeWrapper.DomainInfo.ZoneName, oldCertificate: orderResult.oldCertificate, storeName: StoreName.My, logger: _logger ) )
                                    goto EXIT;
                            }
                        }

                    } else if ( orderResult.taskType == TaskType.INSTALL_CERT ) {
                        cert = orderResult.oldCertificate;
                        if ( acmeWrapper.ServerType == WebServerEnum.NGINX ) {
                            acmeWrapper.CopyTo( );
                        } else if ( App.IsWindows && acmeWrapper.ServerType == WebServerEnum.IIS ) {
                            if ( !iISWrapper.InstallCertificate( certificate: cert, zoneName: acmeWrapper.DomainInfo.ZoneName, storeName: StoreName.My, logger: _logger ) )
                                goto EXIT;
                        }
                    } else {
                        goto EXIT;
                    }
                    orderResult = null;
                } else {
                    if ( acmeWrapper.ServerType == WebServerEnum.NGINX ) {
                        acmeWrapper.CopyTo( );
                    } else if ( App.IsWindows && acmeWrapper.ServerType == WebServerEnum.IIS ) {
                        if ( acmeWrapper.DomainInfo.StoreCertificate == true ) {
                            if ( !iISWrapper.InstallCertificate( certificate: cert, zoneName: acmeWrapper.DomainInfo.ZoneName, storeName: StoreName.My, logger: _logger ) )
                                goto EXIT;
                        }
                    }
                }
                if ( App.IsWindows ) {
                    if ( acmeWrapper.ServerType == WebServerEnum.IIS ) {
                        if ( acmeWrapper.DomainInfo.StoreCertificate == true ) {
                            _logger.Write( "Bind Certificate for {0}", acmeWrapper.DomainInfo.ZoneName );
                            IIISWrapperResponse iSWrapperResponse = iISWrapper.BindCertificate( AppSettings: new IISWrapperSettings {
                                SiteName = acmeWrapper.DomainInfo.ZoneName,
                                CertificateStoreName = "My",
                                AppPool = acmeWrapper.DomainInfo.AppSettings.AppPool,
                                CertificateHash = cert.GetCertHash( ),
                                Site = acmeWrapper.DomainInfo.AppSettings.Site,
                                ZoneName = acmeWrapper.DomainInfo.ZoneName
                            }, logger: _logger, token: _tokenSource.Token );
                            if ( iSWrapperResponse.Error ) {
                                _logger.Write( iSWrapperResponse.ErrorDescription );
                            }
                        }
                    }
                    _logger.Write( "Certificate process completed for {0}", acmeWrapper.DomainInfo.ZoneName );
                    IWinConfig winConfig = acmeWrapper.AppConfig.WinCfg;
                    string batAbsolute = System.IO.Path.Combine( acmeWrapper.DomainDir, "ScheduleRunner.bat" );
                    if ( FileWorker.ExistsFile( batAbsolute ) ) {
                        FileWorker.DeleteFile( batAbsolute );
                    }
                    _logger.Write( "Creating .bat file for Schedule runner for {0}", acmeWrapper.DomainInfo.ZoneName );
                    string bat = string.Format( "rem Auto Generated on {0}\r\n", DateTime.Now.ToString( ) );
                    bat += $"cd {App.Dir} \r\n";
                    bat += $"{App.FileName} -e {_config.CertEmail} -w {acmeWrapper.DomainInfo.DomainName} -fr y -c {acmeWrapper.AppConfig.ConfigKey}";
                    if ( acmeWrapper.DomainInfo.AppSettings.WebServer == WebServerEnum.NGINX ) {
                        bat += "\r\nnet stop nginx";
                        bat += "\r\nnet start nginx";
                    }
                    FileWorker.WriteFile( bat, batAbsolute );
                    _logger.Write( ".bat file creation completed for Schedule runner for {0}", acmeWrapper.DomainInfo.ZoneName );
                    _logger.Write( "Register new Task Schedule for Renew the Certificate for {0}", acmeWrapper.DomainInfo.ZoneName );
                    if ( winConfig == null || string.IsNullOrEmpty( winConfig.WinUser ) || string.IsNullOrEmpty( winConfig.WinPassword ) ) {
                        _logger.Write( "We are unable to create schedule for Renew the Certificate" );
                        _logger.Write( "No windows crediantial found in config->WinConfig" );
                    } else {
                        TaskSchedule.Create( settings: new ScheduleSettings {
                            TaskName = string.Format( @"\LetsEncryptWrapper\SSL\{0}", acmeWrapper.DomainInfo.ZoneName ),
                            TriggerDateTime = cert.NotAfter.Subtract( TimeSpan.FromDays( 1 ) ),
                            Description = string.Format( "Register new Task Schedule for Renew the Certificate for {0}; Cert Serail::{1}", acmeWrapper.DomainInfo.ZoneName, cert.SerialNumber ),
                            ActionPath = batAbsolute,//@"dotnet wcert_gen.dll",
                            Arguments = null,//string.Format( "-e {0} -w {1} -fy Y", config.Email, acmeWrapper.domain.DomainName ),
                            UserName = winConfig.WinUser,
                            Password = winConfig.WinPassword,
                            StartIn = App.Dir
                        }, logger: _logger );
                        _logger.Write( "Task Schedule Registration completed for {0}", acmeWrapper.DomainInfo.ZoneName );
                    }
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
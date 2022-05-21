/**
* Copyright (c) 2018, Sow ( https://safeonline.world, https://www.facebook.com/safeonlineworld). (https://github.com/RKTUXYN) All rights reserved.
* Copyrights licensed under the New BSD License.
* See the accompanying LICENSE file for terms.
*/
// 10:51 AM 5/21/2022
// Rajib Chy
using System;
using Sow.Framework;
using Sow.Framework.Files;
using Sow.Framework.Security.LetsEncrypt;
namespace Sow.WCartGen;
class Unix : WCartGenBase {
    public Unix( Arguments arguments ) : base( arguments ) { }
    protected override async void StartWork( object state ) {
        try {
            Console.WriteLine( @"We are in Unix Operating system." );
            //6:10 AM 9/14/2018 Rajib
            //IConfig config = AcmeWrapper.GetConfig( "mssclang@outlook.com" );
            //IAcmeWrapper acmeWrapper = new AcmeWrapper( config, "*.tripecosys.com", CancellationToken.None );
            using ( IAcmeWrapper acmeWrapper = new AcmeWrapper( config: _config, web: _arguments.web, acmeApiServer: _arguments.acmeApiServer ?? AcmeWrapper.DefaultAcmeApiServer, logger: _logger, ct: _tokenSource.Token ) ) {
                if ( _arguments.forceRenew == false ) {
                    _logger.Write( "Check if valid Certificate exists for {0}", acmeWrapper.domain.ZoneName );
                    if ( acmeWrapper.ExistsCertificate( ) ) {
                        ICertificate cer = acmeWrapper.GetCertificate( );
                        if ( !cer.isExpired && cer.status ) {
                            var (status, resp) = acmeWrapper.IsValidConfig( logging: true );
                            if ( !status ) {
                                goto EXIT;
                            }
                            _logger.Write( "A valid Certificate found for {0}; Cert Serial:: {1}", acmeWrapper.domain.ZoneName, cer.Cert.SerialNumber );
                            goto EXIT;
                        }
                    }
                }
                IOrderResult orderResult = await acmeWrapper.CreateOrRenewCert( forceRenew: _arguments.forceRenew, rec: acmeWrapper.MAX_TRY );
                if ( orderResult.success != true ) {
                    goto EXIT;
                }
                if ( orderResult.taskType == TaskType.DOWNLOAD_CERT ) {
                    ICertificate certificate = await acmeWrapper.CreateCertificate( );
                    if ( certificate.status == false ) {
                        goto EXIT;
                    }
                } else {
                    goto EXIT;
                }
                orderResult = null;
                _logger.Write( "Your certificate is ready. Your cert bundle here  {0}", acmeWrapper.certDir );
                string batAbsolute = string.Concat( acmeWrapper.domainDir, "ScheduleRunner.bat" );
                if ( FileWorker.ExistsFile( batAbsolute ) ) {
                    FileWorker.DeleteFile( batAbsolute );
                }
                _logger.Write( "Creating .bat file for Schedule runner for {0}", acmeWrapper.domain.ZoneName );
                string bat = string.Format( "rem Auto Generated on {0}\r\n", DateTime.Now.ToString( ) );
                bat += string.Format( "dotnet wcert_gen.dll -e {0} -w {1} -fr Y", _config.CertEmail, acmeWrapper.domain.DomainName );
                FileWorker.WriteFile( bat, batAbsolute );
                _logger.Write( ".bat file creation completed for Schedule runner for {0}", acmeWrapper.domain.ZoneName );

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
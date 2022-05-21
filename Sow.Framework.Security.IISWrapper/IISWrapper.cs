/**
* Copyright (c) 2018, Sow ( https://safeonline.world, https://www.facebook.com/safeonlineworld). (https://github.com/RKTUXYN) All rights reserved.
* Copyrights licensed under the New BSD License.
* See the accompanying LICENSE file for terms.
*/
// 8:04 PM 9/15/2018
// Rajib Chy
using System;
using System.Linq;
using System.Threading;
namespace Sow.Framework.Security;
using Microsoft.Web.Administration;
using System.Security.Cryptography.X509Certificates;
public class IISWrapper : IIISWrapper {
    public IISWrapper( ) { }
    public bool ExistsCertificate( X509Store store, string serialNumber, ILogger logger, bool remove = false, bool validOnly = false ) {
        var certificates = store.Certificates.Find( X509FindType.FindBySerialNumber, serialNumber, validOnly );
        if ( !remove ) {
            if ( certificates != null && certificates.Count > 0 ) return true;
            return false;
        }
        if ( certificates == null ) return false;
        if ( certificates.Count <= 0 ) return false;
        foreach ( var cert in certificates ) {
            if ( cert.SerialNumber == serialNumber ) {
                store.Remove( cert ); break;
            }
        }
        return true;
    }
    public bool InstallCertificate( X509Certificate2 certificate, ILogger logger, string zoneName, StoreName storeName, X509Certificate2 oldCertificate = null ) {
        try {
            logger.Write( "Installing Certificate for {0}; Cert Serial:: {1}", zoneName, certificate.SerialNumber );
            using ( X509Store store = new X509Store( storeName, StoreLocation.LocalMachine ) ) {
                store.Open( OpenFlags.ReadWrite );
                string serial = string.Empty;
                if ( oldCertificate != null ) {
                    serial = oldCertificate.SerialNumber;
                    if ( this.ExistsCertificate( store: store, serialNumber: oldCertificate.SerialNumber, remove: true, logger: logger ) ) {
                        logger.Write( "Removed old Certificate for {0}; Cert Serial:: {1}", zoneName, serial );
                    }
                }
                serial = certificate.SerialNumber;
                if ( this.ExistsCertificate( store: store, serialNumber: certificate.SerialNumber, remove: false, logger: logger ) ) {
                    store.Close( );
                    logger.Write( "Certificate already exists for {0}; Cert Serial:: {1}", zoneName, serial );
                } else {
                    store.Add( certificate );
                    store.Close( );
                    logger.Write( "Certificate installed for {0}; Cert Serial:: {1}", zoneName, serial );
                }

            }
            return true;
        } catch ( Exception e ) {
            logger.Write( "Error occured while Installing Certificate to Local Machine for {0} :: error==>{1}", zoneName, e.Message );
            logger.Write( e.StackTrace );
            return false;
        }

    }
    public IIISWrapperResponse BindCertificate( IISWrapperSettings AppSettings, ILogger logger, CancellationToken token ) {
        try {
            bool found = false;
            logger.Write( string.Format( "Opening ServerManager and find the host {0}", AppSettings.ZoneName ) );
            using ( ServerManager iisServerManager = new ServerManager( ) ) {
                foreach ( Site site in iisServerManager.Sites ) {
                    if ( AppSettings.Site.Count( a => a == site.Name ) <= 0 ) continue;
                    if ( site == null ) continue;
                    if ( site.Name == "Default Web Site" ) continue;
                    logger.Write( string.Format( "Working for Site {0}", site.Name ) );
                    if ( site.Bindings == null ) continue;
                    foreach ( Binding binding in site.Bindings ) {
                        if ( binding.Protocol != "https" ) continue;
                        if ( binding.Host.IndexOf( AppSettings.ZoneName ) < 0 ) {
                            break;
                        }
                        logger.Write( "Updating Existing https binding for host {0}", binding.Host );
                        found = true;
                        binding.CertificateHash = AppSettings.CertificateHash;
                        binding.CertificateStoreName = AppSettings.CertificateStoreName;
                        binding.BindingInformation = string.Format( "{0}:{1}:{2}", "*", binding.EndPoint.Port, binding.Host );
                        //binding.SetAttributeValue( attr.Name, attr.Value );
                    }
                }
                if ( found ) {
                    logger.Write( "Commit Changes to IIS for {0}", AppSettings.ZoneName );
                    try {
                        iisServerManager.CommitChanges( );
                        if ( WaitHandler.Wait( 1000, token ) ) new IISWrapperResponse { Error = false, ErrorDescription = "Task canclled" };
                    } catch ( Exception x ) {
                        logger.Write( "Error in iisServerManager.CommitChanges ==> {0}", x.Message );
                        return new IISWrapperResponse { Error = true, ErrorDescription = string.Format( "Error in iisServerManager.CommitChanges ==> {0}", x.Message ) };
                    }
                    if ( AppSettings.AppPool != null ) {
                        logger.Write( "Start Recycleing AppPool(s)" );
                        AppSettings.AppPool.Select( a => {
                            try {
                                ApplicationPool applicationPool = iisServerManager.ApplicationPools.FirstOrDefault( ( p ) => p.Name == a ? true : false );
                                if ( applicationPool != null ) {
                                    try {
                                        if ( applicationPool.State != ObjectState.Started ) return a;
                                    } catch {
                                        return a;
                                    }
                                    logger.Write( "Recycleing AppPool {0}", a );
                                    applicationPool.Recycle( );
                                    logger.Write( "Completed Recycleing for AppPool {0}", a );
                                }
                            } catch ( Exception e ) {
                                logger.Write( "Error in while working in App Pool ==> {0}", e.Message );
                            }
                            return a;
                        } ).ToList( );
                        logger.Write( "End Recycleing AppPool(s)" );
                    }
                } else {
                    logger.Write( string.Format( "No such host match with {0} in ServerManager.", AppSettings.ZoneName ) );
                }

            }
            return new IISWrapperResponse { Error = false };
        } catch ( Exception e ) {
            logger.Write( "Error occured while Binding Certificate to IIS for to {0} :: Error==>{1} Trace=>{2}", AppSettings.ZoneName, e.Message, e.StackTrace );
            logger.Write( e.StackTrace );
            return new IISWrapperResponse { Error = true, ErrorDescription = e.Message };
        }

    }

    public void Dispose( ) {
        GC.SuppressFinalize( this );
        GC.Collect( 0, GCCollectionMode.Optimized );
    }
}
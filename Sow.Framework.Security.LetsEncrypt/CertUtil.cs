/**
* Copyright (c) 2018, Sow ( https://safeonline.world, https://www.facebook.com/safeonlineworld). (https://github.com/RKTUXYN) All rights reserved.
* Copyrights licensed under the New BSD License.
* See the accompanying LICENSE file for terms.
*/
// 8:29 PM 5/21/2022
// Rajib Chy
using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Sow.Framework.Files;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
namespace Sow.Framework.Security.LetsEncrypt;
public interface ICertSvcConfig {
    string Email { get; set; }
    string ZoneName { get; set; }
    string Password { get; set; }
    string ConfigKey { get; set; }
}
public class CertSvcConfig : ICertSvcConfig {
    [JsonIgnore]
    public string Dir { get; set; }
    public string Email { get; set; }
    public string ZoneName { get; set; }
    public string Password { get; set; }
    public string ConfigKey { get; set; }
    [JsonIgnore]
    public DateTime NotAfter { get; set; }
    [JsonIgnore]
    public bool IsValidConfig { get; set; }
}
public interface ICertSvcWorker {
    ICertSvcConfig Validate( );
    void UpdateExpiration( );
}
public class CertSvcWorker : ICertSvcWorker {
    private readonly ILogger _logger;
    private readonly List<CertSvcConfig> _svcConfigs;
    public CertSvcWorker( ILogger logger ) {
        _logger = logger;
        _svcConfigs = LoadConfig( );
        UpdateConfig( );
    }
    private void UpdateConfig( ) {
        if ( _svcConfigs == null )
            throw new NullReferenceException( "Service cofing not found" );
        foreach ( CertSvcConfig config in _svcConfigs ) {
            if ( string.IsNullOrEmpty( config.ZoneName ) ) {
                throw new NullReferenceException( "Zone should not null." );
            }
            if ( string.IsNullOrEmpty( config.Email ) ) {
                throw new NullReferenceException( $"Cert email not found for {config.ZoneName}." );
            }
            if ( string.IsNullOrEmpty( config.ConfigKey ) ) {
                throw new NullReferenceException( $"No config key found for {config.ZoneName}." );
            }
            string rdir = Util.RegisterNewDirectory( config.Email );
            string domainDir = Util.RegisterNewDirectory( config.ZoneName, rdir );
            config.Dir = Path.Combine( domainDir, @"/cert/" );
            if ( string.IsNullOrEmpty( config.Password ) ) {
                config.Password = Util.DefaultCertPassword;
            }
        }
        UpdateExpiration( );
    }
    public void UpdateExpiration( ) => _svcConfigs.ForEach( UpdateExpiration );
    private static List<CertSvcConfig> LoadConfig( ) {
        string physicalPath = Path.Combine( App.Dir, "svc.config.json" );
        if ( string.IsNullOrEmpty( physicalPath ) )
            throw new FileNotFoundException( $"Service cofing not found in {physicalPath}" );
        string data = File.ReadAllText( physicalPath, Encoding.UTF8 );
        if ( string.IsNullOrEmpty( data ) )
            throw new Exception( string.Format( "No data found in {0}", physicalPath ) );
        return JsonConvert.DeserializeObject<List<CertSvcConfig>>( data, Util.jsonSettings );
    }
    public ICertSvcConfig Validate( ) {
        foreach ( var certConfig in _svcConfigs ) {
            if ( !certConfig.IsValidConfig ) continue;
            if ( DateTime.Compare( certConfig.NotAfter, DateTime.Now ) < 0 ) return certConfig;
        }
        return null;
    }
    private void UpdateExpiration( CertSvcConfig certConfig ) {
        try {
            string certPath = Path.Combine( certConfig.Dir, $"{certConfig.ZoneName}.pfx" );
            byte[] pfx = FileWorker.ReadAllByte( certPath );
            if ( pfx == null ) {
                _logger.Write( $"No cert found in {certPath}" );
                certConfig.IsValidConfig = false;
                return;
            }
            certConfig.IsValidConfig = true;
            X509Certificate2 cert = GetX509Certificate2( pfx, certConfig.Password );
            certConfig.NotAfter = cert.NotAfter.Subtract( TimeSpan.FromDays( 1 ) );
            return;
        } catch (Exception ex) {
            _logger.Write( ex );
            return;
        }
    }
    public static ILogger CreateLogger( string logName ) {
        ILogger logger = new Logger( isMs: true, write_cache: true );
        if ( App.IsWindows ) {
            logger.Open( Path.Combine( App.Dir, @$"{App.Dir}\log\{Logger.LogDate}_{logName}.log" ) );
        } else {
            logger.Open( Path.Combine( App.Dir, @$"{App.Dir}/log/{Logger.LogDate}_{logName}.log" ) );
        }
        return logger;
    }
    private static X509Certificate2 GetX509Certificate2( byte[] pfx, string password ) => new X509Certificate2( pfx, password, X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet );
}
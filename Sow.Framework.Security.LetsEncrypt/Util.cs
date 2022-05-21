//8:32 PM 9/15/2018 Rajib
using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;
namespace Sow.Framework.Security.LetsEncrypt;
sealed class Util {
    public static readonly JsonSerializerSettings jsonSettings = new JsonSerializerSettings {
        NullValueHandling = NullValueHandling.Ignore,
        Formatting = Formatting.Indented
    };
    private static IGAppConfig _gConfig = null;
    public static IGAppConfig GConfig => _gConfig;
    public static string ConfigDir => "app-store";
    public static string DefaultCertPassword => "sow_le_pkey";
    public static void Load( string physicalPath, string configKey ) {
        if ( !File.Exists( physicalPath ) )
            throw new Exception( string.Format( "No Settings file found in {0}", physicalPath ) );
        string data = File.ReadAllText( physicalPath, Encoding.UTF8 );
        if ( string.IsNullOrEmpty( data ) )
            throw new Exception( string.Format( "No data found in {0}", physicalPath ) );
        try {
            _gConfig = JsonConvert.DeserializeObject<GAppConfig>( data, jsonSettings );
            _gConfig.Config.ForEach( a => {
                a.Domain.ForEach( d => {
                    if ( d.AppSettings.WebServerName == "NGINX" ) {
                        d.AppSettings.WebServer = WebServerEnum.NGINX;
                    } else if ( d.AppSettings.WebServerName == "IIS" ) {
                        d.AppSettings.WebServer = WebServerEnum.IIS;
                    } else {
                        d.AppSettings.WebServer = WebServerEnum.NONE;
                    }
                } );
            } );
            if ( string.IsNullOrEmpty( _gConfig.CertPassword ) ) {
                _gConfig.CertPassword = DefaultCertPassword;
            }
            _gConfig.ConfigKey = configKey;
        } catch ( Exception e ) {
            Console.WriteLine( e.Message );
        }
    }
    public static string RegisterNewDirectory( string threshold, string dir = null ) {
        if ( string.IsNullOrEmpty( dir ) )
            dir = $@"{App.Dir}/{ConfigDir}/info/";
        if ( !Directory.Exists( dir ) ) {
            Directory.CreateDirectory( dir );
        }
        threshold = threshold.Replace( "@", "_" ).Replace( ".", "_" ).Replace( "*", "_" );
        dir = $@"{dir}{threshold}/";
        if ( !Directory.Exists( dir ) ) {
            Directory.CreateDirectory( dir );
        }
        return dir;
    }
}
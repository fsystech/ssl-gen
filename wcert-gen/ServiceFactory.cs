/**
* Copyright (c) 2018, Sow ( https://safeonline.world, https://www.facebook.com/safeonlineworld). (https://github.com/RKTUXYN) All rights reserved.
* Copyrights licensed under the New BSD License.
* See the accompanying LICENSE file for terms.
*/
// 12:15 PM 5/12/2022
// Rajib Chy
using System;
using System.IO;
using Sow.Framework;
using Newtonsoft.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Hosting;
using Sow.Framework.Security.LetsEncrypt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting.WindowsServices;
namespace Sow.WCartGen;
/// <summary>Commonly used environment names.</summary>
public static class EnvironmentName {
    public static readonly string Development = "Development";
    public static readonly string Staging = "Staging";
    public static readonly string Production = "Production";
}
public class ServiceFactory : BackgroundService {
    private ILogger _logger;
    private CancellationToken _token;
    private ICartGenerator _wCartGen = null;
    private bool _isFirstTime = true;

    private System.Timers.Timer _timer = new( );
    private readonly ICertSvcWorker _svcWorker = null;
    private DateTime _lastCheck = DateTime.Now;

    public ServiceFactory( ) : base( ) {
        _logger = CertSvcWorker.CreateLogger( "svc" );
        _logger.Write( $"Starting {App.Name}" );
        try {
            _svcWorker = new CertSvcWorker( _logger );
        } catch ( Exception ex ) {
            _logger.Write( ex );
            StopApp( );
            Environment.Exit( -1 );
        }
        _logger.FlushMemory( );
    }
    private void StopApp( ) {
        ThreadSafe.Exchange( ref _timer, ThreadSafe.Dispose );
        ThreadSafe.Exchange( ref _wCartGen, ThreadSafe.Dispose );
        _logger.Write( $"Exiting {App.Name}" );
        ThreadSafe.Exchange( ref _logger, ThreadSafe.Dispose );
    }
    public override async Task StartAsync( CancellationToken cancellationToken ) {
        _token = cancellationToken;
        _timer.Elapsed += new System.Timers.ElapsedEventHandler( OnElapsedTime );
        _timer.Interval = 60000 * 20; //number in milisecinds  
        _timer.Enabled = true;
        await base.StartAsync( cancellationToken );
    }
    private void OnElapsedTime( object source, System.Timers.ElapsedEventArgs e ) {
        if ( _wCartGen != null ) {
            if ( !_wCartGen.IsExited ) return;
            _svcWorker.UpdateExpiration( );
            _logger.FlushMemory( );
            ThreadSafe.Exchange( ref _wCartGen, ThreadSafe.Dispose );
        } else {
            if( !_isFirstTime ) {
                if ( _lastCheck.Day == DateTime.Now.Day ) {
                    _logger.FlushMemory( );
                    return;
                }
            } else {
                _isFirstTime = false;
            }
            _logger.Write( "Checking certificate expiration.." );
            _lastCheck = DateTime.Now;
        }
        ICertSvcConfig certConfig = _svcWorker.Validate( );
        if ( certConfig == null ) return;
        Arguments args = new Arguments {
            ForceRenew = true,
            Email = certConfig.Email,
            Web = certConfig.ZoneName,
            ConfigKey = certConfig.ConfigKey,
            AcmeApiServer = AcmeWrapper.DefaultAcmeApiServer
        };
        if ( Environment.OSVersion.Platform == PlatformID.Unix ) {
            _wCartGen = new Unix( args, true, _token );
        } else {
            _wCartGen = new Win( args, true, _token );
        }
        _wCartGen.Start( );
        _logger.FlushMemory( );
    }
    public override async Task StopAsync( CancellationToken stoppingToken ) {
        StopApp( );
        await base.StopAsync( stoppingToken );
    }
    protected override Task ExecuteAsync( CancellationToken stoppingToken ) => Task.CompletedTask;
    #region Static Member
    private static void UseEnvironment( IHostBuilder appBuilder ) {
        var envSettingsPath = Path.Combine( App.Root, "env.json" );
        bool ok = false;
        if ( File.Exists( envSettingsPath ) ) {
            Dictionary<string, object> envSettings = JsonConvert.DeserializeObject<Dictionary<string, object>>( File.ReadAllText( envSettingsPath ) );
            string enviromentValue = envSettings[ "APP_ENVIRONMENT" ].ToString( );
            if ( !string.IsNullOrEmpty( enviromentValue ) ) {
                appBuilder.UseEnvironment( enviromentValue );
                ok = true;
            }
        }
        if ( !ok ) {
            appBuilder.UseEnvironment( EnvironmentName.Development );
        }
        return;
    }
    public static Arguments BuildApp( string[] args ) {
        App.Name = "LetsEncrypt SSL Cert Generator";
        App.UserInteractive = WindowsServiceHelpers.IsWindowsService( ) == false;
        if( App.UserInteractive ) return Arguments.Parse( args );
        IHostBuilder appBuilder = Host.CreateDefaultBuilder( args ).UseWindowsService( options => {
            options.ServiceName = "ssl-gen";
        } ).ConfigureServices( ( hostContext, services ) => {
            services.AddHostedService<ServiceFactory>( );
        } );
        UseEnvironment( appBuilder );
        appBuilder.Build( ).Run( );
        return null;
    }
    #endregion Static Member
}
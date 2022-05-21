/**
* Copyright (c) 2018, Sow ( https://safeonline.world, https://www.facebook.com/safeonlineworld). (https://github.com/RKTUXYN) All rights reserved.
* Copyrights licensed under the New BSD License.
* See the accompanying LICENSE file for terms.
*/
// 10:51 AM 5/21/2022
// Rajib Chy
using System;
using Sow.Framework;
using System.Threading;
using Sow.Framework.Security.LetsEncrypt;
namespace Sow.WCartGen {
    internal class WCartGenBase {
        private Thread _th;
        protected readonly IConfig _config;
        protected readonly ILogger _logger;
        protected readonly Arguments _arguments;
        protected readonly CancellationTokenSource _tokenSource;
        public WCartGenBase( Arguments arguments ) {
            _arguments = arguments; _tokenSource = new CancellationTokenSource();
            _config = AcmeWrapper.GetConfig( email: _arguments.email, dir: App.Dir );
            _logger = CreateLogger( );
        }
        protected virtual void StartWork( object state ) { }
        public bool Wait( int millisecondsDelay ) => WaitHandler.Wait( millisecondsDelay, _tokenSource.Token );
        public void Exit( ) {
            ThreadSafe.DisposeToken( _tokenSource );
            ThreadSafe.ExitThread( _th );
            _logger.Write( "Program being closed!" );
            _logger.Close( );
            Environment.Exit( Environment.ExitCode );
        }
        public bool IsCancellationRequested => _tokenSource.IsCancellationRequested;
        public bool Start( ) {
            if ( _config == null ) {
                _logger.Write( string.Format( "No config found for email==>{0}", _arguments.email ) );
                Exit( );
                return false;
            }
            _th = new Thread( new ParameterizedThreadStart( StartWork ) );
            _th.Start( );
            return true;
        }
        public static ILogger CreateLogger( ) {
            ILogger logger = new Logger( isMs: true, write_cache: true );
            if ( App.IsWindows ) {
                logger.Open( string.Format( @"{0}\log\{1}.log", App.Dir, DateTime.Now.ToString( "yyyy'-'MM'-'dd" ) ) );
            } else {
                logger.Open( string.Format( @"{0}/log/{1}.log", App.Dir, DateTime.Now.ToString( "yyyy'-'MM'-'dd" ) ) );
            }
            logger.Write( "------------------------------" );
            logger.Write( "Opening..." );
            return logger;
        }
    }
}

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
    internal interface ICartGenerator : IDisposable {
        bool IsExited { get; }
        bool Start( );
        void Stop( );
        bool Wait( int millisecondsDelay );
        bool IsCancellationRequested { get; }
    }
    internal class WCartGenBase : ICartGenerator {
        private Thread _th;
        private long _isExited = 0;
        protected readonly ILogger _logger;
        protected readonly IAppConfig _config;
        protected readonly Arguments _arguments;
        private readonly bool _isService = false;
        public bool IsExited => Interlocked.Read( ref _isExited ) != 0;
        protected readonly CancellationTokenSource _tokenSource;
        public WCartGenBase( Arguments arguments, bool isService, CancellationToken token ) {
            _arguments = arguments; _tokenSource = CancellationTokenSource.CreateLinkedTokenSource( token );
            _config = AcmeWrapper.GetConfig( email: _arguments.Email, configKey: arguments.ConfigKey );
            _logger = CertSvcWorker.CreateLogger( "gen" ); _isService = isService;
        }
        protected virtual void StartWork( object state ) { }
        public bool Wait( int millisecondsDelay ) => WaitHandler.Wait( millisecondsDelay, _tokenSource.Token );
        public void Stop( ) => Dispose( );
        public void Dispose( ) {
            ThreadSafe.DisposeToken( _tokenSource );
            ThreadSafe.ExitThread( _th );
            _logger.Write( $"Stoping {App.Name}!" );
            _logger.Close( );
            GC.SuppressFinalize( this );
            if ( !_isService ) {
                Environment.Exit( Environment.ExitCode );
            }
        }
        public bool IsCancellationRequested => _tokenSource.IsCancellationRequested;
        public bool Start( ) {
            _logger.Write( $"Starting {App.Name}" );
            if ( _config == null ) {
                _logger.Write( string.Format( "No config found for email==>{0}", _arguments.Email ) );
                Stop( );
                return false;
            }
            _th = new Thread( new ParameterizedThreadStart( StartWork ) );
            _th.Start( );
            return true;
        }
    }
}

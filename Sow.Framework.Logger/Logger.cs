/**
* Copyright (c) 2018, Sow ( https://safeonline.world, https://www.facebook.com/safeonlineworld). (https://github.com/RKTUXYN) All rights reserved.
* Copyrights licensed under the New BSD License.
* See the accompanying LICENSE file for terms.
*/
// 7:50 PM 9/15/2018
// Rajib Chy
using System;
using System.IO;
using System.Text;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Sow.Framework;
public class Logger : ILogger {
    bool _opening { get; set; }
    bool _isCach { get; set; }
    StringBuilder _cache { get; set; }
    bool _isMs { get; set; }
    MemoryStream _ms { get; set; }
    Stream _fs { get; set; }
    string _physicalPath { get; set; }
    bool _closed = false;
    bool _disposed { get; set; }
    string _dir { get; set; }
    bool _iSUserInteractive { get; set; }
    bool _isHtml { get; set; }
    long _line_count = 0;
    bool IsEmpty => Interlocked.Read( ref _line_count ) == 0;

    bool _write_cache { get; set; }
    const int MAX_LINE = 100;
    public FileMode fileMode { get; private set; }
    public Logger( bool isMs = true, bool isHtml = false, bool write_cache = true ) {
        _isCach = false; _write_cache = write_cache;
        _cache = new StringBuilder( );
        _iSUserInteractive = Environment.UserInteractive;
        _isMs = isMs; _disposed = false;
        _isHtml = isHtml;
        if ( _write_cache == false ) {
            _isMs = false;
        }
    }
    public byte[] GetCurLog( ) {
        if ( _ms == null || _ms.CanRead == false ) return null;
        return _ms.ToArray( );
    }
    public MemoryStream GetLogStream( ) => _ms;
    private void WriteTofile( bool reOpen = false ) {
        _ = Interlocked.Exchange( ref _line_count, 0 );
        try {
            FileStream fs;
            if ( File.Exists( _physicalPath ) ) {
                fs = new FileStream( _physicalPath, FileMode.Append, FileAccess.Write, FileShare.Read );
            } else {
                fs = new FileStream( _physicalPath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.ReadWrite );
                byte[] buffer = Encoding.UTF8.GetBytes( $"Log Genarte On {DateTime.Now.ToString( )}\r\n{new String( '-', 67 )}\r\n" );
                fs.Write( buffer, 0, buffer.Length );
            }
            _ms.WriteTo( fs );
            fs.Flush( true ); fs.Close( ); fs.Dispose( ); fs = null;
            _ms.Close( ); _ms.Dispose( ); _ms = null;
            if ( reOpen ) {
                _ms = new MemoryStream( );
                _closed = false;
            }
        } catch ( RuntimeWrappedException e ) {
            if ( _ms == null || ( _ms != null && _ms.CanWrite == false ) ) {
                _ms = new MemoryStream( );
                _closed = false;
            }
            this.Write( Encoding.UTF8.GetBytes( $"{e.Message}\n{e.StackTrace}" ) );
            if ( _iSUserInteractive )
                Console.WriteLine( e.Message );
        }
    }
    private void OpenFile( bool newLine = true ) {
        try {
            if ( File.Exists( _physicalPath ) ) {
                _fs = new FileStream( _physicalPath, FileMode.Append, FileAccess.Write, FileShare.Read );
                fileMode = FileMode.Append;
                if ( newLine )
                    NewLine( );
            } else {
                _fs = new FileStream( _physicalPath, FileMode.CreateNew, FileAccess.Write, FileShare.Read );
                fileMode = FileMode.CreateNew;
                byte[] buffer = Encoding.UTF8.GetBytes( $"Log Genarte On {DateTime.Now.ToString( )}\r\n{new String( '-', 67 )}\r\n" );
                _fs.Write( buffer, 0, buffer.Length );
            }
            _ = Interlocked.Exchange( ref _line_count, 0 );
            _closed = false;
        } catch {
            System.Threading.Thread.Sleep( 100 );
            if ( !string.IsNullOrEmpty( _physicalPath ) )
                Open( string.Concat( _physicalPath, "__", Guid.NewGuid( ).ToString( "N" ) ) );
        }
    }
    public ILogger Open( string physicalPath = null, bool newLine = true ) {
        _opening = true;
        if ( !string.IsNullOrEmpty( physicalPath ) ) {
            _physicalPath = physicalPath;
            _dir = Path.GetDirectoryName( physicalPath );
        }
        if ( string.IsNullOrEmpty( _physicalPath ) || string.IsNullOrEmpty( _dir ) )
            throw new ArgumentNullException( "Physical path required" );

        if ( !Directory.Exists( _dir ) ) {
            Directory.CreateDirectory( _dir );
        }
        if ( _isMs )
            _ms = new MemoryStream( );
        else
            OpenFile( newLine );
        _opening = false;
        return this;
    }
    public void NewLine( ) {
        Write( Encoding.UTF8.GetBytes( $"{new String( '-', 67 )}\r\n" ) );
    }
    public void Write( params string[] data ) {
        if ( data == null ) return;
        if ( data.Length <= 0 ) return;
        Write( string.Format( data[ 0 ], data.Slice( 1, data.Length ) ) );
    }
    private string StripHTML( string htmlString ) {
        string pattern = @"<(.|\n)*?>";
        return System.Text.RegularExpressions.Regex.Replace( htmlString, pattern, string.Empty );
    }
    private string GetFormattedText( string txt, LogLevel logLevel ) {
        string level = "", style = "";
        switch ( logLevel ) {
            case LogLevel.TRACE: style = "color:white!important;"; level = "[TRACE]"; break;
            case LogLevel.INFO: style = "color:green!important;"; level = "[INFO]"; break;
            case LogLevel.DEBUG: style = "color:blue!important;"; level = "[DEBUG]"; break;
            case LogLevel.WARNING: style = "color:yellow!important;"; level = "[WARNING]"; break;
            case LogLevel.ERROR: style = "color:#ff0000!important;"; level = "[ERROR]"; break;
            case LogLevel.FATAL: style = "color:#ff0000!important;"; level = "[FATAL]"; break;
            case LogLevel.PGSQL: style = "color:#ff0000!important;"; level = "[PGSQL]"; break;
            case LogLevel.HTTP: style = "color:ff0000!important;"; level = "[HTTP]"; break;
        }
        return _isHtml == true ? $"<li style=\"{style}\">{DateTime.Now.ToString( )}\t\t\t{level}\t\t\t{StripHTML( txt )}</li>\n"
        : $"{DateTime.Now}\t\t\t{level}\t\t\t{txt}\n";
    }
    public void Write( byte[] buffer ) {
        if ( _disposed ) return;
        if ( _isMs ) {
            if ( _closed || _ms == null ) return;
            lock ( _ms ) {
                _ms.Write( buffer, 0, buffer.Length );
            }
            _ = Interlocked.Increment( ref _line_count );
            if ( _line_count > MAX_LINE ) {
                WriteTofile( reOpen: true );
            }
            return;
        }
        if ( _fs == null || _closed || ( _fs != null && !_fs.CanWrite ) ) {
            this.Open( ); _closed = false;
        }
        lock ( _fs ) {
            _fs.Write( buffer, 0, buffer.Length );
            if ( _write_cache == false ) _fs.Flush( );
        }
        _ = Interlocked.Increment( ref _line_count );
        //if ( _line_count > MAX_LINE ) this.Flush( );
    }
    private void CleanCache( ) {
        if ( !_isCach ) return;
        _isCach = false;
        if ( _cache.Length <= 0 ) return;
        Write( Encoding.UTF8.GetBytes( _cache.ToString( ) ) );
        _cache.Clear( );
    }
    private void WriteInvoke( string formatedStr ) {
        if ( _isMs ) {
            if ( _disposed ) return;
            if ( _ms == null ) {
                _isCach = true;
                _cache.Append( formatedStr );
                return;
            }
            CleanCache( );
            Write( Encoding.UTF8.GetBytes( formatedStr ) );
            return;
        }
        if ( _opening ) {
            _isCach = true;
            _cache.Append( formatedStr );
            return;
        }
        CleanCache( );
        Write( Encoding.UTF8.GetBytes( formatedStr ) );
        return;

    }
    private void _Write( string data, LogLevel logLevel ) {
        if ( _iSUserInteractive ) {
            Console.WriteLine( data );
        }
        WriteInvoke( GetFormattedText( data, logLevel ) );
    }
    public void Write<T>( T e ) where T : Exception => Write( $"{e.Message}\r\n{e.StackTrace}", LogLevel.FATAL );
    public void Write( string data, LogLevel logLevel = LogLevel.INFO ) {
        string[] lines = data.Split( new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries );
        foreach ( string line in lines ) {
            _Write( line, logLevel );
        }
    }
    public void Flush( ) {
        if ( _disposed || _closed ) return;
        if ( _isMs ) {
            NewLine( );
            WriteTofile( reOpen: true );
            return;
        }
        _fs.Flush( );
    }
    public void FlushMemory( ) {
        if ( _disposed || _closed ) return;
        if ( !_isMs ) return;
        if ( !IsEmpty ) {
            WriteTofile( reOpen: true );
        }
    }
    public void Close( ) {
        if ( _closed || _disposed ) return;
        CleanCache( ); NewLine( );
        if ( _isMs ) {
            WriteTofile( reOpen: false );
        } else {
            _fs.Flush( ); _fs = null;
        }
        _closed = true;
        _cache.Clear( );
        GC.SuppressFinalize( this );
        GC.WaitForPendingFinalizers( );
        GC.Collect( );
    }
    public void Dispose( ) {
        if ( _disposed ) return;
        this.Close( );
        _disposed = true;
    }
    public static string LogDate => DateTime.Now.ToString( "yyyy-MM-dd" ).Replace( "-", "_" );
}
public static class Extensions {
    /// <summary>
    /// Get the array slice between the two indexes.
    /// ... Inclusive for start index, exclusive for end index.
    /// </summary>
    public static T[] Slice<T>( this T[] source, int start, int end ) {
        // Handles negative ends.
        if ( end < 0 ) {
            end = source.Length + end;
        }
        int len = end - start;

        // Return new array.
        T[] res = new T[ len ];
        for ( int i = 0; i < len; i++ ) {
            res[ i ] = source[ i + start ];
        }
        return res;
    }
}
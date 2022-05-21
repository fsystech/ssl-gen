/**
* Copyright (c) 2018, Sow ( https://safeonline.world, https://www.facebook.com/safeonlineworld). (https://github.com/RKTUXYN) All rights reserved.
* Copyrights licensed under the New BSD License.
* See the accompanying LICENSE file for terms.
*/
// 7:50 PM 9/15/2018 Rajib
// Rajib Chy
namespace Sow.Framework {
    using System.IO;
    [System.Flags]
    public enum LogLevel {
        TRACE,
        INFO,
        DEBUG,
        WARNING,
        ERROR,
        FATAL,
        PGSQL,
        HTTP
    }
    public interface ILogger : System.IDisposable {
        FileMode fileMode { get; }
        byte[] GetCurLog( );
        ILogger Open( string physicalPath = null, bool newLine = true );
        MemoryStream GetLogStream( );
        void Flush( );
        void FlushMemory( );
        void NewLine( );
        void Write( byte[] buffer );
        void Write( string data, LogLevel logLevel = LogLevel.INFO );
        void Write( params string[] data );
        void Close( );
    }
}

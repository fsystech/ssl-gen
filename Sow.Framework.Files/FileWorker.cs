/**
* Copyright (c) 2018, Sow ( https://safeonline.world, https://www.facebook.com/safeonlineworld). (https://github.com/RKTUXYN) All rights reserved.
* Copyrights licensed under the New BSD License.
* See the accompanying LICENSE file for terms.
*/
// 7:50 PM 9/15/2018 Rajib
// Rajib Chy
using System;
using System.IO;
using System.Text;
using System.Runtime.Serialization.Formatters.Binary;
namespace Sow.Framework.Files {
    public class FileWorker {
        public FileWorker( ) { }
        public static void CreateDirectory( string dir ) {
            if ( !Directory.Exists( dir ) ) {
                Directory.CreateDirectory( dir );
            }
        }
        public static string GetDirectoryName( string absolute ) {
            if ( string.IsNullOrEmpty( absolute ) ) return null;
            return Path.GetDirectoryName( absolute );
        }
        public static void DeleteFile( string absolute ) {
            if ( !string.IsNullOrEmpty( absolute ) ) {
                if ( File.Exists( absolute ) ) {
                    File.Delete( absolute );
                }
            }
        }

        public static bool ExistsFile( string absolute ) {
            if ( string.IsNullOrEmpty( absolute ) )
                return false;
            return ( File.Exists( absolute ) ? true : false );
        }

        public static string Read( string absolute ) {
            string str;
            if ( File.Exists( absolute ) ) {
                str = File.ReadAllText( absolute, Encoding.UTF8 );
            } else {
                str = null;
            }
            return str;
        }

        public static byte[] ReadAllByte( string absolute ) {
            if ( File.Exists( absolute ) ) {
                return File.ReadAllBytes( absolute );
            }
            return null;
        }

        public static void WriteFile( string data, string absolute ) {
            FileWorker.WriteFile( Encoding.UTF8.GetBytes( data ), absolute );
        }

        public static void WriteFile( byte[] buffer, string absolute ) {
            string directoryName = Path.GetDirectoryName( absolute );
            if ( !Directory.Exists( directoryName ) ) {
                Directory.CreateDirectory( directoryName );
            }
            if ( File.Exists( absolute ) ) {
                File.Move( absolute, string.Concat( absolute, "__old.", Guid.NewGuid( ).ToString( "N" ) ) );
            }
            using ( FileStream fileStream = new FileStream( absolute, FileMode.CreateNew, FileAccess.ReadWrite ) ) {
                try {
                    fileStream.Write( buffer, 0, ( int )buffer.Length );
                    fileStream.Flush( true );
                } finally {
                    GC.Collect( );
                }
            }
        }
    }
}
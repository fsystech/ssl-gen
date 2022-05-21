/**
* Copyright (c) 2018, Sow ( https://safeonline.world, https://www.facebook.com/safeonlineworld). (https://github.com/RKTUXYN) All rights reserved.
* Copyrights licensed under the New BSD License.
* See the accompanying LICENSE file for terms.
*/
// 10:38 AM 5/21/2022
// Rajib Chy
using System;
using Sow.Framework;
namespace Sow.WCartGen {
    class Arguments {
        public string email { get; set; }
        public string web { get; set; }
        public bool forceRenew { get; set; }
        public string acmeApiServer { get; set; }
        public static Arguments Parse( string[] args ) {
            if ( args == null || args.Length == 0 ) {
                Console.WriteLine( "Null Argument..." );
                PrintHelp( );
                return null;
            }
            var argv = ThreadSafe.ParseArgument( args );
            if ( argv == null || argv.Count == 0 ) {
                PrintHelp( );
                return null;
            }
            return new( ) {
                web = argv.TryGetValue( "w" ),
                email = argv.TryGetValue( "e" ),
                forceRenew = argv.TryGetBool( "fr" ),
                acmeApiServer = argv.TryGetValue( "api" )
            };

        }
        public static void PrintHelp( ) {
            if ( !Environment.UserInteractive ) return;
            string newLine = new( '-', 30 );
            Console.WriteLine( newLine );
            Console.WriteLine( "-api Acme API server. e.g. LetsEncryptV2" );
            Console.WriteLine( "-e myemail@mydomain.com --> Authorization Email" );
            Console.WriteLine( "-w *.mydomain.com --> Need Generate Certificate" );
            Console.WriteLine( "-fr Y/N ---> Force Renew" );
            Console.WriteLine( newLine );
        }
    }
}

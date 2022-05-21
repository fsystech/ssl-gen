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
        public string Email { get; set; }
        public string Web { get; set; }
        public bool ForceRenew { get; set; }
        public string AcmeApiServer { get; set; }
        public string ConfigKey { get; set; }
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
                Web = argv.TryGetValue( "w" ),
                Email = argv.TryGetValue( "e" ),
                ConfigKey = argv.TryGetValue( "c" ) ?? "test",
                ForceRenew = argv.TryGetValue( "fr" )?.ToLower( ) == "y",
                AcmeApiServer = argv.TryGetValue( "api" )
            };
        }
        public static void PrintHelp( ) {
            if ( !Environment.UserInteractive ) return;
            string newLine = new( '-', 30 );
            Console.WriteLine( newLine );
            Console.WriteLine( "-api Acme API server. e.g. LetsEncryptV2" );
            Console.WriteLine( "-c App configuration key. e.g. test" );
            Console.WriteLine( "-e myemail@mydomain.com --> Authorization Email" );
            Console.WriteLine( "-w *.mydomain.com --> Need Generate Certificate" );
            Console.WriteLine( "-fr y/n ---> Force Renew" );
            Console.WriteLine( newLine );
        }
    }
}

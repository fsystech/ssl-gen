/**
* Copyright (c) 2018, Sow ( https://safeonline.world, https://www.facebook.com/safeonlineworld). (https://github.com/RKTUXYN) All rights reserved.
* Copyrights licensed under the New BSD License.
* See the accompanying LICENSE file for terms.
*/
// 7:50 PM 9/15/2018
// Rajib Chy
using System;
using System.Runtime.InteropServices;
namespace Sow.WCartGen;
class Program {
    static WCartGenBase _wCartGen;
    private static bool ConsoleCtrlCheck( CtrlTypes ctrlType ) {
        if ( _wCartGen == null ) {
            Console.WriteLine( "Program being closed!" );
            Environment.Exit( Environment.ExitCode );
        } else {
            _wCartGen.Exit( );
        }
        return true;
    }
    public static void Exit( ) {
        ConsoleCtrlCheck( CtrlTypes.EVENT_EXIT );
    }
    static void Main( string[] args ) {
        Arguments arguments = Arguments.Parse( args );
        if ( arguments == null ) {
            Exit( );
            return;
        }
        if ( string.IsNullOrEmpty( arguments.email ) || string.IsNullOrEmpty( arguments.web ) ) {
            Arguments.PrintHelp( );
            Exit( );
            return;
        }
        if ( Environment.OSVersion.Platform == PlatformID.Unix ) {
            _wCartGen = new Unix( arguments );
        } else {
            _wCartGen = new Win( arguments );
            if ( Environment.UserInteractive ) {
                SetConsoleCtrlHandler( new HandlerRoutine( ConsoleCtrlCheck ), true );
                Console.WriteLine( "CTRL+C,CTRL+BREAK or suppress the application to exit" );
            }
        }
        if ( !_wCartGen.Start( ) ) return;
        do {
            if ( _wCartGen.Wait( 1000 ) ) break;
        } while ( !_wCartGen.IsCancellationRequested );
        _wCartGen.Exit( );
        return;
    }
    #region unmanaged
    // Declare the SetConsoleCtrlHandler function
    // as external and receiving a delegate.

    [DllImport( "Kernel32" )]
    public static extern bool SetConsoleCtrlHandler( HandlerRoutine Handler, bool Add );

    // A delegate type to be used as the handler routine
    // for SetConsoleCtrlHandler.
    public delegate bool HandlerRoutine( CtrlTypes CtrlType );

    // An enumerated type for the control messages
    // sent to the handler routine.

    public enum CtrlTypes {
        EVENT_EXIT = -1,
        CTRL_C_EVENT = 0,
        CTRL_BREAK_EVENT,
        CTRL_CLOSE_EVENT,
        CTRL_LOGOFF_EVENT = 5,
        CTRL_SHUTDOWN_EVENT
    }

    #endregion
}

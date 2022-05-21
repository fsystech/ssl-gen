/// <![CDATA[copyright]]>
/// Copyright (c) 2018, Sow ( https://safeonline.world, https://www.facebook.com/safeonlineworld). (https://github.com/RKTUXYN) All rights reserved.
/// Copyrights licensed under the New BSD License.
/// See the accompanying LICENSE file for terms.
/// <![CDATA[copyright]]>
/// <![CDATA[Author]]>
/// By Rajib Chy
/// On 6/7/2021 3:52:48 PM
/// <![CDATA[Author]]>
using System;
using System.IO;
using System.Reflection;
using System.Diagnostics;
namespace Sow.Framework {
    public static class App {
        private static string _dir;
        private static string _name;
        private static string _location;
        private static string _fileName;
        private static string _wdir;
        private static int _id = -1;
        private static string _key;
        private static bool _isSilent = false;
        public static bool _logging = true;
        public static bool _isEncrypted = true;
        public static bool IsSilent {
            get => _isSilent;
            set => _isSilent = value;
        }
        public static bool Logging {
            get => _logging;
            set => _logging = value;
        }
        public static bool IsEncrypted {
            get => _isEncrypted;
            set => _isEncrypted = value;
        }
        public static bool IsWindows => Environment.OSVersion.Platform == PlatformID.Win32NT;
        /// <summary>
        ///  Gets the unique identifier for the associated <see cref="T:System.Diagnostics.Process"/>.
        /// </summary>
        /// <returns>
        /// The system-generated unique identifier of the <see cref="T:System.Diagnostics.Process"/> that is referenced by this
        /// <see cref="T:System.Diagnostics.Process"/> instance
        /// </returns>
        /// <exception cref="T:System.PlatformNotSupportedException">
        /// The process's <see cref="T:System.Diagnostics.Process.Id"/> property has not been set.-or- There
        /// is no process associated with this <see cref="T:System.Diagnostics.Process"/> <see cref="T:Object"/>.
        /// </exception>
        /// <exception cref="T:System.PlatformNotSupportedException">
        /// The platform is Windows 98 or Windows Millennium Edition (Windows Me); set the
        /// <see cref="T:System.Diagnostics.ProcessStartInfo.UseShellExecute"/> property to false to access
        /// this property on Windows 98 and Windows Me.
        /// </exception>
        public static int Id {
            get {
                if ( _id < 0 ) {
                    _id = Process.GetCurrentProcess( ).Id;
                }
                return _id;
            }
        }
        /// <summary>
        /// Gets the current working <see cref="T:System.IO.Directory"/> of the application.
        /// </summary>
        /// <returns>
        /// A <see cref="string"/> that contains the path of the current working <see cref="T:System.IO.Directory"/>, and does not end with a backslash (\).
        /// </returns>
        /// <exception cref="T:System.UnauthorizedAccessException">
        ///  The caller does not have the required permission.
        /// </exception>
        /// <exception cref="T:System.NotSupportedException">
        /// The operating system is Windows CE, which does not have current <see cref="T:System.IO.Directory"/> functionality.This
        /// method is available in the .NET Compact Framework, but is not currently supported.
        /// </exception>
        public static string CurrentDirectory {
            get {
                if ( string.IsNullOrEmpty( _wdir ) ) {
                    _wdir = Directory.GetCurrentDirectory( );
                }
                return _wdir;
            }
        }
        /// <summary>
        /// <see cref="T:System.Reflection.Assembly"/> <see cref="T:System.IO.Directory"/>
        /// </summary>
        /// <exception cref="T:System.NotSupportedException">
        /// We are unable to determine <see cref="T:System.Reflection.Assembly"/> location.
        /// </exception>
        public static string Dir {
            get {
                if ( string.IsNullOrEmpty( _dir ) ) {
                    if ( string.IsNullOrEmpty( Location ) ) {
                        throw new NotSupportedException( "We are unable to determine assembly location." );
                    }
                    _dir = Path.GetDirectoryName( Location );
                }
                return _dir;
            }
        }
        /// <summary>
        /// <see cref="App"/> Name. Default <see cref="T:System.Reflection.Assembly"/> Name
        /// </summary>
        public static string Name {
            get {

                return _name ?? FileName;
            }
            set {
                _name = value;
            }
        }
        /// <summary>
        /// <see cref="App"/> unique <see cref="string"/> key
        /// <para>
        /// If we like to keep all associated application in one folder
        /// </para>
        /// </summary>
        public static string Key {
            get {

                return _key;
            }
            set {
                _key = value;
            }
        }
        /// <summary>
        /// <see cref="T:System.Reflection.Assembly"/> location
        /// </summary>
        /// <returns>The <see cref="T:System.Reflection.Assembly"/> that contains the code that is currently executing.</returns>
        public static string Location {
            get {
                if ( !string.IsNullOrEmpty( _location ) ) return _location;
                _location = Assembly.GetExecutingAssembly( ).Location;
                if ( !string.IsNullOrEmpty( _location ) ) return _location;
                _location = Environment.ProcessPath;//Process.GetCurrentProcess( ).MainModule.FileName;
                if ( !string.IsNullOrEmpty( _location ) ) return _location;
                _location = Assembly.GetEntryAssembly( ).Location;
                if ( !string.IsNullOrEmpty( _location ) ) return _location;
                return _location;
            }
        }
        /// <summary>
        /// <see cref="T:System.Reflection.Assembly"/> <see cref="T:System.IO.Directory"/>
        /// </summary>
        public static string Root {
            get => Dir;
        }
        /// <summary>
        /// Retrieves the parent <see cref="T:System.IO.Directory"/> of the current <see cref="T:System.Reflection.Assembly"/>
        /// </summary>
        /// <returns>
        /// The parent <see cref="T:System.IO.Directory"/>, or null if path is the root <see cref="T:System.IO.Directory"/>, including the root
        /// of a UNC server or share name.
        /// </returns>
        public static string GetParentDirectory( ) {
            return Directory.GetParent( Dir ).FullName;
        }
        /// <summary>
        /// <see cref="T:System.Reflection.Assembly"/> Name
        /// </summary>
        public static string FileName {
            get {
                if ( !string.IsNullOrEmpty( _fileName ) ) return _fileName;
                _fileName = Path.GetFileName( Location );
                return _fileName;
            }
        }
        /// <summary>
        /// Add <see cref="App.Key"/> begaining of given (<paramref name="pathStr"/>)
        /// <see cref="T:System.IO.Path"/> <see cref="T:System.String"/>
        /// </summary>
        /// <param name="str"></param>
        /// <returns>Extended <see cref="T:System.IO.Path"/> <see cref="T:System.String"/></returns>
        public static string ExtendKey( string pathStr ) {
            if ( string.IsNullOrEmpty( pathStr ) ) return pathStr;
            if ( string.IsNullOrEmpty( Key ) ) return pathStr;
            if ( pathStr[ 0 ] == '\\' ) {
                return $@"\{Key}{pathStr}";
            }
            return $@"{Key}\{pathStr}";
        }
        private static bool _userInteractive = Environment.UserInteractive;
        /// <summary>
        /// Gets a value indicating whether the current <see cref="T:System.Diagnostics.Process"/> is running in {<see cref="T:System.Environment"/>} interactive mode.
        /// </summary>
        /// <returns>
        /// true If the current <see cref="T:System.Diagnostics.Process"/> is running {<see cref="T:System.Environment"/>} interactive mode; otherwise, false.
        /// </returns>
        public static bool UserInteractive {
            get => _userInteractive;
            set => _userInteractive = value;
        }
        /// <summary>
        /// Skip current <see cref="T:System.Diagnostics.Process"/>, kill all open <see cref="T:System.Diagnostics.Process"/> by this <see cref="T:System.Reflection.Assembly"/> Name.
        /// </summary>
        public static void KillMyShadow( ) => ProcHelp.KillByProcessName( App.FileName, App.Id );
    }
}

/**
* Copyright (c) 2018, Sow ( https://safeonline.world, https://www.facebook.com/safeonlineworld). (https://github.com/RKTUXYN) All rights reserved.
* Copyrights licensed under the New BSD License.
* See the accompanying LICENSE file for terms.
*/
// 6/3/2021 10:43:14 PM
// Rajib Chy
using System;
using System.Linq;
using System.Threading;
using System.Collections.Generic;
namespace Sow.Framework;
public class ThreadSafe {
    public static Dictionary<string, string> ParseArgument( string[] args, int start = 0 ) {
        Dictionary<string, string> result = new Dictionary<string, string>( );
        for ( int i = start, l = args.Length; i < l; i += 2 ) {
            string param = args[ i ];
            if ( string.IsNullOrEmpty( param ) ) continue;
            if ( param[ 0 ] != '-' ) continue;
            string key = param.Substring( 1, param.Length - 1 );
            if ( i + 1 >= l ) continue;
            string value = args[ i + 1 ];
            result.Add( key, value );
        }
        return result;
    }
    public static void ToEmpty<T>( ref List<T> data ) {
        if ( data == null ) return;
        data.Clear( );
        Exchange( ref data );
    }
    public static void ToEmpty<T>( ref IList<T> data ) {
        if ( data == null ) return;
        data.Clear( );
        Exchange( ref data );
    }
    public static void DisposeToken( CancellationTokenSource source ) {
        try {
            if ( source == null ) return;
            if ( !source.IsCancellationRequested ) {
                source.Cancel( true );
            }
            source.Dispose( );
        } catch ( ObjectDisposedException ) {
            // We can get this exception in .Net 4.0. In this case, the cancellation did occur and the cts is already disposed
        }
    }
    public static void TryWrape( Action next, Action<Exception> onError = null ) {
        try {
            next.Invoke( );
        } catch ( ObjectDisposedException oe ) {
            // We can get this exception in .Net 4.0. In this case, the cancellation did occur and the cts is already disposed
            onError?.Invoke( oe );
        } catch ( Exception e ) {
            if ( onError == null ) {
                Console.WriteLine( $"{e.Message}\r\n{e.StackTrace}" );
                return;
            }
            onError.Invoke( e );
        }
    }
    public static T TryWrape<T>( Func<T> next, Action<Exception> onError = null ) {
        T result;
        try {
            result = next.Invoke( );
        } catch ( Exception e ) {
            result = default;
            if ( onError != null ) {
                onError.Invoke( e );
                return result;
            }
            Console.WriteLine( $"{e.Message}\r\n{e.StackTrace}" );
        }
        return result;
    }
    public static void ExitThread( Thread th ) {
        if ( th == null ) return;
        if ( !th.IsAlive ) return;
        try { th.Interrupt( ); } catch { }
        // try { th.Abort( ); } catch { }
        try { th.Join( TimeSpan.FromMilliseconds( 500 ) ); } catch { }
    }
    public static T TryGetValue<T>( T[] data, int index ) {
        return data.ElementAtOrDefault( index );
    }
    public static T TryGetValue<T>( List<T> data, int index ) {
        return data.ElementAt( index );
    }
    public static void Dispose<T>( T instance ) where T : IDisposable {
        if ( instance == null ) return;
        try {
            instance.Dispose( );
        } catch ( ObjectDisposedException ) {
            // Object disposed...
        } catch ( Exception e ) {
            Console.Error.WriteLine( $"{e.Message}\r\n{e.StackTrace}" );
        }
    }
    /// <summary>
    /// Sets a variable of the specified type T to a null value as an atomic operation
    /// and original value exists, then the original value will invoked next function
    /// </summary>
    /// <typeparam name="TInstance"></typeparam>
    /// <param name="instance"></param>
    /// <param name="next"></param>
    public static void Exchange<TInstance>( ref TInstance instance, Action<TInstance> next ) where TInstance : class {
        if ( instance == null && next == null ) return;
        //Volatile.Write( ref instance, null );
        TInstance obj = Interlocked.Exchange( ref instance, null );
        if ( obj == null ) return;
        next?.Invoke( obj );
    }
    /// <summary>
    /// Sets a variable of the specified type <see cref="T"/> to a specified value and returns the
    /// original value, as an atomic operation.
    /// </summary>
    /// <typeparam name="TInstance"></typeparam>
    /// <param name="instance"></param>
    /// <param name="next"></param>
    public static TInstance Exchange<TInstance>( ref TInstance instance, TInstance transFrom = null ) where TInstance : class {
        if ( instance == null && transFrom == null ) return instance;
        return Interlocked.Exchange( ref instance, transFrom );
    }
    public static string Exchange( ref string oldValue, string newValue = null ) {
        if ( oldValue == null && newValue == null ) return oldValue;
        return Interlocked.Exchange( ref oldValue, newValue );
    }
    /// <summary>
    /// Sets a 32-bit signed integer to a specified value and returns the original value,
    /// as an atomic operation.
    /// </summary>
    /// <param name="oldValue"></param>
    /// <param name="newValue"></param>
    /// <returns>The original value of oldValue.</returns>
    /// <exception cref="T:System.ArgumentNullException:"></exception>
    public static int Exchange( ref int oldValue, int newValue = 0 ) {
        return Interlocked.Exchange( ref oldValue, newValue );
    }
    /// <summary>
    /// Sets a 64-bit signed integer to a specified value and returns the original value,
    /// as an atomic operation.
    /// </summary>
    /// <param name="oldValue"></param>
    /// <param name="newValue"></param>
    /// <returns>The original value of oldValue.</returns>
    /// <exception cref="T:System.ArgumentNullException:"></exception>
    public static long Exchange( ref long oldValue, long newValue = 0 ) {
        return Interlocked.Exchange( ref oldValue, newValue );
    }
    public static void Exchange<TInstance>( ref TInstance instance, TInstance transFrom, Action<TInstance> next ) where TInstance : class {
        if ( instance == null && transFrom == null ) return;
        TInstance obj = Interlocked.Exchange( ref instance, transFrom );
        if ( obj == null ) return;
        next?.Invoke( obj );
    }
}
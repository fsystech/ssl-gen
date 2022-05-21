/**
* Copyright (c) 2018, Sow ( https://safeonline.world, https://www.facebook.com/safeonlineworld). (https://github.com/RKTUXYN) All rights reserved.
* Copyrights licensed under the New BSD License.
* See the accompanying LICENSE file for terms.
*/
// 2:42 PM 4/16/2021
// Rajib Chy
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
namespace Sow.Framework;
    public static class MultiThreading {
        public static int GetMaxDegreeOfParallelism( int totalEnqueue, int reqPerThread = 5 ) {
            int maxDegreeOfParallelism = ( int )( totalEnqueue / reqPerThread );
            if ( maxDegreeOfParallelism <= 0 ) {
                maxDegreeOfParallelism = 1;
            } else if ( maxDegreeOfParallelism > 30 ) {
                maxDegreeOfParallelism = 30;
            }
            if ( maxDegreeOfParallelism > totalEnqueue ) {
                maxDegreeOfParallelism = totalEnqueue;
            }
            return maxDegreeOfParallelism;
        }

        /// <summary>
        /// Make partition the given <see cref="List{T}"/> with <see cref="int"/> <paramref name="partitionSize"/> and
        /// invoke with seperate <see cref="Task"/> 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="next"></param>
        /// <param name="data">List</param>
        /// <param name="partitionSize">Each partition size</param>
        /// <param name="ct">CancellationToken</param>
        /// <returns>A <see cref="Task"/> that represents the completion of all of the supplied tasks.</returns>
        public static Task ParallelInvoke<T>( 
            Func<List<T>, Task> next, List<T> data, 
            int partitionSize, CancellationToken ct 
        ) {
            List<Task> tasks = new List<Task>( );
            IEnumerable<List<T>> parts = data.Partition( partitionSize );
            foreach ( List<T> part in parts ) {
                tasks.Add( Task.Run( ( ) => ThreadSafe.TryWrape( ( ) => next( part ) ), ct ) );
            }
            return Task.WhenAll( tasks.ToArray( ) );
        }
        public static Task ParallelInvoke<T>(
            Func<List<T>, Task> next, List<T> data,
            int partitionSize
        ) => ParallelInvoke( next, data, partitionSize, CancellationToken.None );
        public static Task ParallelInvoke<T>(
            Func<Task<T>> next, int totalEnqueue,
            int reqPerThread = 5
         ) {
            //each thread handle min 5 request
            int maxDegreeOfParallelism = GetMaxDegreeOfParallelism( totalEnqueue, reqPerThread );
            List<Task> tasks = new List<Task>( );
            for ( int i = 0; i < maxDegreeOfParallelism; i++ ) {
                tasks.Add( next( ) );
            }
            return Task.WhenAll( tasks.ToArray( ) );
        }

        public static Task ParallelInvoke<T>(
            Func<Task<T>> next, int totalEnqueue,
            CancellationToken ct, int reqPerThread = 5, bool wrapeWithTask = true
         ) {
            //each thread handle min 5 request
            int maxDegreeOfParallelism = GetMaxDegreeOfParallelism( totalEnqueue, reqPerThread );
            List<Task> tasks = new List<Task>( );
            for ( int i = 0; i < maxDegreeOfParallelism; i++ ) {
                if ( wrapeWithTask ) {
                    tasks.Add( Task.Run( ( ) => ThreadSafe.TryWrape( next ), ct ) );
                } else {
                    tasks.Add( next( ) );
                }
            }
            return Task.WhenAll( tasks.ToArray( ) );
        }
        /// <summary>
        /// Creates <see cref="Parallel"/> <see cref="Task"/> <see cref="Array"/> that will complete when all of the <see cref="Task"/>
        ///  <see cref="Object"/> in an <see cref="Array"/> have completed.
        /// </summary>
        /// <param name="next"></param>
        /// <param name="totalEnqueue"></param>
        /// <param name="ct"></param>
        /// <param name="reqPerThread"></param>
        /// <param name="wrapeWithTask"></param>
        /// <returns>A <see cref="Task"/> that represents the completion of all of the supplied <see cref="Task"/>s.</returns>
        /// <exception cref="ArgumentNullException">The <see cref="Task"/>s argument was <see cref="null"/></exception>
        /// <exception cref="ArgumentException">The <see cref="Task"/>s <see cref="Array"/> contained a <see cref="null"/> <see cref="Task"/></exception>
        public static Task ParallelInvoke(
            Func<Task> next, int totalEnqueue,
            CancellationToken ct, int reqPerThread = 5, bool wrapeWithTask = true
         ) {
            //each thread handle min 5 request
            int maxDegreeOfParallelism = GetMaxDegreeOfParallelism( totalEnqueue, reqPerThread );
            List<Task> tasks = new List<Task>( );
            for ( int i = 0; i < maxDegreeOfParallelism; i++ ) {
                if( wrapeWithTask ) {
                    tasks.Add( Task.Run( ( ) => ThreadSafe.TryWrape( next ), ct ) );
                } else {
                    tasks.Add( next() );
                }
            }
            return Task.WhenAll( tasks.ToArray( ) );
        }
    }
    public static class WaitHandler {
        /// <summary>
        /// Suspends the current <see cref="Thread"/> for the specified number of milliseconds.
        /// </summary>
        /// <param name="millisecondsDelay"></param>
        /// <returns>true if the current instance exception occured; otherwise, false.</returns>
        public static bool Wait( int millisecondsDelay ) {
            try {
                Thread.Sleep( millisecondsDelay );
            } catch {
                return true;
            }
            return false;
        }
        /// <summary>
        /// Blocks the current <see cref="Thread"/> until the current <see cref="WaitHandle"/> receives
        /// a signal, using a <see cref="Int32"/> 32-bit signed integer to specify the time interval in milliseconds.
        /// </summary>
        /// <param name="millisecondsDelay"></param>
        /// <param name="cancellationToken"></param>
        /// <returns><see cref="Boolean"/> if the current instance receives a signal then <see cref="true"/>; otherwise, false.</returns>
        public static bool Wait(int millisecondsDelay, CancellationToken cancellationToken ) {
            if ( cancellationToken == CancellationToken.None ) {
                try {
                    Thread.Sleep( millisecondsDelay );
                } catch {
                    return true;
                }
                return false;
            }
            if ( cancellationToken.IsCancellationRequested ) return true;
            bool cancelled = true;
            WaitHandle handle;
            try {
                handle = cancellationToken.WaitHandle;
            } catch {
                // eg. CancellationTokenSource is disposed
                return cancelled;
            }
            try {
                cancelled = handle.WaitOne( millisecondsDelay );
            } catch { }
            return cancelled;
        }
        /// <summary>
        /// Blocks the current <see cref="Thread"/> until the current <see cref="WaitHandle"/> receives
        /// a signal, using a <see cref="Int32"/> 32-bit signed integer to specify the time interval in milliseconds.
        /// </summary>
        /// <param name="millisecondsDelay"></param>
        /// <param name="cancellationToken"></param>
        /// <returns><see cref="Boolean"/> if the current instance receives a signal then <see cref="true"/>; otherwise, false.</returns>
        public static bool Sleep( int millisecondsDelay, CancellationToken cancellationToken ) {
            return Wait( millisecondsDelay, cancellationToken );
        }
    }

/**
* Copyright (c) 2018, Sow ( https://safeonline.world, https://www.facebook.com/safeonlineworld). (https://github.com/RKTUXYN) All rights reserved.
* Copyrights licensed under the New BSD License.
* See the accompanying LICENSE file for terms.
*/
// 4:16 PM 5/6/2021
// Rajib Chy
using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Collections.Concurrent;
namespace Sow.Framework;
public static class ListExt {
    public static IEnumerable<List<T>> Partition<T>( this IList<T> source, Int32 size ) {
        for ( int i = 0; i < Math.Ceiling( source.Count / ( Double )size ); i++ )
            yield return new List<T>( source.Skip( size * i ).Take( size ) );
    }
}
public static class ConcurrentDictionaryExt {
    public static TValue TryGetValue<TKey, TValue>(
        this ConcurrentDictionary<TKey, TValue> self, TKey key
     ) {
        _ = self.TryGetValue( key, out TValue value );
        return value;
    }
    public static bool TryRemove<TKey, TValue>(
        this ConcurrentDictionary<TKey, TValue> self, TKey key
     ) => self.TryRemove( key, out _ );
    public static bool TryUpdate<TKey, TValue>(
        this ConcurrentDictionary<TKey, TValue> source, TKey key, TValue newValue
     ) => source.TryUpdate( key, newValue, source.TryGetValue( key ) );
    public static IEnumerable<KeyValuePair<TKey, TValue>> Find<TKey, TValue>(
        this ConcurrentDictionary<TKey, TValue> source, Func<TValue, bool> predicate
     ) => source.Where( a => predicate( a.Value ) );
    public static ConcurrentDictionary<TKey, TValue> ToConcurrentDictionary<TKey, TValue>(
        this IEnumerable<TValue> source, Func<TValue, TKey> valueSelector
    ) => new ConcurrentDictionary<TKey, TValue>( source.ToDictionary( valueSelector ) );
}
public static class Extension {
    public static IList<T> Clone<T>( this IList<T> listToClone ) where T : ICloneable {
        return listToClone.Select( item => ( T )item.Clone( ) ).ToList( );
    }
    public static bool ObjectAreEqual( object valA, object valB )
        => ( valA == null && valB == null ) || ( valA != null && valA.Equals( valB ) );
    public static bool ClassPropertiesEqual<T>(
        T self, T to, List<string> ignoreList
    ) where T : class {
        Type type = typeof( T );
        PropertyInfo[] oProp = type.GetProperties( BindingFlags.Public | BindingFlags.Instance );
        foreach ( PropertyInfo pi in oProp ) {
            if ( ignoreList.Contains( pi.Name ) ) continue;
            object oValue = pi.GetValue( self, null );
            object nValue = pi.GetValue( to, null );
            if ( !ObjectAreEqual( oValue, nValue ) ) return false;
        }
        return true;
    }
    public static bool ClassPropertiesEqual<T>(
        T self, T to, params string[] ignore
    ) where T : class {
        return ClassPropertiesEqual( self, to, new List<string>( ignore ) );
    }
    private static ParallelQuery<TSource> InternalCreateParallelQuery<TSource>( IEnumerable<TSource> source, int degreeOfParallelism ) {
        if ( degreeOfParallelism < 1 )
            throw new ArgumentOutOfRangeException( "degreeOfParallelism" );
        if ( source == null ) return null;
        if ( source.Count( ) <= degreeOfParallelism ) {
            return source.AsParallel( ).WithDegreeOfParallelism( 1 );
        }
        return source.AsParallel( ).WithDegreeOfParallelism( degreeOfParallelism );
    }
    /// <summary>
    ///     Create "ParallelQuery" and Sets the degree of parallelism to use in a query. Degree of parallelism is the
    ///     maximum number of concurrently executing tasks that will be used to process the
    ///     query.
    /// </summary>
    /// <typeparam name="TSource"></typeparam>
    /// <param name="self"></param>
    /// <param name="degreeOfParallelism"></param>
    /// <returns></returns>
    public static ParallelQuery<TSource> CreateParallelQuery<TSource>(
        this List<TSource> self, int degreeOfParallelism = 2
    ) => InternalCreateParallelQuery( self, degreeOfParallelism );
    /// <summary>
    ///     Create "ParallelQuery" and Sets the degree of parallelism to use in a query. Degree of parallelism is the
    ///     maximum number of concurrently executing tasks that will be used to process the
    ///     query.
    /// </summary>
    /// <typeparam name="TSource"></typeparam>
    /// <param name="self"></param>
    /// <param name="degreeOfParallelism"></param>
    /// <returns></returns>
    public static ParallelQuery<TSource> CreateParallelQuery<TSource>(
        this IEnumerable<TSource> self, int degreeOfParallelism = 2
    ) => InternalCreateParallelQuery( self, degreeOfParallelism );
    /// <summary>
    ///     Create "ParallelQuery" and Sets the degree of parallelism to use in a query. Degree of parallelism is the
    ///     maximum number of concurrently executing tasks that will be used to process the
    ///     query.
    /// </summary>
    /// <typeparam name="TSource"></typeparam>
    /// <param name="self"></param>
    /// <param name="degreeOfParallelism"></param>
    /// <returns></returns>
    public static ParallelQuery<TSource> CreateParallelQuery<TSource>(
        this TSource[] self, int degreeOfParallelism = 2
    ) => InternalCreateParallelQuery( self, degreeOfParallelism );
}
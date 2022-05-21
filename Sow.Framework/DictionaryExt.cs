/**
* Copyright (c) 2018, Sow ( https://safeonline.world, https://www.facebook.com/safeonlineworld). (https://github.com/RKTUXYN) All rights reserved.
* Copyrights licensed under the New BSD License.
* See the accompanying LICENSE file for terms.
*/
// 4:13 PM 6/17/2021
// Rajib Chy
using System.Collections.Generic;
namespace Sow.Framework;
public static class DictionaryExt {
    public static TValue TryGetValue<TKey, TValue>(
        this Dictionary<TKey, TValue> self, TKey key
    ) {
        if ( self.TryGetValue( key, out TValue value ) ) {
            return value;
        }
        return default;
    }
    public static string TryGetString<TKey, TValue>(
        this Dictionary<TKey, TValue> self, TKey key
    ) {
        if ( self.TryGetValue( key, out TValue value ) ) {
            return value.ToString( );
        }
        return string.Empty;
    }
    public static int TryGetInt<TKey, TValue>(
        this Dictionary<TKey, TValue> self, TKey key, int defaultValue = -1
    ) {
        int result = defaultValue;
        if ( !self.TryGetValue( key, out TValue value ) ) return result;
        if ( value == null ) return result;
        if ( !int.TryParse( value.ToString( ), out result ) ) return result;
        return result;
    }
    public static decimal TryGetDecimal<TKey, TValue>(
       this Dictionary<TKey, TValue> self, TKey key, decimal defaultValue = -1
   ) {
        decimal result = defaultValue;
        if ( !self.TryGetValue( key, out TValue value ) ) return result;
        if ( value == null ) return result;
        if ( !decimal.TryParse( value.ToString( ), out result ) ) return result;
        return result;
    }
    public static long TryGetLong<TKey, TValue>(
        this Dictionary<TKey, TValue> self, TKey key, long defaultValue = -1
    ) {
        long result = defaultValue;
        if ( !self.TryGetValue( key, out TValue value ) ) return result;
        if ( !long.TryParse( value.ToString( ), out result ) ) return result;
        return result;
    }
    public static bool TryGetBool<TKey, TValue>(
        this Dictionary<TKey, TValue> self, TKey key, bool defaultValue = false
    ) {
        bool result = defaultValue;
        if ( !self.TryGetValue( key, out TValue value ) ) return result;
        if ( value == null ) return result;
        if ( !bool.TryParse( value.ToString( ), out result ) ) return result;
        return result;
    }
}
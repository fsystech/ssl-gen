/**
* Copyright (c) 2018, Sow ( https://safeonline.world, https://www.facebook.com/safeonlineworld). (https://github.com/RKTUXYN) All rights reserved.
* Copyrights licensed under the New BSD License.
* See the accompanying LICENSE file for terms.
*/
// 8:18 PM 9/13/2018 Rajib
// Rajib Chy
namespace Sow.Framework.Security.CloudflareWrapper.Http {
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    interface IWebHttp : IDisposable {
        Task<IWebHttpResponse> GetAsync( string requestUri, Dictionary<string, string> header = null );
        Task<IWebHttpResponse> PostAsync( string requestUri, string postJson, Dictionary<string, string> header = null );
        Task<IWebHttpResponse> DeleteAsync( string requestUri, Dictionary<string, string> header = null );
    }
}

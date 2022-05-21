/**
* Copyright (c) 2018, Sow ( https://safeonline.world, https://www.facebook.com/safeonlineworld). (https://github.com/RKTUXYN) All rights reserved.
* Copyrights licensed under the New BSD License.
* See the accompanying LICENSE file for terms.
*/
// 8:18 PM 9/13/2018
// Rajib Chy
namespace Sow.Framework.Security.CloudflareWrapper.Http {
    interface IWebHttpResponse {
        WebHttpStatus status { get; set; }
        string errorDescription { get; set; }
        string responseText { get; set; }
    }
}

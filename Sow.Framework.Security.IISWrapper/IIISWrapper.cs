/**
* Copyright (c) 2018, Sow ( https://safeonline.world, https://www.facebook.com/safeonlineworld). (https://github.com/RKTUXYN) All rights reserved.
* Copyrights licensed under the New BSD License.
* See the accompanying LICENSE file for terms.
*/
// 7:50 PM 9/15/2018
// Rajib Chy
namespace Sow.Framework.Security {
    using System;
    using System.Security.Cryptography.X509Certificates;
    public interface IIISWrapper:IDisposable {
        bool ExistsCertificate( X509Store store, string serialNumber, ILogger logger, bool remove = false, bool validOnly = false );
        bool InstallCertificate( X509Certificate2 certificate, ILogger logger, string zoneName, StoreName storeName, X509Certificate2 oldCertificate = null );
        IIISWrapperResponse BindCertificate( IISWrapperSettings AppSettings, ILogger logger );
    }
}

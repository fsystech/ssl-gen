/**
* Copyright (c) 2018, Sow ( https://safeonline.world, https://www.facebook.com/safeonlineworld). (https://github.com/RKTUXYN) All rights reserved.
* Copyrights licensed under the New BSD License.
* See the accompanying LICENSE file for terms.
*/
// 6:46 PM 9/14/2018
// Rajib Chy
using System.Security.Cryptography.X509Certificates;
namespace Sow.Framework.Security.LetsEncrypt {
    public class Certificate : ICertificate {
        public string cert_dir { get; set; }
        public string cert_path { get; set; }
        public X509Certificate2 Cert { get; set; }
        public bool isExpired { get; set; }
        public bool status { get; set; }
        public string errorDescription { get; set; }
    }
}

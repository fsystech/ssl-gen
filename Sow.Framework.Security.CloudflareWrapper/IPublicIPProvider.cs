using System;
using System.Collections.Generic;
using System.Text;

namespace Sow.Framework.Security.CloudflareWrapper {
    interface IPublicIPProvider {
        bool exit { get; set; }
        string GetIp();
    }
}

/**
* Copyright (c) 2018, Sow ( https://safeonline.world, https://www.facebook.com/safeonlineworld). (https://github.com/RKTUXYN) All rights reserved.
* Copyrights licensed under the New BSD License.
* See the accompanying LICENSE file for terms.
*/
// 6:46 PM 9/14/2018
// Rajib Chy
using Certes;
using Certes.Acme;
namespace Sow.Framework.Security.LetsEncrypt {
    class AcmeCtx : IAcmeCtx {
        public IAcmeContext Ctx { get; set; }
        public IAccountContext ACtx { get; set; }
    }
}

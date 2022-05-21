//10:08 PM 9/14/2018 Rajib
namespace Sow.Framework.Security.LetsEncrypt {
    using Certes.Acme;
    class ChallengeCtx : IChallengeCtx {
        public IChallengeContext ctx { get; set; }
        public string txtName { get; set; }
    }
}

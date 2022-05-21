namespace Sow.Framework.Security.LetsEncrypt {
    using Certes;
    using Certes.Acme;
    interface IAcmeCtx {
        IAcmeContext Ctx { get; set; }
        IAccountContext ACtx { get; set; }
    }
}

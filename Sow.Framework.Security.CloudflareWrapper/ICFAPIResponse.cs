//2:40 AM 9/14/2018 Rajib
namespace Sow.Framework.Security.CloudflareWrapper {
    public interface ICFAPIResponse {
        object result { get; set; }
        bool success { get; set; }
        object errors { get; set; }
        object messages { get; set; }
    }
}

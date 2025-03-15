//2:55 AM 9/14/2018 Rajib
namespace Sow.Framework.Security.CloudflareWrapper {
    using System;
    using Newtonsoft.Json;
    using System.Threading.Tasks;
    public interface ICFDNS : IDisposable {
        JsonSerializerSettings JsonConfig { get; }
        Task<ICFAPIResponse> AddRecord( IQueryConfig qConfig, bool checkExistence = true );
        Task<ICFAPIResponse> ExistsRecord( IQueryConfig qConfig );
        Task<ICFAPIResponse> RemoveRecord( IQueryConfig qConfig );
    }
}

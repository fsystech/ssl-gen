//3:16 AM 9/14/2018 Rajib
namespace Sow.Framework.Security.CloudflareWrapper {
    using System;
    using Newtonsoft.Json;
    using System.Threading.Tasks;
    using System.Collections.Generic;
    using Sow.Framework.Security.CloudflareWrapper.Http;

    public class CFDNS : ICFDNS {
        ILogger _logger { get; set; }
        IWebHttp _webHttp { get; set; }
        ICFConfig _config { get; set; }
        private readonly JsonSerializerSettings _jsonSettings;
        public JsonSerializerSettings JsonConfig => _jsonSettings;
        public CFDNS( ICFConfig config, ILogger logger ) {
            _config = config; _logger = logger;
            _webHttp = new WebHttp( _config.CF_API );
            _jsonSettings = new JsonSerializerSettings {
                Formatting = Formatting.Indented,
                NullValueHandling = NullValueHandling.Ignore
            };
        }
        private static string GetRecordText( CFRecordType cFRecord ) {
            return cFRecord switch {
                CFRecordType.A => "A",
                CFRecordType.AAA => "AAA",
                CFRecordType.CNAME => "CNAME",
                CFRecordType.MX => "MX",
                CFRecordType.LOC => "LOC",
                CFRecordType.SRV => "SRV",
                CFRecordType.SPF => "SPF",
                CFRecordType.TXT => "TXT",
                CFRecordType.NS => "NS",
                CFRecordType.CAA => "CAA",
                _ => throw new Exception( "Invalid Record type defined!!!" ),
            };
        }
        private static Dictionary<object, object> GetData( IQueryConfig qConfig ) {
            return new Dictionary<object, object>( ) {
                { "type", GetRecordText(qConfig.RECORD_TYPE) },
                { "name", qConfig.RECORD_NAME },
                { "content", qConfig.RECORD_CONTENT },
                { "ttl", 120}
            };
        }
        private Dictionary<string, string> GetHeader( ) {
            _logger.Write( $"AUTH KEY: {_config.CF_AUTH_KEY}; AUTH EMAIL: {_config.CF_AUTH_EMAIL}" );
            // "Content-Type: application/json"
            return new Dictionary<string, string>( ) {
                { "X-Auth-Email", _config.CF_AUTH_EMAIL },
                { "X-Auth-Key", _config.CF_AUTH_KEY }
                // { "Authorization", $"Bearer {_config.CF_AUTH_KEY}" }
            };
        }
        public async Task<ICFAPIResponse> ExistsRecord( IQueryConfig qConfig ) {
            // { "Content-Type", "application/json" },
            string requestUriString = string.Format( "{0}zones/{1}/dns_records?type={2}&name={3}&content={4}", _config.CF_URI, _config.CF_DNS_ZONE, qConfig.RECORD_TYPE, qConfig.NAME, qConfig.RECORD_CONTENT );
            _logger.Write( requestUriString );
            IWebHttpResponse resp = await _webHttp.GetAsync( requestUriString, GetHeader( ) );
            if ( resp.status != WebHttpStatus.SUCCESS ) {
                _logger.Write( "Error occured while checking {0} record for {1} . Error=>", qConfig.RECORD_NAME, qConfig.DOMAIN_NAME, resp.errorDescription );
                return new CFAPIResponse {
                    success = false, errors = new object[] { resp.errorDescription }
                };
            }
            if ( string.IsNullOrEmpty( resp.responseText ) ) {
                return new CFAPIResponse {
                    success = false, errors = new object[] { "No Response found from API!!!" }
                };
            }
            ICFAPIResponse cFAPIResponse = JsonConvert.DeserializeObject<CFAPIResponse>( resp.responseText, JsonConfig );
            if ( cFAPIResponse.result == null ) {
                return new CFAPIResponse {
                    success = false, errors = new object[] { "No Response found from API!!!" }
                };
            }
            if ( cFAPIResponse.result is Newtonsoft.Json.Linq.JArray ) {
                Newtonsoft.Json.Linq.JArray rs = ( Newtonsoft.Json.Linq.JArray )cFAPIResponse.result;
                if ( rs.Count <= 0 ) {
                    return new CFAPIResponse {
                        success = false, errors = new object[] { "Not Exists!!!" }
                    };
                }
                _logger.Write( "{0} record already exists in {1}", qConfig.RECORD_NAME, qConfig.DOMAIN_NAME );
            }
            return cFAPIResponse;
        }
        public async Task<ICFAPIResponse> RemoveRecord( IQueryConfig qConfig ) {
            try {
                ICFAPIResponse aPI = await ExistsRecord( qConfig );
                if ( aPI.success != true ) {
                    return new CFAPIResponse {
                        success = true, messages = new object[] { "Not Exists this record!!!" }
                    };
                }
                _logger.Write( "Deleting DNS Record for {0}, Record type {1}", qConfig.DOMAIN_NAME, GetRecordText( qConfig.RECORD_TYPE ) );
                IWebHttpResponse resp = await _webHttp.DeleteAsync( string.Format( "{0}zones/{1}/dns_records/{2}", _config.CF_URI, _config.CF_DNS_ZONE, qConfig.RECORD_ID ), this.GetHeader( ) );
                if ( resp.status != WebHttpStatus.SUCCESS ) {
                    _logger.Write( "Error occured while delete {0} record for {1} . Error=> {2}", qConfig.RECORD_NAME, qConfig.DOMAIN_NAME, resp.errorDescription );
                    return new CFAPIResponse {
                        success = false, errors = new object[] { resp.errorDescription }
                    };
                }
                if ( !string.IsNullOrEmpty( resp.responseText ) ) {
                    _logger.Write( $"RemoveRecord-> {qConfig.DOMAIN_NAME} {GetRecordText( qConfig.RECORD_TYPE )}" );
                    _logger.Write( resp.responseText );
                    //_logger.Write( "Error occured while add {0} record for {1} . Error=> No Response found from API!!!", qConfig.RECORD_NAME, qConfig.DOMAIN_NAME );
                    //return new CFAPIResponse {
                    //    success = false, errors = new object[] { "No Response found from API!!!" }
                    //};
                }
                return new CFAPIResponse {
                    success = true, messages = new object[] { "Success" }
                };
                //throw new NotImplementedException( "!TODO" );
            } catch ( Exception e ) {
                _logger.Write( "Error occured Remove DNS TXT Record for {0} . Error=> {1}", qConfig.DOMAIN_NAME, e.Message );
                _logger.Write( e.StackTrace );
                return new CFAPIResponse {
                    success = true, messages = new object[] { e.Message }
                };
            }
        }
        public async Task<ICFAPIResponse> AddRecord( IQueryConfig qConfig, bool checkExistence = true ) {
            if ( qConfig.RECORD_TYPE != CFRecordType.TXT )
                throw new NotImplementedException( "Not Implemented Yet!!!" );
            if ( checkExistence ) {
                ICFAPIResponse aPI = await ExistsRecord( qConfig );
                if ( aPI.success == true ) {
                    return new CFAPIResponse {
                        success = true, messages = new object[] { "Exists this record!!!" }
                    };
                }
            }
            _logger.Write( "Adding DNS Record for {0}, Record type {1}", qConfig.DOMAIN_NAME, GetRecordText( qConfig.RECORD_TYPE ) );
            IWebHttpResponse resp = null;
            try {
                resp = await _webHttp.PostAsync( string.Format( "{0}zones/{1}/dns_records", _config.CF_URI, _config.CF_DNS_ZONE ), JsonConvert.SerializeObject( GetData( qConfig ), JsonConfig ), GetHeader( ) );
            } catch ( Exception e ) {
                _logger.Write( e );
                return new CFAPIResponse {
                    success = false, errors = new object[] { e.Message }
                };
            }
            if ( resp.status != WebHttpStatus.SUCCESS ) {
                _logger.Write( "Error occured while add {0} record for {1} . Error=> {2}", qConfig.RECORD_NAME, qConfig.DOMAIN_NAME, resp.errorDescription );
                return new CFAPIResponse {
                    success = false, errors = new object[] { resp.errorDescription }
                };
            }
            if ( string.IsNullOrEmpty( resp.responseText ) ) {
                _logger.Write( "Error occured while add {0} record for {1} . Error=> No Response found from API!!!", qConfig.RECORD_NAME, qConfig.DOMAIN_NAME );
                return new CFAPIResponse {
                    success = false, errors = new object[] { "No Response found from API!!!" }
                };
            }
            return JsonConvert.DeserializeObject<CFAPIResponse>( resp.responseText, JsonConfig );

        }
        //private static string GetIPAddress( ) {
        //    string ip = string.Empty;
        //    using ( WebClient webClient = new WebClient( ) )
        //        ip = webClient.DownloadString( "https://icanhazip.com/" );
        //    return ip;
        //}
        public void Dispose( ) {
            _config = null;
            _webHttp.Dispose( );
            GC.SuppressFinalize( this );
            GC.Collect( 0, GCCollectionMode.Optimized );
        }
    }
}
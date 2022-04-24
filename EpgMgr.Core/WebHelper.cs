using System.Net;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace EpgMgr
{
    /// <summary>
    /// Webhelper. Aids with making web requests including JSON deserialization/serialization
    /// </summary>
    public class WebHelper
    {
        private readonly string DefaultUserAgent;
        private readonly HttpClient m_httpClient;

        /// <summary>
        /// The HTTP client
        /// </summary>
        public HttpClient Client => m_httpClient;

        /// <summary>
        /// Create new instance of the web helper
        /// </summary>
        /// <param name="baseUri"></param>
        /// <param name="userAgent"></param>
        /// <param name="timeOut"></param>
        /// <param name="decompMethods"></param>
        /// <param name="acceptHeaders"></param>
        public WebHelper(string baseUri, string? userAgent = null, int? timeOut = 5000, DecompressionMethods? decompMethods = null, MediaTypeWithQualityHeaderValue[]? acceptHeaders = null)
        {
            if (userAgent != null)
                DefaultUserAgent = userAgent;
            else
            {
                DefaultUserAgent = $"Mozilla/5.0 ({EnvAgentString})";
                var version = Assembly.GetExecutingAssembly().GetName().Version;
                if (version != null)
                    DefaultUserAgent +=
                        $" EpgMgr/{version.Major}.{version.Minor}";
            }

            m_httpClient = new HttpClient(new HttpClientHandler()
            {
                AutomaticDecompression = decompMethods ?? DecompressionMethods.GZip | DecompressionMethods.Deflate
            });

            m_httpClient.BaseAddress = new Uri(baseUri);

            if (acceptHeaders != null)
            {
                foreach (var header in acceptHeaders)
                    m_httpClient.DefaultRequestHeaders.Accept.Add(header);
            }
            else
            {
                m_httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json")
                {
                    CharSet = Encoding.UTF8.WebName
                });
            }
        }

        /// <summary>
        /// Generates an agent string for the current environment
        /// </summary>
        public static string EnvAgentString
        {
            get
            {
                var envId = "Other";
                if (OperatingSystem.IsLinux())
                {
                    envId = "Linux;";
                    if (Environment.Is64BitOperatingSystem) 
                        envId += " x86_64";
                    else
                        envId += " x86";
                }

                if (OperatingSystem.IsMacOS())
                    envId = $"Macintosh; Intel Mac OS X {Environment.OSVersion.Version.Major}_{Environment.OSVersion.Version.Minor}";

                if (OperatingSystem.IsWindows())
                {
                    envId = $"Windows NT {Environment.OSVersion.Version.Major}.{Environment.OSVersion.Version.Minor}";
                    if (Environment.Is64BitOperatingSystem)
                        envId += "; Win64; x64";
                }

                return envId;
            }
        }

        /// <summary>
        /// For cases where we can't create a known object type
        /// Parse JSON string and return dynamic type
        /// </summary>
        /// <param name="jsonstring"></param>
        /// <returns></returns>
        public dynamic? GetDynamic(string jsonstring)
        {
            return JsonSerializer.Deserialize<dynamic>(jsonstring);
        }

        /// <summary>
        /// Serialize object to JSON string
        /// </summary>
        /// <param name="obj"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public string? CreateJSONstring<T>(T obj) => JsonSerializer.Serialize<T>(obj);

        /// <summary>
        /// Deserialize JSON string to object
        /// </summary>
        /// <param name="input"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T? ParseJSON<T>(string input) => JsonSerializer.Deserialize<T>(input);

        /// <summary>
        /// Perform post method and parse response via JSON deserializer to known object type
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="obj"></param>
        /// <param name="headers"></param>
        /// <typeparam name="V"></typeparam>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public V? PostJSON<V, T>(string uri, T obj, Dictionary<string, string>? headers = null)
        {
            var requestString = CreateJSONstring(obj);
            if (requestString == null) return default;
            var response = WebPost(uri, requestString, headers);
            var result = ParseJSON<V>(response);
            return result;
        }

        /// <summary>
        /// Perform get method and parse response via JSON serializer to known object type
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="headers"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T? GetJSON<T>(string uri, Dictionary<string, string>? headers = null) => ParseJSON<T>(WebGet(uri, headers));

        /// <summary>
        /// Perform put method and parse response via JSON serializer to known object type
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="headers"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T? PutJSON<T>(string uri, Dictionary<string, string>? headers = null) => ParseJSON<T>(WebPut(uri, headers));

        /// <summary>
        /// Perform delete method and parse response via JSON serializer to known object type
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="headers"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T? DeleteJSON<T>(string uri, Dictionary<string, string>? headers = null) => ParseJSON<T>(WebDelete(uri, headers));

        /// <summary>
        /// Handle get request, return response as string 
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="headers"></param>
        /// <returns></returns>
        public string WebGet(string uri, Dictionary<string, string>? headers = null)
        {
            var postResponse = WebAction(uri, null, "GET", headers);
            using var resp = new StreamReader(postResponse.Content.ReadAsStream());
            return resp.ReadToEnd();
        }

        /// <summary>
        /// Handle put request, return response as string
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="headers"></param>
        /// <returns></returns>
        public string WebPut(string uri, Dictionary<string, string>? headers = null)
        {
            var postResponse = WebAction(uri, null, "PUT", headers);
            using var resp = new StreamReader(postResponse.Content.ReadAsStream());
            return resp.ReadToEnd();
        }

        /// <summary>
        /// Handle delete request, return response as string
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="headers"></param>
        /// <returns></returns>
        public string WebDelete(string uri, Dictionary<string, string>? headers = null)
        {
            var postResponse = WebAction(uri, null, "DELETE", headers);
            using var resp = new StreamReader(postResponse.Content.ReadAsStream());
            return resp.ReadToEnd();
        }

        /// <summary>
        /// Handle post request, return response as string
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="jsonstring"></param>
        /// <param name="headers"></param>
        /// <returns></returns>
        public string WebPost(string uri, string jsonstring, Dictionary<string, string>? headers = null)
        {
            var postResponse = WebAction(uri, jsonstring, "POST", headers);
            using var resp = new StreamReader(postResponse.Content.ReadAsStream());
            return resp.ReadToEnd();
        }

        /// <summary>
        /// Create web request for specified method and URL
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="content"></param>
        /// <param name="method"></param>
        /// <param name="headers"></param>
        /// <returns></returns>
        public HttpResponseMessage WebAction(string uri, string? content = null, string method = "GET", Dictionary<string, string>? headers = null)
        {
            var request = new HttpRequestMessage(new HttpMethod(method), new Uri(uri));
            if (content != null)
                request.Content = new StringContent(content, Encoding.UTF8, "application/json");

            if (headers != null)
            {
                foreach (var header in headers)
                    request.Headers.Add(header.Key, header.Value);
            }
            request.Headers.UserAgent.Clear();
            request.Headers.UserAgent.ParseAdd(DefaultUserAgent);

            return m_httpClient.Send(request);
        }
    }
}

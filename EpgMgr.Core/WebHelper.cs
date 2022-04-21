using System.Net;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace EpgMgr
{
    public class WebHelper
    {
        private string DefaultUserAgent;
        private readonly HttpClient m_httpClient;
        public HttpClient Client => m_httpClient;
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

        // For cases where we can't create a known object type
        // Parse JSON string and return dynamic type
        public dynamic? GetDynamic(string jsonstring)
        {
            return JsonSerializer.Deserialize<dynamic>(jsonstring);
        }

        // Parse known class object and return JSON string
        public string? CreateJSONstring<T>(T obj) => JsonSerializer.Serialize<T>(obj);

        // Parse JSON string and return known class object
        public T? ParseJSON<T>(string input) => JsonSerializer.Deserialize<T>(input);

        // Parse incoming known object with JSON serializer.
        // Perform post method and parse response via JSON serializer to known object type
        public V? PostJSON<V, T>(string command, T obj, Dictionary<string, string>? headers = null)
        {
            var requestString = CreateJSONstring(obj);
            if (requestString == null) return default;
            var response = WebPost(command, requestString, headers);
            var result = ParseJSON<V>(response);
            return result;
        }

        // Perform get method and parse response via JSON serializer to known object type
        public T? GetJSON<T>(string command, Dictionary<string, string>? headers = null) => ParseJSON<T>(WebGet(command, headers));

        // Perform put method and parse response via JSON serializer to known object type
        public T? PutJSON<T>(string command, Dictionary<string, string>? headers = null) => ParseJSON<T>(WebPut(command, headers));
    
        public T? DeleteJSON<T>(string command, Dictionary<string, string>? headers = null) => ParseJSON<T>(WebDelete(command, headers));

        // Handle get request, return response as string
        public string WebGet(string uri, Dictionary<string, string>? headers = null)
        {
            var postResponse = WebAction(uri, null, "GET", headers);
            using var resp = new StreamReader(postResponse.Content.ReadAsStream());
            return resp.ReadToEnd();
        }

        // Handle put request, return response as string
        public string WebPut(string uri, Dictionary<string, string>? headers = null)
        {
            var postResponse = WebAction(uri, null, "PUT", headers);
            using var resp = new StreamReader(postResponse.Content.ReadAsStream());
            return resp.ReadToEnd();
        }

        // Handle delete request, return response as string
        public string WebDelete(string uri, Dictionary<string, string>? headers = null)
        {
            var postResponse = WebAction(uri, null, "DELETE", headers);
            using var resp = new StreamReader(postResponse.Content.ReadAsStream());
            return resp.ReadToEnd();
        }

        // Handle post request, return response as string
        public string WebPost(string uri, string jsonstring, Dictionary<string, string>? headers = null)
        {
            var postResponse = WebAction(uri, jsonstring, "POST", headers);
            using var resp = new StreamReader(postResponse.Content.ReadAsStream());
            return resp.ReadToEnd();
        }

        // Create web request for specified method and URL
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

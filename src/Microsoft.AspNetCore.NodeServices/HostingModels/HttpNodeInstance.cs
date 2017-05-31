using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.AspNetCore.NodeServices.HostingModels
{
    /// <summary>
    /// A specialisation of the OutOfProcessNodeInstance base class that uses HTTP to perform RPC invocations.
    ///
    /// The Node child process starts an HTTP listener on an arbitrary available port (except where a nonzero
    /// port number is specified as a constructor parameter), and signals which port was selected using the same
    /// input/output-based mechanism that the base class uses to determine when the child process is ready to
    /// accept RPC invocations.
    /// </summary>
    /// <seealso cref="Microsoft.AspNetCore.NodeServices.HostingModels.OutOfProcessNodeInstance" />
    internal class HttpNodeInstance : OutOfProcessNodeInstance
    {
        private readonly static int streamBufferSize = 16 * 1024;
        private static readonly Regex PortMessageRegex =
            new Regex(@"^\[Microsoft.AspNetCore.NodeServices.HttpNodeHost:Listening on port (\d+)\]$");

        private static readonly JsonSerializerSettings jsonSerializerSettings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            TypeNameHandling = TypeNameHandling.None
        };

        private readonly HttpClient _client;
        private bool _disposed;
        private int _portNumber;

        public HttpNodeInstance(NodeServicesOptions options, int port = 0)
        : base(
                EmbeddedResourceReader.Read(
                    typeof(HttpNodeInstance),
                    "/Content/Node/entrypoint-http.js"),
                options.ProjectPath,
                options.WatchFileExtensions,
                MakeCommandLineOptions(port),
                options.NodeInstanceOutputLogger,
                options.EnvironmentVariables,
                options.InvocationTimeoutMilliseconds,
                options.LaunchWithDebugging,
                options.DebuggingPort)
        {
            _client = new HttpClient();
        }

        private static string MakeCommandLineOptions(int port)
        {
            return $"--port {port}";
        }

        protected override async Task<T> InvokeExportAsync<T>(
            NodeInvocationInfo invocationInfo, CancellationToken cancellationToken)
        {
            var payloadJson = JsonConvert.SerializeObject(invocationInfo, jsonSerializerSettings);
            var payload = new StringContent(payloadJson, Encoding.UTF8, "application/json");
            var response = await _client.PostAsync("http://localhost:" + _portNumber, payload, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                // Unfortunately there's no true way to cancel ReadAsStringAsync calls, hence AbandonIfCancelled
                var responseJson = await response.Content.ReadAsStringAsync().OrThrowOnCancellation(cancellationToken);
                var responseError = JsonConvert.DeserializeObject<RpcJsonResponse>(responseJson, jsonSerializerSettings);

                throw new NodeInvocationException(responseError.ErrorMessage, responseError.ErrorDetails);
            }

            var responseContentType = response.Content.Headers.ContentType;
            switch (responseContentType.MediaType)
            {
                case "text/plain":
                    // String responses can skip JSON encoding/decoding
                    if (typeof(T) != typeof(string))
                    {
                        throw new ArgumentException(
                            "Node module responded with non-JSON string. This cannot be converted to the requested generic type: " +
                            typeof(T).FullName);
                    }

                    var responseString = await response.Content.ReadAsStringAsync().OrThrowOnCancellation(cancellationToken);
                    return (T)(object)responseString;

                case "application/json":
                    var responseJson = await response.Content.ReadAsStringAsync().OrThrowOnCancellation(cancellationToken);
                    return JsonConvert.DeserializeObject<T>(responseJson, jsonSerializerSettings);

                case "application/octet-stream":
                    // Streamed responses have to be received as System.IO.Stream instances
                    if (typeof(T) != typeof(Stream) && typeof(T) != typeof(object))
                    {
                        throw new ArgumentException(
                            "Node module responded with binary stream. This cannot be converted to the requested generic type: " +
                            typeof(T).FullName + ". Instead you must use the generic type System.IO.Stream.");
                    }

                    return (T)(object)(await response.Content.ReadAsStreamAsync().OrThrowOnCancellation(cancellationToken));

                default:
                    throw new InvalidOperationException("Unexpected response content type: " + responseContentType.MediaType);
            }
        }

        protected override void OnOutputDataReceived(string outputData)
        {
            // Watch for "port selected" messages, and when observed, store the port number
            // so we can use it when making HTTP requests. The child process will always send
            // one of these messages before it sends a "ready for connections" message.
            var match = _portNumber != 0 ? null : PortMessageRegex.Match(outputData);
            if (match != null && match.Success)
            {
                _portNumber = int.Parse(match.Groups[1].Captures[0].Value);
            }
            else
            {
                base.OnOutputDataReceived(outputData);
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (!_disposed)
            {
                if (disposing)
                {
                    _client.Dispose();
                }

                _disposed = true;
            }
        }

        private static async Task<T> ReadJsonAsync<T>(Stream stream, CancellationToken cancellationToken)
        {
            var json = Encoding.UTF8.GetString(await ReadAllBytesAsync(stream, cancellationToken));
            return JsonConvert.DeserializeObject<T>(json, jsonSerializerSettings);
        }

        private static async Task<byte[]> ReadAllBytesAsync(Stream input, CancellationToken cancellationToken)
        {
            byte[] buffer = new byte[streamBufferSize];

            using (var ms = new MemoryStream())
            {
                int read;
                while ((read = await input.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
                {
                    ms.Write(buffer, 0, read);
                }

                return ms.ToArray();
            }
        }

#pragma warning disable 649 // These properties are populated via JSON deserialization
        private class RpcJsonResponse
        {
            public string ErrorMessage { get; set; }
            public string ErrorDetails { get; set; }
        }
#pragma warning restore 649
    }
}

// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Http;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.SpaServices.Extensions.Proxy
{
    // This duplicates and updates the proxying logic in SpaServices so that we can update
    // the project templates without waiting for 2.1 to ship. When 2.1 is ready to ship,
    // merge the additional proxying features (e.g., proxying websocket connections) back
    // into the SpaServices proxying code. It's all internal.
    internal static class ConditionalProxy
    {
        private const int DefaultWebSocketBufferSize = 4096;
        private const int StreamCopyBufferSize = 81920;

        private static readonly string[] NotForwardedWebSocketHeaders = new[] { "Connection", "Host", "Upgrade", "Sec-WebSocket-Key", "Sec-WebSocket-Version" };

        public static HttpClient CreateHttpClientForProxy(TimeSpan requestTimeout)
        {
            var handler = new HttpClientHandler
            {
                AllowAutoRedirect = false,
                UseCookies = false,
                
            };

            return new HttpClient(handler)
            {
                Timeout = requestTimeout
            };
        }

        public static async Task<bool> PerformProxyRequest(
            HttpContext context,
            HttpClient httpClient,
            Task<ConditionalProxyMiddlewareTarget> targetTask,
            CancellationToken applicationStoppingToken)
        {
            // Stop proxying if either the server or client wants to disconnect
            var proxyCancellationToken = CancellationTokenSource.CreateLinkedTokenSource(
                context.RequestAborted,
                applicationStoppingToken).Token;

            // We allow for the case where the target isn't known ahead of time, and want to
            // delay proxied requests until the target becomes known. This is useful, for example,
            // when proxying to Angular CLI middleware: we won't know what port it's listening
            // on until it finishes starting up.
            var target = await targetTask;
            var targetUri = new UriBuilder(
                target.Scheme,
                target.Host,
                int.Parse(target.Port),
                context.Request.Path,
                context.Request.QueryString.Value).Uri;

            try
            {
                if (context.WebSockets.IsWebSocketRequest)
                {
                    await AcceptProxyWebSocketRequest(context, ToWebSocketScheme(targetUri), proxyCancellationToken);
                    return true;
                }
                else
                {
                    using (var requestMessage = CreateProxyHttpRequest(context, targetUri))
                    using (var responseMessage = await SendProxyHttpRequest(context, httpClient, requestMessage, proxyCancellationToken))
                    {
                        return await CopyProxyHttpResponse(context, responseMessage, proxyCancellationToken);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // If we're aborting because either the client disconnected, or the server
                // is shutting down, don't treat this as an error.
                return true;
            }
            catch (IOException)
            {
                // This kind of exception can also occur if a proxy read/write gets interrupted
                // due to the process shutting down.
                return true;
            }
        }

        private static HttpRequestMessage CreateProxyHttpRequest(HttpContext context, Uri uri)
        {
            var request = context.Request;

            var requestMessage = new HttpRequestMessage();
            var requestMethod = request.Method;
            if (!HttpMethods.IsGet(requestMethod) &&
                !HttpMethods.IsHead(requestMethod) &&
                !HttpMethods.IsDelete(requestMethod) &&
                !HttpMethods.IsTrace(requestMethod))
            {
                var streamContent = new StreamContent(request.Body);
                requestMessage.Content = streamContent;
            }

            // Copy the request headers
            foreach (var header in request.Headers)
            {
                if (!requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray()) && requestMessage.Content != null)
                {
                    requestMessage.Content?.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
                }
            }

            requestMessage.Headers.Host = uri.Authority;
            requestMessage.RequestUri = uri;
            requestMessage.Method = new HttpMethod(request.Method);

            return requestMessage;
        }

        private static Task<HttpResponseMessage> SendProxyHttpRequest(HttpContext context, HttpClient httpClient, HttpRequestMessage requestMessage, CancellationToken cancellationToken)
        {
            if (requestMessage == null)
            {
                throw new ArgumentNullException(nameof(requestMessage));
            }

            return httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        }

        private static async Task<bool> CopyProxyHttpResponse(HttpContext context, HttpResponseMessage responseMessage, CancellationToken cancellationToken)
        {
            if (responseMessage.StatusCode == HttpStatusCode.NotFound)
            {
                // Let some other middleware handle this
                return false;
            }

            // We can handle this
            context.Response.StatusCode = (int)responseMessage.StatusCode;
            foreach (var header in responseMessage.Headers)
            {
                context.Response.Headers[header.Key] = header.Value.ToArray();
            }

            foreach (var header in responseMessage.Content.Headers)
            {
                context.Response.Headers[header.Key] = header.Value.ToArray();
            }

            // SendAsync removes chunking from the response. This removes the header so it doesn't expect a chunked response.
            context.Response.Headers.Remove("transfer-encoding");

            using (var responseStream = await responseMessage.Content.ReadAsStreamAsync())
            {
                await responseStream.CopyToAsync(context.Response.Body, StreamCopyBufferSize, cancellationToken);
            }

            return true;
        }

        private static Uri ToWebSocketScheme(Uri uri)
        {
            if (uri == null)
            {
                throw new ArgumentNullException(nameof(uri));
            }

            var uriBuilder = new UriBuilder(uri);
            if (string.Equals(uriBuilder.Scheme, "https", StringComparison.OrdinalIgnoreCase))
            {
                uriBuilder.Scheme = "wss";
            }
            else if (string.Equals(uriBuilder.Scheme, "http", StringComparison.OrdinalIgnoreCase))
            {
                uriBuilder.Scheme = "ws";
            }

            return uriBuilder.Uri;
        }

        private static async Task<bool> AcceptProxyWebSocketRequest(HttpContext context, Uri destinationUri, CancellationToken cancellationToken)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }
            if (destinationUri == null)
            {
                throw new ArgumentNullException(nameof(destinationUri));
            }
            if (!context.WebSockets.IsWebSocketRequest)
            {
                throw new InvalidOperationException();
            }

            using (var client = new ClientWebSocket())
            {
                foreach (var headerEntry in context.Request.Headers)
                {
                    if (!NotForwardedWebSocketHeaders.Contains(headerEntry.Key, StringComparer.OrdinalIgnoreCase))
                    {
                        client.Options.SetRequestHeader(headerEntry.Key, headerEntry.Value);
                    }
                }

                try
                {
                    // Note that this is not really good enough to make Websockets work with
                    // Angular CLI middleware. For some reason, ConnectAsync takes over 1 second,
                    // by which time the logic in SockJS has already timed out and made it fall
                    // back on some other transport (xhr_streaming, usually). This is not a problem,
                    // because the transport fallback logic works correctly and doesn't surface any
                    // errors, but it would be better if ConnectAsync was fast enough and the
                    // initial Websocket transport could actually be used.
                    await client.ConnectAsync(destinationUri, cancellationToken);
                }
                catch (WebSocketException)
                {
                    context.Response.StatusCode = 400;
                    return false;
                }

                using (var server = await context.WebSockets.AcceptWebSocketAsync(client.SubProtocol))
                {
                    var bufferSize = DefaultWebSocketBufferSize;
                    await Task.WhenAll(
                        PumpWebSocket(client, server, bufferSize, cancellationToken),
                        PumpWebSocket(server, client, bufferSize, cancellationToken));
                }

                return true;
            }
        }

        private static async Task PumpWebSocket(WebSocket source, WebSocket destination, int bufferSize, CancellationToken cancellationToken)
        {
            if (bufferSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(bufferSize));
            }

            var buffer = new byte[bufferSize];

            while (true)
            {
                var result = await source.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    if (destination.State == WebSocketState.Open || destination.State == WebSocketState.CloseReceived)
                    {
                        await destination.CloseOutputAsync(source.CloseStatus.Value, source.CloseStatusDescription, cancellationToken);
                    }

                    return;
                }

                await destination.SendAsync(new ArraySegment<byte>(buffer, 0, result.Count), result.MessageType, result.EndOfMessage, cancellationToken);
            }
        }
    }
}

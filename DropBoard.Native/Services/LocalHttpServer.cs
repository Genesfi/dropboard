using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;

namespace DropBoard.Native.Services
{
    public class LocalHttpServer
    {
        private HttpListener? _listener;
        private CancellationTokenSource? _cts;
        private readonly int _port;
        private readonly Action<string, string> _onAddImage;
        private readonly HttpClient _httpClient = new();
        private readonly YoutubeClient _ytClient = new();
        private readonly ConcurrentDictionary<string, (string Url, long Size, DateTime ExpiresAt)> _streamCache = new();
        public int Port => _port;

        public LocalHttpServer(int port, Action<string, string> onAddImage)
        {
            _port = port;
            _onAddImage = onAddImage;
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        }

        public void Start()
        {
            try
            {
                _listener = new HttpListener();
                _listener.Prefixes.Add($"http://127.0.0.1:{_port}/");
                _listener.Prefixes.Add($"http://localhost:{_port}/");
                _listener.Start();

                _cts = new CancellationTokenSource();
                Task.Run(() => ListenLoop(_cts.Token));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LocalHttpServer Start failed: {ex.Message}");
            }
        }

        public void Stop()
        {
            try
            {
                _cts?.Cancel();
                _listener?.Stop();
                _listener?.Close();
            }
            catch { }
        }

        private async Task ListenLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested && _listener != null && _listener.IsListening)
            {
                try
                {
                    HttpListenerContext context = await _listener.GetContextAsync();
                    _ = ProcessRequestAsync(context);
                }
                catch (HttpListenerException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"HttpListener loop error: {ex.Message}");
                }
            }
        }

        private async Task ProcessRequestAsync(HttpListenerContext context)
        {
            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;

            // Apply CORS headers matching original DropBoard C++ server
            response.Headers.Add("Access-Control-Allow-Origin", "*");
            response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
            response.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Authorization");

            try
            {
                if (request.HttpMethod == "OPTIONS")
                {
                    response.StatusCode = 204;
                    response.Close();
                    return;
                }

                string rawUrl = request.Url?.AbsolutePath.ToLowerInvariant() ?? "";

                if (rawUrl == "/ping" && request.HttpMethod == "GET")
                {
                    byte[] pingBytes = Encoding.UTF8.GetBytes("{\"status\":\"ready\",\"app\":\"DropBoard\",\"version\":\"2.0.0\"}");
                    response.ContentType = "application/json";
                    response.StatusCode = 200;
                    response.ContentLength64 = pingBytes.Length;
                    await response.OutputStream.WriteAsync(pingBytes);
                    response.Close();
                    return;
                }

                if (rawUrl == "/yt" && request.HttpMethod == "GET")
                {
                    string ytId = request.QueryString["id"] ?? "";
                    string html = $@"<!DOCTYPE html>
<html>
<head>
  <meta charset=""utf-8"">
  <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
  <meta name=""referrer"" content=""strict-origin-when-cross-origin"">
  <title>YouTube Player</title>
  <style>
    * {{ margin: 0; padding: 0; box-sizing: border-box; }}
    html, body {{ width: 100%; height: 100%; overflow: hidden; background: #0F1117; }}
    iframe {{ border: none; width: 100%; height: 100%; display: block; }}
  </style>
</head>
<body>
  <iframe 
    id=""ytplayer""
    src=""https://www.youtube.com/embed/{ytId}?autoplay=1&playsinline=1&rel=0&modestbranding=1&enablejsapi=1""
    referrerpolicy=""strict-origin-when-cross-origin""
    allow=""accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture; web-share""
    allowfullscreen>
  </iframe>
</body>
</html>";
                    byte[] htmlBytes = Encoding.UTF8.GetBytes(html);
                    response.ContentType = "text/html; charset=utf-8";
                    response.StatusCode = 200;
                    response.ContentLength64 = htmlBytes.Length;
                    await response.OutputStream.WriteAsync(htmlBytes);
                    response.Close();
                    return;
                }

                if (rawUrl == "/ytstream" && (request.HttpMethod == "GET" || request.HttpMethod == "HEAD"))
                {
                    string ytId = request.QueryString["id"] ?? "";
                    if (string.IsNullOrEmpty(ytId))
                    {
                        response.StatusCode = 400;
                        response.Close();
                        return;
                    }

                    string? streamUrl = null;
                    long totalSize = 0;

                    if (_streamCache.TryGetValue(ytId, out var cached) && cached.ExpiresAt > DateTime.UtcNow)
                    {
                        streamUrl = cached.Url;
                        totalSize = cached.Size;
                    }
                    else
                    {
                        try
                        {
                            var manifest = await _ytClient.Videos.Streams.GetManifestAsync(ytId);
                            var muxed = manifest.GetMuxedStreams()
                                .Where(s => s.Container == Container.Mp4)
                                .OrderByDescending(s => s.VideoQuality.MaxHeight)
                                .FirstOrDefault()
                                ?? manifest.GetMuxedStreams().OrderByDescending(s => s.VideoQuality.MaxHeight).FirstOrDefault();

                            if (muxed != null)
                            {
                                streamUrl = muxed.Url;
                                totalSize = muxed.Size.Bytes;
                                _streamCache[ytId] = (streamUrl, totalSize, DateTime.UtcNow.AddMinutes(20));
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[StreamProxy] Error resolving stream: {ex.Message}");
                        }
                    }

                    if (string.IsNullOrEmpty(streamUrl))
                    {
                        response.StatusCode = 404;
                        response.Close();
                        return;
                    }

                    try
                    {
                        if (request.HttpMethod == "HEAD")
                        {
                            response.StatusCode = 200;
                            response.ContentType = "video/mp4";
                            response.Headers["Accept-Ranges"] = "bytes";
                            if (totalSize > 0) response.ContentLength64 = totalSize;
                            response.Close();
                            return;
                        }

                        using var proxyReq = new HttpRequestMessage(HttpMethod.Get, streamUrl);
                        proxyReq.Headers.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");

                        string? range = request.Headers["Range"];
                        if (!string.IsNullOrEmpty(range))
                        {
                            proxyReq.Headers.TryAddWithoutValidation("Range", range);
                        }

                        using var ytResp = await _httpClient.SendAsync(proxyReq, HttpCompletionOption.ResponseHeadersRead);

                        response.StatusCode = (int)ytResp.StatusCode;
                        response.ContentType = "video/mp4";
                        response.Headers["Accept-Ranges"] = "bytes";

                        if (ytResp.Content.Headers.ContentRange != null)
                        {
                            response.Headers["Content-Range"] = ytResp.Content.Headers.ContentRange.ToString();
                        }
                        if (ytResp.Content.Headers.ContentLength.HasValue)
                        {
                            response.ContentLength64 = ytResp.Content.Headers.ContentLength.Value;
                        }

                        using var ytStream = await ytResp.Content.ReadAsStreamAsync();
                        byte[] buffer = new byte[65536];
                        int bytesRead;
                        while ((bytesRead = await ytStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            await response.OutputStream.WriteAsync(buffer, 0, bytesRead);
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[StreamProxy] Streaming aborted or client closed: {ex.Message}");
                    }
                    finally
                    {
                        try { response.Close(); } catch { }
                    }
                    return;
                }

                if (rawUrl == "/add" && request.HttpMethod == "POST")
                {
                    using StreamReader reader = new StreamReader(request.InputStream, request.ContentEncoding);
                    string body = await reader.ReadToEndAsync();

                    string imgUrl = "";
                    string imgTitle = "";

                    try
                    {
                        using JsonDocument doc = JsonDocument.Parse(body);
                        if (doc.RootElement.TryGetProperty("url", out JsonElement urlEl))
                            imgUrl = urlEl.GetString() ?? "";
                        if (doc.RootElement.TryGetProperty("title", out JsonElement titleEl))
                            imgTitle = titleEl.GetString() ?? "";
                    }
                    catch { }

                    if (!string.IsNullOrEmpty(imgUrl))
                    {
                        _onAddImage(imgUrl, imgTitle);

                        byte[] okBytes = Encoding.UTF8.GetBytes("{\"success\":true,\"message\":\"Added to DropBoard\"}");
                        response.ContentType = "application/json";
                        response.StatusCode = 200;
                        response.ContentLength64 = okBytes.Length;
                        await response.OutputStream.WriteAsync(okBytes);
                    }
                    else
                    {
                        byte[] failBytes = Encoding.UTF8.GetBytes("{\"success\":false,\"message\":\"No URL provided\"}");
                        response.ContentType = "application/json";
                        response.StatusCode = 400;
                        response.ContentLength64 = failBytes.Length;
                        await response.OutputStream.WriteAsync(failBytes);
                    }
                    response.Close();
                    return;
                }

                response.StatusCode = 404;
                response.Close();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error handling HTTP request: {ex.Message}");
                try
                {
                    response.StatusCode = 500;
                    response.Close();
                }
                catch { }
            }
        }
    }
}

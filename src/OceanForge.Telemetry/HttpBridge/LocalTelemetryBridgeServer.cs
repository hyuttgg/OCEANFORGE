using System;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using OceanForge.Contracts.Telemetry;
using OceanForge.Telemetry.Services;

namespace OceanForge.Telemetry.HttpBridge;

/// <summary>
/// High-speed local HTTP Bridge listening for incoming telemetry from Roblox executors running sender copy.lua.
/// Default endpoints: /api/lua/update and /api/webhook/roblox
/// </summary>
public class LocalTelemetryBridgeServer : IAsyncDisposable
{
    private readonly ITelemetryIngestionService _ingestionService;
    private readonly HttpListener _listener;
    private readonly CancellationTokenSource _cts = new();
    private Task? _listenerLoopTask;
    private bool _isRunning;

    public int Port { get; }
    public bool IsRunning => _isRunning;

    public LocalTelemetryBridgeServer(ITelemetryIngestionService ingestionService, int port = 5088)
    {
        _ingestionService = ingestionService;
        Port = port;
        _listener = new HttpListener();
        _listener.Prefixes.Add($"http://127.0.0.1:{Port}/");
        _listener.Prefixes.Add($"http://localhost:{Port}/");
    }

    public void Start()
    {
        if (_isRunning) return;

        try
        {
            _listener.Start();
            _isRunning = true;
            _listenerLoopTask = Task.Run(ListenLoopAsync);
        }
        catch (HttpListenerException)
        {
            // Port might be in use or permission restricted; fallback gracefully
            _isRunning = false;
        }
    }

    public void Stop()
    {
        if (!_isRunning) return;

        _isRunning = false;
        _cts.Cancel();
        try
        {
            _listener.Stop();
        }
        catch { }
    }

    private async Task ListenLoopAsync()
    {
        while (_isRunning && !_cts.IsCancellationRequested)
        {
            try
            {
                var context = await _listener.GetContextAsync();
                _ = Task.Run(() => HandleRequestAsync(context));
            }
            catch (HttpListenerException)
            {
                break;
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception)
            {
                // Continue loop
            }
        }
    }

    private async Task HandleRequestAsync(HttpListenerContext context)
    {
        var req = context.Request;
        var resp = context.Response;

        // CORS headers for local executor compatibility
        resp.Headers.Add("Access-Control-Allow-Origin", "*");
        resp.Headers.Add("Access-Control-Allow-Methods", "POST, GET, OPTIONS");
        resp.Headers.Add("Access-Control-Allow-Headers", "Content-Type, x-api-key, x-csharp-accelerated, x-csharp-dedup-hash, x-csharp-state-unchanged");

        if (req.HttpMethod.Equals("OPTIONS", StringComparison.OrdinalIgnoreCase))
        {
            resp.StatusCode = (int)HttpStatusCode.NoContent;
            resp.Close();
            return;
        }

        string path = req.Url?.AbsolutePath ?? string.Empty;

        // Health check endpoint
        if (req.HttpMethod.Equals("GET", StringComparison.OrdinalIgnoreCase) && path.Equals("/health", StringComparison.OrdinalIgnoreCase))
        {
            resp.ContentType = "application/json";
            resp.StatusCode = (int)HttpStatusCode.OK;
            string healthJson = JsonSerializer.Serialize(new
            {
                status = "OK",
                engine = "OceanForge C# Engine",
                packetsPerSecond = _ingestionService.CurrentPacketsPerSecond,
                totalPackets = _ingestionService.TotalPacketsIngested
            });
            byte[] bytes = Encoding.UTF8.GetBytes(healthJson);
            await resp.OutputStream.WriteAsync(bytes);
            resp.Close();
            return;
        }

        // Telemetry Ingestion endpoints matching sender copy.lua
        if (req.HttpMethod.Equals("POST", StringComparison.OrdinalIgnoreCase) &&
            (path.Equals("/api/lua/update", StringComparison.OrdinalIgnoreCase) ||
             path.Equals("/api/webhook/roblox", StringComparison.OrdinalIgnoreCase)))
        {
            try
            {
                using var reader = new StreamReader(req.InputStream, req.ContentEncoding);
                string jsonBody = await reader.ReadToEndAsync();

                if (!string.IsNullOrWhiteSpace(jsonBody))
                {
                    var payload = JsonSerializer.Deserialize<RobloxTelemetryPayload>(jsonBody, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (payload != null)
                    {
                        var result = await _ingestionService.IngestAsync(payload);

                        resp.ContentType = "application/json";
                        resp.StatusCode = (int)HttpStatusCode.OK;
                        string resJson = JsonSerializer.Serialize(result);
                        byte[] bytes = Encoding.UTF8.GetBytes(resJson);
                        await resp.OutputStream.WriteAsync(bytes);
                        resp.Close();
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                resp.StatusCode = (int)HttpStatusCode.BadRequest;
                byte[] bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { error = ex.Message }));
                await resp.OutputStream.WriteAsync(bytes);
                resp.Close();
                return;
            }
        }

        resp.StatusCode = (int)HttpStatusCode.NotFound;
        resp.Close();
    }

    public async ValueTask DisposeAsync()
    {
        Stop();
        if (_listenerLoopTask != null)
        {
            try { await _listenerLoopTask; } catch { }
        }
        _listener.Close();
        _cts.Dispose();
    }
}

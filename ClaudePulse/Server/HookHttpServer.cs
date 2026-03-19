using System.Net;
using System.Text;
using System.Text.Json;
using ClaudePulse.Models;

namespace ClaudePulse.Server;

public class HookHttpServer : IDisposable
{
    public event Action<HookEvent>? OnHookEvent;
    public int Port { get; private set; }

    private HttpListener? _listener;
    private CancellationTokenSource? _cts;
    private readonly SynchronizationContext _syncContext;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public HookHttpServer(SynchronizationContext syncContext)
    {
        _syncContext = syncContext;
    }

    public bool Start()
    {
        for (int port = 19280; port <= 19289; port++)
        {
            try
            {
                _listener = new HttpListener();
                _listener.Prefixes.Add($"http://localhost:{port}/");
                _listener.Start();
                Port = port;
                _cts = new CancellationTokenSource();
                _ = ListenLoop(_cts.Token);
                return true;
            }
            catch (Exception)
            {
                _listener?.Close();
                _listener = null;
            }
        }
        return false;
    }

    private async Task ListenLoop(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested && _listener != null)
        {
            try
            {
                var context = await _listener.GetContextAsync();
                _ = HandleRequest(context);
            }
            catch (ObjectDisposedException) { break; }
            catch (HttpListenerException) { break; }
            catch (OperationCanceledException) { break; }
        }
    }

    private async Task HandleRequest(HttpListenerContext context)
    {
        try
        {
            var request = context.Request;
            var response = context.Response;

            if (request.HttpMethod == "POST" && request.HasEntityBody)
            {
                using var reader = new StreamReader(request.InputStream, Encoding.UTF8);
                var body = await reader.ReadToEndAsync();

                try
                {
                    var hookEvent = JsonSerializer.Deserialize<HookEvent>(body, JsonOptions);
                    if (hookEvent != null && !string.IsNullOrEmpty(hookEvent.HookEventName))
                    {
                        _syncContext.Post(_ => OnHookEvent?.Invoke(hookEvent), null);
                    }
                }
                catch (JsonException)
                {
                    // Malformed JSON - ignore
                }
            }

            response.StatusCode = 200;
            response.ContentLength64 = 0;
            response.Close();
        }
        catch (Exception)
        {
            // Connection closed or other error - ignore
        }
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _listener?.Stop();
        _listener?.Close();
        _cts?.Dispose();
    }
}

using Microsoft.AspNetCore.Mvc;

namespace ChatCompletion.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatCompletionController : ControllerBase
    {
        private readonly ILogger<ChatCompletionController> _logger;

        private readonly string[] dotNetTerms = new string[]
        {
            ".NET Core", ".NET 5", ".NET 6", ".NET 7", ".NET 8", "ASP.NET",
            "Entity Framework", "LINQ", "C#", "Visual Studio", "NuGet",
            "Blazor", "Razor Pages", "SignalR", "Web API", "MVC",
            "Middleware", "Dependency Injection", "IServiceCollection", "ILogger", "IConfiguration",
            "HttpClient", "AppSettings", "Kestrel", "EF Core", "Identity",
            "Authorization", "Authentication", "JWT", "Task", "async",
            "await", "IActionResult", "Controller", "ViewModel", "Model Binding",
            "Minimal APIs", "Hot Reload", "Source Generators", "Records", "Pattern Matching"
        };

        public ChatCompletionController(ILogger<ChatCompletionController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return Ok("Chat Completion API is running");
        }

        [HttpGet("/stream-dotnet-terms")]
        public async Task StreamDotNetTerms(CancellationToken cancellationToken = default)
        {
            try
            {
                // Set up response headers for streaming
                Response.ContentType = "text/event-stream";
                Response.Headers.Add("Cache-Control", "no-cache");
                Response.Headers.Add("Connection", "keep-alive");
                Response.Headers.Add("Access-Control-Allow-Origin", "*");
                Response.Headers.Add("X-Accel-Buffering", "no"); // Disable nginx buffering

                // Disable response buffering
                var feature = HttpContext.Features.Get<Microsoft.AspNetCore.Http.Features.IHttpResponseBodyFeature>();
                feature?.DisableBuffering();

                _logger.LogInformation("Starting to stream .NET terms");

                // Send initial connection event
                await WriteEventAsync("Connected to .NET terms stream", cancellationToken);

                // Stream each term with a delay
                foreach (var term in dotNetTerms)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        _logger.LogInformation("Streaming cancelled by client");
                        break;
                    }

                    await WriteEventAsync(term, cancellationToken);
                    await Task.Delay(2000, cancellationToken); // 2 second delay between terms
                }

                // Send completion event
                if (!cancellationToken.IsCancellationRequested)
                {
                    await WriteEventAsync("Stream completed", cancellationToken);
                }

                _logger.LogInformation("Finished streaming .NET terms");
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Streaming operation was cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while streaming");

                try
                {
                    await WriteEventAsync($"Error: {ex.Message}", cancellationToken);
                }
                catch
                {
                    // If we can't write the error, the connection is likely broken
                }
            }
        }

        [HttpPost("/stream-custom-terms")]
        public async Task StreamCustomTerms([FromBody] StreamRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                Response.ContentType = "text/event-stream";
                Response.Headers.Add("Cache-Control", "no-cache");
                Response.Headers.Add("Connection", "keep-alive");
                Response.Headers.Add("Access-Control-Allow-Origin", "*");

                var feature = HttpContext.Features.Get<Microsoft.AspNetCore.Http.Features.IHttpResponseBodyFeature>();
                feature?.DisableBuffering();

                var terms = request?.Terms ?? dotNetTerms;
                var delay = request?.DelayMs ?? 2000;

                _logger.LogInformation("Starting to stream {Count} custom terms", terms.Length);

                foreach (var term in terms)
                {
                    if (cancellationToken.IsCancellationRequested) break;

                    await WriteEventAsync(term, cancellationToken);
                    await Task.Delay(delay, cancellationToken);
                }

                if (!cancellationToken.IsCancellationRequested)
                {
                    await WriteEventAsync("Custom stream completed", cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Custom streaming operation was cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while streaming custom terms");
            }
        }

        private async Task WriteEventAsync(string data, CancellationToken cancellationToken = default)
        {
            var message = $"data: {data}\n\n";
            await Response.WriteAsync(message, cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);
        }
    }

    public class StreamRequest
    {
        public string[] Terms { get; set; } = Array.Empty<string>();
        public int DelayMs { get; set; } = 500;
    }
}
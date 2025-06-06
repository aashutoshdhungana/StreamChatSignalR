using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Text;
using Microsoft.SemanticKernel;
using ChatCompletion.AIHelpers;

namespace ChatCompletion.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StreamController : ControllerBase
    {
        private readonly Kernel _kernel;
        public StreamController(Kernel kernel)
        {
            _kernel = kernel;
        }

        [HttpGet("strings")]
        public async Task StreamStrings(string message, CancellationToken cancellationToken = default)
        {
            Response.Headers.Add("Content-Type", "application/json");
            Response.Headers.Add("Cache-Control", "no-cache");
            Response.Headers.Add("Connection", "keep-alive");

            var response = _kernel.CompleteChat(message);

            await Response.StartAsync(cancellationToken);
            await foreach (var str in response)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                var jsonData = JsonSerializer.Serialize(new { Message = str, Timestamp = DateTime.UtcNow });
                var bytes = Encoding.UTF8.GetBytes(jsonData + "\n");

                await Response.Body.WriteAsync(bytes, 0, bytes.Length, cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);
            }
            await Response.CompleteAsync();
        }

        [HttpGet("strings-sse")]
        public async Task StreamStringsSSE(string message, CancellationToken cancellationToken = default)
        {
            Response.Headers.Add("Content-Type", "text/event-stream");
            Response.Headers.Add("Cache-Control", "no-cache");
            Response.Headers.Add("Connection", "keep-alive");
            Response.Headers.Add("Access-Control-Allow-Origin", "*");

            var strings = _kernel.CompleteChat(message);

            await Response.StartAsync(cancellationToken);

            await foreach (var str in strings)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                var sseData = $"data: {JsonSerializer.Serialize(new { Message = str, Timestamp = DateTime.UtcNow })}\n\n";
                var bytes = Encoding.UTF8.GetBytes(sseData);

                await Response.Body.WriteAsync(bytes, 0, bytes.Length, cancellationToken);
                await Response.Body.FlushAsync(cancellationToken);

                await Task.Delay(1000, cancellationToken);
            }

            // Send end event
            var endData = "data: {\"end\": true}\n\n";
            var endBytes = Encoding.UTF8.GetBytes(endData);
            await Response.Body.WriteAsync(endBytes, 0, endBytes.Length, cancellationToken);
            await Response.Body.FlushAsync(cancellationToken);

            await Response.CompleteAsync();
        }
    }
}

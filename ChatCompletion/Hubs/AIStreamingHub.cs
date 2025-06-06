using ChatCompletion.AIHelpers;
using Microsoft.AspNetCore.SignalR;
using Microsoft.SemanticKernel;
using System.Threading.Tasks;

namespace ChatCompletion.Hubs
{
    public class AIStreamingHub : Hub
    {
        private readonly Kernel _kernel;
        public AIStreamingHub(Kernel kernel)
        {
            _kernel = kernel;
        }
        public async IAsyncEnumerable<string> StreamResponse(string message)
        {
            var response = _kernel.CompleteChat(message);
            await foreach (var chunk in response)
            {
                yield return chunk;
            }
        }
    }
}

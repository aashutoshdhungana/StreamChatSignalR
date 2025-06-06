using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Threading.Tasks;

namespace AILayer
{
    public static class AIHelpers
    {
        public static Kernel GetKernel(string apiKey, string modelId)
        {
            var builder = Kernel.CreateBuilder();
            builder.AddOpenAIChatCompletion(modelId, apiKey);

            var kernel = builder.Build();
            return kernel;
        }

        public static async IAsyncEnumerable<string> GetSuggestion(this Kernel kernel, string userMessage, ChatHistory history)
        {
            var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

            var response = chatCompletionService.GetStreamingChatMessageContentsAsync
            (
                    chatHistory: history,
                    kernel: kernel
            );
            await foreach (var item in response)
            {
                yield return item.Content ?? "";
            }
        }
    }
}

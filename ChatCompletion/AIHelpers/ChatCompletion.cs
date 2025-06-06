using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace ChatCompletion.AIHelpers
{
    public static class ChatCompletion
    {
        public static async IAsyncEnumerable<string> CompleteChat(this Kernel kernel, string message)
        {
            ChatHistory history = new ChatHistory();
            history.AddUserMessage(message);
            var chatCompletionService = kernel.Services.GetRequiredService<IChatCompletionService>();
            var response = chatCompletionService
                .GetStreamingChatMessageContentsAsync(history, kernel: kernel);
            await foreach (var chunk in response)
            {
                yield return chunk.Content ?? string.Empty;
            }
        }
    }
}

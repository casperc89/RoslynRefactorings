using OpenAI.Chat;

namespace RoslynRefactorings.Console.ArgumentTranslation;

internal class OpenAITranslateInputAsync : IArgumentTranslationStrategy
{
    private readonly string _systemPrompt =
        @"
You are translating formatted log4net messages and are given a string literal (possibly interpolated).
Translate the following code from Finnish to English. 
Always keep all placeholders and only respond with the translation as code.

Examples:
1. `""{0} kyselyn suorittaminen osoitteessa {1} epäonnistui koodilla: {2}""` results in ""{0} executing the query at {1} failed with code: {2}""
2. `""$""{0} kyselyn suorittaminen osoitteessa {1} epäonnistui koodilla: {2}""""` results in $""{0} executing the query at {1} failed with code: {2}""";

    private readonly ChatClient _chatClient = new("gpt-3.5-turbo");

    public async Task<string> TranslateInputAsync(string input)
    {
        var systemMsg = new SystemChatMessage(_systemPrompt);
        
        var userMsg = new UserChatMessage(input);
        var result = await _chatClient.CompleteChatAsync([systemMsg, userMsg]);
        var content = result.Value.Content;
        var responseStr = content.First().Text;

        return responseStr;
    }
}
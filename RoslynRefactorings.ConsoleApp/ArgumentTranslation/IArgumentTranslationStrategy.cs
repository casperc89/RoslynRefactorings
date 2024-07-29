namespace RoslynRefactorings.ConsoleApp.ArgumentTranslation;

internal interface IArgumentTranslationStrategy
{
    Task<string> TranslateInputAsync(string input);
}

internal class NoArgumentTranslationStrategy : IArgumentTranslationStrategy
{
    public Task<string> TranslateInputAsync(string input) => Task.FromResult(input);
}
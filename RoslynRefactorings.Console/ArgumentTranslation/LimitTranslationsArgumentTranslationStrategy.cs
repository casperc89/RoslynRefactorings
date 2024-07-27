namespace RoslynRefactorings.Console.ArgumentTranslation;

internal class LimitTranslationsArgumentTranslationStrategy : IArgumentTranslationStrategy
{
    private readonly IArgumentTranslationStrategy _inner;
    private readonly int _maxNumberOfTranslations;
    private int _numberOfTranslations = 0;
    private readonly NoArgumentTranslationStrategy _noArgumentTranslation;

    public LimitTranslationsArgumentTranslationStrategy(IArgumentTranslationStrategy inner,
        int maxNumberOfTranslations = int.MaxValue)
    {
        _noArgumentTranslation = new NoArgumentTranslationStrategy();
        _inner = inner;
        _maxNumberOfTranslations = maxNumberOfTranslations;
    }

    public Task<string> TranslateInputAsync(string input)
    {
        if (_numberOfTranslations++ < _maxNumberOfTranslations)
            return _inner.TranslateInputAsync(input);
            
        return _noArgumentTranslation.TranslateInputAsync(input);
    }
}
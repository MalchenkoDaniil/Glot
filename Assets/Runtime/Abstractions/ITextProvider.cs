namespace Localization.Abstractions
{
    public interface ITextProvider : ILocalizationEvents
    {
        string GetText(string key);
    }
}

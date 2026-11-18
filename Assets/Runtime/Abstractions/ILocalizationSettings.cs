using System.Collections.Generic;

namespace Runtime.Abstractions
{
    public interface ILocalizationSettings
    {
        string DefaultLanguage { get; }
        IReadOnlyList<string> Languages { get; }   
    }
}
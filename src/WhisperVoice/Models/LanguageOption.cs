using System.Collections.Generic;

namespace WhisperVoice.Models;

public sealed record LanguageOption(string Code, string DisplayName)
{
    public static readonly IReadOnlyList<LanguageOption> All =
    [
        new("auto", "Automatisch / Auto"),
        new("de", "Deutsch"),
        new("en", "English"),
        new("sq", "Shqip (Albanian)"),
        new("ar", "\u0627\u0644\u0639\u0631\u0628\u064A\u0629 (Arabic)"),
        new("zh", "\u4E2D\u6587 (Chinese)"),
        new("hr", "Hrvatski (Croatian)"),
        new("cs", "\u010Ce\u0161tina (Czech)"),
        new("da", "Dansk (Danish)"),
        new("nl", "Nederlands (Dutch)"),
        new("fi", "Suomi (Finnish)"),
        new("fr", "Fran\u00E7ais (French)"),
        new("hi", "\u0939\u093F\u0928\u094D\u0926\u0940 (Hindi)"),
        new("hu", "Magyar (Hungarian)"),
        new("it", "Italiano (Italian)"),
        new("ja", "\u65E5\u672C\u8A9E (Japanese)"),
        new("ko", "\uD55C\uAD6D\uC5B4 (Korean)"),
        new("no", "Norsk (Norwegian)"),
        new("pl", "Polski (Polish)"),
        new("pt", "Portugu\u00EAs (Portuguese)"),
        new("ro", "Rom\u00E2n\u0103 (Romanian)"),
        new("ru", "\u0420\u0443\u0441\u0441\u043A\u0438\u0439 (Russian)"),
        new("sr", "\u0421\u0440\u043F\u0441\u043A\u0438 (Serbian)"),
        new("sk", "Sloven\u010Dina (Slovak)"),
        new("sl", "Sloven\u0161\u010Dina (Slovenian)"),
        new("es", "Espa\u00F1ol (Spanish)"),
        new("sv", "Svenska (Swedish)"),
        new("tr", "T\u00FCrk\u00E7e (Turkish)"),
        new("uk", "\u0423\u043A\u0440\u0430\u0457\u043D\u0441\u044C\u043A\u0430 (Ukrainian)"),
    ];
}

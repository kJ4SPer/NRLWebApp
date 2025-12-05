namespace FirstWebApplication.Models.Settings
{
    public class SettingsViewModel
    {
        public string CurrentLanguage { get; set; } = "nb-NO";

        // Tilgjengelige språk for dropdown
        public Dictionary<string, string> AvailableLanguages { get; } = new()
        {
            { "nb-NO", "Norsk" },
            { "en-US", "English" }
        };
    }
}

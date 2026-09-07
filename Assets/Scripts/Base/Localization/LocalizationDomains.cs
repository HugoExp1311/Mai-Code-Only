namespace Base.Localization
{
    /// <summary>
    /// Runtime CSV localization domains.
    /// </summary>
    public static class LocalizationDomains
    {
        public const string UI = "UI";
        public const string Dialogues = "Dialogues";

        public static string Normalize(string domain)
        {
            if (string.IsNullOrWhiteSpace(domain))
                return UI;

            return domain.Trim() switch
            {
                "UI Table" => UI,
                "Dialogue Table" => Dialogues,
                "Dialogue" => Dialogues,
                "Dialogues" => Dialogues,
                "UI" => UI,
                _ => domain.Trim()
            };
        }
    }
}

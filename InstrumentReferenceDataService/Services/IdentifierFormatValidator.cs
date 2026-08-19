using System.Text.RegularExpressions;

namespace InstrumentReferenceDataService.Services;

internal static class IdentifierFormatValidator
{
    private static readonly IReadOnlyDictionary<string, (Regex Regex, string Description)> ValidationRules =
        new Dictionary<string, (Regex Regex, string Description)>(StringComparer.OrdinalIgnoreCase)
        {
            ["CUSIP"] = (new Regex("^[0-9A-Z]{9}$", RegexOptions.Compiled), "9 uppercase alphanumeric characters."),
            ["ISIN"] = (new Regex("^[A-Z]{2}[A-Z0-9]{9}[0-9]$", RegexOptions.Compiled), "2 letters, followed by 9 uppercase alphanumeric characters, followed by 1 digit."),
            ["RIC"] = (new Regex("^[A-Z0-9]+(\\.[A-Z0-9]+)?$", RegexOptions.Compiled), "Base uppercase alphanumeric code with an optional '.SUFFIX'."),
            ["SEDOL"] = (new Regex("^[B-DF-HJ-NP-TV-Z0-9]{6}[0-9]$", RegexOptions.Compiled), "6 uppercase characters (excluding vowels) followed by 1 digit."),
            ["TICKER"] = (new Regex("^[A-Z0-9.\\-/]{1,12}$", RegexOptions.Compiled), "1-12 uppercase characters using letters, digits, '.', '-', or '/'.")
        };

    public static bool TryNormalizeAndValidate(
        string identifierTypeId,
        string identifierValue,
        out string normalizedValue,
        out string? errorMessage)
    {
        normalizedValue = identifierValue.Trim().ToUpperInvariant();

        if (!ValidationRules.TryGetValue(identifierTypeId, out var rule))
        {
            errorMessage = null;
            return true;
        }

        if (rule.Regex.IsMatch(normalizedValue))
        {
            errorMessage = null;
            return true;
        }

        errorMessage = $"Invalid {identifierTypeId.ToUpperInvariant()} identifier format. Expected: {rule.Description}";
        return false;
    }
}
namespace BikeMate.Core.Services;

public static class RepairConcernMatcher
{
    private static readonly IReadOnlyDictionary<string, string[]> ConcernTerms =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["Tire Problem"] = ["tire", "flat", "puncture", "wheel"],
            ["Brake Adjustment"] = ["brake"],
            ["Gear Shifting Issue"] = ["gear", "shifting", "drivetrain", "derailleur", "transmission"],
            ["Accessory Installation"] = ["accessory", "installation", "electrical upgrade"],
            ["Chain Maintenance"] = ["chain", "sprocket"],
            ["General Tune-up"] = ["tune-up", "tune up", "preventive maintenance", "periodic maintenance", "general inspection"]
        };

    public static bool Matches(
        string? concern,
        string? categoryName,
        string? serviceName,
        string? serviceDescription = null)
    {
        if (string.IsNullOrWhiteSpace(concern))
        {
            return true;
        }

        var terms = ConcernTerms.TryGetValue(concern.Trim(), out var mappedTerms)
            ? mappedTerms
            : [concern.Trim()];
        var searchableText = string.Join(
            " ",
            new[] { categoryName, serviceName, serviceDescription }
                .Where(value => !string.IsNullOrWhiteSpace(value)));

        return terms.Any(term => searchableText.Contains(term, StringComparison.OrdinalIgnoreCase));
    }
}

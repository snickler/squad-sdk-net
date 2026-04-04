namespace Squad.SDK.NET.Marketplace;

/// <summary>
/// Validates <see cref="MarketplaceManifest"/> instances against the marketplace schema rules.
/// </summary>
public static class ManifestValidator
{
    /// <summary>Validates the given manifest and returns a list of error messages.</summary>
    /// <param name="manifest">The manifest to validate.</param>
    /// <returns>An empty list when valid; otherwise a list of validation error messages.</returns>
    public static IReadOnlyList<string> Validate(MarketplaceManifest manifest)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(manifest.Name))
            errors.Add("Manifest name is required.");

        if (string.IsNullOrWhiteSpace(manifest.Version))
            errors.Add("Manifest version is required.");

        if (manifest.Name is not null && manifest.Name.Length > 128)
            errors.Add("Manifest name must be 128 characters or fewer.");

        if (manifest.Description is not null && manifest.Description.Length > 1024)
            errors.Add("Manifest description must be 1024 characters or fewer.");

        if (manifest.Tags.Count > 20)
            errors.Add("Maximum 20 tags allowed.");

        foreach (var tag in manifest.Tags)
        {
            if (string.IsNullOrWhiteSpace(tag))
                errors.Add("Tags must not be empty.");
            else if (tag.Length > 64)
                errors.Add($"Tag '{tag}' exceeds maximum length of 64 characters.");
        }

        foreach (var capability in manifest.Capabilities)
        {
            if (string.IsNullOrWhiteSpace(capability.Name))
                errors.Add("Capability name is required.");
        }

        return errors.AsReadOnly();
    }

    /// <summary>Returns <see langword="true"/> if the manifest passes all validation rules.</summary>
    /// <param name="manifest">The manifest to check.</param>
    /// <returns><see langword="true"/> when valid; otherwise <see langword="false"/>.</returns>
    public static bool IsValid(MarketplaceManifest manifest) => Validate(manifest).Count == 0;
}

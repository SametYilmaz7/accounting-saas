using System.Text;

namespace SaaSPlatform.Domain.Features.Tenants;

public static class SlugNormalizer
{
    public static string Normalize(string slug)
    {
        ArgumentNullException.ThrowIfNull(slug);

        var result = new StringBuilder();
        var separatorPending = false;

        foreach (var character in slug.Trim().ToLowerInvariant())
        {
            if (character is >= 'a' and <= 'z' or >= '0' and <= '9')
            {
                if (separatorPending && result.Length > 0)
                {
                    result.Append('-');
                }

                result.Append(character);
                separatorPending = false;
            }
            else if (character == '-' || char.IsWhiteSpace(character))
            {
                separatorPending = true;
            }
        }

        return result.ToString();
    }
}

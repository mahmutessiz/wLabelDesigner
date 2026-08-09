using System.IO;

namespace wLabelDesigner.Services;

public static class StartupTemplatePathResolver
{
    public static string? Resolve(IEnumerable<string>? arguments)
    {
        if (arguments is null)
        {
            return null;
        }

        foreach (var argument in arguments)
        {
            if (string.IsNullOrWhiteSpace(argument))
            {
                continue;
            }

            var candidate = argument.Trim().Trim('"');
            try
            {
                if (!string.Equals(Path.GetExtension(candidate), ".wld", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                return Path.GetFullPath(candidate);
            }
            catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
            {
                continue;
            }
        }

        return null;
    }
}

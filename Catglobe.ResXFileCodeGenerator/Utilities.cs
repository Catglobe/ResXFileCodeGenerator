namespace Catglobe.ResXFileCodeGenerator;

public static class Utilities
{
    // Code from: https://github.com/dotnet/ResXResourceManager/blob/c8b5798d760f202a1842a74191e6010c6e8bbbc0/src/ResXManager.VSIX/Visuals/MoveToResourceViewModel.cs#L120

    public static string GetLocalNamespace(
        string? resxPath,
        string? targetPath,
        string projectPath,
        string projectName,
        string? rootNamespace
    )
    {
        try
        {
            if (resxPath is null)
            {
                return string.Empty;
            }

            var resxFolder = Path.GetDirectoryName(resxPath);
            var projectFolder = Path.GetDirectoryName(projectPath);
            rootNamespace ??= string.Empty;

            if (resxFolder is null || projectFolder is null)
            {
                return string.Empty;
            }

            var localNamespace = string.Empty;

            if (!string.IsNullOrWhiteSpace(targetPath))
            {
                localNamespace = Path.GetDirectoryName(targetPath)
                    .Trim(Path.DirectorySeparatorChar)
                    .Trim(Path.AltDirectorySeparatorChar)
                    .Replace(Path.DirectorySeparatorChar, '.')
                    .Replace(Path.AltDirectorySeparatorChar, '.');
            }
            else if (resxFolder.StartsWith(projectFolder, StringComparison.OrdinalIgnoreCase))
            {
                localNamespace = resxFolder
                    .Substring(projectFolder.Length)
                    .Trim(Path.DirectorySeparatorChar)
                    .Trim(Path.AltDirectorySeparatorChar)
                    .Replace(Path.DirectorySeparatorChar, '.')
                    .Replace(Path.AltDirectorySeparatorChar, '.');
            }

            if (string.IsNullOrEmpty(rootNamespace) && string.IsNullOrEmpty(localNamespace))
            {
                // If local namespace is empty, e.g file is in root project folder, root namespace set to empty
                // fallback to project name as a namespace
                localNamespace = SanitizeNamespace(projectName);
            }
            else
            {
                localNamespace = (string.IsNullOrEmpty(localNamespace)
                        ? rootNamespace
                        : $"{rootNamespace}.{SanitizeNamespace(localNamespace, false)}")
                    .Trim('.');
            }

            return localNamespace;
        }
        catch (Exception)
        {
            return string.Empty;
        }
    }

    public static string SanitizeNamespace(string ns, bool sanitizeFirstChar = true)
    {
        if (string.IsNullOrEmpty(ns))
        {
            return ns;
        }

        // A namespace must contain only alphabetic characters, decimal digits, dots and underscores, and must begin with an alphabetic character or underscore (_)
        // In case there are invalid chars we'll use same logic as Visual Studio and replace them with underscore (_) and append underscore (_) if project does not start with alphabetic or underscore (_)

        var sanitizedNs = Regex
            .Replace(ns, @"[^a-zA-Z0-9_\.]", "_");

        // Handle folder containing multiple dots, e.g. 'test..test2' or starting, ending with dots
        sanitizedNs = Regex
            .Replace(sanitizedNs, @"\.+", ".");

        if (sanitizeFirstChar)
        {
            sanitizedNs = sanitizedNs.Trim('.');
        }

        return sanitizeFirstChar
            // Handle namespace starting with digit
            ? char.IsDigit(sanitizedNs[0]) ? $"_{sanitizedNs}" : sanitizedNs
            : sanitizedNs;
    }

	public static bool BasenameFromPath(string fullPath, [MaybeNullWhen(false)]out string basename, [MaybeNullWhen(false)]out string file)
	{
		basename = null!;
		file = null!;
		if (Path.GetFileName(fullPath) is not { } filename || Path.GetDirectoryName(fullPath) is not { } path) return false;
		//extract basename and iso from path...
		//x.y.z.resx has basename x and culture null.
		//x.y.z.resx -> (x,null)
		//x.y.z.nn.resx -> (x,nn)
		//x.y.z.nn-CC.resx -> (x,nn-CC)
		//z.resx -> (z,null)
		//z.nn.resx -> (z,nn)
		basename = path + Path.DirectorySeparatorChar + (filename.IndexOf('.') is var idx && idx < 0 ? filename : filename.Substring(0, idx));
		file = filename;
		return true;
	}
}

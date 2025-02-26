using System.Collections.Concurrent;

namespace Catglobe.ResXFileCodeGenerator;

internal sealed record ResxFile(string Basename, CultureInfo? Culture, string? CultureIso, AdditionalText File)
{
	public bool Equals(ResxFile? other) => Basename.Equals(other?.Basename) && ReferenceEquals(Culture, other?.Culture);

	public override int GetHashCode()
	{
		unchecked
		{
			return (Basename.GetHashCode() * 397) ^ (Culture != null ? Culture.GetHashCode() : 0);
		}
	}

	public override string ToString() => $"{nameof(Basename)}: {Basename}, {nameof(CultureIso)}: {CultureIso}, {nameof(Culture)}: {Culture?.LCID}";

	public static ResxFile? From(AdditionalText file, CancellationToken ct = default)
	{
		if (Path.GetFileName(file.Path) is not { } filename || Path.GetDirectoryName(file.Path) is not { } path) return null;
		//extract basename and iso from path...
		//x.y.z.resx has basename x and culture null.
		//x.y.z.resx -> (x,null)
		//x.y.z.nn.resx -> (x,nn)
		//x.y.z.nn-CC.resx -> (x,nn-CC)
		//z.resx -> (z,null)
		//z.nn.resx -> (z,nn)
		var basename = path + Path.DirectorySeparatorChar + (filename.IndexOf('.') is var idx && idx < 0 ? filename : filename.Substring(0, idx));

		var beforeDotResx = filename.Length - ".resx".Length - 1;
		var lastDot = filename.LastIndexOf('.', beforeDotResx);
		if (lastDot <= 0)
			return new(basename, null, null, file);

		//check if lastDot to beforeDotResx is a culture
		var possibleCulture = filename.Substring(lastDot + 1, beforeDotResx - lastDot);
		var cultureInfo = ValidLanguagesCache.GetOrAdd(possibleCulture, IsValidLanguageName);
		return new(basename, cultureInfo, cultureInfo is not null ? possibleCulture.ToLowerInvariant() : null, file);
	}

	private static readonly ConcurrentDictionary<string, CultureInfo?> ValidLanguagesCache = new();

	private static CultureInfo? IsValidLanguageName(string? languageName)
	{
		if (string.IsNullOrWhiteSpace(languageName))
		{
			return null;
		}

		var dash = languageName!.IndexOf('-');
		if (dash >= 4 || (dash == -1 && languageName.Length >= 4))
		{
			return null;
		}

		try
		{
			var culture = new CultureInfo(languageName);

			//while (!culture.IsNeutralCulture)
			//{
			//	culture = culture.Parent;
			//}

			return culture.LCID != 4096 ? culture : null;
		}
		catch
		{
			return null;
		}
	}
}

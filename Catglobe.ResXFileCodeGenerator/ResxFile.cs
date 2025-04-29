using System.Collections.Concurrent;

namespace Catglobe.ResXFileCodeGenerator;

internal sealed record ResxFile(string Basename, CultureInfo? Culture, string? CultureIso, SourceText Content, ImmutableEquatableArray<byte> ContentHash, AdditionalText File)
{
	public bool Equals(ResxFile? other)
	{
		if (other is null)
		{
			return false;
		}

		if (ReferenceEquals(this, other))
		{
			return true;
		}

		return Basename == other.Basename && CultureIso == other.CultureIso && ContentHash.Equals(other.ContentHash);
	}

	public override int GetHashCode()
	{
		unchecked
		{
			var hashCode = Basename.GetHashCode();
			hashCode = (hashCode * 397) ^ (CultureIso != null ? CultureIso.GetHashCode() : 0);
			hashCode = (hashCode * 397) ^ ContentHash.GetHashCode();
			return hashCode;
		}
	}
	
	public override string ToString() => $"{nameof(Basename)}: {Basename}, {nameof(CultureIso)}: {CultureIso}, {nameof(Culture)}: {Culture?.LCID}";

	public static ResxFile? From(AdditionalText file, CancellationToken ct = default)
	{
		if (!Utilities.BasenameFromPath(file.Path, out var basename, out var filename))
			return null;
		var content = file.GetText(ct)?? SourceText.From("");
		var contentHash = content.GetContentHash();

		var beforeDotResx = filename.Length - ".resx".Length - 1;
		var lastDot = filename.LastIndexOf('.', beforeDotResx);
		//main file
		if (lastDot <= 0)
			return new(basename, null, null, content, contentHash, file);

		//check if lastDot to beforeDotResx is a culture
		var possibleCulture = filename.Substring(lastDot + 1, beforeDotResx - lastDot);
		var cultureInfo = ValidLanguagesCache.GetOrAdd(possibleCulture, IsValidLanguageName);

		return new(basename, cultureInfo, cultureInfo is not null ? possibleCulture.ToLowerInvariant() : null, content, contentHash, file);
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

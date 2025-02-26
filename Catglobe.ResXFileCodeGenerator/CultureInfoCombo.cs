namespace Catglobe.ResXFileCodeGenerator;

/// <summary>
/// Note: Equality takes into consideration Iso property only
/// </summary>
/// <param name="CultureInfos">Already ordered list of files</param>
internal sealed record CultureInfoCombo(IReadOnlyList<ResxFile> CultureInfos)
{
	//public static CultureInfoCombo Empty = new([]);
	public bool Equals(CultureInfoCombo? other)
    {
        return CultureInfos.Select(x => x.Culture!.LCID)
            .SequenceEqual(other?.CultureInfos.Select(x => x.Culture!.LCID) ?? []);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            const int seedValue = 0x2D2816FE;
            const int primeNumber = 397;
            var val = seedValue;
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var cultureInfo in CultureInfos)
            {
	            val = (val * primeNumber) + cultureInfo.Culture!.LCID;
			}
            return val;
        } 
    }
}

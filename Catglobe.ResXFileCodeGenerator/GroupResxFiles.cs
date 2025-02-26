namespace Catglobe.ResXFileCodeGenerator;

internal static class GroupResxFiles
{
    public static IEnumerable<ResxGroup> Group(IReadOnlyList<ResxFile> resx) =>
	    resx.ToLookup(x=>x.Basename).Select(x => new ResxGroup(x.ToList()));

    public static ImmutableArray<CultureInfoCombo> DetectChildCombos(IReadOnlyList<ResxGroup> groupedAdditionalFiles) =>
	    [..groupedAdditionalFiles.Select(x => x.SubFiles).Distinct()];
}

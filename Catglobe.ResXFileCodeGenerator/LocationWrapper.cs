namespace Catglobe.ResXFileCodeGenerator;

internal sealed record LocationWrapper(Location Location)
{
	//all are equal (except null), and thus ignored in memoization
	public bool Equals(LocationWrapper? other) => other is not null;

	public override int GetHashCode() => 0;

	public static implicit operator LocationWrapper(Location? l) => new(l ?? Location.None);
	public static implicit operator Location(LocationWrapper l) => l.Location;
}

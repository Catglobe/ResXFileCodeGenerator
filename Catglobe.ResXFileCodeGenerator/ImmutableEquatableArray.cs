using System.Collections;

namespace Catglobe.ResXFileCodeGenerator;

/// <summary>
/// Provides an immutable list implementation which implements sequence equality.
/// </summary>
internal readonly struct ImmutableEquatableArray<T>(ImmutableArray<T> values)
	: IEquatable<ImmutableEquatableArray<T>>, IReadOnlyList<T>
	where T : IEquatable<T>
{
	public static ImmutableEquatableArray<T> Empty { get; } = new([]);

	private readonly ImmutableArray<T> _values = values.IsDefault ? [..Array.Empty<T>()] : values;
	public T this[int index] => _values[index];
	public int Count => _values.Length;

	public bool Equals(ImmutableEquatableArray<T> other) => _values.SequenceEqual(other._values);

	public override bool Equals(object? obj)
		=> obj is ImmutableEquatableArray<T> other && Equals(other);

	public override int GetHashCode()
	{
		const int seedValue = 0x2D2816FE;
		const int primeNumber = 397;
		var val = seedValue;
		// ReSharper disable once LoopCanBeConvertedToQuery
		// ReSharper disable once ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
		foreach (var v in _values)
		{
			val = (val * primeNumber) + v.GetHashCode();
		}
		return val;
	}

	public Enumerator GetEnumerator() => new Enumerator(_values);
	IEnumerator<T> IEnumerable<T>.GetEnumerator() => ((IEnumerable<T>)_values).GetEnumerator();
	IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)_values).GetEnumerator();

	public struct Enumerator(ImmutableArray<T> values)
	{
		private int _index = -1;

		public bool MoveNext()
		{
			return ++_index < values.Length;
		}

		public readonly T Current => values[_index];
	}

	public static implicit operator ImmutableEquatableArray<T>(ImmutableArray<T> values) => new(values);
	public static implicit operator ImmutableEquatableArray<T>(ImmutableArray<T>? values) => new(values ?? throw new NullReferenceException("array is null"));
	public static implicit operator ImmutableEquatableArray<T>?(ImmutableArray<T>? values) => values is null ? null : new(values.Value);
	public static implicit operator ImmutableEquatableArray<T>(ImmutableArray<T>.Builder values) => new(values.ToImmutable());
}


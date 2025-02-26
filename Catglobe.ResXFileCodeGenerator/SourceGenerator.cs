using System.Runtime.CompilerServices;

[assembly:InternalsVisibleTo("Catglobe.ResXFileCodeGenerator.Tests")]

namespace Catglobe.ResXFileCodeGenerator;

[Generator]
public class SourceGenerator : IIncrementalGenerator
{
	private static readonly IGenerator s_generator = new StringBuilderGenerator();

	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		var globalOptions = context.AnalyzerConfigOptionsProvider.Select(GlobalOptions.Select);

		var allResxFiles = context.AdditionalTextsProvider
			.Where(static af => af.Path.EndsWith(".resx", StringComparison.OrdinalIgnoreCase))
			.Select(ResxFile.From)
			.Where(x => x is not null);

		var monitor = allResxFiles.Collect().SelectMany(static (x, _) => GroupResxFiles.Group(x));
		
		var inputs = monitor.Where(x=>x.Error is null)
			.Combine(globalOptions.Combine(context.AnalyzerConfigOptionsProvider))
			.Select(static (x, _) =>
			{
				var (resourceGroup, (globalOptions, fileOptionsProvider)) = x;
				return new FileOptions(
					groupedFile: resourceGroup,
					options: fileOptionsProvider.GetOptions(resourceGroup.MainFile.File),
					globalOptions: globalOptions
				);
			});

		context.RegisterSourceOutput(monitor.Where(x=>x.Error is not null), (ctx, file) =>
		{
			ctx.ReportDiagnostic(file.Error!);
		});

		context.RegisterSourceOutput(inputs, (ctx, file) =>
		{
			var (generatedFileName, sourceCode, errorsAndWarnings) =
				s_generator.Generate(file, ctx.CancellationToken);
			foreach (var sourceErrorsAndWarning in errorsAndWarnings)
			{
				ctx.ReportDiagnostic(sourceErrorsAndWarning);
			}

			ctx.AddSource(generatedFileName, sourceCode);
		});

		var detectAllCombosOfResx = monitor.Collect().SelectMany((x, _) => GroupResxFiles.DetectChildCombos(x));
		context.RegisterSourceOutput(detectAllCombosOfResx, (ctx, combo) =>
		{
			var (generatedFileName, sourceCode, errorsAndWarnings) =
				s_generator.Generate(combo, ctx.CancellationToken);
			foreach (var sourceErrorsAndWarning in errorsAndWarnings)
			{
				ctx.ReportDiagnostic(sourceErrorsAndWarning);
			}

			ctx.AddSource(generatedFileName, sourceCode);
		});
	}

	private sealed class
		ImmutableDictionaryEqualityComparer<TKey, TValue> : IEqualityComparer<ImmutableDictionary<TKey, TValue>?>
		where TKey : notnull
	{
		public static readonly ImmutableDictionaryEqualityComparer<TKey, TValue> Instance = new();

		public bool Equals(ImmutableDictionary<TKey, TValue>? x, ImmutableDictionary<TKey, TValue>? y)
		{
			if (ReferenceEquals(x, y))
				return true;

			if (x is null || y is null)
				return false;

			if (!Equals(x.KeyComparer, y.KeyComparer))
				return false;

			if (!Equals(x.ValueComparer, y.ValueComparer))
				return false;

			foreach (var kvp in x)
			{
				var (key, value) = (kvp.Key, kvp.Value);
				if (!y.TryGetValue(key, out var other)
				    || !x.ValueComparer.Equals(value, other))
				{
					return false;
				}
			}

			return true;
		}

		public int GetHashCode(ImmutableDictionary<TKey, TValue>? obj)
		{
			return obj?.Count ?? 0;
		}
	}
}

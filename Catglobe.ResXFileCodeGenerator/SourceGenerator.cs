using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis.CSharp.Syntax;

[assembly:InternalsVisibleTo("Catglobe.ResXFileCodeGenerator.Tests")]

namespace Catglobe.ResXFileCodeGenerator;

/// <summary>
/// The generator
/// </summary>
[Generator]
public class SourceGenerator : IIncrementalGenerator
{
	private static readonly DiagnosticDescriptor s_unmatchedAttribute = new(
		id: "CatglobeResXFileCodeGenerator008",
		title: "Unmatched attribute",
		messageFormat: "'{0}' has no corresponding resx files found",
		category: "ResXFileCodeGenerator",
		defaultSeverity: DiagnosticSeverity.Warning,
		isEnabledByDefault: true
	);

	private static readonly IGenerator s_generator = new StringBuilderGenerator();

	/// <summary>
	/// Initialize the generator
	/// </summary>
	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		//graph TD
		// %% Primary Data Flow
		// A[context] --> B[globalOptions]
		// A --> C[allResxFiles]
		// C --> D[resxGroup]
		// D --> E[simpleNoErrorGroups]
		// E --> F[noErrorResGroups]
		// F --> G[resxGroupLookup]
		// 
		// %% Error Handling for resxGroup
		// D -- Error --> H[resxGroup Error Sink]
		// H -- ctx.ReportDiagnostic --> H1[Diagnostic Report]
		// 
		// %% Enum Attributes Processing
		// A --> I[enumAttributes]
		// G --> I
		// I --> J[matcherEnumAttributes]
		// J -- "Match is null" --> K[Report Unmatched Enum Attribute]
		// K -- ctx.ReportDiagnostic --> K1[Diagnostic Report]
		// J -- "Match is not null" --> L[matchedEnum]
		// L --> M[Error: matchedEnum has no sink]
		// 
		// %% Class Attributes Processing
		// A --> N[classAttributes]
		// G --> N
		// N -- Error --> O[classAttributes Error Sink]
		// O -- ctx.ReportDiagnostic --> O1[Diagnostic Report]
		// N --> P[matcherClassAttributes]
		// P -- "Match is null" --> Q[Report Unmatched Class Attribute]
		// Q -- ctx.ReportDiagnostic --> Q1[Diagnostic Report]
		// P -- "Match is not null" --> R[matchedClass]
		// R --> S[Generate Source for Matched Class Attributes]
		// S -- ctx.AddSource --> T[Uses AddSource]
		// 
		// %% No Error Resx Groups Sink
		// F --> U[Generate Source for No Error Resx Groups]
		// U -- ctx.AddSource --> V[Uses AddSource]
		// 
		// %% Detect Child Combos and Sink
		// E --> W[simpleNoErrorGroups Collect]
		// W --> X[Detect Child Combos]
		// X --> Y[Generate Source for Child Combos]
		// Y -- ctx.AddSource --> Z[Uses AddSource]
		// 
		// %% Styling Definitions
		// classDef sinkGreen fill:#cfc,stroke:#333,stroke-width:2px;
		// classDef sinkYellow fill:#ffeb3b,stroke:#333,stroke-width:2px;
		// classDef errorRed fill:#fbb,stroke:#333,stroke-width:2px;
		// 
		// %% Applying Styles
		// class H1,K1,O1,Q1 sinkYellow
		// class T,V,Z sinkGreen
		// class L errorRed

		var globalOptions = context.AnalyzerConfigOptionsProvider.Select(GlobalOptions.Select);

		var allResxFiles = context.AdditionalTextsProvider
			.Where(static af => af.Path.EndsWith(".resx", StringComparison.OrdinalIgnoreCase))
			.Select(ResxFile.From)
			.Where(x => x is not null)
			.WithTrackingName("allResxFiles");

		var resxGroup = allResxFiles.Collect().SelectMany(static (x, _) => GroupResxFiles.Group(x))
			.WithTrackingName("resxGroup");

		context.RegisterSourceOutput(resxGroup.Where(x=>x.Error is not null), static (ctx, file) => ctx.ReportDiagnostic(file.Error!));

		var simpleNoErrorGroups = resxGroup.Where(x=>x.Error is null).WithTrackingName("simpleNoErrorGroups");
		var noErrorResGroups = simpleNoErrorGroups
			.Combine(globalOptions)
			.Combine(context.AnalyzerConfigOptionsProvider)
			.Select(static (x, _) =>
			{
				var ((resourceGroup, globalOptions), fileOptionsProvider) = x;
				return new FileOptions(
					groupedFile: resourceGroup,
					options: fileOptionsProvider.GetOptions(resourceGroup.MainFile.File),
					globalOptions: globalOptions
				);
			}).Collect()
			.WithTrackingName("noErrorResGroups");

		var resxGroupLookup = noErrorResGroups.Select((x, _) => x.ToLookup(y => y.GroupedFile.Basename))
			.WithTrackingName("resxGroupLookup");

		var classAttributes =
			context.SyntaxProvider
				.ForAttributeWithMetadataName(Parser.ClassAttribute,
					predicate: (node, _) => node is ClassDeclarationSyntax,
					transform: Parser.ParseClass).WithTrackingName("classAttributes");
		context.RegisterSourceOutput(classAttributes.Where(static m => m.Error is not null), static (context, info) =>
			context.ReportDiagnostic(Diagnostic.Create(info.Error!.Value.Diag, info.Error!.Value!.Location, info.Error!.Value!.Member)));
		var matcherClassAttributes = classAttributes.Where(static m => m.Spec is not null).Combine(resxGroupLookup)
			.Select((x,_)=>MatchWithGroup(x.Left, x.Left!.Spec!.SymbolReference, x.Right)).WithTrackingName("matcherClassAttributes");
		context.RegisterSourceOutput(matcherClassAttributes.Where(x=>x.Match is null), static (context, info) =>
			context.ReportDiagnostic(Diagnostic.Create(s_unmatchedAttribute, info.Attr!.Spec!.Location, info.Attr.Spec!.SymbolReference.TheType.TypeSymbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat))));
		var matchedClass = matcherClassAttributes.Where(x => x.Match is not null).Collect().WithTrackingName("matchedClass");

		context.RegisterSourceOutput(matchedClass.SelectMany((x,_)=>x), (ctx, file) =>
		{
			var (generatedFileName, sourceCode, errorsAndWarnings) =
				s_generator.Generate(file.Match!, file.Attr!.Spec!, ctx.CancellationToken);
			foreach (var sourceErrorsAndWarning in errorsAndWarnings)
			{
				ctx.ReportDiagnostic(sourceErrorsAndWarning);
			}

			ctx.AddSource(generatedFileName, sourceCode);
		});

		context.RegisterSourceOutput(
			noErrorResGroups.SelectMany((x, _) => x).Combine(matchedClass), (ctx, chain) =>
			{
				//we do not actually need look at the matchedClass,
				//we just need them to be in the pipeline to have mutated f.Matched
				var file = chain.Left;
				if (file.Matched) return;
				var (generatedFileName, sourceCode, errorsAndWarnings) =
					s_generator.Generate(file, null, ctx.CancellationToken);
				foreach (var sourceErrorsAndWarning in errorsAndWarnings)
				{
					ctx.ReportDiagnostic(sourceErrorsAndWarning);
				}

				ctx.AddSource(generatedFileName, sourceCode);
			});

		context.RegisterSourceOutput(
			simpleNoErrorGroups.Collect().SelectMany((x, _) => GroupResxFiles.DetectChildCombos(x)), (ctx, combo) =>
			{
				var (generatedFileName, sourceCode, errorsAndWarnings) =
					s_generator.Generate(combo, ctx.CancellationToken);
				foreach (var sourceErrorsAndWarning in errorsAndWarnings)
				{
					ctx.ReportDiagnostic(sourceErrorsAndWarning);
				}

				ctx.AddSource(generatedFileName, sourceCode);
			});

		return;

		static (T Attr, FileOptions? Match) MatchWithGroup<T>(T spec, SymbolReference symbolReference, ILookup<string, FileOptions> groups)
		{
			foreach (var path in symbolReference.Basenames)
			{
				var match = groups[path].FirstOrDefault();
				if (match is not null)
				{
					match.Matched = true;
					return (spec, match);
				}
			}

			return (spec, null);
		}
	}
}

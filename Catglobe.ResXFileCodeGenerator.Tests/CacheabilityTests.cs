using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp;
using Catglobe.ResXFileCodeGenerator;

namespace Catglobe.ResXFileCodeGenerator.Tests;

public class IncrementalCacheabilityTests
{
    private static readonly string ResxXml = string.Join("\n",
        @"<?xml version=""1.0"" encoding=""utf-8""?>",
        @"<root>",
        @"  <xsd:schema id=""root"" xmlns="""" xmlns:xsd=""http://www.w3.org/2001/XMLSchema"" xmlns:msdata=""urn:schemas-microsoft-com:xml-msdata"">",
        @"    <xsd:import namespace=""http://www.w3.org/XML/1998/namespace"" />",
        @"    <xsd:element name=""root"" msdata:IsDataSet=""true"">",
        @"      <xsd:complexType>",
        @"        <xsd:choice maxOccurs=""unbounded"">",
        @"          <xsd:element name=""data"">",
        @"            <xsd:complexType>",
        @"              <xsd:sequence>",
        @"                <xsd:element name=""value"" type=""xsd:string"" minOccurs=""0"" msdata:Ordinal=""1"" />",
        @"                <xsd:element name=""comment"" type=""xsd:string"" minOccurs=""0"" msdata:Ordinal=""2"" />",
        @"              </xsd:sequence>",
        @"              <xsd:attribute name=""name"" type=""xsd:string"" use=""required"" msdata:Ordinal=""1"" />",
        @"              <xsd:attribute name=""type"" type=""xsd:string"" msdata:Ordinal=""3"" />",
        @"              <xsd:attribute name=""mimetype"" type=""xsd:string"" msdata:Ordinal=""4"" />",
        @"              <xsd:attribute ref=""xml:space"" />",
        @"            </xsd:complexType>",
        @"          </xsd:element>",
        @"          <xsd:element name=""resheader"">",
        @"            <xsd:complexType>",
        @"              <xsd:sequence>",
        @"                <xsd:element name=""value"" type=""xsd:string"" minOccurs=""0"" msdata:Ordinal=""1"" />",
        @"              </xsd:sequence>",
        @"              <xsd:attribute name=""name"" type=""xsd:string"" use=""required"" />",
        @"            </xsd:complexType>",
        @"          </xsd:element>",
        @"        </xsd:choice>",
        @"      </xsd:complexType>",
        @"    </xsd:element>",
        @"  </xsd:schema>",
        @"  <resheader name=""resmimetype"">",
        @"    <value>text/microsoft-resx</value>",
        @"  </resheader>",
        @"  <resheader name=""version"">",
        @"    <value>2.0</value>",
        @"  </resheader>",
        @"  <resheader name=""reader"">",
        @"    <value>System.Resources.ResXResourceReader, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value>",
        @"  </resheader>",
        @"  <resheader name=""writer"">",
        @"    <value>System.Resources.ResXResourceWriter, System.Windows.Forms, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089</value>",
        @"  </resheader>",
        @"  <data name=""MyKey"" xml:space=""preserve"">",
        @"    <value>MyValue</value>",
        @"  </data>",
        @"</root>"
    );

    private const string ResxSettingsAttributeSource = """
using System;

namespace Catglobe.ResXFileCodeGenerator
{
    public enum Visibility
    {
        NotGenerated,
        SameAsOuter,
        Private,
        Public,
        Internal,
        Protected
    }

    [AttributeUsage(AttributeTargets.Class)]
    public sealed class ResxSettingsAttribute : Attribute
    {
        public bool StaticMembers { get; set; } = true;
        public Visibility MembersVisibility { get; set; } = Visibility.Private;
        public bool GenerateLookup { get; set; }
    }
}
""";

    private const string UserSourceWithAttribute = """
using Catglobe.ResXFileCodeGenerator;

namespace MyNamespace
{
    [ResxSettings()]
    public partial class MyResources { }
}
""";

    private const string UserSourceWithoutAttribute = """
using Catglobe.ResXFileCodeGenerator;

namespace MyNamespace
{
    public partial class MyResources { }
}
""";

    private static readonly string[] AllTrackingNames =
    [
        "allResxFiles",
        "resxGroup",
        "simpleNoErrorGroups",
        "noErrorResGroups",
        "resxGroupLookup",
        "classAttributes",
        "matcherClassAttributes",
        "matchedClass",
    ];

    private static MetadataReference[] CreateReferences()
    {
        var runtimeDir = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
        return new[]
        {
            MetadataReference.CreateFromFile(Path.Combine(runtimeDir, "System.Runtime.dll")),
            MetadataReference.CreateFromFile(Path.Combine(runtimeDir, "System.Collections.dll")),
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location),
        };
    }

    private static CSharpCompilation CreateCompilation(string userSource) =>
        CSharpCompilation.Create(
            assemblyName: "TestAssembly",
            syntaxTrees: new[]
            {
                CSharpSyntaxTree.ParseText(ResxSettingsAttributeSource, path: @"C:\Project\Catglobe.ResXFileCodeGenerator\Attributes.cs"),
                CSharpSyntaxTree.ParseText(userSource, path: @"C:\Project\MyNamespace\MyResources.cs"),
            },
            references: CreateReferences(),
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

    private static Dictionary<string, List<IncrementalStepRunReason>> GetStepReasons(
        GeneratorDriverRunResult runResult)
    {
        return runResult.Results[0].TrackedSteps
            .Where(kvp => AllTrackingNames.Contains(kvp.Key))
            .ToDictionary(
                kvp => kvp.Key,
                kvp => kvp.Value
                    .SelectMany(execution => execution.Outputs)
                    .Select(output => output.Reason)
                    .ToList());
    }

    private static void AssertAllReasons(
        Dictionary<string, List<IncrementalStepRunReason>> reasons,
        string stepName,
        params IncrementalStepRunReason[] allowedReasons)
    {
        var allowed = new HashSet<IncrementalStepRunReason>(allowedReasons);
        reasons.ShouldContainKey(stepName, $"step '{stepName}' not found in tracked output");
        reasons[stepName].ShouldAllBe(r => allowed.Contains(r),
            $"step '{stepName}' expected all in [{string.Join(", ", allowed)}], " +
            $"got [{string.Join(", ", reasons[stepName])}]");
    }

    private static void AssertContainsReason(
        Dictionary<string, List<IncrementalStepRunReason>> reasons,
        string stepName,
        IncrementalStepRunReason expectedReason)
    {
        reasons.ShouldContainKey(stepName);
        reasons[stepName].ShouldContain(expectedReason,
            $"step '{stepName}' expected to contain {expectedReason}, " +
            $"got [{string.Join(", ", reasons[stepName])}]");
    }

    [Fact]
    public void Pipeline_CachesAllTrackedSteps_WhenInputsAreUnchanged()
    {
        var compilation = CreateCompilation(UserSourceWithAttribute);
        var resxText = new AdditionalTextStub(
            @"C:\Project\MyNamespace\MyResources.resx", ResxXml);

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            generators: [new SourceGenerator().AsSourceGenerator()],
            additionalTexts: [resxText],
            parseOptions: CSharpParseOptions.Default,
            driverOptions: new GeneratorDriverOptions(
                disabledOutputs: IncrementalGeneratorOutputKind.None,
                trackIncrementalGeneratorSteps: true));

        driver = driver.RunGenerators(compilation);
        driver = driver.RunGenerators(compilation.Clone());

        var reasons = GetStepReasons(driver.GetRunResult());

        foreach (var (step, reasonList) in reasons)
        {
            reasonList.ShouldAllBe(
                r => r == IncrementalStepRunReason.Cached || r == IncrementalStepRunReason.Unchanged,
                $"step '{step}' should be all Cached/Unchanged, got [{string.Join(", ", reasonList)}]");
        }
    }

    [Fact]
    public void Pipeline_AddingFile_NewFileIsNew_ExistingFileStaysCached()
    {
        var compilation = CreateCompilation(UserSourceWithAttribute);
        var resx1 = new AdditionalTextStub(
            @"C:\Project\MyNamespace\MyResources.resx", ResxXml);
        var resx2 = new AdditionalTextStub(
            @"C:\Project\MyNamespace\OtherResources.resx", ResxXml);

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            generators: [new SourceGenerator().AsSourceGenerator()],
            additionalTexts: [resx1],
            parseOptions: CSharpParseOptions.Default,
            driverOptions: new GeneratorDriverOptions(
                disabledOutputs: IncrementalGeneratorOutputKind.None,
                trackIncrementalGeneratorSteps: true));

        driver = driver.RunGenerators(compilation);

        driver = driver.ReplaceAdditionalText(resx1, resx1);
        driver = driver.AddAdditionalTexts(ImmutableArray.Create<AdditionalText>(resx2));
        driver = driver.RunGenerators(compilation.Clone());

        var reasons = GetStepReasons(driver.GetRunResult());

        AssertContainsReason(reasons, "allResxFiles", IncrementalStepRunReason.Cached);
        AssertContainsReason(reasons, "allResxFiles", IncrementalStepRunReason.New);
        AssertAllReasons(reasons, "classAttributes",
            IncrementalStepRunReason.Cached,
            IncrementalStepRunReason.Unchanged);
    }

    [Fact]
    public void Pipeline_ChangingFileContent_OnlyThatFileIsAffected()
    {
        var compilation = CreateCompilation(UserSourceWithAttribute);
        var resxBefore = new AdditionalTextStub(
            @"C:\Project\MyNamespace\MyResources.resx", ResxXml);
        var resxAfter = new AdditionalTextStub(
            @"C:\Project\MyNamespace\MyResources.resx",
            ResxXml.Replace(">MyValue<", ">ChangedValue<"));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            generators: [new SourceGenerator().AsSourceGenerator()],
            additionalTexts: [resxBefore],
            parseOptions: CSharpParseOptions.Default,
            driverOptions: new GeneratorDriverOptions(
                disabledOutputs: IncrementalGeneratorOutputKind.None,
                trackIncrementalGeneratorSteps: true));

        driver = driver.RunGenerators(compilation);
        driver = driver.ReplaceAdditionalText(resxBefore, resxAfter);
        driver = driver.RunGenerators(compilation.Clone());

        var reasons = GetStepReasons(driver.GetRunResult());

        AssertContainsReason(reasons, "allResxFiles", IncrementalStepRunReason.Modified);
        AssertAllReasons(reasons, "classAttributes",
            IncrementalStepRunReason.Cached,
            IncrementalStepRunReason.Unchanged);
    }

    [Fact]
    public void Pipeline_AddingAttribute_ResxStepsStayCached()
    {
        var compileWithoutAttr = CreateCompilation(UserSourceWithoutAttribute);
        var compileWithAttr = CreateCompilation(UserSourceWithAttribute);

        var resxText = new AdditionalTextStub(
            @"C:\Project\MyNamespace\MyResources.resx", ResxXml);

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            generators: [new SourceGenerator().AsSourceGenerator()],
            additionalTexts: [resxText],
            parseOptions: CSharpParseOptions.Default,
            driverOptions: new GeneratorDriverOptions(
                disabledOutputs: IncrementalGeneratorOutputKind.None,
                trackIncrementalGeneratorSteps: true));

        driver = driver.RunGenerators(compileWithoutAttr);
        driver = driver.RunGenerators(compileWithAttr);

        var reasons = GetStepReasons(driver.GetRunResult());

        AssertAllReasons(reasons, "allResxFiles", IncrementalStepRunReason.Cached);
        AssertContainsReason(reasons, "classAttributes", IncrementalStepRunReason.New);
    }

    [Fact]
    public void Pipeline_RemovingAttribute_ResxStepsStayCached()
    {
        var compileWithAttr = CreateCompilation(UserSourceWithAttribute);
        var compileWithoutAttr = CreateCompilation(UserSourceWithoutAttribute);

        var resxText = new AdditionalTextStub(
            @"C:\Project\MyNamespace\MyResources.resx", ResxXml);

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            generators: [new SourceGenerator().AsSourceGenerator()],
            additionalTexts: [resxText],
            parseOptions: CSharpParseOptions.Default,
            driverOptions: new GeneratorDriverOptions(
                disabledOutputs: IncrementalGeneratorOutputKind.None,
                trackIncrementalGeneratorSteps: true));

        driver = driver.RunGenerators(compileWithAttr);
        driver = driver.RunGenerators(compileWithoutAttr);

        var reasons = GetStepReasons(driver.GetRunResult());

        AssertAllReasons(reasons, "allResxFiles", IncrementalStepRunReason.Cached);
        AssertContainsReason(reasons, "classAttributes", IncrementalStepRunReason.Removed);
    }
}

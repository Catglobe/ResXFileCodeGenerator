namespace Catglobe.ResXFileCodeGenerator.Tests;

public class SettingsTests
{
    private static readonly GlobalOptions s_globalOptions = GlobalOptions.Select(
        provider: new AnalyzerConfigOptionsProviderStub(
            globalOptions: new AnalyzerConfigOptionsStub
            {
                RootNamespace = "namespace1",
                MSBuildProjectFullPath = "project1.csproj",
                MSBuildProjectName = "project1",
            },
            fileOptions: null!
        ),
        token: default
    );

    [Fact]
    public void GlobalDefaults()
    {
        var globalOptions = s_globalOptions;
        globalOptions.ProjectName.ShouldBe("project1");
        globalOptions.RootNamespace.ShouldBe("namespace1");
        globalOptions.ProjectFullPath.ShouldBe("project1.csproj");
        globalOptions.InnerClassName.ShouldBeNullOrEmpty();
        globalOptions.ClassNamePostfix.ShouldBeNullOrEmpty();
        globalOptions.InnerClassInstanceName.ShouldBeNullOrEmpty();
        globalOptions.InnerClassVisibility.ShouldBe(InnerClassVisibility.NotGenerated);
        globalOptions.NullForgivingOperators.ShouldBe(false);
        globalOptions.StaticClass.ShouldBe(true);
        globalOptions.StaticMembers.ShouldBe(true);
        globalOptions.PublicClass.ShouldBe(false);
        globalOptions.PartialClass.ShouldBe(false);
        globalOptions.IsValid.ShouldBe(true);
    }

    [Fact]
    public void GlobalSettings_CanReadAll()
    {
        var globalOptions = GlobalOptions.Select(
            provider: new AnalyzerConfigOptionsProviderStub(
                globalOptions: new AnalyzerConfigOptionsStub
                {
                    RootNamespace = "namespace1",
                    MSBuildProjectFullPath = "project1.csproj",
                    MSBuildProjectName = "project1",
                    ResXFileCodeGenerator_InnerClassName = "test1",
                    ResXFileCodeGenerator_InnerClassInstanceName = "test2",
                    ResXFileCodeGenerator_ClassNamePostfix= "test3",
                    ResXFileCodeGenerator_InnerClassVisibility = "public",
                    ResXFileCodeGenerator_NullForgivingOperators = "true",
                    ResXFileCodeGenerator_StaticClass = "false",
                    ResXFileCodeGenerator_StaticMembers = "false",
                    ResXFileCodeGenerator_UseResManager = "true",
                    ResXFileCodeGenerator_PublicClass = "true",
                    ResXFileCodeGenerator_PartialClass = "true",
                },
                fileOptions: null!
            ),
            token: default
        );
        globalOptions.RootNamespace.ShouldBe("namespace1");
        globalOptions.ProjectFullPath.ShouldBe("project1.csproj");
        globalOptions.ProjectName.ShouldBe("project1");
        globalOptions.InnerClassName.ShouldBe("test1");
        globalOptions.InnerClassInstanceName.ShouldBe("test2");
        globalOptions.ClassNamePostfix.ShouldBe("test3");
        globalOptions.InnerClassVisibility.ShouldBe(InnerClassVisibility.Public);
        globalOptions.NullForgivingOperators.ShouldBe(true);
        globalOptions.StaticClass.ShouldBe(false);
        globalOptions.UseResManager.ShouldBe(true);
        globalOptions.StaticMembers.ShouldBe(false);
        globalOptions.PublicClass.ShouldBe(true);
        globalOptions.PartialClass.ShouldBe(true);
        globalOptions.IsValid.ShouldBe(true);
    }

    [Fact]
    public void FileDefaults()
    {
        var fileOptions = new FileOptions(
			groupedFile: new ([
				ResxFile.From(new AdditionalTextStub("Path1.resx"))!
				]
            ),
            options: new AnalyzerConfigOptionsStub(),
            globalOptions: s_globalOptions
        );
        fileOptions.InnerClassName.ShouldBeNullOrEmpty();
        fileOptions.InnerClassInstanceName.ShouldBeNullOrEmpty();
        fileOptions.InnerClassVisibility.ShouldBe(InnerClassVisibility.NotGenerated);
        fileOptions.NullForgivingOperators.ShouldBe(false);
        fileOptions.StaticClass.ShouldBe(true);
        fileOptions.StaticMembers.ShouldBe(true);
        fileOptions.PublicClass.ShouldBe(false);
        fileOptions.PartialClass.ShouldBe(false);
        fileOptions.UseResManager.ShouldBe(false);
        fileOptions.LocalNamespace.ShouldBe("namespace1");
        fileOptions.CustomToolNamespace.ShouldBeNullOrEmpty();
        fileOptions.GroupedFile.MainFile.File.Path.ShouldBe("Path1.resx");
        fileOptions.ClassName.ShouldBe("Path1");
        fileOptions.IsValid.ShouldBe(true);
    }

    [Theory]
    [InlineData("project1.csproj", "Path1.resx", "", "project1", "project1.Path1")]
    [InlineData("project1.csproj", "Path1.resx", "rootNamespace","rootNamespace", "rootNamespace.Path1")]
    [InlineData(@"ProjectFolder\project1.csproj", @"ProjectFolder\SubFolder\Path1.resx", "rootNamespace", "rootNamespace.SubFolder", "rootNamespace.SubFolder.Path1")]
    [InlineData(@"ProjectFolder\project1.csproj", @"ProjectFolder\SubFolder With Space\Path1.resx", "rootNamespace", "rootNamespace.SubFolder_With_Space", "rootNamespace.SubFolder_With_Space.Path1")]
    [InlineData(@"ProjectFolder\project1.csproj", @"ProjectFolder\SubFolder\Path1.resx", "", "SubFolder", "SubFolder.Path1")]
    [InlineData(@"ProjectFolder\8 project.csproj", @"ProjectFolder\Path1.resx", "", "_8_project", "_8_project.Path1")]
    [InlineData(@"ProjectFolder\8 project.csproj", @"ProjectFolder\SubFolder\Path1.resx", "", "SubFolder", "SubFolder.Path1")]
    public void FileSettings_RespectsEmptyRootNamespace(
        string msBuildProjectFullPath,
        string mainFile,
        string rootNamespace,
        string expectedLocalNamespace,
        string expectedEmbeddedFilename
    )
    {
	    msBuildProjectFullPath = msBuildProjectFullPath.Replace('\\', Path.DirectorySeparatorChar);
	    mainFile = mainFile.Replace('\\', Path.DirectorySeparatorChar);
        var fileOptions = new FileOptions(
	        groupedFile: new ([
			        ResxFile.From(new AdditionalTextStub(mainFile))!
		        ]
	        ),
            options: new AnalyzerConfigOptionsStub(),
            globalOptions: GlobalOptions.Select(
                provider: new AnalyzerConfigOptionsProviderStub(
                    globalOptions: new AnalyzerConfigOptionsStub
                    {
                        MSBuildProjectName = Path.GetFileNameWithoutExtension(msBuildProjectFullPath),
                        RootNamespace = rootNamespace,
                        MSBuildProjectFullPath = msBuildProjectFullPath
                    },
                    fileOptions: null!
                ),
                token: default
            )
        );
        fileOptions.InnerClassName.ShouldBeNullOrEmpty();
        fileOptions.InnerClassInstanceName.ShouldBeNullOrEmpty();
        fileOptions.InnerClassVisibility.ShouldBe(InnerClassVisibility.NotGenerated);
        fileOptions.NullForgivingOperators.ShouldBe(false);
        fileOptions.StaticClass.ShouldBe(true);
        fileOptions.StaticMembers.ShouldBe(true);
        fileOptions.PublicClass.ShouldBe(false);
        fileOptions.PartialClass.ShouldBe(false);
        fileOptions.UseResManager.ShouldBe(false);
        fileOptions.LocalNamespace.ShouldBe(expectedLocalNamespace);
        fileOptions.CustomToolNamespace.ShouldBeNullOrEmpty();
        fileOptions.GroupedFile.MainFile.File.Path.ShouldBe(mainFile);
        fileOptions.EmbeddedFilename.ShouldBe(expectedEmbeddedFilename);
        fileOptions.ClassName.ShouldBe("Path1");
        fileOptions.IsValid.ShouldBe(true);
    }

    [Fact]
    public void File_PostFix()
    {
        var fileOptions = new FileOptions(
	        groupedFile: new ([
			        ResxFile.From(new AdditionalTextStub("Path1.resx"))!
		        ]
	        ),
            options: new AnalyzerConfigOptionsStub { ClassNamePostfix = "test1" },
            globalOptions: s_globalOptions
        );
        fileOptions.ClassName.ShouldBe("Path1test1");
        fileOptions.IsValid.ShouldBe(true);
    }

    [Fact]
    public void FileSettings_CanReadAll()
    {
        var fileOptions = new FileOptions(
	        groupedFile: new ([
			        ResxFile.From(new AdditionalTextStub("Path1.resx"))!
		        ]
	        ),
            options: new AnalyzerConfigOptionsStub
                {
                    RootNamespace = "namespace1", MSBuildProjectFullPath = "project1.csproj",
                    CustomToolNamespace = "ns1",
                    InnerClassName = "test1",
                    InnerClassInstanceName = "test2",
                    InnerClassVisibility = "public",
                    NullForgivingOperators = "true",
                    StaticClass = "false",
                    StaticMembers = "false",
                    PublicClass = "true",
                    PartialClass = "true",
                    UseResManager = "true",
                },
            globalOptions: s_globalOptions
        );
        fileOptions.InnerClassName.ShouldBe("test1");
        fileOptions.InnerClassInstanceName.ShouldBe("test2");
        fileOptions.InnerClassVisibility.ShouldBe(InnerClassVisibility.Public);
        fileOptions.NullForgivingOperators.ShouldBe(false);
        fileOptions.StaticClass.ShouldBe(false);
        fileOptions.StaticMembers.ShouldBe(false);
        fileOptions.PublicClass.ShouldBe(true);
        fileOptions.PartialClass.ShouldBe(true);
        fileOptions.IsValid.ShouldBe(true);
        fileOptions.UseResManager.ShouldBe(true);
        fileOptions.LocalNamespace.ShouldBe("namespace1");
        fileOptions.CustomToolNamespace.ShouldBe("ns1");
        fileOptions.GroupedFile.MainFile.File.Path.ShouldBe("Path1.resx");
        fileOptions.ClassName.ShouldBe("Path1");
    }

    [Fact]
    public void FileSettings_RespectsGlobalDefaults()
    {
        var globalOptions = GlobalOptions.Select(
            provider: new AnalyzerConfigOptionsProviderStub(
                globalOptions: new AnalyzerConfigOptionsStub
                {
                    RootNamespace = "namespace1",
                    MSBuildProjectFullPath = "project1.csproj",
                    MSBuildProjectName = "project1",
                    ResXFileCodeGenerator_InnerClassName = "test1",
                    ResXFileCodeGenerator_InnerClassInstanceName = "test2",
                    ResXFileCodeGenerator_ClassNamePostfix= "test3",
                    ResXFileCodeGenerator_InnerClassVisibility = "public",
                    ResXFileCodeGenerator_NullForgivingOperators = "true",
                    ResXFileCodeGenerator_StaticClass = "false",
                    ResXFileCodeGenerator_StaticMembers = "false",
                    ResXFileCodeGenerator_PublicClass = "true",
                    ResXFileCodeGenerator_PartialClass = "true",
                },
                fileOptions: null!
            ),
            token: default
        );
        var fileOptions = new FileOptions(
	        groupedFile: new ([
			        ResxFile.From(new AdditionalTextStub("Path1.resx"))!
		        ]
	        ),
            options: new AnalyzerConfigOptionsStub(),
            globalOptions: globalOptions
        );
        fileOptions.InnerClassName.ShouldBe("test1");
        fileOptions.InnerClassInstanceName.ShouldBe("test2");
        fileOptions.InnerClassVisibility.ShouldBe(InnerClassVisibility.Public);
        fileOptions.NullForgivingOperators.ShouldBe(true);
        fileOptions.StaticClass.ShouldBe(false);
        fileOptions.StaticMembers.ShouldBe(false);
        fileOptions.PublicClass.ShouldBe(true);
        fileOptions.PartialClass.ShouldBe(true);
        fileOptions.IsValid.ShouldBe(true);
        fileOptions.UseResManager.ShouldBe(false);
        fileOptions.LocalNamespace.ShouldBe("namespace1");
        fileOptions.CustomToolNamespace.ShouldBeNullOrEmpty();
        fileOptions.GroupedFile.MainFile.File.Path.ShouldBe("Path1.resx");
        fileOptions.ClassName.ShouldBe("Path1test3");
        fileOptions.IsValid.ShouldBe(true);
    }

    private class AnalyzerConfigOptionsStub : AnalyzerConfigOptions
    {
        // ReSharper disable InconsistentNaming
        public string? MSBuildProjectFullPath { get; init; }
        // ReSharper disable InconsistentNaming
        public string? MSBuildProjectName { get; init; }
        public string? RootNamespace { get; init; }
        public string? ResXFileCodeGenerator_ClassNamePostfix { get; init; }
        public string? ResXFileCodeGenerator_PublicClass { get; init; }
        public string? ResXFileCodeGenerator_NullForgivingOperators { get; init; }
        public string? ResXFileCodeGenerator_StaticClass { get; init; }
        public string? ResXFileCodeGenerator_StaticMembers { get; init; }
        public string? ResXFileCodeGenerator_PartialClass { get; init; }
        public string? ResXFileCodeGenerator_InnerClassVisibility { get; init; }
        public string? ResXFileCodeGenerator_InnerClassName { get; init; }
        public string? ResXFileCodeGenerator_InnerClassInstanceName { get; init; }
        public string? ResXFileCodeGenerator_UseResManager { get; init; }
        public string? CustomToolNamespace { get; init; }
        public string? TargetPath { get; init; }
        public string? ClassNamePostfix { get; init; }
        public string? PublicClass { get; init; }
        public string? NullForgivingOperators { get; init; }
        public string? StaticClass { get; init; }
        public string? StaticMembers { get; init; }
        public string? PartialClass { get; init; }
        public string? InnerClassVisibility { get; init; }
        public string? InnerClassName { get; init; }
        public string? InnerClassInstanceName { get; init; }
        public string? UseResManager { get; init; }

        // ReSharper restore InconsistentNaming

        public override bool TryGetValue(string key, [NotNullWhen(true)] out string value)
        {
			var v = GetVal();
			if (v == null)
			{
				value = null!;
				return false;
			}
			value = v;
	        return true;

            string? GetVal() =>
	            key switch
	            {
		            "build_property.MSBuildProjectFullPath" => MSBuildProjectFullPath,
		            "build_property.MSBuildProjectName" => MSBuildProjectName,
		            "build_property.RootNamespace" => RootNamespace,
		            "build_property.ResXFileCodeGenerator_UseResManager" => ResXFileCodeGenerator_UseResManager,
		            "build_property.ResXFileCodeGenerator_ClassNamePostfix" => ResXFileCodeGenerator_ClassNamePostfix,
		            "build_property.ResXFileCodeGenerator_PublicClass" => ResXFileCodeGenerator_PublicClass,
		            "build_property.ResXFileCodeGenerator_NullForgivingOperators" => ResXFileCodeGenerator_NullForgivingOperators,
		            "build_property.ResXFileCodeGenerator_StaticClass" => ResXFileCodeGenerator_StaticClass,
		            "build_property.ResXFileCodeGenerator_StaticMembers" => ResXFileCodeGenerator_StaticMembers,
		            "build_property.ResXFileCodeGenerator_PartialClass" => ResXFileCodeGenerator_PartialClass,
		            "build_property.ResXFileCodeGenerator_InnerClassVisibility" => ResXFileCodeGenerator_InnerClassVisibility,
		            "build_property.ResXFileCodeGenerator_InnerClassName" => ResXFileCodeGenerator_InnerClassName,
		            "build_property.ResXFileCodeGenerator_InnerClassInstanceName" => ResXFileCodeGenerator_InnerClassInstanceName,
		            "build_metadata.EmbeddedResource.CustomToolNamespace" => CustomToolNamespace,
		            "build_metadata.EmbeddedResource.TargetPath" => TargetPath,
		            "build_metadata.EmbeddedResource.ClassNamePostfix" => ClassNamePostfix,
		            "build_metadata.EmbeddedResource.PublicClass" => PublicClass,
		            "build_metadata.EmbeddedResource.NullForgivingOperators" => NullForgivingOperators,
		            "build_metadata.EmbeddedResource.StaticClass" => StaticClass,
		            "build_metadata.EmbeddedResource.StaticMembers" => StaticMembers,
		            "build_metadata.EmbeddedResource.PartialClass" => PartialClass,
		            "build_metadata.EmbeddedResource.InnerClassVisibility" => InnerClassVisibility,
		            "build_metadata.EmbeddedResource.InnerClassName" => InnerClassName,
		            "build_metadata.EmbeddedResource.InnerClassInstanceName" => InnerClassInstanceName,
		            "build_metadata.EmbeddedResource.UseResManager" => UseResManager,
		            _ => null,
	            };
        }
    }

    private class AnalyzerConfigOptionsProviderStub(
	    AnalyzerConfigOptions globalOptions,
	    AnalyzerConfigOptions fileOptions)
	    : AnalyzerConfigOptionsProvider
    {
	    public override AnalyzerConfigOptions GlobalOptions { get; } = globalOptions;

        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => throw new NotImplementedException();

        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => fileOptions;

    }
}

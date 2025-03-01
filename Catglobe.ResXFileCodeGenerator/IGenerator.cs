﻿namespace Catglobe.ResXFileCodeGenerator;

internal interface IGenerator
{
    /// <summary>
    /// Generate source file with properties for each translated resource
    /// </summary>
    (string GeneratedFileName, string SourceCode, ICollection<Diagnostic> ErrorsAndWarnings)
        Generate(FileOptions options, ClassSpec? classSpec = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate helper functions to determine which translated resource to use in the current moment
    /// </summary>
    (string GeneratedFileName, string SourceCode, ICollection<Diagnostic> ErrorsAndWarnings)
        Generate(CultureInfoCombo combo, CancellationToken cancellationToken);

}

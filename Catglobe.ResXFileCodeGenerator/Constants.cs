﻿namespace Catglobe.ResXFileCodeGenerator;

internal static class Constants
{
    public const string SystemDiagnosticsCodeAnalysis =
        $"global::{nameof(System)}.{nameof(System.Diagnostics)}.{nameof(System.Diagnostics.CodeAnalysis)}";

    public const string SystemGlobalization = $"global::{nameof(System)}.{nameof(System.Globalization)}";
    public const string SystemResources = $"global::{nameof(System)}.{nameof(System.Resources)}";

    public const string AutoGeneratedHeader =
        @"// ------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
// ------------------------------------------------------------------------------";

    public const string s_resourceManagerVariable = "s_resourceManager";
    public const string ResourceManagerVariable = "ResourceManager";
    public const string CultureInfoVariable = "CultureInfo";
}

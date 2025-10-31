using System;
using System.Reflection;
using System.Resources;
using System.Runtime.InteropServices;

// Explicitly qualify the Vsix reference to avoid ambiguity
[assembly: AssemblyCompany(ASGV.CodeMaid.Vsix.Author)]
[assembly: AssemblyCopyright("Copyright 2007-2025 Steve Cadwallader/Adhemar Soria Galvarro Vargas (LGPL v3)")]
[assembly: AssemblyDescription(ASGV.CodeMaid.Vsix.Description)]
[assembly: AssemblyFileVersion(ASGV.CodeMaid.Vsix.Version)]
[assembly: AssemblyProduct(ASGV.CodeMaid.Vsix.Name)]
[assembly: AssemblyVersion(ASGV.CodeMaid.Vsix.Version)]
[assembly: CLSCompliant(false)]
[assembly: ComVisible(false)]
[assembly: NeutralResourcesLanguage("en-US", UltimateResourceFallbackLocation.Satellite)]
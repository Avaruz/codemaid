using EnvDTE;
using Microsoft.VisualStudio.Shell;
using System;

namespace ASGV.CodeMaid.Helpers
{
  /// <summary>
  /// A static helper class for working with regions.
  /// </summary>
  internal static class RegionHelper
  {
    #region Internal Methods

    internal static string GetRegionName(EditPoint editPoint, string regionText)
    {
      ThreadHelper.ThrowIfNotOnUIThread();
      CodeLanguage codeLanguage = editPoint.GetCodeLanguage();
      switch (codeLanguage)
      {
        case CodeLanguage.CSharp:
          return regionText.Substring(8).Trim();

        case CodeLanguage.VisualBasic:
          // Remove the leading/trailing double quote character.
          string text = regionText.Substring(8).Trim();
          return text.Substring(1, text.Length - 2);

        default:
          throw new NotImplementedException($"Regions are not supported for '{codeLanguage}'.");
      }
    }

    internal static string GetRegionTagText(EditPoint editPoint, string name = null)
    {
      ThreadHelper.ThrowIfNotOnUIThread();
      CodeLanguage codeLanguage = editPoint.GetCodeLanguage();
      return codeLanguage switch
      {
        CodeLanguage.CSharp => "#region " +
                         (name ?? string.Empty),
        CodeLanguage.VisualBasic => "#Region " +
                         (name != null ? $"\"{name}\"" : string.Empty),
        _ => throw new NotImplementedException($"Regions are not supported for '{codeLanguage}'."),
      };
    }

    internal static string GetEndRegionTagText(EditPoint editPoint)
    {
      CodeLanguage codeLanguage = editPoint.GetCodeLanguage();
      return codeLanguage switch
      {
        CodeLanguage.CSharp => "#endregion",
        CodeLanguage.VisualBasic => "#End Region",
        _ => throw new NotImplementedException($"Regions are not supported for '{codeLanguage}'."),
      };
    }

    internal static bool LanguageSupportsUpdatingEndRegionDirectives(EditPoint editPoint)
    {
      CodeLanguage codeLanguage = editPoint.GetCodeLanguage();

      return codeLanguage switch
      {
        CodeLanguage.CSharp => true,
        _ => false,
      };
    }

    #endregion Internal Methods
  }
}
namespace ASGV.CodeMaid.Helpers
{
    /// <summary>
    /// A helper class for mapping <see cref="CodeLanguage"/> from a string.
    /// </summary>
    internal static class CodeLanguageHelper
    {
        /// <summary>
        /// Gets a <see cref="CodeLanguage"/> based on the specified language string.
        /// </summary>
        /// <param name="language">The language as a string.</param>
        /// <returns>A <see cref="CodeLanguage"/>.</returns>
        internal static CodeLanguage GetCodeLanguage(string language)
        {
      return language switch
      {
        "Basic" => CodeLanguage.VisualBasic,
        "CSharp" => CodeLanguage.CSharp,
        "C/C++" or "C/C++ (VisualGDB)" => CodeLanguage.CPlusPlus,
        "CSS" => CodeLanguage.CSS,
        "F#" => CodeLanguage.FSharp,
        "HTML" or "HTMLX" or "Razor" or "WebForms" => CodeLanguage.HTML,
        "JavaScript" or "JScript" or "Node.js" => CodeLanguage.JavaScript,
        "JSON" => CodeLanguage.JSON,
        "LESS" => CodeLanguage.LESS,
        "PHP" => CodeLanguage.PHP,
        "PowerShell" => CodeLanguage.PowerShell,
        "R" => CodeLanguage.R,
        "SCSS" => CodeLanguage.SCSS,
        "TypeScript" => CodeLanguage.TypeScript,
        "XAML" => CodeLanguage.XAML,
        "XML" => CodeLanguage.XML,
        _ => CodeLanguage.Unknown,
      };
    }
    }
}
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using ASGV.CodeMaid.Properties;
using ASGV.CodeMaid.UI.Enumerations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ASGV.CodeMaid.Helpers
{
  internal static class FileHeaderHelper
  {
    /// <summary>
    /// Gets the file header from settings based on the language of the specified document.
    /// </summary>
    /// <param name="textDocument">The text document.</param>
    /// <returns>A file header from settings.</returns>
    internal static string GetFileHeaderFromSettings(TextDocument textDocument)
    {
      ThreadHelper.ThrowIfNotOnUIThread();
      return textDocument.GetCodeLanguage() switch
      {
        CodeLanguage.CPlusPlus => Settings.Default.Cleaning_UpdateFileHeaderCPlusPlus,
        CodeLanguage.CSharp => Settings.Default.Cleaning_UpdateFileHeaderCSharp,
        CodeLanguage.CSS => Settings.Default.Cleaning_UpdateFileHeaderCSS,
        CodeLanguage.FSharp => Settings.Default.Cleaning_UpdateFileHeaderFSharp,
        CodeLanguage.HTML => Settings.Default.Cleaning_UpdateFileHeaderHTML,
        CodeLanguage.JavaScript => Settings.Default.Cleaning_UpdateFileHeaderJavaScript,
        CodeLanguage.JSON => Settings.Default.Cleaning_UpdateFileHeaderJSON,
        CodeLanguage.LESS => Settings.Default.Cleaning_UpdateFileHeaderLESS,
        CodeLanguage.PHP => Settings.Default.Cleaning_UpdateFileHeaderPHP,
        CodeLanguage.PowerShell => Settings.Default.Cleaning_UpdateFileHeaderPowerShell,
        CodeLanguage.R => Settings.Default.Cleaning_UpdateFileHeaderR,
        CodeLanguage.SCSS => Settings.Default.Cleaning_UpdateFileHeaderSCSS,
        CodeLanguage.TypeScript => Settings.Default.Cleaning_UpdateFileHeaderTypeScript,
        CodeLanguage.VisualBasic => Settings.Default.Cleaning_UpdateFileHeaderVB,
        CodeLanguage.XAML => Settings.Default.Cleaning_UpdateFileHeaderXAML,
        CodeLanguage.XML => Settings.Default.Cleaning_UpdateFileHeaderXML,
        _ => null,
      };
    }

    internal static HeaderPosition GetFileHeaderPositionFromSettings(TextDocument textDocument)
    {
      ThreadHelper.ThrowIfNotOnUIThread();
      return textDocument.GetCodeLanguage() switch
      {
        CodeLanguage.CSharp => (HeaderPosition)Settings.Default.Cleaning_UpdateFileHeader_HeaderPosition,
        _ => HeaderPosition.DocumentStart,
      };
    }

    internal static int GetHeaderLength(CodeLanguage language, string text, bool skipUsings = false)
    {
      return language switch
      {
        CodeLanguage.CSharp => GetHeaderLength(text, "//", skipUsings) +
                      GetHeaderLength(text, "/*", "*/", skipUsings),
        CodeLanguage.CPlusPlus or CodeLanguage.JavaScript or CodeLanguage.LESS or CodeLanguage.SCSS or CodeLanguage.TypeScript => GetHeaderLength(text, "//") +
                      GetHeaderLength(text, "/*", "*/"),
        CodeLanguage.HTML or CodeLanguage.XAML or CodeLanguage.XML => GetHeaderLength(text, "<!--", "-->"),
        CodeLanguage.CSS => GetHeaderLength(text, "/*", "*/"),
        CodeLanguage.PHP => GetHeaderLength(text, "//") +
                      GetHeaderLength(text, "#") +
                      GetHeaderLength(text, "/*", "*/"),
        CodeLanguage.PowerShell => GetHeaderLength(text, "#") +
                      GetHeaderLength(text, "<#", "#>"),
        CodeLanguage.R => GetHeaderLength(text, "#"),
        CodeLanguage.FSharp => GetHeaderLength(text, "//") +
                      GetHeaderLength(text, "(*", "*)"),
        CodeLanguage.VisualBasic => GetHeaderLength(text, "'"),
        _ => 0,
      };
    }

    internal static int GetHeaderLength(string text, string commentSyntax, bool skipUsings)
    {
      return skipUsings ? GetHeaderLengthSkipUsings(text, commentSyntax) : GetHeaderLength(text, commentSyntax);
    }

    internal static int GetHeaderLength(string text, string commentSyntaxStart, string commentSyntaxEnd, bool skipUsings)
    {
      return skipUsings
        ? GetHeaderLengthSkipUsings(text, commentSyntaxStart, commentSyntaxEnd)
              : GetHeaderLength(text, commentSyntaxStart, commentSyntaxEnd);
    }

    /// <summary>
    /// Gets the number of lines to skip to pass occurrences of <paramref name="startOfLine"/>
    /// </summary>
    /// <param name="startOfLine">The pattern of the start of lines we want to skip</param>
    /// <param name="text">The document to search</param>
    /// <param name="limits">The limits not to pass. <paramref name="startOfLine"/> beyond those limits are ignored</param>
    /// <returns>The number of lines to skip</returns>
    internal static int GetNbLinesToSkip(string startOfLine, string text, IEnumerable<string> limits)
    {
      int max = GetLowestIndex(text, limits);
      string potentialTextBlock = text.Substring(0, max);
      int lastIndex = potentialTextBlock.LastIndexOf(startOfLine);

      if (lastIndex == -1)
      {
        return 0;
      }

      string relevantTextBlock = potentialTextBlock.Substring(0, lastIndex);

      return Regex.Matches(relevantTextBlock, Environment.NewLine).Count + 1;
    }

    private static IEnumerable<string> GetEmptyLines(IEnumerable<string> lines, int nbLinesToSkip)
    {
      List<string> result = [];

      foreach (string line in lines.Skip(nbLinesToSkip))
      {
        if (!string.IsNullOrWhiteSpace(line))
        {
          break;
        }

        result.Add(line);
      }

      return result;
    }

    /// <summary>
    /// Computes the length of the header
    /// </summary>
    /// <param name="text">The beginning of the document containing the header</param>
    /// <param name="commentSyntax">The syntax of the comment tag in the processed language</param>
    /// <returns>The header length</returns>
    /// <remarks>EnvDTE API only counts 1 character per end of line (\r\n counts for 1)</remarks>
    private static int GetHeaderLength(string text, string commentSyntax)
    {
      if (!text.TrimStart().StartsWith(commentSyntax))
      {
        return 0;
      }

      IEnumerable<string> lines = SplitLines(text);
      List<string> header = new();

      header.AddRange(GetEmptyLines(lines, 0));
      header.AddRange(GetLinesStartingWith(commentSyntax, lines, header.Count));

      int nbChar = 0;

      if (header.Count == 0)
      {
        return 0;
      }

      header.ToList().ForEach(x => nbChar += x.Length + 1);

      return nbChar;
    }

    /// <summary>
    /// Computes the length of the header
    /// </summary>
    /// <param name="text">The beginning of the document containing the header</param>
    /// <param name="commentSyntaxStart">The syntax of the comment tag start in the processed language</param>
    /// <param name="commentSyntaxEnd">The syntax of the comment tag end in the processed language</param>
    /// <returns>The header length</returns>
    /// <remarks>EnvDTE API only counts 1 character per end of line (\r\n counts for 1)</remarks>
    private static int GetHeaderLength(string text, string commentSyntaxStart, string commentSyntaxEnd)
    {
      if (!text.TrimStart().StartsWith(commentSyntaxStart) || text.IndexOf(commentSyntaxEnd) == -1)
      {
        return 0;
      }

      IEnumerable<string> lines = SplitLines(text);
      List<string> header = new();

      header.AddRange(GetEmptyLines(lines, 0));

      foreach (string line in lines.Skip(header.Count))
      {
        header.Add(line);

        if (line.TrimEnd().EndsWith(commentSyntaxEnd))
        {
          break;
        }
      }

      int nbChar = 0;

      if (header.Count == 0)
      {
        return 0;
      }

      header.ToList().ForEach(x => nbChar += x.Length + 1);

      return nbChar;
    }

    private static int GetHeaderLengthSkipUsings(string text, string commentSyntax)
    {
      text = SkipUsings(text);

      IEnumerable<string> lines = SplitLines(text);
      List<string> header = new();
      header.AddRange(GetLinesStartingWith(commentSyntax, lines));

      int nbChar = 0;
      header.ToList().ForEach(x => nbChar += x.Length + 1);

      return nbChar == 0 ? 0 : nbChar + 1;
    }

    private static int GetHeaderLengthSkipUsings(string text, string commentSyntaxStart, string commentSyntaxEnd)
    {
      text = SkipUsings(text);

      int startIndex = text.IndexOf(commentSyntaxStart);
      int endIndex = text.IndexOf(commentSyntaxEnd);

      if (startIndex == -1 || endIndex == -1)
      {
        return 0;
      }

      string header = text.Substring(startIndex, endIndex - startIndex);
      int nbNewLines = Regex.Matches(header, Environment.NewLine).Count;

      return header.Length == 0 && nbNewLines == 0 ? 0 : header.Length + commentSyntaxEnd.Length - nbNewLines + 1;
    }

    private static IEnumerable<string> GetLinesStartingWith(string pattern, IEnumerable<string> lines, int nbLinesToSkip = 0)
    {
      List<string> result = [];

      foreach (string line in lines.Skip(nbLinesToSkip))
      {
        if (!line.StartsWith(pattern))
        {
          break;
        }

        result.Add(line);
      }

      return result;
    }

    /// <summary>
    /// Looks for the index of the first limit found in the text
    /// </summary>
    /// <param name="text">The text to search in</param>
    /// <param name="limits">The limits to search for</param>
    /// <returns>Lowest index of all the limits found</returns>
    private static int GetLowestIndex(string text, IEnumerable<string> limits)
    {
      List<int> indexes = [];

      foreach (string limit in limits)
      {
        int limitIndex = text.IndexOf(limit);

        if (limitIndex > -1)
        {
          indexes.Add(limitIndex);
        }
      }

      return indexes.Count == 0 ? text.Length : indexes.Min();
    }

    private static string SkipUsings(string document)
    {
      // we cannot simply look for the last using since it can be used inside the code
      // so we look for the last using before namespace
      int namespaceIndex = document.IndexOf("namespace ");
      int startIndex = 0;
      int lastUsingIndex = 0;

      while (startIndex < namespaceIndex)
      {
        lastUsingIndex = startIndex;
        startIndex = document.IndexOf("using ", startIndex);

        if (startIndex++ == -1)
        {
          break;
        }
      }

      int afterUsingIndex = 0;

      if (lastUsingIndex > 0)
      {
        afterUsingIndex = document.IndexOf($"{Environment.NewLine}", lastUsingIndex) + 1;
      }

      return document.Substring(afterUsingIndex).TrimStart();
    }

    private static IEnumerable<string> SplitLines(string text)
    {
      string[] separator = new string[] { Environment.NewLine };

      return text.Split(separator, StringSplitOptions.None);
    }
  }
}
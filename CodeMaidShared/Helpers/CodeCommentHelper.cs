using EnvDTE;
using Microsoft.VisualStudio.Shell;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

namespace ASGV.CodeMaid.Helpers
{
    /// <summary>
    /// A set of helper methods focused around code comments.
    /// </summary>
    internal static class CodeCommentHelper
    {
        public const int CopyrightExtraIndent = 4;
        public const char KeepTogetherSpacer = '\a';
        public const char Spacer = ' ';

        internal static string FakeToSpace(string value)
        {
            return value.Replace(KeepTogetherSpacer, Spacer);
        }

        /// <summary>
        /// Get the comment prefix (regex) for the given document's language.
        /// </summary>
        /// <param name="document">The document.</param>
        /// <returns>The comment prefix regex, without trailing spaces.</returns>
        internal static string GetCommentPrefix(TextDocument document)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            return GetCommentPrefixForLanguage(document.GetCodeLanguage());
        }

        /// <summary>
        /// Get the comment prefix (regex) for the specified code language.
        /// </summary>
        /// <param name="codeLanguage">The code language.</param>
        /// <returns>The comment prefix regex, without trailing spaces.</returns>
        internal static string GetCommentPrefixForLanguage(CodeLanguage codeLanguage)
        {
            return codeLanguage switch
            {
                CodeLanguage.CPlusPlus or CodeLanguage.CSharp or CodeLanguage.CSS or CodeLanguage.FSharp or CodeLanguage.JavaScript or CodeLanguage.LESS or CodeLanguage.PHP or CodeLanguage.SCSS or CodeLanguage.TypeScript => "///?",
                CodeLanguage.PowerShell or CodeLanguage.R => "#+",
                CodeLanguage.VisualBasic => "'+",
                _ => null,
            };
        }

        /// <summary>
        /// Gets the regex for matching a complete comment line.
        /// </summary>
        internal static Regex GetCommentRegex(CodeLanguage codeLanguage, bool includePrefix = true)
        {
            string prefix = null;
            if (includePrefix)
            {
                prefix = GetCommentPrefixForLanguage(codeLanguage);
                if (prefix == null)
                {
                    Debug.Fail("Attempting to create a comment regex for a document that has no comment prefix specified.");
                }

                // Be aware of the added space to the prefix. When prefix is added, we should take
                // care not to match code comment lines.
                prefix = string.Format(@"(?<prefix>[\t ]*{0})(?<initialspacer>( |\t|\r|\n|$))?", prefix);
            }

            string pattern = string.Format(@"^{0}(?<indent>[\t ]*)(?<line>(?<listprefix>[-=\*\+]+[ \t]*|\w+[\):][ \t]+|\d+\.[ \t]+)?((?<words>[^\t\r\n ]+)*[\t ]*)*)\r*\n?$", prefix);
            return new Regex(pattern, RegexOptions.ExplicitCapture | RegexOptions.Multiline);
        }

        /// <summary>
        /// Gets the list of tokens defined in Tools &gt; Options &gt; Environment &gt; Task List.
        /// </summary>
        /// <param name="package"></param>
        /// <returns></returns>
        public static IEnumerable<string> GetTaskListTokens(CodeMaidPackage package)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            EnvDTE.Properties settings = package.IDE.Properties["Environment", "TaskList"];
            if (settings.Item("CommentTokens").Value is not string[] tokens || tokens.Length < 1)
            {
                return [];
            }

            // Tokens values are written like "NAME:PRIORITY". We want only the names, and require
            // that they are followed by a semicolon and a space.
            return tokens.Select(t => t.Substring(0, t.LastIndexOf(':') + 1) + " ");
        }

        internal static bool IsCommentLine(EditPoint point)
        {
            return LineMatchesRegex(point, GetCommentRegex(point.GetCodeLanguage())).Success;
        }

        internal static Match LineMatchesRegex(EditPoint point, Regex regex)
        {
            string line = point.GetLine();
            Match match = regex.Match(line);
            return match;
        }

        internal static string SpaceToFake(string value)
        {
            return value.Replace(Spacer, KeepTogetherSpacer);
        }
    }
}
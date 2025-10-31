using System.Text.RegularExpressions;

namespace ASGV.CodeMaid.Model.Comments.Options
{
    /// <summary>
    /// Comment specific options for the formatter.
    /// </summary>
    internal class CommentOptions
    {
        public string Prefix { get; internal set; }

        public Regex Regex { get; internal set; }
    }
}
using System.Linq;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using ASGV.CodeMaid.Helpers;
using ASGV.CodeMaid.Model.Comments;
using ASGV.CodeMaid.Model.Comments.Options;
using ASGV.CodeMaid.Properties;

namespace ASGV.CodeMaid.Logic.Formatting
{
  /// <summary>
  /// A class for encapsulating comment formatting logic.
  /// </summary>
  internal sealed class CommentFormatLogic
  {
    #region Fields

    /// <summary>
    /// The singleton instance of the <see cref="CommentFormatLogic" /> class.
    /// </summary>
    private static CommentFormatLogic _instance;

    private readonly CodeMaidPackage _package;

    #endregion Fields

    #region Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="CommentFormatLogic" /> class.
    /// </summary>
    /// <param name="package">The hosting package.</param>
    private CommentFormatLogic(CodeMaidPackage package)
    {
      _package = package;
    }

    #endregion Constructors

    #region Methods

    /// <summary>
    /// Reformat all comments in the specified document.
    /// </summary>
    /// <param name="textDocument">The text document.</param>
    public void FormatComments(TextDocument textDocument)
    {
      ThreadHelper.ThrowIfNotOnUIThread(); // Ensure this method runs on the UI thread

      if (!Settings.Default.Formatting_CommentRunDuringCleanup)
      {
        return;
      }

      FormatComments(textDocument, textDocument.StartPoint.CreateEditPoint(), textDocument.EndPoint.CreateEditPoint());
    }

    /// <summary>
    /// Reformat all comments between the specified start and end point. Comments that start
    /// within the range, even if they overlap the end are included.
    /// </summary>
    /// <param name="textDocument">The text document.</param>
    /// <param name="start">The start point.</param>
    /// <param name="end">The end point.</param>
    public bool FormatComments(TextDocument textDocument, EditPoint start, EditPoint end)
    {
      ThreadHelper.ThrowIfNotOnUIThread(); // Ensure this method runs on the UI thread

      bool foundComments = false;
      FormatterOptions options = FormatterOptions
          .FromSettings(Settings.Default)
          .Set(o =>
          {
            ThreadHelper.ThrowIfNotOnUIThread();
            o.TabSize = textDocument.TabSize;
            o.IgnoreTokens =
            [
              .. CodeCommentHelper
                                    .GetTaskListTokens(_package)
,
              .. Settings.Default.Formatting_IgnoreLinesStartingWith.Cast<string>(),
            ];
          });

      while (start.Line <= end.Line)
      {
        if (CodeCommentHelper.IsCommentLine(start))
        {
          CodeComment comment = new(start, options);

          if (comment.IsValid)
          {
            comment.Format();
            foundComments = true;
          }

          if (comment.EndPoint != null)
          {
            start = comment.EndPoint.CreateEditPoint();
          }
        }

        if (start.Line == textDocument.EndPoint.Line)
        {
          break;
        }

        start.LineDown();
        start.StartOfLine();
      }

      return foundComments;
    }

    /// <summary>
    /// Gets an instance of the <see cref="CommentFormatLogic"/> class.
    /// </summary>
    /// <param name="package">The hosting package.</param>
    /// <returns>An instance of the <see cref="CommentFormatLogic"/> class.</returns>
    internal static CommentFormatLogic GetInstance(CodeMaidPackage package)
    {
      return _instance ??= new CommentFormatLogic(package);
    }

    #endregion Methods
  }
}
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using ASGV.CodeMaid.Helpers;
using ASGV.CodeMaid.Model.CodeItems;
using ASGV.CodeMaid.Properties;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ASGV.CodeMaid.Logic.Cleaning
{
  /// <summary>
  /// A class for encapsulating insertion of blank line padding logic.
  /// </summary>
  internal sealed class InsertBlankLinePaddingLogic
  {
    #region Fields

    private readonly CodeMaidPackage _package;

    #endregion Fields

    #region Constructors

    /// <summary>
    /// The singleton instance of the <see cref="InsertBlankLinePaddingLogic" /> class.
    /// </summary>
    private static InsertBlankLinePaddingLogic _instance;

    /// <summary>
    /// Gets an instance of the <see cref="InsertBlankLinePaddingLogic" /> class.
    /// </summary>
    /// <param name="package">The hosting package.</param>
    /// <returns>An instance of the <see cref="InsertBlankLinePaddingLogic" /> class.</returns>
    internal static InsertBlankLinePaddingLogic GetInstance(CodeMaidPackage package)
    {
      return _instance ??= new InsertBlankLinePaddingLogic(package);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InsertBlankLinePaddingLogic" /> class.
    /// </summary>
    /// <param name="package">The hosting package.</param>
    private InsertBlankLinePaddingLogic(CodeMaidPackage package)
    {
      _package = package;
    }

    #endregion Constructors

    #region Calculation Methods

    /// <summary>
    /// Determines if the specified code item instance should be preceded by a blank line.
    /// Defaults to false for unknown kinds or null objects.
    /// </summary>
    /// <param name="codeItem">The code item.</param>
    /// <returns>True if code item should be preceded by a blank line, otherwise false.</returns>
    internal bool ShouldBePrecededByBlankLine(BaseCodeItem codeItem)
    {
      return codeItem != null && codeItem.Kind switch
      {
        KindCodeItem.Class => Settings.Default.Cleaning_InsertBlankLinePaddingBeforeClasses,
        KindCodeItem.Delegate => Settings.Default.Cleaning_InsertBlankLinePaddingBeforeDelegates,
        KindCodeItem.Enum => Settings.Default.Cleaning_InsertBlankLinePaddingBeforeEnumerations,
        KindCodeItem.Event => Settings.Default.Cleaning_InsertBlankLinePaddingBeforeEvents,
        KindCodeItem.Field => codeItem.IsMultiLine
                      ? Settings.Default.Cleaning_InsertBlankLinePaddingBeforeFieldsMultiLine
                      : Settings.Default.Cleaning_InsertBlankLinePaddingBeforeFieldsSingleLine,
        KindCodeItem.Interface => Settings.Default.Cleaning_InsertBlankLinePaddingBeforeInterfaces,
        KindCodeItem.Namespace => Settings.Default.Cleaning_InsertBlankLinePaddingBeforeNamespaces,
        KindCodeItem.Constructor or KindCodeItem.Destructor or KindCodeItem.Method => Settings.Default.Cleaning_InsertBlankLinePaddingBeforeMethods,
        KindCodeItem.Indexer or KindCodeItem.Property => codeItem.IsMultiLine
                      ? Settings.Default.Cleaning_InsertBlankLinePaddingBeforePropertiesMultiLine
                      : Settings.Default.Cleaning_InsertBlankLinePaddingBeforePropertiesSingleLine,
        KindCodeItem.Region => Settings.Default.Cleaning_InsertBlankLinePaddingBeforeRegionTags,
        KindCodeItem.Struct => Settings.Default.Cleaning_InsertBlankLinePaddingBeforeStructs,
        KindCodeItem.Using => Settings.Default.Cleaning_InsertBlankLinePaddingBeforeUsingStatementBlocks,
        _ => false,
      };
    }

    /// <summary>
    /// Determines if the specified code item instance should be followed by a blank line.
    /// Defaults to false for unknown kinds or null objects.
    /// </summary>
    /// <param name="codeItem">The code item.</param>
    /// <returns>True if code item should be followed by a blank line, otherwise false.</returns>
    internal bool ShouldBeFollowedByBlankLine(BaseCodeItem codeItem)
    {
      return codeItem != null && codeItem.Kind switch
      {
        KindCodeItem.Class => Settings.Default.Cleaning_InsertBlankLinePaddingAfterClasses,
        KindCodeItem.Delegate => Settings.Default.Cleaning_InsertBlankLinePaddingAfterDelegates,
        KindCodeItem.Enum => Settings.Default.Cleaning_InsertBlankLinePaddingAfterEnumerations,
        KindCodeItem.Event => Settings.Default.Cleaning_InsertBlankLinePaddingAfterEvents,
        KindCodeItem.Field => codeItem.IsMultiLine
                      ? Settings.Default.Cleaning_InsertBlankLinePaddingAfterFieldsMultiLine
                      : Settings.Default.Cleaning_InsertBlankLinePaddingAfterFieldsSingleLine,
        KindCodeItem.Interface => Settings.Default.Cleaning_InsertBlankLinePaddingAfterInterfaces,
        KindCodeItem.Namespace => Settings.Default.Cleaning_InsertBlankLinePaddingAfterNamespaces,
        KindCodeItem.Constructor or KindCodeItem.Destructor or KindCodeItem.Method => Settings.Default.Cleaning_InsertBlankLinePaddingAfterMethods,
        KindCodeItem.Indexer or KindCodeItem.Property => codeItem.IsMultiLine
                      ? Settings.Default.Cleaning_InsertBlankLinePaddingAfterPropertiesMultiLine
                      : Settings.Default.Cleaning_InsertBlankLinePaddingAfterPropertiesSingleLine,
        KindCodeItem.Region => Settings.Default.Cleaning_InsertBlankLinePaddingAfterEndRegionTags,
        KindCodeItem.Struct => Settings.Default.Cleaning_InsertBlankLinePaddingAfterStructs,
        KindCodeItem.Using => Settings.Default.Cleaning_InsertBlankLinePaddingAfterUsingStatementBlocks,
        _ => false,
      };
    }

    #endregion Calculation Methods

    #region Insertion Methods

    /// <summary>
    /// Inserts a blank line before #region tags except where adjacent to a brace.
    /// </summary>
    /// <param name="regions">The regions to pad.</param>
    internal void InsertPaddingBeforeRegionTags(IEnumerable<CodeItemRegion> regions)
    {
      if (!Settings.Default.Cleaning_InsertBlankLinePaddingBeforeRegionTags)
      {
        return;
      }
      ThreadHelper.ThrowIfNotOnUIThread();
      foreach (CodeItemRegion region in regions.Where(x => !x.IsInvalidated))
      {
        EditPoint startPoint = region.StartPoint.CreateEditPoint();

        TextDocumentHelper.InsertBlankLineBeforePoint(startPoint);
      }
    }

    /// <summary>
    /// Inserts a blank line after #region tags except where adjacent to a brace.
    /// </summary>
    /// <param name="regions">The regions to pad.</param>
    internal void InsertPaddingAfterRegionTags(IEnumerable<CodeItemRegion> regions)
    {
      if (!Settings.Default.Cleaning_InsertBlankLinePaddingAfterRegionTags)
      {
        return;
      }
      ThreadHelper.ThrowIfNotOnUIThread();
      foreach (CodeItemRegion region in regions.Where(x => !x.IsInvalidated))
      {
        EditPoint startPoint = region.StartPoint.CreateEditPoint();

        TextDocumentHelper.InsertBlankLineAfterPoint(startPoint);
      }
    }

    /// <summary>
    /// Inserts a blank line before #endregion tags except where adjacent to a brace.
    /// </summary>
    /// <param name="regions">The regions to pad.</param>
    internal void InsertPaddingBeforeEndRegionTags(IEnumerable<CodeItemRegion> regions)
    {
      if (!Settings.Default.Cleaning_InsertBlankLinePaddingBeforeEndRegionTags)
      {
        return;
      }
      ThreadHelper.ThrowIfNotOnUIThread();
      foreach (CodeItemRegion region in regions.Where(x => !x.IsInvalidated))
      {
        EditPoint endPoint = region.EndPoint.CreateEditPoint();

        TextDocumentHelper.InsertBlankLineBeforePoint(endPoint);
      }
    }

    /// <summary>
    /// Inserts a blank line after #endregion tags except where adjacent to a brace.
    /// </summary>
    /// <param name="regions">The regions to pad.</param>
    internal void InsertPaddingAfterEndRegionTags(IEnumerable<CodeItemRegion> regions)
    {
      if (!Settings.Default.Cleaning_InsertBlankLinePaddingAfterEndRegionTags)
      {
        return;
      }
      ThreadHelper.ThrowIfNotOnUIThread();
      foreach (CodeItemRegion region in regions.Where(x => !x.IsInvalidated))
      {
        EditPoint endPoint = region.EndPoint.CreateEditPoint();

        TextDocumentHelper.InsertBlankLineAfterPoint(endPoint);
      }
    }

    /// <summary>
    /// Inserts a blank line before the specified code elements except where adjacent to a brace.
    /// </summary>
    /// <typeparam name="T">The type of the code element.</typeparam>
    /// <param name="codeElements">The code elements to pad.</param>
    internal void InsertPaddingBeforeCodeElements<T>(IEnumerable<T> codeElements)
        where T : BaseCodeItemElement
    {
      foreach (T codeElement in codeElements.Where(ShouldBePrecededByBlankLine))
      {
        TextDocumentHelper.InsertBlankLineBeforePoint(codeElement.StartPoint);
      }
    }

    /// <summary>
    /// Inserts a blank line after the specified code elements except where adjacent to a brace.
    /// </summary>
    /// <typeparam name="T">The type of the code element.</typeparam>
    /// <param name="codeElements">The code elements to pad.</param>
    internal void InsertPaddingAfterCodeElements<T>(IEnumerable<T> codeElements)
        where T : BaseCodeItemElement
    {
      ThreadHelper.ThrowIfNotOnUIThread();
      foreach (T codeElement in codeElements.Where(ShouldBeFollowedByBlankLine))
      {
        TextDocumentHelper.InsertBlankLineAfterPoint(codeElement.EndPoint);
      }
    }

    /// <summary>
    /// Inserts a blank line before case statements except for single-line case statements.
    /// </summary>
    /// <param name="textDocument">The text document.</param>
    internal void InsertPaddingBeforeCaseStatements(TextDocument textDocument)
    {
      if (!Settings.Default.Cleaning_InsertBlankLinePaddingBeforeCaseStatements)
      {
        return;
      }

      const string pattern = @"(^[ \t]*)(break;|return([ \t][^;]*)?;)\r?\n([ \t]*)(case|default)";
      string replacement = "$1$2" + Environment.NewLine + Environment.NewLine + "$4$5";

      TextDocumentHelper.SubstituteAllStringMatches(textDocument, pattern, replacement);
    }

    /// <summary>
    /// Inserts a blank line before single line comments except where adjacent to a brace,
    /// another single line comment line or a quadruple slash comment.
    /// </summary>
    /// <param name="textDocument">The text document.</param>
    internal void InsertPaddingBeforeSingleLineComments(TextDocument textDocument)
    {
      if (!Settings.Default.Cleaning_InsertBlankLinePaddingBeforeSingleLineComments)
      {
        return;
      }

      const string pattern = @"(^[ \t]*(?!//)[^ \t\r\n\{].*\r?\n)([ \t]*//)(?!//)";
      string replacement = "$1" + Environment.NewLine + "$2";

      TextDocumentHelper.SubstituteAllStringMatches(textDocument, pattern, replacement);
    }

    /// <summary>
    /// Inserts a blank line between multi-line property accessors.
    /// </summary>
    /// <param name="properties">The properties.</param>
    internal void InsertPaddingBetweenMultiLinePropertyAccessors(IEnumerable<CodeItemProperty> properties)
    {
      if (!Settings.Default.Cleaning_InsertBlankLinePaddingBetweenPropertiesMultiLineAccessors)
      {
        return;
      }

      ThreadHelper.ThrowIfNotOnUIThread();
      foreach (CodeItemProperty property in properties)
      {
        CodeFunction getter = property.CodeProperty.Getter;
        CodeFunction setter = property.CodeProperty.Setter;

        if (getter != null && setter != null && (getter.StartPoint.Line < getter.EndPoint.Line ||
                                                 setter.StartPoint.Line < setter.EndPoint.Line))
        {
          TextDocumentHelper.InsertBlankLineAfterPoint(setter.EndPoint.Line > getter.EndPoint.Line
                                                           ? getter.EndPoint.CreateEditPoint()
                                                           : setter.EndPoint.CreateEditPoint());
        }
      }
    }

    #endregion Insertion Methods
  }
}
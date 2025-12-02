using EnvDTE;
using Microsoft.VisualStudio.Shell;
using ASGV.CodeMaid.Helpers;
using ASGV.CodeMaid.Model.CodeItems;
using System.Collections.Generic;
using System.Linq;

namespace ASGV.CodeMaid.Model
{
    /// <summary>
    /// A helper class for working with the code model.
    /// </summary>
    internal sealed class CodeModelHelper
    {
        #region Fields

        private readonly CodeMaidPackage _package;

        #endregion Fields

        #region Constructors

        /// <summary>
        /// The singleton instance of the <see cref="CodeModelHelper" /> class.
        /// </summary>
        private static CodeModelHelper _instance;

        /// <summary>
        /// Initializes a new instance of the <see cref="CodeModelHelper" /> class.
        /// </summary>
        /// <param name="package">The hosting package.</param>
        private CodeModelHelper(CodeMaidPackage package)
        {
            _package = package;
        }

        /// <summary>
        /// Gets an instance of the <see cref="CodeModelHelper" /> class.
        /// </summary>
        /// <param name="package">The hosting package.</param>
        /// <returns>An instance of the <see cref="CodeModelHelper" /> class.</returns>
        internal static CodeModelHelper GetInstance(CodeMaidPackage package)
        {
            return _instance ??= new CodeModelHelper(package);
        }

        #endregion Constructors

        #region Internal Methods

        /// <summary>
        /// Gets the specified code items as unique blocks by consecutive line positioning.
        /// </summary>
        /// <typeparam name="T">The type of the code item.</typeparam>
        /// <param name="codeItems">The code items.</param>
        /// <returns>An enumerable collection of blocks of code items.</returns>
        internal static IEnumerable<IList<T>> GetCodeItemBlocks<T>(IEnumerable<T> codeItems)
            where T : BaseCodeItem
        {
            List<IList<T>> codeItemBlocks = [];
            IList<T> currentBlock = null;

            IOrderedEnumerable<T> orderedCodeItems = codeItems.OrderBy(x => x.StartLine);
            foreach (T codeItem in orderedCodeItems)
            {
                if (currentBlock != null &&
                    (codeItem.StartLine <= currentBlock.Last().EndLine + 1))
                {
                    // This item belongs in the current block, add it.
                    currentBlock.Add(codeItem);
                }
                else
                {
                    // This item starts a new block, create one.
                    currentBlock = [codeItem];
                    codeItemBlocks.Add(currentBlock);
                }
            }

            return codeItemBlocks;
        }

        /// <summary>
        /// Determines if there is a region under the cursor for the specified text document.
        /// </summary>
        /// <param name="textDocument">The text document.</param>
        /// <returns>True if there is a region under the cursor, otherwise false.</returns>
        internal bool IsCodeRegionUnderCursor(TextDocument textDocument)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (textDocument?.Selection != null)
            {
                EditPoint cursor = textDocument.GetEditPointAtCursor();
                string currentLineText = cursor.GetLine();

                return RegexNullSafe.IsMatch(currentLineText, RegionPattern);
            }

            return false;
        }

        /// <summary>
        /// Retrieves code regions from the specified text document.
        /// </summary>
        /// <param name="textDocument">The text document to walk.</param>
        /// <returns>An enumerable collection of regions.</returns>
        internal IEnumerable<CodeItemRegion> RetrieveCodeRegions(TextDocument textDocument)
        {
            IEnumerable<EditPoint> editPoints = TextDocumentHelper.FindMatches(textDocument, RegionPattern);

            return RetrieveCodeRegions(editPoints);
        }

        /// <summary>
        /// Retrieves code regions from the specified text selection.
        /// </summary>
        /// <param name="textSelection">The text selection to walk.</param>
        /// <returns>An enumerable collection of regions.</returns>
        internal IEnumerable<CodeItemRegion> RetrieveCodeRegions(TextSelection textSelection)
        {
            IEnumerable<EditPoint> editPoints = TextDocumentHelper.FindMatches(textSelection, RegionPattern);

            return RetrieveCodeRegions(editPoints);
        }

        /// <summary>
        /// Retrieves the region under the cursor for the specified text document.
        /// </summary>
        /// <param name="textDocument">The text document.</param>
        /// <returns>The region under the cursor, otherwise null.</returns>
        internal CodeItemRegion RetrieveCodeRegionUnderCursor(TextDocument textDocument)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (IsCodeRegionUnderCursor(textDocument))
            {
                IEnumerable<CodeItemRegion> regions = RetrieveCodeRegions(textDocument);
                int currentLine = textDocument.Selection.CurrentLine;

                return regions.FirstOrDefault(x => x.StartLine == currentLine || x.EndLine == currentLine);
            }

            return null;
        }

        #endregion Internal Methods

        #region Private Properties

        /// <summary>
        /// Gets the regular expression pattern for region matching.
        /// </summary>
        private string RegionPattern => @"^[ \t]*#([Rr]egion|endregion|End Region)";

        #endregion Private Properties

        #region Private Methods

        /// <summary>
        /// Retrieves code regions based on the specified edit points.
        /// </summary>
        /// <param name="editPoints">The edit points to walk.</param>
        /// <returns>An enumerable collection of regions.</returns>
        private static IEnumerable<CodeItemRegion> RetrieveCodeRegions(IEnumerable<EditPoint> editPoints)
        {
            Stack<CodeItemRegion> regionStack = new();
            List<CodeItemRegion> codeItems = [];
            ThreadHelper.ThrowIfNotOnUIThread();
            foreach (EditPoint cursor in editPoints)
            {
                // Create a pointer to capture the text for this line.
                EditPoint eolCursor = cursor.CreateEditPoint();
                eolCursor.EndOfLine();
                string regionText = cursor.GetText(eolCursor).TrimStart(' ', '\t');

                if (regionText.StartsWith(RegionHelper.GetRegionTagText(cursor)))
                {
                    // Get the region name.
                    string regionName = RegionHelper.GetRegionName(cursor, regionText);

                    // Push the parsed region info onto the top of the stack.
                    regionStack.Push(new CodeItemRegion
                    {
                        Name = regionName,
                        StartLine = cursor.Line,
                        StartOffset = cursor.AbsoluteCharOffset,
                        StartPoint = cursor.CreateEditPoint()
                    });
                }
                else if (regionText.StartsWith(RegionHelper.GetEndRegionTagText(cursor)))
                {
                    if (regionStack.Count > 0)
                    {
                        CodeItemRegion region = regionStack.Pop();
                        region.EndLine = eolCursor.Line;
                        region.EndOffset = eolCursor.AbsoluteCharOffset;
                        region.EndPoint = eolCursor.CreateEditPoint();

                        codeItems.Add(region);
                    }
                    else
                    {
                        // This document is improperly formatted, abort.
                        return [];
                    }
                }
            }

            return codeItems;
        }

        #endregion Private Methods
    }
}
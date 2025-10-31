using EnvDTE;
using Microsoft.VisualStudio.Shell;
using ASGV.CodeMaid.Helpers;
using ASGV.CodeMaid.Logic.Cleaning;
using ASGV.CodeMaid.Model;
using ASGV.CodeMaid.Model.CodeItems;
using ASGV.CodeMaid.Model.CodeTree;
using ASGV.CodeMaid.Properties;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ASGV.CodeMaid.Logic.Reorganizing
{
  /// <summary>
  /// A manager class for reorganizing code.
  /// </summary>
  internal sealed class CodeReorganizationManager
  {
    #region Fields

    private readonly CodeMaidPackage _package;

    private readonly CodeModelManager _codeModelManager;

    private readonly CodeReorganizationAvailabilityLogic _codeReorganizationAvailabilityLogic;
    private readonly GenerateRegionLogic _generateRegionLogic;
    private readonly InsertBlankLinePaddingLogic _insertBlankLinePaddingLogic;
    private readonly RemoveRegionLogic _removeRegionLogic;

    #endregion Fields

    #region Constructors

    /// <summary>
    /// The singleton instance of the <see cref="CodeReorganizationManager" /> class.
    /// </summary>
    private static CodeReorganizationManager _instance;

    /// <summary>
    /// Gets an instance of the <see cref="CodeReorganizationManager" /> class.
    /// </summary>
    /// <param name="package">The hosting package.</param>
    /// <returns>An instance of the <see cref="CodeReorganizationManager" /> class.</returns>
    internal static CodeReorganizationManager GetInstance(CodeMaidPackage package)
    {
      return _instance ??= new CodeReorganizationManager(package);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CodeReorganizationManager" /> class.
    /// </summary>
    /// <param name="package">The hosting package.</param>
    private CodeReorganizationManager(CodeMaidPackage package)
    {
      _package = package;

      _codeModelManager = CodeModelManager.GetInstance(_package);

      _codeReorganizationAvailabilityLogic = CodeReorganizationAvailabilityLogic.GetInstance(_package);
      _generateRegionLogic = GenerateRegionLogic.GetInstance(_package);
      _insertBlankLinePaddingLogic = InsertBlankLinePaddingLogic.GetInstance(_package);
      _removeRegionLogic = RemoveRegionLogic.GetInstance(_package);
    }

    #endregion Constructors

    #region Internal Methods

    /// <summary>
    /// Moves the specified item above the specified base within an undo transaction.
    /// </summary>
    /// <param name="itemToMove">The item to move.</param>
    /// <param name="baseItem">The base item.</param>
    internal void MoveItemAboveBase(BaseCodeItem itemToMove, BaseCodeItem baseItem)
    {
      new UndoTransactionHelper(_package, Resources.CodeMaidMoveItemAbove).Run(
          () => RepositionItemAboveBase(itemToMove, baseItem));
    }

    /// <summary>
    /// Moves the specified item below the specified base.
    /// </summary>
    /// <param name="itemToMove">The item to move.</param>
    /// <param name="baseItem">The base item.</param>
    internal void MoveItemBelowBase(BaseCodeItem itemToMove, BaseCodeItem baseItem)
    {
      new UndoTransactionHelper(_package, Resources.CodeMaidMoveItemBelow).Run(
          () => RepositionItemBelowBase(itemToMove, baseItem));
    }

    /// <summary>
    /// Moves the specified item into the specified base.
    /// </summary>
    /// <param name="itemToMove">The item to move.</param>
    /// <param name="baseItem">The base item.</param>
    internal void MoveItemIntoBase(BaseCodeItem itemToMove, ICodeItemParent baseItem)
    {
      new UndoTransactionHelper(_package, Resources.CodeMaidMoveItemInto).Run(
          () => RepositionItemIntoBase(itemToMove, baseItem));
    }

    /// <summary>
    /// Reorganizes the specified document.
    /// </summary>
    /// <param name="document">The document for reorganizing.</param>
    internal void Reorganize(Document document)
    {
      if (!_codeReorganizationAvailabilityLogic.CanReorganize(document, true))
      {
        return;
      }
      ThreadHelper.ThrowIfNotOnUIThread();
      new UndoTransactionHelper(_package, string.Format(Resources.CodeMaidReorganizeFor0, document.Name)).Run(
                delegate
                {
                  ThreadHelper.ThrowIfNotOnUIThread();
                  OutputWindowHelper.DiagnosticWriteLine($"CodeReorganizationManager.Reorganize started for '{document.FullName}'");
                  _package.IDE.StatusBar.Text = string.Format(Resources.Reorganize_CodeMaidIsReorganizing0, document.Name);

                  // Retrieve all relevant code items (excluding using statements).
                  IEnumerable<BaseCodeItem> rawCodeItems = _codeModelManager.RetrieveAllCodeItems(document).Where(x => x is not CodeItemUsingStatement);

                  // Build the code tree based on the current file sort order.
                  SetCodeItems codeItems = new(rawCodeItems);
                  SetCodeItems codeTree = CodeTreeBuilder.RetrieveCodeTree(new CodeTreeRequest(document, codeItems, CodeSortOrder.File));

                  // Recursively reorganize the code tree.
                  RecursivelyReorganize(codeTree);

                  _package.IDE.StatusBar.Text = string.Format(Resources.CodeMaidReorganized0, document.Name);
                  OutputWindowHelper.DiagnosticWriteLine($"CodeReorganizationManager.Reorganize completed for '{document.FullName}'");
                });
    }

    #endregion Internal Methods

    #region Private Methods

    /// <summary>
    /// Gets the set of reorganizable code item elements from the specified set of code items.
    /// </summary>
    /// <param name="codeItems">The code items.</param>
    /// <returns>The set of reorganizable code item elements.</returns>
    private static IList<BaseCodeItemElement> GetReorganizableCodeItemElements(IEnumerable<BaseCodeItem> codeItems)
    {
      // Get all code item elements.
      List<BaseCodeItemElement> codeItemElements = [.. codeItems.OfType<BaseCodeItemElement>()];

      // Refresh them to make sure all positions are updated.
      codeItemElements.ForEach(x => x.RefreshCachedPositionAndName());

      // Sort the items, pulling out the first item in a set if there are items sharing a definition (ex: fields).
      return [.. codeItemElements.GroupBy(x => x.StartOffset).Select(y => y.First()).OrderBy(z => z.StartOffset)];
    }

    /// <summary>
    /// Gets the text and removes the specified item.
    /// </summary>
    /// <param name="itemToRemove">The item to remove.</param>
    /// <param name="cursorOffset">
    /// The cursor's offset within the item being removed, otherwise -1.
    /// </param>
    private static string GetTextAndRemoveItem(BaseCodeItem itemToRemove, out int cursorOffset)
    {
      // Refresh the code item and capture its end points.
      itemToRemove.RefreshCachedPositionAndName();
      EditPoint removeStartPoint = itemToRemove.StartPoint;
      EditPoint removeEndPoint = itemToRemove.EndPoint;
      ThreadHelper.ThrowIfNotOnUIThread();
      // Determine the cursor's offset if within the item being removed.
      int cursorAbsoluteOffset = removeStartPoint.Parent.Selection.ActivePoint.AbsoluteCharOffset;
      cursorOffset = cursorAbsoluteOffset >= removeStartPoint.AbsoluteCharOffset && cursorAbsoluteOffset <= removeEndPoint.AbsoluteCharOffset
        ? cursorAbsoluteOffset - removeStartPoint.AbsoluteCharOffset
        : -1;

      // Capture the text.
      string text = removeStartPoint.GetText(removeEndPoint);

      // Remove the text and cleanup whitespace.
      removeStartPoint.Delete(removeEndPoint);
      removeStartPoint.DeleteWhitespace(vsWhitespaceOptions.vsWhitespaceOptionsVertical);

      return text;
    }

    /// <summary>
    /// Determines if the two specified items should be separated by a newline.
    /// </summary>
    /// <param name="firstItem">The first item.</param>
    /// <param name="secondItem">The second item.</param>
    /// <returns>True if the items should be separated by a newline, otherwise false.</returns>
    private bool ShouldBeSeparatedByNewLine(BaseCodeItem firstItem, BaseCodeItem secondItem)
    {
      return _insertBlankLinePaddingLogic.ShouldBeFollowedByBlankLine(firstItem) ||
             _insertBlankLinePaddingLogic.ShouldBePrecededByBlankLine(secondItem);
    }

    /// <summary>
    /// Determines if the specified item's children should be reorganized.
    /// </summary>
    /// <param name="parent">The parent item.</param>
    /// <returns>True if the parent's children should be reorganized, otherwise false.</returns>
    private bool ShouldReorganizeChildren(BaseCodeItemElement parent)
    {
      // Enumeration values should never be reordered.
      if (parent is CodeItemEnum)
      {
        return false;
      }

      CodeElements parentAttributes = parent.Attributes;
      ThreadHelper.ThrowIfNotOnUIThread();
      if (parentAttributes != null)
      {
        // Some attributes indicate that order is critical and should not be reordered.
        string[] attributesToIgnore = new[]
        {
                    "System.Runtime.InteropServices.ComImportAttribute",
                    "System.Runtime.InteropServices.StructLayoutAttribute"
                };
        if (parentAttributes.OfType<CodeAttribute>().Any(x =>
        {
          ThreadHelper.ThrowIfNotOnUIThread();
          return attributesToIgnore.Contains(x.FullName);
        }))
        {
          return false;
        }
      }

      return true;
    }

    /// <summary>
    /// Repositions the specified item above the specified base.
    /// </summary>
    /// <param name="itemToMove">The item to move.</param>
    /// <param name="baseItem">The base item.</param>
    private void RepositionItemAboveBase(BaseCodeItem itemToMove, BaseCodeItem baseItem)
    {
      if (itemToMove == baseItem)
      {
        return;
      }
      ThreadHelper.ThrowIfNotOnUIThread();
      bool separateWithNewLine = ShouldBeSeparatedByNewLine(itemToMove, baseItem);
      string text = GetTextAndRemoveItem(itemToMove, out int cursorOffset);

      baseItem.RefreshCachedPositionAndName();
      EditPoint baseStartPoint = baseItem.StartPoint;
      EditPoint pastePoint = baseStartPoint.CreateEditPoint();

      pastePoint.Insert(text);
      pastePoint.Insert(Environment.NewLine);
      if (separateWithNewLine)
      {
        pastePoint.Insert(Environment.NewLine);
      }

      pastePoint.EndOfLine();
      baseStartPoint.SmartFormat(pastePoint);

      if (cursorOffset >= 0)
      {
        baseStartPoint.Parent.Selection.MoveToAbsoluteOffset(baseStartPoint.AbsoluteCharOffset + cursorOffset);
      }

      itemToMove.RefreshCachedPositionAndName();
      baseItem.RefreshCachedPositionAndName();
    }

    /// <summary>
    /// Repositions the specified item below the specified base.
    /// </summary>
    /// <param name="itemToMove">The item to move.</param>
    /// <param name="baseItem">The base item.</param>
    private void RepositionItemBelowBase(BaseCodeItem itemToMove, BaseCodeItem baseItem)
    {
      if (itemToMove == baseItem)
      {
        return;
      }

      bool separateWithNewLine = ShouldBeSeparatedByNewLine(baseItem, itemToMove);
      string text = GetTextAndRemoveItem(itemToMove, out int cursorOffset);
      ThreadHelper.ThrowIfNotOnUIThread();
      baseItem.RefreshCachedPositionAndName();
      EditPoint baseEndPoint = baseItem.EndPoint;
      EditPoint pastePoint = baseEndPoint.CreateEditPoint();

      pastePoint.Insert(Environment.NewLine);
      if (separateWithNewLine)
      {
        pastePoint.Insert(Environment.NewLine);
      }

      EditPoint formatPoint = pastePoint.CreateEditPoint();
      EditPoint insertPoint = pastePoint.CreateEditPoint();

      pastePoint.Insert(text);

      formatPoint.EndOfLine();
      baseEndPoint.SmartFormat(formatPoint);

      if (cursorOffset >= 0)
      {
        insertPoint.Parent.Selection.MoveToAbsoluteOffset(insertPoint.AbsoluteCharOffset + cursorOffset);
      }

      itemToMove.RefreshCachedPositionAndName();
      baseItem.RefreshCachedPositionAndName();
    }

    /// <summary>
    /// Repositions the specified item into the specified base.
    /// </summary>
    /// <param name="itemToMove">The item to move.</param>
    /// <param name="baseItem">The base item.</param>
    private void RepositionItemIntoBase(BaseCodeItem itemToMove, ICodeItemParent baseItem)
    {
      if (itemToMove == baseItem)
      {
        return;
      }

      bool padWithNewLine = _insertBlankLinePaddingLogic.ShouldBeFollowedByBlankLine(itemToMove);
      string text = GetTextAndRemoveItem(itemToMove, out int cursorOffset);
      ThreadHelper.ThrowIfNotOnUIThread();
      baseItem.RefreshCachedPositionAndName();
      EditPoint baseInsertPoint = baseItem.InsertPoint;
      EditPoint pastePoint = baseInsertPoint.CreateEditPoint();

      pastePoint.Insert(text);
      pastePoint.Insert(Environment.NewLine);
      if (padWithNewLine)
      {
        pastePoint.Insert(Environment.NewLine);
      }

      pastePoint.EndOfLine();
      baseInsertPoint.SmartFormat(pastePoint);

      if (cursorOffset >= 0)
      {
        baseInsertPoint.Parent.Selection.MoveToAbsoluteOffset(baseInsertPoint.AbsoluteCharOffset + cursorOffset);
      }

      itemToMove.RefreshCachedPositionAndName();
      baseItem.RefreshCachedPositionAndName();
    }

    /// <summary>
    /// Recursively reorganizes the specified code items.
    /// </summary>
    /// <param name="codeItems">The code items.</param>
    /// <param name="parent">The parent to the code items, otherwise null.</param>
    private void RecursivelyReorganize(IEnumerable<BaseCodeItem> codeItems, ICodeItemParent parent = null)
    {
      if (!codeItems.Any())
      {
        // If there are no code items, the only action we may want to take is conditionally insert regions.
        RegionsInsert(codeItems, parent);
        return;
      }

      // Conditionally remove existing regions.
      codeItems = RegionsRemoveExisting(codeItems);

      // Conditionally ignore regions.
      codeItems = RegionsFlatten(codeItems);

      // Get the items in their current order and their desired order.
      IList<BaseCodeItemElement> currentOrder = GetReorganizableCodeItemElements(codeItems);
      List<BaseCodeItemElement> desiredOrder = new(currentOrder);
      desiredOrder.Sort(new CodeItemTypeComparer(Settings.Default.Reorganizing_AlphabetizeMembersOfTheSameGroup));

      // Iterate across the items in the desired order, moving them when necessary.
      for (int desiredIndex = 0; desiredIndex < desiredOrder.Count; desiredIndex++)
      {
        BaseCodeItemElement item = desiredOrder[desiredIndex];

        if (item is ICodeItemParent itemAsParent && ShouldReorganizeChildren(item))
        {
          RecursivelyReorganize(itemAsParent.Children, itemAsParent);
        }

        int currentIndex = currentOrder.IndexOf(item);
        if (desiredIndex != currentIndex)
        {
          // Move the item above what is in its desired position.
          RepositionItemAboveBase(item, currentOrder[desiredIndex]);

          // Update the current order to match the move.
          currentOrder.RemoveAt(currentIndex);
          currentOrder.Insert(desiredIndex > currentIndex ? desiredIndex - 1 : desiredIndex, item);
        }
      }

      // Conditionally insert regions.
      RegionsInsert(codeItems, parent);

      // Recursively reorganize the contents of any regions as well.
      IEnumerable<CodeItemRegion> codeItemRegions = codeItems.OfType<CodeItemRegion>();
      foreach (CodeItemRegion codeItemRegion in codeItemRegions)
      {
        RecursivelyReorganize(codeItemRegion.Children, codeItemRegion);
      }
    }

    /// <summary>
    /// Conditionally removes existing regions that should not remain and returns an updated
    /// collection including the members of removed regions.
    /// </summary>
    /// <param name="codeItems">The code items.</param>
    /// <returns>An updated code items collection.</returns>
    private IEnumerable<BaseCodeItem> RegionsRemoveExisting(IEnumerable<BaseCodeItem> codeItems)
    {
      if (!Settings.Default.Reorganizing_RegionsRemoveExistingRegions)
      {
        return codeItems;
      }
      ThreadHelper.ThrowIfNotOnUIThread();
      while (true)
      {
        List<CodeItemRegion> regionsToRemove = [.. _generateRegionLogic.GetRegionsToRemove(codeItems)];
        List<BaseCodeItem> regionsToRemoveChildren = [.. regionsToRemove.SelectMany(x => x.Children)];
        List<CodeItemRegion> regionsToRemoveChildrenRegions = [.. regionsToRemoveChildren.OfType<CodeItemRegion>()];
        // Nested regions do not track points well, so offset cursor position by 1 as a workaround.
        foreach (CodeItemRegion nestedRegion in regionsToRemoveChildrenRegions)
        {
          nestedRegion.StartPoint.CharRight();
          nestedRegion.EndPoint.CharRight();
        }

        _removeRegionLogic.RemoveRegions(regionsToRemove);

        // Reverse offset of cursor position for nested regions.
        foreach (CodeItemRegion nestedRegion in regionsToRemoveChildrenRegions)
        {
          nestedRegion.StartPoint.CharLeft();
          nestedRegion.EndPoint.CharLeft();
        }

        // Update the code items collection by excluding the removed regions and including those region's direct children.
        codeItems = codeItems.Except(regionsToRemove).Union(regionsToRemoveChildren);

        // If there were any nested regions in those regions that were removed, loop back over again.
        if (regionsToRemoveChildrenRegions.Any())
        {
          continue;
        }

        return codeItems;
      }
    }

    /// <summary>
    /// Conditionally flattens the contents of regions into the specified collection.
    /// </summary>
    /// <param name="codeItems">The code items.</param>
    /// <returns>An updated code items collection.</returns>
    private IEnumerable<BaseCodeItem> RegionsFlatten(IEnumerable<BaseCodeItem> codeItems)
    {
      if (Settings.Default.Reorganizing_KeepMembersWithinRegions)
      {
        return codeItems;
      }

      while (true)
      {
        IEnumerable<CodeItemRegion> regions = codeItems.OfType<CodeItemRegion>();
        if (!regions.Any())
        {
          break;
        }

        // Update the code items collection by excluding the regions and including those region's direct children.
        codeItems = codeItems.Except(regions).Union(regions.SelectMany(x => x.Children));
      }

      return codeItems;
    }

    /// <summary>
    /// Conditionally inserts regions for the specified code items.
    /// </summary>
    /// <param name="codeItems">The code items.</param>
    /// <param name="parent">The parent to the code items, otherwise null.</param>
    private void RegionsInsert(IEnumerable<BaseCodeItem> codeItems, ICodeItemParent parent)
    {
      if (Settings.Default.Reorganizing_RegionsInsertNewRegions)
      {
        // Only insert regions when directly inside the scope of a class, interface or struct.
        if (parent is CodeItemClass || parent is CodeItemInterface || parent is CodeItemStruct)
        {
          _generateRegionLogic.InsertRegions(codeItems, parent.InsertPoint);
        }
      }
    }

    #endregion Private Methods
  }
}
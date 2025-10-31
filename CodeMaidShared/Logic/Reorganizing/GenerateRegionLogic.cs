using System;
using System.Collections.Generic;
using System.Linq;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using ASGV.CodeMaid.Helpers;
using ASGV.CodeMaid.Logic.Cleaning;
using ASGV.CodeMaid.Model.CodeItems;
using ASGV.CodeMaid.Properties;
using Thread = System.Threading.Thread;

namespace ASGV.CodeMaid.Logic.Reorganizing
{
  /// <summary>
  /// A class for encapsulating the logic of generating regions.
  /// </summary>
  internal sealed class GenerateRegionLogic
  {
    #region Fields

    private readonly CodeMaidPackage _package;
    private readonly InsertBlankLinePaddingLogic _insertBlankLinePaddingLogic;
    private readonly RegionComparerByName _regionComparerByName;

    #endregion Fields

    #region Constructors

    /// <summary>
    /// The singleton instance of the <see cref="GenerateRegionLogic" /> class.
    /// </summary>
    private static GenerateRegionLogic _instance;

    /// <summary>
    /// Gets an instance of the <see cref="GenerateRegionLogic" /> class.
    /// </summary>
    /// <param name="package">The hosting package.</param>
    /// <returns>An instance of the <see cref="GenerateRegionLogic" /> class.</returns>
    internal static GenerateRegionLogic GetInstance(CodeMaidPackage package)
    {
      return _instance ??= new GenerateRegionLogic(package);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GenerateRegionLogic" /> class.
    /// </summary>
    /// <param name="package">The hosting package.</param>
    private GenerateRegionLogic(CodeMaidPackage package)
    {
      _package = package;
      _insertBlankLinePaddingLogic = InsertBlankLinePaddingLogic.GetInstance(_package);
      _regionComparerByName = new RegionComparerByName();
    }

    #endregion Constructors

    #region Properties

    /// <summary>
    /// A list of possible access modifiers.
    /// </summary>
    private static IEnumerable<string> AccessModifiers => ["Public", "Internal", "Protected Internal", "Protected", "Private"];

    #endregion Properties

    #region Methods

    /// <summary>
    /// Gets the enumerable set of regions to be removed based on the specified code items.
    /// </summary>
    /// <param name="codeItems">The code items.</param>
    /// <returns>An enumerable set of regions to be removed.</returns>
    public IEnumerable<CodeItemRegion> GetRegionsToRemove(IEnumerable<BaseCodeItem> codeItems)
    {
      IEnumerable<CodeItemRegion> existingRegions = codeItems.OfType<CodeItemRegion>();

      // If also inserting regions, remove all existing for more comprehensive reorganization.
      if (Settings.Default.Reorganizing_RegionsInsertNewRegions)
      {
        return existingRegions;
      }

      IEnumerable<CodeItemRegion> composedRegions = ComposeRegionsList(codeItems);
      IEnumerable<CodeItemRegion> regionsToRemove = existingRegions.Except(composedRegions, _regionComparerByName);

      return regionsToRemove;
    }

    /// <summary>
    /// Inserts regions per user settings.
    /// </summary>
    /// <param name="codeItems">The code items.</param>
    /// <param name="insertPoint">The default insertion point.</param>
    public void InsertRegions(IEnumerable<BaseCodeItem> codeItems, EditPoint insertPoint)
    {
      // Refresh and sort the code items.
      foreach (BaseCodeItem codeItem in codeItems)
      {
        codeItem.RefreshCachedPositionAndName();
      }

      codeItems = [.. codeItems.OrderBy(x => x.StartOffset)];
      ThreadHelper.ThrowIfNotOnUIThread();
      IEnumerable<CodeItemRegion> regions = ComposeRegionsList(codeItems);
      IEnumerator<BaseCodeItem> codeItemEnumerator = codeItems.GetEnumerator();
      codeItemEnumerator.MoveNext();
      EditPoint cursor = insertPoint.CreateEditPoint();

      foreach (CodeItemRegion region in regions)
      {
        // While the current code item is a region not in the list, advance to the next code item.
        CodeItemRegion currentCodeItemAsRegion = codeItemEnumerator.Current as CodeItemRegion;
        while (currentCodeItemAsRegion != null && !regions.Contains(currentCodeItemAsRegion, _regionComparerByName))
        {
          cursor = codeItemEnumerator.Current.EndPoint;
          codeItemEnumerator.MoveNext();
          currentCodeItemAsRegion = codeItemEnumerator.Current as CodeItemRegion;
        }

        // If the current code item is this region, advance to the next code item and end this iteration.
        if (_regionComparerByName.Equals(currentCodeItemAsRegion, region))
        {
          cursor = codeItemEnumerator.Current.EndPoint;
          codeItemEnumerator.MoveNext();
          continue;
        }

        // Update the cursor position to the current code item.
        if (codeItemEnumerator.Current != null)
        {
          cursor = codeItemEnumerator.Current.StartPoint;
        }

        // If the current code item is a region, offset the position by 1 to workaround points not tracking for region types.
        if (currentCodeItemAsRegion != null)
        {
          cursor = cursor.CreateEditPoint();
          currentCodeItemAsRegion.StartPoint.CharRight();
          currentCodeItemAsRegion.EndPoint.CharRight();
        }

        // Insert the #region tag
        cursor = InsertRegionTag(region, cursor);

        // Keep jumping forwards in code items as long as there's matches.
        while (CodeItemBelongsInRegion(codeItemEnumerator.Current, region))
        {
          cursor = codeItemEnumerator.Current.EndPoint;
          codeItemEnumerator.MoveNext();
        }

        // Insert the #endregion tag
        cursor = InsertEndRegionTag(region, cursor);

        // If the current code item is a region, reverse offset of the position.
        if (currentCodeItemAsRegion != null)
        {
          currentCodeItemAsRegion.StartPoint.CharLeft();
          currentCodeItemAsRegion.EndPoint.CharLeft();
        }
      }
    }

    /// <summary>
    /// Inserts a #region tag for the specified region preceding the specified start point.
    /// </summary>
    /// <param name="region">The region to start.</param>
    /// <param name="startPoint">The starting point.</param>
    /// <returns>The updated cursor.</returns>
    public EditPoint InsertRegionTag(CodeItemRegion region, EditPoint startPoint)
    {
      ThreadHelper.ThrowIfNotOnUIThread();
      EditPoint cursor = startPoint.CreateEditPoint();

      // If the cursor is not preceeded only by whitespace, insert a new line.
      int firstNonWhitespaceIndex = cursor.GetLine().TakeWhile(char.IsWhiteSpace).Count();
      if (cursor.DisplayColumn > firstNonWhitespaceIndex + 1)
      {
        cursor.Insert(Environment.NewLine);
      }

      cursor.Insert($"{RegionHelper.GetRegionTagText(cursor, region.Name)}{Environment.NewLine}");

      startPoint.SmartFormat(cursor);

      region.StartPoint = cursor.CreateEditPoint();
      region.StartPoint.LineUp();
      region.StartPoint.StartOfLine();

      CodeItemRegion[] regionWrapper = new[] { region };
      _insertBlankLinePaddingLogic.InsertPaddingBeforeRegionTags(regionWrapper);
      _insertBlankLinePaddingLogic.InsertPaddingAfterRegionTags(regionWrapper);

      return cursor;
    }

    /// <summary>
    /// Inserts an #endregion tag for the specified region following the specified end point.
    /// </summary>
    /// <param name="region">The region to end.</param>
    /// <param name="endPoint">The end point.</param>
    /// <returns>The updated cursor.</returns>
    public EditPoint InsertEndRegionTag(CodeItemRegion region, EditPoint endPoint)
    {
      ThreadHelper.ThrowIfNotOnUIThread();
      EditPoint cursor = endPoint.CreateEditPoint();

      // If the cursor is not preceeded only by whitespace, insert a new line.
      int firstNonWhitespaceIndex = cursor.GetLine().TakeWhile(char.IsWhiteSpace).Count();
      if (cursor.DisplayColumn > firstNonWhitespaceIndex + 1)
      {
        cursor.Insert(Environment.NewLine);
      }

      cursor.Insert(RegionHelper.GetEndRegionTagText(cursor));

      if (Settings.Default.Cleaning_UpdateEndRegionDirectives &&
          RegionHelper.LanguageSupportsUpdatingEndRegionDirectives(cursor))
      {
        cursor.Insert(" " + region.Name);
      }

      // If the cursor is not followed only by whitespace, insert a new line.
      int lastNonWhitespaceIndex = cursor.GetLine().TrimEnd().Length;
      if (cursor.DisplayColumn < lastNonWhitespaceIndex + 1)
      {
        cursor.Insert(Environment.NewLine);
        cursor.LineUp();
        cursor.EndOfLine();
      }

      endPoint.SmartFormat(cursor);

      region.EndPoint = cursor.CreateEditPoint();

      CodeItemRegion[] regionWrapper = new[] { region };
      _insertBlankLinePaddingLogic.InsertPaddingBeforeEndRegionTags(regionWrapper);
      _insertBlankLinePaddingLogic.InsertPaddingAfterEndRegionTags(regionWrapper);

      return cursor;
    }

    /// <summary>
    /// Determines if the specified code item belongs in the specified region.
    /// </summary>
    /// <param name="codeItem">The code item.</param>
    /// <param name="region">The region.</param>
    /// <returns>True if the specified code item belongs in the specified region, otherwise false.</returns>
    private bool CodeItemBelongsInRegion(BaseCodeItem codeItem, CodeItemRegion region)
    {
      return codeItem != null && _regionComparerByName.Equals(region, ComposeRegionForCodeItem(codeItem));
    }

    /// <summary>
    /// Composes a list of regions that should be present for the specified set of code items based on user settings.
    /// </summary>
    /// <param name="codeItems">The code items.</param>
    /// <returns>An enumerable set of regions that should be present.</returns>
    private IEnumerable<CodeItemRegion> ComposeRegionsList(IEnumerable<BaseCodeItem> codeItems)
    {
      return Settings.Default.Reorganizing_RegionsInsertKeepEvenIfEmpty
          ? ComposeAllPossibleRegionsList()
          : ComposePresentTypesRegionsList(codeItems);
    }

    /// <summary>
    /// Composes a list of all possible regions.
    /// </summary>
    /// <returns>An enumerable set of regions.</returns>
    private IEnumerable<CodeItemRegion> ComposeAllPossibleRegionsList()
    {
      List<CodeItemRegion> regions = new();

      IOrderedEnumerable<List<MemberTypeSetting>> types = MemberTypeSettingHelper.AllSettings.GroupBy(x => x.Order).Select(y => new List<MemberTypeSetting>(y)).OrderBy(z => z[0].Order);
      foreach (List<MemberTypeSetting> type in types)
      {
        if (Settings.Default.Reorganizing_RegionsIncludeAccessLevel)
        {
          regions.AddRange(AccessModifiers.Select(x => new CodeItemRegion { Name = x + " " + type[0].EffectiveName }));
        }
        else
        {
          regions.Add(new CodeItemRegion { Name = type[0].EffectiveName });
        }
      }

      return regions;
    }

    /// <summary>
    /// Composes a list of regions based on the specified code items.
    /// </summary>
    /// <param name="codeItems">The code items.</param>
    /// <returns>An enumerable set of regions.</returns>
    private IEnumerable<CodeItemRegion> ComposePresentTypesRegionsList(IEnumerable<BaseCodeItem> codeItems)
    {
      HashSet<CodeItemRegion> regions = new(_regionComparerByName);

      foreach (BaseCodeItem codeItem in codeItems)
      {
        CodeItemRegion region = ComposeRegionForCodeItem(codeItem);
        if (region != null)
        {
          regions.Add(region);
        }
        else
        {
          region = codeItem as CodeItemRegion;
          if (region != null)
          {
            // Add an existing region to the list iff it has a child whose composed region name would match it.
            IEnumerable<CodeItemRegion> childrenRegions = ComposePresentTypesRegionsList(region.Children);
            if (childrenRegions.Contains(region, _regionComparerByName))
            {
              regions.Add(region);
            }
          }
        }
      }

      return regions;
    }

    /// <summary>
    /// Composes a region based on the specified code item.
    /// </summary>
    /// <param name="codeItem">The code item.</param>
    /// <returns>A region.</returns>
    private CodeItemRegion ComposeRegionForCodeItem(BaseCodeItem codeItem)
    {
      if (codeItem == null)
      {
        return null;
      }

      MemberTypeSetting setting = MemberTypeSettingHelper.LookupByKind(codeItem.Kind);
      if (setting == null)
      {
        return null;
      }

      string regionName = string.Empty;

      if (Settings.Default.Reorganizing_RegionsIncludeAccessLevel)
      {
        BaseCodeItemElement element = codeItem as BaseCodeItemElement;
        if (element != null && (!Settings.Default.Reorganizing_RegionsIncludeAccessLevelForMethodsOnly || element is CodeItemMethod))
        {
          string accessModifier = CodeElementHelper.GetAccessModifierKeyword(element.Access);
          if (accessModifier != null)
          {
            regionName = Thread.CurrentThread.CurrentCulture.TextInfo.ToTitleCase(accessModifier) + " ";
          }
        }
      }

      regionName += setting.EffectiveName;

      return new CodeItemRegion { Name = regionName };
    }

    #endregion Methods
  }
}
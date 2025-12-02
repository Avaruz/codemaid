using EnvDTE;
using Microsoft.VisualStudio.Shell;
using ASGV.CodeMaid.Helpers;
using ASGV.CodeMaid.Logic.Cleaning;
using ASGV.CodeMaid.Model;
using ASGV.CodeMaid.Properties;
using System.Threading.Tasks;

namespace ASGV.CodeMaid.Integration.Commands
{
    /// <summary>
    /// A command that provides for removing region(s).
    /// </summary>
    internal sealed class RemoveRegionCommand : BaseCommand
    {
        private readonly CodeModelHelper _codeModelHelper;
        private readonly RemoveRegionLogic _removeRegionLogic;

        /// <summary>
        /// Initializes a new instance of the <see cref="RemoveRegionCommand" /> class.
        /// </summary>
        /// <param name="package">The hosting package.</param>
        internal RemoveRegionCommand(CodeMaidPackage package)
            : base(package, PackageGuids.GuidCodeMaidMenuSet, PackageIds.CmdIDCodeMaidRemoveRegion)
        {
            _codeModelHelper = CodeModelHelper.GetInstance(package);
            _removeRegionLogic = RemoveRegionLogic.GetInstance(package);
        }

        /// <summary>
        /// An enumeration of region command scopes.
        /// </summary>
        private enum RegionCommandScope
        {
            None,
            Document,
            CurrentLine,
            Selection
        }

        /// <summary>
        /// A singleton instance of this command.
        /// </summary>
        public static RemoveRegionCommand Instance { get; private set; }

        /// <summary>
        /// Gets the active text document, otherwise null.
        /// </summary>
        private TextDocument ActiveTextDocument
        {
            get
            {
                ThreadHelper.ThrowIfNotOnUIThread(); return Package.ActiveDocument?.GetTextDocument();
            }
        }

        /// <summary>
        /// Initializes a singleton instance of this command.
        /// </summary>
        /// <param name="package">The hosting package.</param>
        /// <returns>A task.</returns>
        public static async Task InitializeAsync(CodeMaidPackage package)
        {
            Instance = new RemoveRegionCommand(package);
            await package.SettingsMonitor.WatchAsync(s => s.Feature_RemoveRegion, Instance.SwitchAsync);
        }

        /// <summary>
        /// Called to update the current status of the command.
        /// </summary>
        protected override void OnBeforeQueryStatus()
        {
            RegionCommandScope regionCommandScope = GetRegionCommandScope();

            Enabled = regionCommandScope != RegionCommandScope.None;

            Text = regionCommandScope switch
            {
                RegionCommandScope.CurrentLine => Resources.RemoveCurrentRegion,
                RegionCommandScope.Selection => Resources.RemoveSelectedRegions,
                _ => Resources.RemoveAllRegions,
            };
        }

        /// <summary>
        /// Called to execute the command.
        /// </summary>
        protected override void OnExecute()
        {
            base.OnExecute();
            ThreadHelper.ThrowIfNotOnUIThread();
            RegionCommandScope regionCommandScope = GetRegionCommandScope();
            switch (regionCommandScope)
            {
                case RegionCommandScope.CurrentLine:
                    _removeRegionLogic.RemoveRegion(_codeModelHelper.RetrieveCodeRegionUnderCursor(ActiveTextDocument));
                    break;

                case RegionCommandScope.Selection:
                    _removeRegionLogic.RemoveRegions(ActiveTextDocument.Selection);
                    break;

                case RegionCommandScope.Document:
                    _removeRegionLogic.RemoveRegions(ActiveTextDocument);
                    break;
            }
        }

        /// <summary>
        /// Gets the region command scope based on the current document and selection conditions.
        /// </summary>
        /// <returns>The scope that should be used for the region command.</returns>
        private RegionCommandScope GetRegionCommandScope()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (_removeRegionLogic.CanRemoveRegions(Package.ActiveDocument))
            {
                TextDocument activeTextDocument = ActiveTextDocument;
                if (activeTextDocument != null)
                {
                    TextSelection textSelection = activeTextDocument.Selection;
                    if (textSelection != null)
                    {
                        if (!textSelection.IsEmpty)
                        {
                            return RegionCommandScope.Selection;
                        }

                        if (_codeModelHelper.IsCodeRegionUnderCursor(ActiveTextDocument))
                        {
                            return RegionCommandScope.CurrentLine;
                        }
                    }

                    return RegionCommandScope.Document;
                }
            }

            return RegionCommandScope.None;
        }
    }
}
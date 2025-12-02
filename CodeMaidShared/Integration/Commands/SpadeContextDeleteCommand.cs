using System;
using System.Linq;
using System.Threading.Tasks;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using ASGV.CodeMaid.Helpers;
using ASGV.CodeMaid.Model.CodeItems;
using ASGV.CodeMaid.Properties;

namespace ASGV.CodeMaid.Integration.Commands
{
    /// <summary>
    /// A command that provides for deleting a member within Spade.
    /// </summary>
    internal sealed class SpadeContextDeleteCommand : BaseCommand
    {
        private readonly UndoTransactionHelper _undoTransactionHelper;

        /// <summary>
        /// Initializes a new instance of the <see cref="SpadeContextDeleteCommand" /> class.
        /// </summary>
        /// <param name="package">The hosting package.</param>
        internal SpadeContextDeleteCommand(CodeMaidPackage package)
            : base(package, PackageGuids.GuidCodeMaidMenuSet, PackageIds.CmdIDCodeMaidSpadeContextDelete)
        {
            _undoTransactionHelper = new UndoTransactionHelper(package, Resources.CodeMaidDeleteItems);
        }

        /// <summary>
        /// A singleton instance of this command.
        /// </summary>
        public static SpadeContextDeleteCommand Instance { get; private set; }

        /// <summary>
        /// Initializes a singleton instance of this command.
        /// </summary>
        /// <param name="package">The hosting package.</param>
        /// <returns>A task.</returns>
        public static async Task InitializeAsync(CodeMaidPackage package)
        {
            Instance = new SpadeContextDeleteCommand(package);
            await Instance.SwitchAsync(on: true);
        }

        /// <summary>
        /// Called to update the current status of the command.
        /// </summary>
        protected override void OnBeforeQueryStatus()
        {
            bool visible = false;

            UI.ToolWindows.Spade.SpadeToolWindow spade = Package.Spade;
            if (spade != null)
            {
                visible = spade.SelectedItems.Any(IsDeletable);
            }

            Visible = visible;
        }

        /// <summary>
        /// Called to execute the command.
        /// </summary>
        protected override void OnExecute()
        {
            base.OnExecute();

            UI.ToolWindows.Spade.SpadeToolWindow spade = Package.Spade;
            if (spade != null)
            {
                // Delay the check of start/end points until execution time, to avoid an intermediate state issue.
                System.Collections.Generic.IEnumerable<BaseCodeItem> items = spade.SelectedItems.Where(IsDeletable).Where(x => x.StartPoint != null && x.EndPoint != null);

                _undoTransactionHelper.Run(() =>
                {
                    ThreadHelper.ThrowIfNotOnUIThread();
                    // Iterate through items in reverse order (reduces line number updates during removal).
                    foreach (BaseCodeItem item in items.OrderByDescending(x => x.StartLine))
                    {
                        EditPoint start = item.StartPoint.CreateEditPoint();

                        start.Delete(item.EndPoint);
                        start.DeleteWhitespace(vsWhitespaceOptions.vsWhitespaceOptionsVertical);
                        start.Insert(Environment.NewLine);
                    }
                });

                spade.Refresh();
            }
        }

        /// <summary>
        /// Determines if the specified item is a candidate for deletion.
        /// </summary>
        /// <param name="codeItem">The code item.</param>
        /// <returns>True if the code item can be deleted, otherwise false.</returns>
        private static bool IsDeletable(BaseCodeItem codeItem)
        {
            return codeItem is not CodeItemRegion || !((CodeItemRegion)codeItem).IsPseudoGroup;
        }
    }
}
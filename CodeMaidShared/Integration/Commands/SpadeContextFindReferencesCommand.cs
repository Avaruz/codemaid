using System.Linq;
using System.Threading.Tasks;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using ASGV.CodeMaid.Model.CodeItems;

namespace ASGV.CodeMaid.Integration.Commands
{
    /// <summary>
    /// A command that provides for finding references of a member within Spade.
    /// </summary>
    internal sealed class SpadeContextFindReferencesCommand : BaseCommand
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SpadeContextFindReferencesCommand" /> class.
        /// </summary>
        /// <param name="package">The hosting package.</param>
        internal SpadeContextFindReferencesCommand(CodeMaidPackage package)
            : base(package, PackageGuids.GuidCodeMaidMenuSet, PackageIds.CmdIDCodeMaidSpadeContextFindReferences)
        {
        }

        /// <summary>
        /// A singleton instance of this command.
        /// </summary>
        public static SpadeContextFindReferencesCommand Instance { get; private set; }

        /// <summary>
        /// Initializes a singleton instance of this command.
        /// </summary>
        /// <param name="package">The hosting package.</param>
        /// <returns>A task.</returns>
        public static async Task InitializeAsync(CodeMaidPackage package)
        {
            Instance = new SpadeContextFindReferencesCommand(package);
            await Instance.SwitchAsync(on: true);
        }

        /// <summary>
        /// Called to update the current status of the command.
        /// </summary>
        protected override void OnBeforeQueryStatus()
        {
            UI.ToolWindows.Spade.SpadeToolWindow spade = Package.Spade;

            Visible = spade?.SelectedItems.OfType<BaseCodeItemElement>().Count() == 1;
        }

        /// <summary>
        /// Called to execute the command.
        /// </summary>
        protected override void OnExecute()
        {
            base.OnExecute();
            ThreadHelper.ThrowIfNotOnUIThread();
            UI.ToolWindows.Spade.SpadeToolWindow spade = Package.Spade;

            BaseCodeItemElement item = spade?.SelectedItems.OfType<BaseCodeItemElement>().FirstOrDefault();
            if (item == null)
            {
                return;
            }

            Document document = spade.Document;
            if (document == null)
            {
                return;
            }

            TextSelection selection = ((TextSelection)document.Selection);

            // Activate the document and set the cursor position to set the command context.
            document.Activate();
            selection.MoveToPoint(item.CodeElement.StartPoint);
            selection.FindText(item.Name, (int)vsFindOptions.vsFindOptionsMatchInHiddenText);
            selection.MoveToPoint(selection.AnchorPoint);

            // Invoke the command.
            Package.IDE.ExecuteCommand("Edit.FindAllReferences");
        }
    }
}
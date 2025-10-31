using EnvDTE;
using Microsoft.VisualStudio.Shell;
using ASGV.CodeMaid.Helpers;
using ASGV.CodeMaid.Logic.Cleaning;
using ASGV.CodeMaid.UI.Dialogs.CleanupProgress;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ASGV.CodeMaid.Integration.Commands
{
  /// <summary>
  /// A command that provides for cleaning up code in the open documents.
  /// </summary>
  internal sealed class CleanupOpenCodeCommand : BaseCommand
  {
    /// <summary>
    /// Initializes a new instance of the <see cref="CleanupOpenCodeCommand" /> class.
    /// </summary>
    /// <param name="package">The hosting package.</param>
    internal CleanupOpenCodeCommand(CodeMaidPackage package)
        : base(package, PackageGuids.GuidCodeMaidMenuSet, PackageIds.CmdIDCodeMaidCleanupOpenCode)
    {
      CodeCleanupAvailabilityLogic = CodeCleanupAvailabilityLogic.GetInstance(Package);
    }

    /// <summary>
    /// A singleton instance of this command.
    /// </summary>
    public static CleanupOpenCodeCommand Instance { get; private set; }

    /// <summary>
    /// Gets or sets the code cleanup availability logic.
    /// </summary>
    private CodeCleanupAvailabilityLogic CodeCleanupAvailabilityLogic { get; }

    /// <summary>
    /// Gets the list of open documents that are cleanup candidates.
    /// </summary>
    private IEnumerable<Document> OpenCleanableDocuments
        => OpenDocuments.Where(x => CodeCleanupAvailabilityLogic.CanCleanupDocument(x));

    /// <summary>
    /// Gets the list of open documents.
    /// </summary>
    private IEnumerable<Document> OpenDocuments
    {
      get
      {
        ThreadHelper.ThrowIfNotOnUIThread();
        return Package.IDE.Documents.OfType<Document>().Where(x =>
        {
          ThreadHelper.ThrowIfNotOnUIThread();
          return x.ActiveWindow != null;
        });
      }
    }

    /// <summary>
    /// Initializes a singleton instance of this command.
    /// </summary>
    /// <param name="package">The hosting package.</param>
    /// <returns>A task.</returns>
    public static async Task InitializeAsync(CodeMaidPackage package)
    {
      Instance = new CleanupOpenCodeCommand(package);
      await package.SettingsMonitor.WatchAsync(s => s.Feature_CleanupOpenCode, Instance.SwitchAsync);
    }

    /// <summary>
    /// Called to update the current status of the command.
    /// </summary>
    protected override void OnBeforeQueryStatus()
    {
      Enabled = OpenDocuments.Any();
    }

    /// <summary>
    /// Called to execute the command.
    /// </summary>
    protected override void OnExecute()
    {
      base.OnExecute();

      using (new ActiveDocumentRestorer(Package))
      {
        CleanupProgressViewModel viewModel = new(Package, OpenCleanableDocuments);
        CleanupProgressWindow window = new() { DataContext = viewModel };

        window.ShowModal();
      }
    }
  }
}
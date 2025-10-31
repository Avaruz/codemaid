using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using ASGV.CodeMaid.Properties;
using System.Threading.Tasks;

namespace ASGV.CodeMaid.Integration.Commands
{
  /// <summary>
  /// A command that provides for launching the build progress tool window.
  /// </summary>
  internal sealed class BuildProgressToolWindowCommand : BaseCommand
  {
    /// <summary>
    /// Initializes a new instance of the <see cref="BuildProgressToolWindowCommand" /> class.
    /// </summary>
    /// <param name="package">The hosting package.</param>
    internal BuildProgressToolWindowCommand(CodeMaidPackage package)
        : base(package, PackageGuids.GuidCodeMaidMenuSet, PackageIds.CmdIDCodeMaidBuildProgressToolWindow)
    {
    }

    /// <summary>
    /// A singleton instance of this command.
    /// </summary>
    public static BuildProgressToolWindowCommand Instance { get; private set; }

    /// <summary>
    /// Gets the build progress window frame.
    /// </summary>
    private IVsWindowFrame BuildProgressWindowFrame
    {
      get
      {
        ThreadHelper.ThrowIfNotOnUIThread();
        UI.ToolWindows.BuildProgress.BuildProgressToolWindow buildProgress = Package.BuildProgressForceLoad;
        return buildProgress != null ? buildProgress.Frame as IVsWindowFrame : null;
      }
    }

    /// <summary>
    /// Initializes a singleton instance of this command.
    /// </summary>
    /// <param name="package">The hosting package.</param>
    /// <returns>A task.</returns>
    public static async Task InitializeAsync(CodeMaidPackage package)
    {
      Instance = new BuildProgressToolWindowCommand(package);
      await package.SettingsMonitor.WatchAsync(s => s.Feature_BuildProgressToolWindow, Instance.SwitchAsync);
    }

    public override async Task SwitchAsync(bool on)
    {
      await base.SwitchAsync(on);
      await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
      if (!on)
      {
        Package.BuildProgress?.Close();
      }
    }

    /// <summary>
    /// Called when a build has begun.
    /// </summary>
    /// <param name="scope">The scope.</param>
    /// <param name="action">The action.</param>
    internal void OnBuildBegin(vsBuildScope scope, vsBuildAction action)
    {
      UI.ToolWindows.BuildProgress.BuildProgressToolWindow buildProgress = Package.BuildProgressForceLoad;
      if (buildProgress != null)
      {
        buildProgress.NotifyBuildBegin(scope, action);

        if (Settings.Default.Progressing_ShowBuildProgressOnBuildStart)
        {
          ShowBuildProgressToolWindowWithoutActivation();
        }
      }
    }

    /// <summary>
    /// Called when a build is done.
    /// </summary>
    /// <param name="scope">The scope.</param>
    /// <param name="action">The action.</param>
    internal void OnBuildDone(vsBuildScope scope, vsBuildAction action)
    {
      UI.ToolWindows.BuildProgress.BuildProgressToolWindow buildProgress = Package.BuildProgressForceLoad;
      if (buildProgress != null)
      {
        buildProgress.NotifyBuildDone(scope, action);

        if (Settings.Default.Progressing_HideBuildProgressOnBuildStop)
        {
          HideBuildProgressToolWindow();
        }
      }
    }

    /// <summary>
    /// Called when an individual project build has begun.
    /// </summary>
    /// <param name="project">The project.</param>
    /// <param name="projectConfig">The project config.</param>
    /// <param name="platform">The platform.</param>
    /// <param name="solutionConfig">The solution config.</param>
    internal void OnBuildProjConfigBegin(string project, string projectConfig, string platform, string solutionConfig)
    {
      UI.ToolWindows.BuildProgress.BuildProgressToolWindow buildProgress = Package.BuildProgressForceLoad;
      buildProgress?.NotifyBuildProjConfigBegin(project, projectConfig, platform, solutionConfig);
    }

    /// <summary>
    /// Called when an individual project build is done.
    /// </summary>
    /// <param name="project">The project.</param>
    /// <param name="projectConfig">The project config.</param>
    /// <param name="platform">The platform.</param>
    /// <param name="solutionConfig">The solution config.</param>
    /// <param name="success">True if project build was successful, otherwise false.</param>
    internal void OnBuildProjConfigDone(string project, string projectConfig, string platform, string solutionConfig, bool success)
    {
      UI.ToolWindows.BuildProgress.BuildProgressToolWindow buildProgress = Package.BuildProgressForceLoad;
      buildProgress?.NotifyBuildProjConfigDone(project, projectConfig, platform, solutionConfig, success);
    }

    /// <summary>
    /// Called to execute the command.
    /// </summary>
    protected override void OnExecute()
    {
      base.OnExecute();

      ShowBuildProgressToolWindow();
    }

    /// <summary>
    /// Docks the specified frame window if it is currently floating.
    /// </summary>
    /// <remarks>Works in VS2010, does not appear to work in VS2008.</remarks>
    /// <param name="frame">The frame.</param>
    private static void DockWindowIfFloating(IVsWindowFrame frame)
    {
      ThreadHelper.ThrowIfNotOnUIThread();
      // Get the current tool window frame mode.
      frame.GetProperty((int)__VSFPROPID.VSFPROPID_FrameMode, out object currentFrameMode);

      // If currently floating, switch to dock mode.
      if ((VSFRAMEMODE)currentFrameMode == VSFRAMEMODE.VSFM_Float)
      {
        frame.SetProperty((int)__VSFPROPID.VSFPROPID_FrameMode, VSFRAMEMODE.VSFM_Dock);
      }
    }

    /// <summary>
    /// Hides the build progress tool window.
    /// </summary>
    private void HideBuildProgressToolWindow()
    {
      ThreadHelper.ThrowIfNotOnUIThread();
      IVsWindowFrame frame = BuildProgressWindowFrame;
      frame?.Hide();
    }

    /// <summary>
    /// Shows the build progress tool window.
    /// </summary>
    private void ShowBuildProgressToolWindow()
    {
      IVsWindowFrame frame = BuildProgressWindowFrame;
      ThreadHelper.ThrowIfNotOnUIThread();
      if (frame != null)
      {
        frame.Show();
        DockWindowIfFloating(frame);
      }
    }

    /// <summary>
    /// Shows the build progress tool window without activation.
    /// </summary>
    private void ShowBuildProgressToolWindowWithoutActivation()
    {
      IVsWindowFrame frame = BuildProgressWindowFrame;
      ThreadHelper.ThrowIfNotOnUIThread();
      if (frame != null)
      {
        frame.ShowNoActivate();
        DockWindowIfFloating(frame);
      }
    }
  }
}
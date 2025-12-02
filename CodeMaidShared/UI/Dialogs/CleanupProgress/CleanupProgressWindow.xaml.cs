using System.ComponentModel;
using System.Reflection;
using System.Windows;

namespace ASGV.CodeMaid.UI.Dialogs.CleanupProgress
{
    /// <summary>
    /// Interaction logic for CleanupProgressWindow.xaml
    /// </summary>
    public partial class CleanupProgressWindow
    {
        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="CleanupProgressWindow" /> class.
        /// </summary>
        public CleanupProgressWindow()
        {
            Application.ResourceAssembly = Assembly.GetExecutingAssembly();

            InitializeComponent();
        }

        #endregion Constructors

        #region Private Event Handlers

        /// <summary>
        /// Called when the window is attempting to close.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">
        /// The <see cref="System.ComponentModel.CancelEventArgs" /> instance containing the event data.
        /// </param>
        private void OnClosing(object sender, CancelEventArgs e)
        {
            if (DataContext is CleanupProgressViewModel viewModel && viewModel.DialogResult == null)
            {
                viewModel.CancelCommand.Execute(null);
                e.Cancel = true;
            }
        }

        #endregion Private Event Handlers
    }
}
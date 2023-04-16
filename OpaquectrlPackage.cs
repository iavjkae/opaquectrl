using Microsoft;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.Settings;
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;
using Task = System.Threading.Tasks.Task;

namespace com.iavjkae.opaquectrl
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the
    /// IVsPackage interface and uses the registration attributes defined in the framework to
    /// register itself and its components with the shell. These attributes tell the pkgdef creation
    /// utility what data to put into .pkgdef file.
    /// </para>
    /// <para>
    /// To get loaded into VS, the package must be referred by &lt;Asset Type="Microsoft.VisualStudio.VsPackage" ...&gt; in .vsixmanifest file.
    /// </para>
    /// </remarks>
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [ProvideAutoLoad(UIContextGuids80.NoSolution, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideAutoLoad(UIContextGuids80.SolutionExists, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideOptionPage(typeof(OpaquectrlOptionPage),
    OpaquectrlPackage.ExtensionName, "generic", 0, 0, true)]
    [Guid(OpaquectrlPackage.PackageGuidString)]
    public sealed class OpaquectrlPackage : AsyncPackage
    {
        /// <summary>
        /// opaquectrlPackage GUID string.
        /// </summary>
        public const string PackageGuidString = "64b4c090-e6a2-4ed5-aef4-4ac469e1ed31";
        public const string ExtensionName = "Opaquectrl";

        private readonly Opaquectrl _opctrl = new Opaquectrl();
        private SettingsManager _settingManager;

       #region Package Members

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to monitor for initialization cancellation, which can occur when VS is shutting down.</param>
        /// <param name="progress">A provider for progress updates.</param>
        /// <returns>A task representing the async work of package initialization, or an already completed task if there is none. Do not return null from this method.</returns>
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            // When initialized asynchronously, the current thread may be a background thread at this point.
            // Do any initialization that requires the UI thread after switching to the UI thread.
            await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            // init _settingManager
            if(_settingManager == null)
            {
                _settingManager = new ShellSettingsManager(this);
            }

            Requires.NotNull(_settingManager, nameof(_settingManager));
            // init setting
            var store = _settingManager.GetWritableSettingsStore(SettingsScope.UserSettings);
            Requires.NotNull(store, nameof(store));
            if(!store.CollectionExists(ExtensionName))
            {
                store.CreateCollection(ExtensionName);
                store.SetBoolean(ExtensionName, nameof(OpaquectrlOptionPage.Enable), false);
            }


            OpaquectrlOptionPage page = GetDialogPage(typeof(OpaquectrlOptionPage)) as OpaquectrlOptionPage;
            bool enabled = store.GetBoolean(ExtensionName, nameof(OpaquectrlOptionPage.Enable), false);

            Requires.NotNull(page, nameof(page));
            if(enabled)
            {
                try
                {
                    _opctrl.Start();
                }catch
                {
                    // set enable to false if starting failed
                    page.Enable = false;
                    store.SetBoolean(ExtensionName, nameof(OpaquectrlOptionPage.Enable), false);
                }
            }

            // setting up OnOptionChanged
            page.OnOptionChanged += (s, e) =>
            {
                var p = s as OpaquectrlOptionPage;
                try
                {
                    Requires.NotNull(p, nameof(p));
                    if (p.Enable)
                    {
                        if (!_opctrl.IsWorking())
                        {
                            _opctrl.Start();
                        }
                    }
                    else
                    {
                        if (_opctrl.IsWorking())
                        {
                            _opctrl.Stop();
                        }
                    }
                    // write store
                    store.SetBoolean(ExtensionName, nameof(OpaquectrlOptionPage.Enable), p.Enable);
                }
                catch
                {
                    // no action
                }
            };
        }
        #endregion
    }

    public class OpaquectrlOptionPage: DialogPage
    {
        private bool enable_ = false;
        public event EventHandler OnOptionChanged;
        [Category("generic")]
        [DisplayName("enable opaquectrl")]
        [Description("enable or disable opaquectrl")]
        public bool Enable
        {
            get => enable_; set
            {
                enable_ = value; OnOptionChanged?.Invoke(this, EventArgs.Empty);
            }
        }

    }
}

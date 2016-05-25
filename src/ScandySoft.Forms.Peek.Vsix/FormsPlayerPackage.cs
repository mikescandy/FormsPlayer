using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Xamarin.Forms.Player;
using Xamarin.Forms.Player.Diagnostics;

namespace ScandySoft.Forms.Peek
{
	[PackageRegistration (UseManagedResourcesOnly = true)]
	[InstalledProductRegistration ("#110", "#112", "1.0", IconResourceID = 400)]
	[ProvideMenuResource ("Menus.ctmenu", 1)]
	[ProvideToolWindow (typeof (FormsPeekWindow))]
	[Guid (GuidList.guidFormsPlayerPkgString)]
	public sealed class FormsPeekPackage : Package
	{
	    private void ShowToolWindow (object sender, EventArgs e)
		{
			var window = FindToolWindow (typeof (FormsPeekWindow), 0, true);
			if (window?.Frame == null) {
				throw new NotSupportedException (Resources.CanNotCreateWindow);
			}

			var windowFrame = (IVsWindowFrame)window.Frame;
			ErrorHandler.ThrowOnFailure (windowFrame.Show ());
		}

		protected override void Initialize ()
		{
			base.Initialize ();

			var manager = new TracerManager();
			manager.SetTracingLevel (GetType ().Namespace, SourceLevels.Information);
			Tracer.Initialize (manager);

			Tracer.Get<FormsPeekPackage> ().Info ("!Xamarin Forms Player Initialized");

			var mcs = GetService (typeof (IMenuCommandService)) as OleMenuCommandService;
		    if (null == mcs) return;
		    // Create the command for the tool window
		    var toolwndCommandId = new CommandID (GuidList.guidFormsPlayerCmdSet, (int)PkgCmdIDList.cmdXamarinFormsPlayer);
		    var menuToolWin = new MenuCommand (ShowToolWindow, toolwndCommandId);
		    mcs.AddCommand (menuToolWin);
		}
	}
}

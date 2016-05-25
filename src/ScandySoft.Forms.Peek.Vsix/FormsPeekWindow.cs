using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;

namespace ScandySoft.Forms.Peek
{
	[Guid ("d619de43-8c34-4d69-96a2-186e6343b238")]
	public sealed class FormsPeekWindow : ToolWindowPane
	{
		public FormsPeekWindow() :
			base (null)
		{
			Caption = Resources.ToolWindowTitle;

			BitmapResourceID = 300;
			BitmapIndex = 1;

			Content = ComponentModel.GlobalComponents.GetService<ScandySoft.Forms.Peek.FormsPlayer> ();
		}
	}
}

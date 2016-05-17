using System.ComponentModel.Composition;
using System.Windows.Controls;

namespace ScandySoft.Forms.Peek
{
	[PartCreationPolicy(CreationPolicy.NonShared)]
	[Export]
    public partial class FormsPlayer : UserControl
    {
        public FormsPlayer()
        {
            InitializeComponent();
        }

		[ImportingConstructor]
		public FormsPlayer (FormsPlayerViewModel model)
			: this()
		{
			DataContext = model;
		}
	}
}
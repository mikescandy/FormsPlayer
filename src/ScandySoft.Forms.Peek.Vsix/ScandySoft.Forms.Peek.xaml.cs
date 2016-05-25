using System.ComponentModel.Composition;

namespace ScandySoft.Forms.Peek
{
	[PartCreationPolicy(CreationPolicy.NonShared)]
	[Export]
    public partial class FormsPlayer
	{
        public FormsPlayer()
        {
            InitializeComponent();
        }

		[ImportingConstructor]
		public FormsPlayer (FormsPeekViewModel model)
			: this()
		{
			DataContext = model;
		}
	}
}
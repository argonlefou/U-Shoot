using System.Windows.Forms;

namespace U_Shoot.GUI.Controls
{
    public partial class Ctrl_Option : UserControl
    {
        private bool _IsLoaded = false;

        public Ctrl_Option()
        {
            InitializeComponent();
        }

        private void Ctrl_Option_Load(object sender, System.EventArgs e)
        {
            //Need to wait before all child-controls creation or SizeChanged event that occurs during creation will try to acces non-existent controls
            _IsLoaded = true;
            DrawLayout();
        }

        public virtual string GetOptionName()
        {
            return Lbl_OptionName.Text;
        }

        public virtual string GetOptionValue()
        { 
            return string.Empty; 
        }

        protected void SetOptionName(string OptionName)
        {
            Lbl_OptionName.Text = OptionName;
        }

        private void Ctrl_Option_SizeChanged(object sender, System.EventArgs e)
        {
            if (_IsLoaded)
                DrawLayout();
        }

        protected virtual void DrawLayout()
        {
            //GUI layout : set different items width, center, etc...
        }
    }
}

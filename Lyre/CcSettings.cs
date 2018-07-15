using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Windows.Forms;
using System.Drawing;

namespace Lyre
{
    class CcSettings : Panel
    {
        private Label ccLabelSettins;
        private Panel ccPanelTest;

        public CcSettings()
        {
            this.DoubleBuffered = true;
            this.BackColor = Shared.preferences.colorBackground;
            this.SizeChanged += CcSettings_SizeChanged;
            this.AutoSize = true;

            InitComponents();
            resizeComponents();
        }

        private void CcSettings_SizeChanged(object sender, EventArgs e)
        {
            resizeComponents();
        }

        private void InitComponents()
        {
            ccLabelSettins = new Label();
            ccLabelSettins.Parent = this;
            this.Controls.Add(ccLabelSettins);
            ccLabelSettins.ForeColor = Shared.preferences.colorFontDefault;
            ccLabelSettins.Font = new Font(Shared.preferences.fontDefault.FontFamily, 36, GraphicsUnit.Pixel);
            ccLabelSettins.Text = "Settings";
            ccLabelSettins.AutoSize = true;

            ccPanelTest = new Panel();
            ccPanelTest.Parent = this;
            this.Controls.Add(ccPanelTest);
            ccPanelTest.BackColor = Shared.preferences.colorAccent2;
        }

        private void resizeComponents()
        {
            int bottomMargin = 30;

            ccLabelSettins.Left = 30;
            ccLabelSettins.Top = 70;

            ccPanelTest.Top = ccLabelSettins.Top + ccLabelSettins.Height + bottomMargin;
            ccPanelTest.Left = ccLabelSettins.Left + 10;
            ccPanelTest.Width = this.Width - (ccPanelTest.Left * 2);
            ccPanelTest.Height = 600;
        }
    }
}

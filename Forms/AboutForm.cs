using System.Drawing;
using System.Windows.Forms;

namespace ConfigEditor.Forms
{
    public class AboutForm : Form
    {
        public AboutForm()
        {
            this.Text = "About Config Editor";
            this.Size = new Size(360, 200);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;

            var lblTitle = new Label
            {
                Text = "Config Editor",
                Font = new Font("Microsoft YaHei", 16, FontStyle.Bold),
                Location = new Point(20, 20),
                Size = new Size(300, 40),
                TextAlign = ContentAlignment.MiddleCenter
            };

            var lblDesc = new Label
            {
                Text = "A hierarchical multi-level INI configuration editor.\n\nManage global, group, file and section-level\nconfiguration items with override support.",
                Location = new Point(20, 70),
                Size = new Size(300, 80),
                TextAlign = ContentAlignment.MiddleCenter
            };

            var btnOk = new Button
            {
                Text = "OK",
                Location = new Point(130, 130),
                Size = new Size(80, 30)
            };
            btnOk.Click += (s, e) => { Close(); };

            this.Controls.AddRange(new Control[] { lblTitle, lblDesc, btnOk });
        }
    }
}

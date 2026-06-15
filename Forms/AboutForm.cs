using System.Drawing;
using System.Windows.Forms;

namespace ConfigEditor.Forms
{
    public class AboutForm : Form
    {
        public AboutForm()
        {
            this.Text = "关于 配置编辑器";
            this.Size = new Size(360, 220);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;

            var lblTitle = new Label
            {
                Text = "配置编辑器",
                Font = new Font("Microsoft YaHei", 16, FontStyle.Bold),
                Location = new Point(20, 20),
                Size = new Size(300, 40),
                TextAlign = ContentAlignment.MiddleCenter
            };

            var lblDesc = new Label
            {
                Text = "多层级 INI 配置文件编辑器\n\n支持全局、组、文件、节四级配置\n优先级: 全局 < 组 < 文件 < 节",
                Location = new Point(20, 65),
                Size = new Size(300, 90),
                TextAlign = ContentAlignment.MiddleCenter
            };

            var btnOk = new Button
            {
                Text = "确定",
                Location = new Point(130, 150),
                Size = new Size(80, 30)
            };
            btnOk.Click += (s, e) => { Close(); };

            this.Controls.AddRange(new Control[] { lblTitle, lblDesc, btnOk });
        }
    }
}

using System;
using System.Drawing;
using System.Windows.Forms;
using ConfigEditor.Models;

namespace ConfigEditor.Forms
{
    public class EditGroupForm : Form
    {
        private TextBox _txtName;
        private Button _btnOk;
        private Button _btnCancel;
        private Label _lblName;

        public string GroupName { get; private set; }

        private EditGroupForm()
        {
            InitializeComponent();
        }

        public EditGroupForm(ConfigGroup group = null)
            : this()
        {
            if (group != null)
                _txtName.Text = group.Name;
        }

        private void InitializeComponent()
        {
            this.Text = "Group";
            this.Size = new Size(380, 140);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;

            _lblName = new Label { Text = "Group Name:", Location = new Point(12, 15), Size = new Size(80, 23) };
            _txtName = new TextBox { Location = new Point(100, 12), Size = new Size(250, 23) };

            _btnOk = new Button { Text = "OK", Location = new Point(190, 55), Size = new Size(75, 30) };
            _btnOk.Click += BtnOk_Click;

            _btnCancel = new Button { Text = "Cancel", Location = new Point(275, 55), Size = new Size(75, 30) };
            _btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            this.Controls.AddRange(new Control[] { _lblName, _txtName, _btnOk, _btnCancel });
            this.AcceptButton = _btnOk;
            this.CancelButton = _btnCancel;
        }

        private void BtnOk_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_txtName.Text))
            {
                MessageBox.Show("Group name cannot be empty.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            GroupName = _txtName.Text.Trim();
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}

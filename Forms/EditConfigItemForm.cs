using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ConfigEditor.Models;

namespace ConfigEditor.Forms
{
    public class EditConfigItemForm : Form
    {
        private TextBox _txtKey;
        private TextBox _txtValue;
        private ComboBox _cmbSection;
        private TextBox _txtComment;
        private Button _btnOk;
        private Button _btnCancel;
        private Label _lblKey;
        private Label _lblValue;
        private Label _lblSection;
        private Label _lblComment;

        public ConfigItem ConfigItem { get; private set; }

        private EditConfigItemForm()
        {
            InitializeComponent();
        }

        public EditConfigItemForm(ConfigItem item, IEnumerable<string> sectionNames, bool showSection)
            : this()
        {
            _txtKey.Text = item?.Key ?? "";
            _txtValue.Text = item?.Value ?? "";
            _txtComment.Text = item?.Comment ?? "";

            _cmbSection.Items.Clear();
            _cmbSection.Items.Add("");
            if (sectionNames != null)
            {
                foreach (var sn in sectionNames.Where(s => !string.IsNullOrEmpty(s)))
                {
                    if (!_cmbSection.Items.Contains(sn))
                        _cmbSection.Items.Add(sn);
                }
            }

            if (item != null)
                _cmbSection.Text = item.Section ?? "";
            else
                _cmbSection.Text = "";

            _lblSection.Visible = showSection;
            _cmbSection.Visible = showSection;
        }

        private void InitializeComponent()
        {
            this.Text = "配置项";
            this.Size = new Size(480, 280);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;

            _lblKey = new Label { Text = "键(&K):", Location = new Point(12, 15), Size = new Size(80, 23) };
            _txtKey = new TextBox { Location = new Point(100, 12), Size = new Size(350, 23), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };

            _lblValue = new Label { Text = "值(&V):", Location = new Point(12, 45), Size = new Size(80, 23) };
            _txtValue = new TextBox { Location = new Point(100, 42), Size = new Size(350, 23), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };

            _lblSection = new Label { Text = "所属节(&S):", Location = new Point(12, 75), Size = new Size(80, 23) };
            _cmbSection = new ComboBox
            {
                Location = new Point(100, 72),
                Size = new Size(350, 23),
                DropDownStyle = ComboBoxStyle.DropDown,
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right
            };

            _lblComment = new Label { Text = "备注(&C):", Location = new Point(12, 105), Size = new Size(80, 23) };
            _txtComment = new TextBox { Location = new Point(100, 102), Size = new Size(350, 60), Multiline = true, AcceptsReturn = false, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };

            _btnOk = new Button { Text = "确定", Location = new Point(280, 180), Size = new Size(80, 30), Anchor = AnchorStyles.Bottom | AnchorStyles.Right };
            _btnOk.Click += BtnOk_Click;

            _btnCancel = new Button { Text = "取消", Location = new Point(370, 180), Size = new Size(80, 30), Anchor = AnchorStyles.Bottom | AnchorStyles.Right };
            _btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            this.Controls.AddRange(new Control[]
            {
                _lblKey, _txtKey, _lblValue, _txtValue,
                _lblSection, _cmbSection, _lblComment, _txtComment,
                _btnOk, _btnCancel
            });

            this.AcceptButton = _btnOk;
            this.CancelButton = _btnCancel;
        }

        private void BtnOk_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_txtKey.Text))
            {
                MessageBox.Show("键不能为空。", "验证错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            ConfigItem = new ConfigItem(
                _txtKey.Text.Trim(),
                _txtValue.Text,
                _cmbSection.Text?.Trim() ?? "",
                _txtComment.Text.Trim()
            );

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}

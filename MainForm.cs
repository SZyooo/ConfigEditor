using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using ConfigEditor.Forms;
using ConfigEditor.Models;
using ConfigEditor.Services;

namespace ConfigEditor
{
    public partial class MainForm : Form
    {
        private ProjectData _project;
        private string _projectFilePath;
        private bool _isModified;

        private SplitContainer _splitContainer;
        private TreeView _treeView;
        private DataGridView _dataGridView;
        private StatusStrip _statusStrip;
        private ToolStripStatusLabel _statusLabel;
        private ToolStripStatusLabel _fileCountLabel;
        private ContextMenuStrip _treeMenu;
        private ContextMenuStrip _gridMenu;

        public MainForm()
        {
            Text = "Config Editor";
            Size = new Size(1100, 700);
            StartPosition = FormStartPosition.CenterScreen;

            CreateMenuStrip();
            CreateStatusStrip();
            CreateSplitContainer();
            CreateTreeView();
            CreateDataGridView();
            CreateContextMenus();

            _project = new ProjectData();
            UpdateTitle();
            RebuildTree();

            Controls.Add(_splitContainer);
            Shown += (s, e) =>
            {
                _splitContainer.Panel1MinSize = 200;
                _splitContainer.Panel2MinSize = 250;
                _splitContainer.SplitterDistance = 320;
                if (_treeView.Nodes.Count > 0)
                {
                    var firstChild = _treeView.Nodes[0].FirstNode;
                    if (firstChild != null)
                        _treeView.SelectedNode = firstChild;
                    else
                        _treeView.SelectedNode = _treeView.Nodes[0];
                }
            };
        }

        private void CreateMenuStrip()
        {
            var menu = new MenuStrip();

            var fileMenu = menu.Items.Add("&File") as ToolStripMenuItem;
            fileMenu.DropDownItems.AddRange(new ToolStripItem[]
            {
                CreateMenuItem("&New Project", "Ctrl+N", (s, e) => NewProject()),
                CreateMenuItem("&Open...", "Ctrl+O", (s, e) => OpenProject()),
                CreateMenuItem("&Save", "Ctrl+S", (s, e) => SaveProject()),
                CreateMenuItem("Save &As...", null, (s, e) => SaveProjectAs()),
                new ToolStripSeparator(),
                CreateMenuItem("&Import INI File...", "Ctrl+I", (s, e) => ImportIniFile()),
                CreateMenuItem("&Export All", "Ctrl+E", (s, e) => ExportAll()),
                new ToolStripSeparator(),
                CreateMenuItem("E&xit", "Alt+F4", (s, e) => Close())
            });

            var editMenu = menu.Items.Add("&Edit") as ToolStripMenuItem;
            editMenu.DropDownItems.AddRange(new ToolStripItem[]
            {
                CreateMenuItem("&Add Config Item", "Ins", (s, e) => AddConfigItem()),
                CreateMenuItem("&Edit Config Item", "Enter", (s, e) => EditConfigItem()),
                CreateMenuItem("&Delete Config Item", "Del", (s, e) => DeleteConfigItem()),
                new ToolStripSeparator(),
                CreateMenuItem("Add &Group...", null, (s, e) => AddGroup()),
                CreateMenuItem("Add &File...", null, (s, e) => AddFile()),
                CreateMenuItem("Add &Section...", null, (s, e) => AddSection())
            });

            var viewMenu = menu.Items.Add("&View") as ToolStripMenuItem;
            viewMenu.DropDownItems.AddRange(new ToolStripItem[]
            {
                CreateMenuItem("&Refresh Tree", "F5", (s, e) => RebuildTree())
            });

            var helpMenu = menu.Items.Add("&Help") as ToolStripMenuItem;
            helpMenu.DropDownItems.Add(new ToolStripMenuItem("&About", null, (s, e) => new AboutForm().ShowDialog(this)));

            MainMenuStrip = menu;
            Controls.Add(menu);
        }

        private ToolStripMenuItem CreateMenuItem(string text, string shortcut, EventHandler handler)
        {
            var item = new ToolStripMenuItem(text, null, handler);
            if (shortcut != null)
            {
                var keys = shortcut.Split('+');
                if (keys.Length == 2 && Enum.TryParse(keys[1], out Keys k))
                {
                    if (keys[0] == "Ctrl") item.ShortcutKeys = Keys.Control | k;
                    else if (keys[0] == "Alt") item.ShortcutKeys = Keys.Alt | k;
                }
                else if (shortcut == "Ins") item.ShortcutKeys = Keys.Insert;
                else if (shortcut == "Del") item.ShortcutKeys = Keys.Delete;
                else if (shortcut == "F5") item.ShortcutKeys = Keys.F5;
                else if (shortcut == "Enter") { }
            }
            return item;
        }

        private void CreateStatusStrip()
        {
            _statusStrip = new StatusStrip();
            _statusLabel = new ToolStripStatusLabel("Ready") { Spring = true, TextAlign = ContentAlignment.MiddleLeft };
            _fileCountLabel = new ToolStripStatusLabel("Files: 0") { TextAlign = ContentAlignment.MiddleRight };
            _statusStrip.Items.AddRange(new ToolStripItem[] { _statusLabel, _fileCountLabel });
            Controls.Add(_statusStrip);
        }

        private void CreateSplitContainer()
        {
            _splitContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                SplitterWidth = 4
            };
        }

        private void CreateTreeView()
        {
            _treeView = new TreeView
            {
                Dock = DockStyle.Fill,
                HideSelection = false,
                ShowLines = true,
                ShowPlusMinus = true,
                ShowRootLines = true,
                Font = new Font("Microsoft YaHei", 9.75f),
                BorderStyle = BorderStyle.FixedSingle
            };
            _treeView.AfterSelect += TreeView_AfterSelect;
            _treeView.NodeMouseClick += TreeView_NodeMouseClick;
            _treeView.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Delete) DeleteSelectedNode();
                if (e.KeyCode == Keys.F2) RenameSelectedNode();
            };
            _splitContainer.Panel1.Controls.Add(_treeView);
        }

        private void CreateDataGridView()
        {
            _dataGridView = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                ReadOnly = false,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BorderStyle = BorderStyle.FixedSingle,
                Font = new Font("Microsoft YaHei", 9.75f),
                BackgroundColor = SystemColors.Window,
                AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle { BackColor = Color.FromArgb(245, 245, 250) }
            };
            _dataGridView.CellMouseClick += GridView_CellMouseClick;
            _dataGridView.CellEndEdit += GridView_CellEndEdit;
            _dataGridView.KeyDown += GridView_KeyDown;
            _splitContainer.Panel2.Controls.Add(_dataGridView);
        }

        private void CreateContextMenus()
        {
            _treeMenu = new ContextMenuStrip();
            _treeMenu.Opening += (s, e) => BuildTreeContextMenu();
            _treeView.ContextMenuStrip = _treeMenu;

            _gridMenu = new ContextMenuStrip();
            _gridMenu.Opening += (s, e) =>
            {
                var data = GetSelectedNodeData();
                bool canEdit = data != null && (data.Type == NodeType.GlobalConfig ||
                    data.Type == NodeType.GroupSettings || data.Type == NodeType.Group ||
                    data.Type == NodeType.Section);
                _gridMenu.Items[0].Enabled = canEdit;
                _gridMenu.Items[1].Enabled = canEdit && _dataGridView.CurrentRow?.Tag != null;
                _gridMenu.Items[2].Enabled = canEdit && _dataGridView.CurrentRow?.Tag != null;
            };
            _gridMenu.Items.Add("&Add Item", null, (s, e) => AddConfigItem());
            _gridMenu.Items.Add("&Edit Item", null, (s, e) => EditConfigItem());
            _gridMenu.Items.Add("&Delete Item", null, (s, e) => DeleteConfigItem());
            _dataGridView.ContextMenuStrip = _gridMenu;
        }

        private void BuildTreeContextMenu()
        {
            _treeMenu.Items.Clear();
            var data = GetSelectedNodeData();
            if (data == null) return;

            switch (data.Type)
            {
                case NodeType.Project:
                    _treeMenu.Items.Add("Add &File...", null, (s, e) => AddFile());
                    _treeMenu.Items.Add("Add &Group...", null, (s, e) => AddGroup());
                    _treeMenu.Items.Add(new ToolStripSeparator());
                    _treeMenu.Items.Add("&Import INI...", null, (s, e) => ImportIniFile());
                    _treeMenu.Items.Add("&Export All", null, (s, e) => ExportAll());
                    break;
                case NodeType.GlobalConfig:
                    _treeMenu.Items.Add("&Add Item", null, (s, e) => AddConfigItem());
                    break;
                case NodeType.GroupsFolder:
                    _treeMenu.Items.Add("Add &Group...", null, (s, e) => AddGroup());
                    break;
                case NodeType.Group:
                    _treeMenu.Items.Add("&Edit Group...", null, (s, e) => EditGroup());
                    _treeMenu.Items.Add("&Delete Group", null, (s, e) => DeleteGroup());
                    _treeMenu.Items.Add(new ToolStripSeparator());
                    _treeMenu.Items.Add("&Add Item to Settings", null, (s, e) => AddConfigItem());
                    _treeMenu.Items.Add("&Manage Members...", null, (s, e) => ManageGroupMembers());
                    break;
                case NodeType.GroupSettings:
                    _treeMenu.Items.Add("&Add Item", null, (s, e) => AddConfigItem());
                    break;
                case NodeType.FilesFolder:
                    _treeMenu.Items.Add("Add &File...", null, (s, e) => AddFile());
                    _treeMenu.Items.Add("&Import INI...", null, (s, e) => ImportIniFile());
                    break;
                case NodeType.File:
                    _treeMenu.Items.Add("Add &Section...", null, (s, e) => AddSection());
                    _treeMenu.Items.Add(new ToolStripSeparator());
                    _treeMenu.Items.Add("&Edit File...", null, (s, e) => EditFile());
                    _treeMenu.Items.Add("&Delete File", null, (s, e) => DeleteFile());
                    _treeMenu.Items.Add(new ToolStripSeparator());
                    _treeMenu.Items.Add("&Generate This File", null, (s, e) => ExportSingleFile());
                    break;
                case NodeType.Section:
                    _treeMenu.Items.Add("&Add Item", null, (s, e) => AddConfigItem());
                    _treeMenu.Items.Add(new ToolStripSeparator());
                    _treeMenu.Items.Add("&Rename Section", null, (s, e) => RenameSection());
                    _treeMenu.Items.Add("&Delete Section", null, (s, e) => DeleteSection());
                    break;
            }
        }

        #region Tree Building

        private void RebuildTree()
        {
            _treeView.Nodes.Clear();
            if (_project == null) return;

            var rootNode = new TreeNode($"Project: {_project.Name}")
            {
                Tag = new TreeNodeData { Type = NodeType.Project }
            };

            AddGlobalConfigNode(rootNode);
            AddGroupsNode(rootNode);
            AddFilesNode(rootNode);

            _treeView.Nodes.Add(rootNode);
            rootNode.Expand();
        }

        private void AddGlobalConfigNode(TreeNode parent)
        {
            var count = _project.GlobalConfig?.Count ?? 0;
            var node = new TreeNode($"Global Config ({count} items)")
            {
                Tag = new TreeNodeData { Type = NodeType.GlobalConfig }
            };
            parent.Nodes.Add(node);
        }

        private void AddGroupsNode(TreeNode parent)
        {
            var groupsNode = new TreeNode($"Groups ({_project.Groups.Count})")
            {
                Tag = new TreeNodeData { Type = NodeType.GroupsFolder }
            };

            foreach (var group in _project.Groups)
            {
                var groupNode = new TreeNode(group.Name)
                {
                    Tag = new TreeNodeData { Type = NodeType.Group, GroupId = group.Id }
                };

                var settingsNode = new TreeNode($"Settings ({group.Settings.Count})")
                {
                    Tag = new TreeNodeData { Type = NodeType.GroupSettings, GroupId = group.Id }
                };
                groupNode.Nodes.Add(settingsNode);

                var membersNode = new TreeNode($"Members ({group.MemberFileIds.Count})")
                {
                    Tag = new TreeNodeData { Type = NodeType.GroupMembers, GroupId = group.Id }
                };
                foreach (var fileId in group.MemberFileIds)
                {
                    var file = FindFile(fileId);
                    if (file != null)
                    {
                        membersNode.Nodes.Add(new TreeNode(file.FileName)
                        {
                            Tag = new TreeNodeData { Type = NodeType.File, FileId = fileId }
                        });
                    }
                }
                groupNode.Nodes.Add(membersNode);

                groupsNode.Nodes.Add(groupNode);
            }

            parent.Nodes.Add(groupsNode);
        }

        private void AddFilesNode(TreeNode parent)
        {
            var filesNode = new TreeNode($"Files ({_project.Files.Count})")
            {
                Tag = new TreeNodeData { Type = NodeType.FilesFolder }
            };

            foreach (var file in _project.Files)
            {
                AddFileNode(filesNode, file);
            }

            parent.Nodes.Add(filesNode);
        }

        private TreeNode AddFileNode(TreeNode parent, IniFileModel file)
        {
            var fileNode = new TreeNode(file.FileName)
            {
                Tag = new TreeNodeData { Type = NodeType.File, FileId = file.Id }
            };

            foreach (var section in file.Sections)
            {
                var displayName = string.IsNullOrEmpty(section.Name) ? "[root]" : $"[{section.Name}]";
                var sectionNode = new TreeNode($"{displayName} ({section.Items.Count})")
                {
                    Tag = new TreeNodeData
                    {
                        Type = NodeType.Section,
                        FileId = file.Id,
                        SectionName = section.Name
                    }
                };
                fileNode.Nodes.Add(sectionNode);
            }

            parent.Nodes.Add(fileNode);
            return fileNode;
        }

        #endregion

        #region Tree Event Handlers

        private void TreeView_AfterSelect(object sender, TreeViewEventArgs e)
        {
            var data = GetSelectedNodeData();
            if (data == null)
            {
                ShowEmptyState();
                return;
            }

            switch (data.Type)
            {
                case NodeType.Project:
                    ShowProjectSummary();
                    break;
                case NodeType.GlobalConfig:
                    ShowGlobalConfig();
                    break;
                case NodeType.GroupsFolder:
                    ShowGroupsSummary();
                    break;
                case NodeType.Group:
                    ShowGroupSummary(data.GroupId);
                    break;
                case NodeType.GroupSettings:
                    ShowGroupSettings(data.GroupId);
                    break;
                case NodeType.GroupMembers:
                    ShowGroupMembers(data.GroupId);
                    break;
                case NodeType.FilesFolder:
                    ShowFilesSummary();
                    break;
                case NodeType.File:
                    ShowEffectiveConfig(data.FileId);
                    break;
                case NodeType.Section:
                    ShowSectionConfig(data.FileId, data.SectionName);
                    break;
            }
        }

        private void TreeView_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
                _treeView.SelectedNode = e.Node;
        }

        #endregion

        #region Display Methods

        private void ClearGrid()
        {
            while (_dataGridView.Controls.Count > 0)
                _dataGridView.Controls[0].Dispose();
            _dataGridView.Columns.Clear();
            _dataGridView.Rows.Clear();
            _dataGridView.ReadOnly = false;
            _dataGridView.AllowUserToAddRows = false;
            _dataGridView.AllowUserToDeleteRows = false;
        }

        private void ShowEmptyState()
        {
            ClearGrid();
            _statusLabel.Text = "Ready";
        }

        private void ShowProjectSummary()
        {
            ClearGrid();
            _dataGridView.Columns.Add("Property", "");
            _dataGridView.Columns.Add("Value", "");
            _dataGridView.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            _dataGridView.Rows.Add("Project Name", _project.Name);
            _dataGridView.Rows.Add("Global Config Items", _project.GlobalConfig.Count);
            _dataGridView.Rows.Add("Groups", _project.Groups.Count);
            _dataGridView.Rows.Add("Files", _project.Files.Count);
            _dataGridView.Rows.Add("", "");
            _dataGridView.Rows.Add("Configuration Priority", "Global < Group < File < Section");
            _dataGridView.Rows.Add("Tip", "Use File menu or right-click tree to add items");
            _dataGridView.ReadOnly = true;
            _statusLabel.Text = $"Project: {_project.Name}";
        }

        private void ShowGlobalConfig()
        {
            ShowConfigItemList(
                _project.GlobalConfig,
                "Global Config",
                showSection: true,
                onAdd: () => AddConfigItem(),
                canEdit: true
            );
        }

        private void ShowGroupSettings(string groupId)
        {
            var group = FindGroup(groupId);
            if (group == null) return;
            ShowConfigItemList(
                group.Settings,
                $"Group: {group.Name}",
                showSection: true,
                onAdd: () => AddConfigItem(),
                canEdit: true
            );
        }

        private void ShowGroupMembers(string groupId)
        {
            ClearGrid();
            var group = FindGroup(groupId);
            if (group == null) return;

            _dataGridView.Columns.Add("FileName", "File Name");
            _dataGridView.Columns.Add("OutputPath", "Output Path");
            _dataGridView.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            foreach (var fileId in group.MemberFileIds)
            {
                var file = FindFile(fileId);
                if (file != null)
                    _dataGridView.Rows.Add(file.FileName, file.OutputPath);
            }

            _dataGridView.ReadOnly = true;
            _statusLabel.Text = $"Members of group '{group.Name}': {group.MemberFileIds.Count} files";
        }

        private void ShowGroupSummary(string groupId)
        {
            var group = FindGroup(groupId);
            if (group == null) return;
            ShowConfigItemList(
                group.Settings,
                $"Group: {group.Name}",
                showSection: true,
                onAdd: () => AddConfigItem(),
                canEdit: true
            );
        }

        private void ShowSectionConfig(string fileId, string sectionName)
        {
            var file = FindFile(fileId);
            if (file == null) return;

            var section = file.Sections.FirstOrDefault(s => s.Name == sectionName);
            if (section == null) return;

            ShowConfigItemList(
                section.Items,
                $"File: {file.FileName}  |  Section: {(string.IsNullOrEmpty(sectionName) ? "[root]" : "[" + sectionName + "]")}",
                showSection: false,
                onAdd: () => AddConfigItem(),
                canEdit: true
            );
        }

        private void ShowEffectiveConfig(string fileId)
        {
            var file = FindFile(fileId);
            if (file == null) return;

            ClearGrid();
            _dataGridView.Columns.Add("Section", "Section");
            _dataGridView.Columns.Add("Key", "Key");
            _dataGridView.Columns.Add("Value", "Value");
            _dataGridView.Columns.Add("Source", "Source");
            _dataGridView.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            var allSections = ProjectService.GetAllSections(_project, file);
            foreach (var section in allSections)
            {
                var resolved = ProjectService.ResolveConfig(_project, file, section.Name);
                var source = ProjectService.GetEffectiveSourceString(_project, file, section.Name);
                foreach (var item in resolved)
                {
                    var displaySection = string.IsNullOrEmpty(section.Name) ? "[root]" : section.Name;
                    _dataGridView.Rows.Add(displaySection, item.Key, item.Value, source);
                }
            }

            _dataGridView.ReadOnly = true;
            _dataGridView.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(245, 245, 250);
            _statusLabel.Text = $"Effective Config: {file.FileName}";
        }

        private void ShowConfigItemList(List<ConfigItem> items, string title, bool showSection, Action onAdd, bool canEdit)
        {
            ClearGrid();

            if (showSection)
            {
                _dataGridView.Columns.Add("Section", "Section");
                _dataGridView.Columns["Section"].AutoSizeMode = DataGridViewAutoSizeColumnMode.DisplayedCells;
            }
            _dataGridView.Columns.Add("Key", "Key");
            _dataGridView.Columns.Add("Value", "Value");
            _dataGridView.Columns.Add("Comment", "Comment");
            _dataGridView.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            if (showSection)
            {
                foreach (var item in items)
                {
                    var displaySection = string.IsNullOrEmpty(item.Section) ? "[root]" : item.Section;
                    _dataGridView.Rows.Add(displaySection, item.Key, item.Value, item.Comment);
                    _dataGridView.Rows[_dataGridView.Rows.Count - 1].Tag = item;
                }
            }
            else
            {
                foreach (var item in items)
                {
                    _dataGridView.Rows.Add(item.Key, item.Value, item.Comment);
                    _dataGridView.Rows[_dataGridView.Rows.Count - 1].Tag = item;
                }
            }

            _dataGridView.ReadOnly = false;
            _dataGridView.AllowUserToAddRows = canEdit;
            _dataGridView.AllowUserToDeleteRows = canEdit;

            _statusLabel.Text = title + $" ({items.Count} items)";
        }

        private void ShowGroupsSummary()
        {
            ClearGrid();
            _dataGridView.Columns.Add("Name", "Group Name");
            _dataGridView.Columns.Add("Settings", "Settings");
            _dataGridView.Columns.Add("Members", "Member Files");
            _dataGridView.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            foreach (var group in _project.Groups)
            {
                _dataGridView.Rows.Add(group.Name, group.Settings.Count, group.MemberFileIds.Count);
            }

            _dataGridView.ReadOnly = true;
            _statusLabel.Text = $"Groups: {_project.Groups.Count}";
        }

        private void ShowFilesSummary()
        {
            ClearGrid();
            _dataGridView.Columns.Add("FileName", "File Name");
            _dataGridView.Columns.Add("Sections", "Sections");
            _dataGridView.Columns.Add("OutputPath", "Output Path");
            _dataGridView.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            foreach (var file in _project.Files)
            {
                var sectionCount = file.Sections.Count;
                _dataGridView.Rows.Add(file.FileName, sectionCount, file.OutputPath);
            }

            _dataGridView.ReadOnly = true;
            _statusLabel.Text = $"Files: {_project.Files.Count}";
        }

        #endregion

        #region Grid Event Handlers

        private void GridView_CellMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right && e.RowIndex >= 0)
            {
                _dataGridView.Rows[e.RowIndex].Selected = true;
                var data = GetSelectedNodeData();
                if (data != null)
                {
                    bool readOnly = data.Type == NodeType.File;
                    _gridMenu.Items[0].Enabled = true;
                    _gridMenu.Items[1].Enabled = !readOnly;
                    _gridMenu.Items[2].Enabled = !readOnly;
                }
            }
        }

        private void GridView_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            var row = _dataGridView.Rows[e.RowIndex];
            if (row.Tag is ConfigItem item)
            {
                var showSection = _dataGridView.Columns.Contains("Section");
                int col = 0;
                if (showSection)
                {
                    var sectionVal = row.Cells[col].Value as string ?? "";
                    item.Section = sectionVal == "[root]" ? "" : sectionVal;
                    col++;
                }
                item.Key = row.Cells[col].Value as string ?? "";
                item.Value = row.Cells[col + 1].Value as string ?? "";
                if (col + 2 < _dataGridView.Columns.Count)
                    item.Comment = row.Cells[col + 2].Value as string ?? "";
                SetModified(true);
            }
        }

        private void GridView_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
                DeleteConfigItem();
            else if (e.KeyCode == Keys.Enter && _dataGridView.CurrentRow != null)
            {
                e.SuppressKeyPress = true;
                EditConfigItem();
            }
            else if (e.KeyCode == Keys.Insert)
                AddConfigItem();
        }

        #endregion

        #region CRUD Operations

        private TreeNodeData GetSelectedNodeData()
        {
            return _treeView.SelectedNode?.Tag as TreeNodeData;
        }

        private string[] GetAllSectionNames()
        {
            var names = new HashSet<string>();
            foreach (var file in _project.Files)
            {
                foreach (var section in file.Sections)
                {
                    if (!string.IsNullOrEmpty(section.Name))
                        names.Add(section.Name);
                }
            }
            return names.OrderBy(n => n).ToArray();
        }

        private void AddConfigItem()
        {
            var data = GetSelectedNodeData();
            if (data == null) return;

            bool showSection = data.Type == NodeType.GlobalConfig ||
                               data.Type == NodeType.GroupSettings ||
                               data.Type == NodeType.Group;

            var form = new EditConfigItemForm(null, GetAllSectionNames(), showSection);
            if (form.ShowDialog(this) != DialogResult.OK) return;

            var item = form.ConfigItem;

            switch (data.Type)
            {
                case NodeType.GlobalConfig:
                    _project.GlobalConfig.Add(item);
                    break;
                case NodeType.GroupSettings:
                case NodeType.Group:
                    var group = FindGroup(data.GroupId);
                    group?.Settings.Add(item);
                    break;
                case NodeType.Section:
                    var file = FindFile(data.FileId);
                    var section = file?.Sections.FirstOrDefault(s => s.Name == data.SectionName);
                    section?.Items.Add(item);
                    break;
                default:
                    return;
            }

            SetModified(true);
            RefreshCurrentView();
        }

        private void EditConfigItem()
        {
            if (_dataGridView.CurrentRow == null) return;
            var item = _dataGridView.CurrentRow.Tag as ConfigItem;
            if (item == null) return;

            var data = GetSelectedNodeData();
            bool showSection = data?.Type == NodeType.GlobalConfig ||
                               data?.Type == NodeType.GroupSettings ||
                               data?.Type == NodeType.Group;

            var form = new EditConfigItemForm(item, GetAllSectionNames(), showSection);
            if (form.ShowDialog(this) != DialogResult.OK) return;

            var newItem = form.ConfigItem;
            item.Key = newItem.Key;
            item.Value = newItem.Value;
            item.Section = newItem.Section;
            item.Comment = newItem.Comment;

            SetModified(true);
            RefreshCurrentView();
        }

        private void DeleteConfigItem()
        {
            if (_dataGridView.CurrentRow == null) return;
            var item = _dataGridView.CurrentRow.Tag as ConfigItem;
            if (item == null) return;

            if (MessageBox.Show($"Delete config item '{item.Key}'?", "Confirm",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            var data = GetSelectedNodeData();
            if (data == null) return;

            switch (data.Type)
            {
                case NodeType.GlobalConfig:
                    _project.GlobalConfig.Remove(item);
                    break;
                case NodeType.GroupSettings:
                case NodeType.Group:
                    var group = FindGroup(data.GroupId);
                    group?.Settings.Remove(item);
                    break;
                case NodeType.Section:
                    var file = FindFile(data.FileId);
                    var section = file?.Sections.FirstOrDefault(s => s.Name == data.SectionName);
                    section?.Items.Remove(item);
                    break;
                default:
                    return;
            }

            SetModified(true);
            RefreshCurrentView();
        }

        private void AddGroup()
        {
            var form = new EditGroupForm();
            if (form.ShowDialog(this) != DialogResult.OK) return;

            var group = new ConfigGroup(form.GroupName);
            _project.Groups.Add(group);
            SetModified(true);
            RebuildTree();
        }

        private void EditGroup()
        {
            var data = GetSelectedNodeData();
            if (data == null) return;
            var group = FindGroup(data.GroupId);
            if (group == null) return;

            var form = new EditGroupForm(group);
            if (form.ShowDialog(this) != DialogResult.OK) return;

            group.Name = form.GroupName;
            SetModified(true);
            RebuildTree();
        }

        private void DeleteGroup()
        {
            var data = GetSelectedNodeData();
            if (data == null) return;
            var group = FindGroup(data.GroupId);
            if (group == null) return;

            if (MessageBox.Show($"Delete group '{group.Name}'?", "Confirm",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            _project.Groups.Remove(group);
            SetModified(true);
            RebuildTree();
        }

        private void ManageGroupMembers()
        {
            var data = GetSelectedNodeData();
            if (data == null) return;
            var group = FindGroup(data.GroupId);
            if (group == null) return;

            var availableFiles = _project.Files
                .Where(f => !group.MemberFileIds.Contains(f.Id))
                .ToList();

            if (availableFiles.Count == 0)
            {
                MessageBox.Show("No unassigned files available.", "Info",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var fileNames = availableFiles.Select(f => f.FileName).ToArray();
            var selected = ShowSelectionDialog("Add Member", "Select a file to add:", fileNames);
            if (selected < 0) return;

            group.MemberFileIds.Add(availableFiles[selected].Id);
            SetModified(true);
            RebuildTree();
        }

        private int ShowSelectionDialog(string title, string prompt, string[] items)
        {
            var form = new Form
            {
                Text = title,
                Size = new Size(400, 300),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false
            };

            var lbl = new Label { Text = prompt, Location = new Point(12, 12), Size = new Size(360, 23) };
            var lb = new ListBox { Location = new Point(12, 40), Size = new Size(360, 180), Items = { } };
            lb.Items.AddRange(items);

            var btnOk = new Button { Text = "OK", Location = new Point(210, 230), Size = new Size(75, 25) };
            var btnCancel = new Button { Text = "Cancel", Location = new Point(295, 230), Size = new Size(75, 25) };

            int result = -1;
            btnOk.Click += (s, e) =>
            {
                if (lb.SelectedIndex >= 0)
                {
                    result = lb.SelectedIndex;
                    form.DialogResult = DialogResult.OK;
                    form.Close();
                }
                else
                {
                    MessageBox.Show("Please select an item.");
                }
            };
            btnCancel.Click += (s, e) => { form.DialogResult = DialogResult.Cancel; form.Close(); };

            form.Controls.AddRange(new Control[] { lbl, lb, btnOk, btnCancel });
            form.ShowDialog(this);
            return result;
        }

        private void AddFile()
        {
            var form = new Form
            {
                Text = "Add File",
                Size = new Size(480, 200),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false
            };

            var lblName = new Label { Text = "Display Name:", Location = new Point(12, 15), Size = new Size(100, 23) };
            var txtName = new TextBox { Location = new Point(120, 12), Size = new Size(330, 23) };

            var lblPath = new Label { Text = "Output Path:", Location = new Point(12, 45), Size = new Size(100, 23) };
            var txtPath = new TextBox { Location = new Point(120, 42), Size = new Size(250, 23) };
            var btnBrowse = new Button { Text = "...", Location = new Point(375, 41), Size = new Size(30, 23) };

            var btnOk = new Button { Text = "OK", Location = new Point(290, 80), Size = new Size(75, 28) };
            var btnCancel = new Button { Text = "Cancel", Location = new Point(375, 80), Size = new Size(75, 28) };

            btnBrowse.Click += (s, e) =>
            {
                var dlg = new SaveFileDialog { Filter = "INI Files (*.ini)|*.ini|All Files (*.*)|*.*" };
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    txtPath.Text = dlg.FileName;
                    if (string.IsNullOrWhiteSpace(txtName.Text))
                        txtName.Text = Path.GetFileName(dlg.FileName);
                }
            };

            btnOk.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtName.Text))
                {
                    MessageBox.Show("Display name is required.");
                    return;
                }
                var file = new IniFileModel(txtName.Text.Trim(), txtPath.Text.Trim());
                _project.Files.Add(file);
                SetModified(true);
                RebuildTree();
                form.Close();
            };

            btnCancel.Click += (s, e) => form.Close();

            form.Controls.AddRange(new Control[]
            {
                lblName, txtName, lblPath, txtPath, btnBrowse,
                btnOk, btnCancel
            });

            form.ShowDialog(this);
        }

        private void EditFile()
        {
            var data = GetSelectedNodeData();
            if (data == null) return;
            var file = FindFile(data.FileId);
            if (file == null) return;

            var form = new Form
            {
                Text = "Edit File",
                Size = new Size(480, 200),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false
            };

            var lblName = new Label { Text = "Display Name:", Location = new Point(12, 15), Size = new Size(100, 23) };
            var txtName = new TextBox { Text = file.FileName, Location = new Point(120, 12), Size = new Size(330, 23) };

            var lblPath = new Label { Text = "Output Path:", Location = new Point(12, 45), Size = new Size(100, 23) };
            var txtPath = new TextBox { Text = file.OutputPath, Location = new Point(120, 42), Size = new Size(250, 23) };
            var btnBrowse = new Button { Text = "...", Location = new Point(375, 41), Size = new Size(30, 23) };

            var btnOk = new Button { Text = "OK", Location = new Point(290, 80), Size = new Size(75, 28) };
            var btnCancel = new Button { Text = "Cancel", Location = new Point(375, 80), Size = new Size(75, 28) };

            btnBrowse.Click += (s, e) =>
            {
                var dlg = new SaveFileDialog { Filter = "INI Files (*.ini)|*.ini|All Files (*.*)|*.*", FileName = txtPath.Text };
                if (dlg.ShowDialog() == DialogResult.OK)
                    txtPath.Text = dlg.FileName;
            };

            btnOk.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtName.Text))
                {
                    MessageBox.Show("Display name is required.");
                    return;
                }
                file.FileName = txtName.Text.Trim();
                file.OutputPath = txtPath.Text.Trim();
                SetModified(true);
                RebuildTree();
                form.Close();
            };

            btnCancel.Click += (s, e) => form.Close();

            form.Controls.AddRange(new Control[]
            {
                lblName, txtName, lblPath, txtPath, btnBrowse,
                btnOk, btnCancel
            });

            form.ShowDialog(this);
        }

        private void DeleteFile()
        {
            var data = GetSelectedNodeData();
            if (data == null) return;
            var file = FindFile(data.FileId);
            if (file == null) return;

            if (MessageBox.Show($"Delete file '{file.FileName}'?\nThis will also remove it from any groups.",
                "Confirm", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            foreach (var group in _project.Groups)
                group.MemberFileIds.Remove(file.Id);

            _project.Files.Remove(file);
            SetModified(true);
            RebuildTree();
        }

        private void AddSection()
        {
            var data = GetSelectedNodeData();
            if (data == null) return;
            var file = FindFile(data.FileId);
            if (file == null) return;

            var input = ShowInputDialog("Add Section", "Enter section name (leave empty for root section):");
            if (input == null) return;

            var sectionName = input.Trim();
            if (file.Sections.Any(s => s.Name == sectionName))
            {
                MessageBox.Show("A section with this name already exists.", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            file.Sections.Add(new SectionModel(sectionName));
            SetModified(true);
            RebuildTree();
        }

        private void RenameSection()
        {
            var data = GetSelectedNodeData();
            if (data == null) return;
            var file = FindFile(data.FileId);
            if (file == null) return;
            var section = file.Sections.FirstOrDefault(s => s.Name == data.SectionName);
            if (section == null) return;

            var input = ShowInputDialog("Rename Section", "Enter new section name (leave empty for root):", section.Name);
            if (input == null) return;

            var newName = input.Trim();
            if (newName != section.Name && file.Sections.Any(s => s.Name == newName))
            {
                MessageBox.Show("A section with this name already exists.", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            section.Name = newName;
            SetModified(true);
            RebuildTree();
        }

        private void DeleteSection()
        {
            var data = GetSelectedNodeData();
            if (data == null) return;
            var file = FindFile(data.FileId);
            if (file == null) return;
            var section = file.Sections.FirstOrDefault(s => s.Name == data.SectionName);
            if (section == null) return;

            var displayName = string.IsNullOrEmpty(section.Name) ? "[root]" : $"[{section.Name}]";
            if (MessageBox.Show($"Delete section {displayName}?", "Confirm",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            file.Sections.Remove(section);
            SetModified(true);
            RebuildTree();
        }

        private void DeleteSelectedNode()
        {
            var data = GetSelectedNodeData();
            if (data == null) return;
            switch (data.Type)
            {
                case NodeType.Group: DeleteGroup(); break;
                case NodeType.File: DeleteFile(); break;
                case NodeType.Section: DeleteSection(); break;
            }
        }

        private void RenameSelectedNode()
        {
            var data = GetSelectedNodeData();
            if (data == null) return;
            switch (data.Type)
            {
                case NodeType.Group: EditGroup(); break;
                case NodeType.Section: RenameSection(); break;
            }
        }

        #endregion

        #region File Operations

        private void NewProject()
        {
            if (_isModified && !ConfirmSave()) return;
            _project = new ProjectData();
            _projectFilePath = null;
            _isModified = false;
            UpdateTitle();
            RebuildTree();
            ClearGrid();
            _statusLabel.Text = "New project created";
            _fileCountLabel.Text = "Files: 0";
        }

        private void OpenProject()
        {
            if (_isModified && !ConfirmSave()) return;
            var dlg = new OpenFileDialog { Filter = ProjectService.GetProjectFileFilter() };
            if (dlg.ShowDialog() != DialogResult.OK) return;
            try
            {
                _project = ProjectService.LoadFromJson(dlg.FileName);
                _projectFilePath = dlg.FileName;
                _isModified = false;
                UpdateTitle();
                RebuildTree();
                _statusLabel.Text = $"Opened: {dlg.FileName}";
                UpdateFileCount();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to open project:\n{ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private bool SaveProject()
        {
            if (string.IsNullOrEmpty(_projectFilePath))
                return SaveProjectAs();

            try
            {
                ProjectService.SaveToJson(_project, _projectFilePath);
                _isModified = false;
                UpdateTitle();
                _statusLabel.Text = $"Saved: {_projectFilePath}";
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save project:\n{ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        private bool SaveProjectAs()
        {
            var dlg = new SaveFileDialog { Filter = ProjectService.GetProjectFileFilter() };
            if (dlg.ShowDialog() != DialogResult.OK) return false;

            _projectFilePath = dlg.FileName;
            return SaveProject();
        }

        private void ExportAll()
        {
            if (_project.Files.Count == 0)
            {
                MessageBox.Show("No files to export.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            try
            {
                ProjectService.ExportAll(_project);
                _statusLabel.Text = $"Exported {_project.Files.Count} files";
                MessageBox.Show($"Successfully exported {_project.Files.Count} files.", "Export Complete",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Export failed:\n{ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ExportSingleFile()
        {
            var data = GetSelectedNodeData();
            if (data == null) return;
            var file = FindFile(data.FileId);
            if (file == null) return;

            try
            {
                ProjectService.ExportSingleFile(_project, file);
                _statusLabel.Text = $"Generated: {file.FileName}";
                MessageBox.Show($"Generated: {file.OutputPath}", "Success",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to generate file:\n{ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ImportIniFile()
        {
            var dlg = new OpenFileDialog { Filter = ProjectService.GetIniFileFilter(), Multiselect = true };
            if (dlg.ShowDialog() != DialogResult.OK) return;

            int count = 0;
            foreach (var filePath in dlg.FileNames)
            {
                try
                {
                    var model = ProjectService.ImportIniFile(filePath);
                    _project.Files.Add(model);
                    count++;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to import '{Path.GetFileName(filePath)}':\n{ex.Message}",
                        "Import Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }

            if (count > 0)
            {
                SetModified(true);
                RebuildTree();
                _statusLabel.Text = $"Imported {count} INI file(s)";
                UpdateFileCount();
            }
        }

        #endregion

        #region Helpers

        private IniFileModel FindFile(string id)
        {
            return _project.Files.FirstOrDefault(f => f.Id == id);
        }

        private ConfigGroup FindGroup(string id)
        {
            return _project.Groups.FirstOrDefault(g => g.Id == id);
        }

        private void SetModified(bool modified)
        {
            _isModified = modified;
            UpdateTitle();
            if (modified) _statusLabel.Text = "Modified";
        }

        private void UpdateTitle()
        {
            var modified = _isModified ? " *" : "";
            var name = _project?.Name ?? "Untitled";
            Text = $"Config Editor - {name}{modified}";
        }

        private void UpdateFileCount()
        {
            _fileCountLabel.Text = $"Files: {_project.Files.Count}";
        }

        private string ShowInputDialog(string title, string prompt, string defaultValue = "")
        {
            var form = new Form
            {
                Text = title,
                Size = new Size(420, 150),
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterParent,
                MaximizeBox = false,
                MinimizeBox = false
            };

            var lbl = new Label { Text = prompt, Location = new Point(12, 12), Size = new Size(380, 23) };
            var txt = new TextBox { Text = defaultValue, Location = new Point(12, 40), Size = new Size(380, 23) };
            var btnOk = new Button { Text = "OK", Location = new Point(230, 75), Size = new Size(75, 28) };
            var btnCancel = new Button { Text = "Cancel", Location = new Point(315, 75), Size = new Size(75, 28) };

            string result = null;
            btnOk.Click += (s, e) =>
            {
                result = txt.Text;
                form.DialogResult = DialogResult.OK;
                form.Close();
            };
            btnCancel.Click += (s, e) =>
            {
                form.DialogResult = DialogResult.Cancel;
                form.Close();
            };

            form.Controls.AddRange(new Control[] { lbl, txt, btnOk, btnCancel });
            form.AcceptButton = btnOk;
            form.ShowDialog(this);
            return result;
        }

        private bool ConfirmSave()
        {
            var result = MessageBox.Show("Save changes?", "Unsaved Changes",
                MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
            if (result == DialogResult.Yes) return SaveProject();
            return result == DialogResult.No;
        }

        private void RefreshCurrentView()
        {
            var selected = _treeView.SelectedNode;
            if (selected != null)
            {
                TreeView_AfterSelect(null, new TreeViewEventArgs(selected));
                UpdateTreeCounts();
            }
        }

        private void UpdateTreeCounts()
        {
            foreach (TreeNode node in _treeView.Nodes)
            {
                UpdateNodeCounts(node);
            }
        }

        private void UpdateNodeCounts(TreeNode node)
        {
            if (node.Tag is TreeNodeData data)
            {
                switch (data.Type)
                {
                    case NodeType.GlobalConfig:
                        node.Text = $"Global Config ({_project.GlobalConfig.Count} items)";
                        break;
                    case NodeType.Group:
                        var group = FindGroup(data.GroupId);
                        if (group != null) node.Text = group.Name;
                        break;
                    case NodeType.GroupSettings:
                        var gs = FindGroup(data.GroupId);
                        if (gs != null) node.Text = $"Settings ({gs.Settings.Count})";
                        break;
                    case NodeType.GroupMembers:
                        var gm = FindGroup(data.GroupId);
                        if (gm != null) node.Text = $"Members ({gm.MemberFileIds.Count})";
                        break;
                    case NodeType.Section:
                        var file = FindFile(data.FileId);
                        if (file != null)
                        {
                            var section = file.Sections.FirstOrDefault(s => s.Name == data.SectionName);
                            var displayName = string.IsNullOrEmpty(data.SectionName) ? "[root]" : $"[{data.SectionName}]";
                            node.Text = $"{displayName} ({section?.Items.Count ?? 0})";
                        }
                        break;
                }
            }
            foreach (TreeNode child in node.Nodes)
                UpdateNodeCounts(child);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            if (_isModified)
            {
                if (!ConfirmSave())
                    e.Cancel = true;
            }
        }

        #endregion
    }
}

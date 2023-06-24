using NikkeModManagerCore;
using NikkeModManagerCore.Exceptions;
using ProtoBuf.Meta;
using static System.ComponentModel.Design.ObjectSelectorEditor;
using static System.Net.Mime.MediaTypeNames;

namespace NikkeModManager {
    partial class MainLayout {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        readonly Color DISABLED_COLOR = Color.LightSalmon;
        readonly Color ENABLED_COLOR = Color.LimeGreen;
        readonly Color NEWLY_ENABLED_COLOR = Color.LightGreen;

        private string filter { get => FilterTextBox.Text; }

        private NikkeDataService _service;
        private Dictionary<TreeNode, NikkeBundle> _nodeBundles = new Dictionary<TreeNode, NikkeBundle>();
        private Dictionary<NikkeBundle, TreeNode> _bundleNodes = new Dictionary<NikkeBundle, TreeNode>();

        private ToolStripDropDownButton _enableAllDropDown;

        private WaitDialog _initalLoadDialog;

        Button MakeButton(string text, Action action) {
            var button = new Button() { Text = text };
            button.Click += (_, _) => BundleTree.ExpandAll();
            return button;
        }

        protected void Setup() {
            _service = new NikkeDataService();

            _service.Error += (text) => {
                MessageBox.Show(text, "Error", MessageBoxButtons.OK);
            };

            if (!_service.ValidateDataPath()) {
                string text = $"Unable to find Nikke game data directory at `{NikkeConfig.GameDataDirectory}`. This is typically found at `~\\AppData\\LocalLow\\com_proximabeta\\NIKKE\\eb`. ";
                text += "Please navigate to the Nikke game data directory (were you would normally place skin mods)";
                MessageBox.Show(text, "Error", MessageBoxButtons.OK);
                SetNikkeGameDataDirectory();
                if (!_service.ValidateDataPath()) {
                    MessageBox.Show("Unable to locate game data directory", "Error", MessageBoxButtons.OK);
                }
            }

            _service.PatchComplete += (bundles) => {
                MessageBox.Show($"Successfully patched {bundles.Count} files", "Patch Complete", MessageBoxButtons.OK);
                DataUpdated();
            };

            _initalLoadDialog = new WaitDialog();
            _initalLoadDialog.BringToFront();
            Task.Delay(1000).ContinueWith(t => Invoke(_initalLoadDialog.BringToFront));
            _initalLoadDialog.StartPosition = FormStartPosition.CenterScreen;
            _initalLoadDialog.Show();

            _service.LoadData();
            _service.DataUpdated += DataUpdated;
            _service.BundleEnabled += EnableBundle;


            #region Toolbar
            Logger.WriteLine("Setting up Toolbar");
            ToolStrip.Items.Add("Expand All").Click += (_, _) => {
                BundleTree.BeginUpdate();
                foreach (TreeNode node in BundleTree.Nodes) {
                    node.Expand();
                }
                BundleTree.EndUpdate();
            };
            ToolStrip.Items.Add("Collapse All").Click += (_, _) => {
                BundleTree.BeginUpdate();
                foreach (TreeNode node in BundleTree.Nodes) {
                    node.Collapse(true);
                }
                BundleTree.EndUpdate();
            };
            ToolStrip.Items.Add("Patch").Click += (_, _) => {
                Confirm($"Overwrite game files with {_service.GetChangedBundles().Count} selected files?", () => {
                    try {
                        _service.PatchGame();
                    } catch (GameDataNotFoundException) {
                        string text = $"Unable to find Nikke game data directory at `{NikkeConfig.GameDataDirectory}`. This if typically found at `~\\AppData\\LocalLow\\com_proximabeta\\NIKKE\\eb`. ";
                        text += "Please navigate to the Nikke game data directory (were you would normally place skin mods)";
                        MessageBox.Show(text, "Error", MessageBoxButtons.OK);
                        SetNikkeGameDataDirectory();
                        if (!_service.ValidateDataPath()) {
                            MessageBox.Show("Unable to locate game data directory", "Error", MessageBoxButtons.OK);
                        }
                    }
                });
            };
            _enableAllDropDown = new ToolStripDropDownButton("Enable All");
            ToolStrip.Items.Add(_enableAllDropDown);

            ToolStripDropDownButton miscDropdown = new ToolStripDropDownButton("Tools");
            ToolStrip.Items.Add(miscDropdown);
            miscDropdown.DropDownItems.Add("Set Nikke Data Directory").Click += (_, _) => {
                DialogResult confirmResult = MessageBox.Show("Please nagivate select the Nikke Game Data Directory (were you would normally place skin mods)", "Confirm", MessageBoxButtons.OK);
                SetNikkeGameDataDirectory();
            };
            miscDropdown.DropDownItems.Add("Clear Cache").Click += (_, _) => {
                Confirm("Are you sure you want to delete all existing cache files?\nThey'll be regenerated the next time the app is opened.", _service.DeleteAllCaches);
            };
            miscDropdown.DropDownItems.Add("Reload Data").Click += (_, _) => {
                Confirm("Reload all mod data?", _service.ReloadAllData);
            };
            miscDropdown.DropDownItems.Add("Delete Default Mod").Click += (_, _) => {
                Confirm("Delete default game mod?\nIt will be rebuilt the next time mod data is loaded.", _service.DeleteDefaultGameMod);
            };
            miscDropdown.DropDownItems.Add("Delete Default Bundles").Click += (_, _) => {
                Confirm("Delete all nikke bundle files in the game's eb folder?\nThis is generally done so they can be redownloaded the next time the game is run.", _service.DeleteGameNikkeBundles);
            };

            ToolStrip.Update();
            #endregion


            #region TreeView
            BundleTree.AfterSelect += (a, b) => {
                if (_nodeBundles.ContainsKey(b.Node)) SelectBundle(_nodeBundles[b.Node]);
            };

            BundleTree.DoubleClick += (a, b) => {
                if (BundleTree.SelectedNode != null && _nodeBundles.ContainsKey(BundleTree.SelectedNode))
                    _service.EnableBundle(_nodeBundles[BundleTree.SelectedNode]);
            };

            FilterTextBox.TextChanged += (_, _) => BuildTree();
            #endregion

        }

        void Confirm(string message, Action action) {
            DialogResult confirmResult = MessageBox.Show(message, "Confirm", MessageBoxButtons.YesNo);

            if (confirmResult == DialogResult.Yes) {
                action();
            }
        }

        void SetNikkeGameDataDirectory() {
            FolderBrowserDialog folderDialog = new FolderBrowserDialog();
            DialogResult result = folderDialog.ShowDialog();
            if (result == DialogResult.OK) {
                NikkeConfig.GameDataDirectory = folderDialog.SelectedPath;
            }
        }

        void DataUpdated() {
            PreviewEngine.PreviewBundle(_service.GetBundles().First());

            Invoke(() => {
                BuildTree();

                _enableAllDropDown.DropDownItems.Clear();
                foreach (var mod in _service.GetMods()) {
                    _enableAllDropDown.DropDownItems.Add(mod.Name).Click += (_, _) => {
                        mod.Bundles.ForEach(_service.EnableBundle);
                    };
                }

                _initalLoadDialog.Hide();
            });
        }

        void BuildTree() {
            BundleTree.BeginUpdate();
            BundleTree.Nodes.Clear();
            foreach (string characterId in _service.GetNikkesIds().OrderBy(id => NikkeDataHelper.GetName(id))) {
                string text = $"{NikkeDataHelper.GetName(characterId)} - {characterId}";
                if (filter != "" && !text.ToLower().Contains(filter.ToLower())) continue;
                TreeNode characterNode = BundleTree.Nodes.Add(text);
                foreach (int skin in _service.GetNikkeSkins(characterId)) {
                    TreeNode skinNode = characterNode.Nodes.Add($"Skin {skin}");
                    foreach (string pose in _service.GetNikkeSkinPoses(characterId, skin)) {
                        TreeNode poseNode = skinNode.Nodes.Add(pose);
                        foreach (NikkeBundle bundle in _service.GetNikkeSkinPoseBundles(characterId, skin, pose)) {
                            TreeNode bundleNode = poseNode.Nodes.Add(_service.GetBundleSource(bundle).Name);
                            _nodeBundles[bundleNode] = bundle;
                            _bundleNodes[bundle] = bundleNode;
                            EnableBundle(bundle, _service.IsEnabled(bundle));
                        }
                    }
                }
            }
            BundleTree.ExpandAll();
            foreach (TreeNode node in BundleTree.Nodes) {
                node.Collapse(true);
            }
            BundleTree.EndUpdate();
            if (BundleTree.Nodes.Count > 0)
                BundleTree.Nodes[0].EnsureVisible();
        }

        void SelectBundle(NikkeBundle bundle) {
            PreviewEngine.PreviewBundle(bundle);
            NikkeMod mod = _service.GetBundleSource(bundle);
            DescriptionList.BeginUpdate();
            DescriptionList.Items.Clear();
            DescriptionList.Items.Add($"Mod: {_service.GetBundleSource(bundle).Name}");
            DescriptionList.Items.Add($"Mod Location: {mod.ModPath}");
            DescriptionList.Items.Add($"Mod Author: {mod.Manifest.Author}");
            DescriptionList.Items.Add($"Mod Link: {mod.Manifest.Link}");
            DescriptionList.Items.Add($"Character Name: {bundle.Name}");
            DescriptionList.Items.Add($"Character Id: {bundle.CharacterId}");
            DescriptionList.Items.Add($"Character Skin: {bundle.SkinKey}");
            DescriptionList.Items.Add($"Character Pose: {bundle.Pose}");
            DescriptionList.Items.Add($"Character Animations:");
            bundle.Animations.ForEach(q => DescriptionList.Items.Add($"\t{q}"));
            DescriptionList.Items.Add($"File: {bundle.FileName}");
            DescriptionList.Items.Add($"Game Version: {mod.Manifest.GameVersion}");
            DescriptionList.Items.Add($"Mod Version: {mod.Manifest.ModVersion}");
            foreach (var pair in mod.Manifest.Data) {
                DescriptionList.Items.Add($"{pair.Key}: {pair.Value}");
            }
            DescriptionList.EndUpdate();
        }

        void EnableBundle(NikkeBundle bundle, bool enabled) {
            TreeNode node = _bundleNodes[bundle];
            node.Text = $"[{(enabled ? "X" : " ")}] {_service.GetBundleSource(bundle).Name}";
            node.BackColor = enabled ? (_service.GetChangedBundles().Contains(bundle) ? NEWLY_ENABLED_COLOR : ENABLED_COLOR) : DISABLED_COLOR;
        }

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing) {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() {
            ToolbarContentLayout = new TableLayoutPanel();
            BundlePreviewSplit = new SplitContainer();
            tableLayoutPanel1 = new TableLayoutPanel();
            BundleTree = new TreeView();
            FilterTextBox = new TextBox();
            PreviewDescSplit = new SplitContainer();
            PreviewEngine = new PreviewEngine.PreviewEngine();
            DescriptionList = new ListBox();
            ToolStrip = new ToolStrip();
            ToolbarContentLayout.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)BundlePreviewSplit).BeginInit();
            BundlePreviewSplit.Panel1.SuspendLayout();
            BundlePreviewSplit.Panel2.SuspendLayout();
            BundlePreviewSplit.SuspendLayout();
            tableLayoutPanel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)PreviewDescSplit).BeginInit();
            PreviewDescSplit.Panel1.SuspendLayout();
            PreviewDescSplit.Panel2.SuspendLayout();
            PreviewDescSplit.SuspendLayout();
            SuspendLayout();
            // 
            // ToolbarContentLayout
            // 
            ToolbarContentLayout.ColumnCount = 1;
            ToolbarContentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            ToolbarContentLayout.Controls.Add(BundlePreviewSplit, 0, 1);
            ToolbarContentLayout.Controls.Add(ToolStrip, 0, 0);
            ToolbarContentLayout.Dock = DockStyle.Fill;
            ToolbarContentLayout.Location = new Point(0, 0);
            ToolbarContentLayout.Name = "ToolbarContentLayout";
            ToolbarContentLayout.RowCount = 2;
            ToolbarContentLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 30F));
            ToolbarContentLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            ToolbarContentLayout.Size = new Size(784, 561);
            ToolbarContentLayout.TabIndex = 2;
            // 
            // BundlePreviewSplit
            // 
            BundlePreviewSplit.Dock = DockStyle.Fill;
            BundlePreviewSplit.Location = new Point(3, 33);
            BundlePreviewSplit.Name = "BundlePreviewSplit";
            // 
            // BundlePreviewSplit.Panel1
            // 
            BundlePreviewSplit.Panel1.Controls.Add(tableLayoutPanel1);
            // 
            // BundlePreviewSplit.Panel2
            // 
            BundlePreviewSplit.Panel2.Controls.Add(PreviewDescSplit);
            BundlePreviewSplit.Size = new Size(778, 525);
            BundlePreviewSplit.SplitterDistance = 400;
            BundlePreviewSplit.TabIndex = 1;
            // 
            // tableLayoutPanel1
            // 
            tableLayoutPanel1.ColumnCount = 1;
            tableLayoutPanel1.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tableLayoutPanel1.Controls.Add(BundleTree, 0, 1);
            tableLayoutPanel1.Controls.Add(FilterTextBox, 0, 0);
            tableLayoutPanel1.Dock = DockStyle.Fill;
            tableLayoutPanel1.Location = new Point(0, 0);
            tableLayoutPanel1.Name = "tableLayoutPanel1";
            tableLayoutPanel1.RowCount = 2;
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Absolute, 25F));
            tableLayoutPanel1.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tableLayoutPanel1.Size = new Size(400, 525);
            tableLayoutPanel1.TabIndex = 1;
            // 
            // BundleTree
            // 
            BundleTree.BackColor = SystemColors.ControlDark;
            BundleTree.Dock = DockStyle.Fill;
            BundleTree.Location = new Point(3, 28);
            BundleTree.Name = "BundleTree";
            BundleTree.Size = new Size(394, 494);
            BundleTree.TabIndex = 0;
            // 
            // FilterTextBox
            // 
            FilterTextBox.Dock = DockStyle.Fill;
            FilterTextBox.Location = new Point(3, 3);
            FilterTextBox.Name = "FilterTextBox";
            FilterTextBox.PlaceholderText = "Search...";
            FilterTextBox.Size = new Size(394, 23);
            FilterTextBox.TabIndex = 1;
            // 
            // PreviewDescSplit
            // 
            PreviewDescSplit.Dock = DockStyle.Fill;
            PreviewDescSplit.Location = new Point(0, 0);
            PreviewDescSplit.Name = "PreviewDescSplit";
            PreviewDescSplit.Orientation = Orientation.Horizontal;
            // 
            // PreviewDescSplit.Panel1
            // 
            PreviewDescSplit.Panel1.Controls.Add(PreviewEngine);
            // 
            // PreviewDescSplit.Panel2
            // 
            PreviewDescSplit.Panel2.Controls.Add(DescriptionList);
            PreviewDescSplit.Size = new Size(374, 525);
            PreviewDescSplit.SplitterDistance = 400;
            PreviewDescSplit.TabIndex = 0;
            // 
            // PreviewEngine
            // 
            PreviewEngine.Dock = DockStyle.Fill;
            PreviewEngine.Location = new Point(0, 0);
            PreviewEngine.MouseHoverUpdatesOnly = false;
            PreviewEngine.Name = "PreviewEngine";
            PreviewEngine.Size = new Size(374, 400);
            PreviewEngine.TabIndex = 0;
            PreviewEngine.Text = "previewEngine1";
            // 
            // DescriptionList
            // 
            DescriptionList.BackColor = SystemColors.ControlDark;
            DescriptionList.Dock = DockStyle.Fill;
            DescriptionList.FormattingEnabled = true;
            DescriptionList.ItemHeight = 15;
            DescriptionList.Location = new Point(0, 0);
            DescriptionList.Name = "DescriptionList";
            DescriptionList.Size = new Size(374, 121);
            DescriptionList.TabIndex = 0;
            // 
            // ToolStrip
            // 
            ToolStrip.Dock = DockStyle.Fill;
            ToolStrip.Location = new Point(0, 0);
            ToolStrip.Name = "ToolStrip";
            ToolStrip.Size = new Size(784, 30);
            ToolStrip.TabIndex = 2;
            ToolStrip.Text = "toolStrip1";
            // 
            // MainLayout
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(784, 561);
            Controls.Add(ToolbarContentLayout);
            Name = "MainLayout";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Nikke Mod Manager";
            Load += MainLayout_Load;
            ToolbarContentLayout.ResumeLayout(false);
            ToolbarContentLayout.PerformLayout();
            BundlePreviewSplit.Panel1.ResumeLayout(false);
            BundlePreviewSplit.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)BundlePreviewSplit).EndInit();
            BundlePreviewSplit.ResumeLayout(false);
            tableLayoutPanel1.ResumeLayout(false);
            tableLayoutPanel1.PerformLayout();
            PreviewDescSplit.Panel1.ResumeLayout(false);
            PreviewDescSplit.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)PreviewDescSplit).EndInit();
            PreviewDescSplit.ResumeLayout(false);
            ResumeLayout(false);
        }

        #endregion

        private SplitContainer BundlePreviewSplit;
        private SplitContainer PreviewDescSplit;
        private TableLayoutPanel ToolbarContentLayout;
        private SplitContainer splitContainer1;
        private SplitContainer splitContainer2;
        private PreviewEngine.PreviewEngine PreviewEngine;
        private ListBox DescriptionList;
        private ToolStrip ToolStrip;
        private TableLayoutPanel tableLayoutPanel1;
        private TreeView BundleTree;
        private TextBox FilterTextBox;
    }
}
namespace NikkeModManager {
    public partial class MainLayout : Form {
        public MainLayout() {
            InitializeComponent();
        }

        private void BundlePreviewSplit_SplitterMoved(object sender, SplitterEventArgs e) {
        }

        private void MainLayout_Load(object sender, EventArgs e) {
            Setup();
        }
    }
}
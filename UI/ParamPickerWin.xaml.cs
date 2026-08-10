using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace HPDTool.UI
{
    public partial class ParamPickerWin : Window
    {
        public string EastingParameter { get; private set; }
        public string NorthingParameter { get; private set; }
        public string ElevationParameter { get; private set; }

        public ParamPickerWin(IEnumerable<string> candidateParams)
        {
            InitializeComponent();

            var items = candidateParams?.Distinct().OrderBy(n => n).ToList() ?? new List<string>();

            EastingCombo.ItemsSource = items;
            NorthingCombo.ItemsSource = items;
            ElevationCombo.ItemsSource = items;

            var lower = items.Select(n => new { n, l = n.ToLowerInvariant() }).ToList();

            var preferredE = lower.FirstOrDefault(x => x.l.Contains("east"))?.n;
            var preferredN = lower.FirstOrDefault(x => x.l.Contains("north"))?.n;
            var preferredZ =
                lower.FirstOrDefault(x => x.l.Contains("elev"))?.n ??
                lower.FirstOrDefault(x => x.l.Equals("z"))?.n ??
                lower.FirstOrDefault(x => x.l.Contains("level"))?.n;

            if (!string.IsNullOrEmpty(preferredE)) EastingCombo.Text = preferredE;
            if (!string.IsNullOrEmpty(preferredN)) NorthingCombo.Text = preferredN;
            if (!string.IsNullOrEmpty(preferredZ)) ElevationCombo.Text = preferredZ;
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            EastingParameter = EastingCombo.Text?.Trim();
            NorthingParameter = NorthingCombo.Text?.Trim();
            ElevationParameter = ElevationCombo.Text?.Trim();

            if (string.IsNullOrWhiteSpace(EastingParameter) ||
                string.IsNullOrWhiteSpace(NorthingParameter) ||
                string.IsNullOrWhiteSpace(ElevationParameter))
            {
                MessageBox.Show("Please provide parameter names for Easting, Northing, and Elevation.", "Missing Input", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}

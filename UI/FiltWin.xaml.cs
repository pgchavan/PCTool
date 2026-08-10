using Autodesk.Revit.DB;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace HPDTool.UI
{
    public partial class FiltWin : Window
    {
        private Document _doc;
        private List<ParameterFilterElement> _filtersToPurge = new List<ParameterFilterElement>();

        public FiltWin(Document doc)
        {
            InitializeComponent();
            _doc = doc;
            LoadUnusedFilters();
        }

        private void LoadUnusedFilters()
        {
            var allFilters = new FilteredElementCollector(_doc)
                .OfClass(typeof(ParameterFilterElement))
                .Cast<ParameterFilterElement>()
                .OrderBy(f => f.Name)
                .ToList();

            var allViews = new FilteredElementCollector(_doc)
                .OfClass(typeof(View))
                .Cast<View>()
                .Where(v =>
                    !v.IsTemplate &&
                    v.ViewType != ViewType.ProjectBrowser &&
                    v.ViewType != ViewType.Legend &&
                    v.ViewType != ViewType.Schedule &&
                    v.ViewType != ViewType.DrawingSheet &&
                    v.AreGraphicsOverridesAllowed())
                .ToList();

            var usedFilterIds = allViews
                .SelectMany(v => v.GetFilters())
                .Distinct()
                .ToHashSet();

            var unusedFilters = allFilters
                .Where(f => !usedFilterIds.Contains(f.Id))
                .OrderBy(f => f.Name)
                .ToList();

            FilterListBox.ItemsSource = unusedFilters;
            FilterListBox.DisplayMemberPath = "Name";

            if (unusedFilters.Count == 0)
            {
                MessageBox.Show("No unused view filters found.");
                this.DialogResult = false; // Close the window indicating cancellation
            }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            _filtersToPurge = FilterListBox.SelectedItems.Cast<ParameterFilterElement>().ToList();

            if (_filtersToPurge.Count > 0)
            {
                this.DialogResult = true; // Indicate OK was clicked
            }
            else
            {
                MessageBox.Show("Please select at least one filter to purge.", "Purge Filters", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false; // Indicate Cancel was clicked
        }

        public List<ParameterFilterElement> FiltersToPurge => _filtersToPurge;
    }
}
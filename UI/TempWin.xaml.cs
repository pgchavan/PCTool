using Autodesk.Revit.DB;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace HPDTool.UI
{
    public partial class TempWin : Window
    {
        private Document _doc;
        private ObservableCollection<ViewTemplateViewModel> _viewTemplates;

        public TempWin(Document doc, List<View> viewTemplates)
        {
            InitializeComponent();
            _doc = doc;
            _viewTemplates = new ObservableCollection<ViewTemplateViewModel>(
                viewTemplates.Select(vt => new ViewTemplateViewModel(vt))
            );
            TemplatesListBox.ItemsSource = _viewTemplates;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            List<View> templatesToPurge = TemplatesListBox.SelectedItems.Cast<ViewTemplateViewModel>()
                .Select(vm => vm.ViewTemplate)
                .ToList();

            if (templatesToPurge.Any())
            {
                this.DataContext = templatesToPurge; // Set DataContext to pass selected templates back
                this.DialogResult = true;
            }
            else
            {
                MessageBox.Show("Please select at least one view template to purge.", "Purge View Templates", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }

    // Helper ViewModel to bind the ListBox to view template names
    public class ViewTemplateViewModel
    {
        public View ViewTemplate { get; }
        public string Name => ViewTemplate.Name;

        public ViewTemplateViewModel(View viewTemplate)
        {
            ViewTemplate = viewTemplate;
        }
    }
}
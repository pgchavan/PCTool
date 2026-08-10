using Autodesk.Revit.DB;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace HPDTool.UI
{
    public partial class ParametersToCopyWindow : Window
    {
        public List<Parameter> SelectedParameters { get; private set; }

        public ParametersToCopyWindow(IList<Parameter> parameters)
        {
            InitializeComponent();
            lstParameters.ItemsSource = parameters;
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            SelectedParameters = lstParameters.SelectedItems
                                             .Cast<Parameter>()
                                             .ToList();

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

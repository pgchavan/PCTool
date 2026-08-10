using Autodesk.Revit.DB;
using Microsoft.Win32;
using System.Collections.Generic;
using System.Windows;

namespace HPDTool.UI
{
    public partial class LinkModelsWindow : Window
    {
        public List<string> SelectedFiles { get; private set; }
        public ImportPlacement SelectedPlacement { get; private set; }

        public LinkModelsWindow()
        {
            InitializeComponent();
            SelectedFiles = new List<string>();
        }

        private void Browse_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog
            {
                Filter = "Revit Files (*.rvt)|*.rvt",
                Multiselect = true
            };

            if (dialog.ShowDialog() == true)
            {
                FilesList.Items.Clear();
                foreach (string file in dialog.FileNames)
                    FilesList.Items.Add(file);
            }
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            SelectedFiles.Clear();
            foreach (var item in FilesList.Items)
                SelectedFiles.Add(item.ToString());

            switch (PlacementCombo.SelectedIndex)
            {
                case 0:
                    SelectedPlacement = ImportPlacement.Shared;
                    break;
                case 1:
                    SelectedPlacement = ImportPlacement.Origin;
                    break;
                case 2:
                    SelectedPlacement = ImportPlacement.Centered;
                    break;
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

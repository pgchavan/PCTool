using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace HPDTool.UI
{
    public partial class FamilySelectionWindow : Window
    {
        public List<FamilyItem> FamilyItems { get; }

        private List<FamilyItem> _allFamilies;

        public FamilySelectionWindow(List<FamilyItem> families)
        {
            InitializeComponent();

            _allFamilies = families;
            FamilyItems = families;

            FamilyList.ItemsSource = FamilyItems;
        }

        // SEARCH FILTER
        private void SearchBox_TextChanged(object sender, RoutedEventArgs e)
        {
            string filter = SearchBox.Text.Trim().ToLower();

            if (string.IsNullOrEmpty(filter))
            {
                FamilyList.ItemsSource = _allFamilies;
            }
            else
            {
                FamilyList.ItemsSource = _allFamilies
                    .Where(f => f.Name.ToLower().Contains(filter))
                    .ToList();
            }
        }

        // SELECT ALL
        private void SelectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var f in _allFamilies)
                f.IsSelected = true;

            FamilyList.Items.Refresh();
        }

        // SELECT NONE
        private void SelectNone_Click(object sender, RoutedEventArgs e)
        {
            foreach (var f in _allFamilies)
                f.IsSelected = false;

            FamilyList.Items.Refresh();
        }

        // OK
        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        // CANCEL
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}

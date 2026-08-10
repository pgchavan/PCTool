using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace HPDTool.UI
{
    public partial class CategoryPickerWin : Window
    {
        public string SelectedCategoryName { get; private set; }

        public CategoryPickerWin(IEnumerable<string> categoryNames)
        {
            InitializeComponent();
            CategoryList.ItemsSource = categoryNames?.ToList() ?? new List<string>();
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            SelectedCategoryName = CategoryList.SelectedItem as string;
            DialogResult = !string.IsNullOrWhiteSpace(SelectedCategoryName);
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}

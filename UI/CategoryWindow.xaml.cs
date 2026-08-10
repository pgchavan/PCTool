using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace HPDTool.UI
{
    public partial class CategoryWindow : Window
    {
        private readonly Document _doc;

        public bool IsAccepted = false;
        public List<Category> SelectedCategories = new List<Category>();

        public CategoryWindow(Document doc)
        {
            InitializeComponent();
            _doc = doc;

            // Load all model categories
            var cats = _doc.Settings.Categories
                .Cast<Category>()
                .Where(c => c.CategoryType == CategoryType.Model)
                .OrderBy(c => c.Name)
                .ToList();

            CategoryList.ItemsSource = cats;
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            SelectedCategories = FindVisualChildren<CheckBox>(CategoryList)
                .Where(cb => cb.IsChecked == true)
                .Select(cb => cb.Tag as Category)
                .Where(c => c != null)
                .ToList();

            if (!SelectedCategories.Any())
            {
                MessageBox.Show("Please select at least one category.");
                return;
            }

            IsAccepted = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        // Helper to find checkboxes inside ItemsControl
        private void SelectAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in CategoryList.Items)
            {
                var container = CategoryList.ItemContainerGenerator.ContainerFromItem(item) as FrameworkElement;
                if (container != null)
                {
                    var checkbox = container.FindName("checkBox") as CheckBox;
                }
            }

            foreach (CheckBox cb in FindVisualChildren<CheckBox>(CategoryList))
            {
                cb.IsChecked = true;
            }
        }

        private void SelectNone_Click(object sender, RoutedEventArgs e)
        {
            foreach (CheckBox cb in FindVisualChildren<CheckBox>(CategoryList))
            {
                cb.IsChecked = false;
            }
        }

        // Helper function
        public static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);

                    if (child != null && child is T t)
                    {
                        yield return t;
                    }

                    foreach (T childOfChild in FindVisualChildren<T>(child))
                    {
                        yield return childOfChild;
                    }
                }
            }
        }
    }
}

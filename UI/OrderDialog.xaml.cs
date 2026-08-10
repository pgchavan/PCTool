using Autodesk.Revit.DB;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace HPDTool.UI
{
    public partial class OrderDialog : Window
    {
        public string SelectedParameter => ParamList.SelectedItem as string ?? string.Empty;
        public int StartNumber => int.Parse(StartBox.Text);
        public int Increment => int.Parse(IncBox.Text);
        public string Prefix => PrefixBox.Text;
        public string Suffix => SuffixBox.Text;

        public OrderDialog(Document doc, List<Element> elements)
        {
            InitializeComponent();

            // Gather available parameters
            HashSet<string> names = new HashSet<string>();

            foreach (Element e in elements)
            {
                foreach (Parameter p in e.Parameters)
                {
                    if (!p.IsReadOnly)
                    {
                        names.Add(p.Definition.Name);
                    }
                }
            }

            ParamList.ItemsSource = names.OrderBy(x => x).ToList();
            if (ParamList.Items.Count > 0)
                ParamList.SelectedIndex = 0;
        }

        private void OK_Click(object sender, RoutedEventArgs e)
        {
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

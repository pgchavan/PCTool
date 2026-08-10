using System;
using System.Linq;
using System.Windows;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace HPDTool.UI
{
    public partial class MainWindow : Window
    {
        private UIDocument _uiDoc;
        private Document _doc;

        public MainWindow(UIDocument uiDoc)
        {
            InitializeComponent();
            _uiDoc = uiDoc;
            _doc = _uiDoc.Document;
            

            if (!SelectedElements.Any())
            {
                MessageBox.Show("Please select elements to rotate.", "HPD Tool - No Selection", MessageBoxButton.OK, MessageBoxImage.Warning);
                Close();
            }
            else
            {
                ShowDialog();
            }
        }

        public System.Collections.Generic.List<Element> SelectedElements
        {
            get { return _uiDoc.Selection.GetElementIds().Select(id => _doc.GetElement(id)).ToList(); }
        }

        public double Degrees
        {
            get
            {
                if (double.TryParse(angleTextBox.Text, out double degrees))
                {
                    return degrees;
                }
                return 0;
            }
        }

        private void button_close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        // You'll need to add a reference to System.Diagnostics for this
        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            System.Diagnostics.Process.Start(e.Uri.AbsoluteUri);
        }

        private void header_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void button_run_Click(object sender, RoutedEventArgs e)
        {
            Rotate();
            Close();
        }

        private void Rotate()
        {
            using (Transaction trans = new Transaction(_doc, "Rotate Elements"))
            {
                trans.Start();
                foreach (Element element in SelectedElements)
                {
                    try
                    {
                        // Get Center Point
                        BoundingBoxXYZ boundingBox = element.get_BoundingBox(_doc.ActiveView);
                        XYZ point = (boundingBox.Min + boundingBox.Max) / 2;

                        // Create Vertical Axis Line
                        Autodesk.Revit.DB.Line axisLine = Autodesk.Revit.DB.Line.CreateBound(point, point + XYZ.BasisZ);

                        // Rotate
                        ElementTransformUtils.RotateElement(_doc, element.Id, axisLine, Math.PI / 180 * Degrees);
                    }
                    catch (Exception ex)
                    {
                        TaskDialog.Show("Error", $"Could not rotate element - {element.Id}: {ex.Message}");
                    }
                }
                trans.Commit();
            }
        }
    }
}
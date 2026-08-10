using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace HPDTool.UI
{
    public partial class ParameterSelectionWindow : Window
    {
        private readonly Document _doc;
        private readonly UIDocument _uiDoc;
        private readonly List<Element> _elements;

        private List<string> _allParameters = new List<string>();
        private List<string> _allValues = new List<string>();

        public ParameterSelectionWindow(Document doc, UIDocument uiDoc)
        {
            InitializeComponent();

            _doc = doc;
            _uiDoc = uiDoc;

            _elements = new FilteredElementCollector(_doc)
                .WhereElementIsNotElementType()
                .ToList();

            LoadParameters();
        }

        // ---------------- LOAD PARAMETERS ----------------

        private void LoadParameters()
        {
            HashSet<string> names = new HashSet<string>();

            foreach (Element el in _elements)
            {
                foreach (Parameter p in el.Parameters)
                {
                    if (p.Definition != null)
                        names.Add(p.Definition.Name);
                }
            }

            _allParameters = names.OrderBy(x => x).ToList();
            cmbParameters.ItemsSource = _allParameters;
        }

        // ---------------- PARAMETER SEARCH ----------------

        private void cmbParameters_KeyUp(object sender, KeyEventArgs e)
        {
            if (!_allParameters.Any())
                return;

            string text = cmbParameters.Text;

            cmbParameters.ItemsSource = _allParameters
                .Where(p => p.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();

            cmbParameters.IsDropDownOpen = true;
        }

        // ---------------- LOAD VALUES ----------------

        private void cmbParameters_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            cmbValues.ItemsSource = null;
            _allValues.Clear();

            string paramName = cmbParameters.SelectedItem as string;
            if (string.IsNullOrEmpty(paramName))
                return;

            HashSet<string> values = new HashSet<string>();

            foreach (Element el in _elements)
            {
                Parameter p = el.LookupParameter(paramName);
                if (p == null || !p.HasValue)
                    continue;

                string val = GetValueAsString(p);
                if (!string.IsNullOrEmpty(val))
                    values.Add(val);
            }

            _allValues = values.OrderBy(x => x).ToList();
            cmbValues.ItemsSource = _allValues;
        }

        // ---------------- VALUE SEARCH ----------------

        private void cmbValues_KeyUp(object sender, KeyEventArgs e)
        {
            if (!_allValues.Any())
                return;

            string text = cmbValues.Text;

            cmbValues.ItemsSource = _allValues
                .Where(v => v.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();

            cmbValues.IsDropDownOpen = true;
        }

        // ---------------- PARAM VALUE STRING ----------------

        private string GetValueAsString(Parameter p)
        {
            switch (p.StorageType)
            {
                case StorageType.String:
                    return p.AsString();

                case StorageType.Integer:
                    return p.AsInteger().ToString();

                case StorageType.Double:
                    return p.AsValueString();

                case StorageType.ElementId:
                    Element el = _doc.GetElement(p.AsElementId());
                    return el?.Name ?? p.AsElementId().IntegerValue.ToString();

                default:
                    return null;
            }
        }

        // ---------------- SELECT ELEMENTS ----------------

        private void Select_Click(object sender, RoutedEventArgs e)
        {
            string paramName = cmbParameters.Text?.Trim();
            string value = cmbValues.Text?.Trim();

            if (string.IsNullOrEmpty(paramName) || string.IsNullOrEmpty(value))
            {
                MessageBox.Show("Please select parameter and value.");
                return;
            }

            List<ElementId> ids = new List<ElementId>();

            foreach (Element el in _elements)
            {
                Parameter p = el.LookupParameter(paramName);
                if (p == null || !p.HasValue)
                    continue;

                bool match = false;

                switch (p.StorageType)
                {
                    case StorageType.String:
                        match = string.Equals(p.AsString(), value, StringComparison.OrdinalIgnoreCase);
                        break;

                    case StorageType.Integer:
                        match = p.AsInteger().ToString() == value;
                        break;

                    case StorageType.Double:
                        match = string.Equals(p.AsValueString(), value, StringComparison.OrdinalIgnoreCase);
                        break;

                    case StorageType.ElementId:
                        Element refEl = _doc.GetElement(p.AsElementId());
                        match = string.Equals(refEl?.Name, value, StringComparison.OrdinalIgnoreCase);
                        break;
                }

                if (match)
                    ids.Add(el.Id);
            }

            if (ids.Any())
            {
                _uiDoc.Selection.SetElementIds(ids);
                MessageBox.Show($"{ids.Count} elements selected.");
            }
            else
            {
                MessageBox.Show("No matching elements found.");
            }

            Close();
        }
    }
}

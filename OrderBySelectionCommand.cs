using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System.Collections.Generic;

namespace HPDTool
{
    [Transaction(TransactionMode.Manual)]
    public class OrderBySelectionCommand : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;

            List<Element> ordered = new List<Element>();

            // -------------------------
            // TRUE SELECTION ORDER LOOP
            // -------------------------
            while (true)
            {
                try
                {
                    Reference r = uidoc.Selection.PickObject(
                        ObjectType.Element,
                        "Click elements in order — press ESC to finish"
                    );

                    ordered.Add(doc.GetElement(r));
                }
                catch
                {
                    break; // ESC ends loop
                }
            }

            if (ordered.Count == 0)
            {
                TaskDialog.Show("Info", "No elements selected.");
                return Result.Cancelled;
            }

            // -------------------------
            // SHOW WPF DIALOG
            // -------------------------
            UI.OrderDialog dlg = new UI.OrderDialog(doc, ordered);
            bool? result = dlg.ShowDialog();

            if (result != true)
                return Result.Cancelled;

            string paramName = dlg.SelectedParameter;
            int start = dlg.StartNumber;
            int inc = dlg.Increment;
            string prefix = dlg.Prefix;
            string suffix = dlg.Suffix;

            using (Transaction t = new Transaction(doc, "Apply Order"))
            {
                t.Start();

                int current = start;

                foreach (Element e in ordered)
                {
                    Parameter p = e.LookupParameter(paramName);

                    if (p != null && !p.IsReadOnly)
                    {
                        string value = prefix + current.ToString() + suffix;

                        if (p.StorageType == StorageType.String)
                            p.Set(value);
                        else if (p.StorageType == StorageType.Integer)
                            p.Set(current);
                        else if (p.StorageType == StorageType.Double)
                            p.Set(current);
                    }

                    current += inc;
                }

                t.Commit();
            }

            TaskDialog.Show("Done", "Values applied successfully.");
            return Result.Succeeded;
        }
    }
}
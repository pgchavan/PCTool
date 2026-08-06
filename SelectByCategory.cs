using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using HPDTool.UI;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HPDTool
{
    // ---------- FILTER: MULTI-CATEGORY FOR ACTIVE DOCUMENT ELEMENTS ----------
    public class MultiCategoryFilter : ISelectionFilter
    {
        private readonly HashSet<int> _catIds;

        public MultiCategoryFilter(List<Category> categories)
        {
            _catIds = categories
                .Select(c => c.Id.IntegerValue)
                .ToHashSet();
        }

        public bool AllowElement(Element elem)
        {
            return elem.Category != null &&
                   _catIds.Contains(elem.Category.Id.IntegerValue);
        }

        public bool AllowReference(Reference reference, XYZ point)
        {
            return true; // Not used for active doc filtering
        }
    }

    // ---------- MAIN COMMAND ----------
    [Transaction(TransactionMode.Manual)]
    public class SelectByCategory : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;

            // Show multi‑category selection window
            CategoryWindow win = new CategoryWindow(doc);
            win.ShowDialog();
            if (!win.IsAccepted)
                return Result.Cancelled;

            List<Category> selectedCategories = win.SelectedCategories;

            try
            {
                // Select elements from ACTIVE PROJECT ONLY
                IList<Reference> pickedRefs = uidoc.Selection.PickObjects(
                    ObjectType.Element,
                    new MultiCategoryFilter(selectedCategories),
                    "Select elements (multi‑category)"
                );

                // Retrieve actual elements from selection
                List<Element> selectedElems = new List<Element>();

                foreach (Reference r in pickedRefs)
                {
                    Element e = doc.GetElement(r.ElementId);
                    if (e != null)
                        selectedElems.Add(e);
                }

                // Hold the selection
                uidoc.Selection.SetElementIds(
                    selectedElems.Select(e => e.Id).ToList()
                );

                TaskDialog.Show("Selection Held",
                    $"You selected {selectedElems.Count} elements.\n" +
                    "Revit selection is now held.");

                return Result.Succeeded;
            }
            catch
            {
                return Result.Cancelled;
            }
        }
    }
}
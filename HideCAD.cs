using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace HPDTool
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class HideCAD : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uiDocument = commandData.Application.ActiveUIDocument;
            Document document = uiDocument.Document;
            var activeView = document.ActiveView;

            // Ask the user whether to hide or unhide
            DialogResult result = MessageBox.Show("Do you want to HIDE CAD links?", "HPD Tool - CAD Link Visibility", MessageBoxButtons.YesNoCancel);

            if (result == DialogResult.Cancel)
            {
                return Result.Cancelled;
            }

            bool hide = (result == DialogResult.Yes);

            // Filter for CAD Link Instances (ImportInstance)
            FilteredElementCollector collector = new FilteredElementCollector(document)
                .OfClass(typeof(ImportInstance));


            List<ElementId> cadLinkIds = new List<ElementId>();

            foreach (ImportInstance import in collector)
            {
                // Optional: filter only CAD links (DWGs)
                ElementType type = document.GetElement(import.GetTypeId()) as ElementType;
                if (type != null && type.Name.EndsWith(".dwg", StringComparison.OrdinalIgnoreCase))
                {
                    cadLinkIds.Add(import.Id);
                }
            }

            if (cadLinkIds.Count == 0)
            {
                MessageBox.Show("No CAD links found in the current view.", "HPD Tool - CAD Link Visibility");
                return Result.Succeeded;
            }

            using (Transaction tx = new Transaction(document, hide ? "Hide CAD Links" : "Unhide CAD Links"))
            {
                tx.Start();

                if (hide)
                {
                    activeView.HideElements(cadLinkIds);
                }
                else
                {
                    activeView.UnhideElements(cadLinkIds);
                }

                tx.Commit();
            }

            return Result.Succeeded;
        }
    }
}
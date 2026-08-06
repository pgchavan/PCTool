using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System.Collections.Generic;

namespace HPDTool
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class MatchElements : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;

            Element sourceElement = null;
            Element targetElement = null;

            // Select the source element
            try
            {
                Reference sourceRef = uidoc.Selection.PickObject(ObjectType.Element, "Select the element to copy override properties from.");
                sourceElement = doc.GetElement(sourceRef);
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                return Result.Cancelled;
            }

            if (sourceElement == null)
            {
                message = "No source element selected.";
                return Result.Failed;
            }

            // Select the target element(s)
            IList<Reference> targetRefs = null;
            try
            {
                targetRefs = uidoc.Selection.PickObjects(ObjectType.Element, "Select the element(s) to apply override properties to.");
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                return Result.Cancelled;
            }

            if (targetRefs == null || targetRefs.Count == 0)
            {
                message = "No target elements selected.";
                return Result.Cancelled;
            }

            using (Transaction trans = new Transaction(doc, "Override Element Properties"))
            {
                trans.Start();

                foreach (Reference targetRef in targetRefs)
                {
                    targetElement = doc.GetElement(targetRef);
                    if (targetElement != null)
                    {
                        // Get the graphic overrides for the source element in the active view
                        OverrideGraphicSettings sourceOverrides = doc.ActiveView.GetElementOverrides(sourceElement.Id);

                        // Apply these overrides to the target element in the active view
                        doc.ActiveView.SetElementOverrides(targetElement.Id, sourceOverrides);
                    }
                }

                trans.Commit();
            }

            TaskDialog.Show("Success", $"Override properties applied to {targetRefs.Count} element(s).");

            return Result.Succeeded;
        }
    }
}
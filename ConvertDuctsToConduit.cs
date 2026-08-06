using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.DB.Electrical;
using Autodesk.Revit.UI.Selection;
using Autodesk.Revit.Attributes;
using System.Collections.Generic;
using System.Linq;

namespace HPDTool
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class ConvertDuctsToConduit : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            UIDocument uiDoc = commandData.Application.ActiveUIDocument;
            Document doc = uiDoc.Document;

            // STEP 1: Pick an existing conduit to get the required ConduitType
            Reference conduitRef;
            try
            {
                conduitRef = uiDoc.Selection.PickObject(
                    ObjectType.Element,
                    new ConduitOnlyFilter(),
                    "Select a conduit with the required Routing Preferences");
            }
            catch
            {
                return Result.Cancelled;
            }

            Conduit seedConduit = doc.GetElement(conduitRef) as Conduit;
            if (seedConduit == null)
            {
                message = "Selected element is not a conduit.";
                return Result.Failed;
            }

            ConduitType conduitType =
                doc.GetElement(seedConduit.GetTypeId()) as ConduitType;

            if (conduitType == null)
            {
                message = "Unable to determine Conduit Type.";
                return Result.Failed;
            }

            // STEP 2: Pick ducts (fittings are intentionally ignored)
            IList<Reference> picked;
            try
            {
                picked = uiDoc.Selection.PickObjects(
                    ObjectType.Element,
                    new DuctOnlyFilter(),
                    "Select ducts to convert to conduits");
            }
            catch
            {
                return Result.Cancelled;
            }

            using (Transaction tx = new Transaction(doc, "Convert Ducts to Conduits"))
            {
                tx.Start();

                foreach (Reference r in picked)
                {
                    Duct duct = doc.GetElement(r) as Duct;
                    if (duct == null) continue;

                    LocationCurve lc = duct.Location as LocationCurve;
                    if (lc == null) continue;

                    Level level = doc.GetElement(duct.LevelId) as Level;
                    if (level == null) continue;

                    Conduit conduit = Conduit.Create(
                        doc,
                        conduitType.Id,
                        lc.Curve.GetEndPoint(0),
                        lc.Curve.GetEndPoint(1),
                        level.Id);

                    // Copy diameter (round ducts only)
                    Parameter ductDia =
                        duct.get_Parameter(BuiltInParameter.RBS_CURVE_DIAMETER_PARAM);

                    Parameter conduitDia =
                        conduit.get_Parameter(BuiltInParameter.RBS_CONDUIT_DIAMETER_PARAM);

                    if (ductDia != null &&
                        conduitDia != null &&
                        !conduitDia.IsReadOnly)
                    {
                        conduitDia.Set(ductDia.AsDouble());
                    }

                    doc.Delete(duct.Id);
                }

                tx.Commit();
            }

            // STEP 3: Inform user how to generate fittings correctly
            TaskDialog.Show(
                "Conversion Complete",
                "Ducts were converted to conduits successfully.\n\n" +
                "To generate conduit fittings using Routing Preferences:\n" +
                "• Use Trim / Extend Conduit\n" +
                "• Use Route Conduit\n\n" +
                "This is the only Revit-supported way to apply conduit fittings.");

            return Result.Succeeded;
        }
    }
    class ConduitOnlyFilter : ISelectionFilter
    {
        public bool AllowElement(Element e)
        {
            return e.Category != null &&
                   e.Category.Id.IntegerValue ==
                   (int)BuiltInCategory.OST_Conduit;
        }

        public bool AllowReference(Reference r, XYZ p) => false;
    }

    class DuctOnlyFilter : ISelectionFilter
    {
        public bool AllowElement(Element e)
        {
            return e.Category != null &&
                   e.Category.Id.IntegerValue ==
                   (int)BuiltInCategory.OST_DuctCurves;
        }

        public bool AllowReference(Reference r, XYZ p) => false;
    }
}

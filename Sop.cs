using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using HPDTool.UI;

namespace HPDTool
{
    [Transaction(TransactionMode.Manual)]
    public class Sop : IExternalCommand
    {
        private enum UnitsKind { Meters, Millimeters }
        private enum ReferenceKind { SurveyPoint, ProjectBasePoint }

        private const double FEET_TO_METERS = 0.3048;
        private const double FEET_TO_MM = 304.8;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;

            // 1) Collect categories with placeable elements

            var allElems = new FilteredElementCollector(doc)
                .WhereElementIsNotElementType()
                .ToElements()
                .Where(e => HasUsableLocation(e))
                .ToList();

            // 1a) Safely read category name once per element (skips null categories)
            var elemsWithCatName = allElems
                .Select(e => new { Element = e, CatName = e.Category?.Name })
                .Where(x => !string.IsNullOrWhiteSpace(x.CatName))
                .ToList();

            // 1b) Build a distinct, case-insensitive, sorted list of category names
            var categoryNames = elemsWithCatName
                .Select(x => x.CatName)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (categoryNames.Count == 0)
            {
                TaskDialog.Show("No Elements", "No placeable elements with a valid location found.");
                return Result.Cancelled;
            }

            // Show picker with unique category names (by string)
            var catPicker = new CategoryPickerWin(categoryNames);
            bool? catRes = catPicker.ShowDialog();
            if (catRes != true || string.IsNullOrWhiteSpace(catPicker.SelectedCategoryName))
                return Result.Cancelled;

            string selectedCategoryName = catPicker.SelectedCategoryName;

            // 2) Collect all elements whose Category.Name == selected name (case-insensitive)
            var elems = elemsWithCatName
                .Where(x => x.CatName.Equals(selectedCategoryName, StringComparison.OrdinalIgnoreCase))
                .Select(x => x.Element)
                .ToList();

            if (elems.Count == 0)
            {
                TaskDialog.Show("No Instances", $"No elements in category '{selectedCategoryName}' found.");
                return Result.Cancelled;
            }


            if (elems.Count == 0)
            {
                TaskDialog.Show("No Instances", $"No elements in category '{selectedCategoryName}' found.");
                return Result.Cancelled;
            }

            // 3) Ask reference kind
            ReferenceKind refKind = AskReferenceKind();
            if ((int)refKind == -1) return Result.Cancelled;

            // 4) Ask units (for string params)
            UnitsKind outUnits = AskUnitsKind();
            if ((int)outUnits == -1) return Result.Cancelled;

            // 5) Ask which parameters to fill (E, N, Z)
            var candidateParams = CollectWritableParamNames(elems, maxScan: 20);

            var paramPicker = new ParamPickerWin(candidateParams);
            bool? paramRes = paramPicker.ShowDialog();
            if (paramRes != true ||
                string.IsNullOrWhiteSpace(paramPicker.EastingParameter) ||
                string.IsNullOrWhiteSpace(paramPicker.NorthingParameter) ||
                string.IsNullOrWhiteSpace(paramPicker.ElevationParameter))
                return Result.Cancelled;

            string eParam = paramPicker.EastingParameter.Trim();
            string nParam = paramPicker.NorthingParameter.Trim();
            string zParam = paramPicker.ElevationParameter.Trim();

            // 6) Transforms & base points
            Transform sharedToInternal = doc.ActiveProjectLocation.GetTotalTransform();
            XYZ surveySharedOrigin = GetSurveyPointShared(doc, sharedToInternal);
            XYZ pbpInternalOrigin = GetProjectBasePointInternal(doc);

            int updated = 0, skippedNoLoc = 0, skippedNoParams = 0;

            using (Transaction tx = new Transaction(doc, "Fill Easting/Northing/Elevation (by Category)"))
            {
                tx.Start();

                foreach (var el in elems)
                {
                    XYZ pInternal = TryGetElementPoint(el);
                    if (pInternal == null)
                    {
                        skippedNoLoc++;
                        continue;
                    }

                    // point in shared (survey) coordinate system
                    XYZ pShared = sharedToInternal.Inverse.OfPoint(pInternal);
                    XYZ zeroOrigin = XYZ.Zero;

                    // offsets in internal feet (X=Easting, Y=Northing, Z=Elevation)
                    double eFeet, nFeet, zFeet;

                    if (refKind == ReferenceKind.SurveyPoint)
                    {
                        //XYZ d = pShared - surveySharedOrigin; // (shared feet) 
                        XYZ d = pShared - zeroOrigin; // Sometimes Survey Point has some values due to that considered Survey Point origin (0,0,0) 
                        eFeet = d.X; nFeet = d.Y; zFeet = d.Z;
                    }
                    else
                    {
                        XYZ d = pInternal - pbpInternalOrigin; // (internal feet)
                        eFeet = d.X; nFeet = d.Y; zFeet = d.Z;
                    }

                    // User display values (for strings)
                    double toUser = (outUnits == UnitsKind.Millimeters) ? FEET_TO_MM : FEET_TO_METERS;
                    double eUser = eFeet * toUser;
                    double nUser = nFeet * toUser;
                    double zUser = zFeet * toUser;

                    // Find selected parameters on this element
                    Parameter pE = LookupParameterInsensitive(el, eParam);
                    Parameter pN = LookupParameterInsensitive(el, nParam);
                    Parameter pZ = LookupParameterInsensitive(el, zParam);

                    if (pE == null && pN == null && pZ == null)
                    {
                        skippedNoParams++;
                        continue;
                    }

                    bool wrote = false;

                    if (pE != null) wrote |= SetParamValue(pE, eUser); // doubles in feet, strings in user units
                    if (pN != null) wrote |= SetParamValue(pN, nUser);
                    if (pZ != null) wrote |= SetParamValue(pZ, zUser);

                    if (wrote) updated++;
                }

                tx.Commit();
            }

            TaskDialog.Show("Populate Easting/Northing/Elevation",
                $"Category: {selectedCategoryName}\n" +
                $"Reference: {(refKind == ReferenceKind.SurveyPoint ? "Survey Point (Shared)" : "Project Base Point (Internal)")}\n" +
                $"String Units: {(outUnits == UnitsKind.Millimeters ? "Millimeters" : "Meters")}\n" +
                $"Target Parameters: E='{eParam}', N='{nParam}', Z='{zParam}'\n\n" +
                $"Processed: {elems.Count}\n" +
                $"Updated: {updated}\n" +
                $"Skipped (no location): {skippedNoLoc}\n" +
                $"Skipped (no parameters on element): {skippedNoParams}");

            return Result.Succeeded;
        }

        private static ReferenceKind AskReferenceKind()
        {
            var td = new TaskDialog("Choose Reference Point")
            { MainInstruction = "Measure offsets from:" };
            td.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, "Survey Point (Shared Coordinates)");
            td.AddCommandLink(TaskDialogCommandLinkId.CommandLink2, "Project Base Point (Internal Coordinates)");
            td.CommonButtons = TaskDialogCommonButtons.Cancel;

            var r = td.Show();
            if (r == TaskDialogResult.CommandLink1) return ReferenceKind.SurveyPoint;
            if (r == TaskDialogResult.CommandLink2) return ReferenceKind.ProjectBasePoint;
            return (ReferenceKind)(-1);
        }

        private static UnitsKind AskUnitsKind()
        {
            var td = new TaskDialog("Choose Units")
            { MainInstruction = "For STRING parameters, output Easting / Northing / Elevation in:" };
            td.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, "Meters (m)");
            td.AddCommandLink(TaskDialogCommandLinkId.CommandLink2, "Millimeters (mm)");
            td.CommonButtons = TaskDialogCommonButtons.Cancel;

            var r = td.Show();
            if (r == TaskDialogResult.CommandLink1) return UnitsKind.Meters;
            if (r == TaskDialogResult.CommandLink2) return UnitsKind.Millimeters;
            return (UnitsKind)(-1);
        }

        // Helpers
        private static bool HasUsableLocation(Element el)
        {
            var loc = el.Location;
            if (loc is LocationPoint) return true;
            if (loc is LocationCurve lc && lc.Curve != null) return true;
            return false;
        }

        private static XYZ TryGetElementPoint(Element el)
        {
            Location loc = el.Location;
            if (loc is LocationPoint lp) return lp.Point;
            if (loc is LocationCurve lc) return lc.Curve?.Evaluate(0.5, true);
            return null;
        }

        private static XYZ GetSurveyPointShared(Document doc, Transform sharedToInternal)
        {
            var sp = new FilteredElementCollector(doc)
                .OfClass(typeof(BasePoint))
                .Cast<BasePoint>()
                .FirstOrDefault(x => x.IsShared);

            return sp != null ? sharedToInternal.Inverse.OfPoint(sp.Position) : XYZ.Zero;
        }

        private static XYZ GetProjectBasePointInternal(Document doc)
        {
            var pbp = new FilteredElementCollector(doc)
                .OfClass(typeof(BasePoint))
                .Cast<BasePoint>()
                .FirstOrDefault(x => !x.IsShared);
            return pbp?.Position ?? XYZ.Zero;
        }

        private static HashSet<string> CollectWritableParamNames(List<Element> elems, int maxScan = 20)
        {
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var el in elems.Take(Math.Max(1, maxScan)))
            {
                foreach (Parameter p in el.Parameters)
                {
                    if (p == null || p.Definition == null) continue;
                    if (p.IsReadOnly) continue;

                    if (p.StorageType == StorageType.String || p.StorageType == StorageType.Double)
                    {
                        names.Add(p.Definition.Name);
                    }
                }
            }
            return names;
        }

        private static Parameter LookupParameterInsensitive(Element el, string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return null;

            var p = el.LookupParameter(name);
            if (p != null) return p;

            foreach (Parameter q in el.Parameters)
            {
                if (q.Definition != null &&
                    string.Equals(q.Definition.Name, name, StringComparison.OrdinalIgnoreCase))
                    return q;
            }
            return null;
        }


        private static bool SetParamValue(Parameter p, double valueUserUnits)
        {
            if (p == null || p.IsReadOnly) return false;

            if (p.StorageType == StorageType.Double)
            {
                // Write the meters/mm number directly into a Number spec parameter
                return p.Set(valueUserUnits);
            }
            if (p.StorageType == StorageType.String)
            {
                return p.Set(valueUserUnits.ToString("0.###", CultureInfo.InvariantCulture));
            }
            return false;
        }

    }
}

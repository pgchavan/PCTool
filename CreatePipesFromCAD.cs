using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using HPDTool;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HPDTool
{
    [Transaction(TransactionMode.Manual)]
    public class CreatePipesFromCAD : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;

            Reference reference;
            try
            {
                reference = uidoc.Selection.PickObject(ObjectType.Element, "Select CAD Import");
            }
            catch
            {
                return Result.Cancelled;
            }

            ImportInstance cadImport = doc.GetElement(reference) as ImportInstance;

            if (cadImport == null)
            {
                TaskDialog.Show("Error", "Invalid CAD selected.");
                return Result.Failed;
            }

            List<string> layers = GetLayers(cadImport, doc);

            if (layers == null || !layers.Any())
            {
                TaskDialog.Show("Error", "No CAD layers found.");
                return Result.Failed;
            }

            List<PipeType> pipeTypes = new FilteredElementCollector(doc)
                .OfClass(typeof(PipeType))
                .Cast<PipeType>()
                .OrderBy(x => x.Name)
                .ToList();

            if (!pipeTypes.Any())
            {
                TaskDialog.Show("Error", "No Pipe Types found in project.");
                return Result.Failed;
            }

            List<PipingSystemType> systemTypes = new FilteredElementCollector(doc)
                .OfClass(typeof(PipingSystemType))
                .Cast<PipingSystemType>()
                .OrderBy(x => x.Name)
                .ToList();

            if (!systemTypes.Any())
            {
                TaskDialog.Show("Error", "No Piping System Types found in project.");
                return Result.Failed;
            }

            PipeForm form = new PipeForm(layers, pipeTypes, systemTypes, doc);

            if (form.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                return Result.Cancelled;

            string selectedLayer = form.SelectedLayer;
            PipeType selectedPipeType = form.SelectedPipeType;
            PipingSystemType selectedSystemType = form.SelectedSystemType;
            double diameterMM = form.Diameter;
            PipeAlignment alignment = form.Alignment;

            if (selectedPipeType == null)
            {
                TaskDialog.Show("Error", "Pipe Type was not selected.");
                return Result.Failed;
            }

            if (selectedSystemType == null)
            {
                TaskDialog.Show("Error", "System Type was not selected.");
                return Result.Failed;
            }

            Level level = new FilteredElementCollector(doc)
                .OfClass(typeof(Level))
                .Cast<Level>()
                .OrderBy(l => l.Elevation)
                .FirstOrDefault();

            if (level == null)
            {
                TaskDialog.Show("Error", "No Level found in project.");
                return Result.Failed;
            }

            double diaInternal = UnitUtils.ConvertToInternalUnits(diameterMM, UnitTypeId.Millimeters);
            double radius = diaInternal / 2.0;

            double minLength = UnitUtils.ConvertToInternalUnits(3, UnitTypeId.Millimeters);

            var curves = GetAllCurves(cadImport, doc, selectedLayer);

            if (curves == null || !curves.Any())
            {
                TaskDialog.Show("Error", "No valid CAD geometry found on selected layer.");
                return Result.Failed;
            }

            int createdCount = 0;

            using (Transaction tx = new Transaction(doc, "Create Pipes From CAD"))
            {
                tx.Start();

                foreach (var c in curves)
                {
                    XYZ p1 = c.Item1;
                    XYZ p2 = c.Item2;

                    if (p1 == null || p2 == null)
                        continue;

                    if (p1.DistanceTo(p2) < minLength)
                        continue;

                    if (p1.IsAlmostEqualTo(p2))
                        continue;

                    XYZ offset = XYZ.Zero;

                    if (alignment == PipeAlignment.Top)
                        offset = new XYZ(0, 0, -radius);
                    else if (alignment == PipeAlignment.Bottom)
                        offset = new XYZ(0, 0, radius);

                    Pipe pipe = Pipe.Create(
                        doc,
                        selectedSystemType.Id,
                        selectedPipeType.Id,
                        level.Id,
                        p1 + offset,
                        p2 + offset);

                    Parameter diaParam = pipe.get_Parameter(BuiltInParameter.RBS_PIPE_DIAMETER_PARAM);
                    if (diaParam != null && !diaParam.IsReadOnly)
                        diaParam.Set(diaInternal);

                    createdCount++;
                }

                tx.Commit();
            }

            TaskDialog.Show("Success", createdCount + " pipe(s) created successfully.");
            return Result.Succeeded;
        }

        private List<string> GetLayers(ImportInstance cad, Document doc)
        {
            HashSet<string> layers = new HashSet<string>();

            GeometryElement geo = cad.get_Geometry(new Options());
            if (geo == null)
                return layers.ToList();

            foreach (GeometryObject obj in geo)
            {
                GeometryInstance inst = obj as GeometryInstance;
                if (inst == null)
                    continue;

                foreach (GeometryObject item in inst.SymbolGeometry)
                {
                    GraphicsStyle style = doc.GetElement(item.GraphicsStyleId) as GraphicsStyle;
                    if (style != null && style.GraphicsStyleCategory != null)
                        layers.Add(style.GraphicsStyleCategory.Name);
                }
            }

            return layers.OrderBy(x => x).ToList();
        }

        private List<Tuple<XYZ, XYZ>> GetAllCurves(ImportInstance cad, Document doc, string layer)
        {
            List<Tuple<XYZ, XYZ>> curves = new List<Tuple<XYZ, XYZ>>();

            GeometryElement geo = cad.get_Geometry(new Options());
            if (geo == null)
                return curves;

            foreach (GeometryObject obj in geo)
            {
                GeometryInstance inst = obj as GeometryInstance;
                if (inst == null)
                    continue;

                Transform transform = inst.Transform;

                foreach (GeometryObject item in inst.SymbolGeometry)
                {
                    GraphicsStyle style = doc.GetElement(item.GraphicsStyleId) as GraphicsStyle;

                    if (style == null || style.GraphicsStyleCategory == null)
                        continue;

                    if (style.GraphicsStyleCategory.Name != layer)
                        continue;

                    PolyLine poly = item as PolyLine;
                    if (poly != null)
                    {
                        IList<XYZ> pts = poly.GetCoordinates();

                        for (int i = 0; i < pts.Count - 1; i++)
                        {
                            XYZ p1 = transform.OfPoint(pts[i]);
                            XYZ p2 = transform.OfPoint(pts[i + 1]);

                            curves.Add(Tuple.Create(p1, p2));
                        }

                        continue;
                    }

                    Line line = item as Line;
                    if (line != null)
                    {
                        XYZ p1 = transform.OfPoint(line.GetEndPoint(0));
                        XYZ p2 = transform.OfPoint(line.GetEndPoint(1));

                        curves.Add(Tuple.Create(p1, p2));
                    }
                }
            }

            return curves;
        }
    }
}

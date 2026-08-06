using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;
using System.Collections.Generic;
using System.Linq;
using HPDTool.UI;

namespace HPDTool
{
    [Transaction(TransactionMode.Manual)]
    public class CopyParametersCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;

            try
            {
                // ------------------------------------
                // STEP 1: Select SOURCE element
                // ------------------------------------
                Reference srcRef = uidoc.Selection.PickObject(
                    ObjectType.Element,
                    "Select the element FROM which parameters will be copied"
                );

                Element sourceElement = doc.GetElement(srcRef);

                // ------------------------------------
                // STEP 2: Choose WHICH parameters to copy
                // ------------------------------------
                List<Parameter> editableParams =
                    sourceElement.Parameters
                    .Cast<Parameter>()
                    .Where(p => !p.IsReadOnly)
                    .ToList();

                ParametersToCopyWindow win =
                    new ParametersToCopyWindow(editableParams);

                bool? winResult = win.ShowDialog();
                if (winResult != true)
                    return Result.Cancelled;

                List<Parameter> selectedParams = win.SelectedParameters;

                // ------------------------------------
                // STEP 3: Select TARGET elements
                // ------------------------------------
                IList<Reference> targetRefs = uidoc.Selection.PickObjects(
                    ObjectType.Element,
                    "Select elements TO which parameters should be copied"
                );

                List<Element> targetElements =
                    targetRefs.Select(r => doc.GetElement(r)).ToList();

                // ------------------------------------
                // STEP 4: Copy values
                // ------------------------------------
                using (Transaction t = new Transaction(doc, "Copy Parameters"))
                {
                    t.Start();

                    foreach (Element target in targetElements)
                    {
                        foreach (Parameter srcParam in selectedParams)
                        {
                            Parameter targetParam =
                                target.LookupParameter(srcParam.Definition.Name);

                            if (targetParam != null && !targetParam.IsReadOnly)
                                CopyValue(srcParam, targetParam);
                        }
                    }

                    t.Commit();
                }

                TaskDialog.Show("Success", "Parameter values copied successfully.");
                return Result.Succeeded;
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                return Result.Cancelled;
            }
        }

        private void CopyValue(Parameter src, Parameter dst)
        {
            switch (src.StorageType)
            {
                case StorageType.String:
                    dst.Set(src.AsString());
                    break;

                case StorageType.Double:
                    dst.Set(src.AsDouble());
                    break;

                case StorageType.Integer:
                    dst.Set(src.AsInteger());
                    break;

                case StorageType.ElementId:
                    dst.Set(src.AsElementId());
                    break;
            }
        }
    }
}

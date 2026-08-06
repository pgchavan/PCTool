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
    public class SelectByParameter : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uiDoc = commandData.Application.ActiveUIDocument;
            Document doc = uiDoc.Document;

            ParameterSelectionWindow window =
                new ParameterSelectionWindow(doc, uiDoc);

            window.ShowDialog();

            return Result.Succeeded;
        }
    }
}

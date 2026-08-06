using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using System;
using System.Linq;
using System.Windows;
using HPDTool.UI;
using Autodesk.Revit.Attributes;

namespace HPDTool
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class TextCase : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;

            // Get selected TextNotes
            var selectedTextNotes = uidoc.Selection
                .GetElementIds()
                .Select(id => doc.GetElement(id))
                .OfType<TextNote>()
                .ToList();

            if (!selectedTextNotes.Any())
            {
                TaskDialog.Show("No TextNotes", "Please select one or more TextNotes.");
                return Result.Cancelled;
            }

            TxtWin window = new TxtWin(doc, selectedTextNotes);
            window.ShowDialog();

            return Result.Succeeded;
        }
    }
}

using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using HPDTool.UI;

namespace HPDTool
{

    [Transaction(TransactionMode.Manual)]
    public class CmdFeedback : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            FeedbackWindow win = new FeedbackWindow();
            win.ShowDialog();
            return Result.Succeeded;
        }
    }
}
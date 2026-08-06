using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.Linq;
using HPDTool.UI;

namespace HPDTool
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class PurgeFilter : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiApp = commandData.Application;
            UIDocument uidoc = uiApp.ActiveUIDocument;
            Document doc = uidoc.Document;

            FiltWin window = new FiltWin(doc);
            bool? result = window.ShowDialog();

            if (result == true)
            {
                var filtersToPurge = window.FiltersToPurge;

                if (filtersToPurge.Any())
                {
                    using (Transaction t = new Transaction(doc, "Purge Selected View Filters"))
                    {
                        t.Start();
                        int purgedCount = 0;
                        foreach (var filter in filtersToPurge)
                        {
                            try
                            {
                                doc.Delete(filter.Id);
                                purgedCount++;
                            }
                            catch (Autodesk.Revit.Exceptions.InvalidOperationException ex)
                            {
                                message += $"Could not delete filter '{filter.Name}'. It might be in use or have dependencies.\n{ex.Message}\n";
                            }
                        }
                        t.Commit();

                        TaskDialog.Show("Purge Filters", $"Purged {purgedCount} selected view filter(s).\n{message}");
                        return Result.Succeeded;
                    }
                }
                else
                {
                    TaskDialog.Show("Purge Filters", "No filters selected for purging.");
                    return Result.Cancelled;
                }
            }
            return Result.Cancelled;
        }
    }
}
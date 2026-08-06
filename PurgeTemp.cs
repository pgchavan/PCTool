using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using HPDTool.UI;

namespace HPDTool
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class PurgeTemp : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            Document doc = uiapp.ActiveUIDocument.Document;

            // Get all view templates in the document
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            List<View> viewTemplates = collector
                .OfClass(typeof(View))
                .Cast<View>()
                .Where(v => v.IsTemplate)
                .OrderBy(v => v.Name)
                .ToList();

            if (!viewTemplates.Any())
            {
                TaskDialog.Show("Purge View Templates", "No view templates found in the document.");
                return Result.Succeeded;
            }

            // Launch the WPF window to select templates to purge
            TempWin window = new TempWin(doc, viewTemplates);
            bool? result = window.ShowDialog();

            if (result == true)
            {
                // Get the list of templates to purge from the window's DataContext
                if (window.DataContext is List<View> templatesToPurge && templatesToPurge.Any())
                {
                    using (Transaction trans = new Transaction(doc, "Purge View Templates"))
                    {
                        trans.Start();
                        int purgedCount = 0;
                        foreach (View template in templatesToPurge)
                        {
                            try
                            {
                                doc.Delete(template.Id);
                                purgedCount++;
                            }
                            catch (Autodesk.Revit.Exceptions.InvalidOperationException ex)
                            {
                                // Handle cases where the template might be in use or cannot be deleted
                                message += $"Error deleting template '{template.Name}': {ex.Message}\n";
                            }
                        }
                        trans.Commit();

                        TaskDialog.Show("Purge View Templates", $"{purgedCount} view template(s) purged successfully.\n{message}");
                    }
                    return Result.Succeeded;
                }
                else
                {
                    TaskDialog.Show("Purge View Templates", "No view templates selected for purging.");
                    return Result.Cancelled;
                }
            }
            else
            {
                return Result.Cancelled; // User cancelled the dialog
            }
        }
    }
}
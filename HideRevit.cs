using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Schema;
using System.Windows.Forms;

namespace HPDTool
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class HideRevit : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var uiApplication = commandData.Application;
            var application = uiApplication.Application;
            var uiDocument = uiApplication.ActiveUIDocument;
            var document = uiDocument.Document;

            var rvtLinksCategory = document.Settings.Categories.get_Item(BuiltInCategory.OST_RvtLinks);

            // Check if there are any Revit Links in the document
            FilteredElementCollector collector = new FilteredElementCollector(document)
                .OfClass(typeof(RevitLinkInstance));

            if (collector.GetElementCount() == 0)
            {
                MessageBox.Show("No Revit Links found in the current project.", "HPD Tool - Revit Link Visibility");
                return Result.Succeeded;
            }

            var isHidden = document.ActiveView.GetCategoryHidden(rvtLinksCategory.Id);

            using (var tx = new Transaction(document, "Toggles Override Revit Links"))
            {
                tx.Start();
                if (isHidden)
                {
                    document.ActiveView.SetCategoryHidden(rvtLinksCategory.Id, false);
                }
                else
                {
                    document.ActiveView.SetCategoryHidden(rvtLinksCategory.Id, true);
                }
                tx.Commit();
            }

            return Result.Succeeded;
        }
    }
}
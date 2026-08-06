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
    public class HidePointCloud : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            var uiApplication = commandData.Application;
            var application = uiApplication.Application;
            var uiDocument = uiApplication.ActiveUIDocument;
            var document = uiDocument.Document;

            var pointCloudCategory = document.Settings.Categories.get_Item(BuiltInCategory.OST_PointClouds);

            // Check if there are any point clouds in the document
            FilteredElementCollector collector = new FilteredElementCollector(document)
                .OfClass(typeof(PointCloudInstance));

            if (collector.GetElementCount() == 0)
            {
                MessageBox.Show("No Point Clouds found in the current project.", "HPD Tool - Point Cloud Visibility");
                return Result.Succeeded;
            }

            var isHidden = document.ActiveView.GetCategoryHidden(pointCloudCategory.Id);

            using (var tx = new Transaction(document, "Toggles Override Point Cloud"))
            {
                tx.Start();
                if (isHidden)
                {
                    document.ActiveView.SetCategoryHidden(pointCloudCategory.Id, false);
                }
                else
                {
                    document.ActiveView.SetCategoryHidden(pointCloudCategory.Id, true);
                }
                tx.Commit();
            }

            return Result.Succeeded;
        }
    }
}
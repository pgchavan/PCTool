using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Media.Imaging;

namespace HPDTool // Changed namespace name
{
    public class AppCommand : IExternalApplication
    {
        public Result OnStartup(UIControlledApplication application)
        {
            var tabName = "HPD Tools"; // Changed tab name

            // Attempt to create the ribbon tab
            try
            {
                application.CreateRibbonTab(tabName);
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Error", $"Error creating ribbon tab '{tabName}': {ex.Message}");
                return Result.Failed;
            }

            // Define panel names
            var panelName1 = "Link Visibility";
            var panelName2 = "Modify Elements";
            var panelName3 = "Purge"; // Added panel for Purge tools
            var panelName4 = "Text";
            var panelName5 = "Creation";
            var panelName6 = "Properties";
            var panelName7 = "Parameter";
            var panelName8 = "Selection";
            var panelName9 = "Project";
            //var panelName10 = "FeedBack";


            // Create the first panel and buttons
            RibbonPanel panel1 = CreatePanel(application, tabName, panelName1);
            if (panel1 != null)
            {
                var assemblyPath = Assembly.GetExecutingAssembly().Location;

                var buttonData1 = CreateButton(
                    "Revit Links",
                    "Revit\nLinks",
                    assemblyPath,
                    "HPDTool.HideRevit", // Changed command namespace
                    "This tool toggles the visibility of all linked Revit models in the active view. It's useful when you want to temporarily hide links to improve model clarity or performance.\r\n— Developed by [Prasad Chavan]",
                    new BitmapImage(new Uri(Path.Combine(Path.GetDirectoryName(assemblyPath), "Resources", "rvt.png")))
                );
                panel1.AddItem(buttonData1);
                panel1.AddSeparator();

                var buttonData2 = CreateButton(
                    "Point Cloud",
                    "Point\nCloud",
                    assemblyPath,
                    "HPDTool.HidePointCloud", // Changed command namespace
                    "This tool toggles the visibility of all linked point cloud files in the current view. Use it to quickly clean up your workspace or focus on Revit elements without deleting the point cloud data.\r\n— Developed by [Prasad Chavan]",
                    new BitmapImage(new Uri(Path.Combine(Path.GetDirectoryName(assemblyPath), "Resources", "pointcloud.png")))
                );
                panel1.AddItem(buttonData2);
                panel1.AddSeparator();

                var buttonData3 = CreateButton(
                    "CAD Links",
                    "CAD\nLinks",
                    assemblyPath,
                    "HPDTool.HideCAD", // Changed command namespace
                    "This tool toggles the visibility of all linked or imported CAD files in the current view. Use it to quickly clean up your workspace or focus on Revit elements without deleting the CAD data.\r\n— Developed by [Prasad Chavan]",
                    new BitmapImage(new Uri(Path.Combine(Path.GetDirectoryName(assemblyPath), "Resources", "cad.png")))
                );
                panel1.AddItem(buttonData3);
                //panel1.AddSeparator();
            }

            // Create the second panel and buttons
            RibbonPanel panel2 = CreatePanel(application, tabName, panelName2);
            if (panel2 != null)
            {
                var assemblyPath = Assembly.GetExecutingAssembly().Location;

                var buttonData4 = CreateButton(
                    "Rotate",
                    "Rotate",
                    assemblyPath,
                    "HPDTool.RotateElementsCommand", // Changed command namespace
                    "Rotates selected elements around its own axis with a specified angle.\r\n— Developed by [Prasad Chavan]",
                    new BitmapImage(new Uri(Path.Combine(Path.GetDirectoryName(assemblyPath), "Resources", "rotate.png"))) // Make sure you have a rotate.png
                    );
                panel2.AddItem(buttonData4);
                //panel2.AddSeparator();
                // Add more buttons for the "Modify Elements" panel here if needed
            }

            // Create the third panel for Purge
            RibbonPanel panel3 = CreatePanel(application, tabName, panelName3);
            if (panel3 != null)
            {
                var assemblyPath = Assembly.GetExecutingAssembly().Location;
                var purgeViewTemplatesButton = CreateButton(
                    "Purge Unused View Templates",
                    "Purge View\nTemplates",
                    assemblyPath,
                    "HPDTool.PurgeTemp", // Changed command namespace
                    "Purges unused view templates in the project.\n—Developed by [Prasad Chavan]",
                    new BitmapImage(new Uri(Path.Combine(Path.GetDirectoryName(assemblyPath), "Resources", "deltemp.png")))); //create appropriate icon
                panel3.AddItem(purgeViewTemplatesButton);
                panel3.AddSeparator();

                var purgeViewFiltersButton = CreateButton(
                    "Purge Unused View Filters",
                    "Purge View\nFilters",
                    assemblyPath,
                    "HPDTool.PurgeFilter", // Changed command namespace
                    "Purges unused view filters in the project.\n—Developed by [Prasad Chavan]",
                    new BitmapImage(new Uri(Path.Combine(Path.GetDirectoryName(assemblyPath), "Resources", "delflt.png"))));  //create appropriate icon
                panel3.AddItem(purgeViewFiltersButton);
                //panel3.AddSeparator();
            }

            RibbonPanel panel4 = CreatePanel(application, tabName, panelName4);
            if (panel4 != null)
            {
                var assemblyPath = Assembly.GetExecutingAssembly().Location;
                var changeTextCasesButton = CreateButton(
                    "Change Text Cases",
                    "Change Text\nCases",
                    assemblyPath,
                    "HPDTool.TextCase", // Changed command namespace
                    "Changes the text case of selected text elements.\n—Developed by [Prasad Chavan]",
                    new BitmapImage(new Uri(Path.Combine(Path.GetDirectoryName(assemblyPath), "Resources", "txtcase.png"))));
                panel4.AddItem(changeTextCasesButton);
                //panel4.AddSeparator();
            }

            // Create the third panel for Purge
            RibbonPanel panel5 = CreatePanel(application, tabName, panelName5);
            if (panel5 != null)
            {
                var assemblyPath = Assembly.GetExecutingAssembly().Location;
                var createPipe = CreateButton(
                    "Create Pipes From CAD",
                    "Create Pipes\nFrom CAD",
                    assemblyPath,
                    "HPDTool.CreatePipesFromCAD", // Changed command namespace
                    "Creates Pipes from selected CAD Layer.\n—Developed by [Prasad Chavan]",
                    new BitmapImage(new Uri(Path.Combine(Path.GetDirectoryName(assemblyPath), "Resources", "pipes.png")))); //create appropriate icon
                panel5.AddItem(createPipe);
                panel5.AddSeparator();

                var convertDuctsTOConduit = CreateButton(
                    "Duct To Conduit",
                    "Duct To\nConduit",
                    assemblyPath,
                    "HPDTool.ConvertDuctsToConduit", // Changed command namespace
                    "Converts selected Ducts into Conduits.\n—Developed by [Prasad Chavan]",
                    new BitmapImage(new Uri(Path.Combine(Path.GetDirectoryName(assemblyPath), "Resources", "conduits.png"))));  //create appropriate icon
                panel5.AddItem(convertDuctsTOConduit);
                //panel5.AddSeparator();
            }


            RibbonPanel panel6 = CreatePanel(application, tabName, panelName6);
            if (panel6 != null)
            {
                var assemblyPath = Assembly.GetExecutingAssembly().Location;
                var matchOverridePropertiesButton = CreateButton(
                    "Match",
                    "Match Override\nProperties",
                    assemblyPath,
                    "HPDTool.MatchElements", // Changed command namespace
                    "Matches the override properties of selected elements.\n—Developed by [Prasad Chavan]",
                    new BitmapImage(new Uri(Path.Combine(Path.GetDirectoryName(assemblyPath), "Resources", "match.png"))));
                panel6.AddItem(matchOverridePropertiesButton);
                //panel6.AddSeparator();
            }

            RibbonPanel panel7 = CreatePanel(application, tabName, panelName7);
            if (panel7 != null)
            {
                var assemblyPath = Assembly.GetExecutingAssembly().Location;
                var copyP = CreateButton(
                    "Copy Parameter\nValue",
                    "Copy Parameter\nValue",
                    assemblyPath,
                    "HPDTool.CopyParametersCommand", // Changed command namespace
                    "Copy parameters from one elements to similar selected elements.\n—Developed by [Prasad Chavan]",
                    new BitmapImage(new Uri(Path.Combine(Path.GetDirectoryName(assemblyPath), "Resources", "copy.png"))));
                panel7.AddItem(copyP);
                panel7.AddSeparator();

                var reOrder = CreateButton(
                    "Reorder\nElements",
                    "Reorder\nElements",
                    assemblyPath,
                    "HPDTool.OrderBySelectionCommand", // Changed command namespace
                    "Reorder the elements as per selection order.\n—Developed by [Prasad Chavan]",
                    new BitmapImage(new Uri(Path.Combine(Path.GetDirectoryName(assemblyPath), "Resources", "reorder.png"))));
                panel7.AddItem(reOrder);
                panel7.AddSeparator();

                var setSOP = CreateButton(
                    "Set\nSOP",
                    "Set\nSOP",
                    assemblyPath,
                    "HPDTool.Sop", // Changed command namespace
                    "Sets Easting & Northing for Selected Family.\n—Developed by [Prasad Chavan]",
                    new BitmapImage(new Uri(Path.Combine(Path.GetDirectoryName(assemblyPath), "Resources", "sop.png"))));
                panel7.AddItem(setSOP);
                //panel7.AddSeparator();

            }

            RibbonPanel panel8 = CreatePanel(application, tabName, panelName8);
            if (panel8 != null)
            {
                var assemblyPath = Assembly.GetExecutingAssembly().Location;

                var selectionByCat = CreateButton(
                 "Select Elements\nBy Category",
                 "Select Elements\nBy Category",
                 assemblyPath,
                 "HPDTool.SelectByCategory", // Changed command namespace
                 "Select elements of required category.\n—Developed by [Prasad Chavan]",
                 new BitmapImage(new Uri(Path.Combine(Path.GetDirectoryName(assemblyPath), "Resources", "catsel.png"))));
                panel8.AddItem(selectionByCat);
                panel8.AddSeparator();

                var selbyPara = CreateButton(
                    "Select By\nParameter Value",
                    "Select By\nParameter Value",
                    assemblyPath,
                    "HPDTool.SelectByParameter", // Changed command namespace
                    "Select elements by there Parameter Value.\n—Developed by [Prasad Chavan]",
                    new BitmapImage(new Uri(Path.Combine(Path.GetDirectoryName(assemblyPath), "Resources", "selbyparam.png"))));
                panel8.AddItem(selbyPara);
                //panel8.AddSeparator();

  
            }

            RibbonPanel panel9 = CreatePanel(application, tabName, panelName9);
            if (panel9 != null)
            {
                var assemblyPath = Assembly.GetExecutingAssembly().Location;

                var exportFamily = CreateButton(
                    "Extract Linked Families",
                    "Extract Linked\nFamilies",
                    assemblyPath,
                    "HPDTool.ExtractLinkedFamiliesCommand", // Changed command namespace
                    "Extract families from linked models.\n—Developed by [Prasad Chavan]",
                    new BitmapImage(new Uri(Path.Combine(Path.GetDirectoryName(assemblyPath), "Resources", "exportfam.png"))));
                panel9.AddItem(exportFamily);
                panel9.AddSeparator();

                var batchLink = CreateButton(
                    "Batch Revit\nLink",
                    "Batch Revit\nLink",
                    assemblyPath,
                    "HPDTool.LinkModelsCommand", // Changed command namespace
                    "Link multiple revit files at once.\n—Developed by [Prasad Chavan]",
                    new BitmapImage(new Uri(Path.Combine(Path.GetDirectoryName(assemblyPath), "Resources", "batchlink.png"))));
                panel9.AddItem(batchLink);
                //panel9.AddSeparator();
            }

            //RibbonPanel panel10 = CreatePanel(application, tabName, panelName10);
            //if (panel10 != null)
            //{
            //    var assemblyPath = Assembly.GetExecutingAssembly().Location;
            //    var feedBack = CreateButton(
            //        "Feedback",
            //        "Feedback",
            //        assemblyPath,
            //        "HPDTool.CmdFeedback", // Changed command namespace
            //        "Send feedback or suggestions.\n—Developed by [Prasad Chavan]",
            //        new BitmapImage(new Uri(Path.Combine(Path.GetDirectoryName(assemblyPath), "Resources", "feedback.png"))));
            //    panel10.AddItem(feedBack);
            //}
            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }

        private RibbonPanel CreatePanel(UIControlledApplication application, string tabName, string panelName)
        {
            RibbonPanel panel = null;
            try
            {
                panel = application.CreateRibbonPanel(tabName, panelName);
            }
            catch (Exception ex)
            {
                // If the panel already exists, get the existing one.
                panel = application.GetRibbonPanels(tabName).FirstOrDefault(p => p.Name == panelName);
                if (panel == null)
                {
                    TaskDialog.Show("Error", $"Error creating ribbon panel '{panelName}': {ex.Message}");
                }
            }
            return panel;
        }

        private PushButtonData CreateButton(
            string buttonName,
            string buttonText,
            string assemblyPath,
            string commandNamespace,
            string toolTip,
            BitmapImage largeImage)
        {
            return new PushButtonData(buttonName, buttonText, assemblyPath, commandNamespace)
            {
                ToolTip = toolTip,
                LargeImage = largeImage
            };
        }
    }
}

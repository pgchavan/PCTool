using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Interop;
using HPDTool.UI;

namespace HPDTool
{
    [Transaction(TransactionMode.Manual)]
    public class LinkModelsCommand : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            UIApplication uiApp = commandData.Application;
            UIDocument uiDoc = uiApp.ActiveUIDocument;
            Document doc = uiDoc.Document;

            try
            {
                // Show dialog
                LinkModelsWindow window = new LinkModelsWindow();
                new WindowInteropHelper(window)
                {
                    Owner = uiApp.MainWindowHandle
                };

                if (window.ShowDialog() != true)
                    return Result.Cancelled;

                using (Transaction tx = new Transaction(doc, "Link Models"))
                {
                    tx.Start();

                    foreach (string path in window.SelectedFiles)
                    {
                        try
                        {
                            if (!File.Exists(path))
                                throw new FileNotFoundException("File not found", path);

                            ModelPath modelPath =
                                ModelPathUtils.ConvertUserVisiblePathToModelPath(path);

                            RevitLinkType linkType =
                                GetOrCreateLinkType(doc, modelPath);

                            if (linkType == null)
                                throw new InvalidOperationException(
                                    $"Failed to create link type:\n{path}");

                            RevitLinkInstance.Create(
                                doc,
                                linkType.Id,
                                window.SelectedPlacement);
                        }
                        catch (Exception ex)
                        {
                            // Log individual file errors, continue processing others
                            TaskDialog.Show(
                                "Link Error",
                                $"Failed to link:\n{path}\n\n{ex.Message}");
                        }
                    }

                    tx.Commit();
                }

                return Result.Succeeded;
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                return Result.Cancelled;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }

        /// <summary>
        /// Gets an existing RevitLinkType or creates a new one.
        /// Safe path comparison across all Revit versions.
        /// </summary>
        private RevitLinkType GetOrCreateLinkType(Document doc, ModelPath modelPath)
        {
            string targetPath =
                ModelPathUtils.ConvertModelPathToUserVisiblePath(modelPath);

            foreach (RevitLinkType type in
                new FilteredElementCollector(doc)
                .OfClass(typeof(RevitLinkType))
                .Cast<RevitLinkType>())
            {
                ExternalFileReference extRef = type.GetExternalFileReference();
                if (extRef == null) continue;

                ModelPath existingPath = extRef.GetPath();
                if (existingPath == null) continue;

                string existingUserPath =
                    ModelPathUtils.ConvertModelPathToUserVisiblePath(existingPath);

                if (string.Equals(existingUserPath, targetPath,
                    StringComparison.InvariantCultureIgnoreCase))
                {
                    return type;
                }
            }

            RevitLinkOptions options = new RevitLinkOptions(false);
            LinkLoadResult result =
                RevitLinkType.Create(doc, modelPath, options);

            return result.ElementId == ElementId.InvalidElementId
                ? null
                : doc.GetElement(result.ElementId) as RevitLinkType;
        }
    }
}

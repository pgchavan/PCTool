using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using HPDTool.UI;

namespace HPDTool
{
    [Transaction(TransactionMode.Manual)]
    public class ExtractLinkedFamiliesCommand : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            UIApplication uiApp = commandData.Application;
            UIDocument uiDoc = uiApp.ActiveUIDocument;
            Document doc = uiDoc.Document;

            // ---------------------------------------------------------
            // 1. COLLECT LOADABLE FAMILIES FROM LINKED MODELS
            // ---------------------------------------------------------
            List<FamilyItem> familyItems = new List<FamilyItem>();

            var links = new FilteredElementCollector(doc)
                .OfClass(typeof(RevitLinkInstance))
                .Cast<RevitLinkInstance>();

            foreach (var link in links)
            {
                Document linkDoc = link.GetLinkDocument();
                if (linkDoc == null) continue;

                var families = new FilteredElementCollector(linkDoc)
                    .OfClass(typeof(Family))
                    .Cast<Family>()
                    .Where(f => f.IsEditable);

                foreach (Family fam in families)
                {
                    if (familyItems.Any(x => x.Name == fam.Name))
                        continue;

                    familyItems.Add(new FamilyItem
                    {
                        Family = fam,
                        IsSelected = false
                    });
                }
            }

            if (familyItems.Count == 0)
            {
                TaskDialog.Show("Extract Families", "No loadable families found in linked models.");
                return Result.Cancelled;
            }

            // ---------------------------------------------------------
            // 2. SHOW FAMILY SELECTION UI
            // ---------------------------------------------------------
            var window = new FamilySelectionWindow(familyItems);
            RevitWindowHelper.SetOwner(window, uiApp);

            if (window.ShowDialog() != true)
                return Result.Cancelled;

            List<FamilyItem> selectedItems =
                window.FamilyItems.Where(f => f.IsSelected).ToList();

            if (selectedItems.Count == 0)
            {
                TaskDialog.Show("Extract Families", "No families selected.");
                return Result.Cancelled;
            }

            // ---------------------------------------------------------
            // 3. ASK USER FOR EXPORT FOLDER
            // ---------------------------------------------------------
            string exportPath;

            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Select folder to save extracted families";
                dialog.ShowNewFolderButton = true;

                if (dialog.ShowDialog() != DialogResult.OK ||
                    string.IsNullOrWhiteSpace(dialog.SelectedPath))
                {
                    return Result.Cancelled;
                }

                exportPath = dialog.SelectedPath;
            }

            // ---------------------------------------------------------
            // 4. COPY VIA FAMILYSYMBOL (RELIABLE METHOD)
            // ---------------------------------------------------------
            List<Family> copiedFamilies = new List<Family>();

            using (Transaction t = new Transaction(doc, "Temporary Copy Families"))
            {
                t.Start();

                CopyPasteOptions options = new CopyPasteOptions();
                options.SetDuplicateTypeNamesHandler(
                    new UseDestinationTypeHandler());

                foreach (var item in selectedItems)
                {
                    FamilySymbol sourceSymbol =
                        item.Family.GetFamilySymbolIds()
                                   .Select(id => item.Family.Document.GetElement(id))
                                   .OfType<FamilySymbol>()
                                   .FirstOrDefault();

                    if (sourceSymbol == null)
                        continue;

                    ICollection<ElementId> copiedIds =
                        ElementTransformUtils.CopyElements(
                            sourceSymbol.Document,
                            new[] { sourceSymbol.Id },
                            doc,
                            Transform.Identity,
                            options);

                    foreach (ElementId id in copiedIds)
                    {
                        Element e = doc.GetElement(id);

                        if (e is FamilySymbol fs)
                            copiedFamilies.Add(fs.Family);
                        else if (e is Family f)
                            copiedFamilies.Add(f);
                    }
                }

                t.Commit();
            }

            if (copiedFamilies.Count == 0)
            {
                TaskDialog.Show("Extract Families", "No families were copied.");
                return Result.Cancelled;
            }

            // ---------------------------------------------------------
            // 5. SAVE FAMILIES AS .RFA FILES
            // ---------------------------------------------------------
            List<ElementId> familiesToDelete = new List<ElementId>();
            int exported = 0;

            foreach (Family fam in copiedFamilies.Distinct())
            {
                if (!fam.IsEditable)
                    continue;

                Document famDoc = doc.EditFamily(fam);

                string filePath =
                    Path.Combine(exportPath, fam.Name + ".rfa");

                famDoc.SaveAs(filePath, new SaveAsOptions
                {
                    OverwriteExistingFile = true
                });

                famDoc.Close(false);

                familiesToDelete.Add(fam.Id);
                exported++;
            }

         

            TaskDialog.Show(
                "Extract Families",
                $"Completed successfully.\n\nFamilies exported: {exported}\n\nSaved to:\n{exportPath}");

            return Result.Succeeded;
        }
    }
}
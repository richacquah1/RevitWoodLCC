using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitWoodLCC;
using System;

namespace RevitWoodLCC
{
    [Transaction(TransactionMode.Manual)] // Transaction attribute is necessary
    public class ImportMaterialsCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                // Get the active document
                Document doc = commandData.Application.ActiveUIDocument.Document;

                // Create and show the ImportMaterials window
                ImportMaterials window = new ImportMaterials(doc);
                window.ShowDialog();

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                // Log or display the error
                message = $"An error occurred: {ex.Message}";
                return Result.Failed;
            }
        }
    }
}
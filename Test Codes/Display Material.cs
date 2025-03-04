using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System.Collections.Generic;
using System;
using System.Linq;


namespace RevitWoodLCC
{
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
public class DisplayMaterial : IExternalCommand
{
    public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
    {
        Document doc = commandData.Application.ActiveUIDocument.Document;
        UIDocument uidoc = commandData.Application.ActiveUIDocument;

        try
        {
            // Get the currently selected elements in the UI
            ICollection<ElementId> selectedIds = uidoc.Selection.GetElementIds();

            // Check if any elements are selected
            if (selectedIds.Count == 0)
            {
                TaskDialog.Show("Info", "Please select an element.");
                return Result.Cancelled;
            }

            // Just process the first selected element for this example
            ElementId elementId = selectedIds.First();
            Element element = doc.GetElement(elementId);

            // Get the material
            ICollection<ElementId> materialIds = null;
            if (element is FamilyInstance familyInstance)
            {
                // If it's a FamilyInstance, we might have to dig a bit deeper
                if (familyInstance.Symbol != null && familyInstance.Symbol.Category != null)
                {
                    materialIds = familyInstance.Symbol.GetMaterialIds(false);
                }
            }
            else
            {
                // Otherwise, just retrieve the material directly
                materialIds = element.GetMaterialIds(false);
            }

            if (materialIds == null || materialIds.Count == 0)
            {
                TaskDialog.Show("Info", "The selected element has no material.");
                return Result.Cancelled;
            }

            // Just show the first material's name for this example
            ElementId materialId = materialIds.First();
            Material material = doc.GetElement(materialId) as Material;

            TaskDialog.Show("Material", $"The material of the selected element is: {material.Name}");
            return Result.Succeeded;
        }
        catch (Exception ex)
        {
            message = ex.Message;
            return Result.Failed;
        }
    }
}
}

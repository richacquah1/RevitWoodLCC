using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace RevitWoodLCC
{
    [Transaction(TransactionMode.Manual)]
    public class LifeCycleCosting : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uiDoc = commandData.Application.ActiveUIDocument;
            ICollection<ElementId> selectedIds = uiDoc.Selection.GetElementIds();

            if (selectedIds.Count == 0)
            {
                TaskDialog.Show("Warning", "Please select a building element.");
                return Result.Failed;
            }

            ElementId firstId = selectedIds.FirstOrDefault();
            if (firstId == null) return Result.Failed;

            Element firstElement = uiDoc.Document.GetElement(firstId);


            Material material = LCC_Utility.GetElementMaterial(firstElement, uiDoc.Document);

            if (material == null)
            {
                TaskDialog.Show("Warning", "No Material Assigned. Please add a material.");
                return Result.Failed;
            }


            MainWindow popup = SetupMainWindow(uiDoc, material, firstElement);
            popup.ShowDialog();

            return Result.Succeeded;
        }

        private MainWindow SetupMainWindow(UIDocument uiDoc, Material material, Element element)
        {
            var popup = new MainWindow(uiDoc.Application, uiDoc, new List<ElementId> { element.Id });

            popup.SetMaterialField(material?.Name ?? "No Material Found");
            popup.SetElementDescriptionField($"ID: {element.Id}, Name: {element.Name}");
            popup.SetProjectNameField(uiDoc.Document.ProjectInformation.Name);
            popup.SetMaterialQuantityField(LCC_Utility.GetMaterialVolume(element));

            double serviceLifeDuration = LCC_Utility.GetParameterAsDouble(element, "Element_Service Life Duration");
            popup.SetElementServiceLifeDurationField(serviceLifeDuration);

            return popup;
        }
    }
}

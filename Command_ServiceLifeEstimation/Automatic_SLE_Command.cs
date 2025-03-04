using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;

namespace RevitWoodLCC
{
    [Transaction(TransactionMode.Manual)] // Transaction attribute is required
    public class AutoEstimateServiceLife : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                UIDocument uiDoc = commandData.Application.ActiveUIDocument;
                Document doc = uiDoc.Document;

                // Prompt the user to select elements
                IList<Reference> selectedElements = uiDoc.Selection.PickObjects(ObjectType.Element, "Select elements for service life estimation.");

                if (selectedElements == null || selectedElements.Count == 0)
                {
                    TaskDialog.Show("Selection", "No elements were selected.");
                    return Result.Cancelled;
                }

                // Pass the selected elements to the UI
                Automatic_SLE dialog = new Automatic_SLE(uiDoc, selectedElements);
                dialog.ShowDialog();

                return Result.Succeeded;
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                // Handle user canceling the selection
                TaskDialog.Show("Operation Canceled", "The operation was canceled by the user.");
                return Result.Cancelled;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Error", $"An error occurred: {ex.Message}");
                return Result.Failed;
            }
        }
    }
}

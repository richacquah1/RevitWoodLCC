using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;

namespace RevitWoodLCC
{
    [Transaction(TransactionMode.Manual)]
    public class OverallLifeCycleCosting : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                // Get the UI document and the current selection
                UIDocument uiDoc = commandData.Application.ActiveUIDocument;
                UIApplication uiApp = commandData.Application;
                ICollection<ElementId> selectedIds = uiDoc.Selection.GetElementIds();

                // Open the ProjectMainWindow
                ProjectMainWindow projectMainWindow = new ProjectMainWindow(uiApp, uiDoc);
                projectMainWindow.ShowDialog();

                return Result.Succeeded;
            }
            catch (FormatException ex)
            {
                TaskDialog.Show("Error", $"Format Exception: {ex.Message}\nStack Trace: {ex.StackTrace}");
                return Result.Failed;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Error", $"General Exception: {ex.Message}\nStack Trace: {ex.StackTrace}");
                return Result.Failed;
            }
        }
    }
}

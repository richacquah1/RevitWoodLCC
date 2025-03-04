using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using Autodesk.Revit.Attributes;
using System;

namespace RevitWoodLCC.InteractiveMapWindow
{
    [Transaction(TransactionMode.Manual)]
    public class ShowInteractiveMapCommand : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            try
            {
                // Create and show the interactive map window
                InteractiveMapWindow interactiveMapWindow = new InteractiveMapWindow();
                interactiveMapWindow.ShowDialog();

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

using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace RevitWoodLCC
{
    [Transaction(TransactionMode.Manual)]
    public class SetProjectLocation : IExternalCommand
    {
        public Autodesk.Revit.UI.Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            SetProjectLocationWindow window = new SetProjectLocationWindow(commandData, "Above Ground");
            window.ShowDialog();

            if (window.DialogResult != true)
            {
                return Autodesk.Revit.UI.Result.Cancelled;
            }

            Location selectedLocation = window.SelectedLocation;

            string errorMessage;
            if (SLEUtility.TrySetProjectLocation(selectedLocation, commandData, out errorMessage))
            {
                SLEUtility.SaveLocation(selectedLocation);
            }
            else
            {
                message = errorMessage;
                return Autodesk.Revit.UI.Result.Failed;
            }

            SLEUtility.DisplayConfirmationDialog(selectedLocation);

            return Autodesk.Revit.UI.Result.Succeeded;
        }
    }
}


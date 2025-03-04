using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.Collections.Generic;
using System.Linq;

namespace RevitWoodLCC
{
    [Transaction(TransactionMode.Manual)]
    public class test3DPreviewXAML : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uiDoc = commandData.Application.ActiveUIDocument;
            ICollection<ElementId> selectedIds = uiDoc.Selection.GetElementIds();

            if (selectedIds.Count > 0)
            {
                ElementId firstId = selectedIds.First();
                Element firstElement = uiDoc.Document.GetElement(firstId);

                if (uiDoc != null)
                {
                    Preview3DForm previewForm = new Preview3DForm(uiDoc);
                    previewForm.ShowDialog();
                }
            }
            else
            {
                TaskDialog.Show("Warning", "Please select a building element.");
            }

            return Result.Succeeded;
        }
    }
}

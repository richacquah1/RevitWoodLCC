//This ode works perfect. Just uncomment this and the UI in the other .cs file and use
//using Autodesk.Revit.ApplicationServices;
//using Autodesk.Revit.Attributes;
//using Autodesk.Revit.DB;
//using Autodesk.Revit.UI;
//using Autodesk.Revit.UI.Selection;
//using System;
//using System.Collections.Generic;
//using System.Linq;

//namespace RevitWoodLCC
//{
//    [Transaction(TransactionMode.Manual)]
//    public class test3DPreview : IExternalCommand
//    {
//        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
//        {
//            UIDocument uiDoc = commandData.Application.ActiveUIDocument;
//            ICollection<SelectedElementId> selectedIds = uiDoc.Selection.GetElementIds();

//            if (selectedIds.Count > 0)
//            {
//                SelectedElementId firstId = selectedIds.First();
//                Element firstElement = uiDoc.Document.GetElement(firstId);

//                if (uiDoc != null)
//                {
//                    Preview3DForm previewForm = new Preview3DForm(uiDoc);
//                    previewForm.ShowDialog();
//                }
//            }
//            else
//            {
//                TaskDialog.Show("Warning", "Please select a building element.");
//            }

//            return Result.Succeeded;
//        }
//    }
//}

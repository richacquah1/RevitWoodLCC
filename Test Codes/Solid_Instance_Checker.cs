using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Linq;

namespace RevitWoodLCC
{ 
[Transaction(TransactionMode.ReadOnly)]
    public class CheckElementType : IExternalCommand
    {
        public Result Execute(
          ExternalCommandData commandData,
          ref string message,
          ElementSet elements)
        {
            UIDocument uiDoc = commandData.Application.ActiveUIDocument;
            Document doc = uiDoc.Document;

            try
            {
                // Get the selected element
                Reference selectedRef = uiDoc.Selection.PickObject(ObjectType.Element);
                Element selectedElement = doc.GetElement(selectedRef);

                // Get the geometry of the element
                Options geomOptions = new Options();
                GeometryElement geomElement = selectedElement.get_Geometry(geomOptions);

                // Check if geomElement is not null
                if (geomElement != null)
                {
                    // Check if it's a solid or a geometry instance
                    bool isSolid = geomElement.Any(g => g is Solid && (g as Solid).Faces.Size > 0);
                    bool isGeometryInstance = geomElement.Any(g => g is GeometryInstance);

                    TaskDialog.Show("Element Type", isSolid ? "Solid Element" : isGeometryInstance ? "Geometry Instance" : "Other");
                }
                else
                {
                    TaskDialog.Show("Element Type", "No geometric data available for this element.");
                }

                return Result.Succeeded;
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                return Result.Cancelled;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }
    }
}
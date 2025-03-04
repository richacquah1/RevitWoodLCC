using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RevitWoodLCC
{
    [Transaction(TransactionMode.Manual)]
    public class RayProjectionCode : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uiDoc = commandData.Application.ActiveUIDocument;
            Document doc = uiDoc.Document;

            try
            {
                Reference pickedRef = uiDoc.Selection.PickObject(ObjectType.Face, new FloorFaceFilter(), "Select a floor face");
                Element floorElement = doc.GetElement(pickedRef);
                Face floorFace = floorElement.GetGeometryObjectFromReference(pickedRef) as Face;

                XYZ rayStartPoint = GetPointOnFace(floorFace);
                XYZ rayDirection = XYZ.BasisZ;

                View3D view3D = Find3DView(doc);
                if (view3D == null)
                {
                    TaskDialog.Show("Error", "No 3D view found in the document");
                    return Result.Failed;
                }

                using (Transaction tx = new Transaction(doc, "Ray Projection"))
                {
                    tx.Start();

                    ReferenceIntersector intersector = new ReferenceIntersector(view3D);
                    intersector.TargetType = FindReferenceTarget.Element;

                    ReferenceWithContext nearestIntersection = intersector.FindNearest(rayStartPoint, rayDirection);
                    if (nearestIntersection == null || nearestIntersection.Proximity < 0)
                    {
                        TaskDialog.Show("Error", "No ceiling found above the selected point");
                        return Result.Failed;
                    }

                    double distanceInFeet = nearestIntersection.Proximity;
                    double distanceInMeters = distanceInFeet * 0.3048; // Convert to meters

                    XYZ intersectionPoint = rayStartPoint + rayDirection.Normalize() * distanceInFeet;
                    CreateModelLine(doc, Line.CreateBound(rayStartPoint, intersectionPoint));

                    TaskDialog.Show("Distance", $"The distance from the floor to the ceiling is: {distanceInMeters} meters");

                    tx.Commit();
                }

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }

        private XYZ GetPointOnFace(Face face)
        {
            BoundingBoxUV bbox = face.GetBoundingBox();
            UV center = bbox.Min + 0.5 * (bbox.Max - bbox.Min);
            return face.Evaluate(center);
        }

        private View3D Find3DView(Document doc)
        {
            // Use OfType<T>() to filter the collected elements
            return new FilteredElementCollector(doc)
                .OfClass(typeof(View3D))
                .OfType<View3D>() // Correct method to filter elements
                .FirstOrDefault(v => !v.IsTemplate);
        }


        private void CreateModelLine(Document doc, Line line)
        {
            DirectShape ds = DirectShape.CreateElement(doc, new ElementId(BuiltInCategory.OST_GenericModel));
            ds.ApplicationId = "RevitWoodLCC";
            ds.ApplicationDataId = "Ray";

            List<GeometryObject> geometryObjects = new List<GeometryObject> { line as GeometryObject };
            ds.SetShape(geometryObjects);
        }
    }

    public class FloorFaceFilter : ISelectionFilter
    {
        public bool AllowElement(Element elem)
        {
            return elem.Category.Id.IntegerValue == (int)BuiltInCategory.OST_Floors;
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            return true;
        }
    }
}

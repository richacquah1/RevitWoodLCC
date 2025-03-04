

//CallConvThiscall code works 


//using System.Runtime.CompilerServices;
//using Autodesk.Revit.Attributes;
//using Autodesk.Revit.DB;
//using Autodesk.Revit.UI;
//using Autodesk.Revit.UI.Selection;
//using System;
//using System.Collections.Generic;

//namespace RevitWoodLCC
//{
//    [Transaction(TransactionMode.Manual)]
//    public class RayProjectionCode_new : IExternalCommand
//    {
//        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
//        {
//            UIDocument uiDoc = commandData.Application.ActiveUIDocument;
//            Document doc = uiDoc.Document;

//            try
//            {
//                // Pick a face from the user selection
//                Reference pickedRef = uiDoc.Selection.PickObject(ObjectType.Face, "Select a face to cast rays from.");
//                Element selectedElement = doc.GetElement(pickedRef);
//                Face selectedFace = selectedElement.GetGeometryObjectFromReference(pickedRef) as Face;

//                // Generate rays from the selected face
//                List<Line> rays = GenerateRaysFromFace(selectedFace);

//                // Visualize each ray as a model line
//                foreach (var ray in rays)
//                {
//                    CreateModelLine(doc, ray);
//                }

//                // Find intersections with other faces in the document
//                List<IntersectionInfo> intersections = FindIntersections(rays, doc);

//                // Handle the intersecting data
//                foreach (var intersection in intersections)
//                {
//                    // Display the information about intersections
//                    string intersectionInfo = $"Ray intersects at Element ID: {intersection.SelectedElementId.IntegerValue} " +
//                                              $"at point: {intersection.FirstRayFaceIntersectionPoint.ToString()}";
//                    TaskDialog.Show("Intersection Info", intersectionInfo);
//                }

//                return Result.Succeeded;
//            }
//            catch (Exception ex)
//            {
//                message = ex.Message;
//                return Result.Failed;
//            }
//        }

//        private List<Line> GenerateRaysFromFace(Face face)
//        {
//            List<Line> rays = new List<Line>();

//            // Logic to generate rays from the selected face
//            // For simplicity, creating a single ray from the center of the face
//            BoundingBoxUV bbox = face.GetBoundingBox();
//            UV centerUV = (bbox.Min + bbox.Max) * 0.5;
//            XYZ centerPoint = face.Evaluate(centerUV);
//            XYZ normal = face.ComputeNormal(centerUV);

//            Line ray = Line.CreateBound(centerPoint, centerPoint + normal * 10.0); // 10 units length
//            rays.Add(ray);

//            return rays;
//        }

//        private List<IntersectionInfo> FindIntersections(IEnumerable<Line> rays, Document doc)
//        {
//            List<IntersectionInfo> intersections = new List<IntersectionInfo>();

//            // Iterate through all elements in the document
//            FilteredElementCollector collector = new FilteredElementCollector(doc).WhereElementIsNotElementType();
//            foreach (Element elem in collector)
//            {
//                GeometryElement geomElement = elem.get_Geometry(new Options());
//                if (geomElement != null)
//                {
//                    ProcessGeometry(geomElement, rays, elem.Id, intersections);
//                }
//            }

//            return intersections;
//        }

//        private void ProcessGeometry(GeometryElement geomElement, IEnumerable<Line> rays, SelectedElementId elemId, List<IntersectionInfo> intersections)
//        {
//            foreach (GeometryObject geomObj in geomElement)
//            {
//                if (geomObj is Solid solid)
//                {
//                    foreach (Face face in solid.Faces)
//                    {
//                        CheckFaceIntersections(face, rays, elemId, intersections);
//                    }
//                }
//                else if (geomObj is GeometryInstance instance)
//                {
//                    GeometryElement instanceGeom = instance.GetInstanceGeometry();
//                    ProcessGeometry(instanceGeom, rays, elemId, intersections);
//                }
//            }
//        }

//        private void CheckFaceIntersections(Face face, IEnumerable<Line> rays, SelectedElementId elemId, List<IntersectionInfo> intersections)
//        {
//            foreach (Line ray in rays)
//            {
//                IntersectionResultArray results;
//                if (face.Intersect(ray, out results) == SetComparisonResult.Overlap)
//                {
//                    foreach (IntersectionResult ir in results)
//                    {
//                        intersections.Add(new IntersectionInfo
//                        {
//                            SelectedElementId = elemId,
//                            FirstRayFaceIntersectionPoint = ir.XYZPoint
//                        });
//                    }
//                }
//            }
//        }

//        private void CreateModelLine(Document doc, Line line)
//        {
//            // Start a new transaction for creating model lines
//            using (Transaction tx = new Transaction(doc, "Create Model Line"))
//            {
//                tx.Start();

//                // Create a vector perpendicular to the line's direction
//                XYZ lineDirection = line.Direction.Normalize();
//                XYZ perpDirection = lineDirection.CrossProduct(XYZ.BasisZ);
//                if (perpDirection.IsAlmostEqualTo(XYZ.Zero)) // In case lineDirection is parallel to Z axis
//                {
//                    perpDirection = lineDirection.CrossProduct(XYZ.BasisX);
//                }

//                // Create a plane using the line's start point and the perpendicular direction
//                Plane geometryPlane = Plane.CreateByNormalAndOrigin(perpDirection, line.GetEndPoint(0));
//                SketchPlane sketchPlane = SketchPlane.Create(doc, geometryPlane);

//                // Create a model curve (line) in the document
//                doc.Create.NewModelCurve(line, sketchPlane);

//                // Commit the transaction
//                tx.Commit();
//            }
//        }



//    }




//    public class IntersectionInfo
//    {
//        public SelectedElementId SelectedElementId { get; set; }
//        public XYZ FirstRayFaceIntersectionPoint { get; set; }
//    }
//}

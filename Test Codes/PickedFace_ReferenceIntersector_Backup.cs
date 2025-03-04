////working code 
//using Autodesk.Revit.Attributes;
//using Autodesk.Revit.DB;
//using Autodesk.Revit.DB.Analysis;
//using Autodesk.Revit.DB.Visual;
//using Autodesk.Revit.UI;
//using Autodesk.Revit.UI.Selection;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Windows.Controls;


//namespace RevitWoodLCC
//{
//    [Transaction(TransactionMode.Manual)]
//    public class PickedFaceRayProjectionCode_new : IExternalCommand
//    {
//        double rayLength = 100.0; // initial value, can be adjusted later
//        private bool visualizeRays = false; // Set to 'true' to enable visualization by default
//        private StringBuilder logBuilder = new StringBuilder(); // For accumulating log messages

//        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
//        {
//            UIApplication uiApp = commandData.Application;
//            UIDocument uiDoc = uiApp.ActiveUIDocument;
//            Document doc = uiDoc.Document;

//            try
//            {

//                // Step 1: Obtain user input for simulation parameters
//                double inputDirectionDegrees = GetUserInputDirection();


//                // Prompt user to select faces
//                IList<Reference> selectedFaceRefs = uiDoc.Selection.PickObjects(ObjectType.Face, "Select faces for AVF application.");
//                if (selectedFaceRefs.Count == 0)
//                {
//                    message = "No faces selected.";
//                    return Result.Cancelled;
//                }

//                // Get and display face details
//                string faceDetails = GetFaceDetails(doc, selectedFaceRefs);
//                TaskDialog.Show("Selected Face Details", faceDetails);

//                using (Transaction tx = new Transaction(doc, "Ray Projection and AVF Application"))
//                {
//                    tx.Start();

//                    // Check for valid face references
//                    foreach (var faceRef in selectedFaceRefs)
//                    {
//                        Element faceElement = doc.GetElement(faceRef.ElementId);
//                        if (faceElement == null || !(faceElement.GetGeometryObjectFromReference(faceRef) is Face))
//                        {
//                            message = $"Invalid face reference: {faceRef.ElementId}";
//                            return Result.Failed;
//                        }
//                    }


//                    // Verify the active view is appropriate
//                    View3D view3D = doc.ActiveView as View3D;
//                    if (view3D == null || !view3D.IsSectionBoxActive)
//                    {
//                        message = "Please make sure you are in a 3D view with an active section box.";
//                        return Result.Failed;
//                    }

//                    BoundingBoxXYZ sectionBox = view3D.GetSectionBox();

//                    // Step 2: Create and visualize the simulation plane
//                    // Corrected call to CreateAndVisualizeSimulationPlane
//                    SimulationPlaneInfo simulationInfo = CreateAndVisualizeSimulationPlane(doc, sectionBox, inputDirectionDegrees);

//                    double factor = 4;
//                    int rayDensity = CalculateRayDensity(view3D, factor);

//                    // Generate and visualize rays from the plane face
//                    List<IdentifiedRay> rays = GenerateAndVisualizeRaysFromFace(simulationInfo.PlaneFace, doc, rayDensity, selectedFaceRefs);

//                    // Find the first intersections of rays with the selected faces
//                    List<IntersectionInfo> firstRayFaceIntersections = FindFirstRayFaceIntersections(doc, rays, selectedFaceRefs);

//                    // Display ray-face intersections in a TaskDialog
//                    DisplayRayFaceIntersectionsInDialog(doc, rays, selectedFaceRefs);

//                    SpatialFieldManager sfm = SpatialFieldManager.GetSpatialFieldManager(doc.ActiveView);
//                    if (sfm == null)
//                    {
//                        sfm = SpatialFieldManager.CreateSpatialFieldManager(doc.ActiveView, 1);
//                    }
//                    else
//                    {
//                        sfm.Clear(); // Clear existing AVF data
//                    }

//                    AnalysisDisplayStyle analysisDisplayStyle = CreateDefaultAnalysisDisplayStyle(doc, "Custom AVF Style");
//                    doc.ActiveView.AnalysisDisplayStyleId = analysisDisplayStyle.Id;

//                    // Apply AVF to selected faces
//                    Dictionary<Reference, List<XYZ>> faceIntersectionPoints = DisplayRayFaceIntersectionsInDialog(doc, rays, selectedFaceRefs);
//                    ApplyAVFToSelectedFaces(doc, faceIntersectionPoints);

//                    tx.Commit();
//                }


//                return Result.Succeeded;
//            }
//            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
//            {
//                message = "Operation canceled by the user.";
//                return Result.Cancelled;
//            }
//            catch (Exception ex)
//            {
//                message = $"Unexpected error occurred: {ex.Message}\n{ex.StackTrace}";
//                TaskDialog.Show("Error", message);
//                return Result.Failed;
//            }
//        }


//        private string GetFaceDetails(Document doc, IList<Reference> faceRefs)
//        {
//            StringBuilder faceDetails = new StringBuilder();
//            foreach (var faceRef in faceRefs)
//            {
//                Element faceElement = doc.GetElement(faceRef.ElementId);
//                if (faceElement != null)
//                {
//                    faceDetails.AppendLine($"Element ID: {faceElement.Id}, Face Reference: {faceRef.ConvertToStableRepresentation(doc)}");
//                }
//            }
//            return faceDetails.ToString();
//        }


//        private SimulationPlaneInfo CreateAndVisualizeSimulationPlane(Document doc, BoundingBoxXYZ sectionBox, double inputDirectionDegrees)
//        {
//            SimulationPlaneInfo simulationInfo = new SimulationPlaneInfo();
//            // Calculate the center of the section box
//            XYZ sectionCenter = new XYZ(
//                (sectionBox.Min.X + sectionBox.Max.X) / 2,
//                (sectionBox.Min.Y + sectionBox.Max.Y) / 2,
//                (sectionBox.Min.Z + sectionBox.Max.Z) / 2);

//            // Determine the longest length of the section box
//            double longestLength = Math.Max(sectionBox.Max.X - sectionBox.Min.X, sectionBox.Max.Y - sectionBox.Min.Y);

//            // Define additional height (e.g., for elevation above the section box)
//            double additionalHeight = 3.0; // Example value, can be adjusted or made user input

//            // Calculate the side length and radius of the hexadecagon
//            double sideLength = longestLength + additionalHeight;
//            double hexadecagonRadius = CalculateHexadecagonRadius(sideLength) / 2;

//            // Calculate the center point for the simulation plane
//            XYZ planeCenter = GenerateSimulationPlaneCenter(sectionCenter, hexadecagonRadius, inputDirectionDegrees);

//            // Set the plane size (width and height)
//            double planeSize = sideLength; // Width of the simulation plane
//            double planeHeight = sideLength; // Height of the simulation plane

//            // Define the height for the simulation planes
//            double simulationPlaneHeight = 10.0; // Example value

//            // Define the inclination angle of the plane
//            double inclinationAngle = 30.0; // Example value, can be adjusted or made user input

//            // Create the simulation plane geometry
//            // Note: The geometry creation is now assumed to be happening within the context of an active transaction.

//            // Create the simulation plane geometry
//            // Assuming you have a SimulationPlaneInfo instance named simulationInfo
//            CreateSimulationPlaneGeometry(doc, planeCenter, planeSize, planeHeight, sectionCenter, simulationPlaneHeight, inclinationAngle, inputDirectionDegrees, simulationInfo);


//            // Assuming CreateSimulationPlaneGeometry method will set the SimulationShapeId of simulationInfo
//            // to the newly created DirectShape's Id.
//            simulationInfo.PlaneFace = null; // Set this appropriately within the CreateSimulationPlaneGeometry method
//            simulationInfo.PlaneOrigin = planeCenter;
//            simulationInfo.PlaneLength = planeSize;
//            simulationInfo.PlaneWidth = planeHeight;
//            // The SimulationShapeId should be set inside the CreateSimulationPlaneGeometry method after the DirectShape is created.

//            return simulationInfo;
//        }


//        // Calculate hexadecagon radius
//        private double CalculateHexadecagonRadius(double sideLength)
//        {
//            const int sides = 16;
//            return sideLength / (2 * Math.Sin(Math.PI / sides));
//        }

//        // Generate the simulation plane center
//        private XYZ GenerateSimulationPlaneCenter(XYZ center, double radius, double directionDegrees)
//        {
//            double angleInRadians = directionDegrees * (Math.PI / 180.0);
//            double y = center.Y + radius * Math.Cos(angleInRadians);
//            double x = center.X + radius * Math.Sin(angleInRadians);
//            return new XYZ(x, y, center.Z);
//        }

//        // Create the simulation plane geometry
//        private void CreateSimulationPlaneGeometry(
//            Document doc,
//            XYZ planeCenter,
//            double planeSize,
//            double planeHeight,
//            XYZ sectionCenter,
//            double simulationPlaneHeight,
//            double inclinationAngle,
//            double inputDirectionDegrees,
//            SimulationPlaneInfo simulationInfo) // Pass simulationInfo to modify directly
//        {
//            // Adjust the Z coordinate of the plane center by the simulationPlaneHeight
//            XYZ elevatedCenter = new XYZ(planeCenter.X, planeCenter.Y, sectionCenter.Z + simulationPlaneHeight);

//            // Calculate the normal to the plane
//            XYZ normal = new XYZ(planeCenter.X - sectionCenter.X, planeCenter.Y - sectionCenter.Y, 0).Normalize();
//            XYZ verticalNormal = new XYZ(0, 0, 1); // Vertical normal pointing upwards
//            XYZ inclinedNormal = GetInclinedNormal(verticalNormal, inclinationAngle, inputDirectionDegrees);

//            // Calculate the directions for the plane's edges
//            XYZ up = new XYZ(0, 0, 1); // Up direction is along Z-axis
//            XYZ right = up.CrossProduct(normal).Normalize();

//            // Define the corners of the plane
//            XYZ p1 = elevatedCenter - right * planeSize / 2 + up * planeHeight / 2;
//            XYZ p2 = elevatedCenter + right * planeSize / 2 + up * planeHeight / 2;
//            XYZ p3 = elevatedCenter + right * planeSize / 2 - up * planeHeight / 2;
//            XYZ p4 = elevatedCenter - right * planeSize / 2 - up * planeHeight / 2;

//            // Create lines for the edges of the plane
//            Line edge1 = Line.CreateBound(p1, p2);
//            Line edge2 = Line.CreateBound(p2, p3);
//            Line edge3 = Line.CreateBound(p3, p4);
//            Line edge4 = Line.CreateBound(p4, p1);

//            // Create a curve loop for the plane's boundary
//            CurveLoop planeBoundary = new CurveLoop();
//            planeBoundary.Append(edge1);
//            planeBoundary.Append(edge2);
//            planeBoundary.Append(edge3);
//            planeBoundary.Append(edge4);

//            // Create a solid extrusion for the simulation plane
//            double extrusionDepth = 0.1; // Thickness of the plane
//            Solid planeSolid = GeometryCreationUtilities.CreateExtrusionGeometry(new List<CurveLoop> { planeBoundary }, inclinedNormal, extrusionDepth);

//            // Create a DirectShape element using the solid geometry
//            DirectShape ds = DirectShape.CreateElement(doc, new ElementId(BuiltInCategory.OST_GenericModel));
//            simulationInfo.SimulationShapeId = ds.Id;
//            ds.SetShape(new List<GeometryObject> { planeSolid });
//            ds.Name = "Simulation Plane";

//            // Get the geometry element of the DirectShape
//            GeometryElement geomElem = ds.get_Geometry(new Options());
//            foreach (GeometryObject obj in geomElem)
//            {
//                // Check if the object is a Solid
//                if (obj is Solid solid)
//                {
//                    foreach (Face face in solid.Faces)
//                    {
//                        // Perform operations with the face
//                        // ...
//                    }
//                }
//            }

//        }

//        // Calculate the inclined normal based on inclination angle and input direction
//        private XYZ GetInclinedNormal(XYZ originalNormal, double inclinationAngle, double directionDegrees)
//        {
//            // Convert inclination angle and direction to radians
//            double angleRadians = inclinationAngle * (Math.PI / 180.0);
//            double directionRadians = directionDegrees * (Math.PI / 180.0);

//            // Define the axis of rotation (around the Z-axis)
//            XYZ rotationAxis = new XYZ(Math.Cos(directionRadians), Math.Sin(directionRadians), 0);

//            // Rotate the original normal around the axis by the inclination angle
//            Transform rotation = Transform.CreateRotation(rotationAxis, angleRadians);
//            XYZ inclinedNormal = rotation.OfVector(originalNormal);

//            return inclinedNormal;
//        }





//        private List<IdentifiedRay> GenerateAndVisualizeRaysFromFace(Face planeFace, Document doc, int rayDensity, IList<Reference> faceRefs)
//        {

//            // Check for null references first and handle them accordingly
//            if (planeFace == null)
//            {
//                throw new ArgumentNullException(nameof(planeFace), "The plane face cannot be null.");
//            }
//            if (doc == null)
//            {
//                throw new ArgumentNullException(nameof(doc), "The document cannot be null.");
//            }
//            if (faceRefs == null)
//            {
//                throw new ArgumentNullException(nameof(faceRefs), "The list of face references cannot be null.");
//            }

//            BoundingBoxUV bbox = planeFace.GetBoundingBox();
//            double uStep = (bbox.Max.U - bbox.Min.U) / rayDensity;
//            double vStep = (bbox.Max.V - bbox.Min.V) / rayDensity;

//            List<IdentifiedRay> identifiedRays = new List<IdentifiedRay>();
//            int rayCounter = 0;

//            for (int i = 0; i <= rayDensity; i++)
//            {
//                for (int j = 0; j <= rayDensity; j++)
//                {
//                    UV pointUV = new UV(bbox.Min.U + i * uStep, bbox.Min.V + j * vStep);
//                    XYZ point = planeFace.Evaluate(pointUV);
//                    XYZ normal = planeFace.ComputeNormal(pointUV);
//                    XYZ endPoint = point - normal * rayLength; // Assuming downward direction is correct

//                    // Append ray details to logBuilder
//                    logBuilder.AppendLine($"Ray {rayCounter}: Start {point.ToString()}, End {endPoint.ToString()}, Direction {normal.ToString()}");

//                    Line rayLine = Line.CreateBound(point, endPoint);
//                    IdentifiedRay identifiedRay = new IdentifiedRay
//                    {
//                        Ray = rayLine,
//                        RayId = $"Ray_{rayCounter++}"
//                    };

//                    if (identifiedRay.Ray != null && identifiedRay.Ray.Length > doc.Application.ShortCurveTolerance)
//                    {
//                        identifiedRays.Add(identifiedRay);
//                    }
//                }
//            }

//            // Visualize the rays if the flag is set to true
//            if (visualizeRays)
//            {
//                VisualizeRays(identifiedRays, doc, faceRefs);
//            }

//            return identifiedRays;
//        }


//        private void VisualizeRays(List<IdentifiedRay> rays, Document doc, IList<Reference> selectedFaceRefs)
//        {
//            // Visualization logic for the rays. This can be adjusted based on how you want to visualize the rays in relation to the selected faces.
//            foreach (IdentifiedRay identifiedRay in rays)
//            {
//                Line rayLine = identifiedRay.Ray;
//                if (rayLine != null && rayLine.Length > doc.Application.ShortCurveTolerance)
//                {
//                    DirectShape ds = DirectShape.CreateElement(doc, new ElementId(BuiltInCategory.OST_Lines));
//                    ds.ApplicationId = "Rays Visualization";
//                    ds.ApplicationDataId = identifiedRay.RayId; // Use RayId for ApplicationDataId
//                    ds.SetShape(new List<GeometryObject> { rayLine });
//                }
//            }
//        }

//        private double CalculateDistance(Face face, XYZ point)
//        {
//            IntersectionResult result = face.Project(point);
//            double distanceInFeet = result.Distance;

//            // Convert distance from feet to meters
//            double distanceInMeters = distanceInFeet * 0.3048; // Since 1 foot = 0.3048 meters

//            return distanceInMeters;
//        }

//        public int CalculateRayDensity(View3D view3D, double factor)
//        {
//            // Example calculation (modify as needed for your simulation)
//            // This could be a dynamic value based on view complexity or other factors
//            // 'factor' could be a user-defined parameter or derived from the model's characteristics
//            int baseDensity = 1; // A base value for ray density

//            // Adjust the density based on the factor (e.g., complexity, user input, etc.)
//            return baseDensity + (int)(factor * 5); // Just an example calculation
//        }

//        public double CalculateRaySpacing(double planeLength, double planeWidth, int rayDensity)
//        {
//            // Calculate the total area of the plane
//            double planeArea = planeLength * planeWidth;

//            // Determine the spacing needed to achieve the desired density
//            // Assuming a uniform distribution of rays over the plane
//            return Math.Sqrt(planeArea / rayDensity);
//        }

//        private List<IntersectionInfo> FindFirstRayFaceIntersections(Document doc, List<IdentifiedRay> rays, IList<Reference> faceRefs)
//        {
//            var firstRayFaceIntersections = new List<IntersectionInfo>();
//            StringBuilder resultsBuilder = new StringBuilder();

//            foreach (IdentifiedRay ray in rays)
//            {
//                List<IntersectionDetail> intersectionDetails = new List<IntersectionDetail>();

//                foreach (Reference faceRef in faceRefs)
//                {
//                    Face face = doc.GetElement(faceRef.ElementId).GetGeometryObjectFromReference(faceRef) as Face;
//                    if (face == null) continue;

//                    IntersectionResultArray results;
//                    SetComparisonResult intersectResult = face.Intersect(ray.Ray, out results);

//                    if (intersectResult == SetComparisonResult.Overlap)
//                    {
//                        foreach (IntersectionResult result in results)
//                        {
//                            double distance = result.XYZPoint.DistanceTo(ray.Ray.GetEndPoint(0));
//                            intersectionDetails.Add(new IntersectionDetail
//                            {
//                                IntersectionResult = result,
//                                FaceReference = faceRef,
//                                Distance = distance
//                            });
//                        }
//                    }
//                }

//                if (intersectionDetails.Any())
//                {
//                    var closestIntersection = intersectionDetails.OrderBy(i => i.Distance).First();

//                    firstRayFaceIntersections.Add(new IntersectionInfo
//                    {
//                        ElementId = closestIntersection.FaceReference.ElementId,
//                        Face = doc.GetElement(closestIntersection.FaceReference.ElementId).GetGeometryObjectFromReference(closestIntersection.FaceReference) as Face,
//                        FaceReference = closestIntersection.FaceReference,
//                        IntersectionPoint = closestIntersection.IntersectionResult.XYZPoint,
//                        Distance = closestIntersection.Distance,
//                        RayId = ray.RayId
//                    });

//                    // Add intersection details to the StringBuilder
//                    resultsBuilder.AppendLine($"Ray ID: {ray.RayId}, Element ID: {closestIntersection.FaceReference.ElementId}, " +
//                                              $"Face Reference: {closestIntersection.FaceReference.ConvertToStableRepresentation(doc)}, " +
//                                              $"Intersection Point: {closestIntersection.IntersectionResult.XYZPoint}, Distance: {closestIntersection.Distance}");
//                }
//            }

//            // Display the results in a TaskDialog
//            TaskDialog.Show("First Ray-Face Intersection Results", resultsBuilder.ToString());

//            return firstRayFaceIntersections;
//        }

//        // Helper class for sorting intersection details
//        private class IntersectionDetail
//        {
//            public IntersectionResult IntersectionResult { get; set; }
//            public Reference FaceReference { get; set; }
//            public double Distance { get; set; }
//        }

//        private List<Reference> GetFirstRayFaceIntersectionsReference(List<IntersectionInfo> firstRayFaceIntersections)
//        {
//            var firstRayFaceIntersectionFaceRefs = new List<Reference>();

//            foreach (var intersection in firstRayFaceIntersections)
//            {
//                if (intersection != null && intersection.FaceReference != null)
//                {
//                    firstRayFaceIntersectionFaceRefs.Add(intersection.FaceReference);
//                }
//            }

//            return firstRayFaceIntersectionFaceRefs;
//        }

//        private Dictionary<Reference, List<XYZ>> DisplayRayFaceIntersectionsInDialog(Document doc, List<IdentifiedRay> rays, IList<Reference> faceRefs)
//        {
//            var faceToFirstIntersectionsMap = new Dictionary<Reference, List<(XYZ IntersectionPoint, string RayId)>>();
//            var faceIntersectionPoints = new Dictionary<Reference, List<XYZ>>();

//            foreach (IdentifiedRay ray in rays)
//            {
//                IntersectionInfo closestIntersectionInfo = null;
//                foreach (Reference faceRef in faceRefs)
//                {
//                    Face face = doc.GetElement(faceRef.ElementId).GetGeometryObjectFromReference(faceRef) as Face;
//                    if (face == null) continue;

//                    IntersectionResultArray results;
//                    SetComparisonResult intersectResult = face.Intersect(ray.Ray, out results);

//                    if (intersectResult == SetComparisonResult.Overlap)
//                    {
//                        foreach (IntersectionResult result in results)
//                        {
//                            var distance = result.XYZPoint.DistanceTo(ray.Ray.GetEndPoint(0));
//                            if (closestIntersectionInfo == null || distance < closestIntersectionInfo.Distance)
//                            {
//                                closestIntersectionInfo = new IntersectionInfo
//                                {
//                                    ElementId = faceRef.ElementId,
//                                    Face = face,
//                                    IntersectionPoint = result.XYZPoint,
//                                    Distance = distance,
//                                    FaceReference = faceRef,
//                                    RayId = ray.RayId
//                                };
//                            }
//                        }
//                    }
//                }

//                if (closestIntersectionInfo != null)
//                {
//                    if (!faceToFirstIntersectionsMap.ContainsKey(closestIntersectionInfo.FaceReference))
//                    {
//                        faceToFirstIntersectionsMap[closestIntersectionInfo.FaceReference] = new List<(XYZ IntersectionPoint, string RayId)>();
//                    }

//                    faceToFirstIntersectionsMap[closestIntersectionInfo.FaceReference].Add((closestIntersectionInfo.IntersectionPoint, closestIntersectionInfo.RayId));
//                }
//            }

//            // Build the string for the TaskDialog grouping by face and populate faceIntersectionPoints
//            StringBuilder dialogContent = new StringBuilder();
//            foreach (var faceRef in faceToFirstIntersectionsMap.Keys)
//            {
//                ElementId faceElementId = faceRef.ElementId;
//                string faceRefString = faceRef.ConvertToStableRepresentation(doc);
//                dialogContent.AppendLine($"Face Reference: {faceRefString}, Element ID: {faceElementId}");
//                dialogContent.AppendLine("First Intersection Points and Ray IDs:");

//                var intersectionPoints = new List<XYZ>();
//                foreach (var intersection in faceToFirstIntersectionsMap[faceRef])
//                {
//                    dialogContent.AppendLine($"Point: {intersection.IntersectionPoint}, Ray ID: {intersection.RayId}");
//                    intersectionPoints.Add(intersection.IntersectionPoint);
//                }
//                faceIntersectionPoints[faceRef] = intersectionPoints;

//                dialogContent.AppendLine("-----");
//            }

//            // Show the TaskDialog
//            TaskDialog.Show("First Ray-Face Intersection Details", dialogContent.ToString());

//            return faceIntersectionPoints;
//        }




//        private Face GetFaceFromReference(Document doc, Reference faceRef)
//        {
//            Element element = doc.GetElement(faceRef.ElementId);
//            if (element != null)
//            {
//                return element.GetGeometryObjectFromReference(faceRef) as Face;
//            }
//            return null;
//        }

//        private void ApplyAVFToSelectedFaces(Document doc, Dictionary<Reference, List<XYZ>> faceIntersectionPoints)
//        {
//            foreach (var entry in faceIntersectionPoints)
//            {
//                Reference faceRef = entry.Key;
//                List<XYZ> intersections = entry.Value;

//                Face face = GetFaceFromReference(doc, faceRef);
//                if (face != null)
//                {
//                    SetUpAndApplyAVF(doc, face, faceRef, intersections);
//                }
//            }
//        }


//        private void SetUpAndApplyAVF(Document doc, Face face, Reference faceRef, List<XYZ> intersections)
//        {
//            // Create or get SpatialFieldManager for the active view
//            SpatialFieldManager sfm = SpatialFieldManager.GetSpatialFieldManager(doc.ActiveView);
//            if (sfm == null)
//            {
//                sfm = SpatialFieldManager.CreateSpatialFieldManager(doc.ActiveView, 1);
//            }

//            // Create an AnalysisDisplayStyle
//            AnalysisDisplayStyle analysisDisplayStyle = CreateDefaultAnalysisDisplayStyle(doc, "Custom AVF Style");

//            // Set the AnalysisDisplayStyle to the active view
//            doc.ActiveView.AnalysisDisplayStyleId = analysisDisplayStyle.Id;

//            // Register a new result schema with the SpatialFieldManager
//            AnalysisResultSchema resultSchema = new AnalysisResultSchema("Custom Schema", "Description");
//            int schemaIndex = GetOrCreateAnalysisResultSchemaIndex(sfm, "Custom Schema", "Description");

//            // Prepare data for AVF
//            IList<UV> uvPoints;
//            FieldDomainPointsByUV fieldPoints = GetFieldDomainPointsByUV(face, out uvPoints);
//            FieldValues fieldValues = GetFieldValuesForIntersections(uvPoints, intersections, face);

//            // Add or Update the spatial field primitive for the face
//            int primitiveId = sfm.AddSpatialFieldPrimitive(faceRef);
//            sfm.UpdateSpatialFieldPrimitive(primitiveId, fieldPoints, fieldValues, schemaIndex);
//        }

//        private FieldValues GetFieldValuesForIntersections(IList<UV> uvPoints, List<XYZ> intersections, Face face)
//        {
//            var values = new List<ValueAtPoint>();
//            double proximityThreshold = 0.26; // Adjust this threshold as needed

//            foreach (UV uv in uvPoints)
//            {
//                XYZ point = face.Evaluate(uv);
//                double nearestDistance = intersections.Min(intersect => point.DistanceTo(intersect));
//                double value = nearestDistance <= proximityThreshold ? 1 : 0;

//                values.Add(new ValueAtPoint(new List<double> { value }));
//            }

//            return new FieldValues(values);
//        }



//        private UV GetUVPoint(Face face, XYZ xyzPoint)
//        {
//            IntersectionResult result = face.Project(xyzPoint);
//            if (result == null) return null;
//            return result.UVPoint;
//        }

//        private int GetOrCreateAnalysisResultSchemaIndex(SpatialFieldManager sfm, string schemaName, string schemaDescription)
//        {
//            foreach (int index in sfm.GetRegisteredResults())
//            {
//                AnalysisResultSchema existingSchema = sfm.GetResultSchema(index);
//                if (existingSchema.Name.Equals(schemaName))
//                {
//                    return index;
//                }
//            }

//            AnalysisResultSchema newSchema = new AnalysisResultSchema(schemaName, schemaDescription);
//            return sfm.RegisterResult(newSchema);
//        }

//        private AnalysisDisplayStyle CreateDefaultAnalysisDisplayStyle(Document doc, string styleName)
//        {
//            var existingStyle = new FilteredElementCollector(doc)
//                .OfClass(typeof(AnalysisDisplayStyle))
//                .Cast<AnalysisDisplayStyle>()
//                .FirstOrDefault(style => style.Name.Equals(styleName));

//            if (existingStyle != null)
//                return existingStyle;

//            AnalysisDisplayColoredSurfaceSettings surfaceSettings = new AnalysisDisplayColoredSurfaceSettings
//            {
//                ShowGridLines = false
//            };

//            AnalysisDisplayColorSettings colorSettings = new AnalysisDisplayColorSettings
//            {
//                MaxColor = new Color(173, 216, 230), // Light Blue (RGB: 173, 216, 230)
//                MinColor = new Color(255, 255, 255)  // White (RGB: 255, 255, 255)
//            };

//            AnalysisDisplayLegendSettings legendSettings = new AnalysisDisplayLegendSettings
//            {
//                ShowLegend = true,
//                NumberOfSteps = 10,             // Adjust the number of steps in the legend
//                ShowDataDescription = true,     // Show or hide data description
//                Rounding = 0.1                  // Set rounding for values
//                                                // Note: Direct control over legend size is not available
//            };

//            return AnalysisDisplayStyle.CreateAnalysisDisplayStyle(doc, styleName, surfaceSettings, colorSettings, legendSettings);
//        }




//        private FieldDomainPointsByUV GetFieldDomainPointsByUV(Face face, out IList<UV> uvPoints)
//        {
//            uvPoints = new List<UV>();
//            BoundingBoxUV bbox = face.GetBoundingBox();
//            double uStep = (bbox.Max.U - bbox.Min.U) / 10; // 10 steps across the U direction
//            double vStep = (bbox.Max.V - bbox.Min.V) / 10; // 10 steps across the V direction

//            for (double u = bbox.Min.U; u <= bbox.Max.U; u += uStep)
//            {
//                for (double v = bbox.Min.V; v <= bbox.Max.V; v += vStep)
//                {
//                    UV uv = new UV(u, v);
//                    if (face.IsInside(uv))
//                    {
//                        uvPoints.Add(uv);
//                    }
//                }
//            }

//            return new FieldDomainPointsByUV(uvPoints);
//        }



//        private FieldValues GetFieldValues(IList<UV> uvPoints)
//        {
//            IList<ValueAtPoint> values = new List<ValueAtPoint>();
//            foreach (UV uv in uvPoints)
//            {
//                double sampleValue = uv.U + uv.V; // Replace this with actual analysis value
//                values.Add(new ValueAtPoint(new List<double> { sampleValue }));
//            }

//            return new FieldValues(values);
//        }


//        // Step 3: Define the method to get user input for the direction
//        private double GetUserInputDirection()
//        {
//            // Define the valid directions in degrees (corresponding to cardinal directions)
//            double[] validDirections = new double[]
//            {
//        0.0, 22.5, 45.0, 67.5, 90.0, 112.5, 135.0, 157.5,
//        180.0, 202.5, 225.0, 247.5, 270.0, 292.5, 315.0, 337.5
//            };

//            // Prompt the user to enter the wind direction
//            string userInput = Microsoft.VisualBasic.Interaction.InputBox(
//                "Enter the wind direction in degrees (one of the 16 cardinal angles only):\n" +
//                "0, 22.5, 45, 67.5, 90, 112.5, 135, 157.5, 180, 202.5, 225, 247.5, 270, 292.5, 315, 337.5",
//                "Wind Direction Input",
//                "0");

//            // Attempt to parse the user input into a double
//            if (double.TryParse(userInput, out double inputDirection))
//            {
//                // Normalize 360 degrees to 0 degrees
//                if (inputDirection == 360.0) inputDirection = 0.0;

//                // Check if the input direction is one of the valid cardinal directions
//                if (validDirections.Contains(inputDirection))
//                {
//                    return inputDirection;
//                }
//                else
//                {
//                    Autodesk.Revit.UI.TaskDialog.Show("Invalid Input", "The value entered is not a valid cardinal direction.");
//                    return GetUserInputDirection(); // Recursively call the method for valid input
//                }
//            }
//            else
//            {
//                Autodesk.Revit.UI.TaskDialog.Show("Invalid Input", "Please enter a numeric value.");
//                return GetUserInputDirection(); // Recursively call the method for valid input
//            }
//        }


//    }

//    // Updated IntersectionInfo class to include distance
//    public class IntersectionInfo
//    {
//        public ElementId ElementId { get; set; }
//        public XYZ IntersectionPoint { get; set; }
//        public Face Face { get; set; }
//        public Reference FaceReference { get; set; }
//        public double Distance { get; set; }
//        public string RayId { get; set; }
//        public string ElementType { get; set; } // Add this line to store the element type
//        public string GeometryType { get; set; } // New property to store the type of geometry
//        public Reference IntersectingFaceRef { get; set; } // New property for intersecting face reference
//    }


//    public class SimulationPlaneInfo
//    {
//        public Face PlaneFace { get; set; }
//        public XYZ PlaneOrigin { get; set; }
//        public double PlaneLength { get; set; }
//        public double PlaneWidth { get; set; }
//        public ElementId SimulationShapeId { get; set; } // Add this line
//    }

//    public class IdentifiedRay
//    {
//        public Line Ray { get; set; }
//        public string RayId { get; set; }
//        public ElementId ElementId { get; set; } // Add this line if ElementId should be a part of IdentifiedRay
//    }
//}

//// Most Recent 


//using Autodesk.Revit.Attributes;
//using Autodesk.Revit.DB;
//using Autodesk.Revit.DB.Analysis;
//using Autodesk.Revit.DB.Visual;
//using Autodesk.Revit.UI;
//using Autodesk.Revit.UI.Selection;
//using Microsoft.VisualBasic;
//using System;
//using System.Collections.Generic;
//using System.Globalization;
//using System.Linq;
//using System.Text;
//using System.Windows.Controls;
//using System.Windows.Forms;
//using System.Windows.Media.Media3D;
//using static IronPython.Modules._ast;
//using System.Diagnostics;
//using System.Threading.Tasks;
//using System.Collections.Concurrent;
//using System.Threading; // For Interlocked



//namespace RevitWoodLCC
//{
//    [Transaction(TransactionMode.Manual)]
//    public class PickedFaceRayProjectionCode_new : IExternalCommand
//    {
//        // Cardinal directions and their corresponding angles
//        private readonly double[] directionAngles = new double[] {
//            0.0, 22.5, 45.0, 67.5, 90.0, 112.5, 135.0, 157.5,
//            180.0, 202.5, 225.0, 247.5, 270.0, 292.5, 315.0, 337.5
//        };

//        private StringBuilder logBuilder = new StringBuilder(); // For accumulating log messages

//        // Booleans to control ray generation
//        bool generateOriginalRays = false /* true or false */;
//        bool generateAdditionalRays = true /* true or false */;


//        private double windSpeed; // Wind speed in m/s
//        private double raindropRadius; // Raindrop radius in meters

//        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
//        {
//            Stopwatch stopwatch = new Stopwatch();
//            stopwatch.Start();

//            UIApplication uiApp = commandData.Application;
//            UIDocument uiDoc = uiApp.ActiveUIDocument;
//            Document doc = uiDoc.Document;


//            try
//            {
//                // Ask user for ray generation preferences
//                generateOriginalRays = GetUserInputForFlag("Generate original rays?", "Ray Generation");
//                generateAdditionalRays = GetUserInputForFlag("Generate additional rays?", "Ray Generation");


//                // Prompt user to select faces
//                IList<Reference> selectedFaceRefs = uiDoc.Selection.PickObjects(ObjectType.Face, "Select faces for AVF application.");
//                if (selectedFaceRefs.Count == 0)
//                {
//                    message = "No faces selected.";
//                    return Result.Cancelled;
//                }

//                // Get and display face details
//                string faceDetails = GetFaceDetails(doc, selectedFaceRefs);
//                TaskDialog.Show("Selected Face Details", faceDetails);

//                // Verify the active view is appropriate
//                View3D view3D = doc.ActiveView as View3D;
//                if (view3D == null || !view3D.IsSectionBoxActive)
//                {
//                    message = "Please make sure you are in a 3D view with an active section box.";
//                    return Result.Failed;
//                }

//                BoundingBoxXYZ sectionBox = view3D.GetSectionBox();
//                XYZ sectionTopCenter = GetSectionBoxTopCenter(sectionBox);
//                double width = sectionBox.Max.X - sectionBox.Min.X;
//                double depth = sectionBox.Max.Y - sectionBox.Min.Y;

//                double simulationPlaneHeight = GetUserInputHeight(sectionTopCenter.Z);
//                double rotationDegrees = GetHexadecagonRotationAngle();

//                windSpeed = GetUserInputWindSpeed(windSpeed);
//                raindropRadius = GetUserInputRaindropRadius(raindropRadius);

//                // Call the CalculateInclinationAngle method with showDebug set to true
//                var results = CalculateInclinationAngle(windSpeed, raindropRadius, true);

//                // Now you can use the results as needed
//                double inclinationAngleRadians = results.InclinationRadians;
//                double inclinationAngleDegrees = results.InclinationDegrees;
//                //double terminalVelocity = results.TerminalVelocity;

//                double angleRadians = rotationDegrees * (Math.PI / 180.0);
//                XYZ right = new XYZ(Math.Sin(angleRadians), Math.Cos(angleRadians), 0);

//                double spacingInMillimeters = 80; // 80 Define the desired spacing in millimeters
//                double rayLength = 100.0; // Define the length of the rays
//                bool visualizeRays = false; // Set to 'true' to enable visualization by default

//                // Calculate inclination angle and terminal velocity
//                //var (inclinationRadians, inclinationDegrees, terminalVelocity) = CalculateInclinationAngle(windSpeed, raindropRadius);
//                //TaskDialog.Show("Inclination Angle and Terminal Velocity",
//                //    $"Inclination Angle: {inclinationDegrees} degrees ({inclinationRadians} radians)\n" +
//                //    $"Terminal Velocity: {terminalVelocity} m/s");

//                using (Transaction tx = new Transaction(doc, "Ray Projection and AVF Application"))
//                {
//                    tx.Start();

//                    // Check for valid face references
//                    foreach (var faceRef in selectedFaceRefs)
//                    {
//                        Element faceElement = doc.GetElement(faceRef.ElementId);
//                        if (faceElement == null || !(faceElement.GetGeometryObjectFromReference(faceRef) is Face))
//                        {
//                            message = $"Invalid face reference: {faceRef.ElementId}";
//                            return Result.Failed;
//                        }
//                    }

//                    SimulationPlaneInfo simulationPlaneInfo = CreateSimulationPlane(doc, sectionTopCenter, width, depth,
//                                                                                    simulationPlaneHeight, rotationDegrees, view3D);

//                    PlanarFace planeFace = simulationPlaneInfo.PlaneFace as PlanarFace;

//                    // Ensure that planeFace is not null before proceeding
//                    if (planeFace == null)
//                    {
//                        TaskDialog.Show("Error", "Failed to obtain the simulation plane face.");
//                        return Result.Failed;
//                    }

//                    // Generate and visualize rays from the plane face
//                    List<IdentifiedRay> rays = GenerateAndVisualizeRaysFromFace(
//                        planeFace,
//                        doc,
//                        spacingInMillimeters, // Pass the spacing parameter
//                        rayLength,
//                        visualizeRays,
//                        right,
//                        rotationDegrees,
//                        generateOriginalRays,
//                        generateAdditionalRays,
//                        selectedFaceRefs);


//                    // Create grids for faces
//                    var faceGrids = CreateGridsForFaces(doc, selectedFaceRefs, spacingInMillimeters);

//                    // Count ray hits per grid
//                    CountRayHitsPerGrid(doc, rays, faceGrids);

//                    // Optional: Log the completion of grid hit counting
//                    logBuilder.AppendLine($"Completed counting ray hits per grid for {faceGrids.Count} faces.");


//                    // Display ray-face intersections in a TaskDialog (if needed)
//                    Dictionary<Reference, List<XYZ>> faceIntersectionPoints = DisplayRayFaceIntersectionsInDialog(doc, rays, selectedFaceRefs);

//                    // Set up AVF
//                    SpatialFieldManager sfm = SpatialFieldManager.GetSpatialFieldManager(doc.ActiveView);
//                    if (sfm == null)
//                    {
//                        sfm = SpatialFieldManager.CreateSpatialFieldManager(doc.ActiveView, 1);
//                    }
//                    else
//                    {
//                        // Clear existing AVF data
//                        sfm.Clear();
//                    }

//                    AnalysisDisplayStyle analysisDisplayStyle = CreateDefaultAnalysisDisplayStyle(doc, "Custom AVF Style");
//                    doc.ActiveView.AnalysisDisplayStyleId = analysisDisplayStyle.Id;

//                    // Example user prompt for selecting the AVF application approach
//                    string avfApproach = GetUserInputForAVFApproach(); // Implement this method based on your UI logic

//                    if (avfApproach == "Grid-Based")
//                    {
//                        ApplyAVFToSelectedFacesBasedOnGrids(doc, faceGrids);
//                    }
//                    else
//                    {
//                        // Existing approach
//                        ApplyAVFToSelectedFaces(doc, faceIntersectionPoints);
//                    }


//                    stopwatch.Stop();
//                    TimeSpan ts = stopwatch.Elapsed;
//                    string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
//                        ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10);
//                    TaskDialog.Show("Execution Time", $"Execution Time: {elapsedTime}");

//                    // Display grid hit counts
//                    DisplayGridHitCounts(doc, faceGrids);

//                    // Display the accumulated log messages
//                    TaskDialog.Show("Execution Log", logBuilder.ToString());


//                    tx.Commit();
//                }

//                return Result.Succeeded;
//            }
//            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
//            {
//                message = "Operation canceled by the user.";
//                return Result.Cancelled;
//            }
//            catch (Exception ex)
//            {
//                message = $"Unexpected error occurred: {ex.Message}\n{ex.StackTrace}";
//                TaskDialog.Show("Error", message);
//                return Result.Failed;
//            }
//        }

//        private double ConvertMillimetersToInternalUnits(double millimeters)
//        {
//            // Convert millimeters to feet directly
//            return UnitUtils.ConvertToInternalUnits(millimeters, UnitTypeId.Millimeters);
//        }


//        private string GetUserInputForAVFApproach()
//        {
//            TaskDialog dialog = new TaskDialog("Choose AVF Application Approach")
//            {
//                MainInstruction = "Select how you want the results to be visualized.",
//                MainContent = "Choose between the original intersection-based approach or the new grid-based approach.",
//                AllowCancellation = true,
//                CommonButtons = TaskDialogCommonButtons.None
//            };
//            dialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, "Intersection-Based Approach");
//            dialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink2, "Grid-Based Approach");

//            TaskDialogResult result = dialog.Show();

//            if (result == TaskDialogResult.CommandLink1)
//            {
//                return "Intersection-Based";
//            }
//            else if (result == TaskDialogResult.CommandLink2)
//            {
//                return "Grid-Based";
//            }
//            else
//            {
//                throw new OperationCanceledException("User canceled the AVF approach selection.");
//            }
//        }

//        private bool GetUserInputForFlag(string prompt, string title)
//        {
//            TaskDialog mainDialog = new TaskDialog(title)
//            {
//                MainInstruction = prompt,
//                MainContent = "",
//                CommonButtons = TaskDialogCommonButtons.Yes | TaskDialogCommonButtons.No,
//                DefaultButton = TaskDialogResult.Yes
//            };
//            TaskDialogResult tResult = mainDialog.Show();

//            return tResult == TaskDialogResult.Yes;
//        }


//        private string GetFaceDetails(Document doc, IList<Reference> faceRefs)
//        {
//            StringBuilder faceDetails = new StringBuilder();
//            foreach (var faceRef in faceRefs)
//            {
//                Element faceElement = doc.GetElement(faceRef.ElementId);
//                if (faceElement != null)
//                {
//                    faceDetails.AppendLine($"Element ID: {faceElement.Id}, Face Reference: {faceRef.ConvertToStableRepresentation(doc)}");
//                }
//            }
//            return faceDetails.ToString();
//        }

//        private XYZ GetSectionBoxTopCenter(BoundingBoxXYZ sectionBox)
//        {
//            return new XYZ(
//                (sectionBox.Min.X + sectionBox.Max.X) / 2,
//                (sectionBox.Min.Y + sectionBox.Max.Y) / 2,
//                sectionBox.Max.Z // Top Z coordinate
//            );
//        }

//        private double GetUserInputHeight(double defaultHeight)
//        {
//            string userInput = Interaction.InputBox(
//                "Enter the height above the section box top for the simulation plane:",
//                "Simulation Plane Height Input",
//                defaultHeight.ToString());
//            return double.TryParse(userInput, out double height) ? height : defaultHeight;
//        }

//        private double GetHexadecagonRotationAngle()
//        {
//            // Define the valid cardinal direction degrees
//            double[] validCardinalDegrees = new double[] {
//                0.0, 22.5, 45.0, 67.5, 90.0, 112.5, 135.0, 157.5,
//                180.0, 202.5, 225.0, 247.5, 270.0, 292.5, 315.0, 337.5
//                };

//            string userInput = Interaction.InputBox(
//                "Enter the cardinal direction in degrees (one of the following: N(0), NNE(22.5), NE(45), ENE(67.5), E(90), " +
//                "ESE(112.5), SE(135), SSE(157.5), S(180), SSW(202.5), " +
//                "SW(225), WSW(247.5), W(270), WNW(292.5), NW(315), NNW(337.5)):",
//                "Hexadecagon Rotation Angle Input",
//                "0");

//            if (double.TryParse(userInput, out double angle))
//            {
//                // Check if the angle is one of the valid cardinal degrees
//                if (validCardinalDegrees.Contains(angle))
//                {
//                    return angle;
//                }
//                else
//                {
//                    // If not, show an error message and prompt again
//                    TaskDialog.Show("Invalid Input", "The value entered is not a valid cardinal direction degree.");
//                    return GetHexadecagonRotationAngle(); // Recursively call the method to get valid input
//                }
//            }
//            else
//            {
//                TaskDialog.Show("Invalid Input", "Please enter a numeric value.");
//                return GetHexadecagonRotationAngle(); // Recursively call the method to get valid input
//            }
//        }

//        private double GetUserInputWindSpeed(double defaultSpeed)
//        {
//            while (true)  // Loop to keep asking for input until it's in the correct format
//            {
//                string userInput = Interaction.InputBox(
//                    "Enter the wind speed in m/s (use a period as the decimal separator):",
//                    "Wind Speed Input",
//                    defaultSpeed.ToString());

//                // Replace comma with period to handle cases where user might use a comma
//                userInput = userInput.Replace(',', '.');

//                // Try to parse the input; if successful, return the parsed value
//                if (double.TryParse(userInput, NumberStyles.Float, CultureInfo.InvariantCulture, out double speed))
//                {
//                    return speed;
//                }

//                // Show an error message if input is invalid
//                MessageBox.Show("Invalid input. Please enter a number using a period (.) as the decimal separator.", "Invalid Input", MessageBoxButtons.OK, MessageBoxIcon.Error);
//            }
//        }


//        private double GetUserInputRaindropRadius(double defaultRadius)
//        {
//            string userInput = Interaction.InputBox(
//                "Enter the raindrop radius in meters (e.g., 0.001 for 1mm):",
//                "Raindrop Radius Input",
//                defaultRadius.ToString());
//            return double.TryParse(userInput, out double radius) ? radius : defaultRadius;
//        }


//        private SimulationPlaneInfo CreateSimulationPlane(Document doc, XYZ center, double width, double depth, double height, double directionAngle, View3D view3D)
//        {

//            SimulationPlaneInfo simulationInfo = new SimulationPlaneInfo();
//            StringBuilder faceInfoBuilder = new StringBuilder();

//            // Assume that the top view of the hexadecagon is flat on the XY plane
//            // The 0 degree direction is aligned with the positive Y axis (North)
//            // The 90 degree direction is aligned with the positive X axis (East)

//            // Calculate the right vector (East) based on the user input direction angle
//            // The direction angle is counterclockwise from the North (+Y axis)
//            double angleRadians = directionAngle * (Math.PI / 180.0);
//            XYZ right = new XYZ(Math.Sin(angleRadians), Math.Cos(angleRadians), 0);
//            XYZ up = new XYZ(-right.Y, right.X, 0); // Perpendicular to the right vector

//            // Define the rectangle corners based on the right and up vectors
//            XYZ p1 = center - right * width / 2 + up * depth / 2;
//            XYZ p2 = center + right * width / 2 + up * depth / 2;
//            XYZ p3 = center + right * width / 2 - up * depth / 2;
//            XYZ p4 = center - right * width / 2 - up * depth / 2;

//            // Create lines for the rectangle edges
//            Line edge1 = Line.CreateBound(p1, p2);
//            Line edge2 = Line.CreateBound(p2, p3);
//            Line edge3 = Line.CreateBound(p3, p4);
//            Line edge4 = Line.CreateBound(p4, p1);

//            // Create and return a curve loop
//            CurveLoop baseRectangle = new CurveLoop();
//            baseRectangle.Append(edge1);
//            baseRectangle.Append(edge2);
//            baseRectangle.Append(edge3);
//            baseRectangle.Append(edge4);

//            // Define the plane geometry
//            XYZ elevatedCenter = new XYZ(center.X, center.Y, center.Z + height);
//            XYZ normal = new XYZ(0, 0, 1); // Normal pointing upwards (Z-axis)

//            double extrusionDepth = 0.1; // Assuming a thin plane
//            Solid planeSolid = GeometryCreationUtilities.CreateExtrusionGeometry(new CurveLoop[] { baseRectangle }, normal, extrusionDepth);

//            // Create a DirectShape for visualization
//            DirectShape ds = DirectShape.CreateElement(doc, new ElementId(BuiltInCategory.OST_GenericModel));
//            ds.ApplicationId = "SimulationPlane";
//            ds.ApplicationDataId = Guid.NewGuid().ToString();

//            // Calculate the endpoints for the existing line
//            XYZ lineStartPoint = elevatedCenter + right * (width / 2) - up * (depth / 2);
//            XYZ lineEndPoint = lineStartPoint + up * depth;

//            // Create the model line for the existing line (as before)
//            Plane plane = Plane.CreateByNormalAndOrigin(normal, elevatedCenter);
//            SketchPlane sketchPlane = SketchPlane.Create(doc, plane);
//            Line existingLine = Line.CreateBound(lineStartPoint, lineEndPoint);
//            ModelCurve existingModelCurve = doc.Create.NewModelCurve(existingLine, sketchPlane);

//            PlanarFace simulationFace = null;
//            Reference faceReference = null;

//            foreach (Face face in planeSolid.Faces)
//            {
//                if (face is PlanarFace planarFace && planarFace.FaceNormal.IsAlmostEqualTo(-XYZ.BasisZ))
//                {
//                    simulationFace = planarFace;
//                    faceReference = face.Reference; // Get the reference of the face.

//                    // Accumulate face info for visualization
//                    faceInfoBuilder.AppendLine($"DirectShape Element ID: {ds.Id.IntegerValue}, " +
//                                               $"DirectShape Unique ID: {ds.UniqueId}, " +
//                                               $"Face: {face}");

//                    // If you want to include the face reference stable representation
//                    if (faceReference != null)
//                    {
//                        string stableReference = faceReference.ConvertToStableRepresentation(doc);
//                        faceInfoBuilder.AppendLine($"Face Reference: {stableReference}");
//                    }
//                }
//            }

//            if (simulationFace != null)
//            {
//                XYZ extrusionDirection = new XYZ(0, 0, 0.01); // Small extrusion in the Z direction
//                Solid extrudedSolid = GeometryCreationUtilities.CreateExtrusionGeometry(new List<CurveLoop> { baseRectangle }, extrusionDirection, extrusionDepth);
//                IList<GeometryObject> geometry = new List<GeometryObject> { extrudedSolid };
//                ds.SetShape(geometry);
//            }

//            // Populate the SimulationPlaneInfo properties
//            simulationInfo.PlaneFace = simulationFace;
//            simulationInfo.SimulationShapeId = ds.Id;
//            simulationInfo.PlaneOrigin = elevatedCenter;
//            simulationInfo.PlaneLength = width;
//            simulationInfo.PlaneWidth = depth;

//            // Show all face info in a TaskDialog
//            //if (faceInfoBuilder.Length > 0)
//            //{
//            //    TaskDialog.Show("Face Info", faceInfoBuilder.ToString());
//            //}

//            return simulationInfo;
//        }

//        //private List<IdentifiedRay> GenerateAndVisualizeRaysFromFace(
//        //    Face planeFace,
//        //    Document doc,
//        //    double spacingInMillimeters, // New parameter for spacing
//        //    double rayLength,
//        //    bool visualizeRays,
//        //    XYZ right,
//        //    double rotationDegrees,
//        //    bool generateOriginalRays,
//        //    bool generateAdditionalRays,
//        //    IList<Reference> faceRefs)
//        //{
//        //    List<IdentifiedRay> identifiedRays = new List<IdentifiedRay>();
//        //    int rayCounter = 0; // Counter for assigning unique IDs to rays
//        //    StringBuilder rayInfoBuilder = new StringBuilder(); // StringBuilder to accumulate ray info

//        //    // Extract the corner points of the plane face
//        //    EdgeArrayArray edgeLoops = planeFace.EdgeLoops;
//        //    EdgeArray edges = edgeLoops.get_Item(0); // Assuming the first loop is the outer loop

//        //    XYZ p1 = edges.get_Item(0).AsCurve().GetEndPoint(0);
//        //    XYZ p2 = edges.get_Item(1).AsCurve().GetEndPoint(0);
//        //    XYZ p3 = edges.get_Item(2).AsCurve().GetEndPoint(0);
//        //    XYZ p4 = edges.get_Item(3).AsCurve().GetEndPoint(0);

//        //    // Calculate side vectors and their lengths
//        //    XYZ side1 = p2 - p1;
//        //    XYZ side2 = p3 - p2;
//        //    double length = side1.GetLength();
//        //    double width = side2.GetLength();

//        //    // Calculate area of the face
//        //    double area = length * width;

//        //    // Convert spacing from millimeters to meters
//        //    double spacingInMeters = spacingInMillimeters / 1000.0;

//        //    // Calculate ray density along each dimension
//        //    double rayDensityLength = Math.Round(length / spacingInMeters);
//        //    double rayDensityWidth = Math.Round(width / spacingInMeters);

//        //    // Normalize side vectors
//        //    XYZ dir1 = side1.Normalize();
//        //    XYZ dir2 = side2.Normalize();

//        //    // Generate grid points
//        //    for (int i = 0; i <= rayDensityLength; i++)
//        //    {
//        //        XYZ startPoint = p1 + i * dir1 * (length / rayDensityLength);
//        //        for (int j = 0; j <= rayDensityWidth; j++)
//        //        {
//        //            XYZ gridPoint = startPoint + j * dir2 * (width / rayDensityWidth);

//        //            if (generateOriginalRays)
//        //            {
//        //                XYZ endPointDown = gridPoint - new XYZ(0, 0, rayLength);
//        //                IdentifiedRay identifiedRay = CreateIdentifiedRay(gridPoint, endPointDown, ref rayCounter, XYZ.BasisZ.Negate());

//        //                // Accumulate the start and endpoints of the original ray
//        //                rayInfoBuilder.AppendLine($"Original Ray {rayCounter}: Start {gridPoint}, End {endPointDown}");

//        //                if (identifiedRay.Ray != null && identifiedRay.Ray.Length > doc.Application.ShortCurveTolerance)
//        //                {
//        //                    identifiedRays.Add(identifiedRay);
//        //                }
//        //            }

//        //            if (generateAdditionalRays)
//        //            {
//        //                // Calculate the inclination angle in radians
//        //                double inclinationAngleRadians = CalculateInclinationAngle(windSpeed, raindropRadius).InclinationRadians;

//        //                // Adjust the calculation of inclinedDirection for vertical inclination
//        //                // The new calculation assumes that an angle of 0 radians represents a downward vertical direction (along -Z)
//        //                // and inclination is measured from this vertical direction.
//        //                XYZ inclinedDirection = new XYZ(
//        //                    Math.Sin(inclinationAngleRadians) * -right.X,  // X component
//        //                    Math.Sin(inclinationAngleRadians) * -right.Y,  // Y component
//        //                    -Math.Cos(inclinationAngleRadians)             // Z component (vertical)
//        //                );

//        //                XYZ endPointInclined = gridPoint + inclinedDirection * rayLength;

//        //                IdentifiedRay identifiedRay = CreateIdentifiedRay(gridPoint, endPointInclined, ref rayCounter, inclinedDirection);

//        //                // Accumulate the start and endpoints of the additional ray
//        //                rayInfoBuilder.AppendLine($"Additional Ray {rayCounter}: Start {gridPoint}, End {endPointInclined}");

//        //                if (identifiedRay.Ray != null && identifiedRay.Ray.Length > doc.Application.ShortCurveTolerance)
//        //                {
//        //                    identifiedRays.Add(identifiedRay);
//        //                }
//        //            }



//        //        }
//        //    }

//        //    if (visualizeRays)
//        //    {
//        //        VisualizeRays(identifiedRays, doc, faceRefs);
//        //    }

//        //    //// Display ray information using TaskDialog
//        //    //TaskDialog.Show("Ray Information", rayInfoBuilder.ToString());

//        //    return identifiedRays;
//        //}

//        //private IdentifiedRay CreateIdentifiedRay(XYZ startPoint, XYZ endPoint, ref int rayCounter, XYZ direction)
//        //{
//        //    Line rayLine = Line.CreateBound(startPoint, endPoint);
//        //    IdentifiedRay identifiedRay = new IdentifiedRay
//        //    {
//        //        Ray = rayLine,
//        //        RayId = $"Ray_{rayCounter++}"
//        //    };

//        //    logBuilder.AppendLine($"Ray {identifiedRay.RayId}: Start {startPoint}, End {endPoint}, Direction {direction}");
//        //    return identifiedRay;
//        //}

//        private List<IdentifiedRay> GenerateAndVisualizeRaysFromFace(
//             Face planeFace,
//             Document doc,
//             double spacingInMillimeters, // New parameter for spacing
//             double rayLength,
//             bool visualizeRays,
//             XYZ right,
//             double rotationDegrees,
//             bool generateOriginalRays,
//             bool generateAdditionalRays,
//             IList<Reference> faceRefs)
//        {
//            ConcurrentBag<IdentifiedRay> concurrentIdentifiedRays = new ConcurrentBag<IdentifiedRay>();
//            EdgeArrayArray edgeLoops = planeFace.EdgeLoops;
//            EdgeArray edges = edgeLoops.get_Item(0); // Assuming the first loop is the outer loop

//            XYZ p1 = edges.get_Item(0).AsCurve().GetEndPoint(0);
//            XYZ p2 = edges.get_Item(1).AsCurve().GetEndPoint(0);
//            XYZ p3 = edges.get_Item(2).AsCurve().GetEndPoint(0);
//            XYZ p4 = edges.get_Item(3).AsCurve().GetEndPoint(0);

//            XYZ side1 = p2 - p1;
//            XYZ side2 = p3 - p2;
//            double length = side1.GetLength();
//            double width = side2.GetLength();

//            double spacingInMeters = spacingInMillimeters / 1000.0;
//            int rayDensityLength = (int)Math.Round(length / spacingInMeters);
//            int rayDensityWidth = (int)Math.Round(width / spacingInMeters);

//            XYZ dir1 = side1.Normalize();
//            XYZ dir2 = side2.Normalize();

//            try
//            {
//                Parallel.For(0, rayDensityLength + 1, i =>
//                {
//                    XYZ startPoint = p1 + i * dir1 * (length / rayDensityLength);
//                    for (int j = 0; j <= rayDensityWidth; j++)
//                    {
//                        XYZ gridPoint = startPoint + j * dir2 * (width / rayDensityWidth);

//                        if (generateOriginalRays)
//                        {
//                            XYZ endPointDown = gridPoint - new XYZ(0, 0, rayLength);
//                            IdentifiedRay identifiedRay = CreateIdentifiedRay(gridPoint, endPointDown, XYZ.BasisZ.Negate());
//                            concurrentIdentifiedRays.Add(identifiedRay);
//                        }

//                        if (generateAdditionalRays)
//                        {
//                            var inclinationResults = CalculateInclinationAngle(windSpeed, raindropRadius, true);
//                            double inclinationAngleRadians = inclinationResults.InclinationRadians;
//                            XYZ inclinedDirection = new XYZ(
//                                Math.Sin(inclinationAngleRadians) * -right.X,
//                                Math.Sin(inclinationAngleRadians) * -right.Y,
//                                -Math.Cos(inclinationAngleRadians));
//                            XYZ endPointInclined = gridPoint + inclinedDirection * rayLength;
//                            IdentifiedRay identifiedRay = CreateIdentifiedRay(gridPoint, endPointInclined, inclinedDirection);
//                            concurrentIdentifiedRays.Add(identifiedRay);
//                        }
//                    }
//                });

//            }
//            catch (AggregateException ae)
//            {
//                // Handle each individual exception
//                foreach (var ex in ae.Flatten().InnerExceptions)
//                {
//                    // Log the exception details
//                    // You might want to log this information to a file or a logging system
//                    Debug.WriteLine($"Exception: {ex.Message}");
//                    Debug.WriteLine($"StackTrace: {ex.StackTrace}");
//                }
//                // Depending on how critical the exception is, you may rethrow, return, or handle the error
//                // Rethrow the exception for the calling method to handle
//                throw;
//            }
//            List<IdentifiedRay> identifiedRays = concurrentIdentifiedRays.ToList();

//            if (visualizeRays)
//            {
//                // Visualize rays; Ensure any non-thread-safe actions are performed outside the parallel region
//                VisualizeRays(identifiedRays, doc, faceRefs);
//            }

//            return identifiedRays;
//        }


//        private static int lastRayId = 0;

//        private IdentifiedRay CreateIdentifiedRay(XYZ startPoint, XYZ endPoint, XYZ direction)
//        {
//            Line rayLine = Line.CreateBound(startPoint, endPoint);
//            int newId = Interlocked.Increment(ref lastRayId); // Atomically increments the ID
//            IdentifiedRay identifiedRay = new IdentifiedRay
//            {
//                Ray = rayLine,
//                RayId = $"Ray_{newId}", // Unique ID based on the atomic increment
//            };
//            return identifiedRay;
//        }

//        private void VisualizeRays(List<IdentifiedRay> rays, Document doc, IList<Reference> faceRefs)
//        {
//            foreach (IdentifiedRay identifiedRay in rays)
//            {
//                Line rayLine = identifiedRay.Ray;
//                if (rayLine != null && rayLine.Length > doc.Application.ShortCurveTolerance)
//                {
//                    // Create a DirectShape to visualize the ray
//                    DirectShape ds = DirectShape.CreateElement(doc, new ElementId(BuiltInCategory.OST_Lines));
//                    ds.ApplicationId = "Rays Visualization";
//                    ds.ApplicationDataId = identifiedRay.RayId;
//                    ds.SetShape(new List<GeometryObject> { rayLine });
//                }
//            }
//        }

//        private (double InclinationRadians, double InclinationDegrees, double TerminalVelocity) CalculateInclinationAngle(double windSpeed, double raindropRadius, bool showDebug = false)
//        {
//            try
//            {
//                // Constants
//                double g = 9.81; // Acceleration due to gravity in m/s^2
//                double rhoW = 1000; // Density of water in kg/m^3
//                double rhoA = 1.225; // Density of air at sea level in kg/m^3
//                double Cd = 0.47; // Drag coefficient for a sphere

//                // Convert raindrop radius from millimeters to meters
//                raindropRadius = raindropRadius / 1000.0; // If raindropRadius was initially in mm

//                double volume = (4.0 / 3.0) * Math.PI * Math.Pow(raindropRadius, 3);
//                double mass = rhoW * volume;

//                // Calculate cross-sectional area of the raindrop
//                double area = Math.PI * Math.Pow(raindropRadius, 2);

//                // Calculate terminal velocity
//                double vT = Math.Sqrt((2 * mass * g) / (rhoA * area * Cd));

//                // Calculate inclination angle in radians
//                double inclinationAngleRadians = Math.Atan(windSpeed / vT);

//                // Convert to degrees
//                double inclinationDegrees = inclinationAngleRadians * (180.0 / Math.PI);

//                // Show debug information if requested
//                //if (showDebug)
//                //{
//                //    TaskDialog debugDialog = new TaskDialog("Debug Information")
//                //    {
//                //        MainInstruction = "Debug Information for Inclination Calculation",
//                //        AllowCancellation = true,
//                //        ExpandedContent = $"Calculating the inclination angle...\n" +
//                //                          $"Volume: {volume} m^3\n" +
//                //                          $"Mass: {mass} kg\n" +
//                //                          $"Cross-sectional Area: {area} m^2\n" +
//                //                          $"Terminal Velocity vT: {vT} m/s\n" +
//                //                          $"Inclination Angle Radians: {inclinationAngleRadians}\n" +
//                //                          $"Inclination Angle Degrees: {inclinationDegrees}"
//                //    };

//                //    debugDialog.Show();
//                //}

//                return (inclinationAngleRadians, inclinationDegrees, vT);
//            }
//            catch (Exception ex)
//            {
//                TaskDialog.Show("Error", $"An error occurred: {ex.Message}");
//                throw; // Re-throw the exception for higher-level handling
//            }
//        }



//        private Dictionary<Reference, List<XYZ>> DisplayRayFaceIntersectionsInDialog(Document doc, List<IdentifiedRay> rays, IList<Reference> faceRefs)
//        {
//            var faceToFirstIntersectionsMap = new Dictionary<Reference, List<(XYZ IntersectionPoint, string RayId)>>();
//            var faceIntersectionPoints = new Dictionary<Reference, List<XYZ>>();

//            foreach (IdentifiedRay ray in rays)
//            {
//                IntersectionInfo closestIntersectionInfo = null;
//                foreach (Reference faceRef in faceRefs)
//                {
//                    Face face = doc.GetElement(faceRef.ElementId).GetGeometryObjectFromReference(faceRef) as Face;
//                    if (face == null) continue;

//                    IntersectionResultArray results;
//                    SetComparisonResult intersectResult = face.Intersect(ray.Ray, out results);

//                    if (intersectResult == SetComparisonResult.Overlap)
//                    {
//                        foreach (IntersectionResult result in results)
//                        {
//                            var distance = result.XYZPoint.DistanceTo(ray.Ray.GetEndPoint(0));
//                            if (closestIntersectionInfo == null || distance < closestIntersectionInfo.Distance)
//                            {
//                                closestIntersectionInfo = new IntersectionInfo
//                                {
//                                    ElementId = faceRef.ElementId,
//                                    Face = face,
//                                    IntersectionPoint = result.XYZPoint,
//                                    Distance = distance,
//                                    FaceReference = faceRef,
//                                    RayId = ray.RayId
//                                };
//                            }
//                        }
//                    }
//                }

//                if (closestIntersectionInfo != null)
//                {
//                    if (!faceToFirstIntersectionsMap.ContainsKey(closestIntersectionInfo.FaceReference))
//                    {
//                        faceToFirstIntersectionsMap[closestIntersectionInfo.FaceReference] = new List<(XYZ IntersectionPoint, string RayId)>();
//                    }

//                    faceToFirstIntersectionsMap[closestIntersectionInfo.FaceReference].Add((closestIntersectionInfo.IntersectionPoint, closestIntersectionInfo.RayId));
//                }
//            }

//            // Build the string for the TaskDialog grouping by face and populate faceIntersectionPoints
//            StringBuilder dialogContent = new StringBuilder();
//            foreach (var faceRef in faceToFirstIntersectionsMap.Keys)
//            {
//                ElementId faceElementId = faceRef.ElementId;
//                string faceRefString = faceRef.ConvertToStableRepresentation(doc);
//                dialogContent.AppendLine($"Face Reference: {faceRefString}, Element ID: {faceElementId}");
//                dialogContent.AppendLine("First Intersection Points and Ray IDs:");

//                var intersectionPoints = new List<XYZ>();
//                foreach (var intersection in faceToFirstIntersectionsMap[faceRef])
//                {
//                    dialogContent.AppendLine($"Point: {intersection.IntersectionPoint}, Ray ID: {intersection.RayId}");
//                    intersectionPoints.Add(intersection.IntersectionPoint);
//                }
//                faceIntersectionPoints[faceRef] = intersectionPoints;

//                dialogContent.AppendLine("-----");
//            }

//            // Show the TaskDialog
//            //TaskDialog.Show("First Ray-Face Intersection Details", dialogContent.ToString());

//            return faceIntersectionPoints;
//        }




//        private Face GetFaceFromReference(Document doc, Reference faceRef)
//        {
//            Element element = doc.GetElement(faceRef.ElementId);
//            if (element != null)
//            {
//                return element.GetGeometryObjectFromReference(faceRef) as Face;
//            }
//            return null;
//        }

//        private Dictionary<Reference, List<UVGridCell>> CreateGridsForFaces(Document doc, IList<Reference> faceRefs, double spacingInMillimeters)
//        {
//            // Convert spacing from millimeters to Revit's internal units (feet)
//            double spacingInInternalUnits = ConvertMillimetersToInternalUnits(spacingInMillimeters);

//            //// Convert millimeters to meters for consistent spacing calculations
//            //double spacingInMeters = spacingInMillimeters / 1000.0;
//            //// If you need to convert spacing from meters to Revit's internal units (feet) for a specific operation
//            //double spacingInFeet = UnitUtils.ConvertToInternalUnits(spacingInMeters, UnitTypeId.Meters);


//            var faceGrids = new Dictionary<Reference, List<UVGridCell>>();

//            foreach (var faceRef in faceRefs)
//            {
//                Face face = doc.GetElement(faceRef.ElementId).GetGeometryObjectFromReference(faceRef) as Face;
//                if (face == null) continue;

//                BoundingBoxUV bboxUV = face.GetBoundingBox();
//                double faceWidth = bboxUV.Max.U - bboxUV.Min.U;
//                double faceHeight = bboxUV.Max.V - bboxUV.Min.V;

//                // Calculate how many divisions fit into the face's width and height using the specified spacing
//                int uDivisions = (int)Math.Ceiling(faceWidth / spacingInInternalUnits);
//                int vDivisions = (int)Math.Ceiling(faceHeight / spacingInInternalUnits);

//                //int uDivisions = (int)Math.Ceiling(faceWidth / spacingInFeet);
//                //int vDivisions = (int)Math.Ceiling(faceHeight / spacingInFeet);


//                var gridCells = new List<UVGridCell>();
//                int cellIdCounter = 1;

//                // Calculate the size of each division in UV space
//                double uStep = faceWidth / uDivisions;
//                double vStep = faceHeight / vDivisions;

//                for (int u = 0; u < uDivisions; u++)
//                {
//                    for (int v = 0; v < vDivisions; v++)
//                    {
//                        UV minUV = new UV(bboxUV.Min.U + u * uStep, bboxUV.Min.V + v * vStep);
//                        UV maxUV = new UV(minUV.U + uStep, minUV.V + vStep);

//                        gridCells.Add(new UVGridCell
//                        {
//                            Id = $"Grid_{cellIdCounter++}", // Assign a unique ID to each grid cell
//                            MinUV = minUV,
//                            MaxUV = maxUV,
//                            Hits = 0 // Initialize the hit counter for each grid cell
//                        });
//                    }
//                }

//                faceGrids.Add(faceRef, gridCells);
//            }

//            return faceGrids;
//        }


//        private void CountRayHitsPerGrid(Document doc, List<IdentifiedRay> rays, Dictionary<Reference, List<UVGridCell>> faceGrids)
//        {
//            foreach (var kvp in faceGrids)
//            {
//                Reference faceRef = kvp.Key;
//                List<UVGridCell> gridCells = kvp.Value;
//                Face face = doc.GetElement(faceRef.ElementId).GetGeometryObjectFromReference(faceRef) as Face;

//                if (face == null) continue;

//                foreach (IdentifiedRay ray in rays)
//                {
//                    IntersectionResultArray results = null;
//                    SetComparisonResult intersectResult = face.Intersect(ray.Ray, out results);

//                    if (intersectResult != SetComparisonResult.Overlap) continue;

//                    foreach (IntersectionResult result in results)
//                    {
//                        UV uvPoint = result.UVPoint;
//                        foreach (UVGridCell cell in gridCells)
//                        {
//                            if (uvPoint.U >= cell.MinUV.U && uvPoint.U < cell.MaxUV.U && uvPoint.V >= cell.MinUV.V && uvPoint.V < cell.MaxUV.V)
//                            {
//                                cell.Hits++;
//                                break; // Assuming a ray can only intersect a grid cell once
//                            }
//                        }
//                    }
//                }
//            }
//        }


//        private void DisplayGridHitCounts(Document doc, Dictionary<Reference, List<UVGridCell>> faceGrids)
//        {
//            StringBuilder sb = new StringBuilder();
//            foreach (var kvp in faceGrids)
//            {
//                var faceRef = kvp.Key;
//                var gridCells = kvp.Value;

//                Element faceElement = doc.GetElement(faceRef.ElementId);
//                sb.AppendLine($"Face ID: {faceElement.Id.IntegerValue}");

//                foreach (var cell in gridCells)
//                {
//                    sb.AppendLine($"Grid ID: {cell.Id}, Hits: {cell.Hits}");
//                }

//                sb.AppendLine("----------");
//            }

//            // Display in a Revit Task Dialog or any other appropriate method
//            TaskDialog.Show("Grid Hit Counts", sb.ToString());
//        }



//        private void ApplyAVFToSelectedFaces(Document doc, Dictionary<Reference, List<XYZ>> faceIntersectionPoints)
//        {
//            foreach (var entry in faceIntersectionPoints)
//            {
//                Reference faceRef = entry.Key;
//                List<XYZ> intersections = entry.Value;

//                Face face = GetFaceFromReference(doc, faceRef);
//                if (face != null)
//                {
//                    SetUpAndApplyAVF(doc, face, faceRef, intersections);
//                }
//            }
//        }


//        private AnalysisDisplayStyle CreateCustomAnalysisDisplayStyle(Document doc)
//        {
//            // Define a unique name for the style to avoid conflicts
//            string styleName = "Custom Grid-Based Analysis Display Style";

//            // Check if the style already exists
//            AnalysisDisplayStyle existingStyle = new FilteredElementCollector(doc)
//                .OfClass(typeof(AnalysisDisplayStyle))
//                .Cast<AnalysisDisplayStyle>()
//                .FirstOrDefault(a => a.Name.Equals(styleName));

//            // If it exists, return the existing style
//            if (existingStyle != null) return existingStyle;

//            // No transaction is started here, assuming the caller manages it

//            // Create color settings for the style
//            AnalysisDisplayColorSettings colorSettings = new AnalysisDisplayColorSettings();
//            colorSettings.MinColor = new Color(255, 255, 255); // White for minimum values
//            colorSettings.MaxColor = new Color(0, 0, 255);     // Blue for maximum values

//            // Create surface settings for the style
//            AnalysisDisplayColoredSurfaceSettings surfaceSettings = new AnalysisDisplayColoredSurfaceSettings();
//            surfaceSettings.ShowGridLines = false;

//            // Create legend settings for the style
//            AnalysisDisplayLegendSettings legendSettings = new AnalysisDisplayLegendSettings();
//            legendSettings.ShowLegend = true;
//            legendSettings.NumberOfSteps = 10;
//            legendSettings.ShowDataDescription = true;

//            // Create the analysis display style without starting a new transaction
//            AnalysisDisplayStyle newStyle = AnalysisDisplayStyle.CreateAnalysisDisplayStyle(doc, styleName, surfaceSettings, colorSettings, legendSettings);

//            return newStyle;
//        }



//        private void ApplyAVFToSelectedFacesBasedOnGrids(Document doc, Dictionary<Reference, List<UVGridCell>> faceGrids)
//        {
//            SpatialFieldManager sfm = SpatialFieldManager.GetSpatialFieldManager(doc.ActiveView);
//            if (sfm == null)
//            {
//                sfm = SpatialFieldManager.CreateSpatialFieldManager(doc.ActiveView, 1);
//            }

//            // Assume CreateCustomAnalysisDisplayStyle is defined elsewhere in your code
//            AnalysisDisplayStyle customDisplayStyle = CreateCustomAnalysisDisplayStyle(doc);
//            doc.ActiveView.AnalysisDisplayStyleId = customDisplayStyle.Id;

//            foreach (var faceGridEntry in faceGrids)
//            {
//                Reference faceRef = faceGridEntry.Key;
//                List<UVGridCell> gridCells = faceGridEntry.Value;

//                int primitiveId = sfm.AddSpatialFieldPrimitive(faceRef);

//                // Determine max hits to normalize the color gradient
//                int maxHits = gridCells.Max(cell => cell.Hits);

//                // Convert grid cell hits to field value list and normalize them based on maxHits
//                IList<ValueAtPoint> values = gridCells.Select(cell =>
//                    new ValueAtPoint(new List<double> { maxHits > 0 ? (double)cell.Hits / maxHits : 0 })).ToList();

//                FieldValues fieldValues = new FieldValues(values);

//                // Prepare UV points for AVF from the center of each grid cell
//                IList<UV> uvPoints = gridCells.Select(cell =>
//                    new UV((cell.MinUV.U + cell.MaxUV.U) / 2, (cell.MinUV.V + cell.MaxUV.V) / 2)).ToList();

//                FieldDomainPointsByUV fieldPoints = new FieldDomainPointsByUV(uvPoints);

//                // Use a schema index specifically for this visualization approach
//                int schemaIndex = GetOrCreateAnalysisResultSchemaIndex(sfm, "Grid-Based Schema",
//                    "Visualization based on ray hits per grid cell");

//                sfm.UpdateSpatialFieldPrimitive(primitiveId, fieldPoints, fieldValues, schemaIndex);
//            }
//        }



//        private void SetUpAndApplyAVF(Document doc, Face face, Reference faceRef, List<XYZ> intersections)
//        {
//            // Create or get SpatialFieldManager for the active view
//            SpatialFieldManager sfm = SpatialFieldManager.GetSpatialFieldManager(doc.ActiveView);
//            if (sfm == null)
//            {
//                sfm = SpatialFieldManager.CreateSpatialFieldManager(doc.ActiveView, 1);
//            }

//            // Create an AnalysisDisplayStyle
//            AnalysisDisplayStyle analysisDisplayStyle = CreateDefaultAnalysisDisplayStyle(doc, "Custom AVF Style");

//            // Set the AnalysisDisplayStyle to the active view
//            doc.ActiveView.AnalysisDisplayStyleId = analysisDisplayStyle.Id;

//            // Register a new result schema with the SpatialFieldManager
//            AnalysisResultSchema resultSchema = new AnalysisResultSchema("Custom Schema", "Description");
//            int schemaIndex = GetOrCreateAnalysisResultSchemaIndex(sfm, "Custom Schema", "Description");

//            // Prepare data for AVF
//            IList<UV> uvPoints;
//            FieldDomainPointsByUV fieldPoints = GetFieldDomainPointsByUV(face, out uvPoints);
//            FieldValues fieldValues = GetFieldValuesForIntersections(uvPoints, intersections, face);

//            // Add or Update the spatial field primitive for the face
//            int primitiveId = sfm.AddSpatialFieldPrimitive(faceRef);
//            sfm.UpdateSpatialFieldPrimitive(primitiveId, fieldPoints, fieldValues, schemaIndex);
//        }

//        private FieldValues GetFieldValuesForIntersections(IList<UV> uvPoints, List<XYZ> intersections, Face face)
//        {
//            var values = new List<ValueAtPoint>();
//            double proximityThreshold = 0.16; // Adjust this threshold as needed 0.26,15

//            foreach (UV uv in uvPoints)
//            {
//                XYZ point = face.Evaluate(uv);
//                double nearestDistance = intersections.Min(intersect => point.DistanceTo(intersect));
//                double value = nearestDistance <= proximityThreshold ? 1 : 0;

//                values.Add(new ValueAtPoint(new List<double> { value }));
//            }

//            return new FieldValues(values);
//        }

//        private UV GetUVPoint(Face face, XYZ xyzPoint)
//        {
//            IntersectionResult result = face.Project(xyzPoint);
//            if (result == null) return null;
//            return result.UVPoint;
//        }

//        private int GetOrCreateAnalysisResultSchemaIndex(SpatialFieldManager sfm, string schemaName, string schemaDescription)
//        {
//            foreach (int index in sfm.GetRegisteredResults())
//            {
//                AnalysisResultSchema existingSchema = sfm.GetResultSchema(index);
//                if (existingSchema.Name.Equals(schemaName))
//                {
//                    return index;
//                }
//            }

//            AnalysisResultSchema newSchema = new AnalysisResultSchema(schemaName, schemaDescription);
//            return sfm.RegisterResult(newSchema);
//        }

//        private AnalysisDisplayStyle CreateDefaultAnalysisDisplayStyle(Document doc, string styleName)
//        {
//            var existingStyle = new FilteredElementCollector(doc)
//                .OfClass(typeof(AnalysisDisplayStyle))
//                .Cast<AnalysisDisplayStyle>()
//                .FirstOrDefault(style => style.Name.Equals(styleName));

//            if (existingStyle != null)
//                return existingStyle;

//            AnalysisDisplayColoredSurfaceSettings surfaceSettings = new AnalysisDisplayColoredSurfaceSettings
//            {
//                ShowGridLines = false
//            };

//            AnalysisDisplayColorSettings colorSettings = new AnalysisDisplayColorSettings
//            {
//                MaxColor = new Color(173, 216, 230), // Light Blue (RGB: 173, 216, 230)
//                MinColor = new Color(255, 255, 255)  // White (RGB: 255, 255, 255)
//            };


//            AnalysisDisplayLegendSettings legendSettings = new AnalysisDisplayLegendSettings
//            {
//                ShowLegend = true,
//                NumberOfSteps = 10,             // Adjust the number of steps in the legend
//                ShowDataDescription = true,     // Show or hide data description
//                Rounding = 0.1                  // Set rounding for values
//                                                // Note: Direct control over legend size is not available
//            };

//            return AnalysisDisplayStyle.CreateAnalysisDisplayStyle(doc, styleName, surfaceSettings, colorSettings, legendSettings);
//        }




//        private FieldDomainPointsByUV GetFieldDomainPointsByUV(Face face, out IList<UV> uvPoints)
//        {
//            uvPoints = new List<UV>();
//            BoundingBoxUV bbox = face.GetBoundingBox();
//            double uStep = (bbox.Max.U - bbox.Min.U) / 10; // 10 steps across the U direction
//            double vStep = (bbox.Max.V - bbox.Min.V) / 10; // 10 steps across the V direction

//            for (double u = bbox.Min.U; u <= bbox.Max.U; u += uStep)
//            {
//                for (double v = bbox.Min.V; v <= bbox.Max.V; v += vStep)
//                {
//                    UV uv = new UV(u, v);
//                    if (face.IsInside(uv))
//                    {
//                        uvPoints.Add(uv);
//                    }
//                }
//            }

//            return new FieldDomainPointsByUV(uvPoints);
//        }



//        private FieldValues GetFieldValues(IList<UV> uvPoints)
//        {
//            IList<ValueAtPoint> values = new List<ValueAtPoint>();
//            foreach (UV uv in uvPoints)
//            {
//                double sampleValue = uv.U + uv.V; // Replace this with actual analysis value
//                values.Add(new ValueAtPoint(new List<double> { sampleValue }));
//            }

//            return new FieldValues(values);
//        }



//        //private AnalysisDisplayStyle GetOrCreateCustomAnalysisDisplayStyle(Document doc, string styleName)
//        //{
//        //    // Try to find an existing style
//        //    var existingStyle = new FilteredElementCollector(doc)
//        //        .OfClass(typeof(AnalysisDisplayStyle))
//        //        .Cast<AnalysisDisplayStyle>()
//        //        .FirstOrDefault(style => style.Name.Equals(styleName));

//        //    if (existingStyle != null) return existingStyle;

//        //    // If not found, create a new one
//        //    using (Transaction tx = new Transaction(doc, "Create Analysis Display Style"))
//        //    {
//        //        tx.Start();

//        //        // Define custom color settings for the style
//        //        AnalysisDisplayColoredSurfaceSettings surfaceSettings = new AnalysisDisplayColoredSurfaceSettings();
//        //        surfaceSettings.ShowGridLines = false;

//        //        AnalysisDisplayColorSettings colorSettings = new AnalysisDisplayColorSettings
//        //        {
//        //            MinColor = new Color(255, 255, 255), // White
//        //            MaxColor = new Color(0, 0, 255) // Deep Blue
//        //        };

//        //        AnalysisDisplayLegendSettings legendSettings = new AnalysisDisplayLegendSettings
//        //        {
//        //            ShowLegend = true,
//        //            NumberOfSteps = 10,
//        //            ShowDataDescription = true,
//        //            Rounding = 0.1
//        //        };

//        //        var newStyle = AnalysisDisplayStyle.CreateAnalysisDisplayStyle(doc, styleName, surfaceSettings, colorSettings, legendSettings);

//        //        tx.Commit();

//        //        return newStyle;
//        //    }
//        //}


//    }

//    // Updated IntersectionInfo class to include distance
//    public class IntersectionInfo
//    {
//        public ElementId ElementId { get; set; }
//        public XYZ IntersectionPoint { get; set; }
//        public Face Face { get; set; }
//        public Reference FaceReference { get; set; }
//        public double Distance { get; set; }
//        public string RayId { get; set; }
//        public string ElementType { get; set; } // Add this line to store the element type
//        public string GeometryType { get; set; } // New property to store the type of geometry
//        public Reference IntersectingFaceRef { get; set; } // New property for intersecting face reference
//    }


//    public class SimulationPlaneInfo
//    {
//        public Face PlaneFace { get; set; }
//        public XYZ PlaneOrigin { get; set; }
//        public double PlaneLength { get; set; }
//        public double PlaneWidth { get; set; }
//        public ElementId SimulationShapeId { get; set; } // Add this line
//    }

//    public class IdentifiedRay
//    {
//        public Line Ray { get; set; }
//        public string RayId { get; set; }
//        public ElementId ElementId { get; set; } // Add this line if ElementId should be a part of IdentifiedRay
//    }

//    public class UVGridCell
//    {
//        public string Id { get; set; }
//        public UV MinUV { get; set; }
//        public UV MaxUV { get; set; }
//        public int Hits { get; set; }
//    }

//}




////this works. 3rd update of 16.03.24. Works perfect adds a WPF user interface

//using Autodesk.Revit.Attributes;
//using Autodesk.Revit.DB.Analysis;
//using Autodesk.Revit.DB;
//using Autodesk.Revit.UI.Selection;
//using Autodesk.Revit.UI;
//using Microsoft.VisualBasic;
//using System.Collections.Concurrent;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.Globalization;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using System.Threading;
//using System.Windows.Forms;
//using System;


//namespace RevitWoodLCC
//{
//    using System;
//    //using System.Globalization;
//    //using System.Windows.Forms;
//    //using System.Drawing;
//    //using System.Windows;

//    public static class RainfallSimulationUtilities
//    {
//        public static double CalculateDropletSpacingInMillimeters(double rainfallIntensityMmPerHr, double raindropRadiusM)
//        {
//            // Convert rainfall intensity from mm/hr to m/s (1 mm/hr = 2.77778e-7 m/s)
//            double rainfallIntensityMS = rainfallIntensityMmPerHr * 2.77778e-7;

//            // Calculate volume of a single raindrop in cubic meters
//            double dropVolumeM3 = (4.0 / 3.0) * Math.PI * Math.Pow(raindropRadiusM, 3);

//            // Calculate the number of raindrops per square meter
//            double dropsPerSquareMeter = rainfallIntensityMS / dropVolumeM3;

//            // Calculate the area covered by a single raindrop
//            double areaPerDropM2 = 1.0 / dropsPerSquareMeter;

//            // Calculate spacing in meters
//            double dropletSpacingM = Math.Sqrt(areaPerDropM2);

//            // Convert spacing to millimeters
//            double dropletSpacingMm = dropletSpacingM * 1000;

//            return dropletSpacingMm;
//        }
//    }



//    [Transaction(TransactionMode.Manual)]
//    public class PickedFaceRayProjectionCode_new : IExternalCommand
//    {
//        // Cardinal directions and their corresponding angles
//        private readonly double[] directionAngles = new double[] {
//            0.0, 22.5, 45.0, 67.5, 90.0, 112.5, 135.0, 157.5,
//            180.0, 202.5, 225.0, 247.5, 270.0, 292.5, 315.0, 337.5
//        };

//        private StringBuilder logBuilder = new StringBuilder(); // For accumulating log messages

//        // Booleans to control ray generation
//        bool generateOriginalRays = false /* true or false */;
//        bool generateAdditionalRays = true /* true or false */;

//        private double windSpeed; // Wind speed in m/s
//        private double raindropRadiusM; // Raindrop radius in meters
//        private double spacingInMillimeters;
//        private double simulationPlaneHeight;
//        private double rainfallIntensityMmPerHr;
//        private double rotationDegrees;
//        private int maxHitCount = 0;

//        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
//        {
//            UIApplication uiApp = commandData.Application;
//            UIDocument uiDoc = uiApp.ActiveUIDocument;
//            Document doc = uiDoc.Document;

//            string avfApproach = string.Empty; // Initialize it as empty or null

//            // Example default values, adjust these as needed
//            //double defaultHeight = 10.0;
//            double defaultRotationAngle = 90.0;
//            double defaultWindSpeed = 3.8;
//            double defaultRaindropRadius = 0.001;
//            double defaultRainfallIntensity = 10;

//            // 80 Define the desired spacing in millimeters
//            double rayLength = 100.0; // Define the length of the rays
//            bool visualizeRays = false; // Set to 'true' to enable visualization by default
//            double gridsize = 2000;// define gridsize for applying AVF IN MM

//            try
//            {
//                // Prompt user to select faces
//                IList<Reference> selectedFaceRefs = uiDoc.Selection.PickObjects(ObjectType.Face, "Select faces for AVF application.");
//                if (selectedFaceRefs.Count == 0)
//                {
//                    message = "No faces selected.";
//                    return Result.Cancelled;
//                }

//                // Get and display face details
//                string faceDetails = GetFaceDetails(doc, selectedFaceRefs);
//                //TaskDialog.Show("Selected Face Details", faceDetails);

//                // Verify the active view is appropriate
//                View3D view3D = doc.ActiveView as View3D;
//                if (view3D == null || !view3D.IsSectionBoxActive)
//                {
//                    message = "Please make sure you are in a 3D view with an active section box.";
//                    return Result.Failed;
//                }

//                BoundingBoxXYZ sectionBox = view3D.GetSectionBox();
//                XYZ sectionTopCenter = GetSectionBoxTopCenter(sectionBox);
//                double width = sectionBox.Max.X - sectionBox.Min.X;
//                double depth = sectionBox.Max.Y - sectionBox.Min.Y;

//                double defaultHeightBasedOnSectionTopCenter = sectionTopCenter.Z;

//                //UI GOES HERE

//                // Create and show the WPF form
//                ComprehensiveInputForm inputForm = new ComprehensiveInputForm(defaultHeightBasedOnSectionTopCenter, defaultRotationAngle, defaultWindSpeed, defaultRaindropRadius, defaultRainfallIntensity);
//                if (inputForm.ShowDialog() != true)
//                {
//                    message = "User cancelled the operation.";
//                    return Result.Cancelled;
//                }

//                // After the form is closed, check the DialogResult
//                // User clicked OK, access properties from the form
//                //double height = inputForm.Height;
//                //double rotationAngle = inputForm.RotationAngle;
//                //double windSpeed = inputForm.WindSpeed;
//                //double raindropRadiusM = inputForm.RaindropRadiusM;
//                //double rainfallIntensityMmPerHr = inputForm.RainfallIntensityMmPerHr;
//                //bool generateOriginalRays = inputForm.GenerateOriginalRays;
//                //bool generateAdditionalRays = inputForm.GenerateAdditionalRays;
//                //avfApproach = inputForm.AVFApproach;

//                double simulationPlaneHeight = inputForm.Height;
//                double rotationDegrees = inputForm.RotationAngle;
//                windSpeed = inputForm.WindSpeed; // Make sure these are declared at the right scope
//                raindropRadiusM = inputForm.RaindropRadiusM;
//                bool generateOriginalRays = inputForm.GenerateOriginalRays;
//                bool generateAdditionalRays = inputForm.GenerateAdditionalRays;
//                double rainfallIntensityMmPerHr = inputForm.RainfallIntensityMmPerHr;
//                avfApproach = inputForm.AVFApproach;


//                // Ensure an AVF approach was selected
//                if (string.IsNullOrEmpty(avfApproach))
//                {
//                    message = "AVF approach not selected.";
//                    return Result.Failed;
//                }

//                // Then calculate spacing
//                // Use the converted raindrop radius in mm and the rainfall intensity in mm/hr
//                double spacingInMillimeters = RainfallSimulationUtilities.CalculateDropletSpacingInMillimeters(rainfallIntensityMmPerHr, raindropRadiusM);

//                double simulationPlaneArea = width * depth; // Assuming this is the area of the simulation plane in square meters

//                // Calculate the number of raindrops per square meter using the provided raindrop radius
//                double raindropsPerSquareMeter = 1.0 / (Math.PI * Math.Pow(raindropRadiusM, 2));

//                // Now, display the calculated values for debugging
//                //TaskDialog debugDialog = new TaskDialog("Debugging Information")
//                //{
//                //    MainInstruction = "Simulation and Raindrop Information",
//                //    MainContent = $"Spacing in Millimeters: {spacingInMillimeters:N2}\n" +
//                //                  $"Simulation Plane Area (sq. meters): {simulationPlaneArea}\n" +
//                //                  $"Raindrop Radius (m): {raindropRadiusM}\n" +
//                //                  $"Raindrops Per Square Meter: {raindropsPerSquareMeter:N2}",
//                //    AllowCancellation = true
//                //};

//                //debugDialog.Show();

//                // Call the CalculateInclinationAngle method with showDebug set to true
//                var results = CalculateInclinationAngle(windSpeed, raindropRadiusM, true);

//                // Now you can use the results as needed
//                double inclinationAngleRadians = results.InclinationRadians;
//                double inclinationAngleDegrees = results.InclinationDegrees;
//                //double terminalVelocity = results.TerminalVelocity;

//                double angleRadians = rotationDegrees * (Math.PI / 180.0);
//                XYZ right = new XYZ(Math.Sin(angleRadians), Math.Cos(angleRadians), 0);

//                // Calculate inclination angle and terminal velocity
//                //var (inclinationRadians, inclinationDegrees, terminalVelocity) = CalculateInclinationAngle(windSpeed, raindropRadiusM);
//                //TaskDialog.Show("Inclination Angle and Terminal Velocity",
//                //    $"Inclination Angle: {inclinationDegrees} degrees ({inclinationRadians} radians)\n" +
//                //    $"Terminal Velocity: {terminalVelocity} m/s");

//                Stopwatch stopwatch = new Stopwatch();
//                stopwatch.Start();

//                using (Transaction tx = new Transaction(doc, "Ray Projection and AVF Application"))
//                {
//                    tx.Start();

//                    // Check for valid face references
//                    foreach (var faceRef in selectedFaceRefs)
//                    {
//                        Element faceElement = doc.GetElement(faceRef.ElementId);
//                        if (faceElement == null || !(faceElement.GetGeometryObjectFromReference(faceRef) is Face))
//                        {
//                            message = $"Invalid face reference: {faceRef.ElementId}";
//                            return Result.Failed;
//                        }
//                    }

//                    SimulationPlaneInfo simulationPlaneInfo = CreateSimulationPlane(doc, sectionTopCenter, width, depth,
//                                                                                    simulationPlaneHeight, rotationDegrees, view3D);


//                    PlanarFace planeFace = simulationPlaneInfo.PlaneFace as PlanarFace;

//                    // Ensure that planeFace is not null before proceeding
//                    if (planeFace == null)
//                    {
//                        TaskDialog.Show("Error", "Failed to obtain the simulation plane face.");
//                        return Result.Failed;
//                    }

//                    // Generate and visualize rays from the plane face
//                    // Adjust this call within the Execute method
//                    List<IdentifiedRay> rays = GenerateAndVisualizeRaysFromFace(
//                        simulationPlaneInfo.PlaneFace as PlanarFace,
//                        doc,
//                        spacingInMillimeters, // Use the calculated spacing
//                        rayLength,
//                        visualizeRays,
//                        right,
//                        rotationDegrees,
//                        generateOriginalRays,
//                        generateAdditionalRays,
//                        selectedFaceRefs);


//                    // Create grids for faces and calculate first intersections...
//                    var faceToFirstIntersectionsMap = GetAllandFilterFirstRayFaceIntersections(doc, rays, selectedFaceRefs);

//                    // The desired grid cell size in meters.
//                    double gridSizeInMeters = 0.5; // Example for half a meter grid cell size

//                    // Call the method to create uniform grids for the selected faces
//                    var faceGrids = CreateUniformGridsForFaces(doc, selectedFaceRefs, gridSizeInMeters);

//                    // After obtaining the intersections from GetAllandFilterFirstRayFaceIntersections
//                    var faceIntersectionPoints = GetAllandFilterFirstRayFaceIntersections(doc, rays, selectedFaceRefs);

//                    // Adapt the data to the expected format for CountRayHitsPerGrid
//                    var adaptedIntersections = AdaptIntersectionData(faceIntersectionPoints);

//                    // Now call CountRayHitsPerGrid with the adapted data
//                    CountRayHitsPerGrid(doc, adaptedIntersections, faceGrids);

//                    // Optional: Log the completion of grid hit counting
//                    logBuilder.AppendLine($"Completed counting ray hits per grid for {faceGrids.Count} faces.");


//                    // Display ray-face intersections in a TaskDialog (if needed)
//                    //Dictionary<Reference, List<XYZ>> faceIntersectionPoints = GetAllandFilterFirstRayFaceIntersections(doc, rays, selectedFaceRefs);

//                    // Set up AVF
//                    SpatialFieldManager sfm = SpatialFieldManager.GetSpatialFieldManager(doc.ActiveView);
//                    if (sfm == null)
//                    {
//                        sfm = SpatialFieldManager.CreateSpatialFieldManager(doc.ActiveView, 1);
//                    }
//                    else
//                    {
//                        // Clear existing AVF data
//                        sfm.Clear();
//                    }

//                    AnalysisDisplayStyle analysisDisplayStyle = CreateDefaultAnalysisDisplayStyle(doc, "Custom AVF Style");
//                    doc.ActiveView.AnalysisDisplayStyleId = analysisDisplayStyle.Id;

//                    // Example user prompt for selecting the AVF application approach
//                    // string avfApproach = GetUserInputForAVFApproach(); // Implement this method based on your UI logic

//                    if (avfApproach == "Grid-Based")
//                    {
//                        ApplyAVFToSelectedFacesBasedOnGrids(doc, faceGrids);
//                    }
//                    else if (avfApproach == "Intersection-Based")
//                    {
//                        ApplyAVFToSelectedFaces(doc, faceIntersectionPoints);
//                    }


//                    stopwatch.Stop();
//                    TimeSpan ts = stopwatch.Elapsed;
//                    string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
//                        ts.Hours, ts.Minutes, ts.Seconds, ts.Milliseconds / 10);
//                    TaskDialog.Show("Execution Time", $"Execution Time: {elapsedTime}");

//                    // Display grid hit counts
//                    DisplayGridHitCounts(doc, faceGrids);

//                    // Display the accumulated log messages
//                    //TaskDialog.Show("Execution Log", logBuilder.ToString());


//                    tx.Commit();
//                }

//                return Result.Succeeded;
//            }

//            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
//            {
//                message = "Operation canceled by the user.";
//                return Result.Cancelled;
//            }
//            catch (Exception ex)
//            {
//                message = $"Unexpected error occurred: {ex.Message}\n{ex.StackTrace}";
//                TaskDialog.Show("Error", message);
//                return Result.Failed;
//            }
//        }

//        private string GetFaceDetails(Document doc, IList<Reference> faceRefs)
//        {
//            StringBuilder faceDetails = new StringBuilder();
//            foreach (var faceRef in faceRefs)
//            {
//                Element faceElement = doc.GetElement(faceRef.ElementId);
//                if (faceElement != null)
//                {
//                    faceDetails.AppendLine($"Element ID: {faceElement.Id}, Face Reference: {faceRef.ConvertToStableRepresentation(doc)}");
//                }
//            }
//            return faceDetails.ToString();
//        }

//        private XYZ GetSectionBoxTopCenter(BoundingBoxXYZ sectionBox)
//        {
//            return new XYZ(
//                (sectionBox.Min.X + sectionBox.Max.X) / 2,
//                (sectionBox.Min.Y + sectionBox.Max.Y) / 2,
//                sectionBox.Max.Z // Top Z coordinate
//            );
//        }
//        private SimulationPlaneInfo CreateSimulationPlane(Document doc, XYZ center, double width, double depth, double height, double directionAngle, View3D view3D)
//        {

//            SimulationPlaneInfo simulationInfo = new SimulationPlaneInfo();
//            StringBuilder faceInfoBuilder = new StringBuilder();

//            // Assume that the top view of the hexadecagon is flat on the XY plane
//            // The 0 degree direction is aligned with the positive Y axis (North)
//            // The 90 degree direction is aligned with the positive X axis (East)

//            // Calculate the right vector (East) based on the user input direction angle
//            // The direction angle is counterclockwise from the North (+Y axis)
//            double angleRadians = directionAngle * (Math.PI / 180.0);
//            XYZ right = new XYZ(Math.Sin(angleRadians), Math.Cos(angleRadians), 0);
//            XYZ up = new XYZ(-right.Y, right.X, 0); // Perpendicular to the right vector

//            // Define the rectangle corners based on the right and up vectors
//            XYZ p1 = center - right * width / 2 + up * depth / 2;
//            XYZ p2 = center + right * width / 2 + up * depth / 2;
//            XYZ p3 = center + right * width / 2 - up * depth / 2;
//            XYZ p4 = center - right * width / 2 - up * depth / 2;

//            // Create lines for the rectangle edges
//            Line edge1 = Line.CreateBound(p1, p2);
//            Line edge2 = Line.CreateBound(p2, p3);
//            Line edge3 = Line.CreateBound(p3, p4);
//            Line edge4 = Line.CreateBound(p4, p1);

//            // Create and return a curve loop
//            CurveLoop baseRectangle = new CurveLoop();
//            baseRectangle.Append(edge1);
//            baseRectangle.Append(edge2);
//            baseRectangle.Append(edge3);
//            baseRectangle.Append(edge4);

//            // Define the plane geometry
//            XYZ elevatedCenter = new XYZ(center.X, center.Y, center.Z + height);
//            XYZ normal = new XYZ(0, 0, 1); // Normal pointing upwards (Z-axis)

//            double extrusionDepth = 0.1; // Assuming a thin plane
//            Solid planeSolid = GeometryCreationUtilities.CreateExtrusionGeometry(new CurveLoop[] { baseRectangle }, normal, extrusionDepth);

//            // Create a DirectShape for visualization
//            DirectShape ds = DirectShape.CreateElement(doc, new ElementId(BuiltInCategory.OST_GenericModel));
//            ds.ApplicationId = "SimulationPlane";
//            ds.ApplicationDataId = Guid.NewGuid().ToString();

//            // Calculate the endpoints for the existing line
//            XYZ lineStartPoint = elevatedCenter + right * (width / 2) - up * (depth / 2);
//            XYZ lineEndPoint = lineStartPoint + up * depth;

//            // Create the model line for the existing line (as before)
//            Plane plane = Plane.CreateByNormalAndOrigin(normal, elevatedCenter);
//            SketchPlane sketchPlane = SketchPlane.Create(doc, plane);
//            Line existingLine = Line.CreateBound(lineStartPoint, lineEndPoint);
//            ModelCurve existingModelCurve = doc.Create.NewModelCurve(existingLine, sketchPlane);

//            PlanarFace simulationFace = null;
//            Reference faceReference = null;

//            foreach (Face face in planeSolid.Faces)
//            {
//                if (face is PlanarFace planarFace && planarFace.FaceNormal.IsAlmostEqualTo(-XYZ.BasisZ))
//                {
//                    simulationFace = planarFace;
//                    faceReference = face.Reference; // Get the reference of the face.

//                    // Accumulate face info for visualization
//                    faceInfoBuilder.AppendLine($"DirectShape Element ID: {ds.Id.IntegerValue}, " +
//                                               $"DirectShape Unique ID: {ds.UniqueId}, " +
//                                               $"Face: {face}");

//                    // If you want to include the face reference stable representation
//                    if (faceReference != null)
//                    {
//                        string stableReference = faceReference.ConvertToStableRepresentation(doc);
//                        faceInfoBuilder.AppendLine($"Face Reference: {stableReference}");
//                    }
//                }
//            }

//            if (simulationFace != null)
//            {
//                XYZ extrusionDirection = new XYZ(0, 0, 0.01); // Small extrusion in the Z direction
//                Solid extrudedSolid = GeometryCreationUtilities.CreateExtrusionGeometry(new List<CurveLoop> { baseRectangle }, extrusionDirection, extrusionDepth);
//                IList<GeometryObject> geometry = new List<GeometryObject> { extrudedSolid };
//                ds.SetShape(geometry);
//            }

//            // Populate the SimulationPlaneInfo properties
//            simulationInfo.PlaneFace = simulationFace;
//            simulationInfo.SimulationShapeId = ds.Id;
//            simulationInfo.PlaneOrigin = elevatedCenter;
//            simulationInfo.PlaneLength = width;
//            simulationInfo.PlaneWidth = depth;

//            // Show all face info in a TaskDialog
//            //if (faceInfoBuilder.Length > 0)
//            //{
//            //    TaskDialog.Show("Face Info", faceInfoBuilder.ToString());
//            //}

//            return simulationInfo;
//        }
//        private List<IdentifiedRay> GenerateAndVisualizeRaysFromFace(
//             Face planeFace,
//             Document doc,
//             double spacingInMillimeters,
//             double rayLength,
//             bool visualizeRays,
//             XYZ right,
//             double rotationDegrees,
//             bool generateOriginalRays,
//             bool generateAdditionalRays,
//             IList<Reference> faceRefs)
//        {
//            // Convert spacing from millimeters to meters
//            double spacingInMeters = spacingInMillimeters / 1000.0;

//            ConcurrentBag<IdentifiedRay> concurrentIdentifiedRays = new ConcurrentBag<IdentifiedRay>();
//            EdgeArrayArray edgeLoops = planeFace.EdgeLoops;
//            EdgeArray edges = edgeLoops.get_Item(0); // Assuming the first loop is the outer loop

//            XYZ p1 = edges.get_Item(0).AsCurve().GetEndPoint(0);
//            XYZ p2 = edges.get_Item(1).AsCurve().GetEndPoint(0);
//            XYZ p3 = edges.get_Item(2).AsCurve().GetEndPoint(0);
//            XYZ p4 = edges.get_Item(3).AsCurve().GetEndPoint(0);

//            XYZ side1 = p2 - p1;
//            XYZ side2 = p3 - p2;
//            double length = side1.GetLength();
//            double width = side2.GetLength();

//            int rayDensityLength = (int)Math.Round(length / spacingInMeters);
//            int rayDensityWidth = (int)Math.Round(width / spacingInMeters);

//            XYZ dir1 = side1.Normalize();
//            XYZ dir2 = side2.Normalize();

//            try
//            {
//                Parallel.For(0, rayDensityLength + 1, i =>
//                {
//                    //try
//                    //{
//                    XYZ startPoint = p1 + i * dir1 * (length / rayDensityLength);
//                    for (int j = 0; j <= rayDensityWidth; j++)
//                    {
//                        XYZ gridPoint = startPoint + j * dir2 * (width / rayDensityWidth);

//                        if (generateOriginalRays)
//                        {
//                            XYZ endPointDown = gridPoint - new XYZ(0, 0, rayLength);
//                            IdentifiedRay identifiedRay = CreateIdentifiedRay(gridPoint, endPointDown, XYZ.BasisZ.Negate());
//                            concurrentIdentifiedRays.Add(identifiedRay);
//                        }

//                        if (generateAdditionalRays)
//                        {
//                            var inclinationResults = CalculateInclinationAngle(windSpeed, raindropRadiusM, false);
//                            double inclinationAngleRadians = inclinationResults.InclinationRadians;
//                            XYZ inclinedDirection = new XYZ(
//                                Math.Sin(inclinationAngleRadians) * -right.X,
//                                Math.Sin(inclinationAngleRadians) * -right.Y,
//                                -Math.Cos(inclinationAngleRadians));
//                            XYZ endPointInclined = gridPoint + inclinedDirection * rayLength;
//                            IdentifiedRay identifiedRay = CreateIdentifiedRay(gridPoint, endPointInclined, inclinedDirection);
//                            concurrentIdentifiedRays.Add(identifiedRay);
//                        }
//                    }
//                    //  }
//                    //catch (Exception ex) // Catch block to handle exceptions
//                    //{
//                    //    // Handle the exception, e.g., log it or display a message
//                    //    Debug.WriteLine($"An error occurred: {ex.Message}");
//                    //}
//                });

//            }
//            catch (AggregateException ae)
//            {
//                // Handle each individual exception
//                foreach (var ex in ae.Flatten().InnerExceptions)
//                {
//                    // Log the exception details
//                    // You might want to log this information to a file or a logging system
//                    Debug.WriteLine($"Exception: {ex.Message}");
//                    Debug.WriteLine($"StackTrace: {ex.StackTrace}");
//                }
//                // Depending on how critical the exception is, you may rethrow, return, or handle the error
//                // Rethrow the exception for the calling method to handle
//                throw;
//            }
//            List<IdentifiedRay> identifiedRays = concurrentIdentifiedRays.ToList();

//            if (visualizeRays)
//            {
//                // Visualize rays; Ensure any non-thread-safe actions are performed outside the parallel region
//                VisualizeRays(identifiedRays, doc, faceRefs);
//            }

//            return identifiedRays;
//        }

//        private static int lastRayId = 0;

//        private IdentifiedRay CreateIdentifiedRay(XYZ startPoint, XYZ endPoint, XYZ direction)
//        {
//            Line rayLine = Line.CreateBound(startPoint, endPoint);
//            int newId = Interlocked.Increment(ref lastRayId); // Atomically increments the ID
//            IdentifiedRay identifiedRay = new IdentifiedRay
//            {
//                Ray = rayLine,
//                RayId = $"Ray_{newId}", // Unique ID based on the atomic increment
//            };
//            return identifiedRay;
//        }

//        private void VisualizeRays(List<IdentifiedRay> rays, Document doc, IList<Reference> faceRefs)
//        {
//            foreach (IdentifiedRay identifiedRay in rays)
//            {
//                Line rayLine = identifiedRay.Ray;
//                if (rayLine != null && rayLine.Length > doc.Application.ShortCurveTolerance)
//                {
//                    // Create a DirectShape to visualize the ray
//                    DirectShape ds = DirectShape.CreateElement(doc, new ElementId(BuiltInCategory.OST_Lines));
//                    ds.ApplicationId = "Rays Visualization";
//                    ds.ApplicationDataId = identifiedRay.RayId;
//                    ds.SetShape(new List<GeometryObject> { rayLine });
//                }
//            }
//        }

//        private (double InclinationRadians, double InclinationDegrees, double TerminalVelocity) CalculateInclinationAngle(double windSpeed, double raindropRadiusM, bool showDebug = false)
//        {
//            try
//            {
//                // Constants
//                double g = 9.81; // Acceleration due to gravity in m/s^2
//                double rhoW = 1000; // Density of water in kg/m^3
//                double rhoA = 1.225; // Density of air at sea level in kg/m^3
//                double Cd = 0.47; // Drag coefficient for a sphere

//                // Convert raindrop radius from millimeters to meters
//                //raindropRadius = raindropRadius / 1000.0; // If raindropRadius was initially in mm

//                double volume = (4.0 / 3.0) * Math.PI * Math.Pow(raindropRadiusM, 3);
//                double mass = rhoW * volume;

//                // Calculate cross-sectional area of the raindrop
//                double area = Math.PI * Math.Pow(raindropRadiusM, 2);

//                // Calculate terminal velocity
//                double vT = Math.Sqrt((2 * mass * g) / (rhoA * area * Cd));

//                // Calculate inclination angle in radians
//                double inclinationAngleRadians = Math.Atan(windSpeed / vT);

//                // Convert to degrees
//                double inclinationDegrees = inclinationAngleRadians * (180.0 / Math.PI);

//                // Show debug information if requested
//                //if (showDebug)
//                //{
//                //    TaskDialog debugDialog = new TaskDialog("Debug Information")
//                //    {
//                //        MainInstruction = "Debug Information for Inclination Calculation",
//                //        AllowCancellation = true,
//                //        ExpandedContent = $"Calculating the inclination angle...\n" +
//                //                          $"Volume: {volume} m^3\n" +
//                //                          $"Mass: {mass} kg\n" +
//                //                          $"Cross-sectional Area: {area} m^2\n" +
//                //                          $"Terminal Velocity vT: {vT} m/s\n" +
//                //                          $"Inclination Angle Radians: {inclinationAngleRadians}\n" +
//                //                          $"Inclination Angle Degrees: {inclinationDegrees}"
//                //    };

//                //    debugDialog.Show();
//                //}

//                return (inclinationAngleRadians, inclinationDegrees, vT);
//            }
//            catch (Exception ex)
//            {
//                TaskDialog.Show("Error", $"An error occurred: {ex.Message}");
//                throw; // Re-throw the exception for higher-level handling
//            }
//        }

//        private Dictionary<Reference, List<XYZ>> GetAllandFilterFirstRayFaceIntersections(Document doc, List<IdentifiedRay> rays, IList<Reference> faceRefs)
//        {
//            var faceToFirstIntersectionsMap = new Dictionary<Reference, List<(XYZ IntersectionPoint, string RayId)>>();
//            var faceIntersectionPoints = new Dictionary<Reference, List<XYZ>>();

//            foreach (IdentifiedRay ray in rays)
//            {
//                IntersectionInfo firsttIntersectionInfo = null;
//                foreach (Reference faceRef in faceRefs)
//                {
//                    Face face = doc.GetElement(faceRef.ElementId).GetGeometryObjectFromReference(faceRef) as Face;
//                    if (face == null) continue;

//                    IntersectionResultArray results;
//                    SetComparisonResult intersectResult = face.Intersect(ray.Ray, out results);

//                    if (intersectResult == SetComparisonResult.Overlap)
//                    {
//                        foreach (IntersectionResult result in results)
//                        {
//                            var distance = result.XYZPoint.DistanceTo(ray.Ray.GetEndPoint(0));
//                            if (firsttIntersectionInfo == null || distance < firsttIntersectionInfo.Distance)
//                            {
//                                firsttIntersectionInfo = new IntersectionInfo
//                                {
//                                    ElementId = faceRef.ElementId,
//                                    Face = face,
//                                    IntersectionPoint = result.XYZPoint,
//                                    Distance = distance,
//                                    FaceReference = faceRef,
//                                    RayId = ray.RayId
//                                };
//                            }
//                        }
//                    }
//                }

//                if (firsttIntersectionInfo != null)
//                {
//                    if (!faceToFirstIntersectionsMap.ContainsKey(firsttIntersectionInfo.FaceReference))
//                    {
//                        faceToFirstIntersectionsMap[firsttIntersectionInfo.FaceReference] = new List<(XYZ IntersectionPoint, string RayId)>();
//                    }

//                    faceToFirstIntersectionsMap[firsttIntersectionInfo.FaceReference].Add((firsttIntersectionInfo.IntersectionPoint, firsttIntersectionInfo.RayId));
//                }
//            }

//            // Build the string for the TaskDialog grouping by face and populate faceIntersectionPoints
//            StringBuilder dialogContent = new StringBuilder();
//            foreach (var faceRef in faceToFirstIntersectionsMap.Keys)
//            {
//                ElementId faceElementId = faceRef.ElementId;
//                string faceRefString = faceRef.ConvertToStableRepresentation(doc);
//                dialogContent.AppendLine($"Face Reference: {faceRefString}, Element ID: {faceElementId}");
//                dialogContent.AppendLine("First Intersection Points and Ray IDs:");

//                var intersectionPoints = new List<XYZ>();
//                foreach (var intersection in faceToFirstIntersectionsMap[faceRef])
//                {
//                    dialogContent.AppendLine($"Point: {intersection.IntersectionPoint}, Ray ID: {intersection.RayId}");
//                    intersectionPoints.Add(intersection.IntersectionPoint);
//                }
//                faceIntersectionPoints[faceRef] = intersectionPoints;

//                dialogContent.AppendLine("-----");
//            }

//            // Show the TaskDialog
//            //TaskDialog.Show("First Ray-Face Intersection Details", dialogContent.ToString());

//            return faceIntersectionPoints;
//        }

//        private Face GetFaceFromReference(Document doc, Reference faceRef)
//        {
//            Element element = doc.GetElement(faceRef.ElementId);
//            if (element != null)
//            {
//                return element.GetGeometryObjectFromReference(faceRef) as Face;
//            }
//            return null;
//        }

//        private Dictionary<Reference, List<UVGridCell>> CreateUniformGridsForFaces(Document doc, IList<Reference> faceRefs, double gridSizeInMeters)
//        {
//            var faceGrids = new Dictionary<Reference, List<UVGridCell>>();

//            foreach (var faceRef in faceRefs)
//            {
//                Face face = doc.GetElement(faceRef.ElementId).GetGeometryObjectFromReference(faceRef) as Face;
//                if (face == null) continue;

//                BoundingBoxUV bboxUV = face.GetBoundingBox();
//                double faceWidth = bboxUV.Max.U - bboxUV.Min.U;
//                double faceHeight = bboxUV.Max.V - bboxUV.Min.V;

//                // Calculate the number of divisions along each dimension based on the fixed grid size.
//                int uDivisions = (int)Math.Ceiling(faceWidth / gridSizeInMeters);
//                int vDivisions = (int)Math.Ceiling(faceHeight / gridSizeInMeters);

//                var gridCells = new List<UVGridCell>();
//                string faceUniqueId = faceRef.ConvertToStableRepresentation(doc);

//                // Use the predefined grid size to determine the UV step increments.
//                double uStep = gridSizeInMeters;
//                double vStep = gridSizeInMeters;

//                // Adjust the last cell size if necessary to cover the edge of the face.
//                double lastUCellSize = faceWidth % gridSizeInMeters;
//                double lastVCellSize = faceHeight % gridSizeInMeters;

//                for (int u = 0; u < uDivisions; u++)
//                {
//                    for (int v = 0; v < vDivisions; v++)
//                    {
//                        // Determine the UV coordinates for the current cell, adjusting for the last cell.
//                        double minU = bboxUV.Min.U + u * uStep;
//                        double minV = bboxUV.Min.V + v * vStep;
//                        double maxU = (u == uDivisions - 1 && lastUCellSize > 0) ? minU + lastUCellSize : minU + uStep;
//                        double maxV = (v == vDivisions - 1 && lastVCellSize > 0) ? minV + lastVCellSize : minV + vStep;

//                        gridCells.Add(new UVGridCell
//                        {
//                            Id = $"{faceUniqueId}_Grid_{u}_{v}",
//                            MinUV = new UV(minU, minV),
//                            MaxUV = new UV(maxU, maxV),
//                            Hits = 0,
//                        });
//                    }
//                }

//                faceGrids.Add(faceRef, gridCells);
//            }

//            return faceGrids;
//        }

//        private Dictionary<Reference, List<(XYZ IntersectionPoint, string RayId)>> AdaptIntersectionData(Dictionary<Reference, List<XYZ>> originalData)
//        {
//            var adaptedData = new Dictionary<Reference, List<(XYZ IntersectionPoint, string RayId)>>();
//            foreach (var kvp in originalData)
//            {
//                var adaptedList = kvp.Value.Select((intersectionPoint, index) => (intersectionPoint, $"Ray_{index + 1}")).ToList();
//                adaptedData.Add(kvp.Key, adaptedList);
//            }
//            return adaptedData;
//        }

//        private void CountRayHitsPerGrid(Document doc, Dictionary<Reference, List<(XYZ IntersectionPoint, string RayId)>> faceToFirstIntersectionsMap, Dictionary<Reference, List<UVGridCell>> faceGrids)
//        {
//            // Iterate over each face reference in the intersections map
//            foreach (var faceEntry in faceToFirstIntersectionsMap)
//            {
//                var faceRef = faceEntry.Key;
//                var intersections = faceEntry.Value;

//                // Retrieve the corresponding grid cells for the current face
//                if (!faceGrids.TryGetValue(faceRef, out List<UVGridCell> gridCells))
//                {
//                    continue; // If there are no grid cells for the face, skip to the next face
//                }

//                Face face = GetFaceFromReference(doc, faceRef);
//                if (face == null) continue;

//                // Iterate over each intersection point for the current face
//                foreach (var intersectionInfo in intersections)
//                {
//                    XYZ intersectionPoint = intersectionInfo.IntersectionPoint;

//                    // Project the 3D intersection point onto the 2D UV plane of the face
//                    UV uvPoint = face.Project(intersectionPoint)?.UVPoint;
//                    if (uvPoint == null) continue; // If the point cannot be projected, skip to the next point

//                    // Find the grid cell that contains the UV point and increment its hit count
//                    foreach (var cell in gridCells)
//                    {
//                        if (uvPoint.U >= cell.MinUV.U && uvPoint.U < cell.MaxUV.U &&
//                            uvPoint.V >= cell.MinUV.V && uvPoint.V < cell.MaxUV.V)
//                        {
//                            cell.Hits++; // Increment the hit count for the grid cell
//                            break; // Since each point can only be in one cell, break after finding it
//                        }
//                    }
//                }
//            }
//        }

//        private void DisplayGridHitCounts(Document doc, Dictionary<Reference, List<UVGridCell>> faceGrids)
//        {
//            StringBuilder sb = new StringBuilder();
//            foreach (var kvp in faceGrids)
//            {
//                var faceRef = kvp.Key;
//                var gridCells = kvp.Value;

//                Element faceElement = doc.GetElement(faceRef.ElementId);
//                sb.AppendLine($"Face ID: {faceElement.Id.IntegerValue}");

//                foreach (var cell in gridCells)
//                {
//                    sb.AppendLine($"Grid ID: {cell.Id}, Hits: {cell.Hits}");
//                }

//                sb.AppendLine("----------");
//            }

//            // Display in a Revit Task Dialog or any other appropriate method
//            TaskDialog.Show("Grid Hit Counts", sb.ToString());
//        }

//        private void ApplyAVFToSelectedFaces(Document doc, Dictionary<Reference, List<XYZ>> faceIntersectionPoints)
//        {
//            foreach (var entry in faceIntersectionPoints)
//            {
//                Reference faceRef = entry.Key;
//                List<XYZ> intersections = entry.Value;

//                Face face = GetFaceFromReference(doc, faceRef);
//                if (face != null)
//                {
//                    SetUpAndApplyAVF(doc, face, faceRef, intersections);
//                }
//            }
//        }

//        //tHIS IS USED FOR THE ApplyAVFToSelectedFacesBasedOnGrids
//        private AnalysisDisplayStyle CreateCustomAnalysisDisplayStyle(Document doc, int maxHitCount)
//        {
//            // Define a unique name for the style to avoid conflicts
//            string styleName = "Custom Grid-Based Analysis Display Style";

//            // Check if the style already exists
//            AnalysisDisplayStyle existingStyle = new FilteredElementCollector(doc)
//                .OfClass(typeof(AnalysisDisplayStyle))
//                .Cast<AnalysisDisplayStyle>()
//                .FirstOrDefault(a => a.Name.Equals(styleName));

//            // If it exists, return the existing style
//            if (existingStyle != null) return existingStyle;

//            // No transaction is started here, assuming the caller manages it

//            // Create color settings for the style
//            AnalysisDisplayColorSettings colorSettings = new AnalysisDisplayColorSettings();
//            colorSettings.MinColor = new Autodesk.Revit.DB.Color(255, 255, 255); // White for minimum values
//            colorSettings.MaxColor = new Autodesk.Revit.DB.Color(0, 0, 255);     // Blue for maximum values

//            // Create surface settings for the style
//            AnalysisDisplayColoredSurfaceSettings surfaceSettings = new AnalysisDisplayColoredSurfaceSettings();
//            surfaceSettings.ShowGridLines = false;

//            // Adjust the legend settings to show actual hit counts
//            AnalysisDisplayLegendSettings legendSettings = new AnalysisDisplayLegendSettings
//            {
//                ShowLegend = true,
//                NumberOfSteps = maxHitCount,
//                ShowDataDescription = true,
//                Rounding = 1 // Use 1 for integer values in the legend
//            };

//            // Create the analysis display style with the updated legend settings
//            AnalysisDisplayStyle newStyle = AnalysisDisplayStyle.CreateAnalysisDisplayStyle(doc, styleName, surfaceSettings, colorSettings, legendSettings);

//            return newStyle;
//        }

//        private void ApplyAVFToSelectedFacesBasedOnGrids(Document doc, Dictionary<Reference, List<UVGridCell>> faceGrids)
//        {
//            SpatialFieldManager sfm = SpatialFieldManager.GetSpatialFieldManager(doc.ActiveView);
//            if (sfm == null)
//            {
//                sfm = SpatialFieldManager.CreateSpatialFieldManager(doc.ActiveView, 1);
//            }

//            // Find the maximum hit count across all grids to use for color mapping and legend steps
//            // Calculate maxHitCount
//            maxHitCount = faceGrids.SelectMany(fg => fg.Value).Max(gc => gc.Hits);

//            // Create a custom analysis display style using maxHitCount
//            AnalysisDisplayStyle analysisDisplayStyle = CreateCustomAnalysisDisplayStyle(doc, maxHitCount);
//            doc.ActiveView.AnalysisDisplayStyleId = analysisDisplayStyle.Id;

//            foreach (KeyValuePair<Reference, List<UVGridCell>> faceGridEntry in faceGrids)
//            {
//                Reference faceRef = faceGridEntry.Key;
//                List<UVGridCell> gridCells = faceGridEntry.Value;

//                IList<ValueAtPoint> valueList = new List<ValueAtPoint>();
//                IList<UV> uvPoints = new List<UV>();

//                foreach (UVGridCell cell in gridCells)
//                {
//                    // Directly use the hit count
//                    double value = cell.Hits;

//                    // Add the value to the list for AVF
//                    valueList.Add(new ValueAtPoint(new List<double> { value }));

//                    // Determine the UV position for this cell (center point)
//                    double uCenter = (cell.MinUV.U + cell.MaxUV.U) / 2;
//                    double vCenter = (cell.MinUV.V + cell.MaxUV.V) / 2;
//                    uvPoints.Add(new UV(uCenter, vCenter));
//                }

//                // Register the spatial field primitive for this face reference
//                int primitiveId = sfm.AddSpatialFieldPrimitive(faceRef);

//                // Create field domain points and field values
//                FieldDomainPointsByUV fieldPoints = new FieldDomainPointsByUV(uvPoints);
//                FieldValues fieldValues = new FieldValues(valueList);

//                // Define the analysis results schema index
//                int schemaIndex = GetOrCreateAnalysisResultSchemaIndex(sfm, "Grid-Based Schema", "Visualization based on actual number of rays in a cell");

//                // Update the spatial field primitive with the AVF data
//                sfm.UpdateSpatialFieldPrimitive(primitiveId, fieldPoints, fieldValues, schemaIndex);
//            }
//        }

//        private void SetUpAndApplyAVF(Document doc, Face face, Reference faceRef, List<XYZ> intersections)
//        {
//            if (face == null) throw new ArgumentNullException(nameof(face));
//            if (faceRef == null) throw new ArgumentNullException(nameof(faceRef));
//            if (intersections == null) throw new ArgumentNullException(nameof(intersections));
//            if (intersections.Count == 0) throw new ArgumentException("intersections list cannot be empty", nameof(intersections));

//            // Create or get SpatialFieldManager for the active view
//            SpatialFieldManager sfm = SpatialFieldManager.GetSpatialFieldManager(doc.ActiveView);
//            if (sfm == null)
//            {
//                sfm = SpatialFieldManager.CreateSpatialFieldManager(doc.ActiveView, 1);
//            }

//            // Create an AnalysisDisplayStyle
//            AnalysisDisplayStyle analysisDisplayStyle = CreateDefaultAnalysisDisplayStyle(doc, "Custom AVF Style");

//            // Set the AnalysisDisplayStyle to the active view
//            doc.ActiveView.AnalysisDisplayStyleId = analysisDisplayStyle.Id;

//            // Register a new result schema with the SpatialFieldManager
//            AnalysisResultSchema resultSchema = new AnalysisResultSchema("Custom Schema", "Description");
//            int schemaIndex = GetOrCreateAnalysisResultSchemaIndex(sfm, "Custom Schema", "Description");

//            // Prepare data for AVF
//            IList<UV> uvPoints;
//            FieldDomainPointsByUV fieldPoints = GetFieldDomainPointsByUV(face, out uvPoints);
//            FieldValues fieldValues = GetFieldValuesForIntersections(uvPoints, intersections, face);

//            // Add or Update the spatial field primitive for the face
//            int primitiveId = sfm.AddSpatialFieldPrimitive(faceRef);
//            sfm.UpdateSpatialFieldPrimitive(primitiveId, fieldPoints, fieldValues, schemaIndex);
//        }

//        private FieldValues GetFieldValuesForIntersections(IList<UV> uvPoints, List<XYZ> intersections, Face face)
//        {
//            var values = new List<ValueAtPoint>();
//            double proximityThreshold = 0.26; // Adjust this threshold as needed 0.26,15

//            foreach (UV uv in uvPoints)
//            {
//                XYZ point = face.Evaluate(uv);
//                double nearestDistance = intersections.Min(intersect => point.DistanceTo(intersect));
//                double value = nearestDistance <= proximityThreshold ? 1 : 0;

//                values.Add(new ValueAtPoint(new List<double> { value }));
//            }

//            return new FieldValues(values);
//        }

//        private UV GetUVPoint(Face face, XYZ xyzPoint)
//        {
//            IntersectionResult result = face.Project(xyzPoint);
//            if (result == null) return null;
//            return result.UVPoint;
//        }

//        private int GetOrCreateAnalysisResultSchemaIndex(SpatialFieldManager sfm, string schemaName, string schemaDescription)
//        {
//            foreach (int index in sfm.GetRegisteredResults())
//            {
//                AnalysisResultSchema existingSchema = sfm.GetResultSchema(index);
//                if (existingSchema.Name.Equals(schemaName))
//                {
//                    return index;
//                }
//            }

//            AnalysisResultSchema newSchema = new AnalysisResultSchema(schemaName, schemaDescription);
//            return sfm.RegisterResult(newSchema);
//        }

//        private AnalysisDisplayStyle CreateDefaultAnalysisDisplayStyle(Document doc, string styleName)
//        {
//            // Define the ranges of values
//            double[] ranges = new double[] { 0.0, 0.5, 1.0 };

//            // Define the colors for each range
//            // Define the colors for each range
//            Autodesk.Revit.DB.Color[] colors = new Autodesk.Revit.DB.Color[]
//            {
//                new Autodesk.Revit.DB.Color(255, 255, 255), // White
//                new Autodesk.Revit.DB.Color(255, 0, 0),     // Red
//                new Autodesk.Revit.DB.Color(173, 216, 230)  // Light Blue
//            };

//            // Check that the arrays have the same length
//            if (ranges.Length != colors.Length)
//            {
//                throw new Exception("The ranges and colors arrays must have the same length.");
//            }

//            // Attempt to find an existing style that matches the provided styleName
//            var existingStyle = new FilteredElementCollector(doc)
//                .OfClass(typeof(AnalysisDisplayStyle))
//                .Cast<AnalysisDisplayStyle>()
//                .FirstOrDefault(style => style.Name.Equals(styleName));

//            // If an existing style is found, return it
//            if (existingStyle != null)
//            {
//                return existingStyle;
//            }

//            // If no existing style was found, create a new one
//            AnalysisDisplayColoredSurfaceSettings surfaceSettings = new AnalysisDisplayColoredSurfaceSettings
//            {
//                ShowGridLines = true
//            };

//            AnalysisDisplayColorSettings colorSettings = new AnalysisDisplayColorSettings
//            {
//                MinColor = colors[0],
//                MaxColor = colors[colors.Length - 1]
//            };

//            //AnalysisDisplayLegendSettings legendSettings = new AnalysisDisplayLegendSettings
//            //{
//            //    ShowLegend = true,
//            //    NumberOfSteps = colors.Length - 1, // One less than the number of colors
//            //    ShowDataDescription = true,
//            //    Rounding = 0.1
//            //};

//            AnalysisDisplayLegendSettings legendSettings = new AnalysisDisplayLegendSettings
//            {
//                ShowLegend = true,
//                NumberOfSteps = maxHitCount, // Adjust this to the actual maximum hit count
//                ShowDataDescription = true,
//                Rounding = 1 // Use 1 if you want integer values in the legend
//            };

//            // Create and return the new AnalysisDisplayStyle
//            return AnalysisDisplayStyle.CreateAnalysisDisplayStyle(doc, styleName, surfaceSettings, colorSettings, legendSettings);
//        }

//        private FieldDomainPointsByUV GetFieldDomainPointsByUV(Face face, out IList<UV> uvPoints)
//        {
//            uvPoints = new List<UV>();
//            BoundingBoxUV bbox = face.GetBoundingBox();
//            double uStep = (bbox.Max.U - bbox.Min.U) / 10; // 10 steps across the U direction
//            double vStep = (bbox.Max.V - bbox.Min.V) / 10; // 10 steps across the V direction

//            for (double u = bbox.Min.U; u <= bbox.Max.U; u += uStep)
//            {
//                for (double v = bbox.Min.V; v <= bbox.Max.V; v += vStep)
//                {
//                    UV uv = new UV(u, v);
//                    if (face.IsInside(uv))
//                    {
//                        uvPoints.Add(uv);
//                    }
//                }
//            }

//            return new FieldDomainPointsByUV(uvPoints);
//        }

//        private FieldValues GetFieldValues(IList<UV> uvPoints)
//        {
//            IList<ValueAtPoint> values = new List<ValueAtPoint>();
//            foreach (UV uv in uvPoints)
//            {
//                double sampleValue = uv.U + uv.V; // Replace this with actual analysis value
//                values.Add(new ValueAtPoint(new List<double> { sampleValue }));
//            }

//            return new FieldValues(values);
//        }

//    }

//    // Updated IntersectionInfo class to include distance
//    public class IntersectionInfo
//    {
//        public ElementId ElementId { get; set; }
//        public XYZ IntersectionPoint { get; set; }
//        public Face Face { get; set; }
//        public Reference FaceReference { get; set; }
//        public double Distance { get; set; }
//        public string RayId { get; set; }
//        public string ElementType { get; set; } // Add this line to store the element type
//        public string GeometryType { get; set; } // New property to store the type of geometry
//        public Reference IntersectingFaceRef { get; set; } // New property for intersecting face reference
//    }

//    public class SimulationPlaneInfo
//    {
//        public Face PlaneFace { get; set; }
//        public XYZ PlaneOrigin { get; set; }
//        public double PlaneLength { get; set; }
//        public double PlaneWidth { get; set; }
//        public ElementId SimulationShapeId { get; set; } // Add this line
//    }

//    public class IdentifiedRay
//    {
//        public Line Ray { get; set; }
//        public string RayId { get; set; }
//        public ElementId ElementId { get; set; } // Add this line if ElementId should be a part of IdentifiedRay
//    }

//    public class UVGridCell
//    {
//        public string Id { get; set; }
//        public UV MinUV { get; set; }
//        public UV MaxUV { get; set; }
//        public int Hits { get; set; }
//    }

//}

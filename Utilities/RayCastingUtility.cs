
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.DB.Analysis;
using System.Runtime.InteropServices;
using Autodesk.Revit.Exceptions;
using System.Windows.Controls;
using System.Security.Cryptography.X509Certificates;
using Newtonsoft.Json.Linq;

namespace RevitWoodLCC
{
    [Transaction(TransactionMode.Manual)]
    public class RayCastingFaceIntersection : IExternalCommand
    {
        private bool drawRaysAsModelLines = true; // Set to 'true' to enable drawing
        private double edgeExclusionDistanceMm = 1.0; // For example, edges of the face to exclude ray

        private const double tolerance = 0.001; // Replace 0.001 with whatever your tolerance should be



        // The Execute method where the main logic runs.
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiApp = commandData.Application;
            UIDocument uiDoc = uiApp.ActiveUIDocument;
            Document doc = uiDoc.Document;

            try
            {
                Reference pickedRef = uiDoc.Selection.PickObject(ObjectType.Element, "Select an element to cast rays from.");
                Element selectedElement = doc.GetElement(pickedRef);

                if (selectedElement == null)
                {
                    message = "No element selected.";
                    return Result.Failed;
                }

                using (Transaction trans = new Transaction(doc, "Create Visualization"))
                {
                    trans.Start();

                    HashSet<ElementId> intersectingElementIds = FindIntersectingElements(selectedElement, doc);
                    ShowIntersectingElementsInfo(doc, intersectingElementIds);

                    double raySpacingInMm = 50.0;
                    double rayLengthInMm = 50.0;

                    List<Line> rays = CreateRaysForElement(selectedElement, raySpacingInMm, rayLengthInMm, doc);

                    List<IntersectingFaceandPointInfo> intersectingFacesInfo = FindIntersectingFaces(rays, doc, selectedElement.Id);
                    ShowIntersectingFacesandPointsInfo(doc, intersectingFacesInfo, selectedElement.Id);

                    //FindIntersectionPoints(rays, doc, selectedElement.Id);


                    List<XYZ> intersectionPoints = FindIntersectionPoints(rays, doc, selectedElement.Id);
                    //ShowIntersectionPointsInfo(doc, intersectionPoints);  // This line is just to show the points. It can be removed if not needed.

                    foreach (var faceInfo in intersectingFacesInfo)
                    {
                        Face face = faceInfo.Face; // Get the Face object from faceInfo
                        bool intersectionsAreValid = VerifyIntersectionPoints(face, intersectionPoints);

                        if (!intersectionsAreValid)
                        {
                            message = "One or more intersection points are not valid for face with element ID: " + faceInfo.ElementId.IntegerValue;
                            // You can choose to fail the command or just log this issue
                            // return Result.Failed;
                        }
                    }

                    if (drawRaysAsModelLines)
                    {
                        DrawRaysAsModelLines(doc, rays);
                    }
                    
                    // Create the analysis display style using the utility class
                    string styleName = "NEWCustomAVFStyleWithoutLegend";
                    AnalysisDisplayStyle analysisDisplayStyle = VisualizationUtils.CreateDefaultAnalysisDisplayStyle(doc, styleName);

                    // Apply the newly created analysis display style to the active view
                    doc.ActiveView.AnalysisDisplayStyleId = analysisDisplayStyle.Id;


                    // AVF Visualization Code
                    SpatialFieldManager sfm = SpatialFieldManager.GetSpatialFieldManager(doc.ActiveView);
                    if (sfm == null)
                    {
                        sfm = SpatialFieldManager.CreateSpatialFieldManager(doc.ActiveView, 1);
                    }

                    // Register a new result schema with SpatialFieldManager
                    AnalysisResultSchema resultSchema = new AnalysisResultSchema("Ray Intersections", "A visualization of ray intersections");
                    int schemaIndex = sfm.RegisterResult(resultSchema);

                    // Iterate over intersecting faces and create a visual representation
                    foreach (IntersectingFaceandPointInfo faceInfo in intersectingFacesInfo)
                    {
                        // Convert spacing from millimeters to feet
                        double raySpacingInFeet = UnitUtils.ConvertToInternalUnits(raySpacingInMm, UnitTypeId.Millimeters);


                        // The grid spacing in millimeters, this value is up to you to define
                        double gridSpacingInMM = 5.0; // for example, in millimeters //This is for 

                        // The maximum distance for intersections in centimeters
                        double maxDistanceInMM = 30.0; // for example in millimeters


                        // Variables to hold the output from the GenerateUVGridAndValues method
                        List<UV> uvPoints;
                        List<ValueAtPoint> valueAtPoints;

                        // Call the GenerateUVGridAndValues method with the necessary arguments
                        GenerateUVGridAndValues(
                            faceInfo.Face, // The face you have already obtained
                            intersectionPoints,
                            out uvPoints,
                            out valueAtPoints,
                            gridSpacingInMM,
                            maxDistanceInMM
                        );

                        // Ensure that uvPoints and valueAtPoints have the same number of elements
                        if (uvPoints.Count != valueAtPoints.Count)
                        {
                            message = "The number of UV points must match the number of ValueAtPoints.";
                            return Result.Failed; // Early exit if the counts don't match
                        }

                        double moistureVisualizationDepthInMM = 1000.0;

                        // Call the method to visualize moisture diffusion
                        //VisualizeMoistureDiffusion(doc, intersectingFacesInfo, moistureVisualizationDepthInMM);

                        // Update the spatial field with the UV points and their corresponding values
                        if (uvPoints.Count > 0)
                        {
                            FieldDomainPointsByUV fdp = new FieldDomainPointsByUV(uvPoints);
                            FieldValues fvs = new FieldValues(valueAtPoints);

                            int primitiveId = sfm.AddSpatialFieldPrimitive(faceInfo.FaceReference);
                            sfm.UpdateSpatialFieldPrimitive(primitiveId, fdp, fvs, schemaIndex);
                        }
                    }

                    


                    trans.Commit();
                }
                return Result.Succeeded;
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                message = "Operation canceled by the user.";
                return Result.Cancelled;
            }
            catch (Exception ex)
            {
                message = $"Unexpected error occurred: {ex.Message}\nStack Trace: {ex.StackTrace}";
                return Result.Failed;
            }
        }


        private SpatialFieldManager GetOrCreateSpatialFieldManager(View view)
        {

            if (view == null)
            {
                throw new System.ArgumentNullException(nameof(view), "The provided view is null.");
            }

            SpatialFieldManager sfm = SpatialFieldManager.GetSpatialFieldManager(view);
            if (sfm == null)
            {
                    sfm = SpatialFieldManager.CreateSpatialFieldManager(view, 1);
            }
            return sfm;
        }


        private AnalysisDisplayStyle GetOrCreateAnalysisDisplayStyle(Document doc, string styleName)
        {
            AnalysisDisplayStyle analysisDisplayStyle = null;

            // Search for an existing AnalysisDisplayStyle with the given name
            FilteredElementCollector collector = new FilteredElementCollector(doc)
                .OfClass(typeof(AnalysisDisplayStyle));

            analysisDisplayStyle = collector
                .FirstOrDefault(e => e.Name.Equals(styleName)) as AnalysisDisplayStyle;

            // If an existing style is not found, create a new one
            if (analysisDisplayStyle == null)
            {
                //using (Transaction tx = new Transaction(doc, "Create Analysis Display Style"))
                //{
                //    tx.Start();

                    try
                    {
                        analysisDisplayStyle = VisualizationUtils.CreateDefaultAnalysisDisplayStyle(doc, styleName);
                    //    tx.Commit();
                    }
                    catch (Exception ex)
                    {
                       // tx.RollBack();
                        throw new System.InvalidOperationException("Unable to create a unique Analysis Display Style.", ex);
                    //}
                }
            }

            return analysisDisplayStyle;
        }




        public static AnalysisDisplayStyle CreateDefaultAnalysisDisplayStyle(Document doc, string styleName)
        {
            // Attempt to find an existing AnalysisDisplayStyle with the specified name
            var existingStyle = new FilteredElementCollector(doc)
                .OfClass(typeof(AnalysisDisplayStyle))
                .Cast<AnalysisDisplayStyle>()
                .FirstOrDefault(style => style.Name.Equals(styleName));

            // If a style with the name already exists, return the existing style
            if (existingStyle != null)
            {
                return existingStyle;
            }

            // If no existing style with the name is found, create a new one
            AnalysisDisplayColoredSurfaceSettings coloredSurfaceSettings = new AnalysisDisplayColoredSurfaceSettings
            {
                ShowGridLines = false
            };

            AnalysisDisplayColorSettings colorSettings = new AnalysisDisplayColorSettings
            {
                MaxColor = new Autodesk.Revit.DB.Color(0, 0, 255), // Blue
                MinColor = new Autodesk.Revit.DB.Color(255, 255, 255) // White
            };

            AnalysisDisplayLegendSettings legendSettings = new AnalysisDisplayLegendSettings
            {
                NumberOfSteps = 10,
                Rounding = 0.05,
                ShowDataDescription = false,
                ShowLegend = false // Set to true to show the legend if needed
            };

            // Create a new AnalysisDisplayStyle
            AnalysisDisplayStyle newStyle = null;

            using (Transaction tx = new Transaction(doc, "Create Analysis Display Style"))
            {
                tx.Start();

                try
                {
                    newStyle = AnalysisDisplayStyle.CreateAnalysisDisplayStyle(doc, styleName, coloredSurfaceSettings, colorSettings, legendSettings);
                    tx.Commit();
                }
                catch (Exception ex)
                {
                    // If an exception occurs, roll back the transaction
                    tx.RollBack();
                    throw new System.InvalidOperationException("Unable to create a unique Analysis Display Style.", ex);
                }
            }

            return newStyle;
        }





        private int GetOrCreateAnalysisResultSchemaIndex(SpatialFieldManager sfm, Document doc, string schemaName)
        {
            // Check if a schema with the same name is already registered
            foreach (int index in sfm.GetRegisteredResults())
            {
                AnalysisResultSchema existingSchema = sfm.GetResultSchema(index);
                if (existingSchema.Name.Equals(schemaName, StringComparison.OrdinalIgnoreCase))
                {
                    // If a schema with the same name exists, use the existing index
                    return index;
                }
            }

            // If no schema with the name exists, create a new one and register it
            using (Transaction tx = new Transaction(doc, "Register Analysis Schema"))
            {
                tx.Start();
                AnalysisResultSchema newSchema = new AnalysisResultSchema(schemaName, "Description");
                int newIndex = sfm.RegisterResult(newSchema);
                tx.Commit();
                return newIndex;
            }
        }





        private void GenerateUVGridAndValues(
            Face face,
            List<XYZ> intersectionPoints,
            out List<UV> uvPoints,
            out List<ValueAtPoint> valueAtPoints,
            double gridSpacingInMM, // Parameter for grid spacing in millimeters
            double maxDistanceInMM // Parameter for max distance in centimeters
        )
        {
            // Convert the max distance from centimeters to Revit internal units (feet)
            double maxDistance = UnitUtils.ConvertToInternalUnits(maxDistanceInMM, UnitTypeId.Millimeters);

            // Convert the grid spacing from millimeters to Revit internal units (feet)
            double gridSpacingInFeet = UnitUtils.ConvertToInternalUnits(gridSpacingInMM, UnitTypeId.Millimeters);

            // Initialize the output lists
            uvPoints = new List<UV>();
            valueAtPoints = new List<ValueAtPoint>();

            // Get the bounding box of the face in UV space
            BoundingBoxUV bbox = face.GetBoundingBox();
            UV bboxMin = bbox.Min;
            UV bboxMax = bbox.Max;

            // Calculate the grid points on the face based on the converted grid spacing
            for (double u = bboxMin.U; u <= bboxMax.U; u += gridSpacingInFeet)
            {
                for (double v = bboxMin.V; v <= bboxMax.V; v += gridSpacingInFeet)
                {
                    UV currentUV = new UV(u, v);
                    if (face.IsInside(currentUV))
                    {
                        XYZ pointOnFace = face.Evaluate(currentUV);
                        double closestDistance = intersectionPoints
                            .Select(intersection => pointOnFace.DistanceTo(intersection))
                            .DefaultIfEmpty(maxDistance) // In case there are no intersection points
                            .Min();

                        // Normalize the closest distance based on the maxDistance
                        double normalizedValue = Math.Min(closestDistance / maxDistance, 1.0);

                        // Determine the value to use for the AVF (Analysis Visual Framework)
                        double avfValue = (1.0 - normalizedValue); // Inverted so closer distances have higher values

                        // Add the UV point and the corresponding value to the output lists
                        uvPoints.Add(currentUV);
                        valueAtPoints.Add(new ValueAtPoint(new List<double> { avfValue }));
                    }
                }
            }
        }


        private bool VerifyIntersectionPoints(Face face, List<XYZ> intersectionPoints)
        {
            foreach (XYZ point in intersectionPoints)
            {
                IntersectionResult intersectionResult = face.Project(point);
                if (intersectionResult == null)
                {
                    // The point does not project onto the face, which means it's not an intersection
                    return false;
                }

                double distanceToFace = intersectionResult.XYZPoint.DistanceTo(point);
                if (distanceToFace > tolerance)
                {
                    // The point is too far from the face, which means it's not a valid intersection
                    return false;
                }
            }
            // All intersection points are valid
            return true;
        }



        private void VisualizeMoistureDiffusion(Document doc, List<IntersectingFaceandPointInfo> intersectingFacesInfo, double moistureVisualizationDepthInMM)
        {
            // Initialize the Spatial Field Manager for the active view or create if it doesn't exist
            SpatialFieldManager sfm = GetOrCreateSpatialFieldManager(doc.ActiveView);

            // Find or create an analysis display style
            string styleName = "Moisture Visualization Style";
            AnalysisDisplayStyle analysisDisplayStyle = GetOrCreateAnalysisDisplayStyle(doc, styleName);

            // Get or create a new schema index for moisture visualization
            string schemaName = "Moisture Diffusion";
            int schemaIndex = GetOrCreateAnalysisResultSchemaIndex(sfm, doc, schemaName);

            // Iterate through each intersecting face info to process moisture levels
            foreach (var faceInfo in intersectingFacesInfo)
            {
                // Convert distance from millimeters to internal units (feet)
                double maxDistanceInFeet = UnitUtils.ConvertToInternalUnits(moistureVisualizationDepthInMM, UnitTypeId.Millimeters);

                // Generate UV grid and value points for AVF based on moisture proximity
                GenerateMoistureVisualizationPoints(faceInfo, maxDistanceInFeet, out List<UV> uvPoints, out List<ValueAtPoint> valueAtPoints);

                // Register the face with the Spatial Field Manager and update the AVF
                if (uvPoints.Count > 0)
                {
                    FieldDomainPointsByUV fdp = new FieldDomainPointsByUV(uvPoints);
                    FieldValues fvs = new FieldValues(valueAtPoints);

                    int primitiveId = sfm.AddSpatialFieldPrimitive(faceInfo.FaceReference);
                    sfm.UpdateSpatialFieldPrimitive(primitiveId, fdp, fvs, schemaIndex);
                }
            }

            // Assign the display style to the view
            doc.ActiveView.AnalysisDisplayStyleId = analysisDisplayStyle.Id;
        }

        private void GenerateMoistureVisualizationPoints(IntersectingFaceandPointInfo faceInfo, double maxDistanceInFeet, out List<UV> uvPoints, out List<ValueAtPoint> valueAtPoints)
        {
            uvPoints = new List<UV>();
            valueAtPoints = new List<ValueAtPoint>();

            Face face = faceInfo.Face;
            BoundingBoxUV bbox = face.GetBoundingBox();

            // Use a nested loop to iterate over a grid of points on the face
            for (double u = bbox.Min.U; u <= bbox.Max.U; u += maxDistanceInFeet)
            {
                for (double v = bbox.Min.V; v <= bbox.Max.V; v += maxDistanceInFeet)
                {
                    UV currentUV = new UV(u, v);
                    if (face.IsInside(currentUV))
                    {
                        XYZ pointOnFace = face.Evaluate(currentUV);
                        double closestDistance = faceInfo.IntersectionPoints
                            .Select(intersection => intersection.DistanceTo(pointOnFace))
                            .Min();

                        // Scale the moisture level based on distance to the closest intersection point
                        double moistureLevel = CalculateMoistureLevel(closestDistance, maxDistanceInFeet);

                        uvPoints.Add(currentUV);
                        valueAtPoints.Add(new ValueAtPoint(new List<double> { moistureLevel }));
                    }
                }
            }
        }

        private double CalculateMoistureLevel(double distance, double maxDistance)
        {
            // Assuming linear diffusion, the moisture level decreases with distance
            // Moisture level is high (1.0) at the face and decreases to 0.0 at the maxDistance
            return (maxDistance - distance) / maxDistance;
        }



        private HashSet<ElementId> FindIntersectingElements(Element element, Document doc)
        {
            HashSet<ElementId> intersectingElementIds = new HashSet<ElementId>();

            // Get the element's bounding box
            BoundingBoxXYZ elementBoundingBox = element.get_BoundingBox(null);
            Outline elementOutline = new Outline(elementBoundingBox.Min, elementBoundingBox.Max);

            // Create a filter based on the bounding box
            BoundingBoxIntersectsFilter intersectsFilter = new BoundingBoxIntersectsFilter(elementOutline);

            // Use the filter to find intersecting elements that have solids or instances
            var collector = new FilteredElementCollector(doc)
                .WherePasses(intersectsFilter)
                .WhereElementIsNotElementType()
                .Excluding(new ElementId[] { element.Id }); // Exclude the element itself

            // Filter to include only elements with Solids or GeometryInstances
            //var elementsWithGeometry = collector
            //    .Where(e => e.get_Geometry(new Options()).Any(g => g is Solid || g is GeometryInstance));

            var elementsWithGeometry = collector
            .Where(e =>
            {
                var geometry = e.get_Geometry(new Options());
                return geometry != null && geometry.Any(g => g is Solid || g is GeometryInstance);
            });

            // Iterate through the collected elements and add their ElementIds to the HashSet
            foreach (Element elem in elementsWithGeometry)
            {
                intersectingElementIds.Add(elem.Id);
            }

            return intersectingElementIds;
        }



        private void ShowIntersectingElementsInfo(Document doc, HashSet<ElementId> intersectingElementIds)
        {
            // Start building the message string to display
            string message = "Intersecting Elements:\n";

            // Check if there are any intersecting elements
            if (intersectingElementIds.Count > 0)
            {
                foreach (ElementId id in intersectingElementIds)
                {
                    // Retrieve the element from the document using the element id
                    Element intersectingElement = doc.GetElement(id);
                    if (intersectingElement == null)
                    {
                        // If the element is null, skip this iteration
                        continue;
                    }

                    int faceCount = 0;
                    string faceRefs = "";

                    // Get geometry of the element
                    Options geomOptions = new Options();
                    GeometryElement geomElement = intersectingElement.get_Geometry(geomOptions);

                    // Iterate through the geometry objects to count faces and get their references
                    if (geomElement != null)
                    {
                        foreach (GeometryObject geomObj in geomElement)
                        {
                            if (geomObj is Solid solid && solid.Faces != null)
                            {
                                faceCount += solid.Faces.Size;
                                foreach (Face face in solid.Faces)
                                {
                                    Reference faceRef = face.Reference;
                                    if (faceRef != null) // Check if the face reference is null
                                    {
                                        faceRefs += faceRef.ConvertToStableRepresentation(doc) + "; ";
                                    }
                                }
                            }
                            else if (geomObj is GeometryInstance instance)
                            {
                                GeometryElement instanceGeom = instance.GetInstanceGeometry();
                                if (instanceGeom != null) // Check if the instance geometry is null
                                {
                                    foreach (GeometryObject instGeomObj in instanceGeom)
                                    {
                                        if (instGeomObj is Solid instSolid && instSolid.Faces != null)
                                        {
                                            faceCount += instSolid.Faces.Size;
                                            foreach (Face face in instSolid.Faces)
                                            {
                                                Reference faceRef = face.Reference;
                                                if (faceRef != null) // Check if the face reference is null
                                                {
                                                    faceRefs += faceRef.ConvertToStableRepresentation(doc) + "; ";
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                    // Append the element's information to the message string
                    message += $"ID: {intersectingElement.Id.IntegerValue}, " +
                                $"Name: {intersectingElement.Name ?? "Unnamed"}, " + // Check if Name is null
                                $"Category: {intersectingElement.Category?.Name ?? "Unknown"}, " + // Category null check
                                $"Number of Faces: {faceCount}, " +
                                $"Face References: {faceRefs.TrimEnd(' ', ';')}\n";
                }
            }
            else
            {
                message += "No intersecting elements were found.";
            }

            // Display the message to the user
            TaskDialog.Show("Intersecting Elements", message);
        }

        private List<Line> CreateRaysForElement(Element element, double raySpacingInMm, double rayLengthInMm, Document doc)
        {
            List<Line> rays = new List<Line>();
            GeometryElement geometryElement = element.get_Geometry(new Options { ComputeReferences = true, IncludeNonVisibleObjects = true });

            if (geometryElement != null)
            {
                foreach (GeometryObject geomObj in geometryElement)
                {
                    if (geomObj is Solid solid)
                    {
                        foreach (Face face in solid.Faces)
                        {
                            PlanarFace planarFace = face as PlanarFace;
                            if (planarFace != null)
                            {
                                rays.AddRange(CreateRaysFromFace(planarFace, raySpacingInMm, rayLengthInMm, edgeExclusionDistanceMm));
                            }
                        }
                    }
                    else if (geomObj is GeometryInstance instance)
                    {
                        GeometryElement instanceGeometry = instance.GetInstanceGeometry();
                        foreach (GeometryObject instGeomObj in instanceGeometry)
                        {
                            if (instGeomObj is Solid instSolid)
                            {
                                foreach (Face face in instSolid.Faces)
                                {
                                    PlanarFace planarFace = face as PlanarFace;
                                    if (planarFace != null)
                                    {
                                        rays.AddRange(CreateRaysFromFace(planarFace, raySpacingInMm, rayLengthInMm, edgeExclusionDistanceMm));
                                    }
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                TaskDialog.Show("Error", $"No geometry found for element with ID {element.Id.IntegerValue}.");
            }

            return rays;
        }

        private IEnumerable<Line> CreateRaysFromFace(PlanarFace face, double raySpacingInMm, double rayLengthInMm, double edgeExclusionDistanceMm)
        {
            List<Line> rays = new List<Line>();
            BoundingBoxUV bbox = face.GetBoundingBox();
            UV min = bbox.Min;
            UV max = bbox.Max;

            // Convert the custom ray spacing and edge exclusion distance from millimeters to internal units (feet)
            double raySpacingInFeet = UnitUtils.ConvertToInternalUnits(raySpacingInMm, UnitTypeId.Millimeters);
            double rayLengthInFeet = UnitUtils.ConvertToInternalUnits(rayLengthInMm, UnitTypeId.Millimeters);
            double edgeExclusionDistanceFeet = UnitUtils.ConvertToInternalUnits(edgeExclusionDistanceMm, UnitTypeId.Millimeters);

            // Adjust the bounding box to account for the edge exclusion
            UV minWithExclusion = new UV(
                Math.Max(min.U + edgeExclusionDistanceFeet, 0),
                Math.Max(min.V + edgeExclusionDistanceFeet, 0)
            );
            UV maxWithExclusion = new UV(
                Math.Min(max.U - edgeExclusionDistanceFeet, bbox.Max.U),
                Math.Min(max.V - edgeExclusionDistanceFeet, bbox.Max.V)
            );

            // Ensure that the adjusted min is less than the adjusted max
            if (minWithExclusion.U >= maxWithExclusion.U || minWithExclusion.V >= maxWithExclusion.V)
            {
                // If the exclusion zone is too large for the face, return an empty list of rays
                return rays;
            }

            // Determine the number of rays in the U and V directions based on the spacing
            int numURays = (int)Math.Ceiling((maxWithExclusion.U - minWithExclusion.U) / raySpacingInFeet);
            int numVRays = (int)Math.Ceiling((maxWithExclusion.V - minWithExclusion.V) / raySpacingInFeet);

            // Calculate the actual spacing in U and V to distribute rays evenly
            double actualUSpacing = (maxWithExclusion.U - minWithExclusion.U) / numURays;
            double actualVSpacing = (maxWithExclusion.V - minWithExclusion.V) / numVRays;

            for (int i = 0; i <= numURays; i++)
            {
                for (int j = 0; j <= numVRays; j++)
                {
                    UV currentUV = new UV(minWithExclusion.U + i * actualUSpacing, minWithExclusion.V + j * actualVSpacing);
                    if (face.IsInside(currentUV))
                    {
                        XYZ pointOnFace = face.Evaluate(currentUV);
                        XYZ normal = face.ComputeNormal(currentUV);

                        // Create a ray starting at the evaluated point and extending in the direction of the face normal
                        XYZ rayStart = pointOnFace;
                        XYZ rayEnd = pointOnFace + normal * rayLengthInFeet;
                        Line ray = Line.CreateBound(rayStart, rayEnd);
                        rays.Add(ray);
                    }
                }
            }

            return rays;
        }


        private void DrawRaysAsModelLines(Document doc, List<Line> rays)
        {
            foreach (Line ray in rays)
            {
                // Use the start point and direction of the ray to define a plane.
                XYZ startPoint = ray.GetEndPoint(0);
                XYZ endPoint = ray.GetEndPoint(1);
                XYZ rayDirection = endPoint.Subtract(startPoint).Normalize();

                // Use a helper method to get a third point that is not collinear to start and end points.
                XYZ thirdPoint = GetThirdPointForPlane(startPoint, rayDirection);

                // Create a plane using the start point, end point, and the third point.
                Plane plane = Plane.CreateByThreePoints(startPoint, endPoint, thirdPoint);

                // Now create a SketchPlane from the plane.
                SketchPlane sketchPlane = SketchPlane.Create(doc, plane);

                // Draw the ray as a model line.
                doc.Create.NewModelCurve(ray, sketchPlane);
            }
        }

        private XYZ GetThirdPointForPlane(XYZ startPoint, XYZ direction)
        {
            // Generate a third point that is not collinear to the start point and direction vector.
            // This is done by taking a cross product of the direction with an arbitrary vector.
            // The arbitrary vector must not be parallel to the direction.
            XYZ arbitraryVector = XYZ.BasisZ;
            if (direction.IsAlmostEqualTo(XYZ.BasisZ) || direction.IsAlmostEqualTo(-XYZ.BasisZ))
            {
                arbitraryVector = XYZ.BasisY;
            }

            XYZ normal = direction.CrossProduct(arbitraryVector).Normalize();
            return startPoint + normal;
        }





        private List<IntersectingFaceandPointInfo> FindIntersectingFaces(IEnumerable<Line> rays, Document doc, ElementId selectedElementId)
        {
            List<IntersectingFaceandPointInfo> intersectingFacesInfo = new List<IntersectingFaceandPointInfo>();

            // Create a collector that will gather all the elements in the document
            FilteredElementCollector collector = new FilteredElementCollector(doc)
                .WhereElementIsNotElementType();

            foreach (Element elem in collector)
            {
                // Skip the selected element to avoid adding its faces
                if (elem.Id == selectedElementId)
                    continue;

                GeometryElement geomElement = elem.get_Geometry(new Options() { ComputeReferences = true });
                if (geomElement == null) continue; // Skip if no geometry is found
                ProcessGeometry(geomElement, rays, elem.Id, intersectingFacesInfo);
            }

            return intersectingFacesInfo;



        }


        private void ProcessGeometry(GeometryElement geomElement, IEnumerable<Line> rays, ElementId elemId, List<IntersectingFaceandPointInfo> intersectingFacesInfo)
        {
            foreach (GeometryObject geomObj in geomElement)
            {
                if (geomObj is Solid solid && solid.Faces != null)
                {
                    ProcessSolid(solid, rays, elemId, intersectingFacesInfo);
                }
                else if (geomObj is GeometryInstance instance)
                {
                    Transform instanceTransform = instance.Transform;
                    GeometryElement instanceGeometry = instance.GetInstanceGeometry(instanceTransform);
                    ProcessGeometry(instanceGeometry, rays, elemId, intersectingFacesInfo);
                }
                // Handle other types of geometry if necessary
            }
        }

        private void ProcessSolid(Solid solid, IEnumerable<Line> rays, ElementId elemId, List<IntersectingFaceandPointInfo> intersectingFacesInfo)
        {
            foreach (Face face in solid.Faces)
            {
                foreach (Line ray in rays)
                {
                    IntersectionResultArray intersectionResultArray;
                    if (face.Intersect(ray, out intersectionResultArray) == SetComparisonResult.Overlap)
                    {
                        // Check if we already have an IntersectingFaceInfo for this face
                        var faceInfo = intersectingFacesInfo.FirstOrDefault(f => f.Face == face && f.ElementId == elemId);

                        // If we don't have one, create it
                        if (faceInfo == null)
                        {
                            faceInfo = new IntersectingFaceandPointInfo
                            {
                                Face = face,
                                ElementId = elemId,
                                FaceReference = face.Reference
                            };
                            intersectingFacesInfo.Add(faceInfo);
                        }

                        // Add the intersection point to the face's list of points
                        faceInfo.IntersectionPoints.Add(intersectionResultArray.get_Item(0).XYZPoint);
                    }
                }
            }
        }



        private List<XYZ> FindIntersectionPoints(IEnumerable<Line> rays, Document doc, ElementId selectedElementId)
        {
            List<XYZ> intersectionPoints = new List<XYZ>();

            // Create a collector to gather all elements that could intersect with the rays
            FilteredElementCollector collector = new FilteredElementCollector(doc)
                .WhereElementIsNotElementType();

            foreach (Element elem in collector)
            {
                // Skip the selected element since we don't want intersections with itself
                if (elem.Id == selectedElementId)
                    continue;

                GeometryElement geomElement = elem.get_Geometry(new Options());
                if (geomElement == null)
                    continue;

                // Modify this call to pass intersectingFacesInfo as a reference
                ProcessGeometryElement(geomElement, rays, intersectionPoints);
            }

            return intersectionPoints;
        }

        private void ProcessGeometryElement(GeometryElement geomElement, IEnumerable<Line> rays, List<XYZ> intersectionPoints)
        {
            foreach (GeometryObject geomObj in geomElement)
            {
                if (geomObj is Solid solid)
                {
                    foreach (Face face in solid.Faces)
                    {
                        foreach (Line ray in rays)
                        {
                            IntersectionResultArray results;
                            if (face.Intersect(ray, out results) == SetComparisonResult.Overlap)
                            {
                                foreach (IntersectionResult ir in results)
                                {
                                    intersectionPoints.Add(ir.XYZPoint);
                                }
                            }
                        }
                    }
                }
                else if (geomObj is GeometryInstance instance)
                {
                    GeometryElement instanceGeom = instance.GetInstanceGeometry();
                    ProcessGeometryElement(instanceGeom, rays, intersectionPoints);
                }
            }
        }

        private void ShowIntersectingFacesandPointsInfo(Document doc, List<IntersectingFaceandPointInfo> intersectingFacesInfo, ElementId selectedElementId)
        {
            // Start building the message string to display
            string message = "Intersecting Faces and Points:\n";

            // Check if there are any intersecting faces
            if (intersectingFacesInfo.Count > 0)
            {
                foreach (IntersectingFaceandPointInfo info in intersectingFacesInfo)
                {
                    // Skip faces that belong to the selected element
                    if (info.ElementId == selectedElementId)
                        continue;

                    // Get the stable representation of the face reference
                    string faceRefString = info.FaceReference != null ? info.FaceReference.ConvertToStableRepresentation(doc) : null;

                    // Retrieve the element to which the face belongs
                    Element owner = doc.GetElement(info.ElementId);

                    // Append the face's and owner element's information to the message string
                    message += $"Face on Element ID: {info.ElementId.IntegerValue}, " +
                               $"Element Name: {owner?.Name ?? "Unknown"}, " +
                               $"Category: {owner?.Category?.Name ?? "Unknown"}, " +
                               $"Face Area: {info.Face.Area}, " +
                               $"Face Reference: {faceRefString}\n";

                    // Append all intersection points for this face
                    foreach (XYZ point in info.IntersectionPoints)
                    {
                        message += $"    Intersection Point: X: {point.X:F2}, Y: {point.Y:F2}, Z: {point.Z:F2}\n";
                    }
                    message += "\n"; // Add a newline for spacing between faces
                }
            }
            else
            {
                message += "No intersecting faces were found.";
            }

            // Display the message to the user
            TaskDialog.Show("Intersecting Faces", message);
        }


    }

    public class IntersectingFaceandPointInfo
    {
        public Face Face { get; set; }
        public ElementId ElementId { get; set; }
        public List<XYZ> IntersectionPoints { get; set; } = new List<XYZ>(); // List to hold points
        public Reference FaceReference { get; set; }
        // Additional properties can be added as needed
    }

}




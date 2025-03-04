/*
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Analysis;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;



namespace RevitWoodLCC
{
    [Transaction(TransactionMode.Manual)]
    public class PickedFaceRayProjectionCode_new : IExternalCommand
    {
        double rayLength = 100.0; // initial value, can be adjusted later
        private bool visualizeRays = true; // Set to 'true' to enable visualization by default
        private bool HighlightClosestIntersection = true;
        private bool bool_CreateAndVisualizeSimulationPlane = true;
        private bool bool_DisplayClosestIntersections = true;
        private bool bool_CalculateAndVisualizeIntersections = true;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiApp = commandData.Application;
            UIDocument uiDoc = uiApp.ActiveUIDocument;
            Document doc = uiDoc.Document;

            try
            {
                using (Transaction tx = new Transaction(doc, "Ray Projection and AVF Application"))
                {
                    tx.Start();


                    // Prompt user to select faces
                    IList<Reference> selectedFaceRefs = uiDoc.Selection.PickObjects(ObjectType.Face, "Select faces for AVF application.");

                    if (selectedFaceRefs.Count == 0)
                    {
                        message = "No faces selected.";
                        return Result.Cancelled;
                    }


                    View3D view3D = doc.ActiveView as View3D;
                    if (view3D == null || !view3D.IsSectionBoxActive)
                    {
                        TaskDialog.Show("Error", "Please make sure you are in a 3D view with an active section box.");
                        return Result.Failed;
                    }

                    BoundingBoxXYZ sectionBox = view3D.GetSectionBox();
                    SimulationPlaneInfo simulationInfo = CreateAndVisualizeSimulationPlane(doc, sectionBox); // Assumes no internal transaction
                    double factor = 2;
                    int rayDensity = CalculateRayDensity(view3D, factor);
                    List<IdentifiedRay> rays = GenerateAndVisualizeRaysFromFace(simulationInfo.PlaneFace, doc, simulationInfo.SimulationShapeId, rayDensity); // Assumes no internal transaction

                    var rayIntersections = CalculateAndSortIntersections(doc, rays, simulationInfo.PlaneFace, simulationInfo.SimulationShapeId);

                    if (bool_CalculateAndVisualizeIntersections)
                    {
                        CalculateAndVisualizeIntersections(rayIntersections, doc); // Assumes no internal transaction
                    }

                    var closestIntersections = GetClosestIntersections(rayIntersections);
                    if (bool_DisplayClosestIntersections)
                    {
                        DisplayClosestIntersections(closestIntersections, doc); // Assumes no internal transaction
                    }

                    if (HighlightClosestIntersection)
                    {
                        HighlightClosestIntersectionElements(doc, closestIntersections); // Assumes no internal transaction
                    }

                    //foreach (Reference faceRef in selectedFaceRefs)
                    //{
                    //    Element element = doc.GetElement(faceRef.SelectedElementId);
                    //    if (element == null) continue;

                    //    GeometryObject geomObj = element.GetGeometryObjectFromReference(faceRef);
                    //    if (geomObj is Face face)
                    //    {
                    //        SetUpAndApplyAVF(doc, face, faceRef);
                    //    }
                    //}

                    List<Reference> closestIntersectionFaceRefs = GetClosestIntersectionFaceReferences(doc, rayIntersections); // Assuming this method is already defined and rayIntersections is available

                    foreach (Reference faceRef in closestIntersectionFaceRefs)
                    {
                        Element element = doc.GetElement(faceRef.SelectedElementId);
                        if (element == null) continue;

                        GeometryObject geomObj = element.GetGeometryObjectFromReference(faceRef);
                        if (geomObj is Face face)
                        {
                            SetUpAndApplyAVF(doc, face, faceRef);
                        }
                    }


                    tx.Commit();
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
                message = $"Unexpected error occurred: {ex.Message}";
                return Result.Failed;
            }
        }



        private SimulationPlaneInfo CreateAndVisualizeSimulationPlane(Document doc, BoundingBoxXYZ sectionBox)
        {
            SimulationPlaneInfo simulationInfo = new SimulationPlaneInfo();
            StringBuilder faceInfoBuilder = new StringBuilder(); // Declare outside the using block

            XYZ min = sectionBox.Min;
            XYZ max = sectionBox.Max;
            XYZ planeOrigin = new XYZ((min.X + max.X) / 2, (min.Y + max.Y) / 2, max.Z + 10);
            double planeLength = max.X - min.X;
            double planeWidth = max.Y - min.Y;

            CurveLoop profile = new CurveLoop();
            profile.Append(Line.CreateBound(planeOrigin, planeOrigin + new XYZ(planeLength, 0, 0)));
            profile.Append(Line.CreateBound(planeOrigin + new XYZ(planeLength, 0, 0), planeOrigin + new XYZ(planeLength, planeWidth, 0)));
            profile.Append(Line.CreateBound(planeOrigin + new XYZ(planeLength, planeWidth, 0), planeOrigin + new XYZ(0, planeWidth, 0)));
            profile.Append(Line.CreateBound(planeOrigin + new XYZ(0, planeWidth, 0), planeOrigin));

            Solid planeSolid = GeometryCreationUtilities.CreateExtrusionGeometry(new List<CurveLoop> { profile }, XYZ.BasisZ, 0.01);

            PlanarFace simulationFace = null;
            foreach (Face face in planeSolid.Faces)
            {
                if (face is PlanarFace planarFace && planarFace.FaceNormal.IsAlmostEqualTo(XYZ.BasisZ))
                {
                    simulationFace = planarFace as PlanarFace;
                    break;
                }
            }
            DirectShape ds = DirectShape.CreateElement(doc, new SelectedElementId(BuiltInCategory.OST_GenericModel));
            if (simulationFace != null)
            {
                XYZ extrusionDirection = new XYZ(0, 0, 0.01); // Small extrusion in the Z direction
                Solid extrudedSolid = GeometryCreationUtilities.CreateExtrusionGeometry(new List<CurveLoop> { profile }, extrusionDirection, 0.1);


                ds.ApplicationId = "SimulationPlane";
                ds.ApplicationDataId = Guid.NewGuid().ToString();

                IList<GeometryObject> geometry = new List<GeometryObject> { extrudedSolid };
                ds.SetShape(geometry);

                SelectedElementId SimulationShapeId = ds.Id;
                string SimulationShapeUniqueFaceIds = ds.UniqueId;

                // Accumulate face info
                foreach (Face face in extrudedSolid.Faces)
                {
                    faceInfoBuilder.AppendLine($"DirectShape Element ID: {SimulationShapeId.IntegerValue}, " +
                                               $"DirectShape Unique ID: {SimulationShapeUniqueFaceIds}, " +
                                               $"Face: {face}");
                }
            }



            simulationInfo.PlaneFace = simulationFace;
            simulationInfo.SimulationShapeId = ds.Id;
            simulationInfo.PlaneOrigin = planeOrigin;
            simulationInfo.PlaneLength = planeLength;
            simulationInfo.PlaneWidth = planeWidth;

            // Show all face info in one TaskDialog
            if (faceInfoBuilder.Length > 0)
            {
                TaskDialog.Show("Plain Face Info", faceInfoBuilder.ToString());
            }

            return simulationInfo;
        }

        private List<IdentifiedRay> GenerateAndVisualizeRaysFromFace(Face PlaneFace, Document doc, SelectedElementId simulationShapeId, int rayDensity)
        {
            BoundingBoxUV bbox = PlaneFace.GetBoundingBox();
            double uStep = (bbox.Max.U - bbox.Min.U) / rayDensity;
            double vStep = (bbox.Max.V - bbox.Min.V) / rayDensity;

            List<IdentifiedRay> identifiedRays = new List<IdentifiedRay>();
            int rayCounter = 0;

            // Iterate over a grid on the plane face
            for (int i = 0; i <= rayDensity; i++)
            {
                for (int j = 0; j <= rayDensity; j++)
                {
                    UV pointUV = new UV(bbox.Min.U + i * uStep, bbox.Min.V + j * vStep);
                    XYZ point = PlaneFace.Evaluate(pointUV);
                    XYZ normal = PlaneFace.ComputeNormal(pointUV);

                    // Create a ray with the initial length
                    Line rayLine = Line.CreateBound(point, point - normal * rayLength);
                    string rayId = $"Ray_{rayCounter++}"; // Assign a unique ID to each ray

                    IdentifiedRay identifiedRay = new IdentifiedRay
                    {
                        Ray = rayLine,
                        RayId = rayId
                    };

                    // Check if the ray is valid
                    if (identifiedRay.Ray != null && identifiedRay.Ray.Length > doc.Application.ShortCurveTolerance)
                    {
                        identifiedRays.Add(identifiedRay);
                    }
                }
            }

            // Visualize the rays if the flag is set to true
            if (visualizeRays)
            {
                VisualizeRays(identifiedRays, doc);
            }

            // Calculate and sort intersections
            var rayIntersections = CalculateAndSortIntersections(doc, identifiedRays, PlaneFace, simulationShapeId);

            // Additional processing or visualization can go here based on sorted intersections

            return identifiedRays;
        }

        private void VisualizeRays(List<IdentifiedRay> rays, Document doc)
        {

            foreach (IdentifiedRay identifiedRay in rays)
            {
                Line rayLine = identifiedRay.Ray;
                if (rayLine != null && rayLine.Length > doc.Application.ShortCurveTolerance)
                {
                    DirectShape ds = DirectShape.CreateElement(doc, new SelectedElementId(BuiltInCategory.OST_Lines));
                    ds.ApplicationId = "Rays Visualization";
                    ds.ApplicationDataId = identifiedRay.RayId; // Use RayId for ApplicationDataId
                    ds.SetShape(new List<GeometryObject> { rayLine });
                }
            }

        }

        private void CalculateAndVisualizeIntersections(Dictionary<string, List<IntersectionInfo>> rayIntersections, Document doc)
        {
            StringBuilder allIntersectionInfo = new StringBuilder();
            bool anyIntersectionsFound = false;

            foreach (var rayPair in rayIntersections)
            {
                string rayId = rayPair.Key;
                var intersections = rayPair.Value;

                if (intersections.Any())
                {
                    anyIntersectionsFound = true;

                    foreach (var intersection in intersections) // Loop through all intersections
                    {
                        // Determine the output based on the geometry type
                        string output;
                        if (intersection.GeometryType == "GeometryInstance")
                        {
                            output = $"Ray ID: {rayId} intersects at Element ID: {intersection.SelectedElementId.IntegerValue}, " +
                                     $"Element Type: {intersection.GeometryType}, " +
                                     $"Point: {intersection.FirstRayFaceIntersectionPoint}, " +
                                     $"Distance: {intersection.Distance:F2} meters, " +
                                     $"Face Reference: {GetFaceReferenceString(intersection, doc)}";
                        }
                        else if (intersection.ElementType == "Solid")
                        {
                            output = $"Ray ID: {rayId} intersects at Element ID: {intersection.SelectedElementId.IntegerValue}, " +
                                     $"Element Type: {intersection.ElementType}, " +
                                     $"Point: {intersection.FirstRayFaceIntersectionPoint}, " +
                                     $"Distance: {intersection.Distance:F2} meters, " +
                                     $"Face Reference: {GetFaceReferenceString(intersection, doc)}";
                        }
                        else
                        {
                            // Fallback for other types, or you could continue the loop with `continue;`
                            output = $"Ray ID: {rayId} intersects at Element ID: {intersection.SelectedElementId.IntegerValue}, " +
                                     $"Geometry Type: {intersection.GeometryType}, " +
                                     $"Element Type: {intersection.ElementType}, " +
                                     $"Point: {intersection.FirstRayFaceIntersectionPoint}, " +
                                     $"Distance: {intersection.Distance:F2} meters, " +
                                     $"Face Reference: {GetFaceReferenceString(intersection, doc)}";
                        }

                        allIntersectionInfo.AppendLine(output);
                    }
                }
            }

            if (anyIntersectionsFound)
            {
                TaskDialog.Show("Intersections Summary", allIntersectionInfo.ToString());
            }
            else
            {
                TaskDialog.Show("Info", "No intersections found for any of the rays.");
            }
        }

        private string GetFaceReferenceString(IntersectionInfo intersection, Document doc)
        {
            try
            {
                return intersection.FaceReference != null
                       ? doc.GetElement(intersection.FaceReference).UniqueId
                       : "Unknown"; // Fallback if FaceReference is null
            }
            catch
            {
                return "Error retrieving face unique ID"; // Error handling
            }
        }



        private List<IntersectionInfo> FindIntersections(Document doc, IdentifiedRay identifiedRay, Face PlaneFace, SelectedElementId simulationShapeId)
        {
            List<IntersectionInfo> intersections = new List<IntersectionInfo>();
            FilteredElementCollector collector = new FilteredElementCollector(doc)
                .WhereElementIsNotElementType();

            foreach (Element elem in collector)
            {
                // Skip if the element is the simulation shape
                if (elem.Id == simulationShapeId)
                    continue;

                ProcessElementGeometry(elem, doc, identifiedRay, PlaneFace, intersections);
            }

            return intersections;
        }

        private void ProcessElementGeometry(Element elem, Document doc, IdentifiedRay identifiedRay, Face PlaneFace, List<IntersectionInfo> intersections)
        {
            Options geomOptions = new Options
            {
                ComputeReferences = true,
                IncludeNonVisibleObjects = true
            };

            GeometryElement geomElement = elem.get_Geometry(geomOptions);

            if (geomElement != null)
            {
                foreach (GeometryObject geomObj in geomElement)
                {
                    if (geomObj is GeometryInstance instance)
                    {
                        ProcessGeometryInstance(instance, elem.Id, doc, identifiedRay, PlaneFace, intersections);
                    }
                    else if (geomObj is Solid solid)
                    {
                        // Pass "Solid" as the geometryType for standalone Solids
                        ProcessSolid(solid, elem.Id, doc, identifiedRay, PlaneFace, intersections, "Solid");
                    }
                    // Additional handling for other GeometryObject types if needed
                }
            }
        }



        private void ProcessGeometryInstance(GeometryInstance instance, SelectedElementId elemId, Document doc, IdentifiedRay identifiedRay, Face PlaneFace, List<IntersectionInfo> intersections)
        {
            // Retrieve the transformed geometry of the GeometryInstance
            GeometryElement instanceGeometry = instance.GetInstanceGeometry();

            // Process the geometry objects within the GeometryInstance
            foreach (GeometryObject geomObj in instanceGeometry)
            {
                if (geomObj is Solid solid)
                {
                    // Process each solid within the GeometryInstance
                    ProcessSolid(solid, elemId, doc, identifiedRay, PlaneFace, intersections, "GeometryInstance");
                }
                // Handle other types of geometry objects if necessary
            }
        }

        private void ProcessSolid(Solid solid, SelectedElementId elemId, Document doc, IdentifiedRay identifiedRay, Face PlaneFace, List<IntersectionInfo> intersections, string geometryType)
        {
            foreach (Face face in solid.Faces)
            {
                // Skip the selected face
                if (face.Equals(PlaneFace))
                    continue;

                IntersectionResultArray results;
                SetComparisonResult result = face.Intersect(identifiedRay.Ray, out results);

                if (result == SetComparisonResult.Overlap && results != null && results.Size > 0)
                {
                    foreach (IntersectionResult ir in results)
                    {
                        Reference faceRef = face.Reference; // No longer checking if faceRef is null

                        intersections.Add(new IntersectionInfo
                        {
                            SelectedElementId = elemId,
                            FirstRayFaceIntersectionPoint = ir.XYZPoint,
                            Face = face,
                            FaceReference = faceRef, // Added even if it's null
                            Distance = CalculateDistance(PlaneFace, ir.XYZPoint),
                            RayId = identifiedRay.RayId,
                            ElementType = "Solid",
                            GeometryType = geometryType
                        });
                    }
                }
            }
        }




        private double CalculateDistance(Face face, XYZ point)
        {
            IntersectionResult result = face.Project(point);
            double distanceInFeet = result.Distance;

            // Convert distance from feet to meters
            double distanceInMeters = distanceInFeet * 0.3048; // Since 1 foot = 0.3048 meters

            return distanceInMeters;
        }


        private Dictionary<string, List<IntersectionInfo>> CalculateAndSortIntersections(Document doc, List<IdentifiedRay> rays, Face PlaneFace, SelectedElementId simulationShapeId)
        {
            Dictionary<string, List<IntersectionInfo>> sortedIntersections = new Dictionary<string, List<IntersectionInfo>>();

            foreach (IdentifiedRay ray in rays)
            {
                List<IntersectionInfo> intersections = FindIntersections(doc, ray, PlaneFace, simulationShapeId);

                // Sort intersections by distance for each ray
                var sorted = intersections
                    .Where(intersection => intersection.Face != PlaneFace)
                    .OrderBy(i => i.Distance)
                    .ToList();

                sortedIntersections.Add(ray.RayId, sorted);
            }

            return sortedIntersections;
        }

        private Dictionary<string, IntersectionInfo> GetClosestIntersections(Dictionary<string, List<IntersectionInfo>> rayIntersections)
        {
            var closestIntersections = new Dictionary<string, IntersectionInfo>();

            foreach (var rayPair in rayIntersections)
            {
                string rayId = rayPair.Key;
                var intersections = rayPair.Value;

                if (intersections.Any())
                {
                    closestIntersections[rayId] = intersections.First();
                }
            }

            return closestIntersections;
        }

        private void DisplayClosestIntersections(Dictionary<string, IntersectionInfo> closestIntersections, Document doc)
        {
            StringBuilder allIntersectionInfo = new StringBuilder();
            bool anyIntersectionsFound = false;

            foreach (var intersectionPair in closestIntersections)
            {
                anyIntersectionsFound = true;
                string rayId = intersectionPair.Key;
                IntersectionInfo intersection = intersectionPair.Value;

                // Use a different string format based on whether the intersection is with a GeometryInstance or a Solid
                string intersectionInfoLine;
                if (intersection.GeometryType == "GeometryInstance")
                {
                    intersectionInfoLine = $"Ray ID: {rayId}, Element ID: {intersection.SelectedElementId.IntegerValue}, " +
                                           $"Geometry Type: {intersection.GeometryType}, " +
                                           $"Distance: {intersection.Distance:F2} meters, " +
                                           $"Face Reference: {GetClosestIntersectionFaceReferenceString(intersection, doc)}";
                }
                else if (intersection.ElementType == "Solid")
                {
                    intersectionInfoLine = $"Ray ID: {rayId}, Element ID: {intersection.SelectedElementId.IntegerValue}, " +
                                           $"Element Type: {intersection.ElementType}, " +
                                           $"Distance: {intersection.Distance:F2} meters, " +
                                           $"Face Reference: {GetClosestIntersectionFaceReferenceString(intersection, doc)}";
                }
                else
                {
                    // Handle any other cases or provide a generic format
                    intersectionInfoLine = $"Ray ID: {rayId}, Element ID: {intersection.SelectedElementId.IntegerValue}, " +
                                           $"Distance: {intersection.Distance:F2} meters, " +
                                           $"Face Reference: {GetClosestIntersectionFaceReferenceString(intersection, doc)}";
                }

                allIntersectionInfo.AppendLine(intersectionInfoLine);
            }

            if (anyIntersectionsFound)
            {
                TaskDialog.Show("Closest Intersections Summary", allIntersectionInfo.ToString());
            }
            else
            {
                TaskDialog.Show("Info", "No intersections found for any of the rays.");
            }
        }

        // Make sure there is only one method with this signature in your class
        private string GetClosestIntersectionFaceReferenceString(IntersectionInfo intersection, Document doc)
        {
            // Convert the face reference to a stable representation or return "N/A" or "Unknown" if not applicable
            return intersection.FaceReference != null ? intersection.FaceReference.ConvertToStableRepresentation(doc) : "N/A";
        }




        public int CalculateRayDensity(View3D view3D, double factor)
        {
            // Example calculation (modify as needed for your simulation)
            // This could be a dynamic value based on view complexity or other factors
            // 'factor' could be a user-defined parameter or derived from the model's characteristics
            int baseDensity = 5; // A base value for ray density

            // Adjust the density based on the factor (e.g., complexity, user input, etc.)
            return baseDensity + (int)(factor * 5); // Just an example calculation
        }

        public double CalculateRaySpacing(double planeLength, double planeWidth, int rayDensity)
        {
            // Calculate the total area of the plane
            double planeArea = planeLength * planeWidth;

            // Determine the spacing needed to achieve the desired density
            // Assuming a uniform distribution of rays over the plane
            return Math.Sqrt(planeArea / rayDensity);
        }

        public void HighlightClosestIntersectionElements(Document doc, Dictionary<string, IntersectionInfo> closestIntersections)
        {
            List<SelectedElementId> elementIdsToHighlight = new List<SelectedElementId>();

            foreach (var intersection in closestIntersections.Values)
            {
                if (intersection.SelectedElementId != null && intersection.SelectedElementId != SelectedElementId.InvalidElementId)
                {
                    elementIdsToHighlight.Add(intersection.SelectedElementId);
                }
            }

            // Get UIDocument
            UIDocument uiDoc = new UIDocument(doc);

            // Highlight elements
            uiDoc.Selection.SetElementIds(elementIdsToHighlight);
        }


        private List<Reference> GetClosestIntersectionFaceReferences(Document doc, Dictionary<string, List<IntersectionInfo>> rayIntersections)
        {
            var closestIntersectionFaceRefs = new List<Reference>(); // List to store face references of the closest intersections

            foreach (var rayPair in rayIntersections)
            {
                var intersections = rayPair.Value;
                if (intersections != null && intersections.Count > 0)
                {
                    // Assuming intersections are already sorted by distance
                    var closestIntersection = intersections.First();
                    if (closestIntersection.FaceReference != null)
                    {
                        closestIntersectionFaceRefs.Add(closestIntersection.FaceReference);
                    }
                }
            }

            // Now closestIntersectionFaceRefs contains the face references of the closest intersections for each ray
            return closestIntersectionFaceRefs;
        }



        private void SetUpAndApplyAVF(Document doc, Face face, Reference faceRef)
        {
            // Create or get SpatialFieldManager for the active view
            SpatialFieldManager sfm = SpatialFieldManager.GetSpatialFieldManager(doc.ActiveView);
            if (sfm == null)
            {
                sfm = SpatialFieldManager.CreateSpatialFieldManager(doc.ActiveView, 1);
            }

            // Create an AnalysisDisplayStyle
            AnalysisDisplayStyle analysisDisplayStyle = CreateDefaultAnalysisDisplayStyle(doc, "Custom AVF Style");

            // Set the AnalysisDisplayStyle to the active view
            doc.ActiveView.AnalysisDisplayStyleId = analysisDisplayStyle.Id;

            // Register a new result schema with the SpatialFieldManager
            AnalysisResultSchema resultSchema = new AnalysisResultSchema("Custom Schema", "Description");
            //int schemaIndex = sfm.RegisterResult(resultSchema);
            int schemaIndex = GetOrCreateAnalysisResultSchemaIndex(sfm, "Custom Schema", "Description");

            // Prepare data for AVF
            IList<UV> uvPoints;
            FieldDomainPointsByUV fieldPoints = GetFieldDomainPointsByUV(face, out uvPoints);
            FieldValues fieldValues = GetFieldValues(uvPoints);

            // Register face with SpatialFieldManager
            int primitiveId = sfm.AddSpatialFieldPrimitive(faceRef);

            // Update the spatial field with the correct resultIndex
            sfm.UpdateSpatialFieldPrimitive(primitiveId, fieldPoints, fieldValues, schemaIndex);
        }

        private int GetOrCreateAnalysisResultSchemaIndex(SpatialFieldManager sfm, string schemaName, string schemaDescription)
        {
            foreach (int index in sfm.GetRegisteredResults())
            {
                AnalysisResultSchema existingSchema = sfm.GetResultSchema(index);
                if (existingSchema.Name.Equals(schemaName))
                {
                    // Schema already exists, return its index
                    return index;
                }
            }

            // Schema does not exist, create a new one
            AnalysisResultSchema newSchema = new AnalysisResultSchema(schemaName, schemaDescription);
            return sfm.RegisterResult(newSchema);
        }

        private AnalysisDisplayStyle CreateDefaultAnalysisDisplayStyle(Document doc, string styleName)
        {
            var existingStyle = new FilteredElementCollector(doc)
                .OfClass(typeof(AnalysisDisplayStyle))
                .Cast<AnalysisDisplayStyle>()
                .FirstOrDefault(style => style.Name.Equals(styleName));

            if (existingStyle != null)
                return existingStyle;

            AnalysisDisplayColoredSurfaceSettings surfaceSettings = new AnalysisDisplayColoredSurfaceSettings
            {
                ShowGridLines = false
            };

            AnalysisDisplayColorSettings colorSettings = new AnalysisDisplayColorSettings
            {
                MaxColor = new Color(255, 0, 0), // Red
                MinColor = new Color(0, 255, 0)  // Green
            };

            AnalysisDisplayLegendSettings legendSettings = new AnalysisDisplayLegendSettings
            {
                ShowLegend = true,
                NumberOfSteps = 10,             // Adjust the number of steps in the legend
                ShowDataDescription = true,     // Show or hide data description
                Rounding = 0.1                  // Set rounding for values
                                                // Note: Direct control over legend size is not available
            };

            return AnalysisDisplayStyle.CreateAnalysisDisplayStyle(doc, styleName, surfaceSettings, colorSettings, legendSettings);
        }




        private FieldDomainPointsByUV GetFieldDomainPointsByUV(Face face, out IList<UV> uvPoints)
        {
            uvPoints = new List<UV>();
            BoundingBoxUV bbox = face.GetBoundingBox();
            double uStep = (bbox.Max.U - bbox.Min.U) / 10; // 10 steps across the U direction
            double vStep = (bbox.Max.V - bbox.Min.V) / 10; // 10 steps across the V direction

            for (double u = bbox.Min.U; u <= bbox.Max.U; u += uStep)
            {
                for (double v = bbox.Min.V; v <= bbox.Max.V; v += vStep)
                {
                    UV uv = new UV(u, v);
                    if (face.IsInside(uv))
                    {
                        uvPoints.Add(uv);
                    }
                }
            }

            return new FieldDomainPointsByUV(uvPoints);
        }



        private FieldValues GetFieldValues(IList<UV> uvPoints)
        {
            IList<ValueAtPoint> values = new List<ValueAtPoint>();
            foreach (UV uv in uvPoints)
            {
                double sampleValue = uv.U + uv.V; // Replace this with actual analysis value
                values.Add(new ValueAtPoint(new List<double> { sampleValue }));
            }

            return new FieldValues(values);
        }



    }

    // Updated IntersectionInfo class to include distance
    public class IntersectionInfo
    {
        public SelectedElementId SelectedElementId { get; set; }
        public XYZ FirstRayFaceIntersectionPoint { get; set; }
        public Face Face { get; set; }
        public Reference FaceReference { get; set; }
        public double Distance { get; set; }
        public string RayId { get; set; }
        public string ElementType { get; set; } // Add this line to store the element type
        public string GeometryType { get; set; } // New property to store the type of geometry
    }

    public class ClosestIntersectionInfo
    {
        public SelectedElementId SelectedElementId { get; set; }
        public XYZ FirstRayFaceIntersectionPoint { get; set; }
        public Face Face { get; set; }
        public Reference FaceReference { get; set; }
        public double Distance { get; set; }
        public string RayId { get; set; }
        public string ElementType { get; set; }
        public string GeometryType { get; set; }

        public ClosestIntersectionInfo(IntersectionInfo intersectionInfo)
        {
            // Assign values from intersectionInfo to the properties of ClosestIntersectionInfo
            // Example:
            this.SelectedElementId = intersectionInfo.SelectedElementId;
            this.FirstRayFaceIntersectionPoint = intersectionInfo.FirstRayFaceIntersectionPoint;
            this.Face = intersectionInfo.Face;
            this.FaceReference = intersectionInfo.FaceReference;
            this.Distance = intersectionInfo.Distance;
            this.RayId = intersectionInfo.RayId;
            this.ElementType = intersectionInfo.ElementType;
            this.GeometryType = intersectionInfo.GeometryType;

            // Add any additional properties that need to be copied over
        }

        // You can add additional properties or methods specific to closest intersections here
    }

    public class SimulationPlaneInfo
    {
        public Face PlaneFace { get; set; }
        public XYZ PlaneOrigin { get; set; }
        public double PlaneLength { get; set; }
        public double PlaneWidth { get; set; }
        public SelectedElementId SimulationShapeId { get; set; } // Add this line
    }

    public class IdentifiedRay
    {
        public Line Ray { get; set; }
        public string RayId { get; set; }
        public SelectedElementId SelectedElementId { get; set; } // Add this line if SelectedElementId should be a part of IdentifiedRay
    }


}

*/
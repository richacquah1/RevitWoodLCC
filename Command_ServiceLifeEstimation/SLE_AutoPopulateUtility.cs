
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Globalization;
using Autodesk.Revit.UI;
using System.Text;
using Autodesk.Revit.DB.Structure;
using Microsoft.Scripting.Interpreter;
using System.Data.Entity.Core.Metadata.Edm;
using NetTopologySuite.Algorithm;
using Microsoft.Scripting.Actions;
using System.Diagnostics;


namespace RevitWoodLCC
{
    public static class SLE_AutoPopulateUtility
    {


        public static bool IsElementInGround(Element element, Document doc)
        {
            try
            {
                // Get the bounding box of the selected element
                BoundingBoxXYZ elementBoundingBox = element.get_BoundingBox(null);
                if (elementBoundingBox == null)
                {
                    return false;
                }

                // Get all TopographySurface elements in the document
                var topoSurfaces = new FilteredElementCollector(doc)
                    .OfClass(typeof(TopographySurface))
                    .Cast<TopographySurface>();

                // Check if the element's bounding box intersects with any topography surface
                foreach (var topoSurface in topoSurfaces)
                {
                    BoundingBoxXYZ topoBoundingBox = topoSurface.get_BoundingBox(null);
                    if (topoBoundingBox != null && BoundingBoxesIntersect(elementBoundingBox, topoBoundingBox))
                    {
                        return true;
                    }
                }

                return false;
            }
            catch
            {
                // If an exception occurs, assume the element is not in ground
                return false;
            }
        }

        public static bool PopulateGroundCondition(
            System.Windows.Controls.ComboBox soilContactField,
            Element element,
            Document doc,
            ref string message,
            Action disableControlsAction)
        {
            try
            {
                // Check if the element is in-ground
                bool isInContactWithTopography = IsElementInGround(element, doc);

                if (isInContactWithTopography)
                {
                    ComboBoxItem inGroundItem = soilContactField.Items.Cast<ComboBoxItem>()
                        .FirstOrDefault(item => item.Content.ToString() == "In-Ground");

                    if (inGroundItem != null)
                    {
                        soilContactField.Dispatcher.Invoke(() =>
                        {
                            soilContactField.SelectedItem = inGroundItem;
                        });

                        // Disable and clear irrelevant fields
                        disableControlsAction?.Invoke();

                        message += "The selected element is in contact with a TopographySurface. 'In-Ground' has been selected.\n";
                    }
                    else
                    {
                        message += "ComboBox does not contain 'In-Ground'. Please ensure it is added to the list.\n";
                    }
                }
                else
                {
                    message += "The selected element is not in contact with any TopographySurface.\n";
                }

                return true;
            }
            catch (Exception ex)
            {
                message += $"Error in PopulateGroundCondition: {ex.Message}\n";
                return false;
            }
        }



        private static bool BoundingBoxesIntersect(BoundingBoxXYZ box1, BoundingBoxXYZ box2)
        {
            // Check if there is any overlap in the X, Y, and Z dimensions
            bool xOverlap = box1.Max.X >= box2.Min.X && box1.Min.X <= box2.Max.X;
            bool yOverlap = box1.Max.Y >= box2.Min.Y && box1.Min.Y <= box2.Max.Y;
            bool zOverlap = box1.Max.Z >= box2.Min.Z && box1.Min.Z <= box2.Max.Z;

            // The boxes intersect if there is overlap in all three dimensions
            return xOverlap && yOverlap && zOverlap;
        }


        public static void PopulateShelterCondition(
            CheckBox roofOverhangCheckbox,
            CheckBox verticalMemberCheckbox,
            System.Windows.Controls.TextBox groundDistTextBox,
            System.Windows.Controls.TextBox overhangTextBox,
            System.Windows.Controls.TextBox shelterDistTextBox,
            Element element,
            Document doc,
            ref string message)
        {
            if (element == null || doc == null)
            {
                message += "Error: Element or document is null.\n";
                return;
            }

            // Initialize lists to store ray information
            // Always reinitialize the list at the start of the method
            List<VerticalRayInfo> verticalRayInfos = new List<VerticalRayInfo>();
            Debug.WriteLine("Initialized verticalRayInfos for PopulateShelterCondition.");


            List<ThirtyDegreeRayInfo> angledRayInfos = new List<ThirtyDegreeRayInfo>();   // List for 30-degree rays

            List<string> obstructionDetails = new List<string>(); // Initialize obstruction details list

            // Determine if the element is sheltered (ray information is populated)
            bool isSheltered = CheckIfElementIsSheltered(element, doc, ref message, verticalRayInfos, angledRayInfos);
            Debug.WriteLine($"PopulateShelterCondition: verticalRayInfos contains {verticalRayInfos.Count} rays.");
            foreach (var ray in verticalRayInfos)
            {
                Debug.WriteLine($"Ray ID: {ray.RayId}, Origin: {ray.Origin}, Direction: {ray.Direction}, Obstructed: {ray.IsObstructed}, Distance: {ray.ObstructionDistance}");
            }



            // Set the checkbox states based on the shelter status
            roofOverhangCheckbox.IsChecked = isSheltered;

            string orientation = DetermineElementOrientation(element);

            if (orientation == "Vertical")
            {
                verticalMemberCheckbox.IsChecked = true;
                message += "The element is vertical.\n";
            }
            else if (orientation == "Horizontal")
            {
                verticalMemberCheckbox.IsChecked = false;
                message += "The element is horizontal.\n";
            }
            else if (orientation == "Inclined")
            {
                verticalMemberCheckbox.IsChecked = false;
                message += "The element is inclined.\n";
            }
            else
            {
                message += "Unable to determine the orientation of the element.\n";
            }



            if (isSheltered)
            {
                try
                {
                    // Populate related fields based on shelter condition
                    groundDistTextBox.Text = CalculateGroundDistance(element, doc).ToString("F2", CultureInfo.InvariantCulture);
                }
                catch (Exception ex)
                {
                    message += $"Error during ground distance calculation: {ex.Message}\n";
                }

                // Handle overhang length calculation separately
                try
                {
                    //Populate overhang length(with added null check)
                    double overhangLength = CalculateOverhangLength(element, doc, verticalRayInfos);

                    if (overhangLength > 0)
                    {
                        overhangTextBox.Text = overhangLength.ToString("F2", CultureInfo.InvariantCulture);
                    }
                    else
                    {
                        message += "Warning: Overhang length calculation returned 0.\n";
                    }
                }
                catch (Exception ex)
                {
                    message += $"Error during overhang length calculation: {ex.Message}\n";
                }

                // Handle shelter distance calculation separately
                try
                {
                    // Use the CalculateShelterDistance method to determine shelter distance for vertical rays
                    double shelterDistance = CalculateShelterDistance(element, doc, verticalRayInfos);

                    if (shelterDistance > 0)
                    {
                        shelterDistTextBox.Text = shelterDistance.ToString("F5", CultureInfo.InvariantCulture);
                    }
                    else
                    {
                        MessageBox.Show("No shelter distance calculated.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                catch (Exception ex)
                {
                    message += $"Error during shelter distance calculation: {ex.Message}\n";
                }

            }
        }



        //------------------------start exposure condition code----------------------------
        public static void PopulateExposureCondition(System.Windows.Controls.ComboBox exposureField, Element element, Document doc, ref string debugInfo)
        {
            if (exposureField.Items.Count == 0)
            {
                exposureField.Dispatcher.Invoke(() =>
                {
                    exposureField.Items.Add(new ComboBoxItem { Content = "Side grain exposed" });
                    exposureField.Items.Add(new ComboBoxItem { Content = "End grain exposed" });
                });
            }

            List<FaceExposureDetail> faceDetails = GetExposureCondition(element, doc, ref debugInfo);
            View3D active3DView = GetActive3DView(doc);
            CheckExposureByRayCasting(faceDetails, doc, active3DView, ref debugInfo);

            // Check the exposure conditions of the end grain faces
            bool anyEndGrainExposed = faceDetails
                .Where(d => d.GrainType == "End Grain")
                .Any(d => d.IsExposed); // Updated logic: true if at least one end grain face is exposed

            ComboBoxItem selectedItem;

            if (anyEndGrainExposed)
            {
                selectedItem = exposureField.Items.Cast<ComboBoxItem>().FirstOrDefault(item => item.Content.ToString() == "End grain exposed");
                debugInfo += "At least one end grain face is exposed.\n";
            }
            else
            {
                selectedItem = exposureField.Items.Cast<ComboBoxItem>().FirstOrDefault(item => item.Content.ToString() == "Side grain exposed");
                debugInfo += "All end grain faces are sheltered.\n";
            }

            if (selectedItem != null)
            {
                exposureField.Dispatcher.Invoke(() => { exposureField.SelectedItem = selectedItem; });
            }
        }



        public static List<FaceExposureDetail> GetExposureCondition(Element element, Document doc, ref string debugInfo)
        {
            List<FaceExposureDetail> faceDetails = new List<FaceExposureDetail>();

            try
            {
                // Get references of all the faces in the element
                var faceReferences = GetFaceReferences(element).ToList();

                // Calculate the centroid of the entire element
                XYZ elementCentroid = CalculateCentroidOfElement(element);
                debugInfo += $"Element Centroid: {elementCentroid}\n";

                // Get the faces at the maximum and minimum distances from the element centroid
                Reference maxDistanceFaceRef, minDistanceFaceRef;
                GetMaxMinDistanceFaces(faceReferences, element, elementCentroid, out maxDistanceFaceRef, out minDistanceFaceRef, ref debugInfo);

                // Calculate the direction of the main axis by finding the vector between the farthest faces
                Face maxFace = element.GetGeometryObjectFromReference(maxDistanceFaceRef) as Face;
                Face minFace = element.GetGeometryObjectFromReference(minDistanceFaceRef) as Face;

                if (maxFace != null && minFace != null)
                {
                    // Use the CentroidUV stored in each face detail to evaluate the centroid of the face
                    BoundingBoxUV maxFaceBBox = maxFace.GetBoundingBox();
                    UV maxFaceCentroidUV = new UV((maxFaceBBox.Min.U + maxFaceBBox.Max.U) / 2, (maxFaceBBox.Min.V + maxFaceBBox.Max.V) / 2);
                    XYZ maxFaceCentroid = maxFace.Evaluate(maxFaceCentroidUV);

                    BoundingBoxUV minFaceBBox = minFace.GetBoundingBox();
                    UV minFaceCentroidUV = new UV((minFaceBBox.Min.U + minFaceBBox.Max.U) / 2, (minFaceBBox.Min.V + minFaceBBox.Max.V) / 2);
                    XYZ minFaceCentroid = minFace.Evaluate(minFaceCentroidUV);

                    // Calculate the main axis direction vector between the farthest points
                    XYZ mainAxis = (maxFaceCentroid - minFaceCentroid).Normalize();
                    debugInfo += $"Main Axis Direction: {mainAxis}\n";

                    // Iterate over each face reference and calculate face-specific details
                    foreach (Reference faceRef in faceReferences)
                    {
                        Face face = element.GetGeometryObjectFromReference(faceRef) as Face;
                        if (face == null)
                            continue;

                        // Compute the normal of the face
                        BoundingBoxUV bbox = face.GetBoundingBox();
                        UV centroidUV = new UV((bbox.Min.U + bbox.Max.U) / 2, (bbox.Min.V + bbox.Max.V) / 2);
                        XYZ faceNormal = face.ComputeNormal(centroidUV);

                        // Calculate the angle between the face normal and the main axis direction
                        double angle = faceNormal.AngleTo(mainAxis) * (180 / Math.PI);
                        string grainType = IdentifyGrainType(angle);
                        debugInfo += $"Face Normal: {faceNormal}, Angle: {angle}°, Classified as: {grainType}\n";

                        // Calculate the centroid of the face
                        XYZ faceCentroid = face.Evaluate(centroidUV);

                        // Add the face details to the list
                        faceDetails.Add(new FaceExposureDetail
                        {
                            FaceReference = faceRef,
                            GrainType = grainType,
                            Normal = faceNormal,
                            Centroid = faceCentroid,
                            CentroidUV = centroidUV,
                            IsExposed = false // Initial value, to be updated after ray casting
                        });
                    }
                }
                else
                {
                    debugInfo += "Error: Could not determine max and min distance faces.\n";
                }
            }
            catch (Exception ex)
            {
                debugInfo += $"Error in GetExposureCondition: {ex.Message}\n{ex.StackTrace}\n";
            }

            return faceDetails;
        }



        public static void CheckExposureByRayCasting(List<FaceExposureDetail> faceDetails, Document doc, View3D active3DView, ref string debugInfo)
        {
            // Filter face details to only include End Grain faces
            var endGrainFaces = faceDetails.Where(detail => detail.GrainType == "End Grain").ToList();

            foreach (var detail in endGrainFaces)
            {
                Element element = doc.GetElement(detail.FaceReference.ElementId);
                Face face = element.GetGeometryObjectFromReference(detail.FaceReference) as Face;
                if (face == null)
                    continue;

                // Get the bounding box of the face
                BoundingBoxUV bbox = face.GetBoundingBox();
                UV min = bbox.Min;
                UV max = bbox.Max;

                double faceWidth = max.U - min.U;
                double faceHeight = max.V - min.V;

                double minGridSize = 0.2; // Adjusted grid size for fewer rays
                int numGridsX = Math.Max(1, (int)Math.Floor(faceWidth / minGridSize));
                int numGridsY = Math.Max(1, (int)Math.Floor(faceHeight / minGridSize));

                double gridWidth = faceWidth / numGridsX;
                double gridHeight = faceHeight / numGridsY;

                List<XYZ> allIntersectionPoints = new List<XYZ>();

                // Get the transform of the element if it's a FamilyInstance
                Transform instanceTransform = null;

                if (element is FamilyInstance familyInstance)
                {
                    instanceTransform = familyInstance.GetTransform();
                }
                else
                {
                    // Retrieve geometry from the element
                    Options options = new Options { ComputeReferences = true };
                    GeometryElement geometryElement = element.get_Geometry(options);

                    // Check if there are any GeometryInstances and get their transform
                    if (geometryElement != null)
                    {
                        foreach (GeometryObject geoObject in geometryElement)
                        {
                            if (geoObject is GeometryInstance geometryInstance)
                            {
                                instanceTransform = geometryInstance.Transform;
                                break; // Stop after finding the first GeometryInstance
                            }
                        }
                    }
                }

                // If instanceTransform is still null, we skip ray casting for this element
                if (instanceTransform == null)
                {
                    debugInfo += "Element does not have a valid transformation.\n";
                    continue; // Skip to the next iteration
                }

                // Loop through grid points on the face
                for (double u = min.U; u < max.U; u += gridWidth)
                {
                    for (double v = min.V; v < max.V; v += gridHeight)
                    {
                        UV centroidUV = new UV(u + gridWidth / 2, v + gridHeight / 2);
                        XYZ centroid = face.Evaluate(centroidUV);
                        XYZ faceNormal = face.ComputeNormal(centroidUV);

                        // Apply transformation to centroid and face normal
                        XYZ transformedCentroid = instanceTransform.OfPoint(centroid);
                        XYZ transformedNormal = instanceTransform.OfVector(faceNormal);

                        // Ray direction is the transformed normal of the face
                        XYZ rayDirection = transformedNormal.Normalize();

                        // Set the ray length to 0.1 meters (10 cm)
                        double rayLength = 1; //was 0.1 in meters

                        // Check for intersections using the ReferenceIntersector
                        ReferenceIntersector intersector = new ReferenceIntersector(GetModelCategoryFilter(doc), FindReferenceTarget.Face, active3DView);
                        IList<ReferenceWithContext> intersectedRefs = intersector.Find(transformedCentroid, rayDirection);

                        bool isObstructed = false;
                        foreach (var refContext in intersectedRefs)
                        {
                            if (refContext.GetReference().ElementId != detail.FaceReference.ElementId)
                            {
                                isObstructed = true;
                                double obstructionDistance = refContext.Proximity;
                                XYZ intersectionPoint = transformedCentroid + rayDirection * Math.Min(obstructionDistance, rayLength);
                                allIntersectionPoints.Add(intersectionPoint);
                                break; // Break after finding the first obstruction
                            }
                        }

                        // Visualize the ray for each point, only up to 10 cm
                        Color rayColor = isObstructed ? new Color(255, 0, 0) : new Color(0, 255, 0);
                        //VisualizeRay(doc, transformedCentroid, transformedNormal, rayLength, rayColor); // Use rayLength here
                    }
                }

                // Set exposure based on ray results
                detail.IsExposed = allIntersectionPoints.Count == 0; // If no obstructed rays, the face is exposed
                debugInfo += $"Face {detail.FaceReference.ConvertToStableRepresentation(doc)}: Exposed = {detail.IsExposed}\n";
            }
        }

        private static IEnumerable<Reference> GetFaceReferences(Element element)
        {
            Options options = new Options { ComputeReferences = true };
            GeometryElement geometryElement = element.get_Geometry(options);
            List<Reference> faceReferences = new List<Reference>();
            CollectFaceReferences(geometryElement, faceReferences);
            return faceReferences;
        }

        private static void CollectFaceReferences(GeometryElement geomElement, List<Reference> faceReferences, Transform parentTransform = null)
        {
            Transform currentTransform = parentTransform ?? Transform.Identity;
            foreach (var geoObj in geomElement)
            {
                if (geoObj is Solid solid)
                {
                    faceReferences.AddRange(solid.Faces.Cast<Face>().Select(face => face.Reference));
                }
                else if (geoObj is GeometryInstance instance)
                {
                    Transform combinedTransform = currentTransform.Multiply(instance.Transform);
                    CollectFaceReferences(instance.GetSymbolGeometry(), faceReferences, combinedTransform);
                }
            }
        }

        private static XYZ CalculateCentroidOfElement(Element element)
        {
            List<XYZ> vertices = GetVerticesFromElementGeometry(element);
            return vertices.Aggregate(XYZ.Zero, (current, vertex) => current + vertex) / vertices.Count;
        }

        private static List<XYZ> GetVerticesFromElementGeometry(Element element)
        {
            List<XYZ> vertices = new List<XYZ>();
            Options options = new Options { ComputeReferences = true };
            GeometryElement geomElement = element.get_Geometry(options);
            CollectVerticesFromGeometry(geomElement, vertices);
            return vertices;
        }

        private static void CollectVerticesFromGeometry(GeometryElement geomElement, List<XYZ> vertices, Transform transform = null)
        {
            Transform currentTransform = transform ?? Transform.Identity;
            foreach (GeometryObject geomObj in geomElement)
            {
                if (geomObj is Solid solid)
                {
                    foreach (Edge edge in solid.Edges)
                    {
                        vertices.AddRange(edge.Tessellate().Select(point => currentTransform.OfPoint(point)));
                    }
                }
                else if (geomObj is GeometryInstance instance)
                {
                    Transform combinedTransform = currentTransform.Multiply(instance.Transform);
                    CollectVerticesFromGeometry(instance.GetInstanceGeometry(), vertices, combinedTransform);
                }
            }
        }

        // Get faces at maximum and minimum distances from the centroid
        private static void GetMaxMinDistanceFaces(List<Reference> faceReferences, Element element, XYZ elementCentroid,
                                          out Reference maxDistanceFaceRef, out Reference minDistanceFaceRef, ref string debugInfo)
        {
            double maxDistance = double.MinValue, minDistance = double.MaxValue;
            maxDistanceFaceRef = null;
            minDistanceFaceRef = null;

            foreach (Reference faceRef in faceReferences)
            {
                Face face = element.GetGeometryObjectFromReference(faceRef) as Face;
                BoundingBoxUV bbox = face.GetBoundingBox();
                UV centroidUV = new UV((bbox.Min.U + bbox.Max.U) / 2, (bbox.Min.V + bbox.Max.V) / 2);
                XYZ faceCentroid = face.Evaluate(centroidUV);
                double faceToElementCentroidDistance = faceCentroid.DistanceTo(elementCentroid);

                if (faceToElementCentroidDistance > maxDistance)
                {
                    maxDistance = faceToElementCentroidDistance;
                    maxDistanceFaceRef = faceRef;
                }
                if (faceToElementCentroidDistance < minDistance)
                {
                    minDistance = faceToElementCentroidDistance;
                    minDistanceFaceRef = faceRef;
                }
                debugInfo += $"Face Reference: {faceRef.ConvertToStableRepresentation(element.Document)}, FaceToElementCentroidDistance: {faceToElementCentroidDistance}\n";
            }
        }

        private static StringBuilder _debugLog = new StringBuilder();

        public static string IdentifyGrainType(double angle)
        {
            const double tolerance = 15.0;
            string grainType;

            // Determine grain type based on the angle
            if (Math.Abs(angle) <= tolerance || Math.Abs(angle - 180) <= tolerance)
            {
                grainType = "End Grain";
                _debugLog.AppendLine($"Angle: {angle}°, Classified as: End Grain");
            }
            else if (Math.Abs(angle - 90) <= tolerance)
            {
                grainType = "Side Grain";
                _debugLog.AppendLine($"Angle: {angle}°, Classified as: Side Grain");
            }
            else
            {
                grainType = (Math.Min(Math.Abs(angle), Math.Abs(angle - 180)) < Math.Abs(angle - 90))
                    ? "End Grain"
                    : "Side Grain";
                _debugLog.AppendLine($"Angle: {angle}°, Classified as: {grainType}");
            }

            return grainType;
        }

        // Call this method once to display the log
        public static void ShowDebugLog()
        {
            if (_debugLog.Length > 0)
            {
                TaskDialog.Show("Grain Determination Summary", _debugLog.ToString());
                _debugLog.Clear(); // Clear the log after displaying
            }
        }




        private static void VisualizeRay(Document doc, XYZ origin, XYZ direction, double length, Color color)
        {
            using (Transaction trans = new Transaction(doc, "Visualize Ray"))
            {
                trans.Start();

                // Create the line representing the ray from origin to the endpoint
                Line rayLine = Line.CreateBound(origin, origin + (direction * length));

                // Create a DirectShape to represent the ray
                DirectShape rayShape = DirectShape.CreateElement(doc, new ElementId(BuiltInCategory.OST_GenericModel));
                rayShape.SetShape(new List<GeometryObject> { rayLine });

                // Set the color and visibility properties
                OverrideGraphicSettings settings = new OverrideGraphicSettings();
                settings.SetProjectionLineColor(color);
                settings.SetProjectionLineWeight(6); // Adjust line thickness for better visibility

                // Apply the settings to the shape in the active view
                doc.ActiveView.SetElementOverrides(rayShape.Id, settings);

                trans.Commit();
            }
        }

        //------------------------end exposure condition code----------------------------------------

        public static bool PopulateElementIntersectionCondition(
            System.Windows.Controls.ComboBox intersectionConditionField,
            Element element,
            Document doc,
            ref string message)
        {
            try
            {
                // Retrieve all face references for the selected element
                var faceReferences = GetFaceReferences(element).ToList();
                View3D active3DView = GetActive3DView(doc);

                if (active3DView == null)
                {
                    message += "No active 3D view found. Please ensure a 3D view is active.\n";
                    return false;
                }

                // Collect face details
                List<FaceExposureDetail> faceDetails = GetExposureCondition(element, doc, ref message);

                // Perform ray casting for intersection checks on each face
                foreach (var faceDetail in faceDetails)
                {
                    PerformElementIntersectionRayCasting(faceDetail, doc, active3DView, ref message);
                }

                // Use DetermineIntersectionCondition to set and return the intersection condition
                string intersectionCondition = DetermineIntersectionCondition(faceDetails, intersectionConditionField);

                message += $"Intersection Condition: {intersectionCondition}\n";
                return true;
            }
            catch (Exception ex)
            {
                message += $"Error in PopulateElementIntersectionCondition: {ex.Message}\n";
                return false;
            }
        }



        public static void PerformElementIntersectionRayCasting(
            FaceExposureDetail faceDetail,
            Document doc,
            View3D active3DView,
            ref string message)
        {
            Element element = doc.GetElement(faceDetail.FaceReference.ElementId);
            bool isGeometryInstance = false;
            Transform instanceTransform = Transform.Identity;

            // Check if the element is a GeometryInstance or FamilyInstance
            if (element is FamilyInstance familyInstance)
            {
                instanceTransform = familyInstance.GetTransform();
                isGeometryInstance = true;
            }
            else
            {
                // Retrieve geometry from the element
                Options options = new Options { ComputeReferences = true };
                GeometryElement geometryElement = element.get_Geometry(options);

                // Check if there are any GeometryInstances and get their transform
                if (geometryElement != null)
                {
                    foreach (GeometryObject geoObject in geometryElement)
                    {
                        if (geoObject is GeometryInstance geometryInstance)
                        {
                            instanceTransform = geometryInstance.Transform;
                            isGeometryInstance = true;
                            break;
                        }
                        else if (geoObject is Solid)
                        {
                            // For solids, skip transformation
                            isGeometryInstance = false;
                        }
                    }
                }
            }

            // Set up grid dimensions for ray-casting across the face
            Face face = element.GetGeometryObjectFromReference(faceDetail.FaceReference) as Face;
            if (face == null) return;

            BoundingBoxUV bbox = face.GetBoundingBox();
            UV min = bbox.Min;
            UV max = bbox.Max;

            double faceWidth = max.U - min.U;
            double faceHeight = max.V - min.V;

            double minGridSize = 0.2; // Adjusted grid size for fewer rays
            int numGridsX = Math.Max(1, (int)Math.Floor(faceWidth / minGridSize));
            int numGridsY = Math.Max(1, (int)Math.Floor(faceHeight / minGridSize));

            double gridWidth = faceWidth / numGridsX;
            double gridHeight = faceHeight / numGridsY;

            List<XYZ> allIntersectionPoints = new List<XYZ>();

            // Loop through grid points on the face and perform ray casting
            for (double u = min.U; u < max.U; u += gridWidth)
            {
                for (double v = min.V; v < max.V; v += gridHeight)
                {
                    UV centroidUV = new UV(u + gridWidth / 2, v + gridHeight / 2);
                    XYZ centroid = face.Evaluate(centroidUV);
                    XYZ faceNormal = face.ComputeNormal(centroidUV);

                    // Only apply transformations if it's a GeometryInstance
                    XYZ transformedCentroid = isGeometryInstance ? instanceTransform.OfPoint(centroid) : centroid;
                    XYZ transformedNormal = isGeometryInstance ? instanceTransform.OfVector(faceNormal).Normalize() : faceNormal.Normalize();

                    // Set up ray direction
                    XYZ rayDirection = transformedNormal;

                    // Define ray length for exposure checking
                    double rayLength = 1;//was 0.1 in meters

                    // Check for intersections using the ReferenceIntersector
                    ReferenceIntersector intersector = new ReferenceIntersector(GetModelCategoryFilter(doc), FindReferenceTarget.Face, active3DView);
                    IList<ReferenceWithContext> intersectedRefs = intersector.Find(transformedCentroid, rayDirection);

                    bool isObstructed = false;
                    foreach (var refContext in intersectedRefs)
                    {
                        if (refContext.GetReference().ElementId != faceDetail.FaceReference.ElementId)
                        {
                            isObstructed = true;
                            double obstructionDistance = refContext.Proximity;
                            XYZ intersectionPoint = transformedCentroid + rayDirection * Math.Min(obstructionDistance, rayLength);
                            allIntersectionPoints.Add(intersectionPoint);
                            break; // Stop after finding the first obstruction
                        }
                    }

                    // Visualize the ray for each point, only up to 10 cm
                    Color rayColor = isObstructed ? new Color(255, 0, 0) : new Color(0, 255, 0);
                    //VisualizeRay(doc, transformedCentroid, transformedNormal, rayLength, rayColor);
                }
            }

            // Update exposure status based on ray intersection result
            faceDetail.IsExposed = allIntersectionPoints.Count == 0; // If no obstructed rays, the face is exposed
            message += $"Face {faceDetail.FaceReference.ConvertToStableRepresentation(doc)}: IsExposed = {faceDetail.IsExposed}\n";
        }

        public static string DetermineIntersectionCondition(
            List<FaceExposureDetail> faceDetails,
            System.Windows.Controls.ComboBox intersectionConditionField)
        {
            // Default to "Direct contact or insufficient ventilation"
            var defaultText = "Direct contact or insufficient ventilation";

            // Attempt to find the default item in the ComboBox
            var defaultItem = intersectionConditionField.Items
                .Cast<ComboBoxItem>()
                .FirstOrDefault(item => item.Content.ToString() == defaultText);

            // Set the default selection
            if (defaultItem != null)
            {
                intersectionConditionField.SelectedItem = defaultItem;
            }

            // Return the condition text
            return defaultText;
        }



        private static void NEWVisualizeRay(Document doc, XYZ origin, XYZ direction, double length, Color color)
        {
            using (Transaction trans = new Transaction(doc, "Visualize Ray"))
            {
                trans.Start();

                // Create the line representing the ray from origin to the endpoint
                Line rayLine = Line.CreateBound(origin, origin + (direction * length));

                // Create a DirectShape to represent the ray
                DirectShape rayShape = DirectShape.CreateElement(doc, new ElementId(BuiltInCategory.OST_GenericModel));
                rayShape.SetShape(new List<GeometryObject> { rayLine });

                // Set the color and visibility properties
                OverrideGraphicSettings settings = new OverrideGraphicSettings();
                settings.SetProjectionLineColor(color);
                settings.SetProjectionLineWeight(6); // Adjust line thickness for better visibility

                // Apply the settings to the shape in the active view
                doc.ActiveView.SetElementOverrides(rayShape.Id, settings);

                trans.Commit();
            }
        }

        public static View3D GetActive3DView(Document doc)
        {
            // Get the active 3D view in the document
            return new FilteredElementCollector(doc)
                .OfClass(typeof(View3D))
                .Cast<View3D>()
                .FirstOrDefault(view => !view.IsTemplate);
        }


        public static bool CheckIfElementIsSheltered(
        Element element,
        Document doc,
        ref string message,
        List<VerticalRayInfo> verticalRayInfos,  // List for vertical rays
        List<ThirtyDegreeRayInfo> angledRayInfos) // List for 30-degree rays
        {
            BoundingBoxXYZ elementBoundingBox = element.get_BoundingBox(null);
            if (elementBoundingBox == null)
            {
                throw new InvalidOperationException("Could not determine the bounding box of the selected element.");
            }

            GeometryElement geomElem = element.get_Geometry(GetDetailedGeometryOptions());
            View3D active3DView = GetActive3DView(doc);
            if (active3DView == null)
            {
                throw new InvalidOperationException("No active 3D view found. Please ensure a 3D view is active.");
            }

            bool isSheltered = false;
            int totalObstructedRays = 0, totalUnobstructedRays = 0;
            int currentRayId = 0; // Initialize ray ID counter

            // Dictionary to store the face-level shelter status
            Dictionary<Reference, bool> faceShelterConditions = new Dictionary<Reference, bool>();

            using (Transaction trans = new Transaction(doc, "Check Sheltered"))
            {
                trans.Start();

                foreach (GeometryObject geomObj in geomElem)
                {
                    if (geomObj is Solid solid)
                    {
                        ProcessGeometry(
                            solid.Faces,
                            doc,
                            active3DView,
                            element,
                            verticalRayInfos,
                            angledRayInfos,
                            ref totalObstructedRays,
                            ref totalUnobstructedRays,
                            faceShelterConditions,
                            ref message,
                            ref currentRayId // Pass the ray ID counter
                        );
                    }
                    else if (geomObj is GeometryInstance geomInstance)
                    {
                        GeometryElement instanceGeom = geomInstance.GetInstanceGeometry(Transform.Identity);
                        foreach (GeometryObject instanceGeomObj in instanceGeom)
                        {
                            if (instanceGeomObj is Solid instanceSolid)
                            {
                                ProcessGeometry(
                                    instanceSolid.Faces,
                                    doc,
                                    active3DView,
                                    element,
                                    verticalRayInfos,
                                    angledRayInfos,
                                    ref totalObstructedRays,
                                    ref totalUnobstructedRays,
                                    faceShelterConditions,
                                    ref message,
                                    ref currentRayId // Pass the ray ID counter
                                );
                            }
                        }
                    }
                }

                trans.Commit();
            }

            // After processing all geometry
            //DisplayCompiledRayData();

            // Determine the element-level shelter condition (retain the 60% logic)
            int totalRays = totalObstructedRays + totalUnobstructedRays;
            double percentageObstructed = totalRays > 0 ? (double)totalObstructedRays / totalRays * 100.0 : 0;

            if (totalRays > 0 && percentageObstructed >= 60.0)
            {
                isSheltered = true;
                message += "Element is sheltered.\n";
            }
            else
            {
                message += "Element is not sheltered.\n";
            }

            // Log the total rays
            message += $"Total rays: {totalRays}\nObstructed rays: {totalObstructedRays} ({percentageObstructed}% obstructed)\n";

            return isSheltered;
        }

        private static void DisplayCompiledRayData()
        {
            // Compile all ray data
            string compiledData = "Compiled Ray Data:\n";

            // List all ray IDs
            compiledData += "All Ray IDs:\n" + string.Join(", ", RayInfoBase.AllRayIds) + "\n";


            // Display the data using TaskDialog
            TaskDialog.Show("Compiled Ray Data", compiledData);
        }


        private static void ProcessGeometry(
            FaceArray faces,
            Document doc,
            View3D active3DView,
            Element element,
            List<VerticalRayInfo> verticalRayInfos,
            List<ThirtyDegreeRayInfo> angledRayInfos,
            ref int totalObstructedRays,
            ref int totalUnobstructedRays,
            Dictionary<Reference, bool> faceShelterConditions,
            ref string message,
            ref int currentRayId) // Add a reference parameter for the ray ID
        {
            // Calculate the total surface area of the element
            double totalSurfaceArea = faces.Cast<Face>().Sum(face => face.Area);

            foreach (Face face in faces)
            {
                PlanarFace planarFace = face as PlanarFace;
                if (planarFace == null) continue;

                // Initialize a new RayInfoBase instance for this face
                RayInfoBase rayInfo = new RayInfoBase
                {
                    FaceReference = planarFace.Reference
                };

                // Determine the face's orientation (vertical, horizontal, inclined)
                string orientation = Math.Abs(planarFace.FaceNormal.Z) > 0.9 ? "Horizontal" :
                                     Math.Abs(planarFace.FaceNormal.Z) < 0.1 ? "Vertical" : "Inclined";

                // Process based on face orientation (vertical, horizontal, or inclined)
                if (Math.Abs(planarFace.FaceNormal.Z) > 0.9)
                {
                    // Create local variables for ray counts
                    int faceObstructedRays, faceUnobstructedRays;

                    // Cast vertical rays from horizontal faces
                    CheckShelterUsingPerformRayIntersection(
                        face, planarFace, doc, active3DView, element,
                        out faceObstructedRays, out faceUnobstructedRays, out _, out _,
                        verticalRayInfos, angledRayInfos, RayTypeOption.Vertical,
                        ref currentRayId // Pass the ray ID counter
                    );

                    Debug.WriteLine($"ProcessGeometry: Added rays for face {face.Reference.ConvertToStableRepresentation(doc)}. Total rays: {verticalRayInfos.Count}.");
                    foreach (var ray in verticalRayInfos)
                    {
                        Debug.WriteLine($"Ray ID: {ray.RayId}, Obstruction Distance: {ray.ObstructionDistance}, Obstructed: {ray.IsObstructed}");
                    }


                    // Update properties in RayInfoBase
                    rayInfo.FaceObstructedRays = faceObstructedRays;
                    rayInfo.FaceUnobstructedRays = faceUnobstructedRays;
                    rayInfo.ObstructedVerticalRays = faceObstructedRays;
                    rayInfo.UnobstructedVerticalRays = faceUnobstructedRays;

                }
                else if (Math.Abs(planarFace.FaceNormal.Z) < 0.1)
                {
                    // Create local variables for ray counts
                    int faceObstructedRays, faceUnobstructedRays;

                    // Cast 30-degree rays from vertical faces
                    CheckShelterUsingPerformRayIntersection(
                        face, planarFace, doc, active3DView, element,
                        out faceObstructedRays, out faceUnobstructedRays, out _, out _,
                        verticalRayInfos, angledRayInfos, RayTypeOption.ThirtyDegree,
                        ref currentRayId // Pass the ray ID counter
                    );
                    Debug.WriteLine($"ProcessGeometry: Added rays for face {face.Reference.ConvertToStableRepresentation(doc)}. Total rays: {verticalRayInfos.Count}.");
                    foreach (var ray in verticalRayInfos)
                    {
                        Debug.WriteLine($"Ray ID: {ray.RayId}, Obstruction Distance: {ray.ObstructionDistance}, Obstructed: {ray.IsObstructed}");
                    }


                    // Update properties in RayInfoBase
                    rayInfo.FaceObstructedRays = faceObstructedRays;
                    rayInfo.FaceUnobstructedRays = faceUnobstructedRays;
                    rayInfo.ObstructedThirtyDegreeRays = faceObstructedRays;
                    rayInfo.UnobstructedThirtyDegreeRays = faceUnobstructedRays;

                }

                // Calculate face-level ray totals and percentages
                int totalFaceRays = rayInfo.FaceObstructedRays + rayInfo.FaceUnobstructedRays;
                double facePercentageObstructed = totalFaceRays > 0 ? (double)rayInfo.FaceObstructedRays / totalFaceRays * 100.0 : 0;
                bool isFaceSheltered = facePercentageObstructed >= 90.0;

                // Save shelter status for the face
                faceShelterConditions[planarFace.Reference] = isFaceSheltered;


                // Calculate averages for obstruction distances (Reconsider moving this outside the  foreach (Face face in faces) )
                rayInfo.AverageObstructionDistanceVertical =
                    RayInfoBase.VerticalObstructionDistances.Any()
                        ? RayInfoBase.VerticalObstructionDistances.Average()
                        : 0.0;

                rayInfo.AverageObstructionDistanceThirtyDegree =
                    RayInfoBase.ThirtyDegreeObstructionDistances.Any()
                        ? RayInfoBase.ThirtyDegreeObstructionDistances.Average()
                        : 0.0;

                // Add details to the message string
                message += $"Face {planarFace.Reference.ConvertToStableRepresentation(doc)} | " +
                           $"Area: {face.Area:F2} m² ({(face.Area / totalSurfaceArea * 100.0):F2}%) | " +
                           $"Orientation: {orientation} | " +
                           $"Face Normal: ({planarFace.FaceNormal.X:F2}, {planarFace.FaceNormal.Y:F2}, {planarFace.FaceNormal.Z:F2}) | " +
                           $"Obstructed Rays: {rayInfo.FaceObstructedRays}/{totalFaceRays} | " +
                           $"Percentage Obstructed: {facePercentageObstructed:F2}% | " +
                           $"Vertical Rays - Obstructed: {rayInfo.ObstructedVerticalRays}, Unobstructed: {rayInfo.UnobstructedVerticalRays} | " +
                           $"30-Degree Rays - Obstructed: {rayInfo.ObstructedThirtyDegreeRays}, Unobstructed: {rayInfo.UnobstructedThirtyDegreeRays} | " +
                           $"Vertical Rays - Distances: [{string.Join(", ", RayInfoBase.VerticalObstructionDistances.Select(d => d.ToString("F2")))}] | " +
                           $"30-Degree Rays - Distances: [{string.Join(", ", RayInfoBase.ThirtyDegreeObstructionDistances.Select(d => d.ToString("F2")))}] | " +
                           $"Average Vertical Obstruction Distance: {rayInfo.AverageObstructionDistanceVertical:F2} | " +
                           $"Average 30-Degree Obstruction Distance: {rayInfo.AverageObstructionDistanceThirtyDegree:F2} | " +
                           $"Shelter Status: {(isFaceSheltered ? "Sheltered" : "Not Sheltered")}\n\n";


                // Skip faces with area < 20% of the total surface area when determining if the entire element is sheltered
                if (face.Area < 0.2 * totalSurfaceArea)
                {
                    message += $"Face {planarFace.Reference.ConvertToStableRepresentation(doc)} is smaller than 20% of total element area and will be ignored in element-level shelter check.\n\n";
                    continue;
                }

                // Update total rays for element-level sheltering
                totalObstructedRays += rayInfo.FaceObstructedRays;
                totalUnobstructedRays += rayInfo.FaceUnobstructedRays;
            }



            // Compute the overall average vertical obstruction distance
            if (RayInfoBase.VerticalObstructionDistances.Any())
            {
                RayInfoBase.OverallAverageVerticalObstructionDistance =
                    RayInfoBase.VerticalObstructionDistances.Average();
            }
            else
            {
                RayInfoBase.OverallAverageVerticalObstructionDistance = 0.0;
            }

            // Add the overall average to the message
            message += $"Overall Average Vertical Obstruction Distance: {RayInfoBase.OverallAverageVerticalObstructionDistance:F2} meters\n\n";



        }


        private static Options GetDetailedGeometryOptions()
        {
            Options options = new Options();
            options.DetailLevel = ViewDetailLevel.Fine;  // Use highest level of detail
            options.ComputeReferences = true;            // Ensure references are computed
            options.IncludeNonVisibleObjects = true;     // Include non-visible objects
            return options;
        }




        private static HashSet<int> processedFaces = new HashSet<int>();

        private static void VisualizeRayWithTransaction(
            Document doc,
            XYZ origin,
            XYZ direction,
            double length,
            Color color,
            bool isActiveTransaction = false)
        {
            XYZ endPoint = origin + (direction.Normalize() * length);

            if (!isActiveTransaction)
            {
                // Start a transaction only if not already active
                using (Transaction transaction = new Transaction(doc, "Visualize Ray"))
                {
                    transaction.Start();

                    Line rayLine = Line.CreateBound(origin, endPoint);
                    DirectShape rayShape = CreateRayShape(doc, rayLine, color);

                    transaction.Commit();
                }
            }
            else
            {
                // Assume active transaction, just create the ray visualization
                Line rayLine = Line.CreateBound(origin, endPoint);
                DirectShape rayShape = CreateRayShape(doc, rayLine, color);
            }
        }

        private static DirectShape CreateRayShape(Document doc, Line line, Color color)
        {
            DirectShape rayShape = DirectShape.CreateElement(doc, new ElementId(BuiltInCategory.OST_GenericModel));
            rayShape.SetShape(new GeometryObject[] { line });

            OverrideGraphicSettings overrideSettings = new OverrideGraphicSettings();
            overrideSettings.SetProjectionLineColor(color);
            overrideSettings.SetProjectionLineWeight(5);

            doc.ActiveView.SetElementOverrides(rayShape.Id, overrideSettings);

            return rayShape;
        }

        private static bool CheckShelterUsingPerformRayIntersection(
            Face face,
            PlanarFace planarFace,
            Document doc,
            View3D active3DView,
            Element selectedElement,
            out int obstructedRays,
            out int unobstructedRays,
            out List<string> obstructionDetails,
            out List<XYZ> allIntersectionPoints,
            List<VerticalRayInfo> verticalRayInfos,
            List<ThirtyDegreeRayInfo> angledRayInfos,
            RayTypeOption rayTypeOption,
            ref int currentRayId) // Add reference parameter for RayId
        {
            BoundingBoxUV bbox = face.GetBoundingBox();
            UV min = bbox.Min;
            UV max = bbox.Max;

            double faceWidth = max.U - min.U;
            double faceHeight = max.V - min.V;

            double minGridSize = 0.1;
            int numGridsX = Math.Max(1, (int)Math.Floor(faceWidth / minGridSize));
            int numGridsY = Math.Max(1, (int)Math.Floor(faceHeight / minGridSize));

            double gridWidth = faceWidth / numGridsX;
            double gridHeight = faceHeight / numGridsY;

            obstructedRays = 0;
            unobstructedRays = 0;
            int obstructedVerticalRays = 0, unobstructedVerticalRays = 0;
            int obstructedThirtyDegreeRays = 0, unobstructedThirtyDegreeRays = 0;

            allIntersectionPoints = new List<XYZ>();
            obstructionDetails = new List<string>();

            // Loop through grid points on the face
            for (double u = min.U; u < max.U; u += gridWidth)
            {
                for (double v = min.V; v < max.V; v += gridHeight)
                {
                    double u_centroid = u + gridWidth / 2;
                    double v_centroid = v + gridHeight / 2;

                    if (u_centroid >= min.U && u_centroid <= max.U && v_centroid >= min.V && v_centroid <= max.V)
                    {
                        UV centroidUV = new UV(u_centroid, v_centroid);
                        XYZ centroid = face.Evaluate(centroidUV);

                        // Process rays based on face orientation
                        if (Math.Abs(planarFace.FaceNormal.Z) > 0.9)  // Horizontal face
                        {
                            // Cast vertical rays
                            if (rayTypeOption == RayTypeOption.Vertical || rayTypeOption == RayTypeOption.Both)
                            {
                                XYZ verticalRayDirection = new XYZ(0, 0, 1); // Cast ray upwards
                                bool isObstructed = PerformRayIntersectionCheck(
                                    centroid,
                                    verticalRayDirection,
                                    doc,
                                    active3DView,
                                    selectedElement,
                                    null,
                                    out List<string> rayObstructionDetails,
                                    out double obstructionDistance,
                                    out List<XYZ> intersectionPoints,
                                    out Reference intersectedReference,
                                    out Element intersectedElement,
                                    verticalRayInfos.Cast<RayInfoBase>().ToList(),
                                    RayType.Vertical
                                );

                                // Display obstructionDistance in a TaskDialog
                                string dialogMessage = $"Ray from Point: {centroid} " +
                                                       $"\nDirection: {verticalRayDirection} " +
                                                       $"\nObstruction Distance: {obstructionDistance:F2} meters.";

                                // TaskDialog.Show("Obstruction Distance", dialogMessage);


                                var rayInfo = new VerticalRayInfo
                                {
                                    RayId = $"{currentRayId}-{(isObstructed ? "Obstructed" : "Unobstructed")}-Vertical"
                                };
                                currentRayId++; // Increment the counter
                                verticalRayInfos.Add(rayInfo);

                                //// Add to global lists
                                RayInfoBase.AllRayIds.Add(rayInfo.RayId);

                                //// Display RayId information
                                //TaskDialog.Show("Ray Information",
                                //    $"Ray ID: {rayInfo.RayId}\n" +
                                //    $"Obstruction Distance: {rayInfo.Obstructionistance:F2} m\n" +
                                //    $"Is Obstructed: {rayInfo.IsObstructed}");


                                // Update ray counts for vertical rays
                                if (isObstructed)
                                {
                                    obstructedRays++;
                                    obstructedVerticalRays++;
                                    RayInfoBase.VerticalObstructionDistances.Add(obstructionDistance); // Add distance to global list
                                }
                                else
                                {
                                    unobstructedRays++;
                                    unobstructedVerticalRays++;
                                }

                                obstructionDetails.AddRange(rayObstructionDetails);
                                allIntersectionPoints.AddRange(intersectionPoints);
                            }
                        }
                        else if (Math.Abs(planarFace.FaceNormal.Z) < 0.1)  // Vertical face
                        {
                            // Cast 30-degree rays
                            if (rayTypeOption == RayTypeOption.ThirtyDegree || rayTypeOption == RayTypeOption.Both)
                            {
                                XYZ angledRayDirection = DetermineRayDirection(planarFace, out string rayType);
                                bool isObstructed = PerformRayIntersectionCheck(
                                    centroid,
                                    angledRayDirection,
                                    doc,
                                    active3DView,
                                    selectedElement,
                                    null,
                                    out List<string> rayObstructionDetails,
                                    out double obstructionDistance,
                                    out List<XYZ> intersectionPoints,
                                    out Reference intersectedReference,
                                    out Element intersectedElement,
                                    angledRayInfos.Cast<RayInfoBase>().ToList(),
                                    RayType.ThirtyDegree
                                );

                                // Display obstructionDistance in a TaskDialog
                                string dialogMessage = $"30-Degree Ray from Point: {centroid} " +
                                                       $"\nDirection: {angledRayDirection} " +
                                                       $"\nObstruction Distance: {obstructionDistance:F2} meters.";

                                // TaskDialog.Show("Obstruction Distance (30-Degree Ray)", dialogMessage);


                                // Assign RayId and update info for 30-degree rays
                                var rayInfo = new ThirtyDegreeRayInfo
                                {
                                    RayId = $"{currentRayId}-{(isObstructed ? "Obstructed" : "Unobstructed")}-30-Degree"
                                };
                                currentRayId++; // Increment the counter
                                angledRayInfos.Add(rayInfo);

                                //// Add to global lists
                                RayInfoBase.AllRayIds.Add(rayInfo.RayId);

                                //TaskDialog.Show("Ray Information",
                                //    $"Ray ID: {rayInfo.RayId}\n" +
                                //    $"Obstruction Distance: {rayInfo.Obstructionistance:F2} m\n" +
                                //    $"Is Obstructed: {rayInfo.IsObstructed}");

                                // Update ray counts for 30-degree rays
                                if (isObstructed)
                                {
                                    obstructedRays++;
                                    obstructedThirtyDegreeRays++;
                                    RayInfoBase.ThirtyDegreeObstructionDistances.Add(obstructionDistance); // Add distance to global list
                                }
                                else
                                {
                                    unobstructedRays++;
                                    unobstructedThirtyDegreeRays++;
                                }

                                obstructionDetails.AddRange(rayObstructionDetails);
                                allIntersectionPoints.AddRange(intersectionPoints);
                            }
                        }
                    }
                }
            }

            // Return true if the face is fully obstructed
            return obstructedRays > 0 && unobstructedRays == 0;
        }


        private static XYZ DetermineRayDirection(PlanarFace planarFace, out string rayType)
        {
            if (Math.Abs(planarFace.FaceNormal.Z) > 0.9)  // Horizontal face
            {
                rayType = "Vertical";
                return new XYZ(0, 0, 1);  // Cast ray upwards
            }
            else  // Vertical face
            {
                rayType = "ThirtyDegree";
                XYZ faceNormal = planarFace.FaceNormal;
                XYZ rayAngleOffset = new XYZ(0, 0, Math.Tan(Math.PI / 6)); // 30 degrees in radians
                return (faceNormal + rayAngleOffset).Normalize();  // Cast ray at 30 degrees
            }
        }




        private static HashSet<XYZ> processedRayDirections = new HashSet<XYZ>();

        private static void CreateRayVisualization(Document doc, XYZ rayOrigin, XYZ rayDirection, bool isObstructed, RayType rayType)
        {
            double rayLength = 100.0; // Define how far the ray should extend

            // Set the line color based on the ray type and obstruction status
            Color lineColor;
            if (isObstructed)
            {
                // Red for obstructed rays
                lineColor = new Color(255, 0, 0);
            }
            else
            {
                // Different colors for unobstructed rays depending on ray type
                if (rayType == RayType.Vertical)
                {
                    lineColor = new Color(0, 255, 0); // Green for unobstructed vertical rays
                }
                else if (rayType == RayType.ThirtyDegree)
                {
                    lineColor = new Color(0, 0, 255); // Blue for unobstructed 30-degree rays
                }
                else
                {
                    lineColor = new Color(255, 255, 255); // White for any undefined ray type (fallback)
                }
            }

            // Create a line representing the ray
            Line rayLine = Line.CreateBound(rayOrigin, rayOrigin + rayDirection * rayLength);

            // Create a DirectShape to visualize the ray
            DirectShape rayShape = DirectShape.CreateElement(doc, new ElementId(BuiltInCategory.OST_GenericModel));
            string rayName = $"{rayType} Ray - {(isObstructed ? "Obstructed" : "Unobstructed")}";
            rayShape.Name = rayName;  // Ensure no prohibited characters are used

            // Set the line color
            OverrideGraphicSettings settings = new OverrideGraphicSettings();
            settings.SetProjectionLineColor(lineColor);
            doc.ActiveView.SetElementOverrides(rayShape.Id, settings);
        }


        private static void CreatePointVisualization(Document doc, XYZ point, RayType rayType)
        {
            // Define the length of the short crossing lines
            double lineLength = 0.1; // Length of the lines (adjust to make the point more visible)
            double halfLength = lineLength / 2; // Half the length for easy calculation of endpoints

            // Choose color based on the ray type
            Color pointColor;
            if (rayType == RayType.Vertical)
            {
                pointColor = new Color(0, 255, 0); // Green for vertical rays
            }
            else if (rayType == RayType.ThirtyDegree)
            {
                pointColor = new Color(0, 0, 255); // Blue for 30-degree rays
            }
            else
            {
                pointColor = new Color(255, 255, 255); // Default to white for any undefined ray type
            }

            // Create two crossing lines: one along the X-axis, one along the Y-axis
            List<Curve> crossingLines = new List<Curve>();

            // Line along the X-axis
            XYZ xStart = new XYZ(point.X - halfLength, point.Y, point.Z);
            XYZ xEnd = new XYZ(point.X + halfLength, point.Y, point.Z);
            crossingLines.Add(Line.CreateBound(xStart, xEnd));

            // Line along the Y-axis
            XYZ yStart = new XYZ(point.X, point.Y - halfLength, point.Z);
            XYZ yEnd = new XYZ(point.X, point.Y + halfLength, point.Z);
            crossingLines.Add(Line.CreateBound(yStart, yEnd));

            // Line along the Z-axis for better visualization in 3D
            XYZ zStart = new XYZ(point.X, point.Y, point.Z - halfLength);
            XYZ zEnd = new XYZ(point.X, point.Y, point.Z + halfLength);
            crossingLines.Add(Line.CreateBound(zStart, zEnd));

            // Create the DirectShape element to hold the crossing lines
            DirectShape shape = DirectShape.CreateElement(doc, new ElementId(BuiltInCategory.OST_GenericModel));

            // Cast the list of curves to an array of GeometryObject
            shape.SetShape(crossingLines.Cast<GeometryObject>().ToArray());
            shape.Name = "Intersection Point Marker";

            // Apply color and line thickness using OverrideGraphicSettings
            OverrideGraphicSettings settings = new OverrideGraphicSettings();
            settings.SetProjectionLineColor(pointColor);

            // Set line weight for thicker lines (value between 1 and 16, 16 is the thickest)
            settings.SetProjectionLineWeight(8); // Adjust this for thickness

            // Apply the settings to the created shape in the active view
            doc.ActiveView.SetElementOverrides(shape.Id, settings);
        }



        private static ElementMulticategoryFilter GetModelCategoryFilter(Document doc)
        {
            // Get all the model categories in the document dynamically
            var modelCategories = doc.Settings.Categories
                .Cast<Category>()
                .Where(c => c.CategoryType == CategoryType.Model && c.CanAddSubcategory) // Filter only model categories
                .Select(c => (BuiltInCategory)c.Id.IntegerValue) // Convert Category to BuiltInCategory
                .ToList();

            // Create an ElementMulticategoryFilter using the model categories
            return new ElementMulticategoryFilter(modelCategories);
        }

        private static bool PerformRayIntersectionCheck(
            XYZ rayOrigin,
            XYZ rayDirection,
            Document doc,
            View3D active3DView,
            Element selectedElement,
            Reference selectedElementFaceReference,
            out List<string> obstructionDetails,
            out double obstructionDistance,
            out List<XYZ> intersectionPoints,
            out Reference closestFaceRef,
            out Element closestIntersectedElement,
            List<RayInfoBase> rayInfos,
            RayType rayType)
        {
            // Debug: Validate input parameters
            //TaskDialog.Show("Debug: Input Validation",
            //    $"rayOrigin: {rayOrigin}\n" +
            //    $"rayDirection: {rayDirection}\n" +
            //    $"Document: {(doc != null ? "Valid" : "Null")}\n" +
            //    $"Active 3D View: {(active3DView != null ? "Valid" : "Null")}\n" +
            //    $"Selected Element: {(selectedElement != null ? selectedElement.Id.ToString() : "Null")}\n" +
            //    $"Selected Face Reference: {(selectedElementFaceReference != null ? selectedElementFaceReference.ConvertToStableRepresentation(doc) : "Null")}");


            // Initialize output parameters
            obstructionDetails = new List<string>();
            obstructionDistance = double.MaxValue;
            intersectionPoints = new List<XYZ>();
            closestFaceRef = null;
            closestIntersectedElement = null;

            // Use the updated GetModelCategoryFilter method
            ElementMulticategoryFilter modelCategoryFilter = GetModelCategoryFilter(doc);

            // Initialize the ReferenceIntersector
            ReferenceIntersector intersector = new ReferenceIntersector(modelCategoryFilter, FindReferenceTarget.Face, active3DView);

            // Cast ray and find all intersections
            IList<ReferenceWithContext> intersectedRefs = intersector.Find(rayOrigin, rayDirection);

            // Initialize variables for closest intersection tracking
            XYZ closestIntersectionPoint = null;

            // Iterate over all intersection results
            foreach (var refContext in intersectedRefs)
            {
                Reference faceRef = refContext.GetReference();
                Element hitElement = doc.GetElement(faceRef.ElementId);

                // Skip self-intersections
                if (hitElement == null || hitElement.Id == selectedElement.Id)
                    continue;

                double currentObstructionDistance = Math.Round(refContext.Proximity * 0.3048, 2); // Convert feet to meters
                if (currentObstructionDistance < obstructionDistance)
                {
                    obstructionDistance = currentObstructionDistance;
                    closestIntersectionPoint = rayOrigin + (rayDirection * refContext.Proximity);
                    closestFaceRef = faceRef;
                    closestIntersectedElement = hitElement;

                    obstructionDetails.Clear();
                    obstructionDetails.Add($"Obstructed by ElementId {hitElement.Id.IntegerValue}");
                }
            }

            if (closestIntersectionPoint != null && closestFaceRef != null && closestIntersectedElement != null)
            {
                intersectionPoints.Add(closestIntersectionPoint);

                RayInfoBase rayInfo = new RayInfoBase
                {
                    Origin = rayOrigin,
                    Direction = rayDirection,
                    IntersectionPoints = new List<XYZ>(intersectionPoints),
                    IntersectedFaceReference = closestFaceRef,
                    IntersectedElement = closestIntersectedElement,
                    SelectedElementFaceReference = selectedElementFaceReference,
                    SelectedElementId = selectedElement.Id,
                    IsObstructed = true,
                    ObstructionDistance = obstructionDistance,
                };
                rayInfos.Add(rayInfo);

                return true; // Intersection found
            }


            return false; // No intersection found
        }



        private static void ProcessGeometryWithInstances(
    GeometryElement geometryElement,
    List<Solid> solids,
    List<GeometryInstance> instances)
        {
            foreach (GeometryObject geomObj in geometryElement)
            {
                if (geomObj is Solid solid && solid.Faces.Size > 0)
                {
                    solids.Add(solid);
                }
                else if (geomObj is GeometryInstance geomInstance)
                {
                    instances.Add(geomInstance);
                    GeometryElement nestedGeometry = geomInstance.GetInstanceGeometry();
                    ProcessGeometryWithInstances(nestedGeometry, solids, instances);
                }
            }
        }

        private static Reference ResolveNestedReference(Reference reference, Document doc, out Element nestedElement)
        {
            nestedElement = null;

            // Get the top-level element from the reference
            Element element = doc.GetElement(reference.ElementId);

            if (element is FamilyInstance || element is RevitLinkInstance)
            {
                // Get the geometry object from the reference
                GeometryObject geometryObject = element.GetGeometryObjectFromReference(reference);

                // If the geometry object is a GeometryInstance, traverse it
                if (geometryObject is GeometryInstance geometryInstance)
                {
                    GeometryElement instanceGeometry = geometryInstance.GetInstanceGeometry();
                    foreach (GeometryObject geomObj in instanceGeometry)
                    {
                        if (geomObj is Solid solid)
                        {
                            foreach (Face face in solid.Faces)
                            {
                                if (face.Reference.ConvertToStableRepresentation(doc) == reference.ConvertToStableRepresentation(doc))
                                {
                                    nestedElement = element;
                                    return face.Reference; // Return the resolved reference
                                }
                            }
                        }
                    }
                }
            }

            // If not a nested reference, return the original reference
            nestedElement = element;
            return reference;
        }



        private static bool ValidateIntersectedFaceReference(Document doc, Reference faceReference, out string validationMessage)
        {
            validationMessage = string.Empty;

            // Get the element containing the face
            Element element = doc.GetElement(faceReference.ElementId);
            if (element == null)
            {
                validationMessage = "Invalid reference: Element not found.";
                return false;
            }

            // Get the geometry object using the face reference
            GeometryObject geomObject = element.GetGeometryObjectFromReference(faceReference);
            if (geomObject == null)
            {
                validationMessage = $"Invalid reference: No geometry object found for Element ID {element.Id.IntegerValue}.";
                return false;
            }

            // Check if the geometry object is a face
            if (geomObject is Face)
            {
                validationMessage = $"Valid Face Reference: Element ID = {element.Id.IntegerValue}, Face Reference = {faceReference.ConvertToStableRepresentation(doc)}.";
                return true;
            }
            else
            {
                validationMessage = $"Invalid Face Reference: Element ID = {element.Id.IntegerValue}, Reference Type = {geomObject.GetType().Name}.";
                return false;
            }
        }

        public static double CalculateGroundDistance(Element element, Document doc)
        {
            // Get the bounding box of the selected element
            BoundingBoxXYZ elementBoundingBox = element.get_BoundingBox(null);
            if (elementBoundingBox == null)
            {
                throw new InvalidOperationException("Could not determine the bounding box of the selected element.");
            }

            // The Z coordinate of the lowest pint of the element
            double elementMinZ = elementBoundingBox.Min.Z;

            // Initialize a variable to track the minimum distance
            double minDistanceToGround = double.MaxValue;

            // Get all TopographySurface elements in the document
            var topoSurfaces = new FilteredElementCollector(doc)
                .OfClass(typeof(TopographySurface))
                .Cast<TopographySurface>();

            foreach (var topoSurface in topoSurfaces)
            {
                // Get the bounding box of the TopographySurface
                BoundingBoxXYZ topoBoundingBox = topoSurface.get_BoundingBox(null);
                if (topoBoundingBox == null)
                    continue;

                // The Z coordinate of the highest point of the TopographySurface
                double topoMaxZ = topoBoundingBox.Max.Z;

                // Calculate the vertical distance from the element's lowest point to the TopographySurface's highest point
                double distanceToGround = elementMinZ - topoMaxZ;

                // If this is the smallest distance we've encountered, store it
                if (distanceToGround < minDistanceToGround)
                {
                    minDistanceToGround = distanceToGround;
                }
            }

            // If no TopographySurface is found, we assume the distance is infinite or return a default value
            if (minDistanceToGround == double.MaxValue)
            {
                MessageBox.Show("No TopographySurface found in the document. Returning a default distance.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return double.PositiveInfinity; // or return a default value, e.g., 0.0 or a specific distance.
            }

            // Convert distance from feet to meters
            minDistanceToGround *= 0.3048;

            // Ensure the distance is positive (if the element is above the ground)
            return Math.Max(0, minDistanceToGround);
        }


        public static double CalculateShelterDistance(Element element, Document doc, List<VerticalRayInfo> verticalRayInfos)
        {
            // Ensure there are valid vertical ray infos
            if (verticalRayInfos == null || verticalRayInfos.Count == 0)
            {
                // Log or handle the case where no vertical ray info is available
                TaskDialog.Show("Error", "No valid vertical ray information available to calculate shelter distance.");
                return 0.0; // Return 0 or an appropriate default value
            }

            // Calculate the overall average vertical obstruction distance if not already computed
            if (RayInfoBase.VerticalObstructionDistances.Any())
            {
                RayInfoBase.OverallAverageVerticalObstructionDistance = RayInfoBase.VerticalObstructionDistances.Average();
            }
            else
            {
                RayInfoBase.OverallAverageVerticalObstructionDistance = 0.0;
            }

            // Return the calculated overall average vertical obstruction distance
            return RayInfoBase.OverallAverageVerticalObstructionDistance;
        }



        private static List<Face> GetMajorOpposingFacesByArea(Element element)
        {
            if (element == null) return null;

            Options options = new Options
            {
                DetailLevel = ViewDetailLevel.Fine,
                ComputeReferences = true,
                IncludeNonVisibleObjects = true
            };

            GeometryElement geometryElement = element.get_Geometry(options);

            List<Face> allFaces = new List<Face>();

            // Collect faces and meshes from all geometry objects
            CollectFacesAndMeshes(geometryElement, allFaces);

            if (allFaces.Count < 2)
            {
                TaskDialog.Show("Error", "Not enough faces detected on the element, including extended geometry.");
                return null;
            }

            // Sort faces by area in descending order
            allFaces.Sort((a, b) => b.Area.CompareTo(a.Area));

            // Return the two largest faces
            return new List<Face> { allFaces[0], allFaces[1] };
        }

        private static void CollectFacesAndMeshes(GeometryElement geomElement, List<Face> allFaces, Transform parentTransform = null)
        {
            Transform currentTransform = parentTransform ?? Transform.Identity;

            foreach (GeometryObject geomObj in geomElement)
            {
                switch (geomObj)
                {
                    case Solid solid when solid.Faces.Size > 0:
                        // Add all faces from solid geometry
                        foreach (Face face in solid.Faces)
                        {
                            allFaces.Add(face);
                        }
                        break;

                    case Mesh mesh:
                        // Approximate faces for meshes
                        //TaskDialog.Show("Mesh Info", $"Mesh found with {mesh.Vertices.Count} vertices.");
                        break;

                    case GeometryInstance instance:
                        // Recursively process nested GeometryInstances
                        GeometryElement nestedGeometry = instance.GetInstanceGeometry();
                        CollectFacesAndMeshes(nestedGeometry, allFaces, instance.Transform);
                        break;

                    case Curve curve:
                        //TaskDialog.Show("Curve Info", $"Curve detected: Start {curve.GetEndPoint(0)}, End {curve.GetEndPoint(1)}");
                        break;

                    default:
                        // Log unsupported geometry types
                        //TaskDialog.Show("Unsupported Geometry", $"Unhandled geometry type: {geomObj.GetType()}");
                        break;
                }
            }
        }


        private static string GetClosestAxis(XYZ normal)
        {
            // Define unit vectors for the axes
            XYZ[] axes = new XYZ[]
            {
                new XYZ(0, 0, 1),  // Z-axis
                new XYZ(0, 0, -1), // -Z-axis
                new XYZ(1, 0, 0),  // X-axis
                new XYZ(-1, 0, 0), // -X-axis
                new XYZ(0, 1, 0),  // Y-axis
                new XYZ(0, -1, 0)  // -Y-axis
            };

            string[] axisNames = new string[]
            {
                 "Z", "-Z", "X", "-X", "Y", "-Y"
            };

            double maxDot = double.MinValue;
            string closestAxis = "";

            // Compare the dot product with each axis
            for (int i = 0; i < axes.Length; i++)
            {
                double dotProduct = normal.DotProduct(axes[i]);
                if (Math.Abs(dotProduct) > maxDot)
                {
                    maxDot = Math.Abs(dotProduct);
                    closestAxis = axisNames[i];
                }
            }

            return closestAxis;
        }

        public static string DetermineElementOrientation(Element element)
        {
            if (element == null)
            {
                TaskDialog.Show("Error", "No element selected.");
                return "Invalid";
            }

            // Step 1: Get the two major opposing faces
            var majorFaces = GetMajorOpposingFacesByArea(element);
            if (majorFaces == null || majorFaces.Count != 2)
            {
                TaskDialog.Show("Error", "Unable to determine major opposing faces.");
                return "Invalid";
            }

            // Step 2: Compute normals at the center of each face
            XYZ normal1 = GetFaceNormalAtCenter(majorFaces[0]);
            XYZ normal2 = GetFaceNormalAtCenter(majorFaces[1]);

            // Step 3: Determine which axis the normals are closest to
            string axis1 = GetClosestAxis(normal1);
            string axis2 = GetClosestAxis(normal2);

            // Step 4: Classify the orientation based on the normals
            if ((axis1 == "Z" || axis1 == "-Z") && (axis2 == "Z" || axis2 == "-Z"))
            {
                //TaskDialog.Show("Feedback", "The element is horizontal (major faces align with Z-axis).");
                return "Horizontal";
            }
            else if ((axis1.StartsWith("X") || axis1.StartsWith("Y")) &&
                     (axis2.StartsWith("X") || axis2.StartsWith("Y")))
            {
                //TaskDialog.Show("Feedback", "The element is vertical (major faces align with X or Y axes).");
                return "Vertical";
            }
            else
            {
                //TaskDialog.Show("Feedback", "The element is inclined.");
                return "Inclined";
            }
        }

        private static XYZ GetFaceNormalAtCenter(Face face)
        {
            // Get the bounding box of the face
            BoundingBoxUV bbox = face.GetBoundingBox();
            UV centerUV = new UV(
                (bbox.Min.U + bbox.Max.U) / 2, // Midpoint U
                (bbox.Min.V + bbox.Max.V) / 2  // Midpoint V
            );

            // Compute the normal at the center of the face
            XYZ normal = face.ComputeNormal(centerUV);
            return normal.Normalize();
        }



        public static double CalculateOverhangLength(Element element, Document doc, List<VerticalRayInfo> verticalRayInfos)
        {
            StringBuilder debugInfoBuilder = new StringBuilder();


            // Step 1: Check if the element and document are valid
            if (element == null || doc == null)
            {
                debugInfoBuilder.AppendLine("Error: Element or Document is null.");
                return 0;
            }

            // Step 2: Check if there are any valid vertical rays
            if (verticalRayInfos == null || verticalRayInfos.Count == 0)
            {
                debugInfoBuilder.AppendLine("Warning: No valid vertical rays found.");
                return 0;
            }

            // Step 3: Calculate the shortest distance and corresponding intersection points for each ray
            var rayIntersectionDetails = verticalRayInfos
                .Where(ray => ray.IsObstructed && ray.IntersectionPoints.Any() && ray.IntersectedFaceReference != null)
                .Select(ray => new
                {
                    RayOrigin = ray.Origin,  // Unique identifier for the ray
                    ShortestIntersectionPoint = ray.IntersectionPoints.OrderBy(point => (point - ray.Origin).GetLength()).FirstOrDefault(), // Closest intersection point for each ray
                    ShortestDistance = ray.IntersectionPoints.Min(point => (point - ray.Origin).GetLength()), // Minimum distance for each ray
                    IntersectedFaceReference = ray.IntersectedFaceReference, // Face reference for each ray
                    IntersectedElement = ray.IntersectedElement // Store the intersected element for each ray
                })
                .ToList();

            // Log the shortest distances and intersection points for debugging
            foreach (var rayDetails in rayIntersectionDetails)
            {
                debugInfoBuilder.AppendLine($"Ray from Origin: {rayDetails.RayOrigin} has a closest intersection point at: {rayDetails.ShortestIntersectionPoint} with a shortest distance of: {rayDetails.ShortestDistance} meters.");
            }

            // Step 4: Identify the global closest intersection point among all rays
            var closestRayInfo = rayIntersectionDetails.OrderBy(ray => ray.ShortestDistance).FirstOrDefault();
            if (closestRayInfo == null)
            {
                debugInfoBuilder.AppendLine("Warning: No valid closest ray found.");
                return 0;
            }

            // Step 5: Extract the details of the global closest intersection point
            XYZ closestIntersectionPoint = closestRayInfo.ShortestIntersectionPoint;
            Reference closestIntersectedFaceReference = closestRayInfo.IntersectedFaceReference;
            Element closestIntersectedElement = closestRayInfo.IntersectedElement;

            // Step 6: Log debug information to ensure the closest intersection point and face are correctly captured
            string debugInfo = $"Closest Intersection Point: {closestIntersectionPoint}\n" +
                               $"Intersected Element ID: {closestIntersectedElement?.Id}\n" +
                               $"Face Reference: {closestIntersectedFaceReference?.ConvertToStableRepresentation(doc) ?? "N/A"}\n";

            // Check if the intersected element and intersection point are valid
            if (closestIntersectedElement == null)
            {
                debugInfo += "Error: No intersected element found.\n";
                //uncomment this to see more info
                //TaskDialog.Show("Debugging Information", debugInfoBuilder.ToString());
                return 0; // Exit if no intersected element
            }

            if (closestIntersectionPoint == null)
            {
                debugInfoBuilder.AppendLine("Error: Closest intersection point is null.");
                return 0;
            }

            // Step 7: Retrieve and validate geometry details of the intersected element
            debugInfo += $"Intersected Element ID: {closestIntersectedElement.Id.IntegerValue}\n";


            // Retrieve geometry of the intersected element
            Options geomOptions = new Options
            {
                ComputeReferences = true, // Ensure references are computed
                IncludeNonVisibleObjects = true,
                DetailLevel = ViewDetailLevel.Fine
            };
            GeometryElement geomElement = closestIntersectedElement.get_Geometry(geomOptions);


            // Check if geomElement is not null and analyze the element type
            if (geomElement != null)
            {
                bool isSolid = geomElement.Any(g => g is Solid && (g as Solid).Faces.Size > 0);
                bool isGeometryInstance = geomElement.Any(g => g is GeometryInstance);

                debugInfo += isSolid ? "Element Type: Solid(1142)\n" : isGeometryInstance ? "Element Type: Geometry Instance\n" : "Element Type: Other\n";
            }
            else
            {
                debugInfo += "No geometric data available for this element.\n";
                debugInfoBuilder.AppendLine(debugInfo);
                return 0; // Exit if no geometric data is available
            }

            // Get the GeometryObject from the intersected reference
            GeometryObject geomObject = closestIntersectedElement.GetGeometryObjectFromReference(closestIntersectedFaceReference);
            Face intersectedFace = null;

            if (geomObject is Face initialIntersectedFace)
            {
                intersectedFace = initialIntersectedFace;
                debugInfo += $"Intersected reference is a valid Face with Area: {initialIntersectedFace.Area}\n";

                // Log edges of the face
                foreach (Edge outerEdge in initialIntersectedFace.EdgeLoops.Cast<EdgeArray>().SelectMany(loop => loop.Cast<Edge>()))
                {
                    debugInfo += $"Edge: {outerEdge.AsCurve().GetEndPoint(0)} to {outerEdge.AsCurve().GetEndPoint(1)}. Length: {outerEdge.AsCurve().Length}\n";
                }

                // Log vertices of the face
                foreach (EdgeArray loop in initialIntersectedFace.EdgeLoops)
                {
                    foreach (Edge loopEdge in loop)
                    {
                        foreach (XYZ vertex in loopEdge.Tessellate())
                        {
                            debugInfo += $"Vertex: {vertex}\n";
                        }
                    }
                }
            }
            else if (geomObject is GeometryElement nestedGeomElement)
            {
                // Inspect nested GeometryElements
                debugInfo += "Intersected reference is a GeometryElement. Inspecting nested geometry...\n";

                int faceCount = 0;  // Counter to keep track of the number of faces
                foreach (GeometryObject nestedGeom in nestedGeomElement)
                {
                    // Check for Face elements
                    if (nestedGeom is Face nestedFace)
                    {
                        faceCount++;  // Increment face counter
                        debugInfo += $"Found nested face with area: {nestedFace.Area}\n";

                        // Log edges of the face
                        foreach (Edge nestedEdge in nestedFace.EdgeLoops.Cast<EdgeArray>().SelectMany(loop => loop.Cast<Edge>()))
                        {
                            debugInfo += $"Edge: {nestedEdge.AsCurve().GetEndPoint(0)} to {nestedEdge.AsCurve().GetEndPoint(1)}. Length: {nestedEdge.AsCurve().Length}\n";
                        }

                        // Log vertices of the face
                        foreach (EdgeArray loop in nestedFace.EdgeLoops)
                        {
                            foreach (Edge nestedLoopEdge in loop)
                            {
                                foreach (XYZ vertex in nestedLoopEdge.Tessellate())
                                {
                                    debugInfo += $"Vertex: {vertex}\n";
                                }
                            }
                        }
                    }

                    // Check for Solid elements
                    else if (nestedGeom is Solid nestedSolid)
                    {
                        debugInfo += "Found a Solid element. Inspecting faces...\n";
                        foreach (Face solidFace in nestedSolid.Faces)
                        {
                            faceCount++;  // Increment face counter
                            debugInfo += $"Found face within Solid. Face area: {solidFace.Area}\n";

                            // Log edges of the solid face
                            foreach (Edge solidEdge in solidFace.EdgeLoops.Cast<EdgeArray>().SelectMany(loop => loop.Cast<Edge>()))
                            {
                                debugInfo += $"Edge: {solidEdge.AsCurve().GetEndPoint(0)} to {solidEdge.AsCurve().GetEndPoint(1)}. Length: {solidEdge.AsCurve().Length}\n";
                            }

                            // Log vertices of the solid face
                            foreach (EdgeArray loop in solidFace.EdgeLoops)
                            {
                                foreach (Edge solidLoopEdge in loop)
                                {
                                    foreach (XYZ vertex in solidLoopEdge.Tessellate())
                                    {
                                        debugInfo += $"Vertex: {vertex}\n";
                                    }
                                }
                            }
                        }
                    }

                    // Check for GeometryInstance elements
                    else if (nestedGeom is GeometryInstance deeperGeomInstance)
                    {
                        debugInfo += "Found nested GeometryInstance. Inspecting its nested geometry...\n";
                        GeometryElement deeperInstanceGeometry = deeperGeomInstance.GetInstanceGeometry();
                        foreach (GeometryObject deeperGeom in deeperInstanceGeometry)
                        {
                            if (deeperGeom is Solid deeperNestedSolid)
                            {
                                debugInfo += "Deeper nested geometry is a Solid. Inspecting faces...\n";
                                foreach (Face nestedSolidFace in deeperNestedSolid.Faces)
                                {
                                    faceCount++;  // Increment face counter
                                    debugInfo += $"Found face within Solid. Face area: {nestedSolidFace.Area}\n";

                                    foreach (Edge solidEdge in nestedSolidFace.EdgeLoops.Cast<EdgeArray>().SelectMany(loop => loop.Cast<Edge>()))
                                    {
                                        debugInfo += $"Edge: {solidEdge.AsCurve().GetEndPoint(0)} to {solidEdge.AsCurve().GetEndPoint(1)}. Length: {solidEdge.AsCurve().Length}\n";
                                    }

                                    foreach (EdgeArray loop in nestedSolidFace.EdgeLoops)
                                    {
                                        foreach (Edge solidLoopEdge in loop)
                                        {
                                            foreach (XYZ vertex in solidLoopEdge.Tessellate())
                                            {
                                                debugInfo += $"Vertex: {vertex}\n";
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                debugInfo += $"Deeper nested geometry inside GeometryInstance is of type: {deeperGeom.GetType()}\n";
                            }
                        }
                    }
                    else
                    {
                        debugInfo += $"Nested geometry is of type: {nestedGeom.GetType()}\n";
                    }
                }
                debugInfo += $"Total number of faces found: {faceCount}\n";
            }
            else
            {
                debugInfo += $"Warning: Intersected reference is of an unknown type. Geometry Type: {geomObject?.GetType().ToString() ?? "Unknown"}\n";
                debugInfoBuilder.AppendLine(debugInfo);
                return 0; // Skip this case if the reference is not a face
            }

            // Step: Loop through all the faces in the geometry element to find the intersected face based on the intersection point


            foreach (GeometryObject geomObj in geomElement)
            {
                if (geomObj is Solid solid)
                {
                    foreach (Face face in solid.Faces)
                    {
                        // Use Project to check if the intersection point is on this face
                        UV uvPoint = face.Project(closestIntersectionPoint)?.UVPoint;
                        if (uvPoint != null)
                        {
                            intersectedFace = face; // The intersection point lies on this face
                            closestIntersectedFaceReference = intersectedFace.Reference; // Save the face reference
                            debugInfo += $"Intersection Point is on Face with Area: {face.Area}\n";
                            break;
                        }
                    }
                }
                if (intersectedFace != null) break;
            }

            // Step: If the intersected face is found, log its details
            if (intersectedFace == null)
            {
                debugInfo += "Could not find the exact face intersected by the intersection point.\n";
            }
            else
            {
                debugInfo += $"The closest intersection point lies on a face with Area: {intersectedFace.Area}\n";
            }

            // Step: Validate the intersected face reference
            if (closestIntersectedFaceReference != null)
            {
                debugInfo += $"Intersected Face Reference is valid. Stable Representation: {closestIntersectedFaceReference.ConvertToStableRepresentation(doc)}\n";

                // Use the reference to get the geometry object and check if it is a face
                GeometryObject validatedGeomObject = closestIntersectedElement.GetGeometryObjectFromReference(closestIntersectedFaceReference);

                if (validatedGeomObject is Face validatedFace)
                {
                    debugInfo += "Successfully validated the reference as a Face.\n";
                }
                else
                {
                    debugInfo += $"Warning: The reference does not correspond to a valid Face. Geometry Type: {validatedGeomObject?.GetType().ToString() ?? "Unknown"}\n";
                }
            }
            else
            {
                debugInfo += "Error: Could not obtain a valid face reference for the intersected face.\n";
            }

            // Log and display the debug information
            debugInfoBuilder.AppendLine(debugInfo);
            //TaskDialog.Show("Debugging Information", debugInfoBuilder.ToString());


            // Step 7: Add more checks for debugging and validation
            string debugMessage = "";

            if (closestIntersectionPoint == null)
            {
                debugMessage += "Error: Closest intersection point is null.\n";
            }
            else
            {
                debugMessage += $"Closest Intersection Point: {closestIntersectionPoint}\n";
            }

            if (closestIntersectedFaceReference == null)
            {
                debugMessage += "Error: Intersected face reference is null.\n";
            }
            else
            {
                // Check the element for the intersected face reference
                Element intersectedFaceElement = doc.GetElement(closestIntersectedFaceReference.ElementId); // Renamed to avoid conflict

                if (intersectedFaceElement != null)
                {
                    // Log the intersected element's ID
                    debugMessage += $"Intersected Element ID: {intersectedFaceElement.Id.IntegerValue}\n";

                    // Retrieve the geometry object from the intersected face reference
                    GeometryObject geometryObject = intersectedFaceElement.GetGeometryObjectFromReference(closestIntersectedFaceReference);

                    if (geometryObject is Face faceReference) // Renamed from 'intersectedFace' to 'faceReference'
                    {
                        // Log if the intersected reference is a valid face
                        debugMessage += $"Valid face reference: {closestIntersectedFaceReference.ConvertToStableRepresentation(doc)}\n";
                    }
                    else
                    {
                        // Log the geometry type if it's not a face
                        string geometryType = geometryObject?.GetType().ToString() ?? "Unknown Type";
                        debugMessage += $"Warning: Intersected reference is not a face. Geometry Type: {geometryType}\n";
                    }
                }
                else
                {
                    // Log an error if the element for the intersected face reference could not be retrieved
                    debugMessage += "Error: Unable to retrieve element for intersected face reference.\n";
                }


            }

            // Display debugging info in a message box or log it
            if (!string.IsNullOrEmpty(debugMessage))
            {
                debugInfoBuilder.AppendLine(debugMessage);
            }

            //// Visualize the closest vertical ray (use red color for visualization)
            //CreateVisualRay(doc, closestRay.Origin, XYZ.BasisZ, closestRay.Distance, new Color(255, 0, 0));

            ProcessClosestIntersectionAndCast4Rays(doc, intersectedFace, closestIntersectionPoint, 10.0);

            // Step 8: Calculate the shortest distance using four rays
            double shortestOverhangDistance = CalculateShortestOverhangDistanceFromFourRays(doc, closestIntersectionPoint, element);

            // Step 9: Return the shortest distance as the overhang length
            return shortestOverhangDistance;


        }

        private static double CalculateShortestOverhangDistanceFromFourRays(
        Document doc,
        XYZ closestIntersectionPoint,
        Element element,
        double rayLength = 10.0)
        {
            // Initialize variables to store intersection points and distances
            List<double> distances = new List<double>();

            // Define the four directions for ray casting (parallel to the face)
            List<XYZ> rayDirections = new List<XYZ>
                {
                    new XYZ(1, 0, 0),  // +X direction
                    new XYZ(-1, 0, 0), // -X direction
                    new XYZ(0, 1, 0),  // +Y direction
                    new XYZ(0, -1, 0)  // -Y direction
                };

            // Process each ray direction
            foreach (XYZ direction in rayDirections)
            {
                // Cast the ray from the closest intersection point in the specified direction
                Reference lastFaceReference;
                Element lastIntersectedElement;
                double rayDistance = GetLastIntersectingFaceAndDistance(
                    doc,
                    closestIntersectionPoint,
                    direction,
                    rayLength,
                    element,
                    out lastFaceReference,
                    out lastIntersectedElement
                );

                // If a valid intersection is found, add the distance to the list
                if (rayDistance > 0)
                {
                    distances.Add(rayDistance);
                }
            }

            // Return the shortest distance from the four rays
            return distances.Any() ? distances.Min() : 0.0;
        }


        private static double GetLastIntersectingFaceAndDistance(
            Document doc,
            XYZ origin,
            XYZ direction,
            double rayLength,
            Element selectedElement,
            out Reference lastFaceReference,
            out Element lastIntersectedElement)
        {
            lastFaceReference = null;
            lastIntersectedElement = null;

            // Set up the category filter and the intersector
            ElementMulticategoryFilter modelCategoryFilter = GetModelCategoryFilter(doc);
            // Get the active 3D view using the helper method
            View3D active3DView = GetActive3DView(doc);

            if (active3DView == null)
            {
                throw new InvalidOperationException("No active 3D view found. Please ensure a 3D view is active.");
            }

            // Create the ReferenceIntersector with the correct View3D object
            ReferenceIntersector intersector = new ReferenceIntersector(modelCategoryFilter, FindReferenceTarget.Face, active3DView);

            // Find all intersections along the ray
            IList<ReferenceWithContext> intersectedRefs = intersector.Find(origin, direction);

            // Initialize variables to track the last intersection
            double maxDistance = 0.0;
            XYZ lastIntersectionPoint = null;

            // Process all intersections to find the last one along the ray
            foreach (var refContext in intersectedRefs)
            {
                // Get the reference to the intersected face
                Reference faceRef = refContext.GetReference();
                Element hitElement = doc.GetElement(faceRef.ElementId); // Get the element that owns the face

                // Skip if the element is the selected element (self-intersection)
                if (hitElement == null || hitElement.Id == selectedElement.Id)
                {
                    continue;
                }

                // Calculate the distance to the intersection point
                double currentDistance = refContext.Proximity;

                // Update the last intersection if this one is farther
                if (currentDistance > maxDistance)
                {
                    maxDistance = currentDistance;
                    lastIntersectionPoint = origin + (direction * refContext.Proximity);
                    lastFaceReference = faceRef;
                    lastIntersectedElement = hitElement;
                }
            }

            // If a valid last intersection point is found, return the distance
            return lastIntersectionPoint != null ? maxDistance : 0.0;
        }

        // Method to draw four rays using UV space from the closest intersection point on the face
        private static void ProcessClosestIntersectionAndCast4Rays(Document doc, Face intersectedFace, XYZ closestIntersectionPoint, double rayLength = 10.0)
        {
            if (intersectedFace == null)
            {
                TaskDialog.Show("Error", "Intersected face is null. Unable to process rays.");
                return;
            }

            UV intersectionUV;

            // Step 1: Check if the face is a PlanarFace
            if (intersectedFace is PlanarFace planarFace)
            {
                // For PlanarFace, calculate the UV coordinates manually using the face's local coordinate system
                XYZ faceOrigin = planarFace.Origin;
                XYZ faceXVec = planarFace.XVector;
                XYZ faceYVec = planarFace.YVector;

                // Calculate UV using the face's local coordinate system
                XYZ localPoint = closestIntersectionPoint - faceOrigin;
                double u = localPoint.DotProduct(faceXVec);
                double v = localPoint.DotProduct(faceYVec);

                // Create a UV point and validate it
                intersectionUV = new UV(u, v);
                //TaskDialog.Show("Debug", $"Manually calculated UV coordinates for PlanarFace: U = {u:F3}, V = {v:F3}");

                // Ensure the calculated UV point is within the face's boundary
                if (!planarFace.IsInside(intersectionUV))
                {
                    //TaskDialog.Show("Error", "The calculated UV coordinates are outside the planar face's boundary.");
                    return;
                }
            }
            else
            {
                // Step 2: Use the default Project method for other face types
                intersectionUV = intersectedFace.Project(closestIntersectionPoint)?.UVPoint;

                if (intersectionUV == null)
                {
                    // Log or display an error message if UV projection fails
                    //TaskDialog.Show("Error", "Unable to determine UV coordinates of the intersection point. The intersection point may not lie on the face's surface or is outside its boundaries.");
                    return;
                }
            }

            // Step 3: Get the UV bounds of the face
            BoundingBoxUV uvBounds = intersectedFace.GetBoundingBox();
            double minU = uvBounds.Min.U;
            double maxU = uvBounds.Max.U;
            double minV = uvBounds.Min.V;
            double maxV = uvBounds.Max.V;

            // Step 4: Check if the intersection point is within the UV bounding box of the face
            if (intersectionUV.U < minU || intersectionUV.U > maxU || intersectionUV.V < minV || intersectionUV.V > maxV)
            {
                TaskDialog.Show("UV Boundary Error",
                    $"Intersection UV: ({intersectionUV.U:F3}, {intersectionUV.V:F3})\n" +
                    $"UV Boundaries:\n" +
                    $"Min U: {minU:F3}, Max U: {maxU:F3}\n" +
                    $"Min V: {minV:F3}, Max V: {maxV:F3}\n" +
                    $"The intersection point is outside the face's UV boundaries. Ensure the point lies on the face surface.");
                return;
            }

            // Step 5: Define the four target UV points from the closest UV point
            UV uvMinU = new UV(minU, intersectionUV.V);  // Towards minU direction
            UV uvMaxU = new UV(maxU, intersectionUV.V);  // Towards maxU direction
            UV uvMinV = new UV(intersectionUV.U, minV);  // Towards minV direction
            UV uvMaxV = new UV(intersectionUV.U, maxV);  // Towards maxV direction

            // Step 6: Log the UV coordinates of the intersection point and its boundaries for debugging
            TaskDialog.Show("Debug UV Info",
                $"Intersection UV: ({intersectionUV.U:F3}, {intersectionUV.V:F3})\n" +
                $"UV Boundaries:\n" +
                $"Min U: {minU:F3}, Max U: {maxU:F3}\n" +
                $"Min V: {minV:F3}, Max V: {maxV:F3}");

            // Step 7: Create rays in four UV directions using Evaluate method on the face
            List<Line> uvRays = new List<Line>
            {
                Line.CreateBound(intersectedFace.Evaluate(intersectionUV), intersectedFace.Evaluate(uvMinU)),  // Ray towards minU
                Line.CreateBound(intersectedFace.Evaluate(intersectionUV), intersectedFace.Evaluate(uvMaxU)),  // Ray towards maxU
                Line.CreateBound(intersectedFace.Evaluate(intersectionUV), intersectedFace.Evaluate(uvMinV)),  // Ray towards minV
                Line.CreateBound(intersectedFace.Evaluate(intersectionUV), intersectedFace.Evaluate(uvMaxV))   // Ray towards maxV
            };

            // Step 8: Visualize the rays using DirectShape with the actual intersection points
            foreach (var ray in uvRays)
            {
                // Cast the ray in each direction and find the exact intersection points
                XYZ intersectionPoint = FindLastIntersectionPoint(doc, ray.GetEndPoint(0), (ray.GetEndPoint(1) - ray.GetEndPoint(0)).Normalize(), rayLength, intersectedFace);

                // Visualize the ray up to the intersection point, or stop at the specified length if no intersection is found
                XYZ endPoint = intersectionPoint ?? ray.GetEndPoint(1);

                //VisualizeRay(doc, ray.GetEndPoint(0), (endPoint - ray.GetEndPoint(0)).Normalize(), endPoint, new Color(0, 255, 0)); // Green rays for visualization
            }

            //TaskDialog.Show("Success", $"Rays drawn successfully from the closest intersection point using UV coordinates.");
        }



        private static XYZ FindLastIntersectionPoint(
            Document doc,
            XYZ origin,
            XYZ direction,
            double rayLength,
            Face intersectedFace)
        {
            // Set up the category filter and the intersector
            ElementMulticategoryFilter modelCategoryFilter = GetModelCategoryFilter(doc);
            View3D active3DView = GetActive3DView(doc);

            if (active3DView == null)
            {
                throw new InvalidOperationException("No active 3D view found. Please ensure a 3D view is active.");
            }

            // Create the ReferenceIntersector with the correct View3D object
            ReferenceIntersector intersector = new ReferenceIntersector(modelCategoryFilter, FindReferenceTarget.Face, active3DView);

            // Find all intersections along the ray
            IList<ReferenceWithContext> intersectedRefs = intersector.Find(origin, direction);

            // Initialize variables to track the last intersection
            double maxDistance = 0.0;
            XYZ lastIntersectionPoint = null;

            // Process all intersections to find the last one along the ray
            foreach (var refContext in intersectedRefs)
            {
                // Get the reference to the intersected face
                Reference faceRef = refContext.GetReference();
                Element hitElement = doc.GetElement(faceRef.ElementId); // Get the element that owns the face

                // Skip if the element is not the intersected face or if it is a self-intersection
                if (hitElement == null || faceRef == intersectedFace.Reference)
                {
                    continue;
                }

                // Calculate the distance to the intersection point
                double currentDistance = refContext.Proximity;

                // Update the last intersection if this one is farther
                if (currentDistance > maxDistance)
                {
                    maxDistance = currentDistance;
                    lastIntersectionPoint = origin + (direction * refContext.Proximity);
                }
            }

            // If a valid last intersection point is found, return it
            return lastIntersectionPoint;
        }

        private static void VisualizeRay(Document doc, XYZ origin, XYZ direction, XYZ endPoint, Color color)
        {
            using (Transaction trans = new Transaction(doc, "Visualize Ray"))
            {
                trans.Start();

                // Create the line representing the ray from origin to the end point
                Line rayLine = Line.CreateBound(origin, endPoint);

                // Create a DirectShape to represent the ray
                DirectShape rayShape = DirectShape.CreateElement(doc, new ElementId(BuiltInCategory.OST_GenericModel));
                rayShape.SetShape(new List<GeometryObject> { rayLine });

                // Set ray color and visibility
                OverrideGraphicSettings settings = new OverrideGraphicSettings();
                settings.SetProjectionLineColor(color); // Color the ray
                settings.SetProjectionLineWeight(6); // Set the line weight for better visibility

                // Apply graphic settings to the ray in the active view
                doc.ActiveView.SetElementOverrides(rayShape.Id, settings);

                trans.Commit();
            }
        }

        private static void CreateVisualRay(Document doc, XYZ origin, XYZ direction, double rayLength, Color rayColor)
        {
            using (Transaction trans = new Transaction(doc, "Create Visual Ray"))
            {
                trans.Start();

                // Create the line representing the ray
                Line rayLine = Line.CreateBound(origin, origin + direction * rayLength);

                // Create a DirectShape to represent the ray
                DirectShape rayShape = DirectShape.CreateElement(doc, new ElementId(BuiltInCategory.OST_GenericModel));
                rayShape.SetShape(new List<GeometryObject> { rayLine });

                // Set ray color and visibility
                OverrideGraphicSettings settings = new OverrideGraphicSettings();
                settings.SetProjectionLineColor(rayColor); // Set the ray color to the passed color
                settings.SetProjectionLineWeight(6); // Adjusted for thicker rays

                // Apply graphic settings to the ray in the active view
                doc.ActiveView.SetElementOverrides(rayShape.Id, settings);

                trans.Commit();
            }
        }

    }

    // Extension method to get the solid representation of the topography surface
    public static class TopographySurfaceExtensions
    {
        public static Solid GetSolid(this TopographySurface topoSurface)
        {
            GeometryElement geomElement = topoSurface.get_Geometry(new Options
            {
                DetailLevel = ViewDetailLevel.Fine,
                ComputeReferences = true,
                IncludeNonVisibleObjects = true
            });
            foreach (GeometryObject geomObj in geomElement)
            {
                if (geomObj is Solid solid && solid.Volume > 0)
                {
                    return solid;
                }
            }
            return null;
        }
    }

    public enum RayType
    {
        Vertical,
        ThirtyDegree
    }
    public enum RayTypeOption
    {
        Vertical,
        ThirtyDegree,
        Both
    }



    public class RayInfoBase
    {
        public string RayId { get; set; } // Unique ID for each ray
        public XYZ Origin { get; set; } // Ray origin point
        public XYZ Direction { get; set; } // Ray direction
        public Reference FaceReference { get; set; } // The specific face reference
        public ElementId SelectedElementId { get; set; } // ID of the intersecting element
        public Element IntersectedElement { get; set; } // The actual intersected element
        public bool IsObstructed { get; set; } // Indicates if the ray is obstructed
        public double ObstructionDistance { get; set; } // Distance to obstruction
        public List<XYZ> IntersectionPoints { get; set; } = new List<XYZ>(); // List of intersection points
        public RayType RayType { get; set; } // Type of ray (e.g., vertical, angled)

        // Global lists to store ray information
        public static List<string> AllRayIds { get; set; } = new List<string>();
        public Reference SelectedElementFaceReference { get; set; } // Face reference of the selected element
        public Reference IntersectedFaceReference { get; set; } // Face reference of the intersected element

        // Ray count variables
        public int FaceObstructedRays { get; set; }
        public int FaceUnobstructedRays { get; set; }
        public int ObstructedVerticalRays { get; set; }
        public int UnobstructedVerticalRays { get; set; }
        public int ObstructedThirtyDegreeRays { get; set; }
        public int UnobstructedThirtyDegreeRays { get; set; }

        // Obstruction distances
        public static List<double> ObstructionDistances { get; set; } = new List<double>();

        public double AverageObstructionDistance { get; set; } // Average distance for all rays

        public static List<double> VerticalObstructionDistances { get; set; } = new List<double>();
        public static List<double> ThirtyDegreeObstructionDistances { get; set; } = new List<double>();

        public double AverageObstructionDistanceThirtyDegree { get; set; }
        public double AverageObstructionDistanceVertical { get; set; }
        public static double OverallAverageVerticalObstructionDistance { get; set; } = 0.0;

    }

    // Inherit the base class for more specific ray types
    public class VerticalRayInfo : RayInfoBase { }

    public class ThirtyDegreeRayInfo : RayInfoBase { }

    public class FaceExposureDetail
    {
        public Reference FaceReference { get; set; }
        public string GrainType { get; set; } // "End Grain" or "Side Grain"
        public XYZ Normal { get; set; }
        public XYZ Centroid { get; set; }
        public UV CentroidUV { get; set; } // Added property to store the UV coordinates of the centroid
        public bool IsExposed { get; set; } // True if this face does not intersect with other elements
    }



}
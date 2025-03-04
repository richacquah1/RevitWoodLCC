//using Autodesk.Revit.DB;
//using Autodesk.Revit.UI;
//using Autodesk.Revit.Attributes;
//using System;
//using System.Linq;
//using Microsoft.VisualBasic;

//namespace RevitWoodLCC
//{
//    [Transaction(TransactionMode.Manual)]
//    public class RotateHorizontalSimulationPlane : IExternalCommand
//    {
//        // Cardinal directions and their corresponding angles
//        private readonly double[] directionAngles = new double[] {
//            0.0, 22.5, 45.0, 67.5, 90.0, 112.5, 135.0, 157.5,
//            180.0, 202.5, 225.0, 247.5, 270.0, 292.5, 315.0, 337.5
//        };

//        public Result Execute(
//            ExternalCommandData commandData,
//            ref string message,
//            ElementSet elements)
//        {
//            UIDocument uidoc = commandData.Application.ActiveUIDocument;
//            Document doc = uidoc.Document;

//            View3D view = uidoc.ActiveView as View3D;
//            if (view == null || !view.IsSectionBoxActive)
//            {
//                message = "Please activate a section box in a 3D view.";
//                return Result.Failed;
//            }

//            BoundingBoxXYZ sectionBox = view.GetSectionBox();
//            XYZ sectionTopCenter = GetSectionBoxTopCenter(sectionBox);
//            double width = sectionBox.Max.X - sectionBox.Min.X;
//            double depth = sectionBox.Max.Y - sectionBox.Min.Y;

//            double simulationPlaneHeight = GetUserInputHeight(sectionTopCenter.Z);
//            double rotationDegrees = GetHexadecagonRotationAngle();

//            using (Transaction tx = new Transaction(doc, "Rotate Horizontal Simulation Plane"))
//            {
//                tx.Start();
//                CreateSimulationPlane(doc, sectionTopCenter, width, depth, simulationPlaneHeight, rotationDegrees);
//                tx.Commit();
//            }

//            return Result.Succeeded;
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
//        0.0, 22.5, 45.0, 67.5, 90.0, 112.5, 135.0, 157.5,
//        180.0, 202.5, 225.0, 247.5, 270.0, 292.5, 315.0, 337.5
//    };

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

//        private void CreateSimulationPlane(Document doc, XYZ center, double width, double depth, double height, double directionAngle)
//        {
//            // Assume that the top view of the hexadecagon is flat on the XY plane
//            // The 0 degree direction is aligned with the positive Y axis (North)
//            // The 90 degree direction is aligned with the positive X axis (East)

//            // Calculate the right vector (East) based on the user input direction angle
//            // The direction angle is counterclockwise from the North (+Y axis)
//            double angleRadians = directionAngle * (Math.PI / 180.0);
//            XYZ right = new XYZ(Math.Sin(angleRadians), Math.Cos(angleRadians), 0);
//            XYZ up = new XYZ(-right.Y, right.X, 0); // Perpendicular to the right vector

//            // Define the plane geometry
//            XYZ elevatedCenter = new XYZ(center.X, center.Y, center.Z + height);
//            XYZ normal = new XYZ(0, 0, 1); // Normal pointing upwards (Z-axis)



//            // Create a rectangle representing the plane
//            CurveLoop baseRectangle = CreateRectangle(elevatedCenter, width, depth, right, up);

//            // Create a solid extrusion for the simulation plane
//            double extrusionDepth = 0.1; // Assuming a thin plane
//            Solid planeSolid = GeometryCreationUtilities.CreateExtrusionGeometry(new CurveLoop[] { baseRectangle }, normal, extrusionDepth);

//            // Create a DirectShape element using the solid geometry
//            DirectShape ds = DirectShape.CreateElement(doc, new SelectedElementId(BuiltInCategory.OST_GenericModel));
//            ds.SetShape(new GeometryObject[] { planeSolid });
//            ds.Name = "Horizontal Simulation Plane";

//            // Now, calculate the endpoints for the 'right' vector line on the edge of the plane
//            XYZ lineStartPoint = elevatedCenter + right * (width / 2) - up * (depth / 2);
//            XYZ lineEndPoint = lineStartPoint + up * depth;

//            // Create the model line
//            Plane plane = Plane.CreateByNormalAndOrigin(normal, elevatedCenter);
//            SketchPlane sketchPlane = SketchPlane.Create(doc, plane);
//            Line line = Line.CreateBound(lineStartPoint, lineEndPoint);
//            ModelCurve modelCurve = doc.Create.NewModelCurve(line, sketchPlane);
//        }

//        private CurveLoop CreateRectangle(XYZ center, double width, double depth, XYZ right, XYZ up)
//        {
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
//            return baseRectangle;
//        }

//    }
//}

//using Autodesk.Revit.DB;
//using Autodesk.Revit.UI;
//using Autodesk.Revit.Attributes;
//using System;
//using System.Linq;
//using Microsoft.VisualBasic;
//using System.Collections.Generic;
//using Microsoft.Scripting.Actions.Calls;
//using System.Text;

//namespace RevitWoodLCC
//{
//    [Transaction(TransactionMode.Manual)]
//    public class RotateHorizontalSimulationPlane : IExternalCommand
//    {
//        // Cardinal directions and their corresponding angles
//        private readonly double[] directionAngles = new double[] {
//            0.0, 22.5, 45.0, 67.5, 90.0, 112.5, 135.0, 157.5,
//            180.0, 202.5, 225.0, 247.5, 270.0, 292.5, 315.0, 337.5
//        };

//        // Declare a StringBuilder for logging
//        private StringBuilder logBuilder = new StringBuilder();

//        public Result Execute(
//            ExternalCommandData commandData,
//            ref string message,
//            ElementSet elements)
//        {
//            UIDocument uidoc = commandData.Application.ActiveUIDocument;
//            Document doc = uidoc.Document;

//            View3D view = uidoc.ActiveView as View3D;
//            if (view == null || !view.IsSectionBoxActive)
//            {
//                message = "Please activate a section box in a 3D view.";
//                return Result.Failed;
//            }

//            BoundingBoxXYZ sectionBox = view.GetSectionBox();
//            XYZ sectionTopCenter = GetSectionBoxTopCenter(sectionBox);
//            double width = sectionBox.Max.X - sectionBox.Min.X;
//            double depth = sectionBox.Max.Y - sectionBox.Min.Y;

//            double simulationPlaneHeight = GetUserInputHeight(sectionTopCenter.Z);
//            double rotationDegrees = GetHexadecagonRotationAngle();

//            using (Transaction tx = new Transaction(doc, "Rotate Horizontal Simulation Plane"))
//            {
//                tx.Start();
//                CreateSimulationPlane(doc, sectionTopCenter, width, depth, simulationPlaneHeight, rotationDegrees);
//                tx.Commit();
//            }

//            return Result.Succeeded;
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
//        0.0, 22.5, 45.0, 67.5, 90.0, 112.5, 135.0, 157.5,
//        180.0, 202.5, 225.0, 247.5, 270.0, 292.5, 315.0, 337.5
//    };

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

//        private void CreateSimulationPlane(Document doc, XYZ center, double width, double depth, double height, double directionAngle)
//        {
//            // Assume that the top view of the hexadecagon is flat on the XY plane
//            // The 0 degree direction is aligned with the positive Y axis (North)
//            // The 90 degree direction is aligned with the positive X axis (East)

//            // Calculate the right vector (East) based on the user input direction angle
//            // The direction angle is counterclockwise from the North (+Y axis)
//            double angleRadians = directionAngle * (Math.PI / 180.0);
//            XYZ right = new XYZ(Math.Sin(angleRadians), Math.Cos(angleRadians), 0);
//            XYZ up = new XYZ(-right.Y, right.X, 0); // Perpendicular to the right vector

//            // Define the plane geometry
//            XYZ elevatedCenter = new XYZ(center.X, center.Y, center.Z + height);
//            XYZ normal = new XYZ(0, 0, 1); // Normal pointing upwards (Z-axis)



//            // Create a rectangle representing the plane
//            CurveLoop baseRectangle = CreateRectangle(elevatedCenter, width, depth, right, up);

//            // Create a solid extrusion for the simulation plane
//            double extrusionDepth = 0.1; // Assuming a thin plane
//            Solid planeSolid = GeometryCreationUtilities.CreateExtrusionGeometry(new CurveLoop[] { baseRectangle }, normal, extrusionDepth);

//            // Create a DirectShape element using the solid geometry
//            DirectShape ds = DirectShape.CreateElement(doc, new SelectedElementId(BuiltInCategory.OST_GenericModel));
//            ds.SetShape(new GeometryObject[] { planeSolid });
//            ds.Name = "Horizontal Simulation Plane";

//            // Now, calculate the endpoints for the 'right' vector line on the edge of the plane
//            XYZ lineStartPoint = elevatedCenter + right * (width / 2) - up * (depth / 2);
//            XYZ lineEndPoint = lineStartPoint + up * depth;

//            // Create the model line
//            Plane plane = Plane.CreateByNormalAndOrigin(normal, elevatedCenter);
//            SketchPlane sketchPlane = SketchPlane.Create(doc, plane);
//            Line line = Line.CreateBound(lineStartPoint, lineEndPoint);
//            ModelCurve modelCurve = doc.Create.NewModelCurve(line, sketchPlane);
//        }

//        private CurveLoop CreateRectangle(XYZ center, double width, double depth, XYZ right, XYZ up)
//        {
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
//            return baseRectangle;
//        }

//        private List<IdentifiedRay> GenerateAndVisualizeRaysFromFace(Face planeFace, Document doc, int rayDensity, IList<Reference> faceRefs)
//        {
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
//                    XYZ endPoint = point + normal * 10.0; // Extend 10 units in the normal direction

//                    // Log ray details
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

//            // Visualize the rays in the Revit document
//            VisualizeRays(identifiedRays, doc, faceRefs);

//            return identifiedRays;
//        }

//        private void VisualizeRays(List<IdentifiedRay> rays, Document doc, IList<Reference> faceRefs)
//        {
//            foreach (IdentifiedRay identifiedRay in rays)
//            {
//                Line rayLine = identifiedRay.Ray;
//                if (rayLine != null && rayLine.Length > doc.Application.ShortCurveTolerance)
//                {
//                    // Create a DirectShape to visualize the ray
//                    DirectShape ds = DirectShape.CreateElement(doc, new SelectedElementId(BuiltInCategory.OST_Lines));
//                    ds.ApplicationId = "Rays Visualization";
//                    ds.ApplicationDataId = identifiedRay.RayId;
//                    ds.SetShape(new List<GeometryObject> { rayLine });
//                }
//            }
//        }
//    }
//}


////Works perfect create simulation plane and rotate it through all 16 cardinal degrees correctly
//using Autodesk.Revit.DB;
//using Autodesk.Revit.UI;
//using Autodesk.Revit.Attributes;
//using System;
//using System.Linq;
//using Microsoft.VisualBasic;
//using System.Text;
//using System.Collections.Generic;

//namespace RevitWoodLCC
//{
//    [Transaction(TransactionMode.Manual)]
//    public class RotateHorizontalSimulationPlane : IExternalCommand
//    {
//        // Cardinal directions and their corresponding angles
//        private readonly double[] directionAngles = new double[] {
//            0.0, 22.5, 45.0, 67.5, 90.0, 112.5, 135.0, 157.5,
//            180.0, 202.5, 225.0, 247.5, 270.0, 292.5, 315.0, 337.5
//        };


//        public Result Execute(
//            ExternalCommandData commandData,
//            ref string message,
//            ElementSet elements)
//        {
//            UIDocument uidoc = commandData.Application.ActiveUIDocument;
//            Document doc = uidoc.Document;

//            View3D view = uidoc.ActiveView as View3D;
//            if (view == null || !view.IsSectionBoxActive)
//            {
//                message = "Please activate a section box in a 3D view.";
//                return Result.Failed;
//            }

//            BoundingBoxXYZ sectionBox = view.GetSectionBox();
//            XYZ sectionTopCenter = GetSectionBoxTopCenter(sectionBox);
//            double width = sectionBox.Max.X - sectionBox.Min.X;
//            double depth = sectionBox.Max.Y - sectionBox.Min.Y;

//            double simulationPlaneHeight = GetUserInputHeight(sectionTopCenter.Z);
//            double rotationDegrees = GetHexadecagonRotationAngle();

//            using (Transaction tx = new Transaction(doc, "Rotate Horizontal Simulation Plane"))
//            {
//                tx.Start();

//                SimulationPlaneInfo simulationPlaneInfo = CreateSimulationPlane(doc, sectionTopCenter, width, depth,
//                                                                                simulationPlaneHeight, rotationDegrees);

//                tx.Commit();
//            }

//            return Result.Succeeded;
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
//        0.0, 22.5, 45.0, 67.5, 90.0, 112.5, 135.0, 157.5,
//        180.0, 202.5, 225.0, 247.5, 270.0, 292.5, 315.0, 337.5
//    };

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

//        private SimulationPlaneInfo CreateSimulationPlane(Document doc, XYZ center, double width, double depth, double height, double directionAngle)
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
//            DirectShape ds = DirectShape.CreateElement(doc, new SelectedElementId(BuiltInCategory.OST_GenericModel));
//            ds.ApplicationId = "SimulationPlane";
//            ds.ApplicationDataId = Guid.NewGuid().ToString();

//            // Now, calculate the endpoints for the 'right' vector line on the edge of the plane
//            XYZ lineStartPoint = elevatedCenter + right * (width / 2) - up * (depth / 2);
//            XYZ lineEndPoint = lineStartPoint + up * depth;

//            // Create the model line
//            Plane plane = Plane.CreateByNormalAndOrigin(normal, elevatedCenter);
//            SketchPlane sketchPlane = SketchPlane.Create(doc, plane);
//            Line line = Line.CreateBound(lineStartPoint, lineEndPoint);
//            ModelCurve modelCurve = doc.Create.NewModelCurve(line, sketchPlane);

//            PlanarFace simulationFace = null;
//            Reference faceReference = null;

//            foreach (Face face in planeSolid.Faces)
//            {
//                if (face is PlanarFace planarFace && planarFace.FaceNormal.IsAlmostEqualTo(XYZ.BasisZ))
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
//            if (faceInfoBuilder.Length > 0)
//            {
//                TaskDialog.Show("Face Info", faceInfoBuilder.ToString());
//            }

//            return simulationInfo;
//        }



//    }
//}


////Works perfect create simulation plane and rotate it through all 16 cardinal degrees correctly,
//// 2 types of rays are generated and inclined properly. 

//using Autodesk.Revit.DB;
//using Autodesk.Revit.UI;
//using Autodesk.Revit.Attributes;
//using System;
//using System.Linq;
//using Microsoft.VisualBasic;
//using System.Text;
//using System.Collections.Generic;
//using System.Windows.Controls;
//using SolarAngles.DeclinationAngle;
//using System.Windows.Media.Media3D;
//using System;
//using System.Linq;
//using System.Windows.Forms;

//namespace RevitWoodLCC
//{
//    [Transaction(TransactionMode.Manual)]
//    public class RotateHorizontalSimulationPlane : IExternalCommand
//    {
//        // Cardinal directions and their corresponding angles
//        private readonly double[] directionAngles = new double[] {
//            0.0, 22.5, 45.0, 67.5, 90.0, 112.5, 135.0, 157.5,
//            180.0, 202.5, 225.0, 247.5, 270.0, 292.5, 315.0, 337.5
//        };

//        private StringBuilder logBuilder = new StringBuilder();

//        // Booleans to control ray generation
//        bool generateOriginalRays = true /* true or false */;
//        bool generateAdditionalRays = true /* true or false */;

//        public Result Execute(
//            ExternalCommandData commandData,
//            ref string message,
//            ElementSet elements)
//        {
//            UIDocument uidoc = commandData.Application.ActiveUIDocument;
//            Document doc = uidoc.Document;

//            View3D view = uidoc.ActiveView as View3D;
//            if (view == null || !view.IsSectionBoxActive)
//            {
//                message = "Please activate a section box in a 3D view.";
//                return Result.Failed;
//            }

//            BoundingBoxXYZ sectionBox = view.GetSectionBox();
//            XYZ sectionTopCenter = GetSectionBoxTopCenter(sectionBox);
//            double width = sectionBox.Max.X - sectionBox.Min.X;
//            double depth = sectionBox.Max.Y - sectionBox.Min.Y;

//            double simulationPlaneHeight = GetUserInputHeight(sectionTopCenter.Z);
//            double rotationDegrees = GetHexadecagonRotationAngle();

//            double angleRadians = rotationDegrees * (Math.PI / 180.0);
//            XYZ right = new XYZ(Math.Sin(angleRadians), Math.Cos(angleRadians), 0);


//            int rayDensity = 5; // Define the ray density as needed
//            double rayLength = 100.0; // Define the length of the rays
//            bool visualizeRays = true; // Set to 'true' to enable visualization by default
//            double inclinationAngle = CalculateInclinationAngle(); // Get the inclination angle value.

//            using (Transaction tx = new Transaction(doc, "Rotate Horizontal Simulation Plane"))
//            {
//                tx.Start();

//                SimulationPlaneInfo simulationPlaneInfo = CreateSimulationPlane(doc, sectionTopCenter, width, depth,
//                                                                                simulationPlaneHeight, rotationDegrees);
//                PlanarFace planeFace = null;

//                if (simulationPlaneInfo.PlaneFace is PlanarFace)
//                {
//                    planeFace = simulationPlaneInfo.PlaneFace as PlanarFace;
//                }

//                // Ensure that planeFace is not null before proceeding
//                if (planeFace == null)
//                {
//                    TaskDialog.Show("Error", "Failed to obtain the simulation plane face.");
//                    return Result.Failed;
//                }

//                // Call to GenerateAndVisualizeRaysFromFace method
//                List<IdentifiedRay> rays = GenerateAndVisualizeRaysFromFace(
//                    planeFace,
//                    doc,
//                    rayDensity,
//                    rayLength,
//                    visualizeRays,
//                    right,
//                    rotationDegrees,
//                    generateOriginalRays,
//                    generateAdditionalRays);

//                tx.Commit();
//            }


//            return Result.Succeeded;
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
//        0.0, 22.5, 45.0, 67.5, 90.0, 112.5, 135.0, 157.5,
//        180.0, 202.5, 225.0, 247.5, 270.0, 292.5, 315.0, 337.5
//    };

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

//        private SimulationPlaneInfo CreateSimulationPlane(Document doc, XYZ center, double width, double depth, double height, double directionAngle)
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

//            double extrusionDepth = 1; // Assuming a thin plane
//            Solid planeSolid = GeometryCreationUtilities.CreateExtrusionGeometry(new CurveLoop[] { baseRectangle }, normal, extrusionDepth);

//            // Create a DirectShape for visualization
//            DirectShape ds = DirectShape.CreateElement(doc, new SelectedElementId(BuiltInCategory.OST_GenericModel));
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
//            if (faceInfoBuilder.Length > 0)
//            {
//                TaskDialog.Show("Face Info", faceInfoBuilder.ToString());
//            }

//            return simulationInfo;
//        }


//        private List<IdentifiedRay> GenerateAndVisualizeRaysFromFace(
//           Face planeFace,
//           Document doc,
//           int rayDensity,
//           double rayLength,
//           bool visualizeRays,
//           XYZ right,
//           double rotationDegrees,
//           bool generateOriginalRays,
//           bool generateAdditionalRays)
//        {
//            List<IdentifiedRay> rays = new List<IdentifiedRay>();
//            int rayCounter = 0; // Counter for assigning unique IDs to rays

//            // Extract the corner points of the plane face
//            EdgeArrayArray edgeLoops = planeFace.EdgeLoops;
//            EdgeArray edges = edgeLoops.get_Item(0); // Assuming the first loop is the outer loop

//            XYZ p1 = edges.get_Item(0).AsCurve().GetEndPoint(0);
//            XYZ p2 = edges.get_Item(1).AsCurve().GetEndPoint(0);
//            XYZ p3 = edges.get_Item(2).AsCurve().GetEndPoint(0);
//            XYZ p4 = edges.get_Item(3).AsCurve().GetEndPoint(0);

//            // Calculate side vectors
//            XYZ side1 = p2 - p1;
//            XYZ side2 = p3 - p2;

//            // Calculate steps
//            double step1 = side1.GetLength() / rayDensity;
//            double step2 = side2.GetLength() / rayDensity;

//            // Normalize side vectors
//            XYZ dir1 = side1.Normalize();
//            XYZ dir2 = side2.Normalize();

//            // Generate grid points
//            for (int i = 0; i <= rayDensity; i++)
//            {
//                XYZ startPoint = p1 + i * dir1 * step1;
//                XYZ endPoint = p4 + i * dir1 * step1;

//                for (int j = 0; j <= rayDensity; j++)
//                {
//                    XYZ gridPoint = startPoint + j * dir2 * step2;

//                    if (generateOriginalRays)
//                    {
//                        XYZ endPointDown = gridPoint - new XYZ(0, 0, rayLength);
//                        IdentifiedRay identifiedRay = CreateIdentifiedRay(gridPoint, endPointDown, ref rayCounter, XYZ.BasisZ.Negate());
//                        if (identifiedRay.Ray != null && identifiedRay.Ray.Length > doc.Application.ShortCurveTolerance)
//                        {
//                            rays.Add(identifiedRay);
//                        }
//                    }

//                    //if (generateAdditionalRays)
//                    //{
//                    //    double inclinationAngleRadians = rotationDegrees * (Math.PI / 180.0); // Convert to radians
//                    //    XYZ inclinedDirection = -right.Normalize() * Math.Cos(inclinationAngleRadians) + new XYZ(0, 0, -Math.Sin(inclinationAngleRadians));
//                    //    XYZ endPointInclined = gridPoint + inclinedDirection * rayLength;
//                    //    IdentifiedRay identifiedRay = CreateIdentifiedRay(gridPoint, endPointInclined, ref rayCounter, inclinedDirection);
//                    //if (identifiedRay.Ray != null && identifiedRay.Ray.Length > doc.Application.ShortCurveTolerance)
//                    //{
//                    //    rays.Add(identifiedRay);
//                    //}
//                    //}

//                    if (generateAdditionalRays)
//                    {
//                        double inclinationAngleRadians = CalculateInclinationAngle();
//                        XYZ inclinedDirection = -right.Normalize() * Math.Cos(inclinationAngleRadians);
//                        inclinedDirection += new XYZ(0, 0, -Math.Sin(inclinationAngleRadians));
//                        XYZ endPointInclined = gridPoint + inclinedDirection * rayLength;
//                        rays.Add(CreateIdentifiedRay(gridPoint, endPointInclined, ref rayCounter, inclinedDirection));
//                    }

//                }
//            }

//            if (visualizeRays)
//            {
//                VisualizeRays(rays, doc);
//            }

//            return rays;
//        }



//        private IdentifiedRay CreateIdentifiedRay(XYZ startPoint, XYZ endPoint, ref int rayCounter, XYZ direction)
//        {
//            Line rayLine = Line.CreateBound(startPoint, endPoint);
//            IdentifiedRay identifiedRay = new IdentifiedRay
//            {
//                Ray = rayLine,
//                RayId = $"Ray_{rayCounter}"
//            };

//            // Append ray details to logBuilder
//            logBuilder.AppendLine($"Ray {rayCounter}: Start {startPoint}, End {endPoint}, Direction {direction}");

//            rayCounter++; // Increment ray counter after logging
//            return identifiedRay;
//        }



//        private void VisualizeRays(List<IdentifiedRay> rays, Document doc)
//        {
//            foreach (IdentifiedRay identifiedRay in rays)
//            {
//                Line rayLine = identifiedRay.Ray;
//                // Create a DirectShape element in the document for each ray
//                DirectShape ds = DirectShape.CreateElement(doc, new SelectedElementId(BuiltInCategory.OST_Lines));
//                ds.ApplicationId = "Ray Visualization";
//                ds.ApplicationDataId = identifiedRay.RayId; // Use RayId for ApplicationDataId
//                ds.SetShape(new List<GeometryObject> { rayLine });
//            }
//        }

//        private double CalculateInclinationAngle()
//        {
//            double inclinationDegrees = 10; // Example inclination in degrees
//            return inclinationDegrees * (Math.PI / 180.0); // Convert to radians
//        }




//    }

//}
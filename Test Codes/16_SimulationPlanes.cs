using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Forms; // Add reference to System.Windows.Forms
using Microsoft.VisualBasic;



namespace RevitWoodLCC
{



    [Transaction(TransactionMode.Manual)]
    public class CreateSimulationPlanes : IExternalCommand
    {
        // Cardinal directions and their corresponding angles
        private readonly string[] cardinalDirections = new string[] {
        "N", "NNE", "NE", "ENE", "E", "ESE", "SE", "SSE",
        "S", "SSW", "SW", "WSW", "W", "WNW", "NW", "NNW"
    };

        private readonly double[] directionAngles = new double[] {
        0.0, 22.5, 45.0, 67.5, 90.0, 112.5, 135.0, 157.5,
        180.0, 202.5, 225.0, 247.5, 270.0, 292.5, 315.0, 337.5
    };
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            // Obtain the user input for direction in degrees (you'll need to implement this)
            double inputDirectionDegrees = GetUserInputDirection();

            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;

            // Verify that the current view is a 3D view with an active section box
            View3D view = uidoc.ActiveView as View3D;
            if (view == null || !view.IsSectionBoxActive)
            {
                message = "Please activate a section box in a 3D view.";
                return Result.Failed;
            }

            BoundingBoxXYZ sectionBox = view.GetSectionBox();
            XYZ sectionCenter = GetSectionBoxCenter(sectionBox);
            double longestLength = CalculateLongestSectionBoxLength(sectionBox);

            // Define additional height as required, for example:
            double additionalHeight = 3.0; // This can be a user input from the UI

            // Calculate the side length of the hexadecagon and radius
            double sideLength = longestLength + additionalHeight;
            double hexadecagonRadius = CalculateHexadecagonRadius(sideLength)/2;

            // Calculate the center point for the simulation plane based on the input direction
            XYZ planeCenter = GenerateSimulationPlaneCenter(sectionCenter, hexadecagonRadius, inputDirectionDegrees);

            // Calculate the plane size (side length of the hexadecagon)
            double planeSize = sideLength; // This will be the width of the simulation plane
            double planeHeight = sideLength; // This will be the height of the simulation plane

            // Finally, create the simulation planes
            double simulationPlaneHeight = 10.0; // Set the height for the simulation planes, this can be a user input

            double inclinationAngle = 30.0; // Example inclination angle in degrees

            // Create the simulation plane
            using (Transaction tx = new Transaction(doc, "Create Simulation Plane Geometry"))
            {
                tx.Start();

                // Pass inputDirectionDegrees as a parameter
                CreateSimulationPlaneGeometry(doc, planeCenter, planeSize, planeHeight, sectionCenter, simulationPlaneHeight, inclinationAngle, inputDirectionDegrees);

                tx.Commit();
            }

            return Result.Succeeded;
        }


        private XYZ GetSectionBoxCenter(BoundingBoxXYZ sectionBox)
        {
            return new XYZ(
                (sectionBox.Min.X + sectionBox.Max.X) / 2,
                (sectionBox.Min.Y + sectionBox.Max.Y) / 2,
                (sectionBox.Min.Z + sectionBox.Max.Z) / 2
            );
        }

        private double CalculateLongestSectionBoxLength(BoundingBoxXYZ sectionBox)
        {
            double lengthX = sectionBox.Max.X - sectionBox.Min.X;
            double lengthY = sectionBox.Max.Y - sectionBox.Min.Y;
            return Math.Max(lengthX, lengthY);
        }

        private double CalculateHexadecagonRadius(double sideLength)
        {
            int sides = 16;
            // The formula for the radius of a regular polygon is (s / (2 * sin(π / n)))
            double radius = sideLength / (2 * Math.Sin(Math.PI / sides));
            return radius;
        }




        private XYZ GenerateSimulationPlaneCenter(XYZ center, double radius, double directionDegrees)
        {
            double angleInRadians = directionDegrees * (Math.PI / 180.0);
            double y = center.Y + radius * Math.Cos(angleInRadians);
            double x = center.X + radius * Math.Sin(angleInRadians);
            return new XYZ(x, y, center.Z);
        }



        private void CreateSimulationPlaneGeometry(
            Document doc,
            XYZ planeCenter,
            double planeSize,
            double planeHeight,
            XYZ sectionCenter,
            double simulationPlaneHeight,
            double inclinationAngle,
            double inputDirectionDegrees) // Added this parameter
        {
            // Adjust the Z coordinate of the center point by the simulationPlaneHeight
            XYZ elevatedCenter = new XYZ(planeCenter.X, planeCenter.Y, sectionCenter.Z + simulationPlaneHeight);

            // Define the plane geometry
            XYZ normal = new XYZ(planeCenter.X - sectionCenter.X, planeCenter.Y - sectionCenter.Y, 0).Normalize();

            // Calculate the inclined normal vector
            // Inside your CreateSimulationPlaneGeometry method
            XYZ verticalNormal = new XYZ(0, 0, 1); // This is a vertical normal pointing upwards
            XYZ inclinedNormal = GetInclinedNormal(verticalNormal, inclinationAngle, planeCenter);


            XYZ up = new XYZ(0, 0, 1); // Up direction is the Z-axis
            XYZ right = up.CrossProduct(normal).Normalize();

            // Define the rectangle representing the plane
            XYZ p1 = elevatedCenter - right * planeSize / 2 + up * planeHeight / 2;
            XYZ p2 = elevatedCenter + right * planeSize / 2 + up * planeHeight / 2;
            XYZ p3 = elevatedCenter + right * planeSize / 2 - up * planeHeight / 2;
            XYZ p4 = elevatedCenter - right * planeSize / 2 - up * planeHeight / 2;

            // Create lines for the rectangle edges
            Line edge1 = Line.CreateBound(p1, p2);
            Line edge2 = Line.CreateBound(p2, p3);
            Line edge3 = Line.CreateBound(p3, p4);
            Line edge4 = Line.CreateBound(p4, p1);

            // Create a curve loop and add the edges
            CurveLoop baseRectangle = new CurveLoop();
            baseRectangle.Append(edge1);
            baseRectangle.Append(edge2);
            baseRectangle.Append(edge3);
            baseRectangle.Append(edge4);

            // Create a solid extrusion for the simulation plane
            double extrusionDepth = 0.1; // Assuming a thin plane
            Solid planeSolid = GeometryCreationUtilities.CreateExtrusionGeometry(new CurveLoop[] { baseRectangle }, inclinedNormal, extrusionDepth);

            // Create a DirectShape element using the solid geometry
            DirectShape ds = DirectShape.CreateElement(doc, new ElementId(BuiltInCategory.OST_GenericModel));
            ds.SetShape(new GeometryObject[] { planeSolid });

            // Assign the name with both direction and angle
            string directionName = GetUserDirectionName(inputDirectionDegrees);
            ds.Name = $"Simulation Plane - {directionName}";

            // Attempt to set the "WindDirection" parameter (if it exists)
            Parameter windDirectionParam = ds.LookupParameter("WindDirection");
            if (
                windDirectionParam != null && !windDirectionParam.IsReadOnly)
            {
                windDirectionParam.Set(directionName);
            }
            // If the parameter doesn't exist or is read-only, then do nothing.
        }


        private string GetUserDirectionName(double angle)
        {
            int index = Array.IndexOf(directionAngles, angle);
            if (index >= 0 && index < cardinalDirections.Length)
            {
                return cardinalDirections[index];
            }
            else
            {
                // Handle unexpected angle values
                throw new ArgumentOutOfRangeException("angle", "The angle is not a valid cardinal direction angle.");
            }
        }

        private XYZ GetInclinedNormal(XYZ originalNormal, double inclinationAngle, XYZ planeCenter)
        {
            // Convert inclination angle from degrees to radians
            double angleRadians = inclinationAngle * (Math.PI / 180.0);

            // The axis of rotation will be the cross product of the original normal and the Y-axis.
            XYZ rotationAxis = new XYZ(1, 0, 0); // Rotate around X-axis

            // Rotate the original normal around the rotation axis by the inclination angle
            Transform rotation = Transform.CreateRotation(rotationAxis, angleRadians);
            XYZ inclinedNormal = rotation.OfVector(originalNormal);

            return inclinedNormal;
        }






        private double GetUserInputDirection()
        {
            // Define the valid directions
            double[] validDirections = new double[]
            {
                0.0, 22.5, 45.0, 67.5, 90.0, 112.5, 135.0, 157.5,
                180.0, 202.5, 225.0, 247.5, 270.0, 292.5, 315.0, 337.5
            };

            // Use Interaction.InputBox for user input
            string userInput = Interaction.InputBox(
                "Enter the wind direction in degrees (one of the 16 cardinal angles only):\n" +
                "0, 22.5, 45, 67.5, 90, 112.5, 135, 157.5, 180, 202.5, 225, 247.5, 270, 292.5, 315, 337.5",
                "Wind Direction Input",
                "0");

            if (double.TryParse(userInput, out double inputDirection))
            {
                // Normalize 360 to 0
                if (inputDirection == 360.0) inputDirection = 0.0;

                // Validate that the input is one of the valid cardinal directions
                if (validDirections.Contains(inputDirection))
                {
                    return inputDirection;
                }
                else
                {
                    TaskDialog.Show("Invalid Input", "The value entered is not a valid cardinal direction.");
                    return GetUserInputDirection(); // Recursively call the method to get valid input
                }
            }
            else
            {
                TaskDialog.Show("Invalid Input", "Please enter a numeric value.");
                return GetUserInputDirection(); // Recursively call the method to get valid input
            }
        }
    }
}

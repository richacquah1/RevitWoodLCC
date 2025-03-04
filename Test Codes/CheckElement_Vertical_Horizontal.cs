using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Linq;

namespace RevitWoodLCC
{
    [Transaction(TransactionMode.Manual)]
    public class OrientationCheckerCommand : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            // Get the current document
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;

            // Start a transaction
            using (Transaction tx = new Transaction(doc, "Check Element Orientation"))
            {
                tx.Start();

                try
                {
                    // Get the selected element
                    ElementId selectedElementId = uidoc.Selection.GetElementIds().FirstOrDefault();

                    if (selectedElementId == null)
                    {
                        TaskDialog.Show("Error", "No element selected.");
                        return Result.Failed;
                    }

                    Element selectedElement = doc.GetElement(selectedElementId);

                    // Check orientation using a helper function
                    string result = CheckElementOrientation(selectedElement);

                    // Commit the transaction
                    tx.Commit();

                    // Show the result
                    TaskDialog.Show("Orientation Result", result);
                }
                catch (Exception ex)
                {
                    // If something goes wrong, rollback the transaction
                    tx.RollBack();
                    TaskDialog.Show("Error", ex.Message);
                    return Result.Failed;
                }
            }

            return Result.Succeeded;
        }

        public string CheckElementOrientation(Element element)
        {
            if (element == null)
                return "No element selected.";

            // Handle line-based elements with LocationCurve
            LocationCurve locCurve = element.Location as LocationCurve;
            if (locCurve != null)
            {
                // Get the direction of the curve
                XYZ direction = locCurve.Curve.GetEndPoint(1) - locCurve.Curve.GetEndPoint(0);

                // Check the Z-component of the direction vector
                if (Math.Abs(direction.Z) > 0.001) // If Z is significant, it's vertical
                {
                    return "The element is vertical.";
                }
                else
                {
                    return "The element is horizontal.";
                }
            }

            // Handle FamilyInstances
            FamilyInstance familyInstance = element as FamilyInstance;
            if (familyInstance != null)
            {
                // Get the instance's transform
                Transform transform = familyInstance.GetTransform();

                // Check the Z direction of the transform's basis vectors
                XYZ upDirection = transform.BasisZ;

                if (Math.Abs(upDirection.Z) > 0.001) // Z-component of BasisZ is significant
                {
                    return "The family instance is vertical.";
                }
                else
                {
                    return "The family instance is horizontal.";
                }
            }

            // Fallback for other elements (e.g., geometries without LocationCurve or FamilyInstance)
            BoundingBoxXYZ boundingBox = element.get_BoundingBox(null);
            if (boundingBox == null)
            {
                return "Bounding box could not be determined.";
            }

            // Calculate the direction vector from the bounding box min to max point
            XYZ elementDirection = boundingBox.Max - boundingBox.Min;
            elementDirection = elementDirection.Normalize(); // Normalize the vector

            // The Z-axis in Revit (vertical axis)
            XYZ verticalAxis = new XYZ(0, 0, 1);

            // Calculate the dot product between the element direction and the vertical axis
            double dotProduct = elementDirection.DotProduct(verticalAxis);
            double angleRadians = Math.Acos(dotProduct); // This gives the angle in radians
            double angleDegrees = angleRadians * (180.0 / Math.PI); // Convert to degrees

            // Tolerance for near-vertical elements
            double verticalTolerance = 5.0; // 5 degrees tolerance for near-vertical elements

            // Check if the element is vertical (within tolerance)
            if (Math.Abs(angleDegrees) < verticalTolerance || Math.Abs(angleDegrees - 180) < verticalTolerance)
            {
                return "The element is vertical.";
            }
            else if (angleDegrees > verticalTolerance && angleDegrees < 90.0)
            {
                return "The element is inclined.";
            }
            else
            {
                return "The element is horizontal.";
            }
        }

    }
}

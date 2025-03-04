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
    public class CreateDummyGeometry : IExternalCommand
    {
        // Define grid spacing in millimeters (can be adjusted later)
        private const double GridSpacingInMM = 100.0;
        // Define margin from the edges in millimeters (to exclude points close to the edges)
        private const double EdgeMarginInMM = 10.0;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiApp = commandData.Application;
            UIDocument uiDoc = uiApp.ActiveUIDocument;
            Document doc = uiDoc.Document;

            try
            {
                // Convert spacing and margin from millimeters to internal units (feet)
                double gridSpacing = UnitUtils.ConvertToInternalUnits(GridSpacingInMM, UnitTypeId.Millimeters);
                double edgeMargin = UnitUtils.ConvertToInternalUnits(EdgeMarginInMM, UnitTypeId.Millimeters);

                // Step 1: Pick a face
                Reference faceRef = uiDoc.Selection.PickObject(ObjectType.Face, "Select a face to generate grids and points.");
                Element elem = doc.GetElement(faceRef);
                Face face = (Face)elem.GetGeometryObjectFromReference(faceRef);

                // Step 2: Generate grids and points on the face, excluding those close to the edges
                IList<UV> gridPoints = GenerateGridPoints(face, gridSpacing, edgeMargin);

                // Step 3: Select points that form a rectangle
                BoundingBoxUV bboxUV = face.GetBoundingBox();
                IList<UV> rectanglePoints = SelectRectanglePoints(gridPoints, bboxUV, gridSpacing);

                using (Transaction trans = new Transaction(doc, "Create Dummy Geometry"))
                {
                    trans.Start();

                    // Initialize the TessellatedShapeBuilder
                    TessellatedShapeBuilder builder = new TessellatedShapeBuilder();
                    builder.OpenConnectedFaceSet(false);

                    // Add the rectangle face to the builder
                    IList<XYZ> xyzPoints = rectanglePoints.Select(uv => face.Evaluate(uv)).ToList();
                    builder.AddFace(new TessellatedFace(xyzPoints, ElementId.InvalidElementId)); // Use an invalid element ID for the default material

                    // Close the TessellatedShapeBuilder work
                    builder.CloseConnectedFaceSet();
                    builder.Target = TessellatedShapeBuilderTarget.Solid;
                    builder.Build();

                    TessellatedShapeBuilderResult result = builder.GetBuildResult();

                    // Create a DirectShape and set its shape to the tessellated solid
                    DirectShape directShape = DirectShape.CreateElement(doc, new ElementId(BuiltInCategory.OST_GenericModel));
                    directShape.SetShape(result.GetGeometricalObjects());

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

        private IList<UV> GenerateGridPoints(Face face, double gridSpacing, double edgeMargin)
        {
            IList<UV> gridPoints = new List<UV>();
            BoundingBoxUV bbox = face.GetBoundingBox();
            UV min = bbox.Min;
            UV max = bbox.Max;

            for (double u = min.U + edgeMargin; u <= max.U - edgeMargin; u += gridSpacing)
            {
                for (double v = min.V + edgeMargin; v <= max.V - edgeMargin; v += gridSpacing)
                {
                    gridPoints.Add(new UV(u, v));
                }
            }
            return gridPoints;
        }

        private IList<UV> SelectRectanglePoints(IList<UV> gridPoints, BoundingBoxUV bboxUV, double gridSpacing)
        {
            // Adjust the min and max UV points to select points a few grids inside the edges
            UV min = new UV(bboxUV.Min.U + gridSpacing, bboxUV.Min.V + gridSpacing);
            UV max = new UV(bboxUV.Max.U - gridSpacing, bboxUV.Max.V - gridSpacing);

            // Filter the grid points to only include those that fall within the adjusted min and max UV values
            var rectanglePoints = gridPoints.Where(uv =>
                uv.U >= min.U && uv.U <= max.U &&
                uv.V >= min.V && uv.V <= max.V).ToList();

            return rectanglePoints;
        }
    }
}

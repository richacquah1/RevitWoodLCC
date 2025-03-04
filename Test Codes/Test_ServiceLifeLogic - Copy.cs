using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.UI.Selection;
using System.Collections.Generic;

namespace RevitWoodLCC
{
    [Transaction(TransactionMode.Manual)]
    public class DrawFourLinesFromMidpoint : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;

            try
            {
                // Step 1: Select a face in the model
                Reference pickedObj = uidoc.Selection.PickObject(ObjectType.Face, "Select a face to draw lines from its midpoint.");

                if (pickedObj == null)
                {
                    ShowInfo("Selection Error", "No face was selected.");
                    return Result.Failed;
                }

                // Step 2: Retrieve the face and parent element
                Element faceElement = doc.GetElement(pickedObj.ElementId);
                GeometryObject geoObject = faceElement.GetGeometryObjectFromReference(pickedObj);
                Face selectedFace = geoObject as Face;

                // Step 3: Determine if the face is part of a geometry instance
                Transform transform = GetTransformForFace(pickedObj, doc);

                if (selectedFace != null)
                {
                    // Format detailed information about the face
                    string faceInfo = $"Selected Face Details:\n" +
                                      $"Element ID: {pickedObj.ElementId.IntegerValue}\n" +
                                      $"Stable Representation: {pickedObj.ConvertToStableRepresentation(doc)}\n" +
                                      $"Face Area: {selectedFace.Area} square units.";

                    // Display information about the face using a TaskDialog
                    ShowInfo("Face Selected", faceInfo);

                    // Step 4: Start transaction to draw lines on the selected face
                    using (Transaction tx = new Transaction(doc, "Draw Four Lines on Selected Face"))
                    {
                        tx.Start();
                        DrawFourLinesFromMidpointOnFace(doc, selectedFace, faceElement, transform);
                        tx.Commit();
                    }

                    return Result.Succeeded;
                }
                else
                {
                    ShowInfo("Error", "Unable to retrieve the selected face details.");
                    return Result.Failed;
                }
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                return Result.Cancelled;
            }
            catch (System.Exception ex)
            {
                ShowInfo("Exception", ex.Message);
                return Result.Failed;
            }
        }

        /// <summary>
        /// Get the transform for a face from a reference, taking into account geometry instances.
        /// </summary>
        /// <param name="reference">The reference to the selected face</param>
        /// <param name="doc">The Revit document</param>
        /// <returns>A Transform object for the geometry instance or identity if not applicable</returns>
        private Transform GetTransformForFace(Reference reference, Document doc)
        {
            Transform transform = Transform.Identity;

            // Check if the reference belongs to a geometry instance
            Element parentElement = doc.GetElement(reference.ElementId);
            if (parentElement is FamilyInstance familyInstance)
            {
                transform = familyInstance.GetTotalTransform();
            }
            else if (parentElement is RevitLinkInstance linkInstance)
            {
                transform = linkInstance.GetTransform();
            }

            return transform;
        }

        /// <summary>
        /// Draw four lines starting from the midpoint of a given face.
        /// </summary>
        /// <param name="doc">The Revit document</param>
        /// <param name="face">The face to draw lines on</param>
        /// <param name="element">The parent element of the face</param>
        /// <param name="transform">The transformation to apply if the face is from a geometry instance</param>
        public void DrawFourLinesFromMidpointOnFace(Document doc, Face face, Element element, Transform transform)
        {
            // Step 5: Get the UV bounds of the face
            BoundingBoxUV uvBounds = face.GetBoundingBox();
            double minU = uvBounds.Min.U;
            double maxU = uvBounds.Max.U;
            double minV = uvBounds.Min.V;
            double maxV = uvBounds.Max.V;

            // Step 6: Calculate the midpoint of the face in UV space
            double midU = (minU + maxU) / 2.0;
            double midV = (minV + maxV) / 2.0;
            UV midUV = new UV(midU, midV);

            // Step 7: Evaluate the midpoint in 3D space
            XYZ midPoint = face.Evaluate(midUV);

            // Step 8: Create points to draw lines from the midpoint in U and V directions
            XYZ point_U1 = face.Evaluate(new UV(minU, midV));  // Towards minU
            XYZ point_U2 = face.Evaluate(new UV(maxU, midV));  // Towards maxU
            XYZ point_V1 = face.Evaluate(new UV(midU, minV));  // Towards minV
            XYZ point_V2 = face.Evaluate(new UV(midU, maxV));  // Towards maxV

            // Step 9: Apply the transformation from the geometry instance
            midPoint = transform.OfPoint(midPoint);
            point_U1 = transform.OfPoint(point_U1);
            point_U2 = transform.OfPoint(point_U2);
            point_V1 = transform.OfPoint(point_V1);
            point_V2 = transform.OfPoint(point_V2);

            Line line_U1 = Line.CreateBound(midPoint, point_U1);  // Line towards minU
            Line line_U2 = Line.CreateBound(midPoint, point_U2);  // Line towards maxU
            Line line_V1 = Line.CreateBound(midPoint, point_V1);  // Line towards minV
            Line line_V2 = Line.CreateBound(midPoint, point_V2);  // Line towards maxV

            // Step 10: Check if the face is a planar face
            PlanarFace planarFace = face as PlanarFace;

            if (planarFace != null)
            {
                // Create a sketch plane using the transformed normal and midpoint
                Plane plane = Plane.CreateByNormalAndOrigin(transform.OfVector(planarFace.FaceNormal), midPoint);
                SketchPlane sketch = SketchPlane.Create(doc, plane);

                // Draw the model curves on the sketch plane
                doc.Create.NewModelCurve(line_U1, sketch);
                doc.Create.NewModelCurve(line_U2, sketch);
                doc.Create.NewModelCurve(line_V1, sketch);
                doc.Create.NewModelCurve(line_V2, sketch);
            }
            else
            {
                ShowInfo("Unsupported Face Type", "The selected face is not planar. Unable to create a sketch plane.");
            }
        }

        /// <summary>
        /// Display a message using TaskDialog.
        /// </summary>
        /// <param name="title">Title of the dialog</param>
        /// <param name="message">Message to display</param>
        private void ShowInfo(string title, string message)
        {
            TaskDialog dialog = new TaskDialog(title)
            {
                MainInstruction = title,
                MainContent = message,
                CommonButtons = TaskDialogCommonButtons.Close
            };
            dialog.Show();
        }
    }
}

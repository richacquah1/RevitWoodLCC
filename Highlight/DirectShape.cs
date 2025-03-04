using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;
using System.Collections.Generic;
using Autodesk.Revit.Attributes;

namespace RevitWoodLCC
{
    [Transaction(TransactionMode.Manual)]
    public class HighlightFaceCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;

            try
            {
                ColorPickedFace(uidoc);
                return Result.Succeeded;
            }
            catch (System.Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }

        public void ColorPickedFace(UIDocument uidoc)
        {
            Reference pickedRef = uidoc.Selection.PickObject(ObjectType.Face, "Please select a face.");
            Element elem = uidoc.Document.GetElement(pickedRef);
            GeometryObject geoObject = elem.GetGeometryObjectFromReference(pickedRef);

            if (geoObject is Face face)
            {
                IList<CurveLoop> loops = face.GetEdgesAsCurveLoops();

                // Obtain plane from face
                UV midPoint = new UV(0.5, 0.5);
                XYZ facePoint = face.Evaluate(midPoint);
                XYZ faceNormal = face.ComputeNormal(midPoint).Normalize();  // Make sure the face normal is a unit vector.

                XYZ extrusionDirection = XYZ.BasisZ;

                // Check if the face's normal is close to Z or its negative
                if (faceNormal.IsAlmostEqualTo(extrusionDirection) || faceNormal.IsAlmostEqualTo(-extrusionDirection))
                {
                    extrusionDirection = XYZ.BasisX;  // Choose the X direction instead.
                }

                XYZ basisX = faceNormal;
                XYZ basisY = extrusionDirection.CrossProduct(basisX).Normalize();  // Ensure this is a unit vector.

                Plane plane = Plane.CreateByOriginAndBasis(facePoint, basisX, basisY);

                // Check if the face's normal is close to Z or its negative
                if (faceNormal.IsAlmostEqualTo(extrusionDirection) || faceNormal.IsAlmostEqualTo(-extrusionDirection))
                {
                    extrusionDirection = XYZ.BasisX;  // Choose the X direction instead.
                }


                List<GeometryObject> geoObjects = new List<GeometryObject>();

                Solid extrudedSolid = GeometryCreationUtilities.CreateExtrusionGeometry(loops, extrusionDirection, 0.01);
                if (extrudedSolid != null && extrudedSolid.Volume > 0)
                {
                    geoObjects.Add(extrudedSolid);
                }

                using (Transaction trans = new Transaction(uidoc.Document, "Highlight Face"))
                {
                    trans.Start();

                    if (geoObjects.Count > 0)
                    {
                        DirectShape ds = DirectShape.CreateElement(uidoc.Document, new ElementId(BuiltInCategory.OST_GenericModel));
                        ds.SetShape(geoObjects);
                        ds.Name = "Highlighted Face";

                        OverrideGraphicSettings ogs = new OverrideGraphicSettings();
                        Color color = new Color(255, 0, 0); // Red color
                        ogs.SetSurfaceForegroundPatternColor(color);
                        ogs.SetSurfaceForegroundPatternId(GetFillPatternIdByName(uidoc.Document, "Solid fill"));
                        uidoc.ActiveView.SetElementOverrides(ds.Id, ogs);
                    }

                    trans.Commit();
                }
            }
        }




        ElementId GetFillPatternIdByName(Document doc, string patternName)
        {
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            ICollection<Element> fillPatterns = collector.OfClass(typeof(FillPatternElement)).ToElements();

            foreach (Element e in fillPatterns)
            {
                if (e.Name == patternName)
                {
                    return e.Id;
                }
            }

            return null; // Return null if no fill pattern found by that name
        }


    }
}

using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;

namespace RevitWoodLCC
{
    [Transaction(TransactionMode.Manual)]
    public class CreateExtrusionCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;

            try
            {
                CreateExtrusionFromFace(doc);
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }

        private void CreateExtrusionFromFace(Document doc)
        {
            UIDocument uidoc = new UIDocument(doc);
            Reference pickedFaceRef = uidoc.Selection.PickObject(ObjectType.Face);

            using (Transaction trans = new Transaction(doc, "Create Mass from Face"))
            {
                trans.Start();

                Face pickedFace = doc.GetElement(pickedFaceRef).GetGeometryObjectFromReference(pickedFaceRef) as Face;
                EdgeArrayArray edgeLoops = pickedFace.EdgeLoops;
                EdgeArray outerLoop = edgeLoops.get_Item(0);

                List<Curve> curves = new List<Curve>();
                foreach (Edge edge in outerLoop)
                {
                    curves.Add(edge.AsCurve());
                }

                // Sort curves to ensure they are contiguous
                curves = SortCurvesContiguously(curves);

                CurveLoop curveLoop = CurveLoop.Create(curves);
                XYZ faceNormal = pickedFace.ComputeNormal(new UV(0, 0));
                double extrusionHeight = 10.0;
                XYZ extrusionDirection = faceNormal.Multiply(extrusionHeight);
                double extrusionLength = extrusionDirection.GetLength();

                Solid extrudedSolid = GeometryCreationUtilities.CreateExtrusionGeometry(new CurveLoop[] { curveLoop }, extrusionDirection, extrusionLength);

                if (null != extrudedSolid)
                {
                    DirectShape directShape = DirectShape.CreateElement(doc, new ElementId(BuiltInCategory.OST_GenericModel));
                    directShape.SetShape(new GeometryObject[] { extrudedSolid });
                    directShape.Name = "Extruded Shape";
                }


                trans.Commit();
            }
        }

        private List<Curve> SortCurvesContiguously(List<Curve> curves)
        {
            List<Curve> sortedCurves = new List<Curve>();
            Curve currentCurve = curves[0];
            sortedCurves.Add(currentCurve);
            curves.RemoveAt(0);

            while (curves.Count > 0)
            {
                XYZ endPoint = currentCurve.GetEndPoint(1);
                bool curveFound = false;

                for (int i = 0; i < curves.Count; i++)
                {
                    if (endPoint.IsAlmostEqualTo(curves[i].GetEndPoint(0)))
                    {
                        currentCurve = curves[i];
                        sortedCurves.Add(currentCurve);
                        curves.RemoveAt(i);
                        curveFound = true;
                        break;
                    }
                }

                if (!curveFound)
                {
                    throw new InvalidOperationException("Non-contiguous curves detected.");
                }
            }

            // Check for loop closure
            if (!sortedCurves[0].GetEndPoint(0).IsAlmostEqualTo(sortedCurves[sortedCurves.Count - 1].GetEndPoint(1)))
            {
                throw new InvalidOperationException("Start and end points of the curve loop do not match.");
            }

            return sortedCurves;
        }






    }
}

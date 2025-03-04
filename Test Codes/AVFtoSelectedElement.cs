using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Analysis;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RevitWoodLCC
{
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public class AVFtoSelectedElement : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            UIApplication uiApp = commandData.Application;
            Document doc = uiApp.ActiveUIDocument.Document;
            UIDocument uidoc = uiApp.ActiveUIDocument;

            try
            {
                // Allow user to select an element
                Reference selectedElementRef = uidoc.Selection.PickObject(ObjectType.Element, "Select an element to color its faces");
                if (selectedElementRef == null)
                {
                    TaskDialog.Show("Error", "No element selected.");
                    return Result.Failed;
                }

                Element element = doc.GetElement(selectedElementRef);
                if (element == null)
                {
                    TaskDialog.Show("Error", "Selected element is null.");
                    return Result.Failed;
                }

                using (Transaction trans = new Transaction(doc, "Set Element Surface Color"))
                {
                    trans.Start();

                    View view = doc.ActiveView;
                    SpatialFieldManager sfm = GetOrCreateSpatialFieldManager(view);

                    AnalysisDisplayStyle analysisDisplayStyle = CreateAnalysisDisplayStyle(doc, "Red Surface Style");
                    view.AnalysisDisplayStyleId = analysisDisplayStyle.Id;

                    int schemaIndex = GetOrCreateAnalysisResultSchemaIndex(sfm, "Custom Data Schema", "Custom Data Description");

                    List<Face> faces = GetElementFacesWithReferences(element, doc);

                    foreach (Face face in faces)
                    {
                        if (face != null)
                        {
                            ApplyColorToFace(doc, sfm, schemaIndex, face);
                        }
                    }

                    trans.Commit();
                }

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                TaskDialog.Show("Exception", ex.ToString());
                return Result.Failed;
            }
        }

        private List<Face> GetElementFacesWithReferences(Element element, Document doc)
        {
            List<Face> faces = new List<Face>();

            Options options = new Options
            {
                ComputeReferences = true,
                DetailLevel = ViewDetailLevel.Fine
            };
            GeometryElement geomElement = element.get_Geometry(options);

            if (geomElement != null)
            {
                foreach (GeometryObject geomObj in geomElement)
                {
                    if (geomObj is Solid solid)
                    {
                        foreach (Face face in solid.Faces)
                        {
                            if (face != null && face.Reference != null)
                            {
                                faces.Add(face);
                            }
                        }
                    }
                    else if (geomObj is GeometryInstance geomInstance)
                    {
                        GeometryElement instanceGeom = geomInstance.GetInstanceGeometry();
                        foreach (GeometryObject instGeomObj in instanceGeom)
                        {
                            if (instGeomObj is Solid instanceSolid)
                            {
                                foreach (Face face in instanceSolid.Faces)
                                {
                                    if (face != null && face.Reference != null)
                                    {
                                        faces.Add(face);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return faces;
        }

        private void ApplyColorToFace(Document doc, SpatialFieldManager sfm, int schemaIndex, Face face)
        {
            if (face == null)
            {
                TaskDialog.Show("Error", "Face is null.");
                return;
            }

            IList<UV> uvPoints;
            FieldDomainPointsByUV fieldPoints = GetFieldDomainPointsByUV(face, out uvPoints);
            FieldValues fieldValues = GetFieldValuesForUVPoints(uvPoints, face);

            Reference faceRef = face.Reference;
            if (faceRef == null)
            {
                TaskDialog.Show("Error", "Face reference is null.");
                return;
            }

            int primitiveId = sfm.AddSpatialFieldPrimitive(faceRef);
            sfm.UpdateSpatialFieldPrimitive(primitiveId, fieldPoints, fieldValues, schemaIndex);
        }

        public static AnalysisDisplayStyle CreateAnalysisDisplayStyle(Document doc, string styleName)
        {
            var existingStyle = new FilteredElementCollector(doc)
                .OfClass(typeof(AnalysisDisplayStyle))
                .Cast<AnalysisDisplayStyle>()
                .FirstOrDefault(style => style.Name.Equals(styleName));

            if (existingStyle != null)
            {
                return existingStyle;
            }

            AnalysisDisplayLegendSettings legendSettings = new AnalysisDisplayLegendSettings
            {
                ShowLegend = true,
                NumberOfSteps = 2,
                ShowDataDescription = true,
                Rounding = 1
            };

            AnalysisDisplayColoredSurfaceSettings surfaceSettings = new AnalysisDisplayColoredSurfaceSettings
            {
                ShowGridLines = false
            };

            AnalysisDisplayColorSettings colorSettings = new AnalysisDisplayColorSettings
            {
                ColorSettingsType = AnalysisDisplayStyleColorSettingsType.SolidColorRanges,
                MinColor = new Autodesk.Revit.DB.Color(255, 0, 0), // Red color
                MaxColor = new Autodesk.Revit.DB.Color(255, 0, 0)  // Red color
            };

            var newStyle = AnalysisDisplayStyle.CreateAnalysisDisplayStyle(doc, styleName, surfaceSettings, colorSettings, legendSettings);
            return newStyle;
        }

        public static SpatialFieldManager GetOrCreateSpatialFieldManager(View view)
        {
            SpatialFieldManager sfm = SpatialFieldManager.GetSpatialFieldManager(view);
            if (sfm == null)
            {
                sfm = SpatialFieldManager.CreateSpatialFieldManager(view, 1);
            }
            return sfm;
        }

        public static int GetOrCreateAnalysisResultSchemaIndex(SpatialFieldManager sfm, string schemaName, string schemaDescription)
        {
            IList<int> existingSchemaIndices = sfm.GetRegisteredResults();
            foreach (int index in existingSchemaIndices)
            {
                AnalysisResultSchema existingSchema = sfm.GetResultSchema(index);
                if (existingSchema.Name == schemaName && existingSchema.Description == schemaDescription)
                {
                    return index;
                }
            }

            AnalysisResultSchema schema = new AnalysisResultSchema(schemaName, schemaDescription);
            schema.SetUnits(new List<string> { "Custom Data" }, new List<double> { 1.0 });

            int newIndex = sfm.RegisterResult(schema);
            return newIndex;
        }

        public static FieldDomainPointsByUV GetFieldDomainPointsByUV(Face face, out IList<UV> uvPoints)
        {
            uvPoints = new List<UV>();
            BoundingBoxUV bbox = face.GetBoundingBox();
            double uStep = (bbox.Max.U - bbox.Min.U) / 10; // Adjust step sizes as needed
            double vStep = (bbox.Max.V - bbox.Min.V) / 10;

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

        public static FieldValues GetFieldValuesForUVPoints(IList<UV> uvPoints, Face face)
        {
            List<ValueAtPoint> values = new List<ValueAtPoint>();
            foreach (UV uv in uvPoints)
            {
                double value = 1.0; // Arbitrary value for demonstration, modify as needed
                values.Add(new ValueAtPoint(new List<double> { value }));
            }

            return new FieldValues(values);
        }
    }
}

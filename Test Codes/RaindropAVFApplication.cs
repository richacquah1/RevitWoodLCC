using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Analysis;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace RevitWoodLCC
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class RaindropAVFApplication : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiApp = commandData.Application;
            Document doc = uiApp.ActiveUIDocument.Document;

            List<XYZ> raindropCoordinates = ReadCoordinatesFromFile();
            if (raindropCoordinates == null || raindropCoordinates.Count == 0)
            {
                message = "No valid coordinates were found.";
                return Result.Failed;
            }

            using (Transaction tx = new Transaction(doc, "Visualize Raindrops"))
            {
                tx.Start();

                // Create or get the SpatialFieldManager for the active view
                SpatialFieldManager sfm = SpatialFieldManager.GetSpatialFieldManager(doc.ActiveView);
                if (sfm == null)
                {
                    sfm = SpatialFieldManager.CreateSpatialFieldManager(doc.ActiveView, 1);
                }

                // Define the analysis display style for visualization
                AnalysisDisplayStyle analysisDisplayStyle = CreateDefaultAnalysisDisplayStyle(doc, "Raindrop Visualization Style");
                doc.ActiveView.AnalysisDisplayStyleId = analysisDisplayStyle.Id;

                // Iterate through each raindrop coordinate to visualize
                int schemaCounter = 0; // Counter for generating unique schema names
                foreach (XYZ point in raindropCoordinates)
                {
                    // Convert XYZ point to a format suitable for AVF visualization
                    // Here, we directly use XYZ points assuming they're in the correct coordinate system and units for Revit
                    var fieldPoints = new FieldDomainPointsByXYZ(new List<XYZ> { point });

                    // Create a simple value to visualize at the point
                    var values = new List<ValueAtPoint> { new ValueAtPoint(new List<double> { 1.0 }) }; // Use 1.0 as a placeholder value
                    var fieldValues = new FieldValues(values);

                    // Generate a unique schema name
                    string schemaName = "Raindrop" + schemaCounter++;

                    // Create a new spatial field primitive for each point and update it with the value
                    int index = sfm.AddSpatialFieldPrimitive();
                    sfm.UpdateSpatialFieldPrimitive(index, fieldPoints, fieldValues, GetOrCreateAnalysisResultSchemaIndex(sfm, schemaName, "Raindrop Visualization"));
                }

                tx.Commit();
            }

            return Result.Succeeded;
        }

        private List<XYZ> ReadCoordinatesFromFile()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Text Files (*.txt)|*.txt",
                Title = "Select a Text File with XYZ Coordinates"
            };

            List<XYZ> coordinates = new List<XYZ>();
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                string[] lines = System.IO.File.ReadAllLines(openFileDialog.FileName);
                foreach (string line in lines)
                {
                    string[] parts = line.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length == 3 && double.TryParse(parts[0], out double x) && double.TryParse(parts[1], out double y) && double.TryParse(parts[2], out double z))
                    {
                        coordinates.Add(new XYZ(x, y, z));
                    }
                    else
                    {
                        // Log or notify about the parsing error
                        Console.WriteLine("Error parsing line: " + line);
                    }
                }
            }
            return coordinates;
        }


        private Face GetFaceFromReference(Document doc, Reference faceRef)
        {
            Element element = doc.GetElement(faceRef.ElementId);
            return element?.GetGeometryObjectFromReference(faceRef) as Face;
        }

        private void ApplyAVFToSelectedFaces(Document doc, Dictionary<Reference, List<XYZ>> faceIntersectionPoints)
        {
            foreach (var entry in faceIntersectionPoints)
            {
                Reference faceRef = entry.Key;
                List<XYZ> intersections = entry.Value;

                Face face = GetFaceFromReference(doc, faceRef);
                if (face != null)
                {
                    SetUpAndApplyAVF(doc, face, faceRef, intersections);
                }
            }
        }

        private void SetUpAndApplyAVF(Document doc, Face face, Reference faceRef, List<XYZ> intersections)
        {
            SpatialFieldManager sfm = SpatialFieldManager.GetSpatialFieldManager(doc.ActiveView) ?? SpatialFieldManager.CreateSpatialFieldManager(doc.ActiveView, 1);
            AnalysisDisplayStyle analysisDisplayStyle = CreateDefaultAnalysisDisplayStyle(doc, "Custom AVF Style");
            doc.ActiveView.AnalysisDisplayStyleId = analysisDisplayStyle.Id;

            int schemaIndex = GetOrCreateAnalysisResultSchemaIndex(sfm, "Custom Schema", "Description");

            IList<UV> uvPoints;
            FieldDomainPointsByUV fieldPoints = GetFieldDomainPointsByUV(face, out uvPoints);
            FieldValues fieldValues = GetFieldValuesForIntersections(uvPoints, intersections, face);

            int primitiveId = sfm.AddSpatialFieldPrimitive(faceRef);
            sfm.UpdateSpatialFieldPrimitive(primitiveId, fieldPoints, fieldValues, schemaIndex);
        }

        private FieldValues GetFieldValuesForIntersections(IList<UV> uvPoints, List<XYZ> intersections, Face face)
        {
            var values = new List<ValueAtPoint>();
            double proximityThreshold = 0.16;

            foreach (UV uv in uvPoints)
            {
                XYZ point = face.Evaluate(uv);
                double nearestDistance = intersections.Min(intersect => point.DistanceTo(intersect));
                double value = nearestDistance <= proximityThreshold ? 1 : 0;

                values.Add(new ValueAtPoint(new List<double> { value }));
            }

            return new FieldValues(values);
        }

        private int GetOrCreateAnalysisResultSchemaIndex(SpatialFieldManager sfm, string schemaName, string schemaDescription)
        {
            foreach (int index in sfm.GetRegisteredResults())
            {
                AnalysisResultSchema existingSchema = sfm.GetResultSchema(index);
                if (existingSchema.Name.Equals(schemaName))
                {
                    return index;
                }
            }

            AnalysisResultSchema newSchema = new AnalysisResultSchema(schemaName, schemaDescription);
            return sfm.RegisterResult(newSchema);
        }

        private AnalysisDisplayStyle CreateDefaultAnalysisDisplayStyle(Document doc, string styleName)
        {
            AnalysisDisplayColoredSurfaceSettings surfaceSettings = new AnalysisDisplayColoredSurfaceSettings
            {
                ShowGridLines = false
            };

            AnalysisDisplayColorSettings colorSettings = new AnalysisDisplayColorSettings
            {
                MaxColor = new Color(0, 0, 255),
                MinColor = new Color(255, 255, 255)
            };

            AnalysisDisplayLegendSettings legendSettings = new AnalysisDisplayLegendSettings
            {
                ShowLegend = true,
                NumberOfSteps = 10,
                ShowDataDescription = true,
                Rounding = 0.1
            };

            return AnalysisDisplayStyle.CreateAnalysisDisplayStyle(doc, styleName, surfaceSettings, colorSettings, legendSettings);
        }

        private FieldDomainPointsByUV GetFieldDomainPointsByUV(Face face, out IList<UV> uvPoints)
        {
            uvPoints = new List<UV>();
            BoundingBoxUV bbox = face.GetBoundingBox();
            double uStep = (bbox.Max.U - bbox.Min.U) / 10;
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
    }
}

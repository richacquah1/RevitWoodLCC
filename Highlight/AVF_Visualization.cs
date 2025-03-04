/// <summary>
/// Visualizes specified faces in given elements.
/// </summary>
/// <param name="doc">The Revit document.</param>
/// <param name="view">The Revit view.</param>
/// <param name="elementGrainData">The grain data of the elements.</param>
/// <param name="analysisDisplayStyle">The style of the analysis display.</param>


/*
 // This works perfect as a utility class. Bellow is better of this code.
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Analysis;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RevitWoodLCC
{
    public static class Visualization
    {

        public static void VisualizeGrainDirection(Document doc, View view, Dictionary<Element, List<Reference>> elementGrainData, AnalysisDisplayStyle analysisDisplayStyle)
        {
            ApplyVisualization(doc, view, elementGrainData, analysisDisplayStyle);
        }

        public static void ApplyVisualization(Document doc, View view, Dictionary<Element, List<Reference>> elementGrainData, AnalysisDisplayStyle analysisDisplayStyle)
        {
            SpatialFieldManager sfm = SpatialFieldManager.GetSpatialFieldManager(view);
            if (sfm == null)
            {
                using (Transaction tx = new Transaction(doc, "Create Spatial Field Manager"))
                {
                    tx.Start();
                    sfm = SpatialFieldManager.CreateSpatialFieldManager(view, 1);
                    tx.Commit();
                }
            }

            int resultIndex = GetOrCreateAnalysisResultSchema(sfm, doc);

            foreach (var kvp in elementGrainData)
            {
                foreach (Reference reference in kvp.Value)
                {
                    ElementId elementId = reference.ElementId;
                    Element element = doc.GetElement(elementId);
                    GeometryObject geometryObject = element.GetGeometryObjectFromReference(reference);
                    Face face = geometryObject as Face;

                    if (face != null)
                    {
                        IList<UV> pts = new List<UV>();
                        IList<ValueAtPoint> valuesAtPoints = new List<ValueAtPoint>();
                        CalculateFieldPointsAndValues(face, ref pts, ref valuesAtPoints);

                        using (Transaction tx = new Transaction(doc, "Apply Color"))
                        {
                            tx.Start();
                            FieldDomainPointsByUV fieldPoints = new FieldDomainPointsByUV(pts);
                            FieldValues fieldValues = new FieldValues(valuesAtPoints);
                            int sfpIndex = sfm.AddSpatialFieldPrimitive(reference);
                            sfm.UpdateSpatialFieldPrimitive(sfpIndex, fieldPoints, fieldValues, resultIndex);
                            tx.Commit();
                        }
                    }
                }
            }
        }

        public static AnalysisDisplayStyle SetAnalysisDisplayStyle(Document doc)
        {
            AnalysisDisplayStyle analysisDisplayStyle;
            const string styleName = "Revit Webcam Display Style";

            FilteredElementCollector collector = new FilteredElementCollector(doc);
            IList<Element> elements = collector.OfClass(typeof(AnalysisDisplayStyle))
                .Where(x => x.Name.Equals(styleName))
                .Cast<Element>()
                .ToList();

            using (Transaction tx = new Transaction(doc, "Set Analysis Display Style"))
            {
                tx.Start();

                if (elements.Count > 0)
                {
                    analysisDisplayStyle = elements[0] as AnalysisDisplayStyle;
                }
                else
                {
                    AnalysisDisplayColoredSurfaceSettings coloredSurfaceSettings = new AnalysisDisplayColoredSurfaceSettings
                    {
                        ShowGridLines = false
                    };

                    AnalysisDisplayColorSettings colorSettings = new AnalysisDisplayColorSettings
                    {
                        MaxColor = new Autodesk.Revit.DB.Color(0, 0, 255), // Blue
                        MinColor = new Autodesk.Revit.DB.Color(255, 255, 255) // White
                    };

                    AnalysisDisplayLegendSettings legendSettings = new AnalysisDisplayLegendSettings
                    {
                        NumberOfSteps = 10,
                        Rounding = 0.05,
                        ShowDataDescription = false,
                        ShowLegend = false  // Set to true to show the legend
                    };

                    analysisDisplayStyle = AnalysisDisplayStyle.CreateAnalysisDisplayStyle(
                        doc, styleName, coloredSurfaceSettings, colorSettings, legendSettings);
                }

                doc.ActiveView.AnalysisDisplayStyleId = analysisDisplayStyle.Id;
                tx.Commit();
            }

            return analysisDisplayStyle;
        }

        private static int GetOrCreateAnalysisResultSchema(SpatialFieldManager sfm, Document doc)
        {
            const string schemaName = "StaticColorSchema";
            IList<int> registeredResults = sfm.GetRegisteredResults();

            if (registeredResults.Count > 0)
            {
                return registeredResults[0];
            }

            int schemaIndex;
            using (Transaction tx = new Transaction(doc, "Register Analysis Schema"))
            {
                tx.Start();
                AnalysisResultSchema resultSchema = new AnalysisResultSchema(schemaName, "Static Color Schema for Surfaces");
                schemaIndex = sfm.RegisterResult(resultSchema);
                tx.Commit();
            }

            return schemaIndex;
        }

        private static void CalculateFieldPointsAndValues(Face face, ref IList<UV> pts, ref IList<ValueAtPoint> valuesAtPoints)
        {
            BoundingBoxUV boundingBox = face.GetBoundingBox();
            double umin = boundingBox.Min.U;
            double umax = boundingBox.Max.U;
            double vmin = boundingBox.Min.V;
            double vmax = boundingBox.Max.V;

            const int _width = 10;
            const int _height = 10;

            double du = (umax - umin) / _width;
            double dv = (vmax - vmin) / _height;

            double constantValue = 0.5;

            for (int i = 0; i <= _width; i++)
            {
                for (int j = 0; j <= _height; j++)
                {
                    double u = umin + i * du;
                    double v = vmin + j * dv;
                    pts.Add(new UV(u, v));
                    valuesAtPoints.Add(new ValueAtPoint(new double[] { constantValue }));
                }
            }
        }
    }
}
*/


using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Analysis;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RevitWoodLCC
{
    public static class VisualizationUtils
    {
        public static SpatialFieldManager GetOrCreateSpatialFieldManager(View view, Document doc)
        {

            if (view == null)
            {
                throw new ArgumentNullException(nameof(view), "The provided view is null.");
            }

            SpatialFieldManager sfm = SpatialFieldManager.GetSpatialFieldManager(view);
            if (sfm == null)
            {
                using (Transaction tx = new Transaction(doc, "Create Spatial Field Manager"))
                {
                    tx.Start();
                    sfm = SpatialFieldManager.CreateSpatialFieldManager(view, 1);
                    tx.Commit();
                }
            }
            return sfm;
        }


        public static AnalysisDisplayStyle GetOrCreateAnalysisDisplayStyle(Document doc)
        {
            if (doc == null)
            {
                throw new ArgumentNullException(nameof(doc), "The provided document is null.");
            }
            const string styleName = "Revit Webcam Display Style";

            FilteredElementCollector collector = new FilteredElementCollector(doc);
            IList<Element> elements = collector.OfClass(typeof(AnalysisDisplayStyle))
                .Where(x => x.Name.Equals(styleName))
                .Cast<Element>()
                .ToList();

            AnalysisDisplayStyle analysisDisplayStyle;

            if (elements.Count > 0)
            {
                analysisDisplayStyle = elements[0] as AnalysisDisplayStyle;
            }
            else
            {
                using (Transaction tx = new Transaction(doc, "Create Analysis Display Style"))
                {
                    tx.Start();
                    analysisDisplayStyle = CreateDefaultAnalysisDisplayStyle(doc, styleName);
                    tx.Commit();
                }
            }


            using (Transaction tx = new Transaction(doc, "Set Analysis Display Style to View"))
            {
                tx.Start();
                doc.ActiveView.AnalysisDisplayStyleId = analysisDisplayStyle.Id;
                tx.Commit();
            }

            return analysisDisplayStyle;


        }


        private static AnalysisDisplayStyle CreateDefaultAnalysisDisplayStyle(Document doc, string styleName)
        {
            AnalysisDisplayColoredSurfaceSettings coloredSurfaceSettings = new AnalysisDisplayColoredSurfaceSettings
            {
                ShowGridLines = false
            };

            AnalysisDisplayColorSettings colorSettings = new AnalysisDisplayColorSettings
            {
                MaxColor = new Autodesk.Revit.DB.Color(0, 0, 255), // Blue
                MinColor = new Autodesk.Revit.DB.Color(255, 255, 255) // White
            };

            AnalysisDisplayLegendSettings legendSettings = new AnalysisDisplayLegendSettings
            {
                NumberOfSteps = 10,
                Rounding = 0.05,
                ShowDataDescription = false,
                ShowLegend = false  // Set to true to show the legend
            };

            return AnalysisDisplayStyle.CreateAnalysisDisplayStyle(doc, styleName, coloredSurfaceSettings, colorSettings, legendSettings);
        }

        public static int GetAnalysisResultSchemaIndex(SpatialFieldManager sfm, Document doc)
        {
            const string schemaName = "StaticColorSchema";
            IList<int> registeredResults = sfm.GetRegisteredResults();

            if (registeredResults.Count > 0)
            {
                return registeredResults[0];
            }

            int schemaIndex;
            using (Transaction tx = new Transaction(doc, "Register Analysis Schema"))
            {
                tx.Start();
                AnalysisResultSchema resultSchema = new AnalysisResultSchema(schemaName, "Static Color Schema for Surfaces");
                schemaIndex = sfm.RegisterResult(resultSchema);
                tx.Commit();
            }

            return schemaIndex;
        }


        public static void CalculateFieldPointsAndValues(Face face, out IList<UV> pts, out IList<ValueAtPoint> valuesAtPoints)
        {
            if (face == null)
            {
                throw new ArgumentNullException(nameof(face), "The provided face is null.");
            }

            pts = new List<UV>();
            valuesAtPoints = new List<ValueAtPoint>();

            BoundingBoxUV boundingBox = face.GetBoundingBox();
            double umin = boundingBox.Min.U;
            double umax = boundingBox.Max.U;
            double vmin = boundingBox.Min.V;
            double vmax = boundingBox.Max.V;

            const int _width = 10;
            const int _height = 10;

            double du = (umax - umin) / _width;
            double dv = (vmax - vmin) / _height;

            double constantValue = 0.5;

            for (int i = 0; i <= _width; i++)
            {
                for (int j = 0; j <= _height; j++)
                {
                    double u = umin + i * du;
                    double v = vmin + j * dv;
                    pts.Add(new UV(u, v));
                    valuesAtPoints.Add(new ValueAtPoint(new double[] { constantValue }));
                }
            }
        }

        public static void ApplyVisualization(Document doc, View view, Dictionary<Element, List<Reference>> elementGrainData, AnalysisDisplayStyle analysisDisplayStyle)
        {

            if (doc == null)
            {
                throw new ArgumentNullException(nameof(doc), "The provided document is null.");
            }

            if (view == null)
            {
                throw new ArgumentNullException(nameof(view), "The provided view is null.");
            }

            if (elementGrainData == null)
            {
                throw new ArgumentNullException(nameof(elementGrainData), "The provided elementGrainData is null.");
            }

            SpatialFieldManager sfm = GetOrCreateSpatialFieldManager(view, doc);

            int resultIndex = GetAnalysisResultSchemaIndex(sfm, doc);

            foreach (var kvp in elementGrainData)
            {
                foreach (Reference reference in kvp.Value)
                {
                    ElementId elementId = reference.ElementId;
                    Element element = doc.GetElement(elementId);
                    GeometryObject geometryObject = element.GetGeometryObjectFromReference(reference);
                    Face face = geometryObject as Face;

                    if (face != null)
                    {
                        IList<UV> pts;
                        IList<ValueAtPoint> valuesAtPoints;
                        CalculateFieldPointsAndValues(face, out pts, out valuesAtPoints);

                        using (Transaction tx = new Transaction(doc, "Apply Color"))
                        {
                            tx.Start();
                            FieldDomainPointsByUV fieldPoints = new FieldDomainPointsByUV(pts);
                            FieldValues fieldValues = new FieldValues(valuesAtPoints);
                            int sfpIndex = sfm.AddSpatialFieldPrimitive(reference);
                            sfm.UpdateSpatialFieldPrimitive(sfpIndex, fieldPoints, fieldValues, resultIndex);
                            tx.Commit();
                        }
                    }
                }
            }
        }

    }
}


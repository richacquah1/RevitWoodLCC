/*using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Analysis;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RevitWoodLCC
{
    public static class AnalysisUtility
    {
        // Function to get or create an AnalysisResultSchema
public static AnalysisResultSchema GetOrCreateAnalysisResultSchema(Document doc, string schemaName, string description)
{
    try
    {
        var existingSchema = new FilteredElementCollector(doc)
                                .OfClass(typeof(AnalysisResultSchema))
                                .Cast<AnalysisResultSchema>()
                                .FirstOrDefault(a => a.Name == schemaName);

        if (existingSchema != null)
        {
            return existingSchema;
        }

        AnalysisResultSchema newSchema = null;

        using (Transaction t = new Transaction(doc, "Create Analysis Schema"))
        {
            t.Start();
            newSchema = new AnalysisResultSchema(schemaName, description);
            t.Commit();
        }

        return newSchema;
    }
    catch (Exception e)
    {
        // Implement appropriate logging here.
        throw; // Or manage the exception as appropriate for your application.
    }
}



        // Function to create or update an AnalysisDisplayStyle
           public static AnalysisDisplayStyle CreateOrUpdateAnalysisDisplayStyle(Document doc, string styleName, AnalysisResultSchema schema)
           {
               var existingStyle = new FilteredElementCollector(doc)
                                       .OfClass(typeof(AnalysisDisplayStyle))
                                       .Cast<AnalysisDisplayStyle>()
                                       .FirstOrDefault(a => a.Name == styleName);

               using (Transaction t = new Transaction(doc, "Create/Update Analysis Style"))
               {
                   t.Start();

                   if (existingStyle == null)
                   {
                       AnalysisDisplayColoredSurfaceSettings surfaceSettings = new AnalysisDisplayColoredSurfaceSettings();
                       AnalysisDisplayColorSettings colorSettings = new AnalysisDisplayColorSettings();
                       AnalysisDisplayLegendSettings legendSettings = new AnalysisDisplayLegendSettings
                       {
                           NumberOfSteps = 10,
                           Rounding = 0.05,
                           ShowDataDescription = false,
                           ShowLegend = false  // Set to true to show the legend
                       };

                       AnalysisDisplayStyle newStyle = AnalysisDisplayStyle.CreateAnalysisDisplayStyle(
                           doc,
                           styleName,
                           surfaceSettings,
                           colorSettings,
                           legendSettings
                       );

                       existingStyle = newStyle;
                   }
                   else
                   {
                       // If the style already exists, update its legend settings to hide the legend
                       AnalysisDisplayLegendSettings legendSettings = existingStyle.GetLegendSettings();
                       legendSettings.ShowLegend = false;  // Set to true to show the legend
                       existingStyle.SetLegendSettings(legendSettings);
                   }

                   doc.Regenerate();
                   t.Commit();
               }

               return existingStyle;
           }
        

        public static AnalysisDisplayStyle CreateOrUpdateAnalysisDisplayStyle(Document doc, string styleName, AnalysisResultSchema schema)
        {
            var existingStyle = new FilteredElementCollector(doc)
                                    .OfClass(typeof(AnalysisDisplayStyle))
                                    .Cast<AnalysisDisplayStyle>()
                                    .FirstOrDefault(a => a.Name == styleName);

            using (Transaction t = new Transaction(doc, "Create/Update Analysis Style"))
            {
                t.Start();

                AnalysisDisplayColoredSurfaceSettings coloredSurfaceSettings = new AnalysisDisplayColoredSurfaceSettings();
                coloredSurfaceSettings.ShowGridLines = false;  // Assuming you want to keep grid lines hidden

                AnalysisDisplayColorSettings colorSettings = new AnalysisDisplayColorSettings();
                colorSettings.MaxColor = new Autodesk.Revit.DB.Color(0, 0, 255);  // Blue
                colorSettings.MinColor = new Autodesk.Revit.DB.Color(255, 255, 255);  // White

                AnalysisDisplayLegendSettings legendSettings = new AnalysisDisplayLegendSettings();
                legendSettings.NumberOfSteps = 10;
                legendSettings.Rounding = 0.05;
                legendSettings.ShowDataDescription = false;
                legendSettings.ShowLegend = false;  // Set to true to show the legend

                if (existingStyle == null)
                {
                    AnalysisDisplayStyle newStyle = AnalysisDisplayStyle.CreateAnalysisDisplayStyle(
                        doc,
                        styleName,
                        coloredSurfaceSettings,
                        colorSettings,
                        legendSettings
                    );

                    existingStyle = newStyle;
                }
                else
                {
                    existingStyle.SetLegendSettings(legendSettings);
                    existingStyle.SetColoredSurfaceSettings(coloredSurfaceSettings);
                    existingStyle.SetColorSettings(colorSettings);
                }

                doc.Regenerate();
                t.Commit();
            }


            return existingStyle;
        }


        // Function to get or create a SpatialFieldManager for a given view
        public static SpatialFieldManager GetOrCreateSpatialFieldManager(Document doc, View view)
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
            return sfm;
        }

        // Function to get or create and register an AnalysisResultSchema
        public static int GetOrCreateAnalysisResultSchema(SpatialFieldManager sfm, Document doc)
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

        // Function to calculate field points and values for a given face
        public static void CalculateFieldPointsAndValues(Face face, ref IList<UV> pts, ref IList<ValueAtPoint> valuesAtPoints)
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

        // Function to update spatial field primitive with field points and values
        public static void UpdateSpatialFieldPrimitive(Document doc, SpatialFieldManager sfm, Reference reference, IList<UV> pts, IList<ValueAtPoint> valuesAtPoints, int resultIndex)
        {
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
*/

/*
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Analysis;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RevitWoodLCC
{
    public class AVFUtility
    {
        public AVFUtility()
        {
            // Constructor, if any initialization is needed, add it here.
        }

        public void ColorFace(Document doc, Face face, Autodesk.Revit.DB.Color color)
        {
            // Initialize AVF
            SpatialFieldManager sfm = SpatialFieldManager.GetSpatialFieldManager(doc.ActiveView);
            if (sfm == null)
            {
                sfm = SpatialFieldManager.CreateSpatialFieldManager(doc.ActiveView, 1);
            }

            // Obtain a Reference to the Face object
            Reference faceReference = null;
            Element element = doc.GetElement(face.Reference);
            if (element != null)
            {
                faceReference = face.Reference;
            }
            if (faceReference == null)
            {
                throw new InvalidOperationException("Unable to obtain a reference to the face.");
            }

            // Prepare Data
            IList<UV> points = new List<UV>();
            IList<ValueAtPoint> values = new List<ValueAtPoint>();
            CalculateFieldPointsAndValues(face, ref points, ref values);

            // Create or Update Analysis Display Style
            AnalysisDisplayStyle style = CreateOrUpdateAnalysisDisplayStyle(doc, color);

            // Update Spatial Field Primitive
            int index;
            using (Transaction t = new Transaction(doc, "Color Face"))
            {
                t.Start();
                index = sfm.AddSpatialFieldPrimitive(faceReference);
                FieldDomainPointsByUV fieldPoints = new FieldDomainPointsByUV(points);
                FieldValues fieldValues = new FieldValues(values);
                sfm.UpdateSpatialFieldPrimitive(index, fieldPoints, fieldValues, 0);
                t.Commit();
            }
        }

        private void CalculateFieldPointsAndValues(Face face, ref IList<UV> points, ref IList<ValueAtPoint> values)
        {
            BoundingBoxUV boundingBox = face.GetBoundingBox();
            double umin = boundingBox.Min.U;
            double umax = boundingBox.Max.U;
            double vmin = boundingBox.Min.V;
            double vmax = boundingBox.Max.V;

            const int gridResolution = 10;  // You can adjust the resolution to get more or fewer points

            double du = (umax - umin) / gridResolution;
            double dv = (vmax - vmin) / gridResolution;

            for (int i = 0; i <= gridResolution; i++)
            {
                for (int j = 0; j <= gridResolution; j++)
                {
                    double u = umin + i * du;
                    double v = vmin + j * dv;

                    UV point = new UV(u, v);
                    points.Add(point);

                    // Assume a constant value for demonstration purposes.
                    // Replace this with your actual analysis to compute a value based on the point's position on the face.
                    double value = 1.0;
                    ValueAtPoint valueAtPoint = new ValueAtPoint(new double[] { value });
                    values.Add(valueAtPoint);
                }
            }
        }

        private AnalysisDisplayStyle CreateOrUpdateAnalysisDisplayStyle(Document doc, Autodesk.Revit.DB.Color color)
        {
            string styleName = "CustomAnalysisStyle";  // Customize the style name as needed
            AnalysisDisplayStyle existingStyle = null;

            // Search for an existing style with the desired name
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            var styles = collector.OfClass(typeof(AnalysisDisplayStyle)).Cast<AnalysisDisplayStyle>();
            foreach (var style in styles)
            {
                if (style.Name == styleName)
                {
                    existingStyle = style;
                    break;
                }
            }

            using (Transaction t = new Transaction(doc, "Create or Update Analysis Style"))
            {
                t.Start();

                if (existingStyle == null)  // Create a new style if none exists with the desired name
                {
                    // Define settings for the colored surface, color settings, and legend settings
                    AnalysisDisplayColoredSurfaceSettings surfaceSettings = new AnalysisDisplayColoredSurfaceSettings
                    {
                        ShowGridLines = false
                    };

                    AnalysisDisplayColorSettings colorSettings = new AnalysisDisplayColorSettings
                    {
                        MaxColor = color,
                        MinColor = color
                    };

                    AnalysisDisplayLegendSettings legendSettings = new AnalysisDisplayLegendSettings
                    {
                        NumberOfSteps = 1,
                        Rounding = 0.1,
                        ShowDataDescription = false,
                        ShowLegend = false
                    };

                    // Create a new AnalysisDisplayStyle
                    existingStyle = AnalysisDisplayStyle.CreateAnalysisDisplayStyle(
                        doc,
                        styleName,
                        surfaceSettings,
                        colorSettings,
                        legendSettings
                    );
                }
                else  // Update the existing style with the new color
                {
                    AnalysisDisplayColorSettings colorSettings = new AnalysisDisplayColorSettings
                    {
                        MaxColor = color,
                        MinColor = color
                    };

                    existingStyle.SetColorSettings(colorSettings);
                }

                t.Commit();
            }

            return existingStyle;
        }
    }
}

*/
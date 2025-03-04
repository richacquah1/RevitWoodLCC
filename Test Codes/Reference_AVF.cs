using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Analysis;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RevitWoodLCC
{
    [Transaction(TransactionMode.Manual)]
    public class ApplyAVFToFace : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiApp = commandData.Application;
            UIDocument uiDoc = uiApp.ActiveUIDocument;
            Document doc = uiDoc.Document;

            try
            {
                // Prompt user to select faces
                IList<Reference> selectedFaceRefs = uiDoc.Selection.PickObjects(ObjectType.Face, "Select faces and click OK.");

                if (selectedFaceRefs.Count == 0)
                {
                    message = "No faces selected.";
                    return Result.Cancelled;
                }

                using (Transaction trans = new Transaction(doc, "Apply AVF to Faces"))
                {
                    trans.Start();

                    SpatialFieldManager sfm = SpatialFieldManager.GetSpatialFieldManager(doc.ActiveView);
                    if (sfm == null)
                    {
                        sfm = SpatialFieldManager.CreateSpatialFieldManager(doc.ActiveView, 1);
                    }
                    else
                    {
                        // Clear existing AVF data
                        sfm.Clear();
                    }

                    AnalysisDisplayStyle analysisDisplayStyle = CreateDefaultAnalysisDisplayStyle(doc, "Custom AVF Style");
                    doc.ActiveView.AnalysisDisplayStyleId = analysisDisplayStyle.Id;

                    // Process each selected face
                    foreach (Reference faceRef in selectedFaceRefs)
                    {
                        Element element = doc.GetElement(faceRef.ElementId);
                        if (element == null) continue;

                        GeometryObject geomObj = element.GetGeometryObjectFromReference(faceRef);
                        if (geomObj is Face face)
                        {
                            SetUpAndApplyAVF(doc, face, faceRef);
                        }
                    }

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
                message = $"Unexpected error occurred: {ex.Message}";
                return Result.Failed;
            }
        }



        private (Face, Reference) GetFaceFromReference(Document doc, Reference reference)
        {
            Element element = doc.GetElement(reference);
            GeometryObject geoObject = element.GetGeometryObjectFromReference(reference);
            Face face = geoObject as Face;
            return (face, reference);
        }

        //private void SetUpAndApplyAVF(Document doc, Face face, Reference faceRef)
        //{
        //    // Create or get SpatialFieldManager for the active view
        //    SpatialFieldManager sfm = SpatialFieldManager.GetSpatialFieldManager(doc.ActiveView);
        //    if (sfm == null)
        //    {
        //        sfm = SpatialFieldManager.CreateSpatialFieldManager(doc.ActiveView, 1);
        //    }

        //    // Create an AnalysisDisplayStyle
        //    AnalysisDisplayStyle analysisDisplayStyle = CreateDefaultAnalysisDisplayStyle(doc, "Custom AVF Style");

        //    // Set the AnalysisDisplayStyle to the active view
        //    doc.ActiveView.AnalysisDisplayStyleId = analysisDisplayStyle.Id;

        //    // Register a new result schema with the SpatialFieldManager
        //    AnalysisResultSchema resultSchema = new AnalysisResultSchema("Custom Schema", "Description");
        //    //int schemaIndex = sfm.RegisterResult(resultSchema);
        //    int schemaIndex = GetOrCreateAnalysisResultSchemaIndex(sfm, "Custom Schema", "Description");

        //    // Prepare data for AVF
        //    IList<UV> uvPoints;
        //    FieldDomainPointsByUV fieldPoints = GetFieldDomainPointsByUV(face, out uvPoints);
        //    FieldValues fieldValues = GetFieldValues(uvPoints);

        //    // Register face with SpatialFieldManager
        //    int primitiveId = sfm.AddSpatialFieldPrimitive(faceRef);

        //    // Update the spatial field with the correct resultIndex
        //    sfm.UpdateSpatialFieldPrimitive(primitiveId, fieldPoints, fieldValues, schemaIndex);
        //}

        private void SetUpAndApplyAVF(Document doc, Face face, Reference faceRef)
        {
            // Get or create SpatialFieldManager for the active view
            SpatialFieldManager sfm = SpatialFieldManager.GetSpatialFieldManager(doc.ActiveView);
            if (sfm == null)
            {
                sfm = SpatialFieldManager.CreateSpatialFieldManager(doc.ActiveView, 1);
            }
            //else
            //{
            //    // Clear existing AVF data
            //    sfm.Clear();
            //}

            // Create an AnalysisDisplayStyle
            AnalysisDisplayStyle analysisDisplayStyle = CreateDefaultAnalysisDisplayStyle(doc, "Custom AVF Style");

            // Set the AnalysisDisplayStyle to the active view
            doc.ActiveView.AnalysisDisplayStyleId = analysisDisplayStyle.Id;

            // Register a new result schema with the SpatialFieldManager
            AnalysisResultSchema resultSchema = new AnalysisResultSchema("Custom Schema", "Description");
            int schemaIndex = GetOrCreateAnalysisResultSchemaIndex(sfm, "Custom Schema", "Description");

            // Prepare data for AVF
            IList<UV> uvPoints;
            FieldDomainPointsByUV fieldPoints = GetFieldDomainPointsByUV(face, out uvPoints);
            FieldValues fieldValues = GetFieldValues(uvPoints);

            // Add or Update the spatial field primitive for the face
            int primitiveId = sfm.AddSpatialFieldPrimitive(faceRef);
            sfm.UpdateSpatialFieldPrimitive(primitiveId, fieldPoints, fieldValues, schemaIndex);
        }


        private int GetOrCreateAnalysisResultSchemaIndex(SpatialFieldManager sfm, string schemaName, string schemaDescription)
        {
            foreach (int index in sfm.GetRegisteredResults())
            {
                AnalysisResultSchema existingSchema = sfm.GetResultSchema(index);
                if (existingSchema.Name.Equals(schemaName))
                {
                    // Schema already exists, return its index
                    return index;
                }
            }

            // Schema does not exist, create a new one
            AnalysisResultSchema newSchema = new AnalysisResultSchema(schemaName, schemaDescription);
            return sfm.RegisterResult(newSchema);
        }

        private AnalysisDisplayStyle CreateDefaultAnalysisDisplayStyle(Document doc, string styleName)
        {
            var existingStyle = new FilteredElementCollector(doc)
                .OfClass(typeof(AnalysisDisplayStyle))
                .Cast<AnalysisDisplayStyle>()
                .FirstOrDefault(style => style.Name.Equals(styleName));

            if (existingStyle != null)
                return existingStyle;

            AnalysisDisplayColoredSurfaceSettings surfaceSettings = new AnalysisDisplayColoredSurfaceSettings
            {
                ShowGridLines = false
            };

            AnalysisDisplayColorSettings colorSettings = new AnalysisDisplayColorSettings
            {
                MaxColor = new Color(255, 0, 0), // Red
                MinColor = new Color(0, 255, 0)  // Green
            };

            AnalysisDisplayLegendSettings legendSettings = new AnalysisDisplayLegendSettings
            {
                ShowLegend = true,
                NumberOfSteps = 10,             // Adjust the number of steps in the legend
                ShowDataDescription = true,     // Show or hide data description
                Rounding = 0.1                  // Set rounding for values
                                                // Note: Direct control over legend size is not available
            };

            return AnalysisDisplayStyle.CreateAnalysisDisplayStyle(doc, styleName, surfaceSettings, colorSettings, legendSettings);
        }




        private FieldDomainPointsByUV GetFieldDomainPointsByUV(Face face, out IList<UV> uvPoints)
        {
            uvPoints = new List<UV>();
            BoundingBoxUV bbox = face.GetBoundingBox();
            double uStep = (bbox.Max.U - bbox.Min.U) / 10; // 10 steps across the U direction
            double vStep = (bbox.Max.V - bbox.Min.V) / 10; // 10 steps across the V direction

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


        private FieldValues GetFieldValues(IList<UV> uvPoints)
        {
            IList<ValueAtPoint> values = new List<ValueAtPoint>();
            foreach (UV uv in uvPoints)
            {
                double sampleValue = uv.U + uv.V; // Replace this with actual analysis value
                values.Add(new ValueAtPoint(new List<double> { sampleValue }));
            }

            return new FieldValues(values);
        }



    }
}

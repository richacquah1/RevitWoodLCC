////This works perfect
//using Autodesk.Revit.ApplicationServices;
//using Autodesk.Revit.Attributes;
//using Autodesk.Revit.DB;
//using Autodesk.Revit.DB.Analysis;
//using Autodesk.Revit.UI;
//using Autodesk.Revit.UI.Selection;
//using System;
//using System.Collections.Generic;
//using System.Linq;

//namespace RevitWoodLCC
//{
//    [Transaction(TransactionMode.Manual)]
//    public class NEWDisplayAnalysis : IExternalCommand
//    {
//        const string _prompt = "Please pick faces to apply color";

//        class BimElementFilter : ISelectionFilter
//        {
//            public bool AllowElement(Element e)
//            {
//                return null != e.Category && e.Category.HasMaterialQuantities;
//            }

//            public bool AllowReference(Reference r, XYZ p)
//            {
//                return true;
//            }
//        }

//        void SetAnalysisDisplayStyle(Document doc)
//        {
//            AnalysisDisplayStyle analysisDisplayStyle;
//            const string styleName = "Revit Webcam Display Style";

//            FilteredElementCollector collector = new FilteredElementCollector(doc);
//            IList<Element> elements = collector.OfClass(typeof(AnalysisDisplayStyle))
//                .Where(x => x.Name.Equals(styleName))
//                .Cast<Element>()
//                .ToList();

//            using (Transaction tx = new Transaction(doc, "Set Analysis Display Style"))
//            {
//                tx.Start();

//                if (elements.Count > 0)
//                {
//                    analysisDisplayStyle = elements[0] as AnalysisDisplayStyle;
//                }
//                else
//                {
//                    AnalysisDisplayColoredSurfaceSettings coloredSurfaceSettings =
//                        new AnalysisDisplayColoredSurfaceSettings();
//                    coloredSurfaceSettings.ShowGridLines = false;

//                    AnalysisDisplayColorSettings colorSettings =
//                        new AnalysisDisplayColorSettings();
//                    colorSettings.MaxColor = new Autodesk.Revit.DB.Color(0, 0, 255); // Blue
//                    colorSettings.MinColor = new Autodesk.Revit.DB.Color(255, 255, 255); // White

//                    AnalysisDisplayLegendSettings legendSettings =
//                        new AnalysisDisplayLegendSettings();
//                    legendSettings.NumberOfSteps = 10;
//                    legendSettings.Rounding = 0.05;
//                    legendSettings.ShowDataDescription = false;
//                    legendSettings.ShowLegend = false; //set to true to show the legend

//                    analysisDisplayStyle = AnalysisDisplayStyle.CreateAnalysisDisplayStyle(
//                        doc, styleName, coloredSurfaceSettings, colorSettings, legendSettings);
//                }

//                doc.ActiveView.AnalysisDisplayStyleId = analysisDisplayStyle.Id;
//                tx.Commit();
//            }
//        }

//        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
//        {
//            try
//            {
//                UIApplication uiapp = commandData.Application;
//                UIDocument uidoc = uiapp.ActiveUIDocument;
//                Document doc = uidoc.Document;
//                View view = doc.ActiveView;

//                IList<Reference> references = uidoc.Selection.PickObjects(ObjectType.Face, new BimElementFilter(), _prompt);
//                SetAnalysisDisplayStyle(doc);

//                SpatialFieldManager sfm = SpatialFieldManager.GetSpatialFieldManager(view);
//                if (sfm == null)
//                {
//                    using (Transaction tx = new Transaction(doc, "Create Spatial Field Manager"))
//                    {
//                        tx.Start();
//                        sfm = SpatialFieldManager.CreateSpatialFieldManager(view, 1);
//                        tx.Commit();
//                    }
//                }

//                int resultIndex = GetOrCreateAnalysisResultSchema(sfm, doc);

//                foreach (Reference reference in references)
//                {
//                    ElementId elementId = reference.ElementId;
//                    Element element = doc.GetElement(elementId);
//                    GeometryObject geometryObject = element.GetGeometryObjectFromReference(reference);
//                    Face face = geometryObject as Face;

//                    if (face != null)
//                    {
//                        IList<UV> pts = new List<UV>();
//                        IList<ValueAtPoint> valuesAtPoints = new List<ValueAtPoint>();
//                        CalculateFieldPointsAndValues(face, ref pts, ref valuesAtPoints);

//                        using (Transaction tx = new Transaction(doc, "Apply Color"))
//                        {
//                            tx.Start();
//                            FieldDomainPointsByUV fieldPoints = new FieldDomainPointsByUV(pts);
//                            FieldValues fieldValues = new FieldValues(valuesAtPoints);
//                            int sfpIndex = sfm.AddSpatialFieldPrimitive(reference);
//                            sfm.UpdateSpatialFieldPrimitive(sfpIndex, fieldPoints, fieldValues, resultIndex);
//                            tx.Commit();
//                        }
//                    }
//                }

//                return Result.Succeeded;
//            }
//            catch (Exception ex)
//            {
//                message = ex.Message;
//                return Result.Failed;
//            }
//        }

//        private int GetOrCreateAnalysisResultSchema(SpatialFieldManager sfm, Document doc)
//        {
//            const string schemaName = "StaticColorSchema";
//            IList<int> registeredResults = sfm.GetRegisteredResults();

//            if (registeredResults.Count > 0)
//            {
//                return registeredResults[0];
//            }

//            int schemaIndex;
//            using (Transaction tx = new Transaction(doc, "Register Analysis Schema"))
//            {
//                tx.Start();
//                AnalysisResultSchema resultSchema = new AnalysisResultSchema(schemaName, "Static Color Schema for Surfaces");
//                schemaIndex = sfm.RegisterResult(resultSchema);
//                tx.Commit();
//            }

//            return schemaIndex;
//        }

//        private void CalculateFieldPointsAndValues(Face face, ref IList<UV> pts, ref IList<ValueAtPoint> valuesAtPoints)
//        {
//            BoundingBoxUV boundingBox = face.GetBoundingBox();
//            double umin = boundingBox.Min.U;
//            double umax = boundingBox.Max.U;
//            double vmin = boundingBox.Min.V;
//            double vmax = boundingBox.Max.V;

//            const int _width = 10;  // Sample grid width
//            const int _height = 10;  // Sample grid height

//            double du = (umax - umin) / _width;
//            double dv = (vmax - vmin) / _height;

//            double constantValue = 0.5;  // Value between 0 and 1.

//            for (int i = 0; i <= _width; i++)
//            {
//                for (int j = 0; j <= _height; j++)
//                {
//                    double u = umin + i * du;
//                    double v = vmin + j * dv;
//                    pts.Add(new UV(u, v));
//                    valuesAtPoints.Add(new ValueAtPoint(new double[] { constantValue }));
//                }
//            }
//        }




//    }
//}



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
    public class NEWDisplayAnalysis : IExternalCommand
    {
        const string _prompt = "Please pick faces to apply color";

        class BimElementFilter : ISelectionFilter
        {
            public bool AllowElement(Element e)
            {
                return null != e.Category && e.Category.HasMaterialQuantities;
            }

            public bool AllowReference(Reference r, XYZ p)
            {
                return true;
            }
        }

        void SetAnalysisDisplayStyle(Document doc)
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
                    AnalysisDisplayColoredSurfaceSettings coloredSurfaceSettings =
                        new AnalysisDisplayColoredSurfaceSettings();
                    coloredSurfaceSettings.ShowGridLines = false;

                    AnalysisDisplayColorSettings colorSettings =
                        new AnalysisDisplayColorSettings();
                    colorSettings.MaxColor = new Autodesk.Revit.DB.Color(0, 0, 255); // Blue
                    colorSettings.MinColor = new Autodesk.Revit.DB.Color(255, 255, 255); // White

                    AnalysisDisplayLegendSettings legendSettings =
                        new AnalysisDisplayLegendSettings();
                    legendSettings.NumberOfSteps = 10;
                    legendSettings.Rounding = 0.05;
                    legendSettings.ShowDataDescription = false;
                    legendSettings.ShowLegend = false; //set to true to show the legend

                    analysisDisplayStyle = AnalysisDisplayStyle.CreateAnalysisDisplayStyle(
                        doc, styleName, coloredSurfaceSettings, colorSettings, legendSettings);
                }

                doc.ActiveView.AnalysisDisplayStyleId = analysisDisplayStyle.Id;
                tx.Commit();
            }
        }

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                UIApplication uiapp = commandData.Application;
                UIDocument uidoc = uiapp.ActiveUIDocument;
                Document doc = uidoc.Document;
                View view = doc.ActiveView;

                IList<Reference> references = uidoc.Selection.PickObjects(ObjectType.Face, new BimElementFilter(), _prompt);
                SetAnalysisDisplayStyle(doc);

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

                foreach (Reference reference in references)
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

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }

        private int GetOrCreateAnalysisResultSchema(SpatialFieldManager sfm, Document doc)
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

        private void CalculateFieldPointsAndValues(Face face, ref IList<UV> pts, ref IList<ValueAtPoint> valuesAtPoints)
        {
            BoundingBoxUV boundingBox = face.GetBoundingBox();
            double umin = boundingBox.Min.U;
            double umax = boundingBox.Max.U;
            double vmin = boundingBox.Min.V;
            double vmax = boundingBox.Max.V;

            const int _width = 10;  // Sample grid width
            const int _height = 10;  // Sample grid height

            double du = (umax - umin) / _width;
            double dv = (vmax - vmin) / _height;

            double constantValue = 0.5;  // Value between 0 and 1.

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

        //handle applying the analysis to the provided faces defined in other codes for example in the Endgrain_AVF code
        //public void ApplyAnalysisToFaces(UIDocument uiDoc, IEnumerable<Reference> faceReferences)
        //{
        //    Document doc = uiDoc.Document;
        //    SetAnalysisDisplayStyle(doc);

        //    View view = doc.ActiveView;
        //    SpatialFieldManager sfm = SpatialFieldManager.GetSpatialFieldManager(view);
        //    if (sfm == null)
        //    {
        //        using (Transaction tx = new Transaction(doc, "Create Spatial Field Manager"))
        //        {
        //            tx.Start();
        //            sfm = SpatialFieldManager.CreateSpatialFieldManager(view, 1);
        //            tx.Commit();
        //        }
        //    }

        //    int resultIndex = GetOrCreateAnalysisResultSchema(sfm, doc);

        //    foreach (Reference reference in faceReferences)
        //    {
        //        ElementId elementId = reference.ElementId;
        //        Element element = doc.GetElement(elementId);
        //        GeometryObject geometryObject = element.GetGeometryObjectFromReference(reference);
        //        Face face = geometryObject as Face;

        //        if (face != null)
        //        {
        //            IList<UV> pts = new List<UV>();
        //            IList<ValueAtPoint> valuesAtPoints = new List<ValueAtPoint>();
        //            CalculateFieldPointsAndValues(face, ref pts, ref valuesAtPoints);

        //            using (Transaction tx = new Transaction(doc, "Apply Color"))
        //            {
        //                tx.Start();
        //                FieldDomainPointsByUV fieldPoints = new FieldDomainPointsByUV(pts);
        //                FieldValues fieldValues = new FieldValues(valuesAtPoints);
        //                int sfpIndex = sfm.AddSpatialFieldPrimitive(reference);
        //                sfm.UpdateSpatialFieldPrimitive(sfpIndex, fieldPoints, fieldValues, resultIndex);
        //                tx.Commit();
        //            }
        //        }
        //    }
        //}

        private bool IsElementVisibleInView(View view, Element element)
        {
            if (element == null || view == null) return false;

            BoundingBoxXYZ bb = element.get_BoundingBox(view);
            if (bb == null) return false;

            Outline outline = new Outline(bb.Min, bb.Max);
            BoundingBoxIntersectsFilter filter = new BoundingBoxIntersectsFilter(outline);

            FilteredElementCollector collector = new FilteredElementCollector(view.Document, view.Id);
            ICollection<ElementId> visibleElements = collector.WherePasses(filter).ToElementIds();

            return visibleElements.Contains(element.Id);
        }

        public void ApplyAnalysisToFaces(UIDocument uiDoc, IEnumerable<Reference> faceReferences)
        {
            Document doc = uiDoc.Document;
            SetAnalysisDisplayStyle(doc);

            View view = doc.ActiveView;
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

            foreach (Reference reference in faceReferences)
            {
                Element element = doc.GetElement(reference);

                // Skip if the element is null or not visible in the active view
                if (element == null || !IsElementVisibleInView(uiDoc.ActiveView, element))
                {
                    // Element is not visible or null
                    continue;
                }


                GeometryObject geoObject = element.GetGeometryObjectFromReference(reference);

                if (!(geoObject is Face face))
                {
                    continue;
                }

                IList<UV> pts = new List<UV>();
                IList<ValueAtPoint> valuesAtPoints = new List<ValueAtPoint>();
                CalculateFieldPointsAndValues(face, ref pts, ref valuesAtPoints);

                try
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
                catch (Autodesk.Revit.Exceptions.ArgumentException ex)
                {
                    // Log the exception or handle it appropriately
                    TaskDialog.Show("Error", $"Failed to apply color to face with ID {element.Id}. Error: {ex.Message}");
                }

            }
        }


    }
}

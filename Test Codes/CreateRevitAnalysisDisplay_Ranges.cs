using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Analysis;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;

namespace RevitWoodLCC
{
    [Transaction(TransactionMode.Manual)]
    public class CreateAnalysisDisplayTypeRanges : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            // Get the UIDocument and Document from the commandData object
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;

            try
            {
                // Select a face manually in the Revit UI before running this command
                Reference pickedRef = uidoc.Selection.PickObject(ObjectType.Face, "Please select a face.");
                if (pickedRef == null) return Result.Cancelled;

                ElementId elementId = pickedRef.ElementId;
                Element element = doc.GetElement(elementId);
                Face face = (Face)doc.GetElement(pickedRef).GetGeometryObjectFromReference(pickedRef);

                // Start a transaction to make changes to the document
                using (Transaction trans = new Transaction(doc, "Apply Color To Face"))
                {
                    trans.Start();

                    // Here, you would create the AnalysisDisplayStyle and apply it. 
                    // Simplified here for brevity. See below for CreateAnalysisDisplayStyle method.

                    // Assuming CreateAnalysisDisplayStyle is a method you've defined to create 
                    // an AnalysisDisplayStyle based on your requirements (e.g., with specific color settings)
                    AnalysisDisplayStyle style = CreateAnalysisDisplayStyle(doc);

                    // Apply style - this part is highly simplified and would actually require
                    // using the Analysis Visualization Framework (AVF) to apply to specific geometry

                    trans.Commit();
                }

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }

        private AnalysisDisplayStyle CreateAnalysisDisplayStyle(Document doc)
        {
            // Define colors
            Color redColor = new Color(255, 0, 0); // Red
            Color yellowColor = new Color(255, 255, 0); // Yellow

            // Define analysis display style settings (simplified for example purposes)
            AnalysisDisplayColoredSurfaceSettings surfaceSettings = new AnalysisDisplayColoredSurfaceSettings();
            AnalysisDisplayColorSettings colorSettings = new AnalysisDisplayColorSettings
            {
                ColorSettingsType = AnalysisDisplayStyleColorSettingsType.SolidColorRanges
            };

            // Define color entries (for simplicity, only defining two colors here)
            List<AnalysisDisplayColorEntry> colorEntries = new List<AnalysisDisplayColorEntry>
            {
                new AnalysisDisplayColorEntry(redColor),
                new AnalysisDisplayColorEntry(yellowColor)
            };
            colorSettings.SetIntermediateColors(colorEntries);

            // Define legend settings (optional, for display purposes)
            AnalysisDisplayLegendSettings legendSettings = new AnalysisDisplayLegendSettings
            {
                ShowLegend = false
            };

            // Create the analysis display style
            AnalysisDisplayStyle analysisDisplayStyle = AnalysisDisplayStyle.CreateAnalysisDisplayStyle(doc, "Custom Face Color Style", surfaceSettings, colorSettings, legendSettings);

            return analysisDisplayStyle;
        }
    }
}

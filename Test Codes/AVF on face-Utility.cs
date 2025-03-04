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
//    public class ApplyAVFtoFaces : IExternalCommand
//    {
//        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
//        {
//            UIApplication uiApp = commandData.Application;
//            UIDocument uiDoc = uiApp.ActiveUIDocument;
//            Document doc = uiDoc.Document;

//            try
//            {
//                // Prompt user to select face(s)
//                IList<Reference> selectedFaceRefs = uiDoc.Selection.PickObjects(ObjectType.Face, "Select face(s) to divide into grids.");
//                if (selectedFaceRefs.Count == 0)
//                {
//                    message = "No faces selected.";
//                    return Result.Cancelled;
//                }

//                // Create and show the WPF form for user to select visualization style
//                VisualizationStyleForm visualizationStyleForm = new VisualizationStyleForm();
//                if (visualizationStyleForm.ShowDialog() != true)
//                {
//                    message = "User cancelled the operation.";
//                    return Result.Cancelled;
//                }

//                // Get the selected visualization style
//                string selectedStyle = visualizationStyleForm.SelectedStyle;
//                bool useSolidColor = selectedStyle == "SolidColor";

//                using (Transaction tx = new Transaction(doc, "Divide Face and Assign Values"))
//                {
//                    tx.Start();

//                    foreach (Reference faceRef in selectedFaceRefs)
//                    {
//                        Face face = doc.GetElement(faceRef.SelectedElementId).GetGeometryObjectFromReference(faceRef) as Face;
//                        if (face != null)
//                        {
//                            DivideFaceIntoGridsAndAssignValues(doc, face, faceRef, useSolidColor);
//                        }
//                    }

//                    tx.Commit();
//                }

//                return Result.Succeeded;
//            }
//            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
//            {
//                message = "Operation canceled by the user.";
//                return Result.Cancelled;
//            }
//            catch (Exception ex)
//            {
//                message = $"Unexpected error occurred: {ex.Message}\n{ex.StackTrace}";
//                TaskDialog.Show("Error", message);
//                return Result.Failed;
//            }
//        }

//        private void DivideFaceIntoGridsAndAssignValues(Document doc, Face face, Reference faceRef, bool useSolidColor)
//        {
//            BoundingBoxUV bboxUV = face.GetBoundingBox();
//            double gridSize = 0.5; // Grid size in meters

//            int uDivisions = (int)Math.Ceiling((bboxUV.Max.U - bboxUV.Min.U) / gridSize);
//            int vDivisions = (int)Math.Ceiling((bboxUV.Max.V - bboxUV.Min.V) / gridSize);

//            List<GridCellInfo> gridCells = new List<GridCellInfo>();

//            for (int u = 0; u < uDivisions; u++)
//            {
//                for (int v = 0; v < vDivisions; v++)
//                {
//                    double minU = bboxUV.Min.U + u * gridSize;
//                    double minV = bboxUV.Min.V + v * gridSize;
//                    double maxU = minU + gridSize;
//                    double maxV = minV + gridSize;

//                    if (u == uDivisions - 1) maxU = bboxUV.Max.U;
//                    if (v == vDivisions - 1) maxV = bboxUV.Max.V;

//                    UV midUV = new UV((minU + maxU) / 2, (minV + maxV) / 2);
//                    XYZ midXYZ = face.Evaluate(midUV);

//                    GridCellInfo cellInfo = new GridCellInfo
//                    {
//                        MinUV = new UV(minU, minV),
//                        MaxUV = new UV(maxU, maxV),
//                        MidpointUV = midUV,
//                        MidpointXYZ = midXYZ,
//                        Value = midXYZ.Z
//                    };

//                    gridCells.Add(cellInfo);
//                }
//            }

//            double minZ = gridCells.Min(c => c.MidpointXYZ.Z);
//            double maxZ = gridCells.Max(c => c.MidpointXYZ.Z);

//            foreach (var cell in gridCells)
//            {
//                cell.NormalizedValue = (cell.Value - minZ) / (maxZ - minZ);
//            }

//            ApplyAVFToFace(doc, faceRef, gridCells, useSolidColor);
//        }

//        private void ApplyAVFToFace(Document doc, Reference faceRef, List<GridCellInfo> gridCells, bool useSolidColor)
//        {
//            SpatialFieldManager sfm = SpatialFieldManager.GetSpatialFieldManager(doc.ActiveView);
//            if (sfm == null)
//            {
//                sfm = SpatialFieldManager.CreateSpatialFieldManager(doc.ActiveView, 1);
//            }

//            double maxValue = gridCells.Max(c => c.NormalizedValue);
//            AnalysisDisplayStyle analysisDisplayStyle = CreateAnalysisDisplayStyle(doc, maxValue, useSolidColor);
//            doc.ActiveView.AnalysisDisplayStyleId = analysisDisplayStyle.Id;

//            IList<ValueAtPoint> valueList = new List<ValueAtPoint>();
//            IList<UV> uvPoints = new List<UV>();

//            foreach (GridCellInfo cell in gridCells)
//            {
//                uvPoints.Add(cell.MidpointUV);
//                valueList.Add(new ValueAtPoint(new List<double> { cell.NormalizedValue }));
//            }

//            int primitiveId = sfm.AddSpatialFieldPrimitive(faceRef);
//            FieldDomainPointsByUV fieldPoints = new FieldDomainPointsByUV(uvPoints);
//            FieldValues fieldValues = new FieldValues(valueList);
//            int schemaIndex = GetOrCreateAnalysisResultSchemaIndex(sfm, "Height-Based Schema", "Height-based value visualization");

//            sfm.UpdateSpatialFieldPrimitive(primitiveId, fieldPoints, fieldValues, schemaIndex);
//        }

//        private AnalysisDisplayStyle CreateAnalysisDisplayStyle(Document doc, double maxValue, bool useSolidColor)
//        {
//            string styleName = useSolidColor ? "SolidColor Height-Based Analysis Display Style" : "Gradient Height-Based Analysis Display Style";

//            AnalysisDisplayStyle existingStyle = new FilteredElementCollector(doc)
//                .OfClass(typeof(AnalysisDisplayStyle))
//                .Cast<AnalysisDisplayStyle>()
//                .FirstOrDefault(a => a.Name.Equals(styleName));

//            if (existingStyle != null) return existingStyle;

//            AnalysisDisplayColorSettings colorSettings = new AnalysisDisplayColorSettings
//            {
//                ColorSettingsType = useSolidColor ? AnalysisDisplayStyleColorSettingsType.SolidColorRanges : AnalysisDisplayStyleColorSettingsType.GradientColor
//            };

//            List<AnalysisDisplayColorEntry> colorEntries = new List<AnalysisDisplayColorEntry>
//            {
//                new AnalysisDisplayColorEntry(new Autodesk.Revit.DB.Color(0, 0, 255), 0.0), // Blue for lowest values
//                new AnalysisDisplayColorEntry(new Autodesk.Revit.DB.Color(0, 255, 0), maxValue * 0.5), // Green for medium values
//                new AnalysisDisplayColorEntry(new Autodesk.Revit.DB.Color(255, 0, 0), maxValue) // Red for highest values
//            };

//            if (useSolidColor)
//            {
//                colorSettings.SetIntermediateColors(new List<AnalysisDisplayColorEntry> {
//                    new AnalysisDisplayColorEntry(new Autodesk.Revit.DB.Color(0, 0, 255), 0.0),
//                    new AnalysisDisplayColorEntry(new Autodesk.Revit.DB.Color(0, 255, 0), maxValue * 0.33),
//                    new AnalysisDisplayColorEntry(new Autodesk.Revit.DB.Color(255, 255, 0), maxValue * 0.66),
//                    new AnalysisDisplayColorEntry(new Autodesk.Revit.DB.Color(255, 0, 0), maxValue)
//                });
//            }
//            else
//            {
//                colorSettings.SetIntermediateColors(colorEntries);
//            }

//            AnalysisDisplayColoredSurfaceSettings surfaceSettings = new AnalysisDisplayColoredSurfaceSettings
//            {
//                ShowGridLines = false
//            };

//            AnalysisDisplayLegendSettings legendSettings = new AnalysisDisplayLegendSettings
//            {
//                ShowLegend = true,
//                NumberOfSteps = 10,
//                ShowDataDescription = true,
//                Rounding = 0.01,
//                ShowUnits = true,
//                NumberForScale = maxValue,
//                ShowDataName = true,
//            };

//            return AnalysisDisplayStyle.CreateAnalysisDisplayStyle(doc, styleName, surfaceSettings, colorSettings, legendSettings);
//        }

//        private int GetOrCreateAnalysisResultSchemaIndex(SpatialFieldManager sfm, string schemaName, string schemaDescription)
//        {
//            IList<int> existingSchemaIndices = sfm.GetRegisteredResults();
//            foreach (int index in existingSchemaIndices)
//            {
//                AnalysisResultSchema existingSchema = sfm.GetResultSchema(index);
//                if (existingSchema.Name == schemaName && existingSchema.Description == schemaDescription)
//                {
//                    return index;
//                }
//            }

//            AnalysisResultSchema schema = new AnalysisResultSchema(schemaName, schemaDescription);
//            schema.SetUnits(new List<string> { "Height" }, new List<double> { 1.0 });

//            return sfm.RegisterResult(schema);
//        }

//        public class GridCellInfo
//        {
//            public UV MinUV { get; set; }
//            public UV MaxUV { get; set; }
//            public UV MidpointUV { get; set; }
//            public XYZ MidpointXYZ { get; set; }
//            public double Value { get; set; }
//            public double NormalizedValue { get; set; }
//        }
//    }

//    public class VisualizationStyleForm : System.Windows.Window
//    {
//        public string SelectedStyle { get; private set; }
//        private System.Windows.Controls.ComboBox comboBox;

//        public VisualizationStyleForm()
//        {
//            Title = "Select Visualization Style";
//            Width = 300;
//            Height = 150;

//            System.Windows.Controls.StackPanel stackPanel = new System.Windows.Controls.StackPanel();

//            System.Windows.Controls.Label label = new System.Windows.Controls.Label
//            {
//                Content = "Select Visualization Style:"
//            };
//            stackPanel.Children.Add(label);

//            comboBox = new System.Windows.Controls.ComboBox();
//            comboBox.Items.Add("Gradient");
//            comboBox.Items.Add("SolidColor");
//            stackPanel.Children.Add(comboBox);

//            System.Windows.Controls.Button button = new System.Windows.Controls.Button
//            {
//                Content = "OK",
//                Width = 100,
//                Height = 30,
//                Margin = new System.Windows.Thickness(10)
//            };
//            button.Click += Button_Click;
//            stackPanel.Children.Add(button);

//            Content = stackPanel;
//        }

//        private void Button_Click(object sender, System.Windows.RoutedEventArgs e)
//        {
//            SelectedStyle = comboBox.SelectedItem as string;
//            DialogResult = true;
//            Close();
//        }
//    }
//}


//////More Advanced Example using WDR

////using Autodesk.Revit.Attributes;
////using Autodesk.Revit.DB;
////using Autodesk.Revit.DB.Analysis;
////using Autodesk.Revit.UI;
////using Autodesk.Revit.UI.Selection;
////using System;
////using System.Collections.Generic;
////using System.Linq;

////namespace RevitWoodLCC
////{
////    [Transaction(TransactionMode.Manual)]
////    public class DivideFaceAndAssignValues : IExternalCommand
////    {
////        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
////        {
////            UIApplication uiApp = commandData.Application;
////            UIDocument uiDoc = uiApp.ActiveUIDocument;
////            Document doc = uiDoc.Document;

////            try
////            {
////                // Prompt user to select face(s)
////                IList<Reference> selectedFaceRefs = uiDoc.Selection.PickObjects(ObjectType.Face, "Select face(s) to divide into grids.");
////                if (selectedFaceRefs.Count == 0)
////                {
////                    message = "No faces selected.";
////                    return Result.Cancelled;
////                }

////                // Create and show the WPF form for user to select visualization style
////                VisualizationStyleForm visualizationStyleForm = new VisualizationStyleForm();
////                if (visualizationStyleForm.ShowDialog() != true)
////                {
////                    message = "User cancelled the operation.";
////                    return Result.Cancelled;
////                }

////                // Get the selected visualization style
////                string selectedStyle = visualizationStyleForm.SelectedStyle;
////                bool useSolidColor = selectedStyle == "SolidColor";

////                using (Transaction tx = new Transaction(doc, "Divide Face and Assign Values"))
////                {
////                    tx.Start();

////                    // Ensure the active view is appropriate
////                    View3D view3D = doc.ActiveView as View3D;
////                    if (view3D == null || !view3D.IsSectionBoxActive)
////                    {
////                        message = "Please make sure you are in a 3D view with an active section box.";
////                        return Result.Failed;
////                    }

////                    BoundingBoxXYZ sectionBox = view3D.GetSectionBox();

////                    foreach (Reference faceRef in selectedFaceRefs)
////                    {
////                        Face face = doc.GetElement(faceRef.SelectedElementId).GetGeometryObjectFromReference(faceRef) as Face;
////                        if (face != null)
////                        {
////                            DivideFaceIntoGridsAndAssignValues(doc, face, faceRef, useSolidColor, sectionBox);
////                        }
////                    }

////                    tx.Commit();
////                }

////                return Result.Succeeded;
////            }
////            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
////            {
////                message = "Operation canceled by the user.";
////                return Result.Cancelled;
////            }
////            catch (Exception ex)
////            {
////                message = $"Unexpected error occurred: {ex.Message}\n{ex.StackTrace}";
////                TaskDialog.Show("Error", message);
////                return Result.Failed;
////            }
////        }

////        private void DivideFaceIntoGridsAndAssignValues(Document doc, Face face, Reference faceRef, bool useSolidColor, BoundingBoxXYZ sectionBox)
////        {
////            BoundingBoxUV bboxUV = face.GetBoundingBox();
////            double gridSize = 0.5; // Grid size in meters

////            int uDivisions = (int)Math.Ceiling((bboxUV.Max.U - bboxUV.Min.U) / gridSize);
////            int vDivisions = (int)Math.Ceiling((bboxUV.Max.V - bboxUV.Min.V) / gridSize);

////            List<UVGridCellInfo> gridCells = new List<UVGridCellInfo>();

////            // Constants for WDR calculation
////            double C_T = 1.0;
////            double C_R = 1.0;
////            double O = 1.0;
////            double W = 1.0;
////            double v_ref = 10.0; // Reference wind speed in m/s
////            double h_ref = 10.0; // Reference height in meters
////            double alpha = 0.14;
////            double R_h = 1.0; // Rain intensity
////            double D = 45.0; // Wind direction in degrees
////            double theta = 0.0; // Face orientation (assuming 0 for simplicity)

////            for (int u = 0; u < uDivisions; u++)
////            {
////                for (int v = 0; v < vDivisions; v++)
////                {
////                    double minU = bboxUV.Min.U + u * gridSize;
////                    double minV = bboxUV.Min.V + v * gridSize;
////                    double maxU = minU + gridSize;
////                    double maxV = minV + gridSize;

////                    if (u == uDivisions - 1) maxU = bboxUV.Max.U;
////                    if (v == vDivisions - 1) maxV = bboxUV.Max.V;

////                    UV midUV = new UV((minU + maxU) / 2, (minV + maxV) / 2);
////                    XYZ midXYZ = face.Evaluate(midUV);

////                    // Calculate the vertical distance from the midpoint to the base of the section box
////                    double gridcellHeight = midXYZ.Z - sectionBox.Min.Z;

////                    // Calculate wind speed at the grid cell height using the power law profile
////                    double windSpeedAtGridCell = v_ref * Math.Pow((gridcellHeight / h_ref), alpha);

////                    // Calculate WDR using the specific wind speed for the grid cell
////                    double R_wdr = WDRSimulationUtilities.CalculateWDR(C_T, C_R, O, W, windSpeedAtGridCell, R_h, D, theta);

////                    UVGridCellInfo cellInfo = new UVGridCellInfo
////                    {
////                        Id = $"{faceRef.ConvertToStableRepresentation(doc)}_Grid_{u}_{v}",
////                        MinUV = new UV(minU, minV),
////                        MaxUV = new UV(maxU, maxV),
////                        Hits = 0,
////                        MidpointUV = midUV,
////                        MidpointXYZ = midXYZ,
////                        CellheightfromGround = gridcellHeight,
////                        GridcellWindSpeed = windSpeedAtGridCell,
////                        GridcellWDR = R_wdr,
////                        Theta = theta
////                    };

////                    gridCells.Add(cellInfo);
////                }
////            }

////            ApplyAVFToFace(doc, faceRef, gridCells, useSolidColor);
////        }

////        private void ApplyAVFToFace(Document doc, Reference faceRef, List<UVGridCellInfo> gridCells, bool useSolidColor)
////        {
////            SpatialFieldManager sfm = SpatialFieldManager.GetSpatialFieldManager(doc.ActiveView);
////            if (sfm == null)
////            {
////                sfm = SpatialFieldManager.CreateSpatialFieldManager(doc.ActiveView, 1);
////            }

////            double maxValue = gridCells.Max(c => c.GridcellWDR);
////            double minValue = gridCells.Min(c => c.GridcellWDR);
////            TaskDialog.Show("Debug", $"Max WDR Value: {maxValue}\nMin WDR Value: {minValue}");
////            AnalysisDisplayStyle analysisDisplayStyle = CreateAnalysisDisplayStyle(doc, minValue, maxValue, useSolidColor);
////            doc.ActiveView.AnalysisDisplayStyleId = analysisDisplayStyle.Id;

////            IList<ValueAtPoint> valueList = new List<ValueAtPoint>();
////            IList<UV> uvPoints = new List<UV>();

////            foreach (UVGridCellInfo cell in gridCells)
////            {
////                uvPoints.Add(cell.MidpointUV);
////                valueList.Add(new ValueAtPoint(new List<double> { cell.GridcellWDR }));
////            }

////            int primitiveId = sfm.AddSpatialFieldPrimitive(faceRef);
////            FieldDomainPointsByUV fieldPoints = new FieldDomainPointsByUV(uvPoints);
////            FieldValues fieldValues = new FieldValues(valueList);
////            int schemaIndex = GetOrCreateAnalysisResultSchemaIndex(sfm, "Height-Based Schema", "Height-based value visualization");

////            sfm.UpdateSpatialFieldPrimitive(primitiveId, fieldPoints, fieldValues, schemaIndex);
////        }

////        private AnalysisDisplayStyle CreateAnalysisDisplayStyle(Document doc, double minValue, double maxValue, bool useSolidColor)
////        {
////            string styleName = useSolidColor ? "SolidColor Height-Based Analysis Display Style" : "Gradient Height-Based Analysis Display Style";

////            AnalysisDisplayStyle existingStyle = new FilteredElementCollector(doc)
////                .OfClass(typeof(AnalysisDisplayStyle))
////                .Cast<AnalysisDisplayStyle>()
////                .FirstOrDefault(a => a.Name.Equals(styleName));

////            if (existingStyle != null) return existingStyle;

////            AnalysisDisplayColorSettings colorSettings = new AnalysisDisplayColorSettings
////            {
////                ColorSettingsType = useSolidColor ? AnalysisDisplayStyleColorSettingsType.SolidColorRanges : AnalysisDisplayStyleColorSettingsType.GradientColor
////            };

////            List<AnalysisDisplayColorEntry> colorEntries = new List<AnalysisDisplayColorEntry>
////            {
////                new AnalysisDisplayColorEntry(new Autodesk.Revit.DB.Color(0, 0, 255), minValue), // Blue for lowest values
////                new AnalysisDisplayColorEntry(new Autodesk.Revit.DB.Color(0, 255, 0), minValue + (maxValue - minValue) * 0.25), // Green for low-mid values
////                new AnalysisDisplayColorEntry(new Autodesk.Revit.DB.Color(255, 255, 0), minValue + (maxValue - minValue) * 0.5), // Yellow for medium values
////                new AnalysisDisplayColorEntry(new Autodesk.Revit.DB.Color(255, 165, 0), minValue + (maxValue - minValue) * 0.75), // Orange for high-mid values
////                new AnalysisDisplayColorEntry(new Autodesk.Revit.DB.Color(255, 0, 0), maxValue) // Red for highest values
////            };

////            colorSettings.SetIntermediateColors(colorEntries);

////            AnalysisDisplayColoredSurfaceSettings surfaceSettings = new AnalysisDisplayColoredSurfaceSettings
////            {
////                ShowGridLines = false
////            };

////            AnalysisDisplayLegendSettings legendSettings = new AnalysisDisplayLegendSettings
////            {
////                ShowLegend = true,
////                NumberOfSteps = 5,
////                ShowDataDescription = true,
////                Rounding = 0.01,
////                ShowUnits = true,
////                NumberForScale = maxValue,
////                ShowDataName = true,
////            };

////            return AnalysisDisplayStyle.CreateAnalysisDisplayStyle(doc, styleName, surfaceSettings, colorSettings, legendSettings);
////        }

////        private int GetOrCreateAnalysisResultSchemaIndex(SpatialFieldManager sfm, string schemaName, string schemaDescription)
////        {
////            IList<int> existingSchemaIndices = sfm.GetRegisteredResults();
////            foreach (int index in existingSchemaIndices)
////            {
////                AnalysisResultSchema existingSchema = sfm.GetResultSchema(index);
////                if (existingSchema.Name == schemaName && existingSchema.Description == schemaDescription)
////                {
////                    return index;
////                }
////            }

////            AnalysisResultSchema schema = new AnalysisResultSchema(schemaName, schemaDescription);
////            schema.SetUnits(new List<string> { "Height" }, new List<double> { 1.0 });

////            return sfm.RegisterResult(schema);
////        }

////        public class UVGridCellInfo
////        {
////            public string Id { get; set; }
////            public UV MinUV { get; set; }
////            public UV MaxUV { get; set; }
////            public UV MidpointUV { get; set; }
////            public XYZ MidpointXYZ { get; set; }
////            public double CellheightfromGround { get; set; }
////            public double GridcellWindSpeed { get; set; }
////            public double GridcellWDR { get; set; }
////            public double Theta { get; set; }
////            public int Hits { get; set; }
////        }
////    }

////    public class VisualizationStyleForm : System.Windows.Window
////    {
////        public string SelectedStyle { get; private set; }
////        private System.Windows.Controls.ComboBox comboBox;

////        public VisualizationStyleForm()
////        {
////            Title = "Select Visualization Style";
////            Width = 300;
////            Height = 150;

////            System.Windows.Controls.StackPanel stackPanel = new System.Windows.Controls.StackPanel();

////            System.Windows.Controls.Label label = new System.Windows.Controls.Label
////            {
////                Content = "Select Visualization Style:"
////            };
////            stackPanel.Children.Add(label);

////            comboBox = new System.Windows.Controls.ComboBox();
////            comboBox.Items.Add("Gradient");
////            comboBox.Items.Add("SolidColor");
////            stackPanel.Children.Add(comboBox);

////            System.Windows.Controls.Button button = new System.Windows.Controls.Button
////            {
////                Content = "OK",
////                Width = 100,
////                Height = 30,
////                Margin = new System.Windows.Thickness(10)
////            };
////            button.Click += Button_Click;
////            stackPanel.Children.Add(button);

////            Content = stackPanel;
////        }

////        private void Button_Click(object sender, System.Windows.RoutedEventArgs e)
////        {
////            SelectedStyle = comboBox.SelectedItem as string;
////            DialogResult = true;
////            Close();
////        }
////    }

////    public static class WDRSimulationUtilities
////    {
////        public static double CalculateWDR(double C_T, double C_R, double O, double W, double v, double R_h, double D, double theta)
////        {
////            double D_rad = D * (Math.PI / 180.0);
////            double theta_rad = theta * (Math.PI / 180.0);
////            double cosTerm = Math.Cos(D_rad - theta_rad);
////            double R_wdr = (2.0 / 9.0) * C_T * C_R * O * W * v * Math.Pow(R_h, 8.0 / 9.0) * cosTerm;
////            return R_wdr;
////        }
////    }
////}



//using Autodesk.Revit.DB;
//using Autodesk.Revit.DB.Analysis;
//using Autodesk.Revit.UI;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using static RevitWoodLCC.DivideFaceAndAssignValues;

//namespace RevitWoodLCC
//{
//    public static class wdrUtility
//    {
//        public static void ApplyAVFToFace(Document doc, Reference faceRef, List<UVGridCellInfo> gridCells, bool useSolidColor, double maxValue)
//        {
//            SpatialFieldManager sfm = SpatialFieldManager.GetSpatialFieldManager(doc.ActiveView);
//            if (sfm == null)
//            {
//                sfm = SpatialFieldManager.CreateSpatialFieldManager(doc.ActiveView, 1);
//            }

//            double minValue = 0; // min value is now zero
//            TaskDialog.Show("Debug", $"Max WDR Value: {maxValue}\nMin WDR Value: {minValue}");
//            AnalysisDisplayStyle analysisDisplayStyle = CreateAnalysisDisplayStyle(doc, minValue, maxValue, useSolidColor);
//            doc.ActiveView.AnalysisDisplayStyleId = analysisDisplayStyle.Id;

//            IList<ValueAtPoint> valueList = new List<ValueAtPoint>();
//            IList<UV> uvPoints = new List<UV>();

//            foreach (UVGridCellInfo cell in gridCells)
//            {
//                uvPoints.Add(cell.MidpointUV);
//                valueList.Add(new ValueAtPoint(new List<double> { cell.GridcellWDR }));
//            }

//            int primitiveId = sfm.AddSpatialFieldPrimitive(faceRef);
//            FieldDomainPointsByUV fieldPoints = new FieldDomainPointsByUV(uvPoints);
//            FieldValues fieldValues = new FieldValues(valueList);
//            int schemaIndex = GetOrCreateAnalysisResultSchemaIndex(sfm, "Height-Based Schema", "Height-based value visualization");

//            sfm.UpdateSpatialFieldPrimitive(primitiveId, fieldPoints, fieldValues, schemaIndex);
//        }

//        public static AnalysisDisplayStyle CreateAnalysisDisplayStyle(Document doc, double minValue, double maxValue, bool useSolidColor)
//        {
//            string styleName = useSolidColor ? "SolidColor Height-Based Analysis Display Style" : "Gradient Height-Based Analysis Display Style";

//            AnalysisDisplayStyle existingStyle = new FilteredElementCollector(doc)
//                .OfClass(typeof(AnalysisDisplayStyle))
//                .Cast<AnalysisDisplayStyle>()
//                .FirstOrDefault(a => a.Name.Equals(styleName));

//            if (existingStyle != null) return existingStyle;

//            AnalysisDisplayColorSettings colorSettings = new AnalysisDisplayColorSettings
//            {
//                ColorSettingsType = useSolidColor ? AnalysisDisplayStyleColorSettingsType.SolidColorRanges : AnalysisDisplayStyleColorSettingsType.GradientColor
//            };

//            List<AnalysisDisplayColorEntry> colorEntries = new List<AnalysisDisplayColorEntry>
//            {
//                new AnalysisDisplayColorEntry(new Autodesk.Revit.DB.Color(0, 0, 255), minValue), // Blue for lowest values
//                new AnalysisDisplayColorEntry(new Autodesk.Revit.DB.Color(0, 255, 0), minValue + (maxValue - minValue) * 0.25), // Green for low-mid values
//                new AnalysisDisplayColorEntry(new Autodesk.Revit.DB.Color(255, 255, 0), minValue + (maxValue - minValue) * 0.5), // Yellow for medium values
//                new AnalysisDisplayColorEntry(new Autodesk.Revit.DB.Color(255, 165, 0), minValue + (maxValue - minValue) * 0.75), // Orange for high-mid values
//                new AnalysisDisplayColorEntry(new Autodesk.Revit.DB.Color(255, 0, 0), maxValue) // Red for highest values
//            };

//            colorSettings.SetIntermediateColors(colorEntries);

//            AnalysisDisplayColoredSurfaceSettings surfaceSettings = new AnalysisDisplayColoredSurfaceSettings
//            {
//                ShowGridLines = false
//            };

//            AnalysisDisplayLegendSettings legendSettings = new AnalysisDisplayLegendSettings
//            {
//                ShowLegend = true,
//                NumberOfSteps = 5,
//                ShowDataDescription = true,
//                Rounding = 0.01,
//                ShowUnits = true,
//                NumberForScale = maxValue,
//                ShowDataName = true,
//            };

//            return AnalysisDisplayStyle.CreateAnalysisDisplayStyle(doc, styleName, surfaceSettings, colorSettings, legendSettings);
//        }

//        public static int GetOrCreateAnalysisResultSchemaIndex(SpatialFieldManager sfm, string schemaName, string schemaDescription)
//        {
//            IList<int> existingSchemaIndices = sfm.GetRegisteredResults();
//            foreach (int index in existingSchemaIndices)
//            {
//                AnalysisResultSchema existingSchema = sfm.GetResultSchema(index);
//                if (existingSchema.Name == schemaName && existingSchema.Description == schemaDescription)
//                {
//                    return index;
//                }
//            }

//            AnalysisResultSchema schema = new AnalysisResultSchema(schemaName, schemaDescription);
//            schema.SetUnits(new List<string> { "Height" }, new List<double> { 1.0 });

//            return sfm.RegisterResult(schema);
//        }
//    }
//}

////only assigned to edges 
//using Autodesk.Revit.Attributes;
//using Autodesk.Revit.DB;
//using Autodesk.Revit.UI;
//using Autodesk.Revit.UI.Selection;
//using System;
//using System.Collections.Generic;

//namespace RevitWoodLCC
//{
//    [Transaction(TransactionMode.Manual)]
//    public class ApplyColorToElements : IExternalCommand
//    {
//        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
//        {
//            UIApplication uiApp = commandData.Application;
//            UIDocument uiDoc = uiApp.ActiveUIDocument;
//            Document doc = uiDoc.Document;
//            View activeView = doc.ActiveView;

//            try
//            {
//                // Prompt user to select elements
//                IList<Reference> selectedElementRefs = uiDoc.Selection.PickObjects(ObjectType.Element, "Select elements to color red.");
//                if (selectedElementRefs.Count == 0)
//                {
//                    message = "No elements selected.";
//                    return Result.Cancelled;
//                }

//                using (Transaction tx = new Transaction(doc, "Color Elements Red"))
//                {
//                    tx.Start();

//                    // Ensure the visual style is set to Shading or Shading with Edges
//                    if (activeView is View3D view3D)
//                    {
//                        view3D.DisplayStyle = DisplayStyle.ShadingWithEdges;
//                    }
//                    else if (activeView is ViewPlan viewPlan)
//                    {
//                        viewPlan.DisplayStyle = DisplayStyle.ShadingWithEdges;
//                    }

//                    foreach (Reference elementRef in selectedElementRefs)
//                    {
//                        Element element = doc.GetElement(elementRef.SelectedElementId);
//                        if (element != null)
//                        {
//                            ApplyRedColorToElement(doc, element);
//                        }
//                    }

//                    tx.Commit();
//                }

//                return Result.Succeeded;
//            }
//            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
//            {
//                message = "Operation canceled by the user.";
//                return Result.Cancelled;
//            }
//            catch (Exception ex)
//            {
//                message = $"Unexpected error occurred: {ex.Message}\n{ex.StackTrace}";
//                TaskDialog.Show("Error", message);
//                return Result.Failed;
//            }
//        }

//        private void ApplyRedColorToElement(Document doc, Element element)
//        {
//            // Create OverrideGraphicSettings
//            OverrideGraphicSettings ogs = new OverrideGraphicSettings();

//            // Define the red color
//            Autodesk.Revit.DB.Color redColor = new Autodesk.Revit.DB.Color(255, 0, 0); // Red color
//            ogs.SetProjectionLineColor(redColor); // Set the color for the projection lines (edges)
//            ogs.SetSurfaceForegroundPatternColor(redColor); // Set the color for the faces
//            ogs.SetSurfaceBackgroundPatternColor(redColor); // Set the background color for the faces
//            ogs.SetSurfaceForegroundPatternId(SelectedElementId.InvalidElementId); // Use the default surface pattern

//            // Apply the overrides to the element in the active view
//            try
//            {
//                doc.ActiveView.SetElementOverrides(element.Id, ogs);
//            }
//            catch (Exception ex)
//            {
//                TaskDialog.Show("Error", $"Failed to apply color to element {element.Id}: {ex.Message}");
//            }
//        }
//    }
//}


////this code works perfect by applying color to selected faces using AVF
//using Autodesk.Revit.DB;
//using Autodesk.Revit.DB.Analysis;
//using Autodesk.Revit.UI;
//using Autodesk.Revit.UI.Selection;
//using System;
//using System.Collections.Generic;
//using System.Linq;

//namespace RevitWoodLCC
//{
//    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
//    public class SetElementSurfaceColorCommand : IExternalCommand
//    {
//        public Result Execute(
//            ExternalCommandData commandData,
//            ref string message,
//            ElementSet elements)
//        {
//            UIApplication uiApp = commandData.Application;
//            Document doc = uiApp.ActiveUIDocument.Document;
//            UIDocument uidoc = uiApp.ActiveUIDocument;

//            try
//            {
//                // Allow user to select faces
//                IList<Reference> selectedFaces = uidoc.Selection.PickObjects(ObjectType.Face, "Select faces to color");

//                if (selectedFaces == null || selectedFaces.Count == 0)
//                {
//                    TaskDialog.Show("Error", "No faces selected.");
//                    return Result.Failed;
//                }

//                using (Transaction trans = new Transaction(doc, "Set Face Surface Color"))
//                {
//                    trans.Start();

//                    View view = doc.ActiveView;
//                    SpatialFieldManager sfm = GetOrCreateSpatialFieldManager(view);

//                    AnalysisDisplayStyle analysisDisplayStyle = CreateAnalysisDisplayStyle(doc, "Red Surface Style");
//                    view.AnalysisDisplayStyleId = analysisDisplayStyle.Id;

//                    int schemaIndex = GetOrCreateAnalysisResultSchemaIndex(sfm, "Custom Data Schema", "Custom Data Description");

//                    foreach (Reference faceRef in selectedFaces)
//                    {
//                        Element element = doc.GetElement(faceRef.SelectedElementId);
//                        GeometryObject geomObject = element.GetGeometryObjectFromReference(faceRef);

//                        if (geomObject is Face face)
//                        {
//                            IList<UV> uvPoints;
//                            FieldDomainPointsByUV fieldPoints = GetFieldDomainPointsByUV(face, out uvPoints);
//                            FieldValues fieldValues = GetFieldValuesForUVPoints(uvPoints, face);

//                            if (faceRef != null)
//                            {
//                                int primitiveId = sfm.AddSpatialFieldPrimitive(faceRef);
//                                sfm.UpdateSpatialFieldPrimitive(primitiveId, fieldPoints, fieldValues, schemaIndex);
//                                TaskDialog.Show("Debug", "Spatial field primitive added and updated");
//                            }
//                            else
//                            {
//                                TaskDialog.Show("Error", "Face reference is null.");
//                                return Result.Failed;
//                            }
//                        }
//                    }

//                    trans.Commit();
//                }

//                return Result.Succeeded;
//            }
//            catch (Exception ex)
//            {
//                message = ex.Message;
//                TaskDialog.Show("Exception", ex.ToString());
//                return Result.Failed;
//            }
//        }

//        public static AnalysisDisplayStyle CreateAnalysisDisplayStyle(Document doc, string styleName)
//        {
//            // Attempt to find an existing style that matches the provided styleName
//            var existingStyle = new FilteredElementCollector(doc)
//                .OfClass(typeof(AnalysisDisplayStyle))
//                .Cast<AnalysisDisplayStyle>()
//                .FirstOrDefault(style => style.Name.Equals(styleName));

//            // If an existing style is found, return it
//            if (existingStyle != null)
//            {
//                TaskDialog.Show("Debug", $"Existing style found: {styleName}");
//                return existingStyle;
//            }

//            AnalysisDisplayLegendSettings legendSettings = new AnalysisDisplayLegendSettings
//            {
//                ShowLegend = true,
//                NumberOfSteps = 2,
//                ShowDataDescription = true,
//                Rounding = 1
//            };

//            AnalysisDisplayColoredSurfaceSettings surfaceSettings = new AnalysisDisplayColoredSurfaceSettings
//            {
//                ShowGridLines = false
//            };

//            AnalysisDisplayColorSettings colorSettings = new AnalysisDisplayColorSettings
//            {
//                ColorSettingsType = AnalysisDisplayStyleColorSettingsType.SolidColorRanges,
//                MinColor = new Autodesk.Revit.DB.Color(255, 0, 0), // Red color
//                MaxColor = new Autodesk.Revit.DB.Color(255, 0, 0)  // Red color
//            };

//            var newStyle = AnalysisDisplayStyle.CreateAnalysisDisplayStyle(doc, styleName, surfaceSettings, colorSettings, legendSettings);
//            TaskDialog.Show("Debug", $"New style created: {styleName}");
//            return newStyle;
//        }

//        public static SpatialFieldManager GetOrCreateSpatialFieldManager(View view)
//        {
//            SpatialFieldManager sfm = SpatialFieldManager.GetSpatialFieldManager(view);
//            if (sfm == null)
//            {
//                sfm = SpatialFieldManager.CreateSpatialFieldManager(view, 1);
//                TaskDialog.Show("Debug", "New SpatialFieldManager created.");
//            }
//            else
//            {
//                TaskDialog.Show("Debug", "Existing SpatialFieldManager found.");
//            }
//            return sfm;
//        }

//        public static int GetOrCreateAnalysisResultSchemaIndex(SpatialFieldManager sfm, string schemaName, string schemaDescription)
//        {
//            IList<int> existingSchemaIndices = sfm.GetRegisteredResults();
//            foreach (int index in existingSchemaIndices)
//            {
//                AnalysisResultSchema existingSchema = sfm.GetResultSchema(index);
//                if (existingSchema.Name == schemaName && existingSchema.Description == schemaDescription)
//                {
//                    TaskDialog.Show("Debug", $"Existing schema found: {schemaName}");
//                    return index;
//                }
//            }

//            AnalysisResultSchema schema = new AnalysisResultSchema(schemaName, schemaDescription);
//            schema.SetUnits(new List<string> { "Custom Data" }, new List<double> { 1.0 });

//            int newIndex = sfm.RegisterResult(schema);
//            TaskDialog.Show("Debug", $"New schema registered: {schemaName}");
//            return newIndex;
//        }

//        public static FieldDomainPointsByUV GetFieldDomainPointsByUV(Face face, out IList<UV> uvPoints)
//        {
//            uvPoints = new List<UV>();
//            BoundingBoxUV bbox = face.GetBoundingBox();
//            double uStep = (bbox.Max.U - bbox.Min.U) / 10; // Adjust step sizes as needed
//            double vStep = (bbox.Max.V - bbox.Min.V) / 10;

//            for (double u = bbox.Min.U; u <= bbox.Max.U; u += uStep)
//            {
//                for (double v = bbox.Min.V; v <= bbox.Max.V; v += vStep)
//                {
//                    UV uv = new UV(u, v);
//                    if (face.IsInside(uv))
//                    {
//                        uvPoints.Add(uv);
//                    }
//                }
//            }

//            TaskDialog.Show("Debug", $"Generated {uvPoints.Count} UV points for the face.");
//            return new FieldDomainPointsByUV(uvPoints);
//        }

//        public static FieldValues GetFieldValuesForUVPoints(IList<UV> uvPoints, Face face)
//        {
//            List<ValueAtPoint> values = new List<ValueAtPoint>();
//            foreach (UV uv in uvPoints)
//            {
//                double value = 1.0; // Arbitrary value for demonstration, modify as needed
//                values.Add(new ValueAtPoint(new List<double> { value }));
//            }

//            TaskDialog.Show("Debug", $"Generated field values for {values.Count} UV points.");
//            return new FieldValues(values);
//        }
//    }
//}
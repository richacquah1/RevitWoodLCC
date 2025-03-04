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
//    public class DivideFaceAndAssignValues : IExternalCommand
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

//                    // Ensure the active view is appropriate
//                    View3D view3D = doc.ActiveView as View3D;
//                    if (view3D == null || !view3D.IsSectionBoxActive)
//                    {
//                        message = "Please make sure you are in a 3D view with an active section box.";
//                        return Result.Failed;
//                    }

//                    BoundingBoxXYZ sectionBox = view3D.GetSectionBox();

//                    foreach (Reference faceRef in selectedFaceRefs)
//                    {
//                        Face face = doc.GetElement(faceRef.SelectedElementId).GetGeometryObjectFromReference(faceRef) as Face;
//                        if (face != null)
//                        {
//                            DivideFaceIntoGridsAndAssignValues(doc, face, faceRef, useSolidColor, sectionBox);
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

//        private void DivideFaceIntoGridsAndAssignValues(Document doc, Face face, Reference faceRef, bool useSolidColor, BoundingBoxXYZ sectionBox)
//        {
//            BoundingBoxUV bboxUV = face.GetBoundingBox();
//            double gridSize = 0.5; // Grid size in meters

//            int uDivisions = (int)Math.Ceiling((bboxUV.Max.U - bboxUV.Min.U) / gridSize);
//            int vDivisions = (int)Math.Ceiling((bboxUV.Max.V - bboxUV.Min.V) / gridSize);

//            List<UVGridCellInfo> gridCells = new List<UVGridCellInfo>();

//            // Constants for WDR calculation
//            double C_T = 1.0;
//            double C_R = 1.0;
//            double O = 1.0;
//            double W = 1.0;
//            double v_ref = 10.0; // Reference wind speed in m/s
//            double h_ref = 10.0; // Reference height in meters
//            double alpha = 0.14;
//            double R_h = 1.0; // Rain intensity
//            double D = 45.0; // Wind direction in degrees
//            double theta = 0.0; // Face orientation (assuming 0 for simplicity)

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

//                    // Calculate the vertical distance from the midpoint to the base of the section box
//                    double gridcellHeight = midXYZ.Z - sectionBox.Min.Z;

//                    // Calculate wind speed at the grid cell height using the power law profile
//                    double windSpeedAtGridCell = v_ref * Math.Pow((gridcellHeight / h_ref), alpha);

//                    // Calculate WDR using the specific wind speed for the grid cell
//                    double R_wdr = WDRSimulationUtilities.CalculateWDR(C_T, C_R, O, W, windSpeedAtGridCell, R_h, D, theta);

//                    UVGridCellInfo cellInfo = new UVGridCellInfo
//                    {
//                        Id = $"{faceRef.ConvertToStableRepresentation(doc)}_Grid_{u}_{v}",
//                        MinUV = new UV(minU, minV),
//                        MaxUV = new UV(maxU, maxV),
//                        Hits = 0,
//                        MidpointUV = midUV,
//                        MidpointXYZ = midXYZ,
//                        CellheightfromGround = gridcellHeight,
//                        GridcellWindSpeed = windSpeedAtGridCell,
//                        GridcellWDR = R_wdr,
//                        Theta = theta
//                    };

//                    gridCells.Add(cellInfo);
//                }
//            }

//            ApplyAVFToFace(doc, faceRef, gridCells, useSolidColor);
//        }

//        private void ApplyAVFToFace(Document doc, Reference faceRef, List<UVGridCellInfo> gridCells, bool useSolidColor)
//        {
//            SpatialFieldManager sfm = SpatialFieldManager.GetSpatialFieldManager(doc.ActiveView);
//            if (sfm == null)
//            {
//                sfm = SpatialFieldManager.CreateSpatialFieldManager(doc.ActiveView, 1);
//            }
//            else
//            {
//                // Clear existing AVF data
//                sfm.Clear();
//            }

//            double maxValue = gridCells.Max(c => c.GridcellWDR);
//            double minValue = gridCells.Min(c => c.GridcellWDR);
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

//        private AnalysisDisplayStyle CreateAnalysisDisplayStyle(Document doc, double minValue, double maxValue, bool useSolidColor)
//        {
//            string styleName = useSolidColor ? "SolidColor Height-Based Analysis Display Style" : "Gradient Height-Based Analysis Display Style";

//            AnalysisDisplayStyle existingStyle = new FilteredElementCollector(doc)
//                .OfClass(typeof(AnalysisDisplayStyle))
//                .Cast<AnalysisDisplayStyle>()
//                .FirstOrDefault(a => a.Name.Equals(styleName));

//            if (existingStyle != null) return existingStyle;

//            // Define a list of colors for the gradient
//            List<Autodesk.Revit.DB.Color> colors = new List<Autodesk.Revit.DB.Color>
//            {
//                new Autodesk.Revit.DB.Color(0, 0, 255), // Blue for lowest values
//                new Autodesk.Revit.DB.Color(0, 255, 0), // Green for low-mid values
//                new Autodesk.Revit.DB.Color(255, 255, 0), // Yellow for medium values
//                new Autodesk.Revit.DB.Color(255, 165, 0), // Orange for high-mid values
//                new Autodesk.Revit.DB.Color(255, 0, 0) // Red for highest values
//            };

//            AnalysisDisplayColorSettings colorSettings = new AnalysisDisplayColorSettings
//            {
//                ColorSettingsType = useSolidColor ? AnalysisDisplayStyleColorSettingsType.Solid
//                s : AnalysisDisplayStyleColorSettingsType.GradientColor
//            };

//            List<AnalysisDisplayColorEntry> colorEntries = new List<AnalysisDisplayColorEntry>();
//            double step = (maxValue - minValue) / (colors.Count - 1);

//            for (int i = 0; i < colors.Count; i++)
//            {
//                double value = minValue + step * i;
//                colorEntries.Add(new AnalysisDisplayColorEntry(colors[i], value));
//            }

//            colorSettings.SetIntermediateColors(colorEntries);

//            AnalysisDisplayColoredSurfaceSettings surfaceSettings = new AnalysisDisplayColoredSurfaceSettings
//            {
//                ShowGridLines = false
//            };

//            AnalysisDisplayLegendSettings legendSettings = new AnalysisDisplayLegendSettings
//            {
//                ShowLegend = true,
//                NumberOfSteps = colors.Count,
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

//        public class UVGridCellInfo
//        {
//            public string Id { get; set; }
//            public UV MinUV { get; set; }
//            public UV MaxUV { get; set; }
//            public UV MidpointUV { get; set; }
//            public XYZ MidpointXYZ { get; set; }
//            public double CellheightfromGround { get; set; }
//            public double GridcellWindSpeed { get; set; }
//            public double GridcellWDR { get; set; }
//            public double Theta { get; set; }
//            public int Hits { get; set; }
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

//    public static class WDRSimulationUtilities
//    {
//        public static double CalculateWDR(double C_T, double C_R, double O, double W, double v, double R_h, double D, double theta)
//        {
//            double D_rad = D * (Math.PI / 180.0);
//            double theta_rad = theta * (Math.PI / 180.0);
//            double cosTerm = Math.Cos(D_rad - theta_rad);
//            double R_wdr = (2.0 / 9.0) * C_T * C_R * O * W * v * Math.Pow(R_h, 8.0 / 9.0) * cosTerm;
//            return R_wdr;
//        }
//    }
//}

/////Updated example with zero values. It works perfect
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
//    public class DivideFaceAndAssignValues : IExternalCommand
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

//                    // Ensure the active view is appropriate
//                    View3D view3D = doc.ActiveView as View3D;
//                    if (view3D == null || !view3D.IsSectionBoxActive)
//                    {
//                        message = "Please make sure you are in a 3D view with an active section box.";
//                        return Result.Failed;
//                    }

//                    BoundingBoxXYZ sectionBox = view3D.GetSectionBox();

//                    foreach (Reference faceRef in selectedFaceRefs)
//                    {
//                        Face face = doc.GetElement(faceRef.SelectedElementId).GetGeometryObjectFromReference(faceRef) as Face;
//                        if (face != null)
//                        {
//                            DivideFaceIntoGridsAndAssignValues(doc, face, faceRef, useSolidColor, sectionBox);
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

//        private void DivideFaceIntoGridsAndAssignValues(Document doc, Face face, Reference faceRef, bool useSolidColor, BoundingBoxXYZ sectionBox)
//        {
//            BoundingBoxUV bboxUV = face.GetBoundingBox();
//            double gridSize = 0.5; // Grid size in meters

//            int uDivisions = (int)Math.Ceiling((bboxUV.Max.U - bboxUV.Min.U) / gridSize);
//            int vDivisions = (int)Math.Ceiling((bboxUV.Max.V - bboxUV.Min.V) / gridSize);

//            List<UVGridCellInfo> gridCells = new List<UVGridCellInfo>();

//            // Constants for WDR calculation
//            double C_T = 1.0;
//            double C_R = 1.0;
//            double O = 1.0;
//            double W = 1.0;
//            double v_ref = 10.0; // Reference wind speed in m/s
//            double h_ref = 10.0; // Reference height in meters
//            double alpha = 0.14;
//            double R_h = 1.0; // Rain intensity
//            double D = 45.0; // Wind direction in degrees
//            double theta = 0.0; // Face orientation (assuming 0 for simplicity)

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

//                    // Calculate the vertical distance from the midpoint to the base of the section box
//                    double gridcellHeight = midXYZ.Z - sectionBox.Min.Z;

//                    // Calculate wind speed at the grid cell height using the power law profile
//                    double windSpeedAtGridCell = v_ref * Math.Pow((gridcellHeight / h_ref), alpha);

//                    // Calculate WDR using the specific wind speed for the grid cell
//                    double R_wdr = WDRSimulationUtilities.CalculateWDR(C_T, C_R, O, W, windSpeedAtGridCell, R_h, D, theta);

//                    UVGridCellInfo cellInfo = new UVGridCellInfo
//                    {
//                        Id = $"{faceRef.ConvertToStableRepresentation(doc)}_Grid_{u}_{v}",
//                        MinUV = new UV(minU, minV),
//                        MaxUV = new UV(maxU, maxV),
//                        Hits = 0,
//                        MidpointUV = midUV,
//                        MidpointXYZ = midXYZ,
//                        CellheightfromGround = gridcellHeight,
//                        GridcellWindSpeed = windSpeedAtGridCell,
//                        GridcellWDR = R_wdr,
//                        Theta = theta
//                    };

//                    gridCells.Add(cellInfo);
//                }
//            }

//            double minValue = gridCells.Min(c => c.GridcellWDR);
//            double maxValue = gridCells.Max(c => c.GridcellWDR);

//            foreach (var cell in gridCells)
//            {
//                cell.GridcellWDR = cell.GridcellWDR - minValue; // Normalize values to have some zero
//            }

//            maxValue = gridCells.Max(c => c.GridcellWDR);

//            ApplyAVFToFace(doc, faceRef, gridCells, useSolidColor, maxValue);
//        }

//        private void ApplyAVFToFace(Document doc, Reference faceRef, List<UVGridCellInfo> gridCells, bool useSolidColor, double maxValue)
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

//        private AnalysisDisplayStyle CreateAnalysisDisplayStyle(Document doc, double minValue, double maxValue, bool useSolidColor)
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

//        public class UVGridCellInfo
//        {
//            public string Id { get; set; }
//            public UV MinUV { get; set; }
//            public UV MaxUV { get; set; }
//            public UV MidpointUV { get; set; }
//            public XYZ MidpointXYZ { get; set; }
//            public double CellheightfromGround { get; set; }
//            public double GridcellWindSpeed { get; set; }
//            public double GridcellWDR { get; set; }
//            public double Theta { get; set; }
//            public int Hits { get; set; }
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

//    public static class WDRSimulationUtilities
//    {
//        public static double CalculateWDR(double C_T, double C_R, double O, double W, double v, double R_h, double D, double theta)
//        {
//            double D_rad = D * (Math.PI / 180.0);
//            double theta_rad = theta * (Math.PI / 180.0);
//            double cosTerm = Math.Cos(D_rad - theta_rad);
//            double R_wdr = (2.0 / 9.0) * C_T * C_R * O * W * v * Math.Pow(R_h, 8.0 / 9.0) * cosTerm;
//            return R_wdr;
//        }
//    }
//}

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
//    public class DivideFaceAndAssignValues : IExternalCommand
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

//                    // Ensure the active view is appropriate
//                    View3D view3D = doc.ActiveView as View3D;
//                    if (view3D == null || !view3D.IsSectionBoxActive)
//                    {
//                        message = "Please make sure you are in a 3D view with an active section box.";
//                        return Result.Failed;
//                    }

//                    BoundingBoxXYZ sectionBox = view3D.GetSectionBox();

//                    foreach (Reference faceRef in selectedFaceRefs)
//                    {
//                        Face face = doc.GetElement(faceRef.SelectedElementId).GetGeometryObjectFromReference(faceRef) as Face;
//                        if (face != null)
//                        {
//                            DivideFaceIntoGridsAndAssignValues(doc, face, faceRef, useSolidColor, sectionBox);
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

//        private void DivideFaceIntoGridsAndAssignValues(Document doc, Face face, Reference faceRef, bool useSolidColor, BoundingBoxXYZ sectionBox)
//        {
//            BoundingBoxUV bboxUV = face.GetBoundingBox();
//            double gridSize = 0.5; // Grid size in meters

//            int uDivisions = (int)Math.Ceiling((bboxUV.Max.U - bboxUV.Min.U) / gridSize);
//            int vDivisions = (int)Math.Ceiling((bboxUV.Max.V - bboxUV.Min.V) / gridSize);

//            List<UVGridCellInfo> gridCells = new List<UVGridCellInfo>();

//            // Constants for WDR calculation
//            double C_T = 1.0;
//            double C_R = 1.0;
//            double O = 1.0;
//            double W = 1.0;
//            double v_ref = 10.0; // Reference wind speed in m/s
//            double h_ref = 10.0; // Reference height in meters
//            double alpha = 0.14;
//            double R_h = 1.0; // Rain intensity
//            double D = 45.0; // Wind direction in degrees
//            double theta = 0.0; // Face orientation (assuming 0 for simplicity)

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

//                    // Calculate the vertical distance from the midpoint to the base of the section box
//                    double gridcellHeight = midXYZ.Z - sectionBox.Min.Z;

//                    // Calculate wind speed at the grid cell height using the power law profile
//                    double windSpeedAtGridCell = v_ref * Math.Pow((gridcellHeight / h_ref), alpha);

//                    // Calculate WDR using the specific wind speed for the grid cell
//                    double R_wdr = WDRSimulationUtilities.CalculateWDR(C_T, C_R, O, W, windSpeedAtGridCell, R_h, D, theta);

//                    UVGridCellInfo cellInfo = new UVGridCellInfo
//                    {
//                        Id = $"{faceRef.ConvertToStableRepresentation(doc)}_Grid_{u}_{v}",
//                        MinUV = new UV(minU, minV),
//                        MaxUV = new UV(maxU, maxV),
//                        Hits = 0,
//                        MidpointUV = midUV,
//                        MidpointXYZ = midXYZ,
//                        CellheightfromGround = gridcellHeight,
//                        GridcellWindSpeed = windSpeedAtGridCell,
//                        GridcellWDR = R_wdr,
//                        Theta = theta
//                    };

//                    gridCells.Add(cellInfo);
//                }
//            }

//            double minValue = gridCells.Min(c => c.GridcellWDR);
//            double maxValue = gridCells.Max(c => c.GridcellWDR);

//            foreach (var cell in gridCells)
//            {
//                cell.GridcellWDR = cell.GridcellWDR - minValue; // Normalize values to have some zero
//            }

//            maxValue = gridCells.Max(c => c.GridcellWDR);

//            wdrUtility.ApplyAVFToFace(doc, faceRef, gridCells, useSolidColor, maxValue);
//        }

//        public class UVGridCellInfo
//        {
//            public string Id { get; set; }
//            public UV MinUV { get; set; }
//            public UV MaxUV { get; set; }
//            public UV MidpointUV { get; set; }
//            public XYZ MidpointXYZ { get; set; }
//            public double CellheightfromGround { get; set; }
//            public double GridcellWindSpeed { get; set; }
//            public double GridcellWDR { get; set; }
//            public double Theta { get; set; }
//            public int Hits { get; set; }
//        }

//        public class VisualizationStyleForm : System.Windows.Window
//        {
//            public string SelectedStyle { get; private set; }
//            private System.Windows.Controls.ComboBox comboBox;

//            public VisualizationStyleForm()
//            {
//                Title = "Select Visualization Style";
//                Width = 300;
//                Height = 150;

//                System.Windows.Controls.StackPanel stackPanel = new System.Windows.Controls.StackPanel();

//                System.Windows.Controls.Label label = new System.Windows.Controls.Label
//                {
//                    Content = "Select Visualization Style:"
//                };
//                stackPanel.Children.Add(label);

//                comboBox = new System.Windows.Controls.ComboBox();
//                comboBox.Items.Add("Gradient");
//                comboBox.Items.Add("SolidColor");
//                stackPanel.Children.Add(comboBox);

//                System.Windows.Controls.Button button = new System.Windows.Controls.Button
//                {
//                    Content = "OK",
//                    Width = 100,
//                    Height = 30,
//                    Margin = new System.Windows.Thickness(10)
//                };
//                button.Click += Button_Click;
//                stackPanel.Children.Add(button);

//                Content = stackPanel;
//            }

//            private void Button_Click(object sender, System.Windows.RoutedEventArgs e)
//            {
//                SelectedStyle = comboBox.SelectedItem as string;
//                DialogResult = true;
//                Close();
//            }
//        }
//    }

//    public static class WDRSimulationUtilities
//    {
//        public static double CalculateWDR(double C_T, double C_R, double O, double W, double v, double R_h, double D, double theta)
//        {
//            double D_rad = D * (Math.PI / 180.0);
//            double theta_rad = theta * (Math.PI / 180.0);
//            double cosTerm = Math.Cos(D_rad - theta_rad);
//            double R_wdr = (2.0 / 9.0) * C_T * C_R * O * W * v * Math.Pow(R_h, 8.0 / 9.0) * cosTerm;
//            return R_wdr;
//        }
//    }
//}

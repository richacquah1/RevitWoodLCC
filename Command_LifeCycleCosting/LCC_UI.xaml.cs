using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using IronPython.Runtime.Operations;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using static RevitWoodLCC.SLEUtility;



namespace RevitWoodLCC
{
    public partial class MainWindow : Window
    {
        private Dictionary<string, double> laborCostData;
        private UIApplication _uiApp;
        private UIDocument _uiDoc;
        private Document _doc;
        private View3D _view3D;
        private ElementId _duplicatedViewId;
        private IList<ElementId> _elementsInDuplicatedView;
        private int _currentElementIndex = 0;
        private PreviewControl previewControl;
        private VisualizationMode currentMode = VisualizationMode.AllElements;
        private ExternalCommandData _commandData;

        public MainWindow(UIApplication uiApp, UIDocument uiDoc, ICollection<ElementId> selectedIds)
        {
            // Set culture settings
            CultureInfo customCulture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
            customCulture.NumberFormat.NumberDecimalSeparator = ".";
            customCulture.NumberFormat.NumberGroupSeparator = ",";
            Thread.CurrentThread.CurrentCulture = customCulture;
            Thread.CurrentThread.CurrentUICulture = customCulture;

            InitializeComponent();
            _uiApp = uiApp;
            _uiDoc = uiDoc;
            _doc = uiDoc.Document;
            _currentElementIndex = 0;

            // Initialize controls
            laborCostField = (System.Windows.Controls.TextBox)FindName("laborCostField");
            unitTimeField = (System.Windows.Controls.TextBox)FindName("unitTimeField");
            laborInputsCheckBox = (CheckBox)FindName("laborInputsCheckBox");
            maintenanceCostsCheckBox = (CheckBox)FindName("maintenanceCostsCheckBox");
            calculatedMaintenanceCostField = (System.Windows.Controls.TextBox)FindName("calculatedMaintenanceCostField");
            EndofLifeValueField = (System.Windows.Controls.TextBox)FindName("EndofLifeValueField");

            elementServiceLifeDurationField = (System.Windows.Controls.TextBox)FindName("elementServiceLifeDurationField");
            projectEndofLifeDurationField = (System.Windows.Controls.TextBox)FindName("projectEndofLifeDurationField");
            chosenCurrencyTextBlock = (TextBlock)FindName("chosenCurrencyTextBlock");

            locationPriceFactorField = (System.Windows.Controls.TextBox)FindName("locationPriceFactorField");
            materialPriceFactorField = (System.Windows.Controls.TextBox)FindName("materialPriceFactorField");
            escalationRateField = (System.Windows.Controls.TextBox)FindName("escalationRateField");
            discountRateField = (System.Windows.Controls.TextBox)FindName("discountRateField");

            initialElementCostsField = (System.Windows.Controls.TextBox)FindName("initialElementCostsField");
            futureElementCostEscalationField = (System.Windows.Controls.TextBox)FindName("futureElementCostEscalationField");
            presentValueDiscountField = (System.Windows.Controls.TextBox)FindName("presentValueDiscountField");

            InitializeDefaultValues();
            SetCurrencyField("EUR");
            UpdateChosenCurrencyTextBlock("EUR");
            InitializeLaborCostData();

            SLEUtility.LoadLocation(locationField, _commandData, "Above Ground"); // Pass the commandData and condition

            AdjustLocationDisplay();
            SetProjectEndofLifeDuration();

            // Initialize 3D preview
            _view3D = GetActive3DViewAndDuplicate(_uiDoc.Document);
            if (_view3D != null)
            {
                InitializeElementsInDuplicatedView();
                AddPreviewControlToUI();
            }
            else
            {
                MessageBox.Show("No suitable 3D view found.");
                return;
            }

            this.Closed += MainWindow_Closed;

            // Add TextChanged event handlers for validation
            materialPriceField.TextChanged += TextBox_TextChanged;
            materialPriceFactorField.TextChanged += TextBox_TextChanged;
            laborCostField.TextChanged += TextBox_TextChanged;
            unitTimeField.TextChanged += TextBox_TextChanged;
            EndofLifeValueField.TextChanged += TextBox_TextChanged;
        }

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            if (_duplicatedViewId != null)
            {
                using (Transaction tx = new Transaction(_doc, "Delete Temp View"))
                {
                    tx.Start();
                    _doc.Delete(_duplicatedViewId);
                    tx.Commit();
                }
            }
        }

        private void InitializeDefaultValues()
        {
            materialPriceField.Text = string.Empty;
            materialPriceFactorField.Text = string.Empty;
            laborCostField.Text = string.Empty;
            unitTimeField.Text = string.Empty;
            EndofLifeValueField.Text = string.Empty;

            locationPriceFactorField.Text = string.Empty;
            escalationRateField.Text = string.Empty;
            discountRateField.Text = string.Empty;
        }

        private void InitializeLaborCostData()
        {
            laborCostData = new Dictionary<string, double>
            {
                { "EU27", 29.10 },
                { "Denmark", 46.90 },
                { "Luxembourg", 43.00 },
                { "Belgium", 41.60 },
                { "Sweden", 39.70 },
                { "Netherlands", 38.30 },
                { "France", 37.90 },
                { "Austria", 37.50 },
                { "Germany", 37.20 },
                { "Finland", 35.10 },
                { "Ireland", 33.50 },
                { "Italy", 29.30 },
                { "Spain", 22.90 },
                { "Slovenia", 21.10 },
                { "Cyprus", 18.30 },
                { "Malta", 17.30 },
                { "Greece", 17.20 },
                { "Portugal", 16.00 },
                { "Czechia", 15.30 },
                { "Estonia", 14.50 },
                { "Slovakia", 14.20 },
                { "Poland", 11.50 },
                { "Lithuania", 11.30 },
                { "Croatia", 11.20 },
                { "Latvia", 11.10 },
                { "Hungary", 10.40 },
                { "Romania", 8.50 },
                { "Bulgaria", 7.00 }
            };
        }

        private void SetProjectEndofLifeDuration()
        {
            projectEndofLifeDurationField.Text = "60"; // Set a test value directly
        }

        private void AddPreviewControlToUI()
        {
            if (_view3D != null)
            {
                previewControl = new PreviewControl(_doc, _view3D.Id);
                previewControl.VerticalAlignment = VerticalAlignment.Stretch;
                PreviewContainer.Children.Add(previewControl);
            }
        }

        private View3D GetActive3DViewAndDuplicate(Document doc)
        {
            View3D activeView3D = doc.ActiveView as View3D;

            if (activeView3D == null || activeView3D.IsTemplate || !activeView3D.CanBePrinted)
                return null;

            using (Transaction tx = new Transaction(doc, "Duplicate 3D View"))
            {
                tx.Start();
                ElementId duplicatedViewId = activeView3D.Duplicate(ViewDuplicateOption.WithDetailing);
                View3D duplicatedView3D = doc.GetElement(duplicatedViewId) as View3D;
                duplicatedView3D.Name = "Temporary Preview View";

                _duplicatedViewId = duplicatedViewId;

                tx.Commit();
                return duplicatedView3D;
            }
        }

        private void InitializeElementsInDuplicatedView()
        {
            FilteredElementCollector collector = new FilteredElementCollector(_doc, _view3D.Id);
            _elementsInDuplicatedView = collector.WhereElementIsNotElementType().ToElementIds().ToList();
            _currentElementIndex = _elementsInDuplicatedView.IndexOf(_uiDoc.Selection.GetElementIds().FirstOrDefault());
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            while (_currentElementIndex < _elementsInDuplicatedView.Count - 1)
            {
                _currentElementIndex++;
                if (IsElementValid(_elementsInDuplicatedView[_currentElementIndex]))
                {
                    SelectElement(_elementsInDuplicatedView[_currentElementIndex]);
                    return;
                }
            }

            MessageBox.Show("This is the last element.", "Navigation", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void PreviousButton_Click(object sender, RoutedEventArgs e)
        {
            while (_currentElementIndex > 0)
            {
                _currentElementIndex--;
                if (IsElementValid(_elementsInDuplicatedView[_currentElementIndex]))
                {
                    SelectElement(_elementsInDuplicatedView[_currentElementIndex]);
                    return;
                }
            }

            MessageBox.Show("This is the first element.", "Navigation", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private bool IsElementValid(ElementId elementId)
        {
            Element element = _doc.GetElement(elementId);
            if (element == null || element.Category == null)
            {
                return false;
            }

            // Exclude specific types or categories of elements
            if (element is View ||
                element is Level ||
                element.Category.Id.IntegerValue == (int)BuiltInCategory.OST_Materials ||
                element.Category.Id.IntegerValue == (int)BuiltInCategory.OST_Rooms ||
                element.Category.Id.IntegerValue == (int)BuiltInCategory.OST_Topography ||
                element.Category.Id.IntegerValue == (int)BuiltInCategory.OST_Schedules ||
                element.Category.Id.IntegerValue == (int)BuiltInCategory.OST_Tags ||
                element.Category.Id.IntegerValue == (int)BuiltInCategory.OST_DetailComponents ||
                element.Category.Id.IntegerValue == (int)BuiltInCategory.OST_Grids ||
                element.Category.Id.IntegerValue == (int)BuiltInCategory.OST_Levels ||
                element.Category.Id.IntegerValue == (int)BuiltInCategory.OST_TextNotes ||
                element.Category.Id.IntegerValue == (int)BuiltInCategory.OST_Dimensions ||
                element.Category.Id.IntegerValue == (int)BuiltInCategory.OST_Callouts ||
                element.Category.Id.IntegerValue == (int)BuiltInCategory.OST_SectionHeads ||
                element.Category.Id.IntegerValue == (int)BuiltInCategory.OST_ElevationMarks ||
                element.Category.Id.IntegerValue == (int)BuiltInCategory.OST_Sheets ||
                element.Category.Id.IntegerValue == (int)BuiltInCategory.OST_TitleBlocks ||
                element.Category.Id.IntegerValue == (int)BuiltInCategory.OST_FilledRegion)
            {
                return false;
            }

            // Ensure the element has valid geometry
            Options options = new Options();
            GeometryElement geomElement = element.get_Geometry(options);
            if (geomElement != null)
            {
                foreach (GeometryObject geomObj in geomElement)
                {
                    if (geomObj is Solid solid && solid.Volume > 0)
                    {
                        return true;
                    }
                    else if (geomObj is GeometryInstance)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private void SelectElement(ElementId elementId)
        {
            if (_uiDoc != null && elementId != ElementId.InvalidElementId)
            {
                _uiDoc.Selection.SetElementIds(new List<ElementId> { elementId });
                ZoomToElement(_uiDoc, elementId);
                _uiDoc.RefreshActiveView();
                UpdateElementDetails(elementId);
            }
        }

      

        public void UpdateElementDetails(ElementId elementId)
        {
            Element element = _doc.GetElement(elementId);

            if (element == null)
            {
                TaskDialog.Show("Error", "Element not found. Please select a valid element.");
                return;
            }

            // Retrieve the Material object from the utility method
            Material material = LCC_Utility.GetElementMaterial(element, _doc);

            if (material == null)
            {
                TaskDialog.Show("Warning", $"No Material Assigned to Element ID: {element.Id}, Name: {element.Name}. Please add a material.");
                return;
            }

            // Extract the material name
            string materialName = material.Name;

            // Update UI with material and element details
            SetMaterialField(materialName);
            SetElementDescriptionField($"ID: {element.Id}, Name: {element.Name}, Category: {element.Category.Name}");
            SetProjectNameField(_doc.ProjectInformation.Name);

            // Fetch and set additional parameters using utility methods
            SetLocationField(LCC_Utility.GetParameterAsString(element, "Location", "Not specified"));
            SetCurrencyField(LCC_Utility.GetParameterAsString(element, "Currency", "Not specified"));

            // Set material quantity field using the volume in cubic meters
            SetMaterialQuantityField(LCC_Utility.GetParameterAsDoubleConverted(element, "Volume", UnitTypeId.CubicMeters));

            // Fetch and set service life duration
            double serviceLifeDuration = LCC_Utility.GetParameterAsDouble(element, "Element_Service Life Duration");
            SetElementServiceLifeDurationField(serviceLifeDuration > 0 ? serviceLifeDuration : 0.0);

            // Optionally list all parameters if no service life duration is set
            if (serviceLifeDuration == 0)
                ListAllSharedParameters(element);

            // Reset these fields to ensure they are empty by default
            ClearInputFields();

            // Set fields that depend on specific conditions
            SetTextBoxIfNotEmpty(locationPriceFactorField, element.LookupParameter("Location Price Factor"));
            SetTextBoxIfNotEmpty(escalationRateField, element.LookupParameter("Escalation Rate"));
            SetTextBoxIfNotEmpty(discountRateField, element.LookupParameter("Discount Rate"));
            SetTextBoxIfNotEmpty(calculatedMaintenanceCostField, element.LookupParameter("Maintenance Costs"));

            // Update the display and labor costs based on the new data
            AdjustLocationDisplay();
            UpdateLaborCost();
        }


        private void ClearInputFields()
        {
            materialPriceField.Text = string.Empty;
            materialPriceFactorField.Text = string.Empty;
            laborCostField.Text = string.Empty;
            unitTimeField.Text = string.Empty;
            EndofLifeValueField.Text = string.Empty;
        }



        private void ListAllSharedParameters(Element element)
        {
            var sb = new StringBuilder();
            foreach (Parameter param in element.Parameters)
            {
                if (param.IsShared)
                {
                    string paramName = param.Definition.Name;
                    StorageType storageType = param.StorageType;  // Use StorageType to determine the type of data stored.
                    string paramValue;

                    // Retrieve the parameter value based on its storage type
                    switch (storageType)
                    {
                        case StorageType.Double:
                            paramValue = param.AsDouble().ToString("F2");  // Format double values
                            break;
                        case StorageType.Integer:
                            paramValue = param.AsInteger().ToString();
                            break;
                        case StorageType.String:
                            paramValue = param.AsString();
                            break;
                        case StorageType.ElementId:
                            ElementId id = param.AsElementId();
                            paramValue = id.IntegerValue.ToString();
                            break;
                        default:
                            paramValue = "Unsupported type";
                            break;
                    }

                    sb.AppendLine($"Name: {paramName}, Type: {storageType}, Value: {paramValue}");
                }
            }

            // Display all shared parameters
            if (sb.Length > 0)
            {
                TaskDialog.Show("Shared Parameters", sb.ToString());
            }
            else
            {
                TaskDialog.Show("Shared Parameters", "No shared parameters found for this element.");
            }
        }



        private void SetTextBoxIfNotEmpty(System.Windows.Controls.TextBox textBox, Parameter parameter)
        {
            if (parameter != null && !string.IsNullOrEmpty(parameter.AsString()))
            {
                textBox.Text = parameter.AsString();
            }
            else
            {
                textBox.Text = string.Empty;
            }
        }

        private void ZoomToElement(UIDocument uiDocument, ElementId elementId)
        {
            try
            {
                Element element = uiDocument.Document.GetElement(elementId);
                if (element != null)
                {
                    BoundingBoxXYZ boundingBox = element.get_BoundingBox(null);
                    if (boundingBox != null)
                    {
                        Outline outline = new Outline(boundingBox.Min, boundingBox.Max);
                        BoundingBoxXYZ newBox = new BoundingBoxXYZ()
                        {
                            Min = outline.MinimumPoint - new XYZ(5, 5, 5),
                            Max = outline.MaximumPoint + new XYZ(5, 5, 5)
                        };

                        View3D view3D = uiDocument.Document.ActiveView as View3D;
                        if (view3D != null)
                        {
                            using (Transaction trans = new Transaction(uiDocument.Document, "Zoom to Element"))
                            {
                                trans.Start();
                                view3D.SetSectionBox(newBox);
                                trans.Commit();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Error", $"Failed to zoom to element: {ex.Message}");
            }
        }

        private Material GetElementMaterial(Document document, Element element)
        {
            string[] materialParamNames = { "Material", "Structural Material", "Finish Material", "Surface Material" };

            foreach (string paramName in materialParamNames)
            {
                Parameter materialParam = element.LookupParameter(paramName);
                if (materialParam != null && materialParam.HasValue)
                {
                    ElementId materialId = materialParam.AsElementId();
                    Material material = document.GetElement(materialId) as Material;

                    if (material != null)
                    {
                        return material;
                    }
                }
            }

            Options geomOptions = new Options();
            GeometryElement geomElement = element.get_Geometry(geomOptions);
            if (geomElement != null)
            {
                foreach (GeometryObject geomObject in geomElement)
                {
                    if (geomObject is Solid solid)
                    {
                        Material material = GetMaterialFromSolid(document, solid);
                        if (material != null)
                        {
                            return material;
                        }
                    }
                    else if (geomObject is GeometryInstance geomInstance)
                    {
                        GeometryElement instanceGeomElement = geomInstance.GetInstanceGeometry();
                        foreach (GeometryObject instanceGeomObject in instanceGeomElement)
                        {
                            if (instanceGeomObject is Solid instanceSolid)
                            {
                                Material material = GetMaterialFromSolid(document, instanceSolid);
                                if (material != null)
                                {
                                    return material;
                                }
                            }
                        }
                    }
                }
            }

            return null;
        }

        private Material GetMaterialFromSolid(Document document, Solid solid)
        {
            foreach (Face face in solid.Faces)
            {
                ElementId materialId = face.MaterialElementId;
                Material material = document.GetElement(materialId) as Material;
                if (material != null)
                {
                    return material;
                }
            }
            return null;
        }

        private void Toggle3DPreviewButton_Click(object sender, RoutedEventArgs e)
        {
            UIDocument uiDoc = _uiDoc;
            Document doc = uiDoc.Document;

            View3D view3D = doc.GetElement(_duplicatedViewId) as View3D;

            ElementId firstSelectedId = uiDoc.Selection.GetElementIds().FirstOrDefault();
            string elementIdAsString = firstSelectedId?.ToString();

            Button toggle3DPreviewButton = sender as Button;

            switch (currentMode)
            {
                case VisualizationMode.AllElements:
                    currentMode = VisualizationMode.SelectedOnly;
                    toggle3DPreviewButton.Content = "Show Selected and Adjacent";
                    break;
                case VisualizationMode.SelectedAndAdjacent:
                    currentMode = VisualizationMode.AllElements;
                    toggle3DPreviewButton.Content = "Show Selected Only";
                    break;
                case VisualizationMode.SelectedOnly:
                    currentMode = VisualizationMode.SelectedAndAdjacent;
                    toggle3DPreviewButton.Content = "Show All Elements";
                    break;
            }

            ElementId selectedElementId = new ElementId(Convert.ToInt32(elementIdAsString));
            IList<ElementId> adjacentElementIds = FindAdjacentElements(doc, selectedElementId);
            Modify3DView(_doc, _view3D, adjacentElementIds, selectedElementId, currentMode);
        }

        private IList<ElementId> FindAdjacentElements(Document doc, ElementId selectedId)
        {
            Element selectedElement = doc.GetElement(selectedId);
            BoundingBoxXYZ selectedBoundingBox = selectedElement.get_BoundingBox(null);
            Outline outline = new Outline(selectedBoundingBox.Min, selectedBoundingBox.Max);
            BoundingBoxIntersectsFilter bbFilter = new BoundingBoxIntersectsFilter(outline);

            FilteredElementCollector collector = new FilteredElementCollector(doc).WherePasses(bbFilter);
            IList<ElementId> adjacentElementIds = new List<ElementId>();

            foreach (Element element in collector)
            {
                if (element.Id == selectedId)
                    continue;

                GeometryElement geomElem = element.get_Geometry(new Options());
                if (geomElem != null)
                {
                    foreach (GeometryObject geomObj in geomElem)
                    {
                        if (geomObj is Solid || geomObj is GeometryInstance)
                        {
                            adjacentElementIds.Add(element.Id);
                            break;
                        }
                    }
                }
            }
            return adjacentElementIds;
        }

        private enum VisualizationMode
        {
            AllElements,
            SelectedAndAdjacent,
            SelectedOnly
        }

        private void Modify3DView(Document doc, View3D view3D, IList<ElementId> adjacentElementIds, ElementId selectedElementId, VisualizationMode mode)
        {
            TransactionStatus txStatus;

            using (Transaction tx = new Transaction(doc, "Modify 3D View"))
            {
                tx.Start();

                switch (mode)
                {
                    case VisualizationMode.AllElements:
                        view3D.DisableTemporaryViewMode(TemporaryViewMode.TemporaryHideIsolate);
                        foreach (Category category in doc.Settings.Categories)
                        {
                            if (view3D.CanCategoryBeHidden(category.Id))
                            {
                                view3D.SetCategoryHidden(category.Id, false);
                            }
                        }
                        Category levelCategory = doc.Settings.Categories.get_Item(BuiltInCategory.OST_Levels);
                        if (levelCategory != null)
                        {
                            view3D.SetCategoryHidden(levelCategory.Id, true);
                        }
                        break;

                    case VisualizationMode.SelectedAndAdjacent:
                        IList<ElementId> SelectedAndAdjacentToIsolate = new List<ElementId>(adjacentElementIds);
                        SelectedAndAdjacentToIsolate.Add(selectedElementId);
                        view3D.DisableTemporaryViewMode(TemporaryViewMode.TemporaryHideIsolate);
                        view3D.IsolateElementsTemporary(SelectedAndAdjacentToIsolate);
                        break;

                    case VisualizationMode.SelectedOnly:
                        IList<ElementId> selectedElementOnly = new List<ElementId> { selectedElementId };
                        view3D.IsolateElementsTemporary(selectedElementOnly);
                        break;
                }

                txStatus = tx.Commit();
            }
        }

        private void CalculateButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                double elementServiceLifeDuration = double.Parse(elementServiceLifeDurationField.Text, CultureInfo.InvariantCulture);
                double projectServiceLifeDuration = double.Parse(projectEndofLifeDurationField.Text, CultureInfo.InvariantCulture);
                double materialQuantity = double.Parse(materialQuantityField.Text, CultureInfo.InvariantCulture);
                double materialPricePerUnit = double.Parse(materialPriceField.Text, CultureInfo.InvariantCulture);
                double materialPriceFactor = double.Parse(materialPriceFactorField.Text, CultureInfo.InvariantCulture);
                double locationPriceFactor = double.Parse(locationPriceFactorField.Text, CultureInfo.InvariantCulture);
                double escalationRate = double.Parse(escalationRateField.Text, CultureInfo.InvariantCulture) / 100;
                double discountRate = double.Parse(discountRateField.Text, CultureInfo.InvariantCulture) / 100;

                double materialCostToday = CalculateMaterialCostToday(materialQuantity, materialPricePerUnit, materialPriceFactor);
                calculatedMaterialCostField.Text = materialCostToday.ToString("N2", CultureInfo.InvariantCulture);

                double laborCostToday = 0;
                double endofLifeValue = 0;
                double otherRelatedCost = 0;
                double laborCostPerHour = 0;
                double unitTimeRequired = 0;

                if (laborInputsCheckBox.IsChecked ?? false)
                {
                    laborCostPerHour = double.Parse(laborCostField.Text, CultureInfo.InvariantCulture);
                    unitTimeRequired = double.Parse(unitTimeField.Text, CultureInfo.InvariantCulture);
                    laborCostToday = CalculateLaborCostToday(unitTimeRequired, laborCostPerHour);
                    calculatedLaborCostField.Text = laborCostToday.ToString("N2", CultureInfo.InvariantCulture);
                }

                if (endofLifeValueCheckBox.IsChecked ?? false)
                {
                    endofLifeValue = double.Parse(EndofLifeValueField.Text, CultureInfo.InvariantCulture);
                }

                double totalMaintenanceCost = 0;
                if (maintenanceCostsCheckBox.IsChecked ?? false)
                {
                    totalMaintenanceCost = CalculateTotalMaintenanceCost(
                        materialCostToday, laborCostToday, escalationRate, discountRate,
                        elementServiceLifeDuration, projectServiceLifeDuration);
                    calculatedMaintenanceCostField.Text = totalMaintenanceCost.ToString("N2", CultureInfo.InvariantCulture);
                }

                double totalCostToday = CalculateTotalCostToday(materialCostToday, laborCostToday, totalMaintenanceCost, endofLifeValue, otherRelatedCost, locationPriceFactor);

                double escalatedLifeCycleCost = CalculateEscalatedLifeCycleCost(totalCostToday, escalationRate, projectServiceLifeDuration);
                double presentValue = CalculatePresentValue(escalatedLifeCycleCost, discountRate, projectServiceLifeDuration);

                initialElementCostsField.Text = totalCostToday.ToString("N2", CultureInfo.InvariantCulture);
                futureElementCostEscalationField.Text = escalatedLifeCycleCost.ToString("N2", CultureInfo.InvariantCulture);
                presentValueDiscountField.Text = presentValue.ToString("N2", CultureInfo.InvariantCulture);

                if (chosenCurrencyTextBlock != null)
                {
                    chosenCurrencyTextBlock.Text = $"Currency: {((ComboBoxItem)currencyField.SelectedItem).Content.ToString()}";
                }

                if (double.TryParse(conversionRateField.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double conversionRate))
                {
                    initialElementCostsExtraField.Text = (totalCostToday * conversionRate).ToString("N2", CultureInfo.InvariantCulture);
                    futureElementCostEscalationExtraField.Text = (escalatedLifeCycleCost * conversionRate).ToString("N2", CultureInfo.InvariantCulture);
                    presentValueDiscountExtraField.Text = (presentValue * conversionRate).ToString("N2", CultureInfo.InvariantCulture);
                }
                else
                {
                    MessageBox.Show("Invalid conversion rate", "Calculation Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }

                // MessageBox.Show($"Life Cycle Cost: {(presentValue).ToString("N2", CultureInfo.InvariantCulture)}", "Calculation Result", MessageBoxButton.OKCancel);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Calculation Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private double CalculateMaterialCostToday(double quantity, double pricePerUnit, double priceFactor)
        {
            return quantity * pricePerUnit * priceFactor;
        }

        private double CalculateLaborCostToday(double unitTime, double laborCostPerHour)
        {
            return unitTime * laborCostPerHour;
        }

        private double CalculateTotalMaintenanceCost(double materialCostToday, double laborCostToday, double escalationRate, double discountRate, double elementServiceLifeDuration, double projectEndofLifeDuration)
        {
            double maintenanceCostPerReplacement = materialCostToday + laborCostToday;
            int numberOfReplacements = (int)(projectEndofLifeDuration / elementServiceLifeDuration);
            double totalMaintenanceCost = 0;

            for (int k = 1; k <= numberOfReplacements; k++)
            {
                double n = k * elementServiceLifeDuration;
                double SCA = Math.Pow(1 + escalationRate, n);
                double SPV = 1 / Math.Pow(1 + discountRate, n);
                totalMaintenanceCost += maintenanceCostPerReplacement * SCA * SPV;
            }

            return totalMaintenanceCost;
        }

        private double CalculateTotalCostToday(double materialCost, double laborCost, double totalMaintenanceCost, double endofLifeValue, double otherRelatedCost, double locationPriceFactor)
        {
            return (materialCost + laborCost + totalMaintenanceCost + endofLifeValue + otherRelatedCost) * locationPriceFactor;
        }

        private double CalculateEscalatedLifeCycleCost(double totalCostToday, double escalationRate, double projectDuration)
        {
            return totalCostToday * Math.Pow(1 + escalationRate, projectDuration);
        }

        private double CalculatePresentValue(double futureCost, double discountRate, double projectDuration)
        {
            return futureCost / Math.Pow(1 + discountRate, projectDuration);
        }

        public void SetProjectNameField(string projectName)
        {
            projectNameField.Text = projectName;
        }

        //public void SetLocationField(string location)
        //{
        //    SLEUtility.SetLocationField(new Location { City = location }, locationField);
        //    AdjustLocationDisplay();
        //    UpdateLaborCost();
        //}

        public void SetLocationField(string location)
        {
            if (string.IsNullOrWhiteSpace(location))
            {
                throw new ArgumentException("Location cannot be null or empty.", nameof(location));
            }

            // Example: Parse location string to extract City and Country
            string[] locationParts = location.Split(',');
            string city = locationParts.Length > 0 ? locationParts[0].Trim() : "Unknown";
            string country = locationParts.Length > 1 ? locationParts[1].Trim() : "Unknown";

            // Create a Location object
            var locationObj = new Location
            {
                City = city,
                Country = country,
            };

            // Use the utility method to populate the ComboBox
            SLEUtility.SetLocationField(locationObj, locationField);

            // Validate the items in the ComboBox
            foreach (var item in locationField.Items)
            {
                if (item is not LocationWrapper)
                {
                    throw new InvalidOperationException("Invalid item type found in locationField.");
                }
            }

            AdjustLocationDisplay();
            UpdateLaborCost();
        }


        public void AdjustLocationDisplay()
        {
            var items = locationField.Items.Cast<object>().ToList(); // Handle mixed types
            locationField.Items.Clear();

            foreach (var item in items)
            {
                if (item is LocationWrapper location)
                {
                    locationField.Items.Add(location); // Re-add LocationWrapper directly
                }
                else if (item is string locationString)
                {
                    // Convert string to LocationWrapper (attempt to parse city, country)
                    var parts = locationString.Split(',');
                    var locationWrapper = new LocationWrapper
                    {
                        City = parts.Length > 0 ? parts[0].Trim() : "Unknown",
                        Country = parts.Length > 1 ? parts[1].Trim() : "Unknown"
                    };
                    locationField.Items.Add(locationWrapper);
                }
            }

            if (locationField.Items.Count > 0)
            {
                locationField.SelectedIndex = 0; // Set default selection
            }
        }

        public void UpdateLaborCost()
        {
            if (locationField.SelectedItem is LocationWrapper selectedLocation)
            {
                string city = selectedLocation.City;
                string country = selectedLocation.Country;

                if (laborCostData.TryGetValue(city, out double laborCost))
                {
                    laborCostField.Text = laborCost.ToString("N2", CultureInfo.InvariantCulture);
                }
                else
                {
                    laborCostField.Text = "0.00";
                }
            }
            else
            {
                laborCostField.Text = "0.00";
            }

        }

        public void SetCurrencyField(string currency)
        {
            foreach (ComboBoxItem item in currencyField.Items)
            {
                if (item.Content.ToString() == currency)
                {
                    currencyField.SelectedItem = item;
                    break;
                }
            }
            UpdateChosenCurrencyTextBlock(currency);
        }

        public void SetMaterialField(string material)
        {
            materialField.Text = material;
        }

        public void SetMaterialQuantityField(double value)
        {
            materialQuantityField.Text = value.ToString("N5", CultureInfo.InvariantCulture);
        }

        //public void SetMaterialPriceField(double value)
        //{
        //    materialPriceField.Text = value.ToString("N2", CultureInfo.InvariantCulture);
        //}

        //public void SetMaterialPriceFactorField(double value)
        //{
        //    materialPriceFactorField.Text = value.ToString("N2", CultureInfo.InvariantCulture);
        //}

        //public void SetLocationPriceFactorField(double value)
        //{
        //    locationPriceFactorField.Text = value.ToString("N2", CultureInfo.InvariantCulture);
        //}

        //public void SetLaborCostField(double value)
        //{
        //    laborCostField.Text = value.ToString("N2", CultureInfo.InvariantCulture);
        //}

        //public void SetUnitTimeField(double value)
        //{
        //    unitTimeField.Text = value.ToString("N2", CultureInfo.InvariantCulture);
        //}

        //public void SetEscalationRateField(double value)
        //{
        //    escalationRateField.Text = value.ToString("N2", CultureInfo.InvariantCulture);
        //}

        //public void SetDiscountRateField(double value)
        //{
        //    discountRateField.Text = value.ToString("N2", CultureInfo.InvariantCulture);
        //}

        public void SetPresentValueDiscountField(double value)
        {
            presentValueDiscountField.Text = value.ToString("N2", CultureInfo.InvariantCulture);
        }

        public void SetMaintenanceCostField(double value)
        {
            calculatedMaintenanceCostField.Text = value.ToString("N2", CultureInfo.InvariantCulture);
        }

        //public void SetEndofLifeValueField(double value)
        //{
        //    EndofLifeValueField.Text = value.ToString("N2", CultureInfo.InvariantCulture);
        //}

        public void SetElementDescriptionField(string description)
        {
            elementDescriptionField.Text = description;
        }

        //public void SetElementServiceLifeDurationField(int value)
        //{
        //    elementServiceLifeDurationField.Text = value.ToString();
        //}

        public void SetElementServiceLifeDurationField(double value)
        {
            elementServiceLifeDurationField.Text = value.ToString("N2"); // Format as needed
        }

        public void SetMaterialPriceField(string value)
        {
            materialPriceField.Text = string.IsNullOrEmpty(value) ? string.Empty : value;
        }

        public void SetMaterialPriceFactorField(string value)
        {
            materialPriceFactorField.Text = string.IsNullOrEmpty(value) ? string.Empty : value;
        }

        public void SetLaborCostField(string value)
        {
            laborCostField.Text = string.IsNullOrEmpty(value) ? string.Empty : value;
        }

        public void SetUnitTimeField(string value)
        {
            unitTimeField.Text = string.IsNullOrEmpty(value) ? string.Empty : value;
        }

        public void SetEndofLifeValueField(string value)
        {
            EndofLifeValueField.Text = string.IsNullOrEmpty(value) ? string.Empty : value;
        }

        public void SetEscalationRateField(string value)
        {
            escalationRateField.Text = string.IsNullOrEmpty(value) ? string.Empty : value;
        }

        public void SetDiscountRateField(string value)
        {
            discountRateField.Text = string.IsNullOrEmpty(value) ? string.Empty : value;
        }

        public void SetLocationPriceFactorField(string value)
        {
            locationPriceFactorField.Text = string.IsNullOrEmpty(value) ? string.Empty : value;
        }


        private void UpdateChosenCurrencyTextBlock(string currency)
        {
            if (chosenCurrencyTextBlock != null)
            {
                chosenCurrencyTextBlock.Text = $"Currency: {currency}";
            }
        }

        private void CurrencyField_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (currencyField.SelectedItem is ComboBoxItem selectedItem)
            {
                string selectedCurrency = selectedItem.Content.ToString();
                UpdateChosenCurrencyTextBlock(selectedCurrency);
            }
        }

        private void LocationField_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateLaborCost();
        }

        private void CheckBox_CheckedChanged(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = sender as CheckBox;

            if (checkBox == null)
                return;

            if (checkBox == laborInputsCheckBox)
            {
                bool isChecked = checkBox.IsChecked ?? false;
                if (laborCostField != null)
                    laborCostField.IsEnabled = isChecked;
                if (unitTimeField != null)
                    unitTimeField.IsEnabled = isChecked;
            }
            else if (checkBox == maintenanceCostsCheckBox)
            {
                bool isChecked = checkBox.IsChecked ?? false;
                if (calculatedMaintenanceCostField != null)
                    calculatedMaintenanceCostField.IsEnabled = isChecked;
            }
            else if (checkBox == endofLifeValueCheckBox)
            {
                bool isChecked = checkBox.IsChecked ?? false;
                if (EndofLifeValueField != null)
                    EndofLifeValueField.IsEnabled = isChecked;
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_currentElementIndex < 0 || _currentElementIndex >= _elementsInDuplicatedView.Count)
                {
                    MessageBox.Show("No element is currently selected.", "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                ElementId selectedElementId = _elementsInDuplicatedView[_currentElementIndex];
                Element selectedElement = _doc.GetElement(selectedElementId);

                if (selectedElement == null)
                {
                    MessageBox.Show("The selected element could not be found.", "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                using (Transaction trans = new Transaction(_doc, "Update Element Shared Parameters"))
                {
                    trans.Start();

                    // Ensure all necessary shared parameters exist
                    EnsureSharedParameterExists("Element_Material Cost Today", SpecTypeId.Currency);
                    EnsureSharedParameterExists("Element_Labor Cost Today", SpecTypeId.Currency);
                    EnsureSharedParameterExists("Element_Total Maintenance Cost Today", SpecTypeId.Currency);
                    EnsureSharedParameterExists("Initial Element Costs", SpecTypeId.Currency);
                    EnsureSharedParameterExists("Future Element Cost", SpecTypeId.Currency);
                    EnsureSharedParameterExists("Present Value of Escalated Element Cost", SpecTypeId.Currency);
                    EnsureSharedParameterExists("Element_End of Life Value", SpecTypeId.Currency);
                    EnsureSharedParameterExists("Material Price per Unit", SpecTypeId.Currency);
                    EnsureSharedParameterExists("Material Price Factor", SpecTypeId.Number);
                    EnsureSharedParameterExists("Labor Cost per Hour", SpecTypeId.Currency);
                    EnsureSharedParameterExists("Unit Time Required", SpecTypeId.Number);
                    EnsureSharedParameterExists("User End of Life Value", SpecTypeId.Currency);

                    // Retrieve shared parameters
                    Parameter materialCostParam = selectedElement.LookupParameter("Element_Material Cost Today");
                    Parameter laborCostParam = selectedElement.LookupParameter("Element_Labor Cost Today");
                    Parameter maintenanceCostParam = selectedElement.LookupParameter("Element_Total Maintenance Cost Today");
                    Parameter initialCostParam = selectedElement.LookupParameter("Initial Element Costs");
                    Parameter futureCostParam = selectedElement.LookupParameter("Future Element Cost");
                    Parameter presentValueCostParam = selectedElement.LookupParameter("Present Value of Escalated Element Cost");
                    Parameter endOfLifeValueParam = selectedElement.LookupParameter("Element_End of Life Value");

                    // New shared parameters to save
                    Parameter materialPricePerUnitParam = selectedElement.LookupParameter("Material Price per Unit");
                    Parameter materialPriceFactorParam = selectedElement.LookupParameter("Material Price Factor");
                    Parameter laborCostPerHourParam = selectedElement.LookupParameter("Labor Cost per Hour");
                    Parameter unitTimeRequiredParam = selectedElement.LookupParameter("Unit Time Required");
                    Parameter userEndOfLifeValueParam = selectedElement.LookupParameter("User End of Life Value");

                    // Set shared parameters with calculated values using SafeParseDouble
                    if (materialCostParam != null)
                        materialCostParam.Set(SafeParseDouble(calculatedMaterialCostField.Text));

                    if (laborCostParam != null)
                        laborCostParam.Set(SafeParseDouble(calculatedLaborCostField.Text));

                    if (maintenanceCostParam != null)
                        maintenanceCostParam.Set(SafeParseDouble(calculatedMaintenanceCostField.Text));

                    if (initialCostParam != null)
                        initialCostParam.Set(SafeParseDouble(initialElementCostsField.Text));

                    if (futureCostParam != null)
                        futureCostParam.Set(SafeParseDouble(futureElementCostEscalationField.Text));

                    if (presentValueCostParam != null)
                        presentValueCostParam.Set(SafeParseDouble(presentValueDiscountField.Text));

                    if (endOfLifeValueParam != null)
                        endOfLifeValueParam.Set(SafeParseDouble(EndofLifeValueField.Text));

                    // Set new shared parameters with user input values
                    if (materialPricePerUnitParam != null)
                        materialPricePerUnitParam.Set(SafeParseDouble(materialPriceField.Text));

                    if (materialPriceFactorParam != null)
                        materialPriceFactorParam.Set(SafeParseDouble(materialPriceFactorField.Text));

                    if (laborCostPerHourParam != null)
                        laborCostPerHourParam.Set(SafeParseDouble(laborCostField.Text));

                    if (unitTimeRequiredParam != null)
                        unitTimeRequiredParam.Set(SafeParseDouble(unitTimeField.Text));

                    if (userEndOfLifeValueParam != null)
                        userEndOfLifeValueParam.Set(SafeParseDouble(EndofLifeValueField.Text));

                    trans.Commit();
                }

                MessageBox.Show("Results saved successfully.", "Save Results", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error: {ex.Message}", "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private double SafeParseDouble(string value, double defaultValue = 0.0)
        {
            if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out double result))
            {
                return result;
            }
            return defaultValue;
        }


        private void EnsureSharedParameterExists(string paramName, ForgeTypeId paramType)
        {
            DefinitionFile sharedParametersFile = _uiApp.Application.OpenSharedParameterFile();
            if (sharedParametersFile == null)
            {
                MessageBox.Show("Shared parameters file not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Find or create the group
            DefinitionGroup sharedParameterGroup = sharedParametersFile.Groups.get_Item("CustomParameters");
            if (sharedParameterGroup == null)
            {
                sharedParameterGroup = sharedParametersFile.Groups.Create("CustomParameters");
            }

            // Find or create the definition
            Definition sharedParameterDefinition = sharedParameterGroup.Definitions.get_Item(paramName);
            if (sharedParameterDefinition == null)
            {
                ExternalDefinitionCreationOptions options = new ExternalDefinitionCreationOptions(paramName, paramType);
                sharedParameterDefinition = sharedParameterGroup.Definitions.Create(options);
            }

            // Check if the shared parameter is already bound
            BindingMap bindingMap = _doc.ParameterBindings;
            bool isParameterBound = bindingMap.Contains(sharedParameterDefinition);
            if (!isParameterBound)
            {
                CategorySet categories = _uiApp.Application.Create.NewCategorySet();
                categories.Insert(_doc.Settings.Categories.get_Item(BuiltInCategory.OST_Walls)); // Adjust this as needed
                categories.Insert(_doc.Settings.Categories.get_Item(BuiltInCategory.OST_Floors)); // Adjust this as needed

                InstanceBinding newIB = _uiApp.Application.Create.NewInstanceBinding(categories);
                bindingMap.Insert(sharedParameterDefinition, newIB, BuiltInParameterGroup.PG_DATA);
            }
        }



        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            System.Windows.Controls.TextBox textBox = sender as System.Windows.Controls.TextBox;

            if (textBox != null)
            {
                if (string.IsNullOrEmpty(textBox.Text))
                {
                    textBox.BorderBrush = Brushes.Red;
                }
                else
                {
                    textBox.ClearValue(BorderBrushProperty);
                }
            }
        }
    }
}

using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using static RevitWoodLCC.SLEUtility;

namespace RevitWoodLCC
{
    public partial class ProjectMainWindow : Window
    {
        private UIApplication _projectUiApp;
        private UIDocument _projectUiDoc;
        private Document _projectDoc;
        private IList<ElementData> _elementDataList;
        private Dictionary<string, double> laborCostData;
        private ExternalCommandData _commandData;

        public ProjectMainWindow(UIApplication uiApp, UIDocument uiDoc)
        {
            // Set culture settings
            CultureInfo customCulture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
            customCulture.NumberFormat.NumberDecimalSeparator = ".";
            customCulture.NumberFormat.NumberGroupSeparator = ",";
            Thread.CurrentThread.CurrentCulture = customCulture;
            Thread.CurrentThread.CurrentUICulture = customCulture;

            InitializeComponent();

            _projectUiApp = uiApp;
            _projectUiDoc = uiDoc;
            _projectDoc = uiDoc.Document;

            // Debugging message
            //TaskDialog.Show("Debug", "Initializing ProjectMainWindow");

            InitializeDefaultValues();
            InitializeLaborCostData();
            LoadElements();
            ElementDataGrid.ItemsSource = _elementDataList;

            string projectName = _projectDoc.ProjectInformation.Name;
            SetProjectNameField(projectName);

            SLEUtility.LoadLocation(locationField, _commandData, "Above Ground"); // Pass the commandData and condition
            AdjustLocationDisplay();
        }

        private void InitializeDefaultValues()
        {
            projectEndofLifeDurationField.Text = "60"; // Set a default value
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

        private void LoadElements()
        {
            _elementDataList = new List<ElementData>();

            FilteredElementCollector collector = new FilteredElementCollector(_projectDoc);
            IList<ElementId> allElementIds = collector.WhereElementIsNotElementType().ToElementIds().ToList();

            // Debugging message
           // TaskDialog.Show("Debug", $"Found {allElementIds.Count} elements");

            foreach (ElementId elementId in allElementIds)
            {
                Element element = _projectDoc.GetElement(elementId);
                if (IsElementValid(elementId))
                {
                    ElementData data = new ElementData
                    {
                        ElementDescription = $"ID: {element.Id}, Name: {element.Name}",
                        ElementServiceLifeDuration = GetElementServiceLifeDuration(element),
                        MaterialType = GetMaterialType(element),
                        MaterialQuantity = GetMaterialQuantity(element),
                        Unit = "m³",
                        MaterialPricePerUnit = GetParameterAsDouble(element, "Material Price per Unit"),  // User-defined value
                        MaterialPriceFactor = GetParameterAsDouble(element, "Material Price Factor"),    // User-defined value
                        CalculatedMaterialCost = GetParameterAsDouble(element, "Element_Material Cost Today"),
                        LaborCostPerHour = GetParameterAsDouble(element, "Labor Cost per Hour"),         // User-defined value
                        UnitTimeRequired = GetParameterAsDouble(element, "Unit Time Required"),         // User-defined value
                        CalculatedLaborCost = GetParameterAsDouble(element, "Element_Labor Cost Today"),
                        CalculatedMaintenanceCost = GetParameterAsDouble(element, "Element_Total Maintenance Cost Today"),
                        EndOfLifeValue = GetParameterAsDouble(element, "Element_End of Life Value"),
                        ElementCostsToday = GetParameterAsDouble(element, "Initial Element Costs"),
                        FutureElementCost = GetParameterAsDouble(element, "Future Element Cost"),
                        PresentValueOfEscalatedCost = GetParameterAsDouble(element, "Present Value of Escalated Element Cost")
                    };

                    // Debugging message for each element
                    //TaskDialog.Show("Debug", $"Element ID: {element.Id}\n" +
                    //                         $"Service Life Duration: {data.ElementServiceLifeDuration}\n" +
                    //                         $"Calculated Material Cost: {data.CalculatedMaterialCost}\n" +
                    //                         $"Calculated Labor Cost: {data.CalculatedLaborCost}");

                    _elementDataList.Add(data);
                }
            }

            ElementDataGrid.ItemsSource = _elementDataList;
        }

        private double GetParameterAsDouble(Element element, string paramName)
        {
            Parameter param = element.LookupParameter(paramName);
            if (param == null || !param.HasValue)
            {
                // Debugging message
                //TaskDialog.Show("Debug", $"Parameter {paramName} not found or has no value");
                return 0;
            }

            return param.AsDouble();
        }

        private double GetUserDefinedParameter(Element element, string paramName)
        {
            // Here you would retrieve the user-defined parameter value, if it's stored elsewhere
            // For this example, we'll return a placeholder value
            return 0;
        }

        private int GetElementServiceLifeDuration(Element element)
        {
            Parameter serviceLifeDurationParam = element.LookupParameter("Element_Service Life Duration");
            if (serviceLifeDurationParam == null || !serviceLifeDurationParam.HasValue)
            {
                // Debugging message
                //TaskDialog.Show("Debug", $"Service Life Duration parameter not found or has no value");
                return 0;
            }

            return serviceLifeDurationParam.AsInteger();
        }

        private bool IsElementValid(ElementId elementId)
        {
            Element element = _projectDoc.GetElement(elementId);
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
                    else if (geomObj is GeometryInstance geomInstance)
                    {
                        GeometryElement instanceGeomElement = geomInstance.GetInstanceGeometry();
                        foreach (GeometryObject instanceGeomObj in instanceGeomElement)
                        {
                            if (instanceGeomObj is Solid instanceSolid && instanceSolid.Volume > 0)
                            {
                                return true;
                            }
                        }
                    }
                }
            }

            return false;
        }

        private string GetMaterialType(Element element)
        {
            Material material = GetElementMaterial(element);
            return material != null ? material.Name : "Unknown";
        }

        private double GetMaterialQuantity(Element element)
        {
            double volume = element.LookupParameter("Volume")?.AsDouble() ?? 0;
            return UnitUtils.ConvertFromInternalUnits(volume, UnitTypeId.CubicMeters); // Use the correct UnitTypeId
        }

        private Material GetElementMaterial(Element element)
        {
            string[] materialParamNames = { "Material", "Structural Material", "Finish Material", "Surface Material" };

            foreach (string paramName in materialParamNames)
            {
                Parameter materialParam = element.LookupParameter(paramName);
                if (materialParam != null && materialParam.HasValue)
                {
                    ElementId materialId = materialParam.AsElementId();
                    Material material = _projectDoc.GetElement(materialId) as Material;

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
                        Material material = GetMaterialFromSolid(solid);
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
                                Material material = GetMaterialFromSolid(instanceSolid);
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

        private Material GetMaterialFromSolid(Solid solid)
        {
            foreach (Face face in solid.Faces)
            {
                ElementId materialId = face.MaterialElementId;
                Material material = _projectDoc.GetElement(materialId) as Material;
                if (material != null)
                {
                    return material;
                }
            }
            return null;
        }



        private void CalculateBuildingLCCButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                double totalProjectCostToday = 0;
                double totalFutureProjectCost = 0;
                double totalPresentValueDiscount = 0;

                // Parse the conversion rate; assume correct input for simplification
                if (!double.TryParse(ConversionRateField.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double conversionRate))
                {
                    MessageBox.Show("Invalid input for Conversion Rate. Please enter a valid number.", "Input Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Sum up the costs from each element
                foreach (ElementData data in _elementDataList)
                {
                    totalProjectCostToday += data.ElementCostsToday;
                    totalFutureProjectCost += data.FutureElementCost;
                    totalPresentValueDiscount += data.PresentValueOfEscalatedCost;
                }

                // Display the total costs
                ProjectCostTodayField.Text = totalProjectCostToday.ToString("N2", CultureInfo.InvariantCulture);
                FutureProjectCostField.Text = totalFutureProjectCost.ToString("N2", CultureInfo.InvariantCulture);
                PresentValueDiscountProjectField.Text = totalPresentValueDiscount.ToString("N2", CultureInfo.InvariantCulture);

                // Display the converted costs
                InitialProjectCostsExtraField.Text = (totalProjectCostToday * conversionRate).ToString("N2", CultureInfo.InvariantCulture);
                FutureProjectCostEscalationExtraField.Text = (totalFutureProjectCost * conversionRate).ToString("N2", CultureInfo.InvariantCulture);
                PresentValueDiscountExtraField.Text = (totalPresentValueDiscount * conversionRate).ToString("N2", CultureInfo.InvariantCulture);

                MessageBox.Show("Calculations completed successfully.", "Building LCC Calculation Result", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}", "Calculation Error", MessageBoxButton.OK, MessageBoxImage.Error);
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

        private void LocationField_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateLaborCost();
        }

        private void FutureProjectCostEscalationExtraField_TextChanged(object sender, TextChangedEventArgs e)
        {
        }

        public void SetProjectNameField(string projectName)
        {
            projectNameField.Text = projectName;
        }

        public void SetEscalationRateField(double value)
        {
            escalationRateField.Text = value.ToString("N2", CultureInfo.InvariantCulture);
        }

        public void SetDiscountRateField(double value)
        {
            discountRateField.Text = value.ToString("N2", CultureInfo.InvariantCulture);
        }

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

    }

    public class ElementData
    {
        public string ElementDescription { get; set; }
        public int ElementServiceLifeDuration { get; set; }
        public string MaterialType { get; set; }
        public double MaterialQuantity { get; set; }
        public string Unit { get; set; }
        public double MaterialPricePerUnit { get; set; }
        public double MaterialPriceFactor { get; set; }
        public double CalculatedMaterialCost { get; set; }
        public double LaborCostPerHour { get; set; }
        public double UnitTimeRequired { get; set; }
        public double CalculatedLaborCost { get; set; }
        public double CalculatedMaintenanceCost { get; set; }
        public double EndOfLifeValue { get; set; }
        public double ElementCostsToday { get; set; }
        public double FutureElementCost { get; set; }
        public double PresentValueOfEscalatedCost { get; set; }
    }
}

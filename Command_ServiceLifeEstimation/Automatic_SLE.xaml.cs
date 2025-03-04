
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using GMap.NET.MapProviders;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Media.Media3D;
using static IronPython.Modules.PythonWeakRef;
using System.Windows.Controls; // Add this namespace


namespace RevitWoodLCC
{
    public partial class Automatic_SLE : Window
    {
        private readonly UIDocument _uiDoc;
        private readonly Document _doc;
        private readonly IList<Reference> _selectedElements;
        private readonly List<ElementCondition> aboveGroundConditions = new();
        private readonly List<ElementCondition> inGroundConditions = new();
        private List<MaterialData> materials;

        public Automatic_SLE(UIDocument uiDoc, IList<Reference> selectedElements)
        {
            InitializeComponent();
            _uiDoc = uiDoc ?? throw new ArgumentNullException(nameof(uiDoc));
            _doc = _uiDoc.Document;
            _selectedElements = selectedElements ?? throw new ArgumentNullException(nameof(selectedElements));

            try
            {
                materials = MaterialImportUtility.GetAllMaterials();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load material data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                materials = new List<MaterialData>();
            }

            // Attach event handler to the Compute button
            btnCompute.Click += BtnCompute_Click;
        }

        private void BtnCompute_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (rbtnAllElements.IsChecked == true)
                {
                    StoreAllElements();
                }
                else if (rbtnExposedElements.IsChecked == true)
                {
                    StoreExposedElements();
                }
                else
                {
                    MessageBox.Show("Please select an option before clicking Compute.", "Input Required", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                ProcessStoredElements();
                DisplaySummaryFeedback();
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void StoreAllElements()
        {
            ClearStoredConditions();

            foreach (Reference reference in _selectedElements)
            {
                Element element = _doc.GetElement(reference);
                CategorizeAndStoreElementCondition(element);
            }
        }
        private void StoreExposedElements()
        {
            ClearStoredConditions();
            string message = "";

            foreach (Reference reference in _selectedElements)
            {
                Element element = _doc.GetElement(reference);

                // Initialize the lists to store ray information
                List<VerticalRayInfo> verticalRayInfos = new();
                List<ThirtyDegreeRayInfo> angledRayInfos = new();

                // Use the utility method to check if the element is sheltered
                bool isExposed = !SLE_AutoPopulateUtility.CheckIfElementIsSheltered(element, _doc, ref message, verticalRayInfos, angledRayInfos);

                if (isExposed)
                {
                    CategorizeAndStoreElementCondition(element);
                }
            }
        }


        private void ClearStoredConditions()
        {
            aboveGroundConditions.Clear();
            inGroundConditions.Clear();
        }

        private void CategorizeAndStoreElementCondition(Element element)
        {
            var condition = CreateElementCondition(element);
            if (condition.GroundCondition == "In-Ground")
            {
                inGroundConditions.Add(condition);
            }
            else
            {
                aboveGroundConditions.Add(condition);
            }
        }

        private ElementCondition CreateElementCondition(Element element)
        {
            var condition = new ElementCondition
            {
                ElementId = element.Id.IntegerValue,
                Name = element.Name,
                Category = element.Category?.Name
            };
            ProcessGroundCondition(element, condition);
            return condition;
        }

        private void DisplaySummaryFeedback()
        {
            StringBuilder feedback = new StringBuilder();
            feedback.AppendLine("Processed Elements Summary:");
            feedback.AppendLine("--------------------------");

            feedback.AppendLine("In-Ground Elements:");
            foreach (var condition in inGroundConditions)
            {
                AppendElementFeedback(feedback, condition);
            }

            feedback.AppendLine("Above-Ground Elements:");
            foreach (var condition in aboveGroundConditions)
            {
                AppendElementFeedback(feedback, condition);
            }

            MessageBox.Show(feedback.ToString(), "Summary of Processed Elements", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void AppendElementFeedback(StringBuilder feedback, ElementCondition condition)
        {
            feedback.AppendLine($"Element ID: {condition.ElementId}");
            feedback.AppendLine($"Name: {condition.Name}");
            feedback.AppendLine($"Category: {condition.Category}");
            feedback.AppendLine($"Ground Condition: {condition.GroundCondition}");
            feedback.AppendLine($"Latitude: {condition.Latitude:F6}");
            feedback.AppendLine($"Longitude: {condition.Longitude:F6}");
            feedback.AppendLine($"City: {condition.City}");
            feedback.AppendLine($"Country: {condition.Country}");
            feedback.AppendLine($"Material: {condition.Material ?? "Not available"}");
            feedback.AppendLine($"Treatment: {condition.Treatment ?? "Not available"}");
            feedback.AppendLine();

            if (condition.GroundCondition == "In-Ground")
            {
                feedback.AppendLine($"In-Ground Resistance Dose: {condition.InGroundResistanceDose?.ToString("F2") ?? "Not available"}");
                feedback.AppendLine($"In-Ground Environmental Doses: {condition.InGroundLocationData?.Doses?.ToString("F2") ?? "Not available"}");
                feedback.AppendLine($"In-Ground Service Life Duration: {condition.InGroundServiceLifeDuration?.ToString("F2") ?? "Not available"} years");
                feedback.AppendLine($"In-Ground Service Life Message: {condition.InGroundServiceLifeMessage ?? "Not available"}");
                feedback.AppendLine();
            }
            else if (condition.GroundCondition == "Above-Ground")
            {
                feedback.AppendLine($"Above-Ground Resistance Dose: {condition.AboveGroundResistanceDose?.ToString("F2") ?? "Not available"}");
                feedback.AppendLine($"Above-Ground Reference Dose (DRef): {condition.AboveGroundLocationData?.DRef?.ToString("F2") ?? "Not available"}");
                feedback.AppendLine($"Exposure Adjustment Factor (k2): {condition.AboveGroundK2?.ToString("F2") ?? "Not available"}");
                feedback.AppendLine($"Shelter Adjustment Factor (k3): {condition.AboveGroundK3?.ToString("F2") ?? "Not available"}");
                feedback.AppendLine($"Above-Ground Service Life Duration: {condition.AboveGroundServiceLifeDuration?.ToString("F2") ?? "Not available"} years");
                feedback.AppendLine($"Above-Ground Service Life Message: {condition.AboveGroundServiceLifeMessage ?? "Not available"}");
                feedback.AppendLine();
            }



        }



        private void ProcessStoredElements()
        {
            foreach (var elementCondition in aboveGroundConditions.Concat(inGroundConditions))
            {
                try
                {
                    Element element = _doc.GetElement(new ElementId(elementCondition.ElementId));

                    // Common processing for both in-ground and above-ground elements
                    ProcessMaterialAndTreatment(element, elementCondition);
                    ProcessLocationData(element, elementCondition);
                    ProcessGroundCondition(element, elementCondition);

                    // Additional processing for above-ground elements only
                    if (elementCondition.GroundCondition == "Above-Ground")
                    {
                        ProcessShelteringAndExposure(element, elementCondition);
                        ProcessIntersectionCondition(element, elementCondition);
                    }

                    // Determine and set IsVertical
                    string orientation = SLE_AutoPopulateUtility.DetermineElementOrientation(element);
                    elementCondition.IsVertical = orientation == "Vertical";

                    // Compute service life duration based on ground condition
                    ComputeServiceLifeDuration(elementCondition);
                }
                catch (Exception ex)
                {
                    // Handle errors for individual element failures
                    elementCondition.Message += $"Error processing element {elementCondition.Name}: {ex.Message}\n";
                }
            }
        }


        private void ComputeServiceLifeDuration(ElementCondition elementCondition)
        {
            try
            {
                // Determine which type of calculation to run based on the ground condition
                switch (elementCondition.GroundCondition)
                {
                    case "In-Ground":
                        ComputeInGroundServiceLife(elementCondition);
                        break;

                    case "Above-Ground":
                        ComputeAboveGroundServiceLife(elementCondition);
                        break;

                    default:
                        if (elementCondition.GroundCondition == "In-Ground")
                        {
                            elementCondition.InGroundServiceLifeMessage = "Error: Unknown ground condition. Cannot compute service life.\n";
                        }
                        else if (elementCondition.GroundCondition == "Above-Ground")
                        {
                            elementCondition.AboveGroundServiceLifeMessage = "Error: Unknown ground condition. Cannot compute service life.\n";
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                if (elementCondition.GroundCondition == "In-Ground")
                {
                    elementCondition.InGroundServiceLifeMessage += $"Error computing service life: {ex.Message}\n";
                }
                else if (elementCondition.GroundCondition == "Above-Ground")
                {
                    elementCondition.AboveGroundServiceLifeMessage += $"Error computing service life: {ex.Message}\n";
                }
            }
        }


        private void ComputeInGroundServiceLife(ElementCondition elementCondition)
        {
            try
            {
                // Retrieve doses from location data
                double? doses = elementCondition.InGroundLocationData?.Doses;

                // Validate doses and material data
                if (!doses.HasValue)
                {
                    elementCondition.InGroundServiceLifeMessage = "Error: Missing or invalid doses data for in-ground calculation.\n";
                    return;
                }

                if (elementCondition.MatchedMaterialData == null || elementCondition.MatchedMaterialData.ResistanceDoseUC4 == null)
                {
                    elementCondition.InGroundResistanceDoseMessage = "Error: Material data for in-ground conditions is missing or unmatched.\n";
                    return;
                }

                // Store in-ground resistance dose
                elementCondition.InGroundResistanceDose = elementCondition.MatchedMaterialData.ResistanceDoseUC4;

                // Call CalculateInGroundServiceLife with validated inputs
                elementCondition.InGroundServiceLifeDuration = SLE_PopupForm_Logic.CalculateInGroundServiceLife(
                    elementCondition.MatchedMaterialData,
                    doses.Value
                );

                // Validate and display the result
                if (elementCondition.InGroundServiceLifeDuration.HasValue && elementCondition.InGroundServiceLifeDuration > 0)
                {
                    elementCondition.InGroundServiceLifeMessage = $"In-Ground Service Life: {elementCondition.InGroundServiceLifeDuration:F2} years\n";
                }
                else
                {
                    elementCondition.InGroundServiceLifeMessage = "Error: Computed in-ground service life duration is invalid (zero or negative).\n";
                }
            }
            catch (Exception ex)
            {
                // Log any exceptions that occur during the calculation
                elementCondition.InGroundServiceLifeMessage = $"Error computing in-ground service life: {ex.Message}\n";
            }
        }

        private void ComputeAboveGroundServiceLife(ElementCondition elementCondition)
        {
            try
            {
                // Validate and convert AboveGroundLocationData to LocationDetails
                if (elementCondition.AboveGroundLocationData == null)
                {
                    elementCondition.AboveGroundServiceLifeMessage = "Error: Above-ground location data is missing.\n";
                    return;
                }

                var locationDetails = ConvertToLocationDetails(elementCondition.AboveGroundLocationData);

                // Compute k2 (exposure adjustment factor)
                double k2 = CalculateK2(elementCondition, locationDetails);
                if (k2 <= 0)
                {
                    elementCondition.AboveGroundServiceLifeMessage += "Error: k2 factor computation failed. Service life calculation cannot proceed.\n";
                    return;
                }
                elementCondition.AboveGroundK2 = k2; // Store k2 in ElementCondition for feedback

                // Compute k3 (shelter adjustment factor)
                double k3 = CalculateK3(elementCondition, locationDetails, k2);
                if (k3 <= 0)
                {
                    elementCondition.AboveGroundServiceLifeMessage += "Error: k3 factor computation failed. Service life calculation cannot proceed.\n";
                    return;
                }
                elementCondition.AboveGroundK3 = k3; // Store k3 in ElementCondition for feedback

                // Store above-ground resistance dose
                elementCondition.AboveGroundResistanceDose = elementCondition.MatchedMaterialData?.ResistanceDoseUC3;
                elementCondition.AboveGroundResistanceDoseMessage = $"Above-Ground Resistance Dose UC3: {elementCondition.AboveGroundResistanceDose}\n";

                // Retrieve dRef and validate
                double? dRef = elementCondition.AboveGroundLocationData?.DRef;
                if (dRef.HasValue && elementCondition.MatchedMaterialData != null)
                {
                    elementCondition.AboveGroundDRef = dRef.Value; // Store dRef in ElementCondition for feedback

                    // Call CalculateAboveGroundServiceLife
                    elementCondition.AboveGroundServiceLifeDuration = SLE_PopupForm_Logic.CalculateAboveGroundServiceLife(
                        elementCondition.MatchedMaterialData,
                        dRef.Value,
                        k2,
                        k3
                    );

                    if (elementCondition.AboveGroundServiceLifeDuration.HasValue && elementCondition.AboveGroundServiceLifeDuration > 0)
                    {
                        elementCondition.AboveGroundServiceLifeMessage = $"Above-Ground Service Life: {elementCondition.AboveGroundServiceLifeDuration:F2} years\n";
                    }
                    else
                    {
                        elementCondition.AboveGroundServiceLifeMessage = "Error: Computed above-ground service life duration is invalid (zero or negative).\n";
                    }
                }
                else
                {
                    elementCondition.AboveGroundServiceLifeMessage = "Error: Missing above-ground reference dose (dRef) or material data.\n";
                }
            }
            catch (Exception ex)
            {
                elementCondition.AboveGroundServiceLifeMessage += $"Error computing above-ground service life: {ex.Message}\n";
            }
        }

        private double CalculateK2(ElementCondition elementCondition, SLEUtility.LocationDetails locationDetails)
        {
            try
            {
                if (elementCondition.GroundCondition != "Above-Ground")
                {
                    throw new InvalidOperationException("k2 factor is only applicable for above-ground conditions.");
                }

                double k2 = SLE_PopupForm_Logic.RetrieveAndStoreSelections(
                    elementCondition.ExposureCondition,
                    elementCondition.IntersectionCondition,
                    locationDetails
                );

                if (k2 == 0)
                {
                    elementCondition.AboveGroundServiceLifeMessage += "Error: k2 factor calculation failed due to invalid inputs.\n";
                }

                return k2;
            }
            catch (Exception ex)
            {
                elementCondition.AboveGroundServiceLifeMessage += $"Error: Failed to calculate k2 factor: {ex.Message}\n";
                return 0;
            }
        }

        private double CalculateK3(ElementCondition elementCondition, SLEUtility.LocationDetails locationDetails, double k2)
        {
            try
            {
                if (elementCondition.GroundCondition != "Above-Ground")
                {
                    throw new InvalidOperationException("k3 factor is only applicable for above-ground conditions.");
                }

                double k3 = SLE_PopupForm_Logic.CalculateShelterAdjustmentFactor(
                    locationDetails,
                    k2,
                    elementCondition.IsVertical,
                    elementCondition.IsSheltered,
                    elementCondition.OverhangDistance,
                    elementCondition.ShelterDistance,
                    elementCondition.GroundDistance
                );

                if (k3 == 0)
                {
                    elementCondition.AboveGroundServiceLifeMessage += "Error: k3 factor calculation failed due to invalid inputs.\n";
                }

                return k3;
            }
            catch (Exception ex)
            {
                elementCondition.AboveGroundServiceLifeMessage += $"Error: Failed to calculate k3 factor: {ex.Message}\n";
                return 0;
            }
        }



        private void ProcessMaterialAndTreatment(Element element, ElementCondition elementCondition)
        {
            // Retrieve Material and Treatment
            elementCondition.Material = element.LookupParameter("Element_Material")?.AsString();
            elementCondition.Treatment = element.LookupParameter("Element_Treatment")?.AsString();

            // Validate Material and Treatment
            if (string.IsNullOrEmpty(elementCondition.Material))
            {
                elementCondition.MaterialAndTreatmentMessage = "Material parameter is missing or empty.\n";
                return;
            }

            if (string.IsNullOrEmpty(elementCondition.Treatment))
            {
                elementCondition.MaterialAndTreatmentMessage = "Treatment parameter is missing or empty.\n";
                return;
            }

            // Find the matching material data
            MaterialData matchingMaterial = materials.FirstOrDefault(m =>
                m.Name.Contains(elementCondition.Material, StringComparer.OrdinalIgnoreCase) &&
                m.Treatment.Contains(elementCondition.Treatment, StringComparer.OrdinalIgnoreCase));

            // Handle unmatched materials
            if (matchingMaterial == null)
            {
                elementCondition.MaterialAndTreatmentMessage = $"No matching material found for Material: {elementCondition.Material}, Treatment: {elementCondition.Treatment}.\n";
                return;
            }

            // Store the matched material data for reuse
            elementCondition.MatchedMaterialData = matchingMaterial;

            // Prepare the specific resistance dose message based on the ground condition
            if (elementCondition.GroundCondition == "In-Ground")
            {
                elementCondition.InGroundResistanceDose = matchingMaterial.ResistanceDoseUC4; // In-ground requires UC4 dose
                elementCondition.InGroundResistanceDoseMessage = $"Ground Condition: In-Ground\nResistance Dose UC4: {matchingMaterial.ResistanceDoseUC4}\n";
            }
            else
            {
                elementCondition.AboveGroundResistanceDose = matchingMaterial.ResistanceDoseUC3; // Above-ground requires UC3 dose
                elementCondition.AboveGroundResistanceDoseMessage = $"Ground Condition: Above-Ground\nResistance Dose UC3: {matchingMaterial.ResistanceDoseUC3}\n";
            }
        }




        private void ProcessLocationData(Element element, ElementCondition elementCondition)
        {
            // Extract latitude and longitude
            elementCondition.Latitude = element.LookupParameter("Element_Latitude")?.AsDouble() ?? 0.0;
            elementCondition.Longitude = element.LookupParameter("Element_Longitude")?.AsDouble() ?? 0.0;
            elementCondition.City = element.LookupParameter("Element_City")?.AsString();
            elementCondition.Country = element.LookupParameter("Element_Country")?.AsString();

            // Validate latitude and longitude
            if (!IsValidLatitude(elementCondition.Latitude) || !IsValidLongitude(elementCondition.Longitude))
            {
                elementCondition.Message += "Invalid latitude or longitude values.\n";
                return;
            }

            // Retrieve location data based on the ground condition
            if (elementCondition.GroundCondition == "In-Ground")
            {
                string resourcePath = "RevitWoodLCC.assets.dosesIGCityCountry.json";
                var inGroundLocationData = GetInGroundLocationData(resourcePath, elementCondition.Latitude, elementCondition.Longitude);

                if (inGroundLocationData != null)
                {
                    elementCondition.InGroundLocationData = inGroundLocationData;
                }
                else
                {
                    elementCondition.Message += $"Failed to retrieve in-ground location data from {resourcePath}.\n";
                }
            }
            else if (elementCondition.GroundCondition == "Above-Ground")
            {
                string resourcePath = "RevitWoodLCC.assets.DoseInputsCity.json";
                var aboveGroundLocationData = GetAboveGroundLocationData(resourcePath, elementCondition.Latitude, elementCondition.Longitude);

                if (aboveGroundLocationData != null)
                {
                    elementCondition.AboveGroundLocationData = aboveGroundLocationData;
                }
                else
                {
                    elementCondition.Message += $"Failed to retrieve above-ground location data from {resourcePath}.\n";
                }
            }
            else
            {
                elementCondition.Message += "Unknown ground condition. Location data cannot be processed.\n";
            }
        }




        private void ProcessGroundCondition(Element element, ElementCondition elementCondition)
        {
            bool isInGround = SLE_AutoPopulateUtility.IsElementInGround(element, _doc);
            elementCondition.GroundCondition = isInGround ? "In-Ground" : "Above-Ground";
        }

        private void ProcessShelteringAndExposure(Element element, ElementCondition elementCondition)
        {
            string shelterMessage = "";
            List<VerticalRayInfo> verticalRayInfos = new();
            List<ThirtyDegreeRayInfo> angledRayInfos = new();

            bool isSheltered = SLE_AutoPopulateUtility.CheckIfElementIsSheltered(element, _doc, ref shelterMessage, verticalRayInfos, angledRayInfos);
            elementCondition.IsSheltered = isSheltered;
            elementCondition.Message += shelterMessage;

            elementCondition.GroundDistance = SLE_AutoPopulateUtility.CalculateGroundDistance(element, _doc);
            elementCondition.OverhangDistance = SLE_AutoPopulateUtility.CalculateOverhangLength(element, _doc, verticalRayInfos);
            elementCondition.ShelterDistance = SLE_AutoPopulateUtility.CalculateShelterDistance(element, _doc, verticalRayInfos);

            string exposureMessage = "";
            List<FaceExposureDetail> faceDetails = SLE_AutoPopulateUtility.GetExposureCondition(element, _doc, ref exposureMessage);
            SLE_AutoPopulateUtility.CheckExposureByRayCasting(faceDetails, _doc, SLE_AutoPopulateUtility.GetActive3DView(_doc), ref exposureMessage);

            elementCondition.ExposureCondition = faceDetails.Any(f => f.GrainType == "End Grain" && f.IsExposed)
                ? "End grain exposed"
                : "Side grain exposed";

            elementCondition.Message += exposureMessage;
        }

        private void ProcessIntersectionCondition(Element element, ElementCondition elementCondition)
        {
            string intersectionMessage = "";
            List<FaceExposureDetail> intersectionFaceDetails = SLE_AutoPopulateUtility.GetExposureCondition(element, _doc, ref intersectionMessage);

            foreach (var faceDetail in intersectionFaceDetails)
            {
                SLE_AutoPopulateUtility.PerformElementIntersectionRayCasting(faceDetail, _doc, SLE_AutoPopulateUtility.GetActive3DView(_doc), ref intersectionMessage);
            }

            var intersectionConditionField = new System.Windows.Controls.ComboBox();
            string intersectionCondition = SLE_AutoPopulateUtility.DetermineIntersectionCondition(intersectionFaceDetails, intersectionConditionField);
            elementCondition.IntersectionCondition = intersectionCondition;

            elementCondition.Message += intersectionMessage;
        }

        public static InGroundLocationDataSet GetInGroundLocationData(string resourcePath, double latitude, double longitude)
        {
            try
            {
                var locations = GetCachedInGroundLocations(resourcePath);
                if (locations == null || !locations.Any())
                {
                    TaskDialog.Show("Error", $"No data found in {resourcePath}.");
                    return null;
                }

                return locations.FirstOrDefault(loc =>
                    Math.Abs(loc.UnifiedLatitude.GetValueOrDefault() - latitude) < 0.01 &&
                    Math.Abs(loc.UnifiedLongitude.GetValueOrDefault() - longitude) < 0.01);
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Error", $"Failed to retrieve in-ground location data: {ex.Message}");
                return null;
            }
        }

        public static AboveGroundLocationDataSet GetAboveGroundLocationData(string resourcePath, double latitude, double longitude)
        {
            try
            {
                var locations = GetCachedAboveGroundLocations(resourcePath);
                if (locations == null || !locations.Any())
                {
                    TaskDialog.Show("Error", $"No data found in {resourcePath}.");
                    return null;
                }

                return locations.FirstOrDefault(loc =>
                    Math.Abs(loc.UnifiedLatitude.GetValueOrDefault() - latitude) < 0.01 &&
                    Math.Abs(loc.UnifiedLongitude.GetValueOrDefault() - longitude) < 0.01);
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Error", $"Failed to retrieve above-ground location data: {ex.Message}");
                return null;
            }
        }



        public static bool IsValidLatitude(double latitude) => latitude >= -90 && latitude <= 90;
        public static bool IsValidLongitude(double longitude) => longitude >= -180 && longitude <= 180;

        private static List<InGroundLocationDataSet> _cachedInGroundLocations;
        private static List<AboveGroundLocationDataSet> _cachedAboveGroundLocations;

        // Method to get cached in-ground locations
        public static List<InGroundLocationDataSet> GetCachedInGroundLocations(string resourcePath)
        {
            if (_cachedInGroundLocations == null)
            {
                var assembly = Assembly.GetExecutingAssembly();
                using (Stream stream = assembly.GetManifestResourceStream(resourcePath))
                using (StreamReader reader = new StreamReader(stream))
                {
                    string json = reader.ReadToEnd();
                    _cachedInGroundLocations = JsonConvert.DeserializeObject<List<InGroundLocationDataSet>>(json);
                }
            }
            return _cachedInGroundLocations;
        }

        // Method to get cached above-ground locations
        public static List<AboveGroundLocationDataSet> GetCachedAboveGroundLocations(string resourcePath)
        {
            if (_cachedAboveGroundLocations == null)
            {
                var assembly = Assembly.GetExecutingAssembly();
                using (Stream stream = assembly.GetManifestResourceStream(resourcePath))
                using (StreamReader reader = new StreamReader(stream))
                {
                    string json = reader.ReadToEnd();
                    _cachedAboveGroundLocations = JsonConvert.DeserializeObject<List<AboveGroundLocationDataSet>>(json);
                }
            }
            return _cachedAboveGroundLocations;
        }


        private SLEUtility.LocationDetails ConvertToLocationDetails(InGroundLocationDataSet inGroundLocationDataSet)
        {
            if (inGroundLocationDataSet == null) return null;

            return new SLEUtility.LocationDetails
            {
                City = inGroundLocationDataSet.City,
                Country = inGroundLocationDataSet.Country,
                Latitude = inGroundLocationDataSet.UnifiedLatitude,
                Longitude = inGroundLocationDataSet.UnifiedLongitude,
                Doses = inGroundLocationDataSet.Doses
            };
        }

        private SLEUtility.LocationDetails ConvertToLocationDetails(AboveGroundLocationDataSet aboveGroundLocationDataSet)
        {
            if (aboveGroundLocationDataSet == null) return null;

            return new SLEUtility.LocationDetails
            {
                City = aboveGroundLocationDataSet.City,
                Country = aboveGroundLocationDataSet.Country,
                Latitude = aboveGroundLocationDataSet.UnifiedLatitude,
                Longitude = aboveGroundLocationDataSet.UnifiedLongitude,
                DRef = aboveGroundLocationDataSet.DRef?.ToString(CultureInfo.InvariantCulture),
                DShelt = aboveGroundLocationDataSet.DShelt?.ToString(CultureInfo.InvariantCulture),
                KTrap1 = aboveGroundLocationDataSet.KTrap1?.ToString(CultureInfo.InvariantCulture),
                KTrap2 = aboveGroundLocationDataSet.KTrap2?.ToString(CultureInfo.InvariantCulture),
                KTrap3 = aboveGroundLocationDataSet.KTrap3?.ToString(CultureInfo.InvariantCulture),
                KTrap4 = aboveGroundLocationDataSet.KTrap4?.ToString(CultureInfo.InvariantCulture),
                KTrap5 = aboveGroundLocationDataSet.KTrap5?.ToString(CultureInfo.InvariantCulture),
                WDRRatio = aboveGroundLocationDataSet.WDRRatio?.ToString(CultureInfo.InvariantCulture),
                WDRRatioH = aboveGroundLocationDataSet.WDRRatioH?.ToString(CultureInfo.InvariantCulture)
            };
        }



        public class ElementCondition
        {
            public int ElementId { get; set; }
            public string Name { get; set; }
            public string Category { get; set; }
            public string GroundCondition { get; set; }
            public bool IsSheltered { get; set; }
            public bool IsVertical { get; set; }
            public double GroundDistance { get; set; }
            public double OverhangDistance { get; set; }
            public double ShelterDistance { get; set; }
            public string ExposureCondition { get; set; }
            public string IntersectionCondition { get; set; }
            public string Message { get; set; }

            // Specific messages for display
            public string MaterialAndTreatmentMessage { get; set; }
            public string ShelteringMessage { get; set; }
            public string IntersectionMessage { get; set; }

            // Shared Parameters
            public string Material { get; set; }
            public string Treatment { get; set; }
            public double Latitude { get; set; }
            public double Longitude { get; set; }
            public string City { get; set; }
            public string Country { get; set; }

            // New structured fields for location data
            public InGroundLocationDataSet InGroundLocationData { get; set; } // For In-Ground conditions
            public AboveGroundLocationDataSet AboveGroundLocationData { get; set; } // For Above-Ground conditions

            public MaterialData MatchedMaterialData { get; set; } // For storing matched material and treatment

            // Resistance Dose and Messages
            public double? InGroundResistanceDose { get; set; }
            public double? AboveGroundResistanceDose { get; set; }
            public string InGroundResistanceDoseMessage { get; set; }
            public string AboveGroundResistanceDoseMessage { get; set; }

            // Separate service life messages
            public string InGroundServiceLifeMessage { get; set; }
            public string AboveGroundServiceLifeMessage { get; set; }

            // Separate service life durations
            public double? InGroundServiceLifeDuration { get; set; } // In-Ground service life duration
            public double? AboveGroundServiceLifeDuration { get; set; } // Above-Ground service life duration

            public double? AboveGroundK2 { get; set; } // Exposure Adjustment Factor
            public double? AboveGroundK3 { get; set; } // Shelter Adjustment Factor
            public double? AboveGroundDRef { get; set; } // Reference Dose



        }


        // In-Ground Location Data
        public class InGroundLocationDataSet
        {
            [JsonProperty("city")]
            public string City { get; set; }

            [JsonProperty("country")]
            public string Country { get; set; }

            // Handles "lats" (for In-Ground JSON)
            [JsonProperty("lats")]
            public double? Lats { get; set; }

            // Handles "lons" (for In-Ground JSON)
            [JsonProperty("lons")]
            public double? Lons { get; set; }

            [JsonProperty("doses")]
            public double? Doses { get; set; }

            // Unified Latitude property to handle "lats"
            public double? UnifiedLatitude => Lats;

            // Unified Longitude property to handle "lons"
            public double? UnifiedLongitude => Lons;
        }

        // Above-Ground Location Data
        public class AboveGroundLocationDataSet
        {
            [JsonProperty("city")]
            public string City { get; set; }

            [JsonProperty("country")]
            public string Country { get; set; }

            // Handles "lat" (for Above-Ground JSON)
            [JsonProperty("lat")]
            public double? Lat { get; set; }

            // Handles "lon" (for Above-Ground JSON)
            [JsonProperty("lon")]
            public double? Lon { get; set; }

            [JsonProperty("D_ref")]
            public double? DRef { get; set; }

            [JsonProperty("D_shelt")]
            public double? DShelt { get; set; }

            [JsonProperty("k_trap1")]
            public double? KTrap1 { get; set; }

            [JsonProperty("k_trap2")]
            public double? KTrap2 { get; set; }

            [JsonProperty("k_trap3")]
            public double? KTrap3 { get; set; }

            [JsonProperty("k_trap4")]
            public double? KTrap4 { get; set; }

            [JsonProperty("k_trap5")]
            public double? KTrap5 { get; set; }

            [JsonProperty("WDR_ratio")]
            public double? WDRRatio { get; set; }

            [JsonProperty("WDR_ratio_h")]
            public double? WDRRatioH { get; set; }

            // Unified Latitude property to handle "lat"
            public double? UnifiedLatitude => Lat;

            // Unified Longitude property to handle "lon"
            public double? UnifiedLongitude => Lon;
        }



    }
}

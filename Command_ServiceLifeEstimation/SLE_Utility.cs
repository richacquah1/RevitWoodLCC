
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace RevitWoodLCC
{
    public static class SLEUtility
    {
        public static void OpenSetLocationWindow(System.Windows.Controls.ComboBox locationField, ExternalCommandData commandData, string condition)
        {
            //SetProjectLocationWindow setLocationWindow = new SetProjectLocationWindow();
            //SetProjectLocationWindow setLocationWindow = new SetProjectLocationWindow("Above Ground");
            SetProjectLocationWindow setLocationWindow = new SetProjectLocationWindow(commandData, condition);

            var result = setLocationWindow.ShowDialog();
            if (result == true)
            {
                LoadLocation(locationField, commandData, condition);
            }
        }

        public static void SetLocationField(Location location, System.Windows.Controls.ComboBox locationField)
        {
            // Clear existing items in the ComboBox to ensure only one location
            locationField.Items.Clear();

            // Wrap the location data in a LocationWrapper object
            var locationWrapper = new LocationWrapper
            {
                City = location.City,
                Country = location.Country,
                Lat = location.Lat,
                Lon = location.Lon,
                KTrap1 = location.KTrap1,
                KTrap2 = location.KTrap2,
                KTrap3 = location.KTrap3,
                KTrap4 = location.KTrap4,
                KTrap5 = location.KTrap5,
                DRef = location.DRef,
                DShelt = location.DShelt,
                WDRRatio = location.WDRRatio,
                WDRRatioH = location.WDRRatioH
            };

            // Add the LocationWrapper to the ComboBox
            locationField.Items.Add(locationWrapper);
            locationField.SelectedItem = locationWrapper; // Set the added location as the selected item
        }


        public static void LoadLocation(System.Windows.Controls.ComboBox locationField, ExternalCommandData commandData, string condition)
        {
            string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RevitProjectLocation.json");

            if (File.Exists(filePath))
            {
                try
                {
                    string json = File.ReadAllText(filePath);

                    if (string.IsNullOrWhiteSpace(json))
                    {
                        throw new InvalidOperationException("The location file is empty or contains invalid data.");
                    }

                    Location location = JsonConvert.DeserializeObject<Location>(json);

                    if (location == null)
                    {
                        throw new InvalidOperationException("The deserialized location is null. Please check the JSON file format.");
                    }

                    //Debugging: Check all fields after deserialization
                    //MessageBox.Show($"Deserialized Location: City = {location.City}, Country = {location.Country}, " +
                    //                $"Lat = {location.Lat}, Lon = {location.Lon}, " +
                    //                $"KTrap1 = {location.KTrap1}, KTrap2 = {location.KTrap2}, KTrap3 = {location.KTrap3}, " +
                    //                $"KTrap4 = {location.KTrap4}, KTrap5 = {location.KTrap5}, DRef = {location.DRef}, DShelt = {location.DShelt}, " +
                    //                $"WDR_ratio = {location.WDRRatio}, WDR_ratio_h = {location.WDRRatioH}");


                    locationField.Dispatcher.Invoke(() =>
                    {
                        SetLocationField(location, locationField);
                        DisplayConfirmationDialog(location);
                    });

                    // Save the location after loading
                    SaveLocation(location);
                }
                catch (JsonSerializationException ex)
                {
                    MessageBox.Show($"Failed to deserialize location: {ex.Message}\n{ex.StackTrace}", "Deserialization Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to load location: {ex.Message}\n{ex.StackTrace}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("The location file does not exist.", "File Not Found", MessageBoxButton.OK, MessageBoxImage.Warning);
                OpenSetLocationWindow(locationField, commandData, condition);
            }
        }


        public static bool TrySetProjectLocation(Location location, ExternalCommandData commandData, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (!IsValidLatitude(location.Lat) || !IsValidLongitude(location.Lon))
            {
                errorMessage = "Latitude or Longitude is out of range.";
                return false;
            }

            using (Transaction transaction = new Transaction(commandData.Application.ActiveUIDocument.Document, "Set Project Location"))
            {
                try
                {
                    transaction.Start();

                    SiteLocation siteLocation = commandData.Application.ActiveUIDocument.Document.SiteLocation;
                    siteLocation.Latitude = location.Lat * (Math.PI / 180);
                    siteLocation.Longitude = location.Lon * (Math.PI / 180);
                    siteLocation.Name = location.City;

                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    errorMessage = "Failed to set project location: " + ex.Message;
                    transaction.RollBack();
                    return false;
                }
            }

            return true;
        }

        public static bool IsValidLatitude(double latitude) => latitude >= -90 && latitude <= 90;

        public static bool IsValidLongitude(double longitude) => longitude >= -180 && longitude <= 180;

        public static void SaveLocation(Location location)
        {
            string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RevitProjectLocation.json");

            // Ensure all necessary fields are populated before saving
            if (location.DRef != null && location.DShelt != null && location.KTrap1 != null)
            {
                string json = JsonConvert.SerializeObject(location);
                File.WriteAllText(filePath, json);
            }
            else
            {
                // MessageBox.Show("Location data is incomplete. Ensure all fields are filled before saving.", "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        public static LocationDetails DisplayConfirmationDialog(Location location)
        {
            var infoBuilder = new StringBuilder();
            infoBuilder.AppendLine("Location Set Successfully:");

            // Create a LocationDetails object and fill it with formatted strings
            var locationDetails = new LocationDetails
            {
                City = location.City,
                Country = location.Country,
                Latitude = location.Lats.HasValue ? location.Lats.Value : location.Lat,
                Longitude = location.Lons.HasValue ? location.Lons.Value : location.Lon,
                Doses = location.Doses,
                DRef = location.DRef?.ToString("F1", CultureInfo.InvariantCulture),
                DShelt = location.DShelt?.ToString("F1", CultureInfo.InvariantCulture),
                KTrap1 = location.KTrap1?.ToString("F2", CultureInfo.InvariantCulture),
                KTrap2 = location.KTrap2?.ToString("F1", CultureInfo.InvariantCulture),
                KTrap3 = location.KTrap3?.ToString("F2", CultureInfo.InvariantCulture),
                KTrap4 = location.KTrap4?.ToString("F2", CultureInfo.InvariantCulture),
                KTrap5 = location.KTrap5?.ToString("F2", CultureInfo.InvariantCulture),
                WDRRatio = location.WDRRatio?.ToString("F2", CultureInfo.InvariantCulture),
                WDRRatioH = location.WDRRatioH?.ToString("F2", CultureInfo.InvariantCulture)
            };

            // Append the properties in the desired order, using the formatted strings
            infoBuilder.AppendLine($"d_ref: {locationDetails.DRef ?? "null"}");
            infoBuilder.AppendLine($"d_shelt: {locationDetails.DShelt ?? "null"}");
            infoBuilder.AppendLine($"k_trap1: {locationDetails.KTrap1 ?? "null"}");
            infoBuilder.AppendLine($"k_trap2: {locationDetails.KTrap2 ?? "null"}");
            infoBuilder.AppendLine($"k_trap3: {locationDetails.KTrap3 ?? "null"}");
            infoBuilder.AppendLine($"k_trap4: {locationDetails.KTrap4 ?? "null"}");
            infoBuilder.AppendLine($"k_trap5: {locationDetails.KTrap5 ?? "null"}");
            infoBuilder.AppendLine($"WDR_ratio: {locationDetails.WDRRatio ?? "null"}");
            infoBuilder.AppendLine($"WDR_ratio_h: {locationDetails.WDRRatioH ?? "null"}");
            infoBuilder.AppendLine($"lat: {locationDetails.Latitude?.ToString("F1", CultureInfo.InvariantCulture) ?? "null"}");
            infoBuilder.AppendLine($"lon: {locationDetails.Longitude?.ToString("F1", CultureInfo.InvariantCulture) ?? "null"}");

            // Append city and country at the end
            infoBuilder.AppendLine($"city: {location.City}");
            infoBuilder.AppendLine($"country: {location.Country}");

            // Conditional check for Lats, Lons, and Doses values
            if (location.Lats.HasValue && location.Lons.HasValue && location.Doses.HasValue)
            {
                infoBuilder.AppendLine($"lat: {location.Lats.Value.ToString("F1", CultureInfo.InvariantCulture)}");
                infoBuilder.AppendLine($"lon: {location.Lons.Value.ToString("F1", CultureInfo.InvariantCulture)}");
                infoBuilder.AppendLine($"doses: {location.Doses.Value.ToString("F1", CultureInfo.InvariantCulture)}");
            }
            else
            {
                infoBuilder.AppendLine($"lat: {location.Lat.ToString("F1", CultureInfo.InvariantCulture)}");
                infoBuilder.AppendLine($"lon: {location.Lon.ToString("F1", CultureInfo.InvariantCulture)}");
            }

            // Display the dialog with the built information
            //MessageBox.Show(infoBuilder.ToString(), "Confirmation");

            return locationDetails;
        }

        public static void LoadLocationBasedOnSelection(System.Windows.Controls.ComboBox locationField, string selection)
        {
            // Determine the file path based on the selection
            string filePath = selection == "In-Ground"
                ? "RevitWoodLCC.assets.dosesIGCityCountry.json"
                : "RevitWoodLCC.assets.DoseInputsCity.json";

            // Load and update the location combobox
            LoadLocationsFromFile(locationField, filePath);
        }

        private static void LoadLocationsFromFile(System.Windows.Controls.ComboBox locationField, string resourcePath)
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                var resourceName = resourcePath;

                using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                using (StreamReader reader = new StreamReader(stream))
                {
                    string json = reader.ReadToEnd();
                    var locations = JsonConvert.DeserializeObject<List<Location>>(json);
                    UpdateLocationDropdown(locationField, locations);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load locations: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static void UpdateLocationDropdown(System.Windows.Controls.ComboBox locationField, List<Location> locations)
        {
            locationField.Dispatcher.Invoke(() =>
            {
                locationField.Items.Clear(); // Clear existing items first
                locationField.ItemsSource = locations;
                locationField.DisplayMemberPath = "FullLocation"; // Assuming FullLocation is a property that combines City and Country
            });
        }

        //public static double CalculateServiceLifeDuration(SLEUtility.LocationDetails locationDetails, MaterialData materialData, System.Windows.Controls.ComboBox soilContactField, System.Windows.Controls.ComboBox exposureField, System.Windows.Controls.ComboBox elementIntersectionField, CheckBox verticalMemberCheckbox, CheckBox roofOverhangCheckbox, System.Windows.Controls.TextBox groundDistTextBox, System.Windows.Controls.TextBox overhangTextBox, System.Windows.Controls.TextBox shelterDistTextBox)
        //{
        //    try
        //    {
        //        //MessageBox.Show("CalculateServiceLifeDuration: Entered method.");																			

        //        bool isInGround = ((ComboBoxItem)soilContactField.SelectedItem)?.Content.ToString() == "In-Ground";
        //        //MessageBox.Show($"CalculateServiceLifeDuration: Is In-Ground: {isInGround}");																					

        //        if (isInGround)
        //        {
        //            // JavaScript logic for "In-Ground" calculation
        //            double doses = locationDetails.Doses.Value;
        //            double D_res = materialData.ResistanceDoseUC4;
        //            double DR_rel_raw = (double)(D_res / 325.0 * 10) / 10;
        //            double DR_rel = Math.Round(DR_rel_raw, 0);
        //            double daily_dose_reference = 21.2; // Dose value for the Uppsala entry
        //            double DE0_rel_raw = doses / daily_dose_reference;
        //            double DE0_rel = Math.Round(DE0_rel_raw, 0);
        //            double DE_rel = DE0_rel; // As per the JavaScript logic, DE_rel seems to be equivalent to DE0_rel in this context
        //            double serviceLifeInYears_raw = (DR_rel_raw * 86.4) / doses;
        //            double serviceLifeInYears = Math.Round(serviceLifeInYears_raw, 2);

        //            return serviceLifeInYears;
        //        }

        //        else
        //        {
        //            double k2 = RetrieveAndStoreSelections(exposureField, elementIntersectionField);
        //            //MessageBox.Show($"CalculateServiceLifeDuration: k2 factor: {k2}");																			 

        //            double k3 = CalculateShelterAdjustmentFactor(locationDetails, k2, verticalMemberCheckbox, roofOverhangCheckbox, groundDistTextBox, overhangTextBox, shelterDistTextBox);
        //            //MessageBox.Show($"CalculateServiceLifeDuration: k3 factor: {k3}");																		 

        //            double D_ref_current = double.Parse(locationDetails.DRef, CultureInfo.InvariantCulture);
        //            double referenceDRef = 32.3;
        //            double D_res = materialData.ResistanceDoseUC3;
        //            double DR_rel_raw = (double)(materialData.ResistanceDoseUC3 / 325.0 * 10) / 10;
        //            double DR_rel = Math.Round(DR_rel_raw, 0);
        //            const double conversionFactor = 325;
        //            double serviceLifeInDays_raw = (D_res * conversionFactor) / (D_ref_current * k2 * k3);
        //            double serviceLifeInYears_raw = serviceLifeInDays_raw / 325;//365.25
        //            double serviceLifeInYears = Math.Round(serviceLifeInYears_raw, 2);


        //            return serviceLifeInYears;
        //        }
        //    }
        //    catch (Exception ex)
        //    {

        //        throw new InvalidOperationException($"An error occurred: {ex.Message}");
        //    }
        //}


        //public static double RetrieveAndStoreSelections(System.Windows.Controls.ComboBox exposureField, System.Windows.Controls.ComboBox elementIntersectionField)
        //{
        //    string absorp = (exposureField.SelectedItem as ComboBoxItem)?.Content.ToString();
        //    string contact = (elementIntersectionField.SelectedItem as ComboBoxItem)?.Content.ToString();

        //    // Debugging: Log the selected values
        //    //MessageBox.Show($"RetrieveAndStoreSelections: Selected Exposure: {absorp}, Selected Contact: {contact}");

        //    if (string.IsNullOrEmpty(absorp) || string.IsNullOrEmpty(contact))
        //    {
        //        //MessageBox.Show("Error: Please ensure both exposure and element intersection fields are selected.");
        //        return double.NaN;
        //    }

        //    double k2 = 0.0;

        //    // Validate location details
        //    if (_currentSelectedLocation != null)
        //    {
        //        var locationDetails = SLEUtility.DisplayConfirmationDialog(_currentSelectedLocation);

        //        // Debugging: Log location details
        //        // MessageBox.Show($"Location Details: KTrap1: {locationDetails.KTrap1}, KTrap2: {locationDetails.KTrap2}, " + $"KTrap3: {locationDetails.KTrap3}, KTrap4: {locationDetails.KTrap4}, KTrap5: {locationDetails.KTrap5}");

        //        if (locationDetails != null && !string.IsNullOrEmpty(locationDetails.KTrap1) && !string.IsNullOrEmpty(locationDetails.KTrap5))
        //        {
        //            double kTrap1 = double.Parse(locationDetails.KTrap1, CultureInfo.InvariantCulture);
        //            double kTrap2 = double.Parse(locationDetails.KTrap2, CultureInfo.InvariantCulture);
        //            double kTrap3 = double.Parse(locationDetails.KTrap3, CultureInfo.InvariantCulture);
        //            double kTrap4 = double.Parse(locationDetails.KTrap4, CultureInfo.InvariantCulture);
        //            double kTrap5 = double.Parse(locationDetails.KTrap5, CultureInfo.InvariantCulture);

        //            // Debugging: Log kTrap values
        //            //MessageBox.Show($"kTrap Values: kTrap1: {kTrap1}, kTrap2: {kTrap2}, kTrap3: {kTrap3}, kTrap4: {kTrap4}, kTrap5: {kTrap5}");

        //            if (absorp == "Side grain exposed")
        //            {
        //                switch (contact)
        //                {
        //                    case "No contact face or gap size >5 mm free from dirt":
        //                        k2 = 1.0;
        //                        break;
        //                    case "Partially ventilated contact face free from dirt":
        //                        k2 = kTrap1;
        //                        break;
        //                    case "Direct contact or insufficient ventilation":
        //                        k2 = kTrap2;
        //                        break;
        //                }
        //            }
        //            else if (absorp == "End grain exposed")
        //            {
        //                switch (contact)
        //                {
        //                    case "No contact face or gap size >5 mm free from dirt":
        //                        k2 = kTrap5;
        //                        break;
        //                    case "Partially ventilated contact face free from dirt":
        //                        k2 = kTrap3;
        //                        break;
        //                    case "Direct contact or insufficient ventilation":
        //                        k2 = kTrap4;
        //                        break;
        //                }
        //            }
        //        }
        //    }

        //    // Debugging: Log the calculated k2 value
        //    //MessageBox.Show($"Calculated k2: {k2}");

        //    if (k2 == 0.0)
        //    {
        //        MessageBox.Show("Error: k2 factor calculation failed. Please review your selections.");
        //        return double.NaN;
        //    }

        //    return k2;
        //}


        //public static double CalculateShelterAdjustmentFactor(SLEUtility.LocationDetails locationDetails, double k2, CheckBox verticalMemberCheckbox, CheckBox roofOverhangCheckbox, System.Windows.Controls.TextBox groundDistTextBox, System.Windows.Controls.TextBox overhangTextBox, System.Windows.Controls.TextBox shelterDistTextBox)
        //{
        //    if (k2 == double.NaN)
        //    {
        //        MessageBox.Show("Error: Invalid k2 factor.");
        //        return double.NaN;
        //    }

        //    bool isVertical = verticalMemberCheckbox.IsChecked ?? false;
        //    bool hasOverhang = roofOverhangCheckbox.IsChecked ?? false;

        //    // Debugging: Log the values of vertical and overhang checks
        //    //MessageBox.Show($"CalculateShelterAdjustmentFactor: Is vertical member: {isVertical}, Has overhang: {hasOverhang}");

        //    double.TryParse(groundDistTextBox.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double a);
        //    double.TryParse(overhangTextBox.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double e);
        //    double.TryParse(shelterDistTextBox.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double d);

        //    // Debugging: Log parsed distances
        //    //MessageBox.Show($"Parsed Distances: a = {a}, e = {e}, d = {d}");

        //    if (d == 0)
        //    {
        //        MessageBox.Show("Shelter distance (d) cannot be zero.", "Input Error", MessageBoxButton.OK, MessageBoxImage.Warning);
        //        return double.NaN;
        //    }

        //    double D_ref = double.Parse(locationDetails.DRef ?? "0", CultureInfo.InvariantCulture);
        //    double D_shelt = double.Parse(locationDetails.DShelt ?? "0", CultureInfo.InvariantCulture);
        //    double WDR_ratio = double.Parse(locationDetails.WDRRatio ?? "0", CultureInfo.InvariantCulture);
        //    double WDR_ratio_h = double.Parse(locationDetails.WDRRatioH ?? "0", CultureInfo.InvariantCulture);

        //    // Debugging: Log location-specific values
        //    // MessageBox.Show($"Location Details: D_ref = {D_ref}, D_shelt = {D_shelt}, WDR_ratio = {WDR_ratio}, WDR_ratio_h = {WDR_ratio_h}");

        //    double exposedDose = D_ref * k2;
        //    double shelteredDose = D_shelt;
        //    double rainDeltaDose = exposedDose - shelteredDose;
        //    double reducedDose;

        //    if (isVertical && hasOverhang)
        //    {
        //        double k_shelter = Math.Max(1 - e / d, 0);
        //        reducedDose = shelteredDose + (WDR_ratio * k_shelter) * rainDeltaDose;
        //    }
        //    else if (isVertical)
        //    {
        //        reducedDose = shelteredDose + WDR_ratio * rainDeltaDose;
        //    }
        //    else if (hasOverhang)
        //    {
        //        double k_shelter = Math.Max(1 - e / d, 0);
        //        reducedDose = shelteredDose + (WDR_ratio_h * rainDeltaDose * k_shelter);
        //    }
        //    else
        //    {
        //        reducedDose = exposedDose;
        //    }

        //    double k3 = reducedDose / exposedDose;

        //    // Debugging: Log the calculated k3 value
        //    //MessageBox.Show($"Calculated k3: {k3}");

        //    return k3;
        //}


        public class LocationDetails
        {
            public string City { get; set; }
            public string Country { get; set; }
            public double? Latitude { get; set; }
            public double? Longitude { get; set; }
            public double? Doses { get; set; }
            public string DRef { get; set; }
            public string DShelt { get; set; }
            public string KTrap1 { get; set; }
            public string KTrap2 { get; set; }
            public string KTrap3 { get; set; }
            public string KTrap4 { get; set; }
            public string KTrap5 { get; set; }
            public string WDRRatio { get; set; }
            public string WDRRatioH { get; set; }

        }

        public class LocationWrapper
        {
            public string City { get; set; }
            public string Country { get; set; }
            public double? Lat { get; set; }
            public double? Lon { get; set; }
            public double? KTrap1 { get; set; }
            public double? KTrap2 { get; set; }
            public double? KTrap3 { get; set; }
            public double? KTrap4 { get; set; }
            public double? KTrap5 { get; set; }
            public double? DRef { get; set; }
            public double? DShelt { get; set; }
            public double? WDRRatio { get; set; }
            public double? WDRRatioH { get; set; }

            public override string ToString()
            {
                // Only display city and country in the ComboBox
                return $"{City}, {Country}";
            }

            public string GetFullDetails()
            {
                // Return all details as a string for display elsewhere
                return $"City: {City}\n" +
                       $"Country: {Country}\n" +
                       $"Latitude: {Lat}\n" +
                       $"Longitude: {Lon}\n" +
                       $"KTrap1: {KTrap1}\n" +
                       $"KTrap2: {KTrap2}\n" +
                       $"KTrap3: {KTrap3}\n" +
                       $"KTrap4: {KTrap4}\n" +
                       $"KTrap5: {KTrap5}\n" +
                       $"DRef: {DRef}\n" +
                       $"DShelt: {DShelt}\n" +
                       $"WDR Ratio: {WDRRatio}\n" +
                       $"WDR Ratio H: {WDRRatioH}";
            }
        }

    }
}

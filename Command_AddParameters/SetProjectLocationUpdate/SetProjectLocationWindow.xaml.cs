using Newtonsoft.Json;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace RevitWoodLCC
{
    public partial class SetProjectLocationWindow : Window
    {
        private List<Location> _locations;
        public Location SelectedLocation { get; private set; }
        public string CurrentCondition { get; set; }
        private readonly ExternalCommandData _commandData;
        private string DebuggingMessage; // Declare DebuggingMessage as a class member
        private SLE_PopupForm_Logic _logic;

        public SetProjectLocationWindow(ExternalCommandData commandData, string condition)
        {
            _commandData = commandData;
            Title = "Set Revit Project Location";
            Width = 800; // Adjust the width to accommodate the map
            Height = 500; // Adjust the height if needed
            CurrentCondition = condition; // Set condition dynamically
            InitializeComponent(); // InitializeComponent must be called to load the XAML components
            string resourceName = CurrentCondition == "In-Ground"
                ? "RevitWoodLCC.assets.dosesIGCityCountry.json"
                : "RevitWoodLCC.assets.DoseInputsCity.json";

             LoadLocationsAsync(resourceName); // Load based on the current condition

            //Task.Run(async () => await LoadLocationsAsync(resourceName)).Wait();


            // Set debugging message based on condition
            //DebuggingMessage = CurrentCondition == "In-Ground" ? "Using In-Ground predefined locations." : "Using Above Ground predefined locations.";


            // Load the map HTML file
            LoadMapHtml();
        }


        private void LoadMapHtml()
        {
            string tempFilePath = Path.Combine(Path.GetTempPath(), "InteractiveMap.html");
            string resourceName = "RevitWoodLCC.Command_AddParameters.SetProjectLocationUpdate.InteractiveMapWindow.InteractiveMap.html";

            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    MessageBox.Show($"Cannot find embedded resource '{resourceName}'. Ensure the resource name is correct.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                using (FileStream fileStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write))
                {
                    stream.CopyTo(fileStream);
                }
            }

            if (File.Exists(tempFilePath))
            {
                try
                {
                    MapWebBrowser.ObjectForScripting = new ScriptManager(SetCoordinates);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error setting ObjectForScripting: {ex.Message}");
                }

                MapWebBrowser.Navigate(new Uri(tempFilePath));
            }
            else
            {
                MessageBox.Show($"Cannot find file '{tempFilePath}'. Make sure the path or Internet address is correct.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MapWebBrowser_Navigating(object sender, NavigatingCancelEventArgs e)
        {
            dynamic activeX = MapWebBrowser.GetType().InvokeMember("ActiveXInstance",
                BindingFlags.GetProperty | BindingFlags.Instance | BindingFlags.NonPublic,
                null, MapWebBrowser, new object[] { });

            activeX.Silent = true; // Suppress script errors
        }

        private void MapWebBrowser_LoadCompleted(object sender, NavigationEventArgs e)
        {
            SuppressScriptErrors(MapWebBrowser);
        }

        private void SuppressScriptErrors(WebBrowser webBrowser)
        {
            var axIWebBrowser2 = GetActiveXObject(webBrowser);
            if (axIWebBrowser2 == null) return;

            axIWebBrowser2.Silent = true; // Suppress script errors
        }

        private dynamic GetActiveXObject(WebBrowser webBrowser)
        {
            var fieldInfo = typeof(WebBrowser).GetField("_axIWebBrowser2", BindingFlags.Instance | BindingFlags.NonPublic);
            if (fieldInfo == null) return null;

            return fieldInfo.GetValue(webBrowser);
        }

        public void SetCoordinates(double latitude, double longitude)
        {
            //MessageBox.Show($"SetCoordinates called with: Latitude = {latitude}, Longitude = {longitude}");
            FindClosestCoordinates(latitude, longitude);

            if (SelectedLocation != null)
            {
                // Update the dropdown to show the selected location
                LocationDropdown.SelectedItem = SelectedLocation;
                // Trigger the SelectionChanged event
                LocationDropdown_SelectionChanged(LocationDropdown, null);

                string errorMessage;
                if (SLEUtility.TrySetProjectLocation(SelectedLocation, _commandData, out errorMessage))
                {
                    SLEUtility.SaveLocation(SelectedLocation);

                }
                else
                {
                    MessageBox.Show($"Failed to set location(SetCoordinates): {errorMessage}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void FindClosestCoordinates(double latitude, double longitude)
        {
            // Debugging message to indicate which list of predefined locations is being used
            // MessageBox.Show(DebuggingMessage, "Debugging Information", MessageBoxButton.OK, MessageBoxImage.Information);

            //if (_locations == null || !_locations.Any())
            if (_locations == null || _locations.Count == 0)
            {
                MessageBox.Show("Locations data is not loaded.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            Location closestLocation = null;
            double closestDistance = double.MaxValue;
            var distanceMessages = new StringBuilder();

            foreach (var location in _locations)
            {
                // Skip invalid entries
                if (!SLEUtility.IsValidLatitude(location.Lat) || !SLEUtility.IsValidLongitude(location.Lon))
                {
                    continue;
                }

                double distance = GetDistance(latitude, longitude, location.Lat, location.Lon);

                // Aggregate distance calculations for debugging
                if (distanceMessages.Length < 1000) // Limit the number of characters for debugging
                {
                    distanceMessages.AppendLine($"Distance to {location.FullLocation} (Lat: {location.Lat}, Lon: {location.Lon}): {distance}");
                }

                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestLocation = location;
                }
            }

            // Show aggregated distance calculations
            //MessageBox.Show(distanceMessages.ToString(), "Distance Calculation", MessageBoxButton.OK, MessageBoxImage.Information);

            if (closestLocation != null)
            {
                SelectedLocation = closestLocation;
                //MessageBox.Show($"Closest Location: {closestLocation.FullLocation}\nLat: {closestLocation.Lat}\nLon: {closestLocation.Lon}\nDoses: {closestLocation.Doses}", "Closest Location", MessageBoxButton.OK, MessageBoxImage.Information);

                // Process the selected location
                string errorMessage;
                if (SLEUtility.TrySetProjectLocation(SelectedLocation, _commandData, out errorMessage))
                {
                    SLEUtility.SaveLocation(SelectedLocation);
                    //SLEUtility.DisplayConfirmationDialog(SelectedLocation);
                }
                else
                {
                    MessageBox.Show($"Failed to set location(FindClosestCoordinates): {errorMessage}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }



        private double GetDistance(double lat1, double lon1, double lat2, double lon2)
        {
            var d1 = lat1 * (Math.PI / 180.0);
            var num1 = lon1 * (Math.PI / 180.0);
            var d2 = lat2 * (Math.PI / 180.0);
            var num2 = lon2 * (Math.PI / 180.0) - num1;
            var d3 = Math.Pow(Math.Sin((d2 - d1) / 2.0), 2.0) + Math.Cos(d1) * Math.Cos(d2) * Math.Pow(Math.Sin(num2 / 2.0), 2.0);

            return 6376500.0 * (2.0 * Math.Atan2(Math.Sqrt(d3), Math.Sqrt(1.0 - d3))); // Distance in meters
        }

        private async Task LoadLocationsAsync(string resourceName)
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                using (StreamReader reader = new StreamReader(stream))
                {
                    string json = await reader.ReadToEndAsync();
                    List<Location> locations;
                    if (CurrentCondition == "In-Ground")
                    {
                        locations = JsonConvert.DeserializeObject<List<Location>>(json, new JsonSerializerSettings
                        {
                            NullValueHandling = NullValueHandling.Ignore
                        });
                    }
                    else
                    {
                        locations = JsonConvert.DeserializeObject<List<Location>>(json);
                    }

                    // Filter out invalid entries
                    locations = locations.Where(loc => SLEUtility.IsValidLatitude(loc.Lat) && SLEUtility.IsValidLongitude(loc.Lon)).ToList();

                    UpdateLocationDropdown(locations);
                }
            }
            catch (JsonException jsonEx)
            {
                MessageBox.Show($"Failed to parse locations: {jsonEx.Message}", "JSON Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load locations: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        public async Task LoadLocationsBasedOnConditionAsync(string condition)
        {
            string resourceName = condition == "In-Ground"
                ? "RevitWoodLCC.assets.dosesIGCityCountry.json"
                : "RevitWoodLCC.assets.DoseInputsCity.json";

            await LoadLocationsAsync(resourceName);
        }

        private void UpdateLocationDropdown(List<Location> locations)
        {
            Dispatcher.Invoke(() =>
            {
                _locations = locations;
                LocationDropdown.ItemsSource = _locations;
                LocationDropdown.DisplayMemberPath = "FullLocation";
            });
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = SearchBox.Text.ToLower();
            LocationDropdown.ItemsSource = _locations.Where(location => location.FullLocation.ToLower().Contains(searchText)).ToList();
        }

        private void LocationDropdown_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectedLocation = LocationDropdown.SelectedItem as Location;
            if (SelectedLocation != null)
            {
                string errorMessage;
                if (SLEUtility.TrySetProjectLocation(SelectedLocation, _commandData, out errorMessage))
                {
                    SLEUtility.SaveLocation(SelectedLocation);
                    //SLEUtility.DisplayConfirmationDialog(SelectedLocation);											  
                    DialogResult = true;
                    Close();
                }
                else
                {
                    MessageBox.Show($"Failed to set location(LocationDropdown_SelectionChanged): {errorMessage}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }


        [ComVisible(true)]
        public class ScriptManager
        {
            private readonly Action<double, double> _setCoordinates;

            public ScriptManager(Action<double, double> setCoordinates)
            {
                _setCoordinates = setCoordinates;
            }

            public void SetCoordinates(double latitude, double longitude)
            {
                //MessageBox.Show($"SetCoordinates called from JavaScript with: Latitude = {latitude}, Longitude = {longitude}");
                _setCoordinates?.Invoke(latitude, longitude);
            }

            public void TestMethod()
            {
                MessageBox.Show("TestMethod called from JavaScript");
            }
        }

    }

    public class Location
    {
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

        [JsonProperty("lat")]
        public double Lat { get; set; }

        [JsonProperty("lon")]
        public double Lon { get; set; }

        [JsonProperty("lats")]
        public double? Lats
        {
            get => Lat;
            set
            {
                if (value.HasValue)
                {
                    Lat = value.Value;
                }
            }
        }

        [JsonProperty("lons")]
        public double? Lons
        {
            get => Lon;
            set
            {
                if (value.HasValue)
                {
                    Lon = value.Value;
                }
            }
        }

        [JsonProperty("WDR_ratio")]
        public double? WDRRatio { get; set; }

        [JsonProperty("WDR_ratio_h")]
        public double? WDRRatioH { get; set; }

        [JsonProperty("city")]
        public string City { get; set; }

        [JsonProperty("country")]
        public string Country { get; set; }

        [JsonProperty("doses")]
        public double? Doses { get; set; }

        public string FullLocation => $"{City}, {Country}";
    }

}

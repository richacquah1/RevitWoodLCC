

using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Globalization;
using System.Linq;

namespace RevitWoodLCC
{
    [Transaction(TransactionMode.Manual)]
    public class SetProjectLocation : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            SetProjectLocationWindow window = new SetProjectLocationWindow();
            window.ShowDialog();

            if (window.DialogResult != true)
            {
                return Result.Cancelled;
            }

            Location selectedLocation = window.SelectedLocation;
            if (!LocationUtils.TrySetProjectLocation(selectedLocation, out string errorMessage))
            {
                message = errorMessage;
                MessageBox.Show(errorMessage, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return Result.Failed;
            }

            // At this point, location values have been validated and are correct
            if (!double.TryParse(selectedLocation.Lat, NumberStyles.Any, CultureInfo.InvariantCulture, out double latitude) ||
                !double.TryParse(selectedLocation.Lon, NumberStyles.Any, CultureInfo.InvariantCulture, out double longitude))
            {
                // This should never happen because `TrySetProjectLocation` already checked these values
                message = "Unexpected error parsing latitude or longitude values.";
                return Result.Failed;
            }

            double latitudeInRadians = latitude * (Math.PI / 180);
            double longitudeInRadians = longitude * (Math.PI / 180);

            try
            {
                using (Transaction transaction = new Transaction(commandData.Application.ActiveUIDocument.Document, "Set Project Location"))
                {
                    transaction.Start();

                    SiteLocation siteLocation = commandData.Application.ActiveUIDocument.Document.SiteLocation;
                    siteLocation.Latitude = latitudeInRadians;
                    siteLocation.Longitude = longitudeInRadians;
                    siteLocation.Name = selectedLocation.City;
                    transaction.Commit();
                }

                // This is where you call FormatLocationData and display the TaskDialog
                string extraData = FormatLocationData(selectedLocation);
                TaskDialog.Show("Location Set", "Project location set successfully.\n\n" + extraData);

            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }

            return Result.Succeeded;
        }

        // This method goes inside the SetProjectLocation class, not inside any other method.
        private string FormatLocationData(Location location)
        {
            // Check each property for null or default values if needed before formatting
            return $"City: {location.City}\n" +
                   $"Country: {location.Country}\n" +
                   $"D_ref: {location.DRef}\n" +
                   $"D_shelt: {location.DShelt}\n" +
                   $"K_trap1: {location.KTrap1}\n" +
                   $"K_trap2: {location.KTrap2}\n" +
                   $"K_trap3: {location.KTrap3}\n" +
                   $"K_trap4: {location.KTrap4}\n" +
                   $"K_trap5: {location.KTrap5}\n" +
                   $"WDR_ratio: {location.WDRRatio}\n" +
                   $"WDR_ratio_h: {location.WDRRatioH}";
        }
    }



    // This could be moved to a shared utility class or kept here but made static
    public static class LocationUtils
    {
        public static bool TrySetProjectLocation(Location location, out string errorMessage)
        {
            errorMessage = "";
            if (!double.TryParse(location.Lat, NumberStyles.Any, CultureInfo.InvariantCulture, out double latitude) ||
                !double.TryParse(location.Lon, NumberStyles.Any, CultureInfo.InvariantCulture, out double longitude))
            {
                errorMessage = "Invalid latitude or longitude value.";
                return false;
            }

            if (latitude < -90 || latitude > 90)
            {
                errorMessage = "The latitude value is out of range. It must be between -90 and 90 degrees.";
                return false;
            }

            if (longitude < -180 || longitude > 180)
            {
                errorMessage = "The longitude value is out of range. It must be between -180 and 180 degrees.";
                return false;
            }

            SaveLocation(location);

            return true;
        }
        public static void SaveLocation(Location location)
        {
            string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RevitProjectLocation.json");
            string json = JsonConvert.SerializeObject(location);
            File.WriteAllText(filePath, json);
        }

    }

    public class SetProjectLocationWindow : Window
    {
        private System.Windows.Controls.ComboBox LocationDropdown;
        private System.Windows.Controls.TextBox SearchBox;
        private List<Location> _locations;
        public Location SelectedLocation { get; private set; }

        //private readonly Document _document;
        public SetProjectLocationWindow()
        {

            Title = "Set Revit Project Location";
            Width = 450;
            Height = 200;
            InitializeComponents();
            LoadLocations();
        }

        private void InitializeComponents()
        {
            System.Windows.Controls.Grid mainGrid = new System.Windows.Controls.Grid();
            Content = mainGrid;

            ColumnDefinition colDef1 = new ColumnDefinition();
            ColumnDefinition colDef2 = new ColumnDefinition();
            mainGrid.ColumnDefinitions.Add(colDef1);
            mainGrid.ColumnDefinitions.Add(colDef2);

            StackPanel stackPanel = new StackPanel();
            mainGrid.Children.Add(stackPanel);
            System.Windows.Controls.Grid.SetColumn(stackPanel, 0);

            SearchBox = new System.Windows.Controls.TextBox { Width = 200, Margin = new Thickness(5) };
            SearchBox.TextChanged += SearchBox_TextChanged;
            stackPanel.Children.Add(SearchBox);

            LocationDropdown = new System.Windows.Controls.ComboBox { Width = 200, Margin = new Thickness(5) };
            LocationDropdown.SelectionChanged += LocationDropdown_SelectionChanged;
            stackPanel.Children.Add(LocationDropdown);

            Border mapPlaceholder = new Border
            {
                Background = Brushes.Gray,
                Margin = new Thickness(5),
                Child = new TextBlock { Text = "Map Placeholder", VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center }
            };
            mainGrid.Children.Add(mapPlaceholder);
            System.Windows.Controls.Grid.SetColumn(mapPlaceholder, 1);
        }

        //private void LoadLocations()
        //{
        //    string jsonFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "locationLatLon.json");
        //    if (!File.Exists(jsonFilePath))
        //    {
        //        MessageBox.Show("The location file does not exist.", "File Not Found", MessageBoxButton.OK, MessageBoxImage.Error);
        //        return;
        //    }

        //    try
        //    {
        //        string json = File.ReadAllText(jsonFilePath);
        //        _locations = JsonConvert.DeserializeObject<List<Location>>(json);
        //        LocationDropdown.ItemsSource = _locations;
        //        LocationDropdown.DisplayMemberPath = "FullLocation";
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show("Error loading locations: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        //    }
        //}

        private void LoadLocations()
        {
            string jsonFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "json1.json");
            if (!File.Exists(jsonFilePath))
            {
                MessageBox.Show("The location file does not exist.", "File Not Found", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            try
            {
                string json = File.ReadAllText(jsonFilePath);
                _locations = JsonConvert.DeserializeObject<List<Location>>(json);
                // Add a breakpoint here to inspect the _locations list
                if (_locations.Any(location => location.DRef == 0)) // This is just an example, adjust according to your data structure
                {
                    MessageBox.Show("One or more locations have zero values for D_ref.", "Data Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                LocationDropdown.ItemsSource = _locations;
                LocationDropdown.DisplayMemberPath = "FullLocation";
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading locations: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_locations == null) return;
            string searchText = SearchBox.Text.ToLower();
            LocationDropdown.ItemsSource = _locations.FindAll(location =>
                location.FullLocation.ToLower().Contains(searchText));
        }

        private void LocationDropdown_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0)
            {
                SelectedLocation = e.AddedItems[0] as Location;
                DialogResult = true;
                Close();
            }
        }

        private void SaveLocation(Location location)
        {
            // Define the path where you want to save the location details
            string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RevitProjectLocation.json");

            // Serialize the Location object to a JSON string
            string json = JsonConvert.SerializeObject(location);

            // Write the JSON string to the file
            File.WriteAllText(filePath, json);
        }


    }

    public class Location
    {
        [JsonProperty("D_ref")]
        public double DRef { get; set; }

        [JsonProperty("D_shelt")]
        public double DShelt { get; set; }

        [JsonProperty("k_trap1")]
        public double KTrap1 { get; set; }

        [JsonProperty("k_trap2")]
        public double KTrap2 { get; set; }

        [JsonProperty("k_trap3")]
        public double KTrap3 { get; set; }

        [JsonProperty("k_trap4")]
        public double KTrap4 { get; set; }

        [JsonProperty("k_trap5")]
        public double KTrap5 { get; set; }

        [JsonProperty("lat")]
        public string Lat { get; set; }

        [JsonProperty("lon")]
        public string Lon { get; set; }

        [JsonProperty("WDR_ratio")]
        public double WDRRatio { get; set; }

        [JsonProperty("WDR_ratio_h")]
        public double WDRRatioH { get; set; }

        [JsonProperty("city")]
        public string City { get; set; }

        [JsonProperty("country")]
        public string Country { get; set; }


        public string FullLocation => $"{City}, {Country}";
    }



}


using System;
using System.Windows;
using System.Windows.Controls;

namespace RevitWoodLCC
{
    public partial class ShowMapView : Window
    {
        public ShowMapView()
        {
            InitializeComponent();
            // Set default map service
            MapServiceComboBox.SelectedIndex = 0;
        }

        private void MapServiceComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MapServiceComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                try
                {
                    // Get the URL from the selected item's Tag property
                    string url = selectedItem.Tag.ToString();
                    MapWebBrowser.Source = new Uri(url);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to load map: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}

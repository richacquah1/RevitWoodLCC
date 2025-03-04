using System;
using System.Windows;
using System.Windows.Navigation;

namespace RevitWoodLCC
{
    public partial class WebViewWindow : Window
    {
        public WebViewWindow()
        {
            InitializeComponent();
            WebBrowserControl.Navigated += WebBrowserControl_Navigated;

            try
            {
                // Replace with your desired URL
                WebBrowserControl.Source = new Uri("https://www.example.com");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load website: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void WebBrowserControl_Navigated(object sender, NavigationEventArgs e)
        {
            if (e.Uri == null)
            {
                MessageBox.Show("Failed to navigate to the specified URL.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
            {
                // You can add additional checks or logging here
            }
        }
    }
}

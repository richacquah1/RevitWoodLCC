//using System;
//using System.Diagnostics;
//using System.IO;
//using System.Reflection;
//using System.Runtime.InteropServices;
//using System.Windows;
//using Microsoft.Win32;

//namespace RevitWoodLCC.InteractiveMapWindow
//{
//    public partial class InteractiveMapWindow : Window
//    {
//        public InteractiveMapWindow()
//        {
//            InitializeComponent();
//            SetBrowserEmulationMode(); // Call the method here
//        }

//        private void ChooseFileButton_Click(object sender, RoutedEventArgs e)
//        {
//            // Create an instance of the open file dialog box
//            OpenFileDialog openFileDialog = new OpenFileDialog
//            {
//                Filter = "HTML files (*.html)|*.html|All files (*.*)|*.*",
//                Title = "Select an HTML File"
//            };

//            // Show the dialog and get result
//            bool? result = openFileDialog.ShowDialog();

//            if (result == true)
//            {
//                // Get the selected file name
//                string filePath = openFileDialog.FileName;

//                // Ensure the file exists before navigating
//                if (File.Exists(filePath))
//                {
//                    MapWebBrowser.ObjectForScripting = new ScriptManager(SetCoordinates);
//                    MapWebBrowser.Navigate(new Uri(filePath));
//                }
//                else
//                {
//                    MessageBox.Show($"Cannot find file '{filePath}'. Make sure the path or Internet address is correct.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
//                }
//            }
//        }

//        private void SetCoordinates(double latitude, double longitude)
//        {
//            // Handle the selected coordinates here
//            MessageBox.Show($"Selected Coordinates: Latitude = {latitude}, Longitude = {longitude}");
//        }

//        // Method to set the browser emulation mode
//        private void SetBrowserEmulationMode()
//        {
//            // Get the name of the executable file
//            string exeName = Path.GetFileName(Process.GetCurrentProcess().MainModule.FileName);

//            try
//            {
//                // Registry path for the browser emulation mode
//                string regKeyPath = @"SOFTWARE\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_BROWSER_EMULATION";

//                // Open the registry key
//                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(regKeyPath, true))
//                {
//                    if (key == null)
//                    {
//                        using (RegistryKey newKey = Registry.CurrentUser.CreateSubKey(regKeyPath))
//                        {
//                            newKey.SetValue(exeName, 11001, RegistryValueKind.DWord);
//                        }
//                    }
//                    else
//                    {
//                        key.SetValue(exeName, 11001, RegistryValueKind.DWord);
//                    }
//                }
//            }
//            catch (Exception ex)
//            {
//                MessageBox.Show("Error setting browser emulation mode: " + ex.Message);
//            }
//        }

//        // ScriptManager class integrated here
//        [ComVisible(true)]
//        public class ScriptManager
//        {
//            private readonly Action<double, double> _setCoordinates;

//            public ScriptManager(Action<double, double> setCoordinates)
//            {
//                _setCoordinates = setCoordinates;
//            }

//            public void SetCoordinates(double latitude, double longitude)
//            {
//                _setCoordinates?.Invoke(latitude, longitude);
//            }
//        }
//    }
//}

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using Microsoft.Win32;

namespace RevitWoodLCC.InteractiveMapWindow
{
    public partial class InteractiveMapWindow : Window
    {
        public InteractiveMapWindow()
        {
            InitializeComponent();
            SetBrowserEmulationMode(); // Call the method here
            LoadHtmlFile(); // Load the HTML file automatically
        }

        private void LoadHtmlFile()
        {
            // Construct the path to the HTML file relative to the application directory
            string tempFilePath = Path.Combine(Path.GetTempPath(), "InteractiveMap.html");
            string resourceName = "RevitWoodLCC.Test_Codes.InteractiveMapWindow.InteractiveMap.html";

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

            // Ensure the file exists before navigating
            if (File.Exists(tempFilePath))
            {
                MapWebBrowser.ObjectForScripting = new ScriptManager(SetCoordinates);
                MapWebBrowser.Navigate(new Uri(tempFilePath));
            }
            else
            {
                MessageBox.Show($"Cannot find file '{tempFilePath}'. Make sure the path or Internet address is correct.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SetCoordinates(double latitude, double longitude)
        {
            // Handle the selected coordinates here
            MessageBox.Show($"Selected Coordinates: Latitude = {latitude}, Longitude = {longitude}");
        }

        // Method to set the browser emulation mode
        private void SetBrowserEmulationMode()
        {
            // Get the name of the executable file
            string exeName = Path.GetFileName(Process.GetCurrentProcess().MainModule.FileName);

            try
            {
                // Registry path for the browser emulation mode
                string regKeyPath = @"SOFTWARE\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_BROWSER_EMULATION";

                // Open the registry key
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(regKeyPath, true))
                {
                    if (key == null)
                    {
                        using (RegistryKey newKey = Registry.CurrentUser.CreateSubKey(regKeyPath))
                        {
                            newKey.SetValue(exeName, 11001, RegistryValueKind.DWord);
                        }
                    }
                    else
                    {
                        key.SetValue(exeName, 11001, RegistryValueKind.DWord);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error setting browser emulation mode: " + ex.Message);
            }
        }

        // ScriptManager class integrated here
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
                _setCoordinates?.Invoke(latitude, longitude);
            }
        }
    }
}

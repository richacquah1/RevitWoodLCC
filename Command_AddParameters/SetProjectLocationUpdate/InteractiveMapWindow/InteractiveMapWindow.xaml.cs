//using System;
//using System.Diagnostics;
//using System.IO;
//using System.Reflection;
//using System.Runtime.InteropServices;
//using System.Windows;
//using Microsoft.Win32;

//namespace RevitWoodLCC
//{
//    public partial class InteractiveMapWindow : Window
//    {
//        public InteractiveMapWindow()
//        {
//            InitializeComponent();
//            SetBrowserEmulationMode();
//            LoadHtmlFile();
//        }

//        public void LoadHtmlFile()
//        {
//            string tempFilePath = Path.Combine(Path.GetTempPath(), "InteractiveMap.html");
//            string resourceName = "RevitWoodLCC.Command_AddParameters.SetProjectLocationUpdate.InteractiveMapWindow.InteractiveMap.html";

//            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
//            {
//                if (stream == null)
//                {
//                    MessageBox.Show($"Cannot find embedded resource '{resourceName}'. Ensure the resource name is correct.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
//                    return;
//                }

//                using (FileStream fileStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write))
//                {
//                    stream.CopyTo(fileStream);
//                }
//            }

//            if (File.Exists(tempFilePath))
//            {
//                MapWebBrowser.ObjectForScripting = new ScriptManager(SetCoordinates);
//                MapWebBrowser.Navigate(new Uri(tempFilePath));
//            }
//            else
//            {
//                MessageBox.Show($"Cannot find file '{tempFilePath}'. Make sure the path or Internet address is correct.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
//            }
//        }

//        public void SetCoordinates(double latitude, double longitude)
//        {
//            MessageBox.Show($"Selected Coordinates: Latitude = {latitude}, Longitude = {longitude}");
//        }

//        private void SetBrowserEmulationMode()
//        {
//            string exeName = Path.GetFileName(Process.GetCurrentProcess().MainModule.FileName);

//            try
//            {
//                string regKeyPath = @"SOFTWARE\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_BROWSER_EMULATION";

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

namespace RevitWoodLCC
{
    public partial class InteractiveMapWindow : Window
    {
        private double _latitude;
        private double _longitude;

        public InteractiveMapWindow()
        {
            InitializeComponent();
            SetBrowserEmulationMode();
            LoadHtmlFile();
        }

        public void LoadHtmlFile()
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
                MapWebBrowser.ObjectForScripting = new ScriptManager(SetCoordinates);
                //MessageBox.Show("Set ObjectForScripting");
                MapWebBrowser.Navigate(new Uri(tempFilePath));
            }
            else
            {
                MessageBox.Show($"Cannot find file '{tempFilePath}'. Make sure the path or Internet address is correct.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [ComVisible(true)]
        public void SetCoordinates(double latitude, double longitude)
        {
            _latitude = latitude;
            _longitude = longitude;
            MessageBox.Show($"SetCoordinates called with: Latitude = {latitude}, Longitude = {longitude}");

            // Method to do something with the coordinates
            FindClosestCoordinates();
        }

        private void FindClosestCoordinates()
        {
            // Example calculation using the coordinates
            double result = _latitude + _longitude; // Replace with your actual calculation logic
            MessageBox.Show($"Closest City and Cordinates are: {result}");
        }

        private void SetBrowserEmulationMode()
        {
            string exeName = Path.GetFileName(Process.GetCurrentProcess().MainModule.FileName);

            try
            {
                string regKeyPath = @"SOFTWARE\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_BROWSER_EMULATION";

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
                MessageBox.Show("ScriptManager SetCoordinates called");
                _setCoordinates?.Invoke(latitude, longitude);
            }
        }
    }
}

using System;
using System.Windows;
using System.Windows.Navigation;

namespace RevitWoodLCC
{
    public partial class AboutWindow : Window
    {
        public AboutWindow()
        {
            InitializeComponentAbout();
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            // Navigate to the named TextBlock (content section)
            var target = this.FindName(e.Uri.OriginalString.TrimStart('#')) as FrameworkElement;
            if (target != null)
            {
                // Scroll to the target element
                target.BringIntoView();
            }
            else
            {
                MessageBox.Show($"The section '{e.Uri.OriginalString}' was not found.", "Navigation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            e.Handled = true;
        }

        private void InitializeComponentAbout()
        {
            // Correct the path to include the folder
            System.Windows.Application.LoadComponent(this, new Uri("/RevitWoodLCC;component/Command_About/AboutWindow.xaml", UriKind.Relative));
        }

    }
}

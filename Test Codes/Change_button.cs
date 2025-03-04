using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.Windows;
using System.Windows.Controls;
using Grid = System.Windows.Controls.Grid;

namespace RevitWoodLCC
{
    [Transaction(TransactionMode.Manual)]
    public class ShowDynamicButtonWindowCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            // Show the WPF window
            DynamicButtonWindow window = new DynamicButtonWindow();
            window.ShowDialog();

            return Result.Succeeded;
        }
    }

    public class DynamicButtonWindow : Window
    {
        private enum ButtonState
        {
            State1,
            State2,
            State3
        }

        private ButtonState currentState = ButtonState.State1;

        public DynamicButtonWindow()
        {
            this.Title = "Dynamic Button Example";
            this.Width = 300;
            this.Height = 200;

            Grid grid = new Grid();
            this.Content = grid;

            CreateDynamicButton(grid);
        }

        private void CreateDynamicButton(Grid grid)
        {
            Button dynamicButton = new Button
            {
                Content = "State 1",
                Width = 100,
                Height = 40,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            dynamicButton.Click += DynamicButton_Click;

            grid.Children.Add(dynamicButton);
        }

        private void DynamicButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;

            switch (currentState)
            {
                case ButtonState.State1:
                    currentState = ButtonState.State2;
                    button.Content = "State 2";
                    break;
                case ButtonState.State2:
                    currentState = ButtonState.State3;
                    button.Content = "State 3";
                    break;
                case ButtonState.State3:
                    currentState = ButtonState.State1;
                    button.Content = "State 1";
                    break;
            }
        }
    }
}

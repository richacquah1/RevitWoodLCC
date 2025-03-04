using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace RevitWoodLCC
{
    [Transaction(TransactionMode.Manual)]
    public class ElementLoopSelector : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                UIDocument uidoc = commandData.Application.ActiveUIDocument;
                Document doc = uidoc.Document;

                // Initiate element selection
                Reference pickedRef = uidoc.Selection.PickObject(ObjectType.Element, "Please select an element.");
                if (pickedRef == null) return Result.Cancelled;

                Element selectedElement = doc.GetElement(pickedRef);

                // Show navigator window
                ElementNavigatorWindow navigatorWindow = new ElementNavigatorWindow(uidoc, selectedElement.Id);
                navigatorWindow.Show();

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }
    }

    public class ElementNavigatorWindow : Window
    {
        private UIDocument _uidoc;
        private List<ElementId> _elementIds;
        private int _currentIndex;

        public ElementNavigatorWindow(UIDocument uidoc, ElementId selectedId)
        {
            _uidoc = uidoc;
            _elementIds = new FilteredElementCollector(_uidoc.Document)
                          .WhereElementIsNotElementType()
                          .ToElementIds().ToList();

            _currentIndex = _elementIds.IndexOf(selectedId);

            InitializeComponents();
        }

        private void InitializeComponents()
        {
            Title = "Element ID Navigator";
            Width = 300;
            Height = 100;
            Topmost = true;

            StackPanel stackPanel = new StackPanel();
            Content = stackPanel;

            TextBlock elementIdText = new TextBlock
            {
                Text = $"Current Element ID: {_elementIds[_currentIndex]}",
                Margin = new Thickness(5)
            };
            stackPanel.Children.Add(elementIdText);

            Button prevButton = new Button
            {
                Content = "Previous",
                Margin = new Thickness(5)
            };
            prevButton.Click += (sender, e) => Navigate(-1, elementIdText);
            stackPanel.Children.Add(prevButton);

            Button nextButton = new Button
            {
                Content = "Next",
                Margin = new Thickness(5)
            };
            nextButton.Click += (sender, e) => Navigate(1, elementIdText);
            stackPanel.Children.Add(nextButton);
        }

        private void Navigate(int direction, TextBlock elementIdText)
        {
            _currentIndex = (_currentIndex + direction + _elementIds.Count) % _elementIds.Count;
            ElementId currentId = _elementIds[_currentIndex];

            _uidoc.Selection.SetElementIds(new List<ElementId> { currentId });
            _uidoc.ShowElements(new List<ElementId> { currentId });

            elementIdText.Text = $"Current Element ID: {currentId}";
        }
    }
}

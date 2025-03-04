
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Grid = System.Windows.Controls.Grid;
using ComboBox = System.Windows.Controls.ComboBox;
using TextBox = System.Windows.Controls.TextBox;
using Button = System.Windows.Controls.Button;
using Label = System.Windows.Controls.Label;
using MessageBox = System.Windows.MessageBox;
using CheckBox = System.Windows.Controls.CheckBox;
using Orientation = System.Windows.Controls.Orientation;
using Image = System.Windows.Controls.Image;
using System.Linq;
using static IronPython.Modules._ast;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ToolTip;
using System.Windows.Forms;
using System.Windows.Media;
using System.Drawing;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using Newtonsoft.Json;
using Autodesk.Revit.UI.Selection;
using System.IO;
using Window = System.Windows.Window;


namespace RevitWoodLCC
{
    public class SLE_PopupForm : Window
    {
        private Document _doc;
        private View3D _view3D;
        private ComboBox materialField;
        private ComboBox treatmentField;
        private ComboBox soilContactField;
        private ComboBox locationField;
        private ComboBox exposureField;
        private ComboBox elementIntersectionField;
        private TextBox serviceLifeOutput;

        private string _elementIdAsString;

        private ElementId _duplicatedViewId;

        private PreviewControl previewControl;

        private bool isIsolatedView = false;

        private UIDocument _uiDoc;

        private System.Windows.Controls.CheckBox verticalMemberCheckbox;
        private System.Windows.Controls.CheckBox roofOverhangCheckbox;
        private TextBox groundDistTextBox;
        private StackPanel overhangPanel;
        private TextBox overhangTextBox;
        private StackPanel shelterDistPanel;
        private TextBox shelterDistTextBox;
        private System.Windows.Controls.Image shelterImage;
        private UIDocument uiDocument;

        private ServiceLifeLogic _serviceLifeLogic = new ServiceLifeLogic();

        // Define Toggle3DPreviewButton as a class-level variable
        private Button toggle3DPreviewButton;

        public SLE_PopupForm(UIDocument uiDoc)
        {
            _doc = uiDoc.Document;
            _uiDoc = uiDoc;
            InitializeComponents(); // Set up the UI components

            //// Initialize toggle3DPreviewButton
            toggle3DPreviewButton = new Button();
            toggle3DPreviewButton.Content = "Toggle 3D Preview";

            //// Attach an event handler
            toggle3DPreviewButton.Click += Toggle3DPreviewButton_Click;

            // Get the selected ElementId
            _elementIdAsString = uiDoc.Selection.GetElementIds().Count > 0 ? uiDoc.Selection.GetElementIds().First().ToString() : "";

            // Get the active 3D view and duplicate it
            _view3D = GetActive3DViewAndDuplicate(_doc);
            if (_view3D == null)
            {
                MessageBox.Show("No suitable 3D view found.");
                return;
            }

            // Assuming _elementIdAsString can be converted to ElementId
            ElementId selectedElementId = new ElementId(Convert.ToInt32(_elementIdAsString));

            // Find adjacent elements
            IList<ElementId> adjacentElementIds = FindAdjacentElements(_doc, selectedElementId);

            // Modify the 3D view
            Modify3DView(_doc, _view3D, adjacentElementIds, selectedElementId, currentMode);

            // Window properties
            Title = "Service Life Estimation";
            Width = 800;
            Height = 700;

            InitializeComponents();
            LoadLocation();

            // Handle the Closed event
            this.Closed += SLE_PopupForm_Closed;

        }

        private void SLE_PopupForm_Closed(object sender, EventArgs e)
        {
            if (_duplicatedViewId != null)
            {
                using (Transaction tx = new Transaction(_doc, "Delete Temp View"))
                {
                    tx.Start();
                    _doc.Delete(_duplicatedViewId);
                    tx.Commit();
                }
            }
        }

        //DONE
        private void InitializeComponents()
        {
            System.Windows.Controls.Grid mainGrid = new System.Windows.Controls.Grid();
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // Add row definitions for each control group
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // For PreviewControl
            for (int i = 1; i < 6; i++) // For the other 5 control groups
            {
                mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            }

            // 3D Preview Control
            if (_view3D != null)
            {
                previewControl = new PreviewControl(_doc, _view3D.Id);
                previewControl.Height = Double.NaN; // Auto height
                previewControl.VerticalAlignment = VerticalAlignment.Stretch;
                System.Windows.Controls.Grid.SetColumn(previewControl, 0);
                mainGrid.Children.Add(previewControl);
            }

            // Vertical stack panel for all controls on the right
            StackPanel stackPanel = new StackPanel();
            System.Windows.Controls.Grid.SetColumn(stackPanel, 1);
            mainGrid.Children.Add(stackPanel);

            string materialDescription = "This ComboBox lets you select the type of material...";
            string treatmentDescription = "This ComboBox lets you select the treatment type of material...";
            string locationDescription = "Choose the location where the wood is placed...";
            string soilContactDescription = "Select if the wood has soil contact...";
            string exposureDescription = "Details about how the wood is exposed...";
            string elementIntersectionDescription = "Information about how wood elements intersect...";
            //string moistureTrapDescription = "Specify the risk of moisture getting trapped...";

            // Adding all ComboBoxes for inputs
            AddLabelAndComboBox(stackPanel, "Material", out materialField, materialDescription, "Norway Spruce"); //"Norway Spruce", "Pine", "Oak");
            AddLabelAndComboBox(stackPanel, "Treatment", out treatmentField, treatmentDescription, "Thermal modification, UC 3"); //, "Thermal modification (Oil-Heat Treatment, OHT), UC 3", "Oak");
            AddLabelAndComboBoxWithButton(stackPanel, "Location", out locationField, locationDescription, "Please select a location"); //, "Lund, Sweden", "Gottingen, Germany");
            AddLabelAndComboBox(stackPanel, "In/Above Ground Condition", out soilContactField, soilContactDescription, "In-Ground", "Above Ground");
            AddLabelAndComboBox(stackPanel, "Exposure", out exposureField, exposureDescription, "Side grain exposed", "end grain exposed");
            AddLabelAndComboBox(stackPanel, "Element Intersection", out elementIntersectionField, elementIntersectionDescription,
                "No contact face or gap size >5 mm free from dirt",
                "Partially ventilated contact face free from dirt",
                "Direct contact or insufficient ventilation");
            // AddLabelAndComboBox(stackPanel, "Risk of Moisture Trap", out riskOfMoistureTrapField, moistureTrapDescription, "High risk", "Medium risk", "Low risk", "No risk");

            // Description text block
            TextBlock descriptionTextBlock = new TextBlock
            {
                Text = "Shelter",
                //TextWrapping = TextWrapping.Wrap,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(5, 5, 0, 0) //
            };
            stackPanel.Children.Add(descriptionTextBlock);

            // Vertical member checkbox
            CheckBox verticalMemberCheckbox = new CheckBox
            {
                Content = "Vertical member (subjected to driving rain)"
            };
            stackPanel.Children.Add(verticalMemberCheckbox);

            // Roof overhang checkbox
            CheckBox roofOverhangCheckbox = new CheckBox
            {
                Content = "Roof overhang"
            };
            stackPanel.Children.Add(roofOverhangCheckbox);

            // Ground distance input
            groundDistTextBox = new TextBox
            {
                Width = 50,
                IsEnabled = false,
                Text = "NA",
                Margin = new Thickness(0, 5, 5, 5) // Added margin
            };

            StackPanel groundDistanceInput = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Children =
            {
                groundDistTextBox,
                new TextBlock { Margin = new Thickness(0, 5, 0, 5), Text = "distance to ground (a)" }
            }
            };
            stackPanel.Children.Add(groundDistanceInput);

            overhangTextBox = new TextBox
            {
                Width = 50,
                Text = "0",
                Margin = new Thickness(0, 5, 5, 5) // Added margin
            };
            overhangPanel = new StackPanel  // Assigning to the field
            {
                Orientation = Orientation.Horizontal,
                Children =
            {
                overhangTextBox,
                new TextBlock { Margin = new Thickness(0, 5, 0, 5), Text = "roof overhang (e)" }
            }
            };

            stackPanel.Children.Add(overhangPanel);

            // Shelter distance input
            shelterDistTextBox = new TextBox
            {
                Width = 50,
                Text = "1.0",
                Margin = new Thickness(0, 5, 5, 5) // Added margin
            };
            shelterDistPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Children =
            {
                shelterDistTextBox,
                new TextBlock { Margin = new Thickness(0, 5, 0, 5), Text = "distance from shelter (d)" }
            }
            };
            stackPanel.Children.Add(shelterDistPanel);

            // Shelter image
            Image shelterImage = new Image
            {
                Source = new BitmapImage(new Uri("path_to_your_image/wall_nooverhang_deck.png", UriKind.Relative)),
                Width = 500
            };
            stackPanel.Children.Add(shelterImage);


            // Create a horizontal stack panel for serviceLifeLabel and serviceLifeOutput + buttons
            StackPanel horizontalPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 5, 0, 5) // Adjust margin if needed
            };

            Label serviceLifeLabel = new Label
            {
                Content = "Service life duration (years)",
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 10, 0) // Some space between label and next stack panel
            };

            // Create a vertical stack panel for serviceLifeOutput and the two buttons
            StackPanel verticalPanelForOutputAndButtons = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Margin = new Thickness(0, 5, 0, 0)  // Top margin added to move the controls down by 5 units. Adjust this value as needed.
            };

            serviceLifeOutput = new TextBox
            {
                IsReadOnly = true,
                Width = 100,
                Margin = new Thickness(0, 0, 0, 5) // Some space between textbox and the buttons
            };
            verticalPanelForOutputAndButtons.Children.Add(serviceLifeOutput);

            // Estimate button
            Button estimateButton = new Button
            {
                Content = "Estimate",
                Width = 100,
                Margin = new Thickness(0, 0, 0, 5) // Space between Estimate and Save button
            };
            estimateButton.Click += EstimateButton_Click;
            verticalPanelForOutputAndButtons.Children.Add(estimateButton);

            // Save button
            Button saveButton = new Button
            {
                Content = "Save",
                Width = 100
            };
            saveButton.Click += SaveButton_Click;
            verticalPanelForOutputAndButtons.Children.Add(saveButton);

            // Add the elements to the main horizontal stack panel
            horizontalPanel.Children.Add(serviceLifeLabel);
            horizontalPanel.Children.Add(verticalPanelForOutputAndButtons);
            stackPanel.Children.Add(horizontalPanel);

            // Create a new main DockPanel for your layout
            DockPanel mainDockPanel = new DockPanel();

            // Button grid for "Toggle 3D Preview", "Previous", and "Next" buttons
            System.Windows.Controls.Grid buttonGrid = new System.Windows.Controls.Grid();
            buttonGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // For Toggle 3D Preview
            buttonGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // For Previous
            buttonGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // For Next

            Button toggle3DPreviewButton = new Button
            {
                Content = "Toggle 3D Preview",
                Width = 160,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Left
            };

            // Attach the click event handler
            toggle3DPreviewButton.Click += Toggle3DPreviewButton_Click;

            // Add the button to the grid
            System.Windows.Controls.Grid.SetColumn(toggle3DPreviewButton, 0); // Far left column
            buttonGrid.Children.Add(toggle3DPreviewButton);

            Button prevButton = new Button
            {
                Content = "Previous",
                Width = 100,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center
            };
            prevButton.Click += PreviousButton_Click;  // Attaching the event handler here
            System.Windows.Controls.Grid.SetColumn(prevButton, 1); // Center column
            buttonGrid.Children.Add(prevButton);

            Button nextButton = new Button
            {
                Content = "Next",
                Width = 100,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Right
            };
            nextButton.Click += NextButton_Click;  // Attaching the event handler here
            System.Windows.Controls.Grid.SetColumn(nextButton, 2); // Far right column
            buttonGrid.Children.Add(nextButton);


            DockPanel.SetDock(buttonGrid, Dock.Bottom); // Place at the bottom of the dock panel
            mainDockPanel.Children.Add(buttonGrid);

            mainDockPanel.Children.Add(mainGrid);

            roofOverhangCheckbox.Checked += RoofOverhangCheckbox_Checked;
            roofOverhangCheckbox.Unchecked += RoofOverhangCheckbox_Unchecked;

            Content = mainDockPanel;
        }

        private void RoofOverhangCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            if (overhangPanel != null)
            {
                overhangPanel.Visibility = System.Windows.Visibility.Visible;
                shelterDistPanel.Visibility = System.Windows.Visibility.Visible;

                groundDistTextBox.IsEnabled = true;       // Activate the groundDistTextBox
                overhangTextBox.IsEnabled = true;         // Activate the overhangTextBox
                shelterDistTextBox.IsEnabled = true;      // Activate the shelterDistTextBox
            }
        }

        private void RoofOverhangCheckbox_Unchecked(object sender, RoutedEventArgs e)
        {
            overhangPanel.Visibility = System.Windows.Visibility.Collapsed;
            shelterDistPanel.Visibility = System.Windows.Visibility.Collapsed;

            groundDistTextBox.IsEnabled = false;      // Deactivate the groundDistTextBox
            overhangTextBox.IsEnabled = false;        // Deactivate the overhangTextBox
            shelterDistTextBox.IsEnabled = false;     // Deactivate the shelterDistTextBox
        }

        private void AddLabelAndComboBox(StackPanel stackPanel, string labelText, out ComboBox comboBox, string tooltipDescription, params string[] comboItems)
        {
            StackPanel horizontalStack = new StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal
            };

            Label label = new Label
            {
                Content = labelText,
                ToolTip = tooltipDescription, // Set tooltip for label,
                Margin = new System.Windows.Thickness(0, 0, 10, 0), // Adding some margin for spacing
                FontWeight = FontWeights.Bold
            };
            stackPanel.Children.Add(label);

            comboBox = new ComboBox
            {
                Width = 250,
                ToolTip = tooltipDescription // Set tooltip for combobox
            };
            foreach (var item in comboItems)
            {
                comboBox.Items.Add(item);
            }
            comboBox.SelectedIndex = 0;

            // Adding an info icon (System.Windows.Controls.Image) next to the ComboBox
            System.Windows.Controls.Image infoIcon = new System.Windows.Controls.Image
            {
                Width = 16,
                Height = 16,
                Margin = new System.Windows.Thickness(5, 0, 0, 0), // Giving some margin for spacing
                Source = System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(
                    System.Drawing.SystemIcons.Information.Handle,
                    System.Windows.Int32Rect.Empty,
                    System.Windows.Media.Imaging.BitmapSizeOptions.FromEmptyOptions())
            };
            infoIcon.ToolTip = tooltipDescription;

            // Add both the ComboBox and the info icon to a horizontal container (another StackPanel)
            StackPanel horizontalContainer = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal };
            horizontalContainer.Children.Add(comboBox);
            horizontalContainer.Children.Add(infoIcon);

            stackPanel.Children.Add(horizontalContainer);
        }
        public class MaterialResistance
        {
            [JsonProperty("name")]
            public List<string> Name { get; set; }

            [JsonProperty("latinName")]
            public List<string> LatinName { get; set; }

            [JsonProperty("treatment")]
            public List<string> Treatment { get; set; }

            [JsonProperty("resistanceDoseUC3")]
            public int ResistanceDoseUC3 { get; set; }

            [JsonProperty("resistanceDoseUC4")]
            public int ResistanceDoseUC4 { get; set; }
        }
        private void AddLabelAndComboBoxWithButton(StackPanel stackPanel, string labelText, out ComboBox comboBox, string tooltipDescription, params string[] comboItems)
        {
            Label label = new Label
            {
                Content = labelText,
                ToolTip = tooltipDescription,
                Margin = new Thickness(0, 0, 10, 0),
                FontWeight = FontWeights.Bold
            };
            stackPanel.Children.Add(label);

            StackPanel horizontalContainer = new StackPanel { Orientation = Orientation.Horizontal };

            comboBox = new ComboBox
            {
                Width = 210, // Adjusted width to accommodate the button
                ToolTip = tooltipDescription
            };
            foreach (var item in comboItems)
            {
                comboBox.Items.Add(item);
            }
            comboBox.SelectedIndex = 0;
            horizontalContainer.Children.Add(comboBox);

            // Create a button to open the list of materials
            // Create the button to open the Set Project Location window
            Button setLocationButton = new Button
            {
                Content = "Set Project Location",
                Width = 150, // Adjust the width as needed
                Margin = new Thickness(5, 0, 0, 0), // Adjust the margin as needed
                HorizontalAlignment = System.Windows.HorizontalAlignment.Left // Align the button to the left within its container
            };

            // Add an event handler to the button
            setLocationButton.Click += OpenSetProjectLocationWindowButton_Click;

            // Add the button to the horizontal container
            horizontalContainer.Children.Add(setLocationButton);


            Button updateLocationButton = new Button
            {
                Content = new Image
                {
                    Source = new BitmapImage(new Uri("pack://application:,,,/RevitWoodLCC;component/Resources/update-icon(10x10).png")), // Adjust the path to your icon                                                                                                                                   // Make sure the path is correct
                },
                Width = 15, // Size of the button, adjust as needed
                //Height = 32, // Size of the button, adjust as needed
                Margin = new Thickness(5, 0, 0, 0), // Adjust as needed
                HorizontalAlignment = System.Windows.HorizontalAlignment.Right // Aligns the button to the right within its container
            };
            updateLocationButton.Click += UpdateLocationButton_Click;
            horizontalContainer.Children.Add(updateLocationButton);

            stackPanel.Children.Add(horizontalContainer);
        }
        private void UpdateLocationButton_Click(object sender, RoutedEventArgs e)
        {
            LoadLocation(); // Refreshes the location data from the saved file and updates the ComboBox
        }

        private void OpenSetProjectLocationWindowButton_Click(object sender, RoutedEventArgs e)
        {
            SetProjectLocationWindow setLocationWindow = new SetProjectLocationWindow();
            var result = setLocationWindow.ShowDialog();
            if (result == true)
            {
                // Assuming SetLocationField() is the method that updates the UI
                SetLocationField(setLocationWindow.SelectedLocation);

                if (!LocationUtils.TrySetProjectLocation(setLocationWindow.SelectedLocation, out string errorMessage))
                {
                    MessageBox.Show(errorMessage, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }


        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show($"Service life duration of {serviceLifeOutput.Text} months has been saved.", "Saved", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void PreviousButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Implement the previous logic
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Implement the next logic
        }

        public void SetMaterialField(string value)
        {
            if (!materialField.Items.Contains(value))
            {
                materialField.Items.Add(value);
            }
            materialField.SelectedItem = value;
        }

        private void LoadLocation()
        {
            string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RevitProjectLocation.json");
            if (File.Exists(filePath))
            {
                string json = File.ReadAllText(filePath);
                Location location = JsonConvert.DeserializeObject<Location>(json);

                // Ensure the ComboBox is updated on the UI thread
                Dispatcher.Invoke(() =>
                {
                    SetLocationField(location);
                });
            }
            else
            {
                // Prompt the user to set the location if it's not already saved
                OpenSetLocationWindow();
            }
        }

        private void SetLocationField(Location location)
        {
            // Assuming location.City is the city name and that's what you want to display and select in the ComboBox
            var locationName = location.City; // This gets the city name from the location object

            // Check if the ComboBox contains an item with the location name
            // This assumes the ComboBox is directly populated with strings. If it's populated with objects, you'd need to adjust the logic to find the correct object based on the city name.
            var locationItem = locationField.Items.Cast<string>().FirstOrDefault(item => item.Contains(locationName));

            if (locationItem != null)
            {
                // If found, select the item in the ComboBox
                locationField.SelectedItem = locationItem;
            }
            else
            {
                // If the location is not found in the existing items, optionally add it and then select it
                locationField.Items.Add(locationName);
                locationField.SelectedItem = locationName;
            }
        }



        private void OpenSetLocationWindow()
        {
            SetProjectLocationWindow setLocationWindow = new SetProjectLocationWindow();
            var result = setLocationWindow.ShowDialog();
            if (result == true)
            {
                // Reinitialize the location field to reflect the newly set location
                LoadLocation();
            }
        }

        private bool ValidateFormInputs()
        {
            return materialField.SelectedItem != null &&
                   treatmentField.SelectedItem != null &&
                   soilContactField.SelectedItem != null &&
                   locationField.SelectedItem != null &&
                   exposureField.SelectedItem != null &&
                   elementIntersectionField.SelectedItem != null; //&&
                                                                  //riskOfMoistureTrapField.SelectedItem != null;
        }


        private View3D GetFirst3DView(Document doc)
        {
            FilteredElementCollector collector = new FilteredElementCollector(doc)
                .OfClass(typeof(View3D));
            foreach (View3D v in collector)
            {
                if (!v.IsTemplate && v.CanBePrinted)
                {
                    return v;
                }
            }
            return null;
        }


        // Method to find adjacent elements based on bounding box intersection
        private IList<ElementId> FindAdjacentElements(Document doc, ElementId selectedId)
        {
            Element selectedElement = doc.GetElement(selectedId);
            BoundingBoxXYZ selectedBoundingBox = selectedElement.get_BoundingBox(null);
            Outline outline = new Outline(selectedBoundingBox.Min, selectedBoundingBox.Max);
            BoundingBoxIntersectsFilter bbFilter = new BoundingBoxIntersectsFilter(outline);

            FilteredElementCollector collector = new FilteredElementCollector(doc).WherePasses(bbFilter);

            IList<ElementId> adjacentElementIds = new List<ElementId>();

            foreach (Element element in collector)
            {
                if (element.Id == selectedId)
                    continue;  // Skip the selected element itself

                GeometryElement geomElem = element.get_Geometry(new Options());
                if (geomElem != null)
                {
                    foreach (GeometryObject geomObj in geomElem)
                    {
                        if (geomObj is Solid || geomObj is GeometryInstance)
                        {
                            adjacentElementIds.Add(element.Id);
                            break;
                        }
                    }
                }
            }

            return adjacentElementIds;
        }

        // VisualizationMode enum definition, if not already defined

        private enum VisualizationMode
        {
            AllElements,
            SelectedAndAdjacent,
            SelectedOnly
        }

        private VisualizationMode currentMode = VisualizationMode.AllElements;

        private void Modify3DView(Document doc, View3D view3D, IList<ElementId> adjacentElementIds, ElementId selectedElementId, VisualizationMode mode)
        {
            TransactionStatus txStatus;

            using (Transaction tx = new Transaction(doc, "Modify 3D View"))
            {
                tx.Start();

                switch (mode)
                {
                    case VisualizationMode.AllElements:
                        // Reset the view to its original state
                        view3D.DisableTemporaryViewMode(TemporaryViewMode.TemporaryHideIsolate);

                        // Unhide any previously hidden categories
                        foreach (Category category in doc.Settings.Categories)
                        {
                            if (view3D.CanCategoryBeHidden(category.Id))
                            {
                                view3D.SetCategoryHidden(category.Id, false);
                            }
                        }

                        // Hide level lines in the duplicated view
                        Category levelCategory = doc.Settings.Categories.get_Item(BuiltInCategory.OST_Levels);
                        if (levelCategory != null)
                        {
                            // Hide the category
                            view3D.SetCategoryHidden(levelCategory.Id, true);
                        }
                        break;

                    case VisualizationMode.SelectedAndAdjacent:
                        IList<ElementId> SelectedAndAdjacentToIsolate = new List<ElementId>(adjacentElementIds);
                        SelectedAndAdjacentToIsolate.Add(selectedElementId);

                        // Reset the view to its original state
                        view3D.DisableTemporaryViewMode(TemporaryViewMode.TemporaryHideIsolate);

                        //view3D.IsolateElementsTemporary(adjacentElementIds);
                        view3D.IsolateElementsTemporary(SelectedAndAdjacentToIsolate);

                        break;
                        ;

                    case VisualizationMode.SelectedOnly:
                        IList<ElementId> selectedElementOnly = new List<ElementId> { selectedElementId }; // Container for only the selected element
                        // Isolate only the selected element
                        view3D.IsolateElementsTemporary(selectedElementOnly);
                        break;
                }

                txStatus = tx.Commit();
            }
        }

        private View3D GetActive3DViewAndDuplicate(Document doc)
        {
            // Get the active view
            View3D activeView3D = doc.ActiveView as View3D;

            if (activeView3D == null || activeView3D.IsTemplate || !activeView3D.CanBePrinted)
                return null;

            // Start a transaction to duplicate the view
            View3D duplicatedView3D;
            using (Transaction tx = new Transaction(doc, "Duplicate 3D View"))
            {
                tx.Start();

                // Duplicate the active view
                ElementId duplicatedViewId = activeView3D.Duplicate(ViewDuplicateOption.WithDetailing);
                duplicatedView3D = doc.GetElement(duplicatedViewId) as View3D;
                duplicatedView3D.Name = "Temporary Preview View"; // Give it a new name

                _duplicatedViewId = duplicatedViewId;

                tx.Commit();
            }

            return duplicatedView3D;
        }

        private void Toggle3DPreviewButton_Click(object sender, RoutedEventArgs e)
        {
            UIDocument uiDoc = _uiDoc;
            Document doc = uiDoc.Document;

            View3D view3D = doc.GetElement(_duplicatedViewId) as View3D; // Assuming you've stored the duplicated view ID in _duplicatedViewId

            ElementId firstSelectedId = uiDoc.Selection.GetElementIds().FirstOrDefault();
            string elementIdAsString = firstSelectedId?.ToString();

            Button toggle3DPreviewButton = sender as Button;
            Button new_toggle3DPreviewButton = sender as Button;

            // Cycle through the visualization modes
            switch (currentMode)
            {

                case VisualizationMode.AllElements:
                    currentMode = VisualizationMode.SelectedOnly;
                    new_toggle3DPreviewButton.Content = "Show Selected and Adjacent";
                    break;

                case VisualizationMode.SelectedAndAdjacent:
                    currentMode = VisualizationMode.AllElements;
                    new_toggle3DPreviewButton.Content = "Show Selected Only";
                    break;

                case VisualizationMode.SelectedOnly:
                    currentMode = VisualizationMode.SelectedAndAdjacent;
                    new_toggle3DPreviewButton.Content = "Show All Elements";
                    break;
            }

            // Assuming elementIdAsString can be converted to ElementId
            ElementId selectedElementId = new ElementId(Convert.ToInt32(elementIdAsString));

            // Find adjacent elements
            IList<ElementId> adjacentElementIds = FindAdjacentElements(doc, selectedElementId);

            // Modify the 3D view
            Modify3DView(_doc, _view3D, adjacentElementIds, selectedElementId, currentMode);

            // Update the UI, e.g., change the button's label to indicate the next mode
            //UpdateButtonLabel();

        }
        private void EstimateButton_Click(object sender, RoutedEventArgs e)
        {
            // Ensure all inputs are provided
            // For simplicity, error checking is omitted; consider adding null checks or validation as needed
            double latitude = double.Parse(locationField.SelectedItem?.ToString().Split(',')[0]); // Assuming format "lat,lon"
            double longitude = double.Parse(locationField.SelectedItem?.ToString().Split(',')[1]);
            string materialName = materialField.SelectedItem.ToString();
            string treatment = treatmentField.SelectedItem.ToString();
            bool isVertical = verticalMemberCheckbox.IsChecked == true;
            bool hasRoofOverhang = roofOverhangCheckbox.IsChecked == true;
            double groundDistance = double.Parse(groundDistTextBox.Text);
            double overhangLength = double.Parse(overhangTextBox.Text);
            double shelterDistance = double.Parse(shelterDistTextBox.Text);
            string exposure = exposureField.SelectedItem.ToString();

            // Initialize ServiceLifeLogic if not already done
            if (_serviceLifeLogic == null)
            {
                _serviceLifeLogic = new ServiceLifeLogic();
            }

            try
            {
                // Calculate service life
                double serviceLife = _serviceLifeLogic.CalculateServiceLife(
                    latitude,
                    longitude,
                    materialName,
                    treatment,
                    isVertical,
                    hasRoofOverhang,
                    groundDistance,
                    overhangLength,
                    shelterDistance,
                    exposure);

                // Display the result
                serviceLifeOutput.Text = serviceLife.ToString("N2"); // Format for readability
            }
            catch (Exception ex)
            {
                // Handle any errors, possibly missing data or incorrect inputs
                MessageBox.Show($"Error calculating service life: {ex.Message}", "Calculation Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private int CalculateServiceLifeDuration(double dose, string material, string treatment, string location)
        {
            // Fetch the material resistance based on the selected material and treatment
            var materialResistance = _serviceLifeLogic.GetAllMatchingMaterialResistances(material, null, treatment).FirstOrDefault();

            // If we can't find the resistance, return an error or default value
            if (materialResistance == null) return -1;  // Or some other default/error value

            // Determine if it's in-ground or above-ground (based on some condition or input)
            bool isInGround = soilContactField.SelectedItem?.ToString() == "In-Ground";


            double resistanceDose = isInGround ? materialResistance.ResistanceDoseUC4 : materialResistance.ResistanceDoseUC3;

            // Calculate the service life estimate based on Dose and resistanceDose
            // This is a simplistic calculation, and you may need a more complex formula
            int serviceLifeEstimate = (int)(resistanceDose / dose);

            //return serviceLifeEstimate;
            return 111;
        }

    }
}





//This works perfect with the SLP FORM
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RevitWoodLCC
{
    [Transaction(TransactionMode.Manual)]
    public class ServiceLifeEstimation : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            // Get the UI document and the current selection
            UIDocument uiDoc = commandData.Application.ActiveUIDocument;
            ICollection<ElementId> selectedIds = uiDoc.Selection.GetElementIds();

            // Check if any elements are selected
            if (selectedIds.Count > 0)
            {
                // Get the first 3D view in the document
                View3D view3D = GetFirst3DView(uiDoc.Document);
                if (view3D == null)
                {
                    TaskDialog.Show("Warning", "No suitable 3D view found.");
                    return Result.Failed;
                }

                // Get the first selected element
                ElementId firstId = selectedIds.First();
                Element firstElement = uiDoc.Document.GetElement(firstId);

                if (uiDoc != null)
                {
                    // Create the pop-up window with the first 3D view
                    //SLE_PopupForm popup = new SLE_PopupForm(uiDoc.Document, view3D);
                    SLE_PopupForm popup = new SLE_PopupForm(uiDoc);


                    // Populate the form fields using the new properties
                    PreFillFormWithData(popup, firstElement, uiDoc.Document);

                    // Display the form
                    popup.ShowDialog();
                    //popup.Show();
                }
            }
            else
            {
                // No elements were selected
                TaskDialog.Show("Warning", "Please select a building element.");
            }

            return Result.Succeeded;

        }

        // Function to get the selected element's material
        private string GetElementMaterial(Element element, Document doc)
        {
            // Get the material IDs
            ICollection<ElementId> materialIds = null;
            if (element is FamilyInstance familyInstance)
            {
                // If it's a FamilyInstance, retrieve the material from the family's symbol
                if (familyInstance.Symbol != null && familyInstance.Symbol.Category != null)
                {
                    materialIds = familyInstance.Symbol.GetMaterialIds(false);
                }
            }
            else
            {
                // Otherwise, retrieve the material directly from the element
                materialIds = element.GetMaterialIds(false);
            }

            if (materialIds == null || materialIds.Count == 0)
            {
                return null;  // Indicate that the element has no material
            }

            // Get the first material's name
            ElementId materialId = materialIds.First();
            Material material = doc.GetElement(materialId) as Material;

            return material.Name;
        }

        // Function to get the project's location
        private string GetProjectLocation(Document doc)
        {
            // The project location is often associated with the Site category or ProjectInfo
            ProjectInfo projectInfo = doc.ProjectInformation;
            if (projectInfo != null)
            {
                Parameter locationParam = projectInfo.LookupParameter("Project Address");
                if (locationParam != null && locationParam.HasValue)
                {
                    return locationParam.AsString();
                }
            }
            return null;  // or a default value if needed
        }

        // Function to pre-fill the form with data using properties
        public void PreFillFormWithData(SLE_PopupForm form, Element element, Document doc)
        {
            string material = GetElementMaterial(element, doc);
            string location = GetProjectLocation(doc);

            form.SetMaterialField(material);
            //form.SetLocationField(location);
        }

        private View3D GetFirst3DView(Document doc)
        {
            FilteredElementCollector collector = new FilteredElementCollector(doc)
                .OfClass(typeof(View3D));
            foreach (View3D v in collector)
            {
                if (!v.IsTemplate && v.CanBePrinted)
                {
                    return v;
                }
            }
            return null;
        }

    }
}




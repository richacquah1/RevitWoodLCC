using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using static RevitWoodLCC.SLE_PopupForm_Logic;
using System.Text.Json;
using System.IO;
using Newtonsoft.Json;
using static RevitWoodLCC.SLEUtility;




namespace RevitWoodLCC
{
    public partial class SLE_PopupForm : Window
    {
        public SLE_PopupForm_Logic _logic;
        public SLE_PopupForm(UIDocument uiDoc, ExternalCommandData commandData)
        {
            if (uiDoc == null) throw new ArgumentNullException(nameof(uiDoc), "UIDocument is null");
            if (commandData == null) throw new ArgumentNullException(nameof(commandData), "ExternalCommandData is null");

            InitializeComponent();


            // Ensure event handler is set
            soilContactField.SelectionChanged += SoilContactField_SelectionChanged;


            try
            {
                // Ensure UI controls are initialized
                ValidateControls();

                _logic = new SLE_PopupForm_Logic(
                    uiDoc,
                    commandData,
                    this,
                    materialField,
                    treatmentField,
                    soilContactField,
                    locationField,
                    exposureField,
                    elementIntersectionField,
                    verticalMemberCheckbox,
                    roofOverhangCheckbox,
                    groundDistTextBox,
                    overhangPanel,
                    overhangTextBox,
                    shelterDistPanel,
                    shelterDistTextBox,
                    toggle3DPreviewButton,
                    serviceLifeOutput
                );

                // Capture the initially selected element
                _logic.CaptureInitialSelection();

                if (_logic._duplicatedViewId == null)
                {
                    MessageBox.Show("Duplicated View ID is null after logic initialization.");
                    return;
                }

                InitializePreviewControl();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during form initialization: {ex.Message}");
            }

        }

        // Public properties for accessing UI controls
        public System.Windows.Controls.ComboBox SoilContactField => soilContactField; // Make sure to adjust access to the actual control
        public System.Windows.Controls.ComboBox ExposureField => exposureField;
        public System.Windows.Controls.ComboBox ElementIntersectionField => elementIntersectionField;
        public System.Windows.Controls.CheckBox RoofOverhangCheckbox => roofOverhangCheckbox;
        public System.Windows.Controls.CheckBox VerticalMemberCheckbox => verticalMemberCheckbox;
        public System.Windows.Controls.TextBox GroundDistTextBox => groundDistTextBox;
        public System.Windows.Controls.TextBox OverhangTextBox => overhangTextBox;
        public System.Windows.Controls.TextBox ShelterDistTextBox => shelterDistTextBox;
        public System.Windows.Controls.TextBox ServiceLifeOutput => serviceLifeOutput;


        public void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            _logic.SaveButton_Click(serviceLifeOutput, sender, e);
        }

        private void InitializePreviewControl()
        {
            try
            {
                PreviewControl previewControl = new PreviewControl(_logic._doc, _logic._duplicatedViewId);
                System.Windows.Controls.Grid.SetColumn(previewControl, 0);
                ((System.Windows.Controls.Grid)((DockPanel)Content).Children[1]).Children.Add(previewControl);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"PreviewControl initialization failed: {ex.Message}");
            }
        }

        // Event handler to be called when the SLP window is loaded
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            SLEUtility.LoadLocation(locationField, _logic._commandData, "Above Ground");
        }


        private void SoilContactField_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (sender == null)
                {
                    MessageBox.Show("Error: Sender is null in SoilContactField_SelectionChanged.");
                    return;
                }

                if (!(sender is System.Windows.Controls.ComboBox comboBox))
                {
                    MessageBox.Show("Error: Sender is not a ComboBox in SoilContactField_SelectionChanged.");
                    return;
                }

                if (comboBox.SelectedItem == null)
                {
                    MessageBox.Show("Error: ComboBox selected item is null.");
                    return;
                }

                ComboBoxItem selectedItem = comboBox.SelectedItem as ComboBoxItem;
                if (selectedItem == null)
                {
                    MessageBox.Show("Error: Selected item is not a ComboBoxItem.");
                    return;
                }
                string selectedItemContent = selectedItem.Content as string;

                if (selectedItemContent == null)
                {
                    MessageBox.Show("Error: Selected item content is null.");
                    return;
                }

                if (locationField == null)
                {
                    return;
                }

                if (selectedItemContent == "In-Ground")
                {
                    locationField.Items.Clear();
                    _logic.DisableIrrelevantControls();
                }
                else
                {
                    locationField.Items.Clear();
                    _logic.EnableRelevantControls();
                }



                SetProjectLocationWindow locationWindow = new SetProjectLocationWindow(_logic._commandData, selectedItemContent);
                locationWindow.LoadLocationsBasedOnConditionAsync(selectedItemContent).ContinueWith(task =>
                {
                    if (task.Exception != null)
                    {
                        MessageBox.Show($"Failed to load locations: {task.Exception.InnerException.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }, TaskScheduler.FromCurrentSynchronizationContext());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error in SoilContactField_SelectionChanged: {ex.Message}");
            }
        }


        private void RoofOverhangCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            _logic.RoofOverhangCheckbox_Checked(sender, e);
        }

        private void RoofOverhangCheckbox_Unchecked(object sender, RoutedEventArgs e)
        {
            _logic.RoofOverhangCheckbox_Unchecked(sender, e);
        }

        private void UpdateLocationButton_Click(object sender, RoutedEventArgs e)
        {
            _logic.UpdateLocationButton_Click(sender, e);
        }

        private async void OpenSetProjectLocationWindowButton_Click(object sender, RoutedEventArgs e)
        {
            await _logic.OpenSetProjectLocationWindowButton_Click(sender, e);
        }

        // Seetter Methods: Event handler for the MaterialField selection changed event
        private void MaterialField_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _logic.MaterialField_SelectionChanged(sender, e);
        }

        private void TreatmentField_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _logic.TreatmentField_SelectionChanged(sender, e);
        }

        private void Toggle3DPreviewButton_Click(object sender, RoutedEventArgs e)
        {
            _logic.Toggle3DPreviewButton_Click(toggle3DPreviewButton, sender, e);
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            _logic.NextButton_Click(sender, e);
        }

        private void PreviousButton_Click(object sender, RoutedEventArgs e)
        {
            _logic.PreviousButton_Click(sender, e);
        }

        public void EstimateButton_Click(object sender, RoutedEventArgs e)
        {
            _logic.EstimateButton_Click(materialField, treatmentField, soilContactField, locationField, exposureField, elementIntersectionField, verticalMemberCheckbox, roofOverhangCheckbox, groundDistTextBox, overhangTextBox, shelterDistTextBox, serviceLifeOutput, sender, e);
        }

        private void ValidateControls()
        {
            if (materialField == null)
                MessageBox.Show("materialField is null");
            if (treatmentField == null)
                MessageBox.Show("treatmentField is null");
            if (soilContactField == null)
                MessageBox.Show("soilContactField is null");
            if (locationField == null)
                MessageBox.Show("locationField is null");
            if (exposureField == null)
                MessageBox.Show("exposureField is null");
            if (elementIntersectionField == null)
                MessageBox.Show("elementIntersectionField is null");
            if (verticalMemberCheckbox == null)
                MessageBox.Show("verticalMemberCheckbox is null");
            if (roofOverhangCheckbox == null)
                MessageBox.Show("roofOverhangCheckbox is null");
            if (groundDistTextBox == null)
                MessageBox.Show("groundDistTextBox is null");
            if (overhangPanel == null)
                MessageBox.Show("overhangPanel is null");
            if (overhangTextBox == null)
                MessageBox.Show("overhangTextBox is null");
            if (shelterDistPanel == null)
                MessageBox.Show("shelterDistPanel is null");
            if (shelterDistTextBox == null)
                MessageBox.Show("shelterDistTextBox is null");
            if (toggle3DPreviewButton == null)
                MessageBox.Show("toggle3DPreviewButton is null");
        }

        public void AutoPopulateButton_Click(object sender, RoutedEventArgs e)
        {
            _logic.AutoPopulateButton_Click(
                soilContactField,
                exposureField,
                elementIntersectionField,
                roofOverhangCheckbox,
                verticalMemberCheckbox,  // Added verticalMemberCheckbox to the call
                groundDistTextBox,
                overhangTextBox,
                shelterDistTextBox,
                sender,
                e
            );
        }

        public void SetMaterialField(string value)
        {
            if (!materialField.Items.Contains(value))
            {
                materialField.Items.Add(value);
            }
            materialField.SelectedItem = value;
        }

        public void SetTreatmentField(string value)
        {
            if (!treatmentField.Items.Contains(value))
            {
                treatmentField.Items.Add(value);
            }
            treatmentField.SelectedItem = value;
        }

        public void SetSoilContactField(string value)
        {
            soilContactField.SelectedItem = value;
        }

        public void SetLocationField(string value)
        {
            locationField.SelectedItem = value;
        }

        public void SetExposureField(string value)
        {
            exposureField.SelectedItem = value;
        }

        public void SetElementIntersectionField(string value)
        {
            elementIntersectionField.SelectedItem = value;
        }

        public void SetVerticalMemberCheckbox(bool value)
        {
            verticalMemberCheckbox.IsChecked = value;
        }

        public void SetRoofOverhangCheckbox(bool value)
        {
            roofOverhangCheckbox.IsChecked = value;
        }

        public void SetGroundDistance(string value)
        {
            groundDistTextBox.Text = value;
        }

        public void SetOverhangDistance(string value)
        {
            overhangTextBox.Text = value;
        }

        public void SetShelterDistance(string value)
        {
            shelterDistTextBox.Text = value;
        }

        public void SetServiceLifeOutput(string value)
        {
            serviceLifeOutput.Text = value;
        }

        // Getter Methods: Save the user selections to the element
        public string GetMaterialField()
        {
            return materialField.SelectedItem?.ToString();
        }

        public string GetTreatmentField()
        {
            return treatmentField.SelectedItem?.ToString();
        }

        public string GetSoilContactField()
        {
            return soilContactField.SelectedItem?.ToString();
        }

        public string GetLocationField()
        {
            return locationField.SelectedItem?.ToString();
        }

        public string GetExposureField()
        {
            return exposureField.SelectedItem?.ToString();
        }

        public string GetElementIntersectionField()
        {
            return elementIntersectionField.SelectedItem?.ToString();
        }

        public bool IsVerticalMemberChecked()
        {
            return verticalMemberCheckbox.IsChecked ?? false;
        }

        public bool IsRoofOverhangChecked()
        {
            return roofOverhangCheckbox.IsChecked ?? false;
        }

        public string GetGroundDistance()
        {
            return groundDistTextBox.Text;
        }

        public string GetOverhangDistance()
        {
            return overhangTextBox.Text;
        }

        public string GetShelterDistance()
        {
            return shelterDistTextBox.Text;
        }

        public string GetServiceLifeOutput()
        {
            return serviceLifeOutput.Text;
        }


    }

    public class SLE_PopupForm_Logic
    {
        internal Document _doc;
        private UIDocument _uiDoc;
        internal ExternalCommandData _commandData;

        private SLE_PopupForm _form;

        internal string _elementIdAsString;
        internal static Location _currentSelectedLocation;
        internal VisualizationMode currentMode;
        internal int _currentElementIndex = 0;
        internal IList<ElementId> _elementsInDuplicatedView;
        internal ElementId _duplicatedViewId;
        internal List<MaterialData> _materialsData;
        internal List<Location> _locations;

        private System.Windows.Controls.ComboBox materialField;
        private System.Windows.Controls.ComboBox treatmentField;
        private System.Windows.Controls.ComboBox soilContactField;
        private System.Windows.Controls.ComboBox locationField;
        private System.Windows.Controls.ComboBox exposureField;
        private System.Windows.Controls.ComboBox elementIntersectionField;
        private CheckBox verticalMemberCheckbox;
        private CheckBox roofOverhangCheckbox;
        private System.Windows.Controls.TextBox groundDistTextBox;
        private StackPanel overhangPanel;
        private System.Windows.Controls.TextBox overhangTextBox;
        private StackPanel shelterDistPanel;
        private System.Windows.Controls.TextBox shelterDistTextBox;
        private System.Windows.Controls.Button toggle3DPreviewButton;
        private System.Windows.Controls.TextBox serviceLifeOutput;

        private View3D _view3D;

        private ElementId _selectedElementId;

        private ElementId _initialSelectedElementId;
        private ElementId _currentSelectedElementId; // Track the current selected element

        public Document GetDocument()
        {
            return _doc;
        }

        public Element GetSelectedElement()
        {
            if (_uiDoc.Selection.GetElementIds().Count > 0)
            {
                return _doc.GetElement(_uiDoc.Selection.GetElementIds().First());
            }
            return null;
        }

        public void CaptureInitialSelection()
        {
            try
            {
                _initialSelectedElementId = _uiDoc.Selection.GetElementIds().FirstOrDefault();
                _currentSelectedElementId = _initialSelectedElementId;

                treatmentField.Items.Clear();
                materialField.Items.Clear();

                if (_initialSelectedElementId != null)
                {
                    Element selectedElement = _doc.GetElement(_initialSelectedElementId);

                    // Retrieve the user selection data from the element (stored in shared parameters)
                    UserSelectionData savedData = GetUserSelections(selectedElement);

                    if (savedData != null)
                    {
                        // Apply the saved user selections to the UI
                        ApplyUserSelectionsToUI(selectedElement, savedData);
                        //MessageBox.Show("Selections loaded successfully.");
                    }
                    else
                    {
                        //MessageBox.Show("No previously saved data found.");
                    }
                }
                else
                {
                    MessageBox.Show("No element selected or element ID is invalid.");
                }

                DisplayElementDetails(_initialSelectedElementId);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error capturing initial selection: {ex.Message}");
            }
        }



        internal enum VisualizationMode
        {
            AllElements,
            SelectedAndAdjacent,
            SelectedOnly
        }

        public SLE_PopupForm_Logic(
           UIDocument uiDoc,
           ExternalCommandData commandData,
           SLE_PopupForm form,
           System.Windows.Controls.ComboBox materialField,
           System.Windows.Controls.ComboBox treatmentField,
           System.Windows.Controls.ComboBox soilContactField,
           System.Windows.Controls.ComboBox locationField,
           System.Windows.Controls.ComboBox exposureField,
           System.Windows.Controls.ComboBox elementIntersectionField,
           CheckBox verticalMemberCheckbox,
           CheckBox roofOverhangCheckbox,
           System.Windows.Controls.TextBox groundDistTextBox,
           StackPanel overhangPanel,
           System.Windows.Controls.TextBox overhangTextBox,
           StackPanel shelterDistPanel,
           System.Windows.Controls.TextBox shelterDistTextBox,
           System.Windows.Controls.Button toggle3DPreviewButton,
           System.Windows.Controls.TextBox serviceLifeOutput)
        {
            try
            {
                if (uiDoc == null) throw new ArgumentNullException(nameof(uiDoc), "UIDocument is null");
                if (commandData == null) throw new ArgumentNullException(nameof(commandData), "ExternalCommandData is null");
                if (form == null) throw new ArgumentNullException(nameof(form), "Form is null");
                if (materialField == null) throw new ArgumentNullException(nameof(materialField), "materialField is null");
                if (treatmentField == null) throw new ArgumentNullException(nameof(treatmentField), "treatmentField is null");
                if (soilContactField == null) throw new ArgumentNullException(nameof(soilContactField), "soilContactField is null");
                if (locationField == null) throw new ArgumentNullException(nameof(locationField), "locationField is null");
                if (exposureField == null) throw new ArgumentNullException(nameof(exposureField), "exposureField is null");
                if (elementIntersectionField == null) throw new ArgumentNullException(nameof(elementIntersectionField), "elementIntersectionField is null");
                if (verticalMemberCheckbox == null) throw new ArgumentNullException(nameof(verticalMemberCheckbox), "verticalMemberCheckbox is null");
                if (roofOverhangCheckbox == null) throw new ArgumentNullException(nameof(roofOverhangCheckbox), "roofOverhangCheckbox is null");
                if (groundDistTextBox == null) throw new ArgumentNullException(nameof(groundDistTextBox), "groundDistTextBox is null");
                if (overhangPanel == null) throw new ArgumentNullException(nameof(overhangPanel), "overhangPanel is null");
                if (overhangTextBox == null) throw new ArgumentNullException(nameof(overhangTextBox), "overhangTextBox is null");
                if (shelterDistPanel == null) throw new ArgumentNullException(nameof(shelterDistPanel), "shelterDistPanel is null");
                if (shelterDistTextBox == null) throw new ArgumentNullException(nameof(shelterDistTextBox), "shelterDistTextBox is null");
                if (toggle3DPreviewButton == null) throw new ArgumentNullException(nameof(toggle3DPreviewButton), "toggle3DPreviewButton is null");
                if (serviceLifeOutput == null) throw new ArgumentNullException(nameof(serviceLifeOutput), "serviceLifeOutput is null");

                _doc = uiDoc.Document ?? throw new ArgumentNullException(nameof(uiDoc.Document), "Document is null");
                _uiDoc = uiDoc ?? throw new ArgumentNullException(nameof(uiDoc), "UIDocument is null");
                _commandData = commandData ?? throw new ArgumentNullException(nameof(commandData), "ExternalCommandData is null");
                _form = form ?? throw new ArgumentNullException(nameof(form), "Form is null");

                this.materialField = materialField ?? throw new ArgumentNullException(nameof(materialField), "materialField is null");
                this.treatmentField = treatmentField ?? throw new ArgumentNullException(nameof(treatmentField), "treatmentField is null");
                this.soilContactField = soilContactField ?? throw new ArgumentNullException(nameof(soilContactField), "soilContactField is null");
                this.locationField = locationField ?? throw new ArgumentNullException(nameof(locationField), "locationField is null");
                this.exposureField = exposureField ?? throw new ArgumentNullException(nameof(exposureField), "exposureField is null");
                this.elementIntersectionField = elementIntersectionField ?? throw new ArgumentNullException(nameof(elementIntersectionField), "elementIntersectionField is null");
                this.verticalMemberCheckbox = verticalMemberCheckbox ?? throw new ArgumentNullException(nameof(verticalMemberCheckbox), "verticalMemberCheckbox is null");
                this.roofOverhangCheckbox = roofOverhangCheckbox ?? throw new ArgumentNullException(nameof(roofOverhangCheckbox), "roofOverhangCheckbox is null");
                this.groundDistTextBox = groundDistTextBox ?? throw new ArgumentNullException(nameof(groundDistTextBox), "groundDistTextBox is null");
                this.overhangPanel = overhangPanel ?? throw new ArgumentNullException(nameof(overhangPanel), "overhangPanel is null");
                this.overhangTextBox = overhangTextBox ?? throw new ArgumentNullException(nameof(overhangTextBox), "overhangTextBox is null");
                this.shelterDistPanel = shelterDistPanel ?? throw new ArgumentNullException(nameof(shelterDistPanel), "shelterDistPanel is null");
                this.shelterDistTextBox = shelterDistTextBox ?? throw new ArgumentNullException(nameof(shelterDistTextBox), "shelterDistTextBox is null");
                this.toggle3DPreviewButton = toggle3DPreviewButton ?? throw new ArgumentNullException(nameof(toggle3DPreviewButton), "toggle3DPreviewButton is null");
                this.serviceLifeOutput = serviceLifeOutput ?? throw new ArgumentNullException(nameof(serviceLifeOutput), "serviceLifeOutput is null");


                _form.Closed += (s, e) => DeleteTemporary3DView();

                _materialsData = MaterialImportUtility.GetAllMaterials() ?? new List<MaterialData>();

                _elementIdAsString = uiDoc.Selection.GetElementIds().Count > 0 ? uiDoc.Selection.GetElementIds().First().ToString() : "";

                _view3D = GetActive3DViewAndDuplicate(_doc);
                if (_view3D != null)
                {
                    InitializeElementsInDuplicatedView();
                }
                else
                {
                    MessageBox.Show("No suitable 3D view found.");
                    return;
                }

                ElementId selectedElementId = new ElementId(Convert.ToInt32(_elementIdAsString));
                IList<ElementId> adjacentElementIds = FindAdjacentElements(_doc, selectedElementId);
                Modify3DView(_doc, _view3D, adjacentElementIds, selectedElementId, currentMode);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during SLE_PopupForm_Logic initialization: {ex.Message}\n{ex.StackTrace}");
            }
        }



        private void DeleteTemporary3DView()
        {
            if (_duplicatedViewId != null)
            {
                using (Transaction tx = new Transaction(_doc, "Delete Temporary 3D View"))
                {
                    tx.Start();
                    _doc.Delete(_duplicatedViewId);
                    tx.Commit();
                }
            }
        }

        public void DisableIrrelevantControls()
        {
            verticalMemberCheckbox.IsEnabled = false;
            roofOverhangCheckbox.IsEnabled = false;
            groundDistTextBox.IsEnabled = false;
            overhangTextBox.IsEnabled = false;
            shelterDistTextBox.IsEnabled = false;

            // Clear and disable Exposure Field
            exposureField.Items.Clear(); // Remove all items
            exposureField.SelectedItem = null; // Clear selection
            exposureField.IsEnabled = false; // Disable interaction

            // Clear and disable Element Intersection Field
            elementIntersectionField.Items.Clear(); // Remove all items
            elementIntersectionField.SelectedItem = null; // Clear selection
            elementIntersectionField.IsEnabled = false; // Disable interaction
        }


        public void EnableRelevantControls()
        {
            verticalMemberCheckbox.IsEnabled = true;
            roofOverhangCheckbox.IsEnabled = true;
            groundDistTextBox.IsEnabled = true;
            overhangTextBox.IsEnabled = true;
            shelterDistTextBox.IsEnabled = true;

            exposureField.IsEnabled = true;
            exposureField.Items.Clear();  // Clear existing items
            exposureField.Items.Add("Side grain exposed");
            exposureField.Items.Add("End grain exposed");
            exposureField.SelectedIndex = 0;

            elementIntersectionField.IsEnabled = true;
            elementIntersectionField.Items.Clear();  // Clear existing items
            elementIntersectionField.Items.Add("No contact face or gap size >5 mm free from dirt");
            elementIntersectionField.Items.Add("Partially ventilated contact face free from dirt");
            elementIntersectionField.Items.Add("Direct contact or insufficient ventilation");
            elementIntersectionField.SelectedIndex = 2;
        }



        public void RoofOverhangCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            bool enableControls = soilContactField.SelectedItem as string != "In-Ground";
            groundDistTextBox.IsEnabled = enableControls;
            overhangTextBox.IsEnabled = enableControls;
            shelterDistTextBox.IsEnabled = enableControls;

            if (overhangPanel != null)
            {
                overhangPanel.Visibility = System.Windows.Visibility.Visible;
                shelterDistPanel.Visibility = System.Windows.Visibility.Visible;

                groundDistTextBox.IsEnabled = true;
                overhangTextBox.IsEnabled = true;
                shelterDistTextBox.IsEnabled = true;
            }
        }


        public void RoofOverhangCheckbox_Unchecked(object sender, RoutedEventArgs e)
        {
            overhangPanel.Visibility = System.Windows.Visibility.Collapsed;
            shelterDistPanel.Visibility = System.Windows.Visibility.Collapsed;

            groundDistTextBox.IsEnabled = false;
            overhangTextBox.IsEnabled = false;
            shelterDistTextBox.IsEnabled = false;
        }


        public void UpdateLocationButton_Click(object sender, RoutedEventArgs e)
        {
            SLEUtility.LoadLocation(_form.locationField, _commandData, "Above Ground");
        }


        public async Task OpenSetProjectLocationWindowButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _form.locationField.Items.Clear();

                if (_form.soilContactField.SelectedItem == null)
                {
                    return;
                }

                string selectedCondition = (_form.soilContactField.SelectedItem as ComboBoxItem)?.Content.ToString();

                SetProjectLocationWindow setLocationWindow = new SetProjectLocationWindow(_commandData, selectedCondition);
                var result = setLocationWindow.ShowDialog();

                if (result == true)
                {
                    Location selectedLocation = setLocationWindow.SelectedLocation;

                    SLEUtility.SetLocationField(selectedLocation, _form.locationField);

                    try
                    {
                        string errorMessage;
                        if (!SLEUtility.TrySetProjectLocation(selectedLocation, _commandData, out errorMessage))
                        {
                            MessageBox.Show($"Failed to set project location. Error: {errorMessage}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        else
                        {
                            SLEUtility.SaveLocation(selectedLocation);
                            _currentSelectedLocation = selectedLocation;
                            SLEUtility.DisplayConfirmationDialog(selectedLocation);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"An error occurred while setting the project location: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    MessageBox.Show("Set Project Location window was closed without setting a location.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void MaterialField_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (materialField.SelectedItem != null)
            {
                string selectedMaterialName = materialField.SelectedItem.ToString();
                var filteredMaterials = _materialsData.Where(md => md.Name.Contains(selectedMaterialName)).ToList();
                string materialDetails = BuildMaterialDetailsString(filteredMaterials);
                UpdateTreatmentComboBox(filteredMaterials);
            }
        }

        public string BuildMaterialDetailsString(List<MaterialData> filteredMaterials)
        {
            var sb = new StringBuilder();
            foreach (var material in filteredMaterials)
            {
                sb.AppendLine($"Material Name(s): {string.Join(", ", material.Name)}");
                sb.AppendLine($"Latin Name(s): {string.Join(", ", material.LatinName)}");
                sb.AppendLine($"Treatment(s): {string.Join(", ", material.Treatment)}");
                sb.AppendLine($"Resistance Dose UC3: {material.ResistanceDoseUC3}");
                sb.AppendLine($"Resistance Dose UC4: {material.ResistanceDoseUC4}");
                sb.AppendLine("---");
            }
            return sb.ToString();
        }

        public void UpdateTreatmentComboBox(List<MaterialData> filteredMaterials)
        {
            var treatments = new HashSet<string>();
            foreach (var material in filteredMaterials)
            {
                foreach (var treatment in material.Treatment)
                {
                    treatments.Add(treatment);
                }
            }
            PopulateTreatmentComboBox(treatments);
        }

        public void PopulateTreatmentComboBox(IEnumerable<string> treatments)
        {
            treatmentField.Items.Clear();
            foreach (var treatment in treatments)
            {
                treatmentField.Items.Add(treatment);
            }
            if (treatmentField.Items.Count > 0)
            {
                treatmentField.SelectedIndex = 0;
            }
        }

        public void TreatmentField_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (materialField.SelectedItem != null && treatmentField.SelectedItem != null)
            {
                string selectedMaterialName = materialField.SelectedItem.ToString();
                string selectedTreatment = treatmentField.SelectedItem.ToString();

                // Fetch material data (replace `_materialsData` with your actual materials data source)
                MaterialData materialData = DisplayMaterialData(selectedMaterialName, selectedTreatment, _materialsData);

                if (materialData == null)
                {
                    MessageBox.Show("Error: No data found for the selected material and treatment. Please ensure valid selections.", "Material Data Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }


        public void Toggle3DPreviewButton_Click(System.Windows.Controls.Button toggle3DPreviewButton, object sender, RoutedEventArgs e)
        {
            ElementId firstSelectedId = _uiDoc.Selection.GetElementIds().FirstOrDefault();
            string elementIdAsString = firstSelectedId?.ToString();

            switch (currentMode)
            {
                case VisualizationMode.AllElements:
                    currentMode = VisualizationMode.SelectedOnly;
                    break;

                case VisualizationMode.SelectedAndAdjacent:
                    currentMode = VisualizationMode.AllElements;
                    break;

                case VisualizationMode.SelectedOnly:
                    currentMode = VisualizationMode.SelectedAndAdjacent;
                    break;
            }

            ElementId selectedElementId = new ElementId(Convert.ToInt32(elementIdAsString));
            IList<ElementId> adjacentElementIds = FindAdjacentElements(_doc, selectedElementId);
            Modify3DView(_doc, _view3D, adjacentElementIds, selectedElementId, currentMode);

            switch (currentMode)
            {
                case VisualizationMode.AllElements:
                    toggle3DPreviewButton.Content = "Show Selected Only";
                    break;
                case VisualizationMode.SelectedOnly:
                    toggle3DPreviewButton.Content = "Show Selected and Adjacent";
                    break;
                case VisualizationMode.SelectedAndAdjacent:
                    toggle3DPreviewButton.Content = "Show All Elements";
                    break;
            }
        }


        public void NextButton_Click(object sender, RoutedEventArgs e)
        {
            while (_currentElementIndex < _elementsInDuplicatedView.Count - 1)
            {
                _currentElementIndex++;
                if (IsElementValid(_elementsInDuplicatedView[_currentElementIndex]))
                {
                    _currentSelectedElementId = _elementsInDuplicatedView[_currentElementIndex];
                    SelectElement(_currentSelectedElementId);

                    // New code: Update the UI based on the new element
                    Element selectedElement = _doc.GetElement(_currentSelectedElementId);
                    if (selectedElement != null)
                    {
                        UserSelectionData savedData = GetUserSelections(selectedElement);
                        ApplyUserSelectionsToUI(selectedElement, savedData);
                    }

                    DisplayElementDetails(_currentSelectedElementId);
                    return;
                }
            }
            MessageBox.Show("This is the last element.", "Navigation", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public void PreviousButton_Click(object sender, RoutedEventArgs e)
        {
            while (_currentElementIndex > 0)
            {
                _currentElementIndex--;
                if (IsElementValid(_elementsInDuplicatedView[_currentElementIndex]))
                {
                    _currentSelectedElementId = _elementsInDuplicatedView[_currentElementIndex];
                    SelectElement(_currentSelectedElementId);

                    // New code: Update the UI based on the new element
                    Element selectedElement = _doc.GetElement(_currentSelectedElementId);
                    if (selectedElement != null)
                    {
                        UserSelectionData savedData = GetUserSelections(selectedElement);
                        ApplyUserSelectionsToUI(selectedElement, savedData);
                    }

                    DisplayElementDetails(_currentSelectedElementId);
                    return;
                }
            }
            MessageBox.Show("This is the first element.", "Navigation", MessageBoxButton.OK, MessageBoxImage.Information);
        }


        private void DisplayElementDetails(ElementId elementId)
        {
            Element element = _doc.GetElement(elementId);
            if (element != null)
            {
                StringBuilder details = new StringBuilder();
                details.AppendLine($"Element ID: {element.Id}");
                details.AppendLine($"Name: {element.Name}");
                details.AppendLine($"Category: {element.Category?.Name}");
                details.AppendLine($"Type: {element.GetType().Name}");


                //MessageBox.Show(details.ToString(), "Element Details", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }


        public bool IsElementValid(ElementId elementId)
        {
            Element element = _uiDoc.Document.GetElement(elementId);
            if (element == null || element.Category == null)
            {
                return false;
            }

            if (element is Autodesk.Revit.DB.View ||
                element is Level ||
                element.Category.Id.IntegerValue == (int)BuiltInCategory.OST_Materials ||
                element.Category.Id.IntegerValue == (int)BuiltInCategory.OST_Rooms ||
                element.Category.Id.IntegerValue == (int)BuiltInCategory.OST_Topography ||
                element.Category.Id.IntegerValue == (int)BuiltInCategory.OST_Schedules ||
                element.Category.Id.IntegerValue == (int)BuiltInCategory.OST_Tags ||
                element.Category.Id.IntegerValue == (int)BuiltInCategory.OST_DetailComponents ||
                element.Category.Id.IntegerValue == (int)BuiltInCategory.OST_Grids ||
                element.Category.Id.IntegerValue == (int)BuiltInCategory.OST_Levels ||
                element.Category.Id.IntegerValue == (int)BuiltInCategory.OST_TextNotes ||
                element.Category.Id.IntegerValue == (int)BuiltInCategory.OST_Dimensions ||
                element.Category.Id.IntegerValue == (int)BuiltInCategory.OST_Callouts ||
                element.Category.Id.IntegerValue == (int)BuiltInCategory.OST_SectionHeads ||
                element.Category.Id.IntegerValue == (int)BuiltInCategory.OST_ElevationMarks ||
                element.Category.Id.IntegerValue == (int)BuiltInCategory.OST_Sheets ||
                element.Category.Id.IntegerValue == (int)BuiltInCategory.OST_TitleBlocks ||
                element.Category.Id.IntegerValue == (int)BuiltInCategory.OST_FilledRegion)
            {
                return false;
            }

            Options options = new Options();
            GeometryElement geomElement = element.get_Geometry(options);
            if (geomElement != null)
            {
                foreach (GeometryObject geomObj in geomElement)
                {
                    if (geomObj is Solid solid && solid.Volume > 0)
                    {
                        return true;
                    }
                    else if (geomObj is GeometryInstance)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public void SelectElement(ElementId elementId)
        {
            if (_uiDoc != null && elementId != ElementId.InvalidElementId)
            {
                _uiDoc.Selection.SetElementIds(new List<ElementId>());
                _uiDoc.Selection.SetElementIds(new List<ElementId> { elementId });
                ZoomToElement(_uiDoc, elementId);
                _uiDoc.RefreshActiveView();
            }
        }

        public void ZoomToElement(UIDocument uiDocument, ElementId elementId)
        {
            try
            {
                Element element = uiDocument.Document.GetElement(elementId);
                if (element != null)
                {
                    BoundingBoxXYZ boundingBox = element.get_BoundingBox(null);
                    if (boundingBox != null)
                    {
                        Outline outline = new Outline(boundingBox.Min, boundingBox.Max);
                        BoundingBoxXYZ newBox = new BoundingBoxXYZ()
                        {
                            Min = outline.MinimumPoint - new XYZ(5, 5, 5),
                            Max = outline.MaximumPoint + new XYZ(5, 5, 5)
                        };

                        View3D view3D = uiDocument.Document.ActiveView as View3D;
                        if (view3D != null)
                        {
                            using (Transaction trans = new Transaction(uiDocument.Document, "Zoom to Element"))
                            {
                                trans.Start();
                                view3D.SetSectionBox(newBox);
                                trans.Commit();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Error", $"Failed to zoom to element: {ex.Message}");
            }
        }

        //PlaceHolder for the k1 calculation. in this code k1 is simplified to a factor of 1
        public enum LocalCondition
        {
            OpenField,
            Forest,
            ShelteredByBuildings
        }

        public static MaterialData DisplayMaterialData(string materialName, string treatment, List<MaterialData> materialsData)
        {
            //MessageBox.Show("DisplayMaterialData: Entered method.");
            //MessageBox.Show($"DisplayMaterialData: Material name: {materialName}, Treatment: {treatment}");

            // Search for the material based on name and treatment
            var materialData = materialsData.FirstOrDefault(m => m.Name.Contains(materialName) && m.Treatment.Contains(treatment));

            if (materialData != null)
            {
                string message = $"Material Name(s): {string.Join(", ", materialData.Name)}\n" +
                                 $"Latin Name(s): {string.Join(", ", materialData.LatinName)}\n" +
                                 $"Treatment(s): {string.Join(", ", materialData.Treatment)}\n" +
                                 $"Resistance Dose UC3: {materialData.ResistanceDoseUC3}\n" +
                                 $"Resistance Dose UC4: {materialData.ResistanceDoseUC4}";
                // Uncomment the line below if you want a message box display
                // MessageBox.Show($"DisplayMaterialData: Found material data.\n{message}", "Material Data");
            }
            else
            {
                // Uncomment the line below if you want a message box display
                // MessageBox.Show("DisplayMaterialData: No data found for the selected material and treatment.", "Data Not Found");
            }

            return materialData; // Return the found material data or null if not found
        }


        private View3D GetActive3DViewAndDuplicate(Document doc)
        {
            View3D activeView3D = doc.ActiveView as View3D;
            if (activeView3D == null || activeView3D.IsTemplate || !activeView3D.CanBePrinted)
                return null;

            View3D duplicatedView3D;
            using (Transaction tx = new Transaction(doc, "Duplicate 3D View"))
            {
                tx.Start();
                ElementId duplicatedViewId = activeView3D.Duplicate(ViewDuplicateOption.WithDetailing);
                duplicatedView3D = doc.GetElement(duplicatedViewId) as View3D;
                duplicatedView3D.Name = "Temporary Preview View";
                _duplicatedViewId = duplicatedViewId;
                tx.Commit();
            }

            return duplicatedView3D;
        }



        private void InitializeElementsInDuplicatedView()
        {
            FilteredElementCollector collector = new FilteredElementCollector(_doc, _duplicatedViewId);
            _elementsInDuplicatedView = collector.WhereElementIsNotElementType().ToElementIds().ToList();
            _currentElementIndex = _elementsInDuplicatedView.IndexOf(_uiDoc.Selection.GetElementIds().FirstOrDefault());
        }

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
                    continue;

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

        private void Modify3DView(Document doc, View3D view3D, IList<ElementId> adjacentElementIds, ElementId selectedElementId, VisualizationMode mode)
        {
            TransactionStatus txStatus;

            using (Transaction tx = new Transaction(doc, "Modify 3D View"))
            {
                tx.Start();

                switch (mode)
                {
                    case VisualizationMode.AllElements:
                        view3D.DisableTemporaryViewMode(TemporaryViewMode.TemporaryHideIsolate);

                        foreach (Category category in doc.Settings.Categories)
                        {
                            if (view3D.CanCategoryBeHidden(category.Id))
                            {
                                view3D.SetCategoryHidden(category.Id, false);
                            }
                        }

                        Category levelCategory = doc.Settings.Categories.get_Item(BuiltInCategory.OST_Levels);
                        if (levelCategory != null)
                        {
                            view3D.SetCategoryHidden(levelCategory.Id, true);
                        }
                        break;

                    case VisualizationMode.SelectedAndAdjacent:
                        IList<ElementId> selectedAndAdjacent = new List<ElementId>(adjacentElementIds) { selectedElementId };

                        view3D.DisableTemporaryViewMode(TemporaryViewMode.TemporaryHideIsolate);
                        view3D.IsolateElementsTemporary(selectedAndAdjacent);
                        break;

                    case VisualizationMode.SelectedOnly:
                        view3D.IsolateElementsTemporary(new List<ElementId> { selectedElementId });
                        break;
                }

                txStatus = tx.Commit();
            }
        }

        public bool ValidateFormInputs(System.Windows.Controls.ComboBox materialField, System.Windows.Controls.ComboBox treatmentField, System.Windows.Controls.ComboBox soilContactField, System.Windows.Controls.ComboBox locationField, System.Windows.Controls.ComboBox exposureField, System.Windows.Controls.ComboBox elementIntersectionField, CheckBox verticalMemberCheckbox, CheckBox roofOverhangCheckbox, System.Windows.Controls.TextBox groundDistTextBox, System.Windows.Controls.TextBox overhangTextBox, System.Windows.Controls.TextBox shelterDistTextBox)
        {
            //MessageBox.Show("ValidateFormInputs: Entered method.");

            bool materialAndTreatmentChecks = materialField.SelectedItem != null && treatmentField.SelectedItem != null;
            //MessageBox.Show($"ValidateFormInputs: Material and treatment checks passed: {materialAndTreatmentChecks}");

            if (!materialAndTreatmentChecks)
                return false;

            if (soilContactField.SelectedItem == null)
                return false;

            if (soilContactField.SelectedItem.ToString() == "Above Ground")
            {
                bool aboveGroundChecks = locationField.SelectedItem != null &&
                                         exposureField.SelectedItem != null &&
                                         elementIntersectionField.SelectedItem != null &&
                                         verticalMemberCheckbox.IsChecked.HasValue &&
                                         roofOverhangCheckbox.IsChecked.HasValue;

                //MessageBox.Show($"ValidateFormInputs: Above ground checks passed: {aboveGroundChecks}");

                if (roofOverhangCheckbox.IsChecked == true)
                {
                    aboveGroundChecks = aboveGroundChecks &&
                                        !string.IsNullOrWhiteSpace(groundDistTextBox.Text) &&
                                        !string.IsNullOrWhiteSpace(overhangTextBox.Text) &&
                                        !string.IsNullOrWhiteSpace(shelterDistTextBox.Text);
                    //MessageBox.Show($"ValidateFormInputs: Above ground checks with overhang passed: {aboveGroundChecks}");
                }

                return aboveGroundChecks;
            }

            //MessageBox.Show("ValidateFormInputs: Validation successful.");
            return true;
        }

        //public void EstimateButton_Click(
        //System.Windows.Controls.ComboBox materialField,
        //System.Windows.Controls.ComboBox treatmentField,
        //System.Windows.Controls.ComboBox soilContactField,
        //System.Windows.Controls.ComboBox locationField,
        //System.Windows.Controls.ComboBox exposureField,
        //System.Windows.Controls.ComboBox elementIntersectionField,
        //CheckBox verticalMemberCheckbox,
        //CheckBox roofOverhangCheckbox,
        //System.Windows.Controls.TextBox groundDistTextBox,
        //System.Windows.Controls.TextBox overhangTextBox,
        //System.Windows.Controls.TextBox shelterDistTextBox,
        //System.Windows.Controls.TextBox serviceLifeOutput,
        //object sender,
        //RoutedEventArgs e)
        //{
        //    try
        //    {
        //        // Helper function to get string from ComboBox
        //        string GetComboBoxValue(System.Windows.Controls.ComboBox comboBox)
        //        {
        //            if (comboBox.SelectedItem is ComboBoxItem comboBoxItem)
        //            {
        //                return comboBoxItem.Content.ToString(); // Get the actual content
        //            }
        //            return comboBox.SelectedItem?.ToString(); // Directly return if it's a string
        //        }

        //        // Validate required fields
        //        string material = GetComboBoxValue(materialField);
        //        string treatment = GetComboBoxValue(treatmentField);
        //        string soilContact = GetComboBoxValue(soilContactField);

        //        if (string.IsNullOrEmpty(material))
        //        {
        //            MessageBox.Show("Error: Material field is required. Please select a material.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
        //            return;
        //        }
        //        if (string.IsNullOrEmpty(treatment))
        //        {
        //            MessageBox.Show("Error: Treatment field is required. Please select a treatment.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
        //            return;
        //        }
        //        if (string.IsNullOrEmpty(soilContact))
        //        {
        //            MessageBox.Show("Error: Soil contact field is required. Please select a soil contact condition.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
        //            return;
        //        }

        //        bool isInGround = soilContact.Equals("In-Ground", StringComparison.OrdinalIgnoreCase);

        //        if (!isInGround)
        //        {
        //            string location = GetComboBoxValue(locationField);
        //            string exposure = GetComboBoxValue(exposureField);
        //            string elementIntersection = GetComboBoxValue(elementIntersectionField);

        //            if (string.IsNullOrEmpty(location))
        //            {
        //                MessageBox.Show("Location field is not selected.");
        //                return;
        //            }
        //            if (string.IsNullOrEmpty(exposure))
        //            {
        //                MessageBox.Show("Exposure field is not selected.");
        //                return;
        //            }
        //            if (string.IsNullOrEmpty(elementIntersection))
        //            {
        //                MessageBox.Show("Element intersection field is not selected.");
        //                return;
        //            }
        //        }

        //        if (!ValidateFormInputs(materialField, treatmentField, soilContactField, locationField, exposureField, elementIntersectionField, verticalMemberCheckbox, roofOverhangCheckbox, groundDistTextBox, overhangTextBox, shelterDistTextBox))
        //        {
        //            MessageBox.Show("Validation failed. Please provide all required inputs.", "Input Error", MessageBoxButton.OK, MessageBoxImage.Warning);
        //            return;
        //        }

        //        // Fetch material data
        //        MaterialData materialData = DisplayMaterialData(material, treatment, _materialsData);
        //        if (materialData == null)
        //        {
        //            MessageBox.Show("Error: No data found for the selected material and treatment. Please ensure valid selections.", "Material Data Error", MessageBoxButton.OK, MessageBoxImage.Warning);
        //            return;
        //        }

        //        // Handle location details
        //        SLEUtility.LocationDetails locationDetails;

        //        if (_currentSelectedLocation != null)
        //        {
        //            locationDetails = SLEUtility.DisplayConfirmationDialog(_currentSelectedLocation);
        //        }
        //        else
        //        {
        //            if (locationField.SelectedItem is LocationWrapper selectedLocation)
        //            {
        //                _currentSelectedLocation = new Location
        //                {
        //                    City = selectedLocation.City,
        //                    Country = selectedLocation.Country,
        //                    Lat = selectedLocation.Lat ?? 0,
        //                    Lon = selectedLocation.Lon ?? 0,
        //                    KTrap1 = selectedLocation.KTrap1,
        //                    KTrap2 = selectedLocation.KTrap2,
        //                    KTrap3 = selectedLocation.KTrap3,
        //                    KTrap4 = selectedLocation.KTrap4,
        //                    KTrap5 = selectedLocation.KTrap5,
        //                    DRef = selectedLocation.DRef,
        //                    DShelt = selectedLocation.DShelt,
        //                    WDRRatio = selectedLocation.WDRRatio,
        //                    WDRRatioH = selectedLocation.WDRRatioH
        //                };

        //                locationDetails = SLEUtility.DisplayConfirmationDialog(_currentSelectedLocation);
        //            }
        //            else
        //            {
        //                MessageBox.Show("Location data not selected.", "Location Error", MessageBoxButton.OK, MessageBoxImage.Warning);
        //                return;
        //            }
        //        }

        //        if (isInGround)
        //        {
        //            // In-Ground calculation
        //            double serviceLifeInYears = CalculateInGroundServiceLife(materialData, locationDetails.Doses ?? 0);
        //            serviceLifeOutput.Text = $"{serviceLifeInYears}";
        //        }
        //        else
        //        {
        //            // Above-Ground calculation
        //            double k1 = 1.0;
        //            double k2 = RetrieveAndStoreSelections(exposureField, elementIntersectionField);
        //            double k3 = CalculateShelterAdjustmentFactor(locationDetails, k2, verticalMemberCheckbox, roofOverhangCheckbox, groundDistTextBox, overhangTextBox, shelterDistTextBox);
        //            double serviceLifeInYears = CalculateAboveGroundServiceLife(
        //                materialData,
        //                double.Parse(locationDetails.DRef, CultureInfo.InvariantCulture),
        //                k2,
        //                k3);
        //            serviceLifeOutput.Text = $"{serviceLifeInYears}";
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show($"An error occurred: {ex.Message}", "Calculation Error", MessageBoxButton.OK, MessageBoxImage.Error);
        //    }
        //}

        public void EstimateButton_Click(
    System.Windows.Controls.ComboBox materialField,
    System.Windows.Controls.ComboBox treatmentField,
    System.Windows.Controls.ComboBox soilContactField,
    System.Windows.Controls.ComboBox locationField,
    System.Windows.Controls.ComboBox exposureField,
    System.Windows.Controls.ComboBox elementIntersectionField,
    CheckBox verticalMemberCheckbox,
    CheckBox roofOverhangCheckbox,
    System.Windows.Controls.TextBox groundDistTextBox,
    System.Windows.Controls.TextBox overhangTextBox,
    System.Windows.Controls.TextBox shelterDistTextBox,
    System.Windows.Controls.TextBox serviceLifeOutput,
    object sender,
    RoutedEventArgs e)
        {
            try
            {
                // Helper function to extract ComboBox values
                string GetComboBoxValue(System.Windows.Controls.ComboBox comboBox)
                {
                    if (comboBox.SelectedItem is ComboBoxItem comboBoxItem)
                    {
                        return comboBoxItem.Content.ToString();
                    }
                    return comboBox.SelectedItem?.ToString();
                }

                // Validate required ComboBox fields
                string material = GetComboBoxValue(materialField);
                string treatment = GetComboBoxValue(treatmentField);
                string soilContact = GetComboBoxValue(soilContactField);

                if (string.IsNullOrEmpty(material) || string.IsNullOrEmpty(treatment) || string.IsNullOrEmpty(soilContact))
                {
                    MessageBox.Show("Error: Please ensure all required fields (Material, Treatment, Soil Contact) are selected.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Determine if the element is in-ground
                bool isInGround = soilContact.Equals("In-Ground", StringComparison.OrdinalIgnoreCase);

                // Fetch material data
                MaterialData materialData = DisplayMaterialData(material, treatment, _materialsData);
                if (materialData == null)
                {
                    MessageBox.Show("Error: No data found for the selected material and treatment. Please ensure valid selections.", "Material Data Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Handle location details
                SLEUtility.LocationDetails locationDetails = null;
                if (_currentSelectedLocation != null)
                {
                    locationDetails = SLEUtility.DisplayConfirmationDialog(_currentSelectedLocation);
                }
                else if (locationField.SelectedItem is LocationWrapper selectedLocation)
                {
                    _currentSelectedLocation = new Location
                    {
                        City = selectedLocation.City,
                        Country = selectedLocation.Country,
                        Lat = selectedLocation.Lat ?? 0,
                        Lon = selectedLocation.Lon ?? 0,
                        KTrap1 = selectedLocation.KTrap1,
                        KTrap2 = selectedLocation.KTrap2,
                        KTrap3 = selectedLocation.KTrap3,
                        KTrap4 = selectedLocation.KTrap4,
                        KTrap5 = selectedLocation.KTrap5,
                        DRef = selectedLocation.DRef,
                        DShelt = selectedLocation.DShelt,
                        WDRRatio = selectedLocation.WDRRatio,
                        WDRRatioH = selectedLocation.WDRRatioH
                    };

                    locationDetails = SLEUtility.DisplayConfirmationDialog(_currentSelectedLocation);
                }
                else
                {
                    MessageBox.Show("Error: Location data is not selected.", "Location Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // If in-ground, calculate and display service life
                if (isInGround)
                {
                    double serviceLifeInYears = CalculateInGroundServiceLife(materialData, locationDetails.Doses ?? 0);
                    serviceLifeOutput.Text = $"{serviceLifeInYears}";
                }
                else
                {
                    // Extract other inputs
                    string exposure = GetComboBoxValue(exposureField);
                    string elementIntersection = GetComboBoxValue(elementIntersectionField);

                    if (string.IsNullOrEmpty(exposure) || string.IsNullOrEmpty(elementIntersection))
                    {
                        MessageBox.Show("Error: Please ensure Exposure and Element Intersection fields are selected.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    bool isVertical = verticalMemberCheckbox.IsChecked ?? false;
                    bool hasOverhang = roofOverhangCheckbox.IsChecked ?? false;

                    double.TryParse(groundDistTextBox.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double groundDist);
                    double.TryParse(overhangTextBox.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double overhangLength);
                    double.TryParse(shelterDistTextBox.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double shelterDist);

                    if (shelterDist == 0)
                    {
                        MessageBox.Show("Error: Shelter distance cannot be zero. Please provide a valid shelter distance.", "Input Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // Calculate k2 and k3 factors
                    double k2 = RetrieveAndStoreSelections(exposure, elementIntersection, locationDetails);
                    double k3 = CalculateShelterAdjustmentFactor(locationDetails, k2, isVertical, hasOverhang, groundDist, overhangLength, shelterDist);

                    // Calculate above-ground service life
                    double serviceLifeInYears = CalculateAboveGroundServiceLife(
                        materialData,
                        double.Parse(locationDetails.DRef, CultureInfo.InvariantCulture),
                        k2,
                        k3);

                    // Display service life
                    serviceLifeOutput.Text = $"{serviceLifeInYears}";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}", "Calculation Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        public void SaveButton_Click(System.Windows.Controls.TextBox serviceLifeOutput, object sender, RoutedEventArgs e)
        {
            try
            {
                if (_currentSelectedElementId == null)
                {
                    MessageBox.Show("No element selected.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Parse the service life duration from the TextBox
                if (!double.TryParse(serviceLifeOutput.Text, out double serviceLifeDuration))
                {
                    MessageBox.Show("Invalid service life duration value.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Start a transaction
                using (Transaction trans = new Transaction(_doc, "Save User Selections and Service Life Duration"))
                {
                    trans.Start();

                    // Ensure shared parameters are set up in the project
                    UIApplication uiApp = new UIApplication(_doc.Application);
                    string sharedParametersPath = SharedParameterUtility.EnsureSharedParameters(uiApp);
                    if (string.IsNullOrEmpty(sharedParametersPath))
                    {
                        MessageBox.Show("Unable to configure shared parameters.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        trans.RollBack();
                        return;
                    }

                    Element elementToSetParam = _doc.GetElement(_currentSelectedElementId);

                    // Retrieve or create the shared parameter for service life duration
                    Parameter serviceLifeParameter = elementToSetParam.LookupParameter("Element_Service Life Duration");
                    if (serviceLifeParameter == null)
                    {
                        Definition serviceLifeParameterDefinition = SharedParameterUtility.FindOrCreateSharedParameter(
                            uiApp,
                            "Element_Service Life Duration",
                            SpecTypeId.Number,
                            BuiltInParameterGroup.PG_DATA);

                        serviceLifeParameter = elementToSetParam.get_Parameter(serviceLifeParameterDefinition);
                    }

                    if (serviceLifeParameter != null && !serviceLifeParameter.IsReadOnly)
                    {
                        serviceLifeParameter.Set(serviceLifeDuration);

                        // Verify the value after setting it
                        double setValue = serviceLifeParameter.AsDouble();
                    }
                    else
                    {
                        MessageBox.Show("Parameter not found or not settable on the selected element.", "Parameter Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        trans.RollBack();
                        return;
                    }

                    // Save user selections to the shared parameter
                    var data = new UserSelectionData
                    {
                        Material = materialField.SelectedItem?.ToString(),
                        Treatment = treatmentField.SelectedItem?.ToString(),
                        //SoilContact = soilContactField.SelectedItem?.ToString(),
                        SoilContact = (soilContactField.SelectedItem as ComboBoxItem)?.Content.ToString(),
                        Location = locationField.SelectedItem?.ToString(),
                        //Exposure = exposureField.SelectedItem?.ToString(),
                        Exposure = (exposureField.SelectedItem as ComboBoxItem)?.Content.ToString(),
                        //ElementIntersection = elementIntersectionField.SelectedItem?.ToString(),
                        ElementIntersection = (elementIntersectionField.SelectedItem as ComboBoxItem)?.Content.ToString(),
                        IsVerticalMember = verticalMemberCheckbox.IsChecked ?? false,
                        HasRoofOverhang = roofOverhangCheckbox.IsChecked ?? false,
                        GroundDistance = groundDistTextBox.Text,
                        OverhangDistance = overhangTextBox.Text,
                        ShelterDistance = shelterDistTextBox.Text,
                        ServiceLife = serviceLifeDuration,

                        // Save the current selected location details
                        SelectedLocationCity = _currentSelectedLocation?.City,
                        SelectedLocationCountry = _currentSelectedLocation?.Country,
                        SelectedLocationLatitude = _currentSelectedLocation?.Lat ?? 0,
                        SelectedLocationLongitude = _currentSelectedLocation?.Lon ?? 0,

                        // Extract and save additional parameters
                        KTrap1 = _currentSelectedLocation?.KTrap1,
                        KTrap2 = _currentSelectedLocation?.KTrap2,
                        KTrap3 = _currentSelectedLocation?.KTrap3,
                        KTrap4 = _currentSelectedLocation?.KTrap4,
                        KTrap5 = _currentSelectedLocation?.KTrap5,
                        DRef = _currentSelectedLocation?.DRef,
                        DShelt = _currentSelectedLocation?.DShelt,
                        WDRRatio = _currentSelectedLocation?.WDRRatio,
                        WDRRatioH = _currentSelectedLocation?.WDRRatioH
                    };

                    SaveUserSelections(elementToSetParam, data);

                    // Update the JSON file
                    //UpdateLocationJsonFile(_currentSelectedLocation);

                    trans.Commit();

                    // Optionally refresh the UI with saved data
                    // ApplyUserSelectionsToUI(data);

                    MessageBox.Show("User selections and service life duration saved.", "Saved", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}", "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateLocationJsonFile(Location location)
        {
            try
            {
                if (location == null)
                {
                    MessageBox.Show("No location data to save.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RevitProjectLocation.json");

                // Serialize the location to JSON
                string json = JsonConvert.SerializeObject(location, Formatting.Indented);

                // Write the JSON to the file
                File.WriteAllText(filePath, json);

                MessageBox.Show("Location data saved to JSON file successfully.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save location data to JSON file: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public static double RetrieveAndStoreSelections(string exposure, string contact, SLEUtility.LocationDetails locationDetails)
        {
            if (string.IsNullOrEmpty(exposure) || string.IsNullOrEmpty(contact))
            {
                MessageBox.Show("Error: Please ensure both exposure and element intersection fields are provided.");
                return double.NaN;
            }

            double k2 = 0.0;

            if (locationDetails != null && !string.IsNullOrEmpty(locationDetails.KTrap1) && !string.IsNullOrEmpty(locationDetails.KTrap5))
            {
                double kTrap1 = double.Parse(locationDetails.KTrap1, CultureInfo.InvariantCulture);
                double kTrap2 = double.Parse(locationDetails.KTrap2, CultureInfo.InvariantCulture);
                double kTrap3 = double.Parse(locationDetails.KTrap3, CultureInfo.InvariantCulture);
                double kTrap4 = double.Parse(locationDetails.KTrap4, CultureInfo.InvariantCulture);
                double kTrap5 = double.Parse(locationDetails.KTrap5, CultureInfo.InvariantCulture);

                if (exposure == "Side grain exposed")
                {
                    switch (contact)
                    {
                        case "No contact face or gap size >5 mm free from dirt":
                            k2 = 1.0;
                            break;
                        case "Partially ventilated contact face free from dirt":
                            k2 = kTrap1;
                            break;
                        case "Direct contact or insufficient ventilation":
                            k2 = kTrap2;
                            break;
                    }
                }
                else if (exposure == "End grain exposed")
                {
                    switch (contact)
                    {
                        case "No contact face or gap size >5 mm free from dirt":
                            k2 = kTrap5;
                            break;
                        case "Partially ventilated contact face free from dirt":
                            k2 = kTrap3;
                            break;
                        case "Direct contact or insufficient ventilation":
                            k2 = kTrap4;
                            break;
                    }
                }
            }

            if (k2 == 0.0)
            {
                MessageBox.Show("Error: k2 factor calculation failed. Please review your selections.");
                return double.NaN;
            }

            return k2;
        }


        public static double CalculateShelterAdjustmentFactor(
            SLEUtility.LocationDetails locationDetails,
            double k2,
            bool isVertical,
            bool hasOverhang,
            double groundDist,
            double overhangLength,
            double shelterDist)
        {
            if (double.IsNaN(k2))
            {
                MessageBox.Show("Error: Failed to calculate k2 factor. Ensure valid selections for exposure and element intersection.", "Calculation Error");
                return double.NaN;
            }

            if (shelterDist == 0)
            {
                MessageBox.Show("Error: Shelter distance cannot be zero. Please provide a valid shelter distance.");
                return double.NaN;
            }

            double D_ref = double.Parse(locationDetails.DRef ?? "0", CultureInfo.InvariantCulture);
            double D_shelt = double.Parse(locationDetails.DShelt ?? "0", CultureInfo.InvariantCulture);
            double WDR_ratio = double.Parse(locationDetails.WDRRatio ?? "0", CultureInfo.InvariantCulture);
            double WDR_ratio_h = double.Parse(locationDetails.WDRRatioH ?? "0", CultureInfo.InvariantCulture);

            double exposedDose = D_ref * k2;
            double shelteredDose = D_shelt;
            double rainDeltaDose = exposedDose - shelteredDose;
            double reducedDose;

            if (isVertical && hasOverhang)
            {
                double k_shelter = Math.Max(1 - overhangLength / shelterDist, 0);
                reducedDose = shelteredDose + (WDR_ratio * k_shelter) * rainDeltaDose;
            }
            else if (isVertical)
            {
                reducedDose = shelteredDose + WDR_ratio * rainDeltaDose;
            }
            else if (hasOverhang)
            {
                double k_shelter = Math.Max(1 - overhangLength / shelterDist, 0);
                reducedDose = shelteredDose + (WDR_ratio_h * rainDeltaDose * k_shelter);
            }
            else
            {
                reducedDose = exposedDose;
            }

            double k3 = reducedDose / exposedDose;
            return k3;
        }


        public static double CalculateServiceLifeDuration(
                SLEUtility.LocationDetails locationDetails,
                MaterialData materialData,
                System.Windows.Controls.ComboBox soilContactField,
                System.Windows.Controls.ComboBox exposureField,
                System.Windows.Controls.ComboBox elementIntersectionField,
                CheckBox verticalMemberCheckbox,
                CheckBox roofOverhangCheckbox,
                System.Windows.Controls.TextBox groundDistTextBox,
                System.Windows.Controls.TextBox overhangTextBox,
                System.Windows.Controls.TextBox shelterDistTextBox)
            {
            try
            {
                // Helper function to get string from ComboBox
                string GetComboBoxValue(System.Windows.Controls.ComboBox comboBox)
                {
                    if (comboBox.SelectedItem is ComboBoxItem comboBoxItem)
                    {
                        return comboBoxItem.Content.ToString();
                    }
                    return comboBox.SelectedItem?.ToString();
                }

                // Extract soil contact value
                string soilContact = GetComboBoxValue(soilContactField);
                bool isInGround = soilContact.Equals("In-Ground", StringComparison.OrdinalIgnoreCase);

                if (isInGround)
                {
                    // Call the In-Ground utility method
                    return CalculateInGroundServiceLife(materialData, locationDetails.Doses ?? 0);
                }
                else
                {
                    // Extract other necessary values
                    string exposure = GetComboBoxValue(exposureField);
                    string elementIntersection = GetComboBoxValue(elementIntersectionField);

                    if (string.IsNullOrEmpty(exposure) || string.IsNullOrEmpty(elementIntersection))
                    {
                        MessageBox.Show("Error: Please ensure Exposure and Element Intersection fields are selected.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return double.NaN;
                    }

                    bool isVertical = verticalMemberCheckbox.IsChecked ?? false;
                    bool hasOverhang = roofOverhangCheckbox.IsChecked ?? false;

                    double.TryParse(groundDistTextBox.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double groundDist);
                    double.TryParse(overhangTextBox.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double overhangLength);
                    double.TryParse(shelterDistTextBox.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out double shelterDist);

                    if (shelterDist == 0)
                    {
                        MessageBox.Show("Error: Shelter distance cannot be zero. Please provide a valid shelter distance.", "Input Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return double.NaN;
                    }

                    // Retrieve k2 and k3 factors
                    double k2 = RetrieveAndStoreSelections(exposure, elementIntersection, locationDetails);
                    double k3 = CalculateShelterAdjustmentFactor(locationDetails, k2, isVertical, hasOverhang, groundDist, overhangLength, shelterDist);

                    // Call the Above-Ground utility method
                    return CalculateAboveGroundServiceLife(
                        materialData,
                        double.Parse(locationDetails.DRef, CultureInfo.InvariantCulture),
                        k2,
                        k3);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An unexpected error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return double.NaN;
            }
        }


        public static double CalculateInGroundServiceLife(MaterialData materialData, double doses)
        {
            if (doses <= 0)
            {
                MessageBox.Show("Error: Doses value is missing or zero for 'In-Ground' calculation.", "Input Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return double.NaN;
            }

            if (materialData?.ResistanceDoseUC4 <= 0)
            {
                MessageBox.Show("Error: Resistance dose UC4 is missing or zero for the selected material.", "Input Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return double.NaN;
            }

            double D_res = materialData.ResistanceDoseUC4;
            double DR_rel_raw = D_res / 325.0;
            double daily_dose_reference = 21.2;
            double serviceLifeInYears_raw = (DR_rel_raw * 86.4) / doses;

            // Debugging message
            string parameters = $"D_res: {D_res}\n" +
                                $"DR_rel_raw: {DR_rel_raw}\n" +
                                $"Daily Dose Reference: {daily_dose_reference}\n" +
                                $"Service Life (raw): {serviceLifeInYears_raw}";
            //MessageBox.Show(parameters, "In-Ground Service Life Debug", MessageBoxButton.OK, MessageBoxImage.Information);

            return Math.Round(serviceLifeInYears_raw, 0);
        }


        public static double CalculateAboveGroundServiceLife(
          MaterialData materialData,
          double D_ref_current,
          double k2,
          double k3,
          double referenceDRef = 32.3)
        {
            if (D_ref_current <= 0 || referenceDRef <= 0)
            {
                MessageBox.Show("Error: Reference dose cannot be zero. Check the location data.", "Calculation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return double.NaN;
            }

            if (materialData?.ResistanceDoseUC3 <= 0)
            {
                MessageBox.Show("Error: Resistance dose UC3 is missing or zero for the selected material.", "Input Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return double.NaN;
            }

            double k1 = 1.0; // Default k1 value
            double D_res = materialData.ResistanceDoseUC3;
            double DR_rel_raw = D_res / 325.0;
            const double conversionFactor = 325.0;
            double DE_rel_raw = (D_ref_current / referenceDRef) * k1 * k2 * k3;
            double serviceLifeInDays_raw = (D_res * conversionFactor) / (D_ref_current * k2 * k3);
            double serviceLifeInYears_raw = serviceLifeInDays_raw / 365.25;

            // Debugging message
            string parameters = $"D_res: {D_res}\n" +
                                $"DR_rel_raw: {DR_rel_raw}\n" +
                                $"k1: {k1}\n" +
                                $"k2: {k2}\n" +
                                $"k3: {k3}\n" +
                                $"DE_rel_raw: {DE_rel_raw}\n" +
                                $"Service Life (raw): {serviceLifeInYears_raw}";
            //MessageBox.Show(parameters, "Above-Ground Service Life Debug", MessageBoxButton.OK, MessageBoxImage.Information);

            return Math.Round(serviceLifeInYears_raw, 0);
        }



        public class SetParameterExternalEventHandler : IExternalEventHandler
        {
            private readonly Definition _parameterDefinition;
            private readonly Element _element;
            private readonly int _value;

            public SetParameterExternalEventHandler(Definition parameterDefinition, Element element, int value)
            {
                _parameterDefinition = parameterDefinition;
                _element = element;
                _value = value;
            }

            public void Execute(UIApplication app)
            {
                try
                {
                    UIDocument uidoc = app.ActiveUIDocument;
                    Document doc = uidoc.Document;

                    using (Transaction trans = new Transaction(doc, "Set Service Life Duration"))
                    {
                        trans.Start();

                        Parameter parameter = _element.get_Parameter(_parameterDefinition);
                        if (parameter != null && !parameter.IsReadOnly)
                        {
                            parameter.Set(_value);
                        }
                        else
                        {
                            TaskDialog.Show("Parameter Error", "Parameter not found or is read-only and cannot be set on the selected element.");
                            trans.RollBack();
                            return;
                        }

                        trans.Commit();
                    }

                    TaskDialog.Show("Saved", $"Service life duration of {_value} years has been saved to the element with ID {_element.Id}.");
                }
                catch (Exception ex)
                {
                    TaskDialog.Show("Error", $"Failed to set service life duration: {ex.Message}");
                }
            }

            public string GetName()
            {
                return "Set Parameter External Event";
            }
        }

        public void SaveUserSelections(Element selectedElement, UserSelectionData data)
        {
            UIApplication uiApp = new UIApplication(_doc.Application);

            try
            {
                // Inform the user that saving is starting
                string message = "Saving the following user selections:\n";

                // Save Material parameter
                Parameter materialParam = selectedElement.LookupParameter("Element_Material");
                if (materialParam == null)
                {
                    Definition materialParamDefinition = SharedParameterUtility.FindOrCreateSharedParameter(
                        uiApp, "Element_Material", SpecTypeId.String.Text, BuiltInParameterGroup.PG_DATA);
                    materialParam = selectedElement.LookupParameter("Element_Material");
                }
                if (materialParam != null && !materialParam.IsReadOnly)
                {
                    materialParam.Set(data.Material);
                    message += $"- Material: {data.Material}\n";
                }

                // Save Treatment parameter
                Parameter treatmentParam = selectedElement.LookupParameter("Element_Treatment");
                if (treatmentParam == null)
                {
                    Definition treatmentParamDefinition = SharedParameterUtility.FindOrCreateSharedParameter(
                        uiApp, "Element_Treatment", SpecTypeId.String.Text, BuiltInParameterGroup.PG_DATA);
                    treatmentParam = selectedElement.LookupParameter("Element_Treatment");
                }
                if (treatmentParam != null && !treatmentParam.IsReadOnly)
                {
                    treatmentParam.Set(data.Treatment);
                    message += $"- Treatment: {data.Treatment}\n";
                }

                // Save SoilContact parameter
                Parameter soilContactParam = selectedElement.LookupParameter("Element_SoilContact");
                if (soilContactParam == null)
                {
                    Definition soilContactParamDefinition = SharedParameterUtility.FindOrCreateSharedParameter(
                        uiApp, "Element_SoilContact", SpecTypeId.String.Text, BuiltInParameterGroup.PG_DATA);
                    soilContactParam = selectedElement.LookupParameter("Element_SoilContact");
                }
                if (soilContactParam != null && !soilContactParam.IsReadOnly)
                {
                    soilContactParam.Set(data.SoilContact);
                    message += $"- Soil Contact: {data.SoilContact}\n";
                }

                // Save Location parameter
                message += SaveParameter(uiApp, selectedElement, "Element_Location", data.Location);

                // Save Exposure parameter
                message += SaveParameter(uiApp, selectedElement, "Element_Exposure", data.Exposure);

                // Save Element Intersection parameter
                message += SaveParameter(uiApp, selectedElement, "Element_Intersection", data.ElementIntersection);

                // Save Vertical Member parameter (as Yes/No)
                Parameter verticalMemberParam = selectedElement.LookupParameter("Element_IsVerticalMember");
                if (verticalMemberParam == null)
                {
                    Definition verticalMemberParamDefinition = SharedParameterUtility.FindOrCreateSharedParameter(
                        uiApp, "Element_IsVerticalMember", SpecTypeId.Int.Integer, BuiltInParameterGroup.PG_DATA);
                    verticalMemberParam = selectedElement.LookupParameter("Element_IsVerticalMember");
                }
                if (verticalMemberParam != null && !verticalMemberParam.IsReadOnly)
                {
                    verticalMemberParam.Set(data.IsVerticalMember ? 1 : 0);
                    message += $"- Vertical Member: {(data.IsVerticalMember ? "True" : "False")}\n";
                }

                // Save Roof Overhang parameter (as Yes/No)
                Parameter roofOverhangParam = selectedElement.LookupParameter("Element_HasRoofOverhang");
                if (roofOverhangParam == null)
                {
                    Definition roofOverhangParamDefinition = SharedParameterUtility.FindOrCreateSharedParameter(
                        uiApp, "Element_HasRoofOverhang", SpecTypeId.Int.Integer, BuiltInParameterGroup.PG_DATA);
                    roofOverhangParam = selectedElement.LookupParameter("Element_HasRoofOverhang");
                }
                if (roofOverhangParam != null && !roofOverhangParam.IsReadOnly)
                {
                    roofOverhangParam.Set(data.HasRoofOverhang ? 1 : 0);
                    message += $"- Roof Overhang: {(data.HasRoofOverhang ? "True" : "False")}\n";
                }

                // Save Ground Distance parameter
                message += SaveParameter(uiApp, selectedElement, "Element_GroundDistance", data.GroundDistance);

                // Save Overhang Distance parameter
                message += SaveParameter(uiApp, selectedElement, "Element_OverhangDistance", data.OverhangDistance);

                // Save Shelter Distance parameter
                message += SaveParameter(uiApp, selectedElement, "Element_ShelterDistance", data.ShelterDistance);

                // Save Service Life parameter
                Parameter serviceLifeParam = selectedElement.LookupParameter("Element_Service Life Duration");
                if (serviceLifeParam == null)
                {
                    Definition serviceLifeParamDefinition = SharedParameterUtility.FindOrCreateSharedParameter(
                        uiApp, "Element_Service Life Duration", SpecTypeId.Number, BuiltInParameterGroup.PG_DATA);
                    serviceLifeParam = selectedElement.LookupParameter("Element_Service Life Duration");
                }
                if (serviceLifeParam != null && !serviceLifeParam.IsReadOnly)
                {
                    serviceLifeParam.Set(data.ServiceLife.ToString(CultureInfo.InvariantCulture));  // Enforcing '.' as separator
                    message += $"- Service Life: {data.ServiceLife} years\n";
                }

                // Save KTraps, DRef, and WDR Ratios
                message += SaveParameter(uiApp, selectedElement, "Element_KTrap1", data.KTrap1?.ToString(CultureInfo.InvariantCulture));
                message += SaveParameter(uiApp, selectedElement, "Element_KTrap2", data.KTrap2?.ToString(CultureInfo.InvariantCulture));
                message += SaveParameter(uiApp, selectedElement, "Element_KTrap3", data.KTrap3?.ToString(CultureInfo.InvariantCulture));
                message += SaveParameter(uiApp, selectedElement, "Element_KTrap4", data.KTrap4?.ToString(CultureInfo.InvariantCulture));
                message += SaveParameter(uiApp, selectedElement, "Element_KTrap5", data.KTrap5?.ToString(CultureInfo.InvariantCulture));
                message += SaveParameter(uiApp, selectedElement, "Element_DRef", data.DRef?.ToString(CultureInfo.InvariantCulture));
                message += SaveParameter(uiApp, selectedElement, "Element_DShelt", data.DShelt?.ToString(CultureInfo.InvariantCulture));
                message += SaveParameter(uiApp, selectedElement, "Element_WDRRatio", data.WDRRatio?.ToString(CultureInfo.InvariantCulture));
                message += SaveParameter(uiApp, selectedElement, "Element_WDRRatioH", data.WDRRatioH?.ToString(CultureInfo.InvariantCulture));

                // Save Latitude parameter
                Parameter latitudeParam = selectedElement.LookupParameter("Element_Latitude");
                if (latitudeParam == null)
                {
                    Definition latitudeParamDefinition = SharedParameterUtility.FindOrCreateSharedParameter(
                        uiApp, "Element_Latitude", SpecTypeId.Number, BuiltInParameterGroup.PG_DATA);
                    latitudeParam = selectedElement.LookupParameter("Element_Latitude");
                }
                if (latitudeParam != null && !latitudeParam.IsReadOnly)
                {
                    latitudeParam.Set(data.SelectedLocationLatitude);
                    message += $"- Latitude: {data.SelectedLocationLatitude}\n";
                }

                // Save Longitude parameter
                Parameter longitudeParam = selectedElement.LookupParameter("Element_Longitude");
                if (longitudeParam == null)
                {
                    Definition longitudeParamDefinition = SharedParameterUtility.FindOrCreateSharedParameter(
                        uiApp, "Element_Longitude", SpecTypeId.Number, BuiltInParameterGroup.PG_DATA);
                    longitudeParam = selectedElement.LookupParameter("Element_Longitude");
                }
                if (longitudeParam != null && !longitudeParam.IsReadOnly)
                {
                    longitudeParam.Set(data.SelectedLocationLongitude);
                    message += $"- Longitude: {data.SelectedLocationLongitude}\n";
                }

                // Save City parameter
                Parameter cityParam = selectedElement.LookupParameter("Element_City");
                if (cityParam == null)
                {
                    Definition cityParamDefinition = SharedParameterUtility.FindOrCreateSharedParameter(
                        uiApp, "Element_City", SpecTypeId.String.Text, BuiltInParameterGroup.PG_DATA);
                    cityParam = selectedElement.LookupParameter("Element_City");
                }
                if (cityParam != null && !cityParam.IsReadOnly)
                {
                    cityParam.Set(data.SelectedLocationCity);
                    message += $"- City: {data.SelectedLocationCity}\n";
                }

                // Save Country parameter
                Parameter countryParam = selectedElement.LookupParameter("Element_Country");
                if (countryParam == null)
                {
                    Definition countryParamDefinition = SharedParameterUtility.FindOrCreateSharedParameter(
                        uiApp, "Element_Country", SpecTypeId.String.Text, BuiltInParameterGroup.PG_DATA);
                    countryParam = selectedElement.LookupParameter("Element_Country");
                }
                if (countryParam != null && !countryParam.IsReadOnly)
                {
                    countryParam.Set(data.SelectedLocationCountry);
                    message += $"- Country: {data.SelectedLocationCountry}\n";
                }

                // Show a single MessageBox with all saved parameters
                //MessageBox.Show(message, "Saved Parameters");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving selections: {ex.Message}", "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }



        private string SaveParameter(UIApplication uiApp, Element selectedElement, string parameterName, string value)
        {
            Parameter param = selectedElement.LookupParameter(parameterName);

            // Ensure the parameter is created if it doesn't already exist
            if (param == null)
            {
                Definition paramDefinition = SharedParameterUtility.FindOrCreateSharedParameter(
                    uiApp, parameterName, SpecTypeId.String.Text, BuiltInParameterGroup.PG_DATA);
                param = selectedElement.LookupParameter(parameterName);
            }

            // Even if the value is empty, make sure the parameter exists
            if (param != null && !param.IsReadOnly)
            {
                param.Set(string.IsNullOrEmpty(value) ? "" : value);
                return $"- {parameterName}: {(string.IsNullOrEmpty(value) ? "Not Set" : value)}\n";
            }
            return $"- {parameterName}: Failed to Create/Set\n";
        }

        public UserSelectionData GetUserSelections(Element selectedElement)
        {
            var data = new UserSelectionData();

            try
            {
                // Retrieve individual shared parameters
                data.Material = selectedElement.LookupParameter("Element_Material")?.AsString();
                data.Treatment = selectedElement.LookupParameter("Element_Treatment")?.AsString();
                data.SoilContact = selectedElement.LookupParameter("Element_SoilContact")?.AsString();
                data.Location = selectedElement.LookupParameter("Element_Location")?.AsString();
                data.Exposure = selectedElement.LookupParameter("Element_Exposure")?.AsString();
                data.ElementIntersection = selectedElement.LookupParameter("Element_Intersection")?.AsString();
                data.IsVerticalMember = selectedElement.LookupParameter("Element_IsVerticalMember")?.AsInteger() == 1;
                data.HasRoofOverhang = selectedElement.LookupParameter("Element_HasRoofOverhang")?.AsInteger() == 1;
                data.GroundDistance = selectedElement.LookupParameter("Element_GroundDistance")?.AsString();
                data.OverhangDistance = selectedElement.LookupParameter("Element_OverhangDistance")?.AsString();
                data.ShelterDistance = selectedElement.LookupParameter("Element_ShelterDistance")?.AsString();
                data.ServiceLife = selectedElement.LookupParameter("Element_Service Life Duration")?.AsDouble() ?? 0;

                return data;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error retrieving selections: {ex.Message}");
                return null;
            }
        }

        public void ApplyUserSelectionsToUI(Element selectedElement, UserSelectionData data)
        {
            // Step 1: Debugging all the retrieved user selections in one window.
            if (data != null)
            {
                try
                {
                    // Debugging: Display all retrieved user selections in a single window
                    string debugMessage = $"User Selections for Element ID: {selectedElement.Id}\n" +
                                          $"Material: {data.Material}\n" +
                                          $"Treatment: {data.Treatment}\n" +
                                          $"Soil Contact: {data.SoilContact}\n" +
                                          $"Location: {data.Location}\n" +
                                          $"Exposure: {data.Exposure}\n" +
                                          $"Element Intersection: {data.ElementIntersection}\n" +
                                          $"Is Vertical Member: {data.IsVerticalMember}\n" +
                                          $"Has Roof Overhang: {data.HasRoofOverhang}\n" +
                                          $"Ground Distance: {data.GroundDistance}\n" +
                                          $"Overhang Distance: {data.OverhangDistance}\n" +
                                          $"Shelter Distance: {data.ShelterDistance}\n" +
                                          $"Service Life: {data.ServiceLife}\n" +
                                          $"Location Details: City={data.SelectedLocationCity}, Country={data.SelectedLocationCountry}, " +
                                          $"Latitude={data.SelectedLocationLatitude}, Longitude={data.SelectedLocationLongitude}\n" +
                                          $"KTrap1: {data.KTrap1}, KTrap2: {data.KTrap2}, KTrap3: {data.KTrap3}, KTrap4: {data.KTrap4}, KTrap5: {data.KTrap5}\n" +
                                          $"DRef: {data.DRef}, DShelt: {data.DShelt}, WDRRatio: {data.WDRRatio}, WDRRatioH: {data.WDRRatioH}";

                    //MessageBox.Show(debugMessage, "Debugging: User Selections");

                    // Step 2: Apply user selections or default values if any are null.
                    SetComboBoxSelection(materialField, data.Material ?? string.Empty); // Material
                    SetComboBoxSelection(treatmentField, data.Treatment ?? string.Empty); // Treatment
                    SetComboBoxSelection(soilContactField, data.SoilContact ?? "Above Ground"); // Soil Contact
                    SetComboBoxSelection(locationField, data.Location ?? string.Empty); // Location
                    SetComboBoxSelection(exposureField, data.Exposure ?? "Side grain exposed"); // Exposure
                    SetComboBoxSelection(elementIntersectionField, data.ElementIntersection ?? "No contact face or gap size >5 mm free from dirt"); // Intersection

                    // Apply default checkbox states
                    SetCheckboxState(verticalMemberCheckbox, data.IsVerticalMember); // Default: unchecked
                    SetCheckboxState(roofOverhangCheckbox, data.HasRoofOverhang); // Default: unchecked

                    // Apply default text values if no data was saved
                    groundDistTextBox.Text = string.IsNullOrEmpty(data.GroundDistance) ? "NA" : data.GroundDistance; // Default: "NA"
                    overhangTextBox.Text = string.IsNullOrEmpty(data.OverhangDistance) ? "0" : data.OverhangDistance; // Default: "0"
                    shelterDistTextBox.Text = string.IsNullOrEmpty(data.ShelterDistance) ? "1.0" : data.ShelterDistance; // Default: "1.0"

                    // Apply default for service life output if no data was saved
                    serviceLifeOutput.Text = data.ServiceLife > 0 ? data.ServiceLife.ToString() : string.Empty; // Default: empty

                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error applying selections to UI: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                // Apply default values if no data is found at all
                //MessageBox.Show("No saved user selections found. Applying default values.", "Info");

                SetComboBoxSelection(soilContactField, "Above Ground");
                SetComboBoxSelection(exposureField, "Side grain exposed");
                SetComboBoxSelection(elementIntersectionField, "No contact face or gap size >5 mm free from dirt");

                verticalMemberCheckbox.IsChecked = false;
                roofOverhangCheckbox.IsChecked = false;

                groundDistTextBox.Text = "NA";
                overhangTextBox.Text = "0";
                shelterDistTextBox.Text = "1.0";
                serviceLifeOutput.Text = string.Empty;
            }
        }

        private void SetComboBoxSelection(System.Windows.Controls.ComboBox comboBox, string value)
        {
            // Clear existing items
            //comboBox.Items.Clear();

            if (string.IsNullOrEmpty(value))
            {
                comboBox.SelectedIndex = -1; // No selection if value is null or empty
                return;
            }

            foreach (var item in comboBox.Items)
            {
                if (item is ComboBoxItem comboBoxItem && comboBoxItem.Content.ToString() == value)
                {
                    comboBox.SelectedItem = comboBoxItem;
                    return;
                }
                else if (item is string stringItem && stringItem == value)
                {
                    comboBox.SelectedItem = stringItem;
                    return;
                }
            }

            comboBox.Items.Add(value);
            comboBox.SelectedItem = value;
        }



        private void SetCheckboxState(CheckBox checkbox, bool isChecked)
        {
            try
            {
                checkbox.IsChecked = isChecked;
                //MessageBox.Show($"Checkbox {checkbox.Name} set to {(isChecked ? "checked" : "unchecked")}");
            }
            catch (Exception ex)
            {
                //MessageBox.Show($"Error setting checkbox state for {checkbox.Name}: {ex.Message}");
            }
        }

        // Helper method to set a parameter
        private void SetParameter(Element element, string parameterName, string value)
        {
            Parameter param = element.LookupParameter(parameterName);
            if (param != null && !param.IsReadOnly)
            {
                param.Set(value);
            }
        }

        // Helper method to set checkbox state as Yes/No parameter
        private void SetCheckboxState(Element element, string parameterName, bool isChecked)
        {
            Parameter param = element.LookupParameter(parameterName);
            if (param != null && !param.IsReadOnly)
            {
                param.Set(isChecked ? 1 : 0); // Assuming 1 for true (checked) and 0 for false (unchecked)
            }
        }

        //---------Start Code for Auto-Populate---------------------------------------------------


        public void AutoPopulateButton_Click(
        System.Windows.Controls.ComboBox soilContactField,
        System.Windows.Controls.ComboBox exposureField,
        System.Windows.Controls.ComboBox elementIntersectionField,
        System.Windows.Controls.CheckBox roofOverhangCheckbox,
        System.Windows.Controls.CheckBox verticalMemberCheckbox,
        System.Windows.Controls.TextBox groundDistTextBox,
        System.Windows.Controls.TextBox overhangTextBox,
        System.Windows.Controls.TextBox shelterDistTextBox,
        object sender,
        RoutedEventArgs e)
        {
            List<string> detailedLog = new List<string>(); // Collect detailed logs

            try
            {
                // Check UI fields before proceeding
                if (soilContactField == null) throw new Exception("Soil Contact Field is null.");
                if (exposureField == null) throw new Exception("Exposure Field is null.");
                if (elementIntersectionField == null) throw new Exception("Element Intersection Field is null.");
                if (roofOverhangCheckbox == null) throw new Exception("Roof Overhang Checkbox is null.");
                if (verticalMemberCheckbox == null) throw new Exception("Vertical Member Checkbox is null.");
                if (groundDistTextBox == null) throw new Exception("Ground Distance TextBox is null.");
                if (overhangTextBox == null) throw new Exception("Overhang TextBox is null.");
                if (shelterDistTextBox == null) throw new Exception("Shelter Distance TextBox is null.");

                // Get the selected element and document
                Element selectedElement = GetSelectedElement();
                if (selectedElement == null)
                {
                    throw new Exception("No element selected or the selected element is invalid.");
                }

                Document doc = GetDocument();
                if (doc == null)
                {
                    throw new Exception("Document is null or invalid.");
                }

                string message = "";  // Collect feedback messages

                // Step 1: Populate Ground Condition
                bool groundConditionSuccess = SLE_AutoPopulateUtility.PopulateGroundCondition(
                    soilContactField,
                    selectedElement,
                    doc,
                    ref message,
                    DisableIrrelevantControls // Pass the DisableIrrelevantControls method as an action
                );

                detailedLog.Add(message);  // Log ground condition result

                // Stop execution if the result indicates "in-ground"
                if (soilContactField.SelectedItem != null && soilContactField.SelectedItem.ToString() == "In-Ground")
                {
                    detailedLog.Add("Ground condition indicates 'In-Ground'. Stopping further execution.");
                    SaveLogToDesktop(detailedLog); // Write log to a text file
                    return; // Exit the method
                }

                // Step 2: Populate Shelter Condition
                string shelterMessage = "";
                SLE_AutoPopulateUtility.PopulateShelterCondition(
                    roofOverhangCheckbox,
                    verticalMemberCheckbox,
                    groundDistTextBox,
                    overhangTextBox,
                    shelterDistTextBox,
                    selectedElement,
                    doc,
                    ref shelterMessage
                );
                detailedLog.Add(shelterMessage); // Log shelter messages

                // Step 3: Populate Exposure Condition
                string exposureMessage = ""; // Create a string to collect debug info
                SLE_AutoPopulateUtility.PopulateExposureCondition(exposureField, selectedElement, doc, ref exposureMessage);
                detailedLog.Add(exposureMessage); // Log the exposure message
                detailedLog.Add("Exposure Condition successfully populated.");

                // Step 4: Populate Element Intersection Condition
                SLE_AutoPopulateUtility.PopulateElementIntersectionCondition(elementIntersectionField, selectedElement, doc, ref message);
                detailedLog.Add("Element Intersection Condition successfully populated.");

                // Write log to a text file on the desktop
                SaveLogToDesktop(detailedLog);



                // Finally, show all collected messages in a TaskDialog
                string detailedLogMessage = string.Join(Environment.NewLine, detailedLog);

                TaskDialog taskDialog = new TaskDialog("Auto-populate Results")
                {
                    MainInstruction = "Auto-populate Results",
                    MainContent = detailedLogMessage,
                    AllowCancellation = false // Optional, disallow cancellation if not needed
                };
                taskDialog.Show();
            }
            catch (Exception ex)
            {
                // Add exception message to the detailed log
                detailedLog.Add($"Error: {ex.Message}");
                string detailedLogMessage = string.Join(Environment.NewLine, detailedLog);

                // Write error log to a text file on the desktop
                SaveLogToDesktop(detailedLog);

                // Display the error in a TaskDialog
                TaskDialog taskDialog = new TaskDialog("Auto-populate Error")
                {
                    MainInstruction = "Failed to auto-populate UI options",
                    MainContent = detailedLogMessage + $"\n\nError: {ex.Message}",
                    MainIcon = TaskDialogIcon.TaskDialogIconWarning // Show warning icon
                };
                taskDialog.Show();
            }
        }


        private void SaveLogToDesktop(List<string> logEntries)
        {
            try
            {
                // Get the path to the user's desktop
                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

                // Define the filename with a timestamp
                string fileName = $"AutoPopulateLog_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
                string fullFilePath = Path.Combine(desktopPath, fileName);

                // Write all log entries to the file
                File.WriteAllLines(fullFilePath, logEntries);
            }
            catch (Exception ex)
            {
                // If there's an error in writing to the file, show a TaskDialog
                TaskDialog.Show("Log File Error", $"Failed to write log to desktop: {ex.Message}");
            }
        }

        public void AutoPopulate_SLE_Parameters()
        {
            List<string> detailedLog = new List<string>(); // To collect messages about the gathered parameters

            try
            {
                // Step 1: Get the selected element
                Element selectedElement = GetSelectedElement();
                if (selectedElement == null)
                {
                    throw new Exception("No valid element selected.");
                }

                // Step 2: Retrieve Material and Treatment Data from the Selected Element
                string material = selectedElement.LookupParameter("Element_Material")?.AsString();
                string treatment = selectedElement.LookupParameter("Element_Treatment")?.AsString();

                if (string.IsNullOrEmpty(material) || string.IsNullOrEmpty(treatment))
                {
                    throw new Exception("Material or Treatment data is missing from the selected element.");
                }

                // Log retrieved material and treatment
                detailedLog.Add($"Material: {material}");
                detailedLog.Add($"Treatment: {treatment}");

                // Step 3: Load Location Data from the JSON File
                string filePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RevitProjectLocation.json");
                UserSelectionData locationData = LoadLocationFromJson(filePath);
                if (locationData == null)
                {
                    throw new Exception("Location data could not be loaded from the JSON file.");
                }

                // Log location details
                detailedLog.Add($"Location: {locationData.SelectedLocationCity}, {locationData.SelectedLocationCountry} (Lat: {locationData.SelectedLocationLatitude}, Lon: {locationData.SelectedLocationLongitude})");

                // Step 4: Populate Ground Condition
                string groundConditionMessage = "";
                SLE_AutoPopulateUtility.PopulateGroundCondition(
                    soilContactField,
                    selectedElement,
                    _doc,
                    ref groundConditionMessage,
                    DisableIrrelevantControls // Pass the DisableIrrelevantControls method as an action
                );
                detailedLog.Add($"Ground Condition: {groundConditionMessage}");

                // Step 5: Populate Shelter Condition
                string shelterConditionMessage = "";
                SLE_AutoPopulateUtility.PopulateShelterCondition(roofOverhangCheckbox, verticalMemberCheckbox, groundDistTextBox, overhangTextBox, shelterDistTextBox, selectedElement, _doc, ref shelterConditionMessage);
                detailedLog.Add($"Shelter Condition: {shelterConditionMessage}");

                // Step 6: Populate Exposure Condition
                string exposureConditionMessage = "";
                SLE_AutoPopulateUtility.PopulateExposureCondition(exposureField, selectedElement, _doc, ref exposureConditionMessage);
                detailedLog.Add($"Exposure Condition: {exposureConditionMessage}");

                // Step 7: Populate Element Intersection Condition
                string elementIntersectionMessage = "";
                SLE_AutoPopulateUtility.PopulateElementIntersectionCondition(elementIntersectionField, selectedElement, _doc, ref elementIntersectionMessage);
                detailedLog.Add($"Element Intersection Condition: {elementIntersectionMessage}");

                // Step 8: Display all gathered parameters in a message
                string finalMessage = string.Join(Environment.NewLine, detailedLog);
                MessageBox.Show(finalMessage, "Auto-Populated Service Life Parameters", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error during parameter population: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        // End of Code for Auto-Populate

        // Helper method to load location data from the saved JSON file
        private UserSelectionData LoadLocationFromJson(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return null; // Return null if the file does not exist
            }

            try
            {
                string json = File.ReadAllText(filePath);
                return JsonConvert.DeserializeObject<UserSelectionData>(json);
            }
            catch (Exception)
            {
                return null; // Return null if deserialization fails
            }
        }



        public class UserSelectionData
        {
            public string Material { get; set; }
            public string Treatment { get; set; }
            public string SoilContact { get; set; }
            public string Location { get; set; } // Add this for storing location
            public string Exposure { get; set; }
            public string ElementIntersection { get; set; }
            public bool IsVerticalMember { get; set; }
            public bool HasRoofOverhang { get; set; }
            public string GroundDistance { get; set; }
            public string OverhangDistance { get; set; }
            public string ShelterDistance { get; set; }
            public double ServiceLife { get; set; }

            // Add these fields to save more complex location data if needed
            public string SelectedLocationCity { get; set; }
            public string SelectedLocationCountry { get; set; }
            public double SelectedLocationLatitude { get; set; }
            public double SelectedLocationLongitude { get; set; }

            // Change these fields to double?
            public double? KTrap1 { get; set; }
            public double? KTrap2 { get; set; }
            public double? KTrap3 { get; set; }
            public double? KTrap4 { get; set; }
            public double? KTrap5 { get; set; }
            public double? DRef { get; set; }
            public double? DShelt { get; set; }
            public double? WDRRatio { get; set; }
            public double? WDRRatioH { get; set; }
        }



    }


}
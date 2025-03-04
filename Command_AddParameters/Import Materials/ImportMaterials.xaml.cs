using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace RevitWoodLCC
{
    public partial class ImportMaterials : Window
    {
        private Document _doc;
        private List<MaterialData> _materials;

        public ImportMaterials(Document doc)
        {
            InitializeComponent();
            _doc = doc;
            LoadMaterials();
        }

        public static List<MaterialData> LoadMaterials()
        {
            try
            {
                // Example: Assuming MaterialImportUtility.GetAllMaterials fetches the data
                var materials = MaterialImportUtility.GetAllMaterials();
                Debug.WriteLine("Materials loaded successfully.");
                return materials;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading materials: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return new List<MaterialData>();
            }
        }


        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = searchTextBox.Text.ToLower();
            materialsComboBox.ItemsSource = _materials.Where(m => m.Name[0].ToLower().Contains(searchText));
        }

        private void MaterialsComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (materialsComboBox.SelectedItem is MaterialData selectedMaterial)
            {
                treatmentsComboBox.ItemsSource = selectedMaterial.Treatment;
            }
        }

        private void AssignMaterialsToAllElementsButton_Click(object sender, RoutedEventArgs e)
        {
            if (_materials == null || _materials.Count == 0)
            {
                MessageBox.Show("No materials available to assign.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (materialsComboBox.SelectedItem is not MaterialData selectedMaterial || treatmentsComboBox.SelectedItem == null)
            {
                MessageBox.Show("Please select both a material and a treatment before assigning.", "Selection Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Assign materials and update shared parameters
            CreateMaterialsInRevit(_materials);
            UpdateSharedParameters(selectedMaterial.Name[0], treatmentsComboBox.SelectedItem.ToString());
        }


        private void CreateMaterialsInRevit(List<MaterialData> materialsData)
        {
            List<string> createdMaterials = new List<string>();
            List<string> failedMaterials = new List<string>();
            bool allSuccess = true;

            using (Transaction tx = new Transaction(_doc, "Assign Material Parameters"))
            {
                try
                {
                    tx.Start();

                    foreach (var materialData in materialsData)
                    {
                        var material = CreateMaterial(_doc, materialData.Name[0], failedMaterials);

                        if (material != null)
                        {
                            // Set parameters for the material
                            bool latinNameSuccess = SetMaterialParameter(material, "LatinName", materialData.LatinName[0]);
                            bool treatmentSuccess = SetMaterialParameter(material, "Treatment", materialData.Treatment[0]);
                            bool resistanceUC3Success = SetMaterialParameter(material, "ResistanceDoseUC3", materialData.ResistanceDoseUC3);
                            bool resistanceUC4Success = SetMaterialParameter(material, "ResistanceDoseUC4", materialData.ResistanceDoseUC4);

                            // Assign Material Class to "Wood"
                            material.MaterialClass = "Wood";

                            // Format and set the description
                            string description = FormatDescription(materialData);
                            bool descriptionSuccess = SetMaterialParameter(material, "Description", description);

                            // Track successes and failures
                            if (latinNameSuccess && treatmentSuccess && resistanceUC3Success && resistanceUC4Success && descriptionSuccess)
                            {
                                createdMaterials.Add(materialData.Name[0]);
                            }
                            else
                            {
                                allSuccess = false;
                                failedMaterials.Add(materialData.Name[0]);
                            }
                        }
                        else
                        {
                            allSuccess = false;
                        }
                    }

                    tx.Commit();
                }
                catch (Exception ex)
                {
                    tx.RollBack();
                    Debug.WriteLine($"Error during material assignment: {ex.Message}");
                    MessageBox.Show($"Error assigning materials: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            // Show results
            if (allSuccess)
            {
                TaskDialog.Show("Complete Success", $"{createdMaterials.Count} materials and their parameters were successfully created and set.");
            }
            else
            {
                TaskDialog td = new TaskDialog("Partial Success");
                td.MainInstruction = $"Successfully created/updated {createdMaterials.Count} materials with some failures.";
                td.MainIcon = TaskDialogIcon.TaskDialogIconWarning;
                td.CommonButtons = TaskDialogCommonButtons.Ok;
                td.Show();
            }
        }

        private void UpdateSharedParameters(string materialName, string treatment)
        {
            using (Transaction tx = new Transaction(_doc, "Update Shared Parameters"))
            {
                try
                {
                    tx.Start();

                    // Get all elements in the active document
                    var allElements = new FilteredElementCollector(_doc)
                        .WhereElementIsNotElementType()
                        .ToElements();

                    foreach (var element in allElements)
                    {
                        // Update Element_Material parameter
                        var materialParam = element.LookupParameter("Element_Material");
                        if (materialParam != null && !materialParam.IsReadOnly)
                        {
                            materialParam.Set(materialName);
                        }

                        // Update Element_Treatment parameter
                        var treatmentParam = element.LookupParameter("Element_Treatment");
                        if (treatmentParam != null && !treatmentParam.IsReadOnly)
                        {
                            treatmentParam.Set(treatment);
                        }
                    }

                    tx.Commit();
                    MessageBox.Show("Shared parameters updated successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    tx.RollBack();
                    MessageBox.Show($"Failed to update shared parameters: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }


        private Material CreateMaterial(Document doc, string name, List<string> failedMaterials)
        {
            Material material = new FilteredElementCollector(doc)
                .OfClass(typeof(Material))
                .Cast<Material>()
                .FirstOrDefault(m => m.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

            if (material == null)
            {
                try
                {
                    ElementId materialId = Material.Create(doc, name);
                    if (materialId != ElementId.InvalidElementId)
                    {
                        material = doc.GetElement(materialId) as Material;
                    }
                    else
                    {
                        failedMaterials.Add(name);
                    }
                }
                catch (Exception)
                {
                    failedMaterials.Add(name);
                }
            }

            return material;
        }

        private bool SetMaterialParameter(Material material, string parameterName, object value)
        {
            try
            {
                Parameter param = material.LookupParameter(parameterName);
                if (param != null && !param.IsReadOnly)
                {
                    switch (param.StorageType)
                    {
                        case StorageType.Double:
                            if (value is double doubleValue) param.Set(doubleValue);
                            break;
                        case StorageType.Integer:
                            if (value is int intValue) param.Set(intValue);
                            break;
                        case StorageType.String:
                            if (value is string stringValue) param.Set(stringValue);
                            break;
                        default:
                            Debug.WriteLine($"Unhandled StorageType for parameter '{parameterName}'.");
                            return false;
                    }

                    return true;
                }
                else
                {
                    Debug.WriteLine($"Parameter '{parameterName}' not found or is read-only.");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to set parameter '{parameterName}' for material '{material.Name}': {ex.Message}");
                return false;
            }
        }

        private string FormatDescription(MaterialData materialData)
        {
            return $"name: {materialData.Name[0]}, " +
                   $"latinName: {string.Join(", ", materialData.LatinName)}, " +
                   $"treatment: {string.Join(", ", materialData.Treatment)}, " +
                   $"resistanceDoseUC3: {materialData.ResistanceDoseUC3}, " +
                   $"resistanceDoseUC4: {materialData.ResistanceDoseUC4}";
        }
    }
}

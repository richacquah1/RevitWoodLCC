using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.Attributes;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
//using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace RevitWoodLCC
{
    [Transaction(TransactionMode.Manual)]
    public class FindAndHighlightElementsWithoutMaterials : IExternalCommand
    {
        public Result Execute(
          ExternalCommandData commandData,
          ref string message,
          ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;

            // Lists to hold ids of elements without materials
            List<ElementId> modelElementsWithoutMaterials = new List<ElementId>();
            List<ElementId> componentElementsWithoutMaterials = new List<ElementId>();

            // Collect all FamilyInstances and HostObjects that are not element types
            var allElements = new FilteredElementCollector(doc)
                .WhereElementIsNotElementType()
                .Where(e => e is FamilyInstance || e is HostObject)
                .ToList();

            // Loop through the elements and categorize them
            foreach (Element elem in allElements)
            {
                if (!ElementHasMaterial(doc, elem))
                {
                    if (elem is HostObject)
                    {
                        modelElementsWithoutMaterials.Add(elem.Id);
                    }
                    else if (elem is FamilyInstance)
                    {
                        componentElementsWithoutMaterials.Add(elem.Id);
                    }
                }
            }

            // Highlight elements in the UI
            var allElementsWithoutMaterials = modelElementsWithoutMaterials.Concat(componentElementsWithoutMaterials).ToList();
            uidoc.Selection.SetElementIds(allElementsWithoutMaterials);

            // Prepare and display the results
            string resultMessage = $"Model Elements without materials: {modelElementsWithoutMaterials.Count}\n" +
                                   $"Component Elements without materials: {componentElementsWithoutMaterials.Count}";

            // Show the non-modal WPF window with the results
            ShowResultsWindow(uidoc, modelElementsWithoutMaterials, componentElementsWithoutMaterials);


            return Result.Succeeded;
        }

        private bool ElementHasMaterial(Document doc, Element elem)
        {
            // Direct material check for simple elements
            if (elem.GetMaterialIds(false).Count > 0)
            {
                return true;
            }

            // Check for compound structures (e.g., walls, floors)
            if (elem is HostObject hostObject)
            {
                CompoundStructure compoundStructure = GetCompoundStructure(hostObject);
                if (compoundStructure != null && compoundStructure.GetLayers().Any(layer => layer.MaterialId != ElementId.InvalidElementId))
                {
                    return true;
                }
            }

            // Deep check for geometry, including families and nested families
            Options geomOptions = new Options
            {
                DetailLevel = ViewDetailLevel.Fine,
                ComputeReferences = true,
                IncludeNonVisibleObjects = false
            };
            GeometryElement geomElement = elem.get_Geometry(geomOptions);
            if (geomElement != null)
            {
                foreach (GeometryObject geomObj in geomElement)
                {
                    if (CheckGeometryObjectForMaterial(geomObj))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private CompoundStructure GetCompoundStructure(HostObject hostObject)
        {
            return hostObject switch
            {
                Wall wall => wall.WallType.GetCompoundStructure(),
                Floor floor => floor.FloorType.GetCompoundStructure(),
                // Add other cases as needed
                _ => null,
            };
        }

        private bool CheckGeometryObjectForMaterial(GeometryObject geomObj)
        {
            return geomObj switch
            {
                GeometryInstance instance => instance.GetInstanceGeometry().Any(instObj => CheckGeometryObjectForMaterial(instObj)),
                Solid solid => solid.Faces.OfType<Face>().Any(face => face.MaterialElementId != ElementId.InvalidElementId),
                // Additional checks for other types like Meshes could be added here if needed
                _ => false,
            };
        }

        //private void ShowResultsWindow(UIDocument uidoc, List<SelectedElementId> modelElementsWithoutMaterials, List<SelectedElementId> componentElementsWithoutMaterials)
        //{
        //    // Create and configure the window
        //    Window resultsWindow = new Window
        //    {
        //        Title = "Results",
        //        Height = 400,
        //        Width = 600,
        //        WindowStartupLocation = WindowStartupLocation.CenterScreen
        //    };

        //    // Create a StackPanel to hold content
        //    StackPanel stackPanel = new StackPanel
        //    {
        //        Orientation = Orientation.Vertical
        //    };

        //    // Correctly combine element IDs and retrieve distinct categories
        //    var allElementIdsWithoutMaterials = modelElementsWithoutMaterials.Concat(componentElementsWithoutMaterials).ToList();
        //    var categories = allElementIdsWithoutMaterials
        //        .Select(id => uidoc.Document.GetElement(id))
        //        .Select(elem => elem.Category)
        //        .Where(cat => cat != null)
        //        .DistinctBy(cat => cat.Name)
        //        .OrderBy(cat => cat.Name);


        //    // Add a section in the window for categories
        //    stackPanel.Children.Add(new TextBlock { Text = "Categories of Elements without Materials:", FontWeight = FontWeights.Bold, Margin = new Thickness(5, 10, 5, 0) });
        //    foreach (var category in categories)
        //    {
        //        stackPanel.Children.Add(new TextBlock { Text = category.Name, Margin = new Thickness(5) });
        //    }

        //    // Create a button to open the new window
        //    Button showMaterialsButton = new Button
        //    {
        //        Content = "Show Elements by Material",
        //        Margin = new Thickness(5)
        //    };
        //    showMaterialsButton.Click += (s, e) => ShowMaterialsWindow(uidoc);

        //    // Add the button to the StackPanel
        //    stackPanel.Children.Add(showMaterialsButton);

        //    // Add headers and ListViews for model and component elements without materials
        //    stackPanel.Children.Add(new TextBlock { Text = "Model Elements without Materials:", FontWeight = FontWeights.Bold });
        //    stackPanel.Children.Add(GenerateStackPanel(modelElementsWithoutMaterials, uidoc));
        //    stackPanel.Children.Add(new TextBlock { Text = "Component Elements without Materials:", FontWeight = FontWeights.Bold });
        //    stackPanel.Children.Add(GenerateStackPanel(componentElementsWithoutMaterials, uidoc));

        //    // Function to generate StackPanel for element data (implementation assumed to be similar to provided, adjusted for context)

        //    // Set the StackPanel as the window content
        //    resultsWindow.Content = new ScrollViewer { Content = stackPanel };

        //    // Show the window
        //    resultsWindow.Show();
        //}

        private void ShowResultsWindow(UIDocument uidoc, List<ElementId> modelElementsWithoutMaterials, List<ElementId> componentElementsWithoutMaterials)
        {
            Window resultsWindow = new Window
            {
                Title = "Results",
                Height = 400,
                Width = 600,
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };

            ScrollViewer scrollViewer = new ScrollViewer();
            StackPanel stackPanel = new StackPanel { Orientation = Orientation.Vertical };
            scrollViewer.Content = stackPanel;

            // Displaying categories
            var allElementsWithoutMaterials = modelElementsWithoutMaterials.Concat(componentElementsWithoutMaterials).ToList();
            var categoryNames = allElementsWithoutMaterials
                .Select(id => uidoc.Document.GetElement(id).Category)
                .Where(category => category != null)
                .Select(category => category.Name)
                .Distinct()
                .OrderBy(name => name);

            TextBlock categoriesHeader = new TextBlock
            {
                Text = "Categories of Elements without Materials:",
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(5, 10, 5, 0)
            };
            stackPanel.Children.Add(categoriesHeader);

            foreach (var categoryName in categoryNames)
            {
                TextBlock categoryText = new TextBlock
                {
                    Text = categoryName,
                    Margin = new Thickness(5)
                };
                stackPanel.Children.Add(categoryText);
            }

            // Optionally, add detailed lists or other UI elements for model and component elements without materials here.

            // Buttons for further actions or displaying more information
            Button showMaterialsButton = new Button
            {
                Content = "Show Elements by Material",
                Margin = new Thickness(5)
            };
            showMaterialsButton.Click += (sender, e) => ShowMaterialsWindow(uidoc);
            stackPanel.Children.Add(showMaterialsButton);

            // Add further UI customization as needed

            resultsWindow.Content = scrollViewer;
            resultsWindow.Show();
        }


        private StackPanel GenerateStackPanel(List<ElementId> elementIds, UIDocument uidoc)
        {
            StackPanel innerStackPanel = new StackPanel
            {
                Margin = new Thickness(5)
            };

            foreach (var id in elementIds)
            {
                Element element = uidoc.Document.GetElement(id);
                string itemName = $"{element.Name} (ID: {element.Id.IntegerValue})";

                TextBlock textBlock = new TextBlock
                {
                    Text = itemName,
                    TextDecorations = TextDecorations.Underline,
                    Foreground = new SolidColorBrush(Colors.Blue),
                    Cursor = System.Windows.Input.Cursors.Hand,
                    Margin = new Thickness(2),
                    Tag = id // Store the SelectedElementId in the Tag property for later use
                };

                // Handle the MouseLeftButtonUp event to select and zoom to the element in Revit
                textBlock.MouseLeftButtonUp += (s, e) =>
                {
                    ElementId clickedId = (ElementId)((TextBlock)s).Tag;
                    uidoc.Selection.SetElementIds(new List<ElementId> { clickedId });
                    uidoc.ShowElements(new List<ElementId> { clickedId });
                };

                innerStackPanel.Children.Add(textBlock);
            }

            return innerStackPanel;
        }

        private void ShowMaterialsWindow(UIDocument uidoc)
        {
            // Create the materials window
            Window materialsWindow = new Window
            {
                Title = "Elements by Material",
                Height = 400,
                Width = 600,
                WindowStartupLocation = WindowStartupLocation.CenterScreen
            };

            // Use a TreeView to group elements by material
            TreeView materialsTreeView = new TreeView();

            // Retrieve all elements with materials
            var allElementsWithMaterials = new FilteredElementCollector(uidoc.Document)
                .WhereElementIsNotElementType()
                .Where(e => e.GetMaterialIds(false).Any())
                .ToList();

            // Group elements by material
            var elementsGroupedByMaterial = allElementsWithMaterials
                .SelectMany(e => e.GetMaterialIds(false).Select(mid => new { Element = e, MaterialId = mid }))
                .GroupBy(x => x.MaterialId)
                .OrderBy(g => uidoc.Document.GetElement(g.Key).Name);

            // Create TreeViewItems for each material group
            foreach (var group in elementsGroupedByMaterial)
            {
                Material mat = (Material)uidoc.Document.GetElement(group.Key);
                string materialName = mat != null ? mat.Name : "None";

                TreeViewItem materialItem = new TreeViewItem { Header = materialName };
                foreach (var elem in group)
                {
                    string itemName = $"{elem.Element.Name} (ID: {elem.Element.Id.IntegerValue})";
                    TextBlock elementText = new TextBlock
                    {
                        Text = itemName,
                        TextDecorations = TextDecorations.Underline,
                        Foreground = new SolidColorBrush(Colors.Blue),
                        Cursor = System.Windows.Input.Cursors.Hand,
                        Tag = elem.Element.Id // Store the SelectedElementId in the Tag property for later use
                    };

                    elementText.MouseLeftButtonUp += (s, e) =>
                    {
                        ElementId clickedId = (ElementId)((TextBlock)s).Tag;

                        // Select the element in Revit
                        uidoc.Selection.SetElementIds(new List<ElementId> { clickedId });

                        // Zoom to the selected element
                        uidoc.ShowElements(new List<ElementId> { clickedId });
                    };

                    materialItem.Items.Add(new TreeViewItem { Header = elementText });
                }
                materialsTreeView.Items.Add(materialItem);
            }

            // Set the TreeView as the content of the window
            materialsWindow.Content = new ScrollViewer { Content = materialsTreeView };

            // Show the window
            materialsWindow.Show();
        }

        private void MakeElementTextClickable(TextBlock elementText, UIDocument uidoc)
        {
            elementText.Cursor = System.Windows.Input.Cursors.Hand;
            elementText.Foreground = new SolidColorBrush(Colors.Blue);
            elementText.TextDecorations = TextDecorations.Underline;
            elementText.MouseDown += (s, e) =>
            {
                if (e.ClickCount == 1) // Single click to select and zoom
                {
                    ElementId clickedId = (ElementId)((TextBlock)s).Tag;
                    uidoc.Selection.SetElementIds(new List<ElementId> { clickedId });
                    uidoc.ShowElements(clickedId);
                }
            };
        }

        private ElementId _selectedMaterialId = ElementId.InvalidElementId;

        private void AssignMaterial(UIDocument uidoc, Dictionary<Material, List<ElementId>> elementsGroupedByMaterial, ElementId newMaterialId)
        {
            Document doc = uidoc.Document;

            // Ensure a valid material is selected
            if (newMaterialId == ElementId.InvalidElementId)
            {
                MessageBox.Show("Please select a material first.", "Material Not Selected", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using (Transaction trans = new Transaction(doc, "Assign Material"))
            {
                trans.Start();

                // Iterate through each group of elements, regardless of their current material
                foreach (var kvp in elementsGroupedByMaterial)
                {
                    foreach (ElementId elemId in kvp.Value)
                    {
                        Element element = doc.GetElement(elemId);

                        // Example: Assign the material to a parameter for FamilyInstances
                        // This assumes your elements support this kind of material assignment.
                        // Adjust as necessary for your specific element types and parameters.
                        if (element is FamilyInstance)
                        {
                            Parameter matParam = element.LookupParameter("Material"); // Adjust parameter name as needed
                            if (matParam != null && !matParam.IsReadOnly)
                            {
                                matParam.Set(newMaterialId);
                            }
                        }
                        // Extend this logic to other element types as needed.
                    }
                }

                trans.Commit();
            }
        }



    }
}

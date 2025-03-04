//This ode works perfect. Just uncomment this and the other in the other .cs file and use
//using Autodesk.Revit.DB;
//using Autodesk.Revit.UI;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Windows;
//using System.Windows.Controls;

//namespace RevitWoodLCC
//{
//    public class Preview3DForm : Window
//    {
//        private Document _doc;
//        private View3D _view3D;
//        private UIDocument _uiDoc;
//        private SelectedElementId _duplicatedViewId;
//        private IList<SelectedElementId> _elementsInDuplicatedView;
//        private int _currentElementIndex = 0;
//        private Button toggle3DPreviewButton;
//        private VisualizationMode currentMode = VisualizationMode.AllElements;
//        private PreviewControl previewControl;

//        public Preview3DForm(UIDocument uiDoc)
//        {
//            _doc = uiDoc.Document;
//            _uiDoc = uiDoc;
//            InitializeComponents();

//            _view3D = GetActive3DViewAndDuplicate(_doc);
//            if (_view3D != null)
//            {
//                InitializeElementsInDuplicatedView();
//                AddPreviewControlToUI(); // Add this line to ensure the preview control is added
//            }
//            else
//            {
//                MessageBox.Show("No suitable 3D view found.");
//                return;
//            }

//            // Handle the Closed event
//            this.Closed += Preview3DForm_Closed;
//        }

//        private void Preview3DForm_Closed(object sender, EventArgs e)
//        {
//            if (_duplicatedViewId != null)
//            {
//                using (Transaction tx = new Transaction(_doc, "Delete Temp View"))
//                {
//                    tx.Start();
//                    _doc.Delete(_duplicatedViewId);
//                    tx.Commit();
//                }
//            }
//        }

//        private void InitializeComponents()
//        {
//            // Window properties
//            Title = "3D Preview";
//            Width = 800;
//            Height = 600;

//            // Main grid
//            System.Windows.Controls.Grid mainGrid = new System.Windows.Controls.Grid();
//            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
//            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

//            // Button grid
//            System.Windows.Controls.Grid buttonGrid = new System.Windows.Controls.Grid();
//            buttonGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
//            buttonGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
//            buttonGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

//            // Toggle 3D Preview button
//            toggle3DPreviewButton = new Button
//            {
//                Content = "Toggle 3D Preview",
//                Width = 160,
//                HorizontalAlignment = HorizontalAlignment.Left
//            };
//            toggle3DPreviewButton.Click += Toggle3DPreviewButton_Click;
//            System.Windows.Controls.Grid.SetColumn(toggle3DPreviewButton, 0);
//            buttonGrid.Children.Add(toggle3DPreviewButton);

//            // Previous button
//            Button prevButton = new Button
//            {
//                Content = "Previous",
//                Width = 100,
//                HorizontalAlignment = HorizontalAlignment.Center
//            };
//            prevButton.Click += PreviousButton_Click;
//            System.Windows.Controls.Grid.SetColumn(prevButton, 1);
//            buttonGrid.Children.Add(prevButton);

//            // Next button
//            Button nextButton = new Button
//            {
//                Content = "Next",
//                Width = 100,
//                HorizontalAlignment = HorizontalAlignment.Right
//            };
//            nextButton.Click += NextButton_Click;
//            System.Windows.Controls.Grid.SetColumn(nextButton, 2);
//            buttonGrid.Children.Add(nextButton);

//            System.Windows.Controls.Grid.SetRow(buttonGrid, 1);
//            mainGrid.Children.Add(buttonGrid);

//            Content = mainGrid;
//        }

//        private void AddPreviewControlToUI()
//        {
//            if (_view3D != null)
//            {
//                previewControl = new PreviewControl(_doc, _view3D.Id);
//                previewControl.VerticalAlignment = VerticalAlignment.Stretch;
//                System.Windows.Controls.Grid.SetRow(previewControl, 0);
//                (Content as System.Windows.Controls.Grid).Children.Insert(0, previewControl);
//            }
//        }

//        private View3D GetActive3DViewAndDuplicate(Document doc)
//        {
//            View3D activeView3D = doc.ActiveView as View3D;

//            if (activeView3D == null || activeView3D.IsTemplate || !activeView3D.CanBePrinted)
//                return null;

//            using (Transaction tx = new Transaction(doc, "Duplicate 3D View"))
//            {
//                tx.Start();

//                SelectedElementId duplicatedViewId = activeView3D.Duplicate(ViewDuplicateOption.WithDetailing);
//                View3D duplicatedView3D = doc.GetElement(duplicatedViewId) as View3D;
//                duplicatedView3D.Name = "Temporary Preview View";

//                _duplicatedViewId = duplicatedViewId;

//                tx.Commit();
//                return duplicatedView3D;
//            }
//        }

//        private void InitializeElementsInDuplicatedView()
//        {
//            FilteredElementCollector collector = new FilteredElementCollector(_doc, _view3D.Id);
//            _elementsInDuplicatedView = collector.WhereElementIsNotElementType().ToElementIds().ToList();
//            _currentElementIndex = _elementsInDuplicatedView.IndexOf(_uiDoc.Selection.GetElementIds().FirstOrDefault());
//        }

//        private void Toggle3DPreviewButton_Click(object sender, RoutedEventArgs e)
//        {
//            UIDocument uiDoc = _uiDoc;
//            Document doc = uiDoc.Document;

//            View3D view3D = doc.GetElement(_duplicatedViewId) as View3D;

//            SelectedElementId firstSelectedId = uiDoc.Selection.GetElementIds().FirstOrDefault();
//            string elementIdAsString = firstSelectedId?.ToString();

//            Button toggle3DPreviewButton = sender as Button;

//            switch (currentMode)
//            {
//                case VisualizationMode.AllElements:
//                    currentMode = VisualizationMode.SelectedOnly;
//                    toggle3DPreviewButton.Content = "Show Selected and Adjacent";
//                    break;
//                case VisualizationMode.SelectedAndAdjacent:
//                    currentMode = VisualizationMode.AllElements;
//                    toggle3DPreviewButton.Content = "Show Selected Only";
//                    break;
//                case VisualizationMode.SelectedOnly:
//                    currentMode = VisualizationMode.SelectedAndAdjacent;
//                    toggle3DPreviewButton.Content = "Show All Elements";
//                    break;
//            }

//            SelectedElementId selectedElementId = new SelectedElementId(Convert.ToInt32(elementIdAsString));
//            IList<SelectedElementId> adjacentElementIds = FindAdjacentElements(doc, selectedElementId);
//            Modify3DView(_doc, _view3D, adjacentElementIds, selectedElementId, currentMode);
//        }

//        private void PreviousButton_Click(object sender, RoutedEventArgs e)
//        {
//            if (_currentElementIndex > 0)
//            {
//                _currentElementIndex--;
//                SelectElement(_elementsInDuplicatedView[_currentElementIndex]);
//            }
//            else
//            {
//                MessageBox.Show("This is the first element.", "Navigation", MessageBoxButton.OK, MessageBoxImage.Information);
//            }
//        }

//        private void NextButton_Click(object sender, RoutedEventArgs e)
//        {
//            if (_currentElementIndex < _elementsInDuplicatedView.Count - 1)
//            {
//                _currentElementIndex++;
//                SelectElement(_elementsInDuplicatedView[_currentElementIndex]);
//            }
//            else
//            {
//                MessageBox.Show("This is the last element.", "Navigation", MessageBoxButton.OK, MessageBoxImage.Information);
//            }
//        }

//        private void SelectElement(SelectedElementId elementId)
//        {
//            if (_uiDoc != null && elementId != SelectedElementId.InvalidElementId)
//            {
//                _uiDoc.Selection.SetElementIds(new List<SelectedElementId> { elementId });
//                ZoomToElement(_uiDoc, elementId);
//                _uiDoc.RefreshActiveView();
//            }
//        }

//        private void ZoomToElement(UIDocument uiDocument, SelectedElementId elementId)
//        {
//            try
//            {
//                Element element = uiDocument.Document.GetElement(elementId);
//                if (element != null)
//                {
//                    BoundingBoxXYZ boundingBox = element.get_BoundingBox(null);
//                    if (boundingBox != null)
//                    {
//                        Outline outline = new Outline(boundingBox.Min, boundingBox.Max);
//                        BoundingBoxXYZ newBox = new BoundingBoxXYZ()
//                        {
//                            Min = outline.MinimumPoint - new XYZ(5, 5, 5),
//                            Max = outline.MaximumPoint + new XYZ(5, 5, 5)
//                        };

//                        View3D view3D = uiDocument.Document.ActiveView as View3D;
//                        if (view3D != null)
//                        {
//                            using (Transaction trans = new Transaction(uiDocument.Document, "Zoom to Element"))
//                            {
//                                trans.Start();
//                                view3D.SetSectionBox(newBox);
//                                trans.Commit();
//                            }
//                        }
//                    }
//                }
//            }
//            catch (Exception ex)
//            {
//                TaskDialog.Show("Error", $"Failed to zoom to element: {ex.Message}");
//            }
//        }

//        private IList<SelectedElementId> FindAdjacentElements(Document doc, SelectedElementId selectedId)
//        {
//            Element selectedElement = doc.GetElement(selectedId);
//            BoundingBoxXYZ selectedBoundingBox = selectedElement.get_BoundingBox(null);
//            Outline outline = new Outline(selectedBoundingBox.Min, selectedBoundingBox.Max);
//            BoundingBoxIntersectsFilter bbFilter = new BoundingBoxIntersectsFilter(outline);

//            FilteredElementCollector collector = new FilteredElementCollector(doc).WherePasses(bbFilter);
//            IList<SelectedElementId> adjacentElementIds = new List<SelectedElementId>();

//            foreach (Element element in collector)
//            {
//                if (element.Id == selectedId)
//                    continue;

//                GeometryElement geomElem = element.get_Geometry(new Options());
//                if (geomElem != null)
//                {
//                    foreach (GeometryObject geomObj in geomElem)
//                    {
//                        if (geomObj is Solid || geomObj is GeometryInstance)
//                        {
//                            adjacentElementIds.Add(element.Id);
//                            break;
//                        }
//                    }
//                }
//            }
//            return adjacentElementIds;
//        }

//        private enum VisualizationMode
//        {
//            AllElements,
//            SelectedAndAdjacent,
//            SelectedOnly
//        }

//        private void Modify3DView(Document doc, View3D view3D, IList<SelectedElementId> adjacentElementIds, SelectedElementId selectedElementId, VisualizationMode mode)
//        {
//            TransactionStatus txStatus;

//            using (Transaction tx = new Transaction(doc, "Modify 3D View"))
//            {
//                tx.Start();

//                switch (mode)
//                {
//                    case VisualizationMode.AllElements:
//                        view3D.DisableTemporaryViewMode(TemporaryViewMode.TemporaryHideIsolate);
//                        foreach (Category category in doc.Settings.Categories)
//                        {
//                            if (view3D.CanCategoryBeHidden(category.Id))
//                            {
//                                view3D.SetCategoryHidden(category.Id, false);
//                            }
//                        }
//                        Category levelCategory = doc.Settings.Categories.get_Item(BuiltInCategory.OST_Levels);
//                        if (levelCategory != null)
//                        {
//                            view3D.SetCategoryHidden(levelCategory.Id, true);
//                        }
//                        break;

//                    case VisualizationMode.SelectedAndAdjacent:
//                        IList<SelectedElementId> SelectedAndAdjacentToIsolate = new List<SelectedElementId>(adjacentElementIds);
//                        SelectedAndAdjacentToIsolate.Add(selectedElementId);
//                        view3D.DisableTemporaryViewMode(TemporaryViewMode.TemporaryHideIsolate);
//                        view3D.IsolateElementsTemporary(SelectedAndAdjacentToIsolate);
//                        break;

//                    case VisualizationMode.SelectedOnly:
//                        IList<SelectedElementId> selectedElementOnly = new List<SelectedElementId> { selectedElementId };
//                        view3D.IsolateElementsTemporary(selectedElementOnly);
//                        break;
//                }

//                txStatus = tx.Commit();
//            }
//        }
//    }
//}

using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;  // For Button class

namespace RevitWoodLCC
{
    public partial class Preview3DForm : Window
    {
        private Document _doc;
        private View3D _view3D;
        private UIDocument _uiDoc;
        private ElementId _duplicatedViewId;
        private IList<ElementId> _elementsInDuplicatedView;
        private int _currentElementIndex = 0;
        private VisualizationMode currentMode = VisualizationMode.AllElements;
        private PreviewControl previewControl;

        public Preview3DForm(UIDocument uiDoc)
        {
            InitializeComponent();

            _doc = uiDoc.Document;
            _uiDoc = uiDoc;

            _view3D = GetActive3DViewAndDuplicate(_doc);
            if (_view3D != null)
            {
                InitializeElementsInDuplicatedView();
                AddPreviewControlToUI();
            }
            else
            {
                MessageBox.Show("No suitable 3D view found.");
                return;
            }

            // Handle the Closed event
            this.Closed += Preview3DForm_Closed;
        }

        private void Preview3DForm_Closed(object sender, EventArgs e)
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

        private void AddPreviewControlToUI()
        {
            if (_view3D != null)
            {
                previewControl = new PreviewControl(_doc, _view3D.Id);
                previewControl.VerticalAlignment = VerticalAlignment.Stretch;
                PreviewContainer.Children.Add(previewControl);
            }
        }

        private View3D GetActive3DViewAndDuplicate(Document doc)
        {
            View3D activeView3D = doc.ActiveView as View3D;

            if (activeView3D == null || activeView3D.IsTemplate || !activeView3D.CanBePrinted)
                return null;

            using (Transaction tx = new Transaction(doc, "Duplicate 3D View"))
            {
                tx.Start();

                ElementId duplicatedViewId = activeView3D.Duplicate(ViewDuplicateOption.WithDetailing);
                View3D duplicatedView3D = doc.GetElement(duplicatedViewId) as View3D;
                duplicatedView3D.Name = "Temporary Preview View";

                _duplicatedViewId = duplicatedViewId;

                tx.Commit();
                return duplicatedView3D;
            }
        }

        private void InitializeElementsInDuplicatedView()
        {
            FilteredElementCollector collector = new FilteredElementCollector(_doc, _view3D.Id);
            _elementsInDuplicatedView = collector.WhereElementIsNotElementType().ToElementIds().ToList();
            _currentElementIndex = _elementsInDuplicatedView.IndexOf(_uiDoc.Selection.GetElementIds().FirstOrDefault());
        }

        private void Toggle3DPreviewButton_Click(object sender, RoutedEventArgs e)
        {
            UIDocument uiDoc = _uiDoc;
            Document doc = uiDoc.Document;

            View3D view3D = doc.GetElement(_duplicatedViewId) as View3D;

            ElementId firstSelectedId = uiDoc.Selection.GetElementIds().FirstOrDefault();
            string elementIdAsString = firstSelectedId?.ToString();

            Button toggle3DPreviewButton = sender as Button;

            switch (currentMode)
            {
                case VisualizationMode.AllElements:
                    currentMode = VisualizationMode.SelectedOnly;
                    toggle3DPreviewButton.Content = "Show Selected and Adjacent";
                    break;
                case VisualizationMode.SelectedAndAdjacent:
                    currentMode = VisualizationMode.AllElements;
                    toggle3DPreviewButton.Content = "Show Selected Only";
                    break;
                case VisualizationMode.SelectedOnly:
                    currentMode = VisualizationMode.SelectedAndAdjacent;
                    toggle3DPreviewButton.Content = "Show All Elements";
                    break;
            }

            ElementId selectedElementId = new ElementId(Convert.ToInt32(elementIdAsString));
            IList<ElementId> adjacentElementIds = FindAdjacentElements(doc, selectedElementId);
            Modify3DView(_doc, _view3D, adjacentElementIds, selectedElementId, currentMode);
        }

        private void PreviousButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentElementIndex > 0)
            {
                _currentElementIndex--;
                SelectElement(_elementsInDuplicatedView[_currentElementIndex]);
            }
            else
            {
                MessageBox.Show("This is the first element.", "Navigation", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentElementIndex < _elementsInDuplicatedView.Count - 1)
            {
                _currentElementIndex++;
                SelectElement(_elementsInDuplicatedView[_currentElementIndex]);
            }
            else
            {
                MessageBox.Show("This is the last element.", "Navigation", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void SelectElement(ElementId elementId)
        {
            if (_uiDoc != null && elementId != ElementId.InvalidElementId)
            {
                _uiDoc.Selection.SetElementIds(new List<ElementId> { elementId });
                ZoomToElement(_uiDoc, elementId);
                _uiDoc.RefreshActiveView();
            }
        }

        private void ZoomToElement(UIDocument uiDocument, ElementId elementId)
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

        private enum VisualizationMode
        {
            AllElements,
            SelectedAndAdjacent,
            SelectedOnly
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
                        IList<ElementId> SelectedAndAdjacentToIsolate = new List<ElementId>(adjacentElementIds);
                        SelectedAndAdjacentToIsolate.Add(selectedElementId);
                        view3D.DisableTemporaryViewMode(TemporaryViewMode.TemporaryHideIsolate);
                        view3D.IsolateElementsTemporary(SelectedAndAdjacentToIsolate);
                        break;

                    case VisualizationMode.SelectedOnly:
                        IList<ElementId> selectedElementOnly = new List<ElementId> { selectedElementId };
                        view3D.IsolateElementsTemporary(selectedElementOnly);
                        break;
                }

                txStatus = tx.Commit();
            }
        }
    }
}

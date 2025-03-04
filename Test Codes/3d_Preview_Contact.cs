/*
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

namespace RevitWoodLCC
{
    [Transaction(TransactionMode.Manual)]
    public class PreviewContact : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;
            using (Transaction trans = new Transaction(doc, "Create 3D Preview"))
            {
                trans.Start();

                StringBuilder dialogContent = new StringBuilder();

                try
                {
                    // Prompt user to pick a single element
                    Reference pickedRef = uidoc.Selection.PickObject(ObjectType.Element, "Please select a solid element.");
                    Element pickedElement = doc.GetElement(pickedRef);

                    // Add details about the picked element to the dialog content
                    GeometryElement pickedGeomElem = pickedElement.get_Geometry(new Options());
                    foreach (GeometryObject geomObj in pickedGeomElem)
                    {
                        if (geomObj is Solid pickedSolid)
                        {
                            if (pickedSolid.Faces.Size > 0 && pickedSolid.Volume > 0)
                            {
                                dialogContent.AppendLine("Selected Solid with Element ID: " + pickedElement.Id + " has " + pickedSolid.Faces.Size + " faces and a volume of " + pickedSolid.Volume);
                            }
                        }
                        else if (geomObj is GeometryInstance pickedInstance)
                        {
                            GeometryElement pickedInstanceGeomElem = pickedInstance.GetSymbolGeometry();
                            foreach (GeometryObject pickedInstanceGeomObj in pickedInstanceGeomElem)
                            {
                                if (pickedInstanceGeomObj is Solid pickedInstanceSolid)
                                {
                                    if (pickedInstanceSolid.Faces.Size > 0 && pickedInstanceSolid.Volume > 0)
                                    {
                                        dialogContent.AppendLine("Selected Instance Solid with Element ID: " + pickedElement.Id + " has " + pickedInstanceSolid.Faces.Size + " faces and a volume of " + pickedInstanceSolid.Volume);
                                    }
                                }
                            }
                        }
                    }

                    // Use bounding box to identify elements in contact
                    BoundingBoxXYZ pickedBB = pickedElement.get_BoundingBox(null);
                    Outline outline = new Outline(pickedBB.Min, pickedBB.Max);

                    // Create a filter using the BoundingBoxIntersectsFilter
                    BoundingBoxIntersectsFilter bbFilter = new BoundingBoxIntersectsFilter(outline);
                    FilteredElementCollector collector = new FilteredElementCollector(doc).WherePasses(bbFilter);

                    // List to store elements that are in contact with the picked element
                    List<Element> contactingElements = new List<Element>();
                    List<SelectedElementId> contactingElementIds = new List<SelectedElementId>();

                    BoundingBoxXYZ sectionBox = new BoundingBoxXYZ();
                    sectionBox.Min = pickedBB.Min;
                    sectionBox.Max = pickedBB.Max;

                    foreach (Element e in collector)
                    {
                        if (e.Id != pickedElement.Id)
                        {
                            // Process the geometry for each element
                            GeometryElement geomElem = e.get_Geometry(new Options());
                            if (geomElem != null)
                            {
                                foreach (GeometryObject geomObj in geomElem)
                                {
                                    if (geomObj is Solid solid)
                                    {
                                        if (solid.Faces.Size > 0 && solid.Volume > 0)
                                        {
                                            // Process solid
                                            int faceCount = solid.Faces.Size;
                                            double volume = solid.Volume;
                                            dialogContent.AppendLine("Solid with Element ID: " + e.Id + " has " + faceCount + " faces and a volume of " + volume);
                                        }
                                    }
                                    else if (geomObj is GeometryInstance instance)
                                    {
                                        GeometryElement instanceGeomElem = instance.GetSymbolGeometry();
                                        foreach (GeometryObject instanceGeomObj in instanceGeomElem)
                                        {
                                            if (instanceGeomObj is Solid instanceSolid)
                                            {
                                                if (instanceSolid.Faces.Size > 0 && instanceSolid.Volume > 0)
                                                {
                                                    // Process instance solid
                                                    int faceCount = instanceSolid.Faces.Size;
                                                    double volume = instanceSolid.Volume;
                                                    dialogContent.AppendLine("Instance Solid with Element ID: " + e.Id + " has " + faceCount + " faces and a volume of " + volume);
                                                }
                                            }
                                        }
                                    }
                                }
                            }

                            // Add the element to the list of contacting elements
                            contactingElements.Add(e);
                            contactingElementIds.Add(e.Id);

                            // Extend the section box to include this element
                            BoundingBoxXYZ eBB = e.get_BoundingBox(null);
                            sectionBox.Min = new XYZ(Math.Min(sectionBox.Min.X, eBB.Min.X), Math.Min(sectionBox.Min.Y, eBB.Min.Y), Math.Min(sectionBox.Min.Z, eBB.Min.Z));
                            sectionBox.Max = new XYZ(Math.Max(sectionBox.Max.X, eBB.Max.X), Math.Max(sectionBox.Max.Y, eBB.Max.Y), Math.Max(sectionBox.Max.Z, eBB.Max.Z));
                        }
                    }

                    // Display list of contacting elements
                    dialogContent.AppendLine("Elements in contact with the selected element have the following IDs: " + string.Join(", ", contactingElementIds));
                    TaskDialog.Show("Contacting Elements and Their Solids", dialogContent.ToString());


                    // Declare the View3D variable outside the transaction block
                    View3D view3D = null;

                    using (Transaction innerTrans = new Transaction(doc, "Inner Create 3D Preview"))
                    {
                        innerTrans.Start();
                        // Create the 3D view here and assign it to view3D
                        view3D = View3D.CreateIsometric(doc, new FilteredElementCollector(doc).OfClass(typeof(ViewFamilyType)).Cast<ViewFamilyType>().FirstOrDefault(x => x.ViewFamily == ViewFamily.ThreeDimensional).Id);
                        view3D.Name = "3D Preview - " + DateTime.Now.ToString("yyyyMMddHHmmss");

                        // Hide all elements in the 3D view
                        ICollection<SelectedElementId> allElementIdsInView = new FilteredElementCollector(doc, view3D.Id).ToElementIds();
                        ICollection<SelectedElementId> elementsToHide = new List<SelectedElementId>(allElementIdsInView);
                        foreach (SelectedElementId id in contactingElementIds)
                        {
                            elementsToHide.Remove(id);
                        }
                        elementsToHide.Remove(pickedElement.Id);

                        // Filter out elements that cannot be hidden
                        List<SelectedElementId> filteredElementsToHide = new List<SelectedElementId>();
                        foreach (SelectedElementId id in elementsToHide)
                        {
                            Element elem = doc.GetElement(id);
                            if (elem.CanBeHidden(view3D))
                            {
                                filteredElementsToHide.Add(id);
                            }
                        }

                        view3D.HideElements(filteredElementsToHide);

                        // Activate and set the crop box to function as a section box
                        view3D.CropBoxActive = true;
                        view3D.CropBox = sectionBox;
                        innerTrans.Commit();
                    }

                    uidoc.ActiveView = view3D;

                    // Commit transaction
                    trans.Commit();



                    // Display list of contacting elements
                    TaskDialog.Show("Contacting Elements and Their Solids", dialogContent.ToString());

                    using (Transaction delTrans = new Transaction(doc, "Delete Temporary 3D View"))
                    {
                        delTrans.Start();
                        doc.Delete(view3D.Id);
                        delTrans.Commit();
                    }

                    return Result.Succeeded;

                }
                catch (Exception ex)
                {
                    trans.RollBack();
                    message = ex.Message;
                    return Result.Failed;
                }
            }
        }
    }

}*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;

namespace RevitWoodLCC
{
    [Transaction(TransactionMode.Manual)]
    public class PreviewContact : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;

            try
            {
                Element pickedElement = PickElement(uidoc);
                List<ElementId> contactingElementIds = GetContactingElements(doc, pickedElement);

                // Create a 3D view
                View3D view3D = Create3DView(doc);

                // Adjust the 3D view's section box to focus on selected and contacting elements
                //Adjust3DViewSectionBox(doc, view3D, pickedElement, contactingElementIds);

                // Hide unrelated elements
                HideElements(doc, view3D, pickedElement.Id, contactingElementIds);

                uidoc.ActiveView = view3D;
                uidoc.RefreshActiveView();

                // Uncomment the following line if you want to delete the temporary view after some operation
                // DeleteTemporaryView(doc, view3D);

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }

        private Element PickElement(UIDocument uidoc)
        {
            Reference pickedRef = uidoc.Selection.PickObject(ObjectType.Element, "Please select a solid element.");
            Element pickedElement = uidoc.Document.GetElement(pickedRef);

            // Call the ProcessGeometryDetails method to get the geometry details
            StringBuilder dialogContent = ProcessGeometryDetails(pickedElement);

            // Display the information in a Task Dialog
            TaskDialog.Show("Selected Element", dialogContent.ToString()); 

            return pickedElement;
        }

        private StringBuilder ProcessGeometryDetails(Element element)
        {
            StringBuilder dialogContent = new StringBuilder();
            GeometryElement geomElem = element.get_Geometry(new Options());

            foreach (GeometryObject geomObj in geomElem)
            {
                if (geomObj is Solid solid)
                {
                    if (solid.Faces.Size > 0 && solid.Volume > 0)
                    {
                        dialogContent.AppendLine("Solid with Element ID: " + element.Id + " has " + solid.Faces.Size + " faces and a volume of " + solid.Volume);
                    }
                }
                else if (geomObj is GeometryInstance instance)
                {
                    GeometryElement instanceGeomElem = instance.GetSymbolGeometry();
                    foreach (GeometryObject instanceGeomObj in instanceGeomElem)
                    {
                        if (instanceGeomObj is Solid instanceSolid)
                        {
                            if (instanceSolid.Faces.Size > 0 && instanceSolid.Volume > 0)
                            {
                                dialogContent.AppendLine("Instance Solid with Element ID: " + element.Id + " has " + instanceSolid.Faces.Size + " faces and a volume of " + instanceSolid.Volume);
                            }
                        }
                    }
                }
            }
            return dialogContent;
        }


        private List<ElementId> GetContactingElements(Document doc, Element pickedElement)
        {
            StringBuilder dialogContent = new StringBuilder();

            BoundingBoxXYZ pickedBB = pickedElement.get_BoundingBox(null);
            Outline outline = new Outline(pickedBB.Min, pickedBB.Max);
            BoundingBoxIntersectsFilter bbFilter = new BoundingBoxIntersectsFilter(outline);
            FilteredElementCollector collector = new FilteredElementCollector(doc).WherePasses(bbFilter);

            List<ElementId> contactingElementIds = new List<ElementId>();

            foreach (Element e in collector)
            {
                if (e.Id != pickedElement.Id)
                {
                    GeometryElement geomElem = e.get_Geometry(new Options());
                    if (geomElem != null)
                    {
                        foreach (GeometryObject geomObj in geomElem)
                        {
                            if (geomObj is Solid || geomObj is GeometryInstance)
                            {
                                contactingElementIds.Add(e.Id);
                                break;
                            }
                        }
                    }
                }
            }

            dialogContent.AppendLine("Elements in contact with the selected element have the following IDs: " + string.Join(", ", contactingElementIds));
            TaskDialog.Show("Contacting Elements", dialogContent.ToString());

            return contactingElementIds;


        }

        private View3D Create3DView(Document doc)
        {
            try {
                // Create a 3D view
                ViewFamilyType viewFamilyType = new FilteredElementCollector(doc)
                    .OfClass(typeof(ViewFamilyType))
                    .Cast<ViewFamilyType>()
                    .FirstOrDefault(x => x.ViewFamily == ViewFamily.ThreeDimensional);

                if (viewFamilyType != null)
                {
                    Transaction trans = new Transaction(doc, "Create 3D View");
                    trans.Start();
                    View3D view3D = View3D.CreateIsometric(doc, viewFamilyType.Id);
                    trans.Commit();
                    return view3D;
                }
                else
                {
                    TaskDialog.Show("Error", "ViewFamilyType for 3D view not found.");

                    // Handle the case where the ViewFamilyType is not found
                    return null;
                }
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Error", ex.Message);
                return null;
            }
    }

        private void HideElements(Document doc, View3D view3D, ElementId pickedElementId, List<ElementId> contactingElementIds)
        {
            Transaction trans = new Transaction(doc, "Hide Elements");
            trans.Start();

            // Create a new set of element ids to hide
            ICollection<ElementId> toHide = new FilteredElementCollector(doc, view3D.Id)
                .ToElementIds()
                .Where(id => id != pickedElementId && !contactingElementIds.Contains(id))
                .ToList();

            // Hide elements
            view3D.HideElementsTemporary(toHide);

            trans.Commit();
        }

        private void DeleteTemporaryView(Document doc, View3D view3D)
        {
            Transaction trans = new Transaction(doc, "Delete Temporary View");
            trans.Start();
            doc.Delete(view3D.Id);
            trans.Commit();
        }

       


        }
    }
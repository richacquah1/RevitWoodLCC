//this code is used to get the face reference of an element in revit
using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Text;

namespace RevitWoodLCC
{
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public class GetFaceRefFromElements : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;

            try
            {
                TaskDialog taskDialog = new TaskDialog("Select Option");
                taskDialog.MainInstruction = "Choose an option";
                taskDialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink1, "Select Face");
                taskDialog.AddCommandLink(TaskDialogCommandLinkId.CommandLink2, "Select Element");

                TaskDialogResult result = taskDialog.Show();

                if (result == TaskDialogResult.CommandLink1)
                {
                    SelectFace(uidoc, doc);
                }
                else if (result == TaskDialogResult.CommandLink2)
                {
                    ListElementFaces(uidoc, doc);
                }
                else
                {
                    return Result.Cancelled;
                }
            }
            catch (OperationCanceledException)
            {
                return Result.Cancelled;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }

            return Result.Succeeded;
        }

        private void SelectFace(UIDocument uidoc, Document doc)
        {
            Reference faceReference = uidoc.Selection.PickObject(ObjectType.Face, "Select a face");
            if (faceReference != null)
            {
                TaskDialog.Show("Face Reference", faceReference.ConvertToStableRepresentation(doc));
            }
        }

        private void ListElementFaces(UIDocument uidoc, Document doc)
        {
            Reference elementReference = uidoc.Selection.PickObject(ObjectType.Element, "Select an element");
            if (elementReference != null)
            {
                Element element = doc.GetElement(elementReference.ElementId);
                Options options = new Options
                {
                    ComputeReferences = true,
                    DetailLevel = ViewDetailLevel.Fine
                };
                GeometryElement geomElement = element.get_Geometry(options);

                List<string> faceReferences = new List<string>();

                foreach (GeometryObject geomObj in geomElement)
                {
                    if (geomObj is Solid solid)
                    {
                        foreach (Face face in solid.Faces)
                        {
                            Reference faceRef = face.Reference;
                            if (faceRef != null)
                            {
                                faceReferences.Add(faceRef.ConvertToStableRepresentation(doc));
                            }
                        }
                    }
                    else if (geomObj is GeometryInstance geomInstance)
                    {
                        GeometryElement instanceGeom = geomInstance.GetInstanceGeometry();
                        foreach (GeometryObject instObj in instanceGeom)
                        {
                            Solid instSolid = instObj as Solid;
                            if (instSolid != null)
                            {
                                foreach (Face face in instSolid.Faces)
                                {
                                    Reference faceRef = face.Reference;
                                    if (faceRef != null)
                                    {
                                        faceReferences.Add(faceRef.ConvertToStableRepresentation(doc));
                                    }
                                }
                            }
                        }
                    }
                }

                StringBuilder sb = new StringBuilder();
                sb.AppendLine("Face References:");
                foreach (var faceRef in faceReferences)
                {
                    sb.AppendLine(faceRef);
                }

                if (faceReferences.Count == 0)
                {
                    sb.AppendLine("No faces found.");
                }

                TaskDialog.Show("Element Faces", sb.ToString());
            }
        }
    }
}

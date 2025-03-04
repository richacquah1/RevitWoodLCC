
//This works perfect with the SLP FORM
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.Collections.Generic;
using System.Linq;
using System;


namespace RevitWoodLCC
{
    [Transaction(TransactionMode.Manual)]
    public class ServiceLifeEstimation : IExternalCommand
    {

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                if (commandData == null)
                {
                    message = "ExternalCommandData is null.";
                    return Result.Failed;
                }

                UIDocument uiDoc = commandData.Application.ActiveUIDocument;
                if (uiDoc == null || uiDoc.Document == null)
                {
                    message = "Active document is null.";
                    return Result.Failed;
                }

                ICollection<ElementId> selectedIds = uiDoc.Selection.GetElementIds();
                if (selectedIds.Count == 0)
                {
                    message = "No elements selected.";
                    return Result.Failed;
                }

                View3D view3D = GetFirst3DView(uiDoc.Document);
                if (view3D == null)
                {
                    message = "No suitable 3D view found.";
                    return Result.Failed;
                }

                ElementId firstId = selectedIds.First();
                Element firstElement = uiDoc.Document.GetElement(firstId);
                if (firstElement == null)
                {
                    message = "The first selected element is null.";
                    return Result.Failed;
                }

                SLE_PopupForm popup = new SLE_PopupForm(uiDoc, commandData);
                if (popup == null)
                {
                    message = "Failed to create the popup form.";
                    return Result.Failed;
                }

                try
                {
                    PreFillFormWithData(popup, firstElement, uiDoc.Document);
                }
                catch (Exception ex)
                {
                    message = $"Failed to pre-fill the form: {ex.Message}";
                    return Result.Failed;
                }

                popup.ShowDialog();
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = $"Unhandled exception: {ex.Message}\n{ex.StackTrace}";
                return Result.Failed;
            }
        }






        private string GetElementMaterial(Element element, Document doc)
        {
            // Define the parameter names you want to check
            string[] parameterNames = new string[] { "Materials and Finishes", "Structural Material" };

            foreach (var paramName in parameterNames)
            {
                Parameter matParam = element.LookupParameter(paramName);
                if (matParam != null && matParam.HasValue)
                {
                    // If it's a material element id, we need to get the material from the document
                    if (matParam.StorageType == StorageType.ElementId)
                    {
                        Material material = doc.GetElement(matParam.AsElementId()) as Material;
                        if (material != null)
                        {
                            return material.Name;
                        }
                    }
                    // If the material is stored as a string, we can just return it
                    else if (matParam.StorageType == StorageType.String)
                    {
                        return matParam.AsString();
                    }
                }
            }

            // Fallback to other methods if specific "Materials and Finishes" or "Structural Material" is not found or not set
            ICollection<ElementId> materialIds = element.GetMaterialIds(false);
            if (materialIds.Count > 0)
            {
                // Get the first material's name
                ElementId materialId = materialIds.First();
                Material material = doc.GetElement(materialId) as Material;
                if (material != null)
                {
                    return material.Name;
                }
            }
            // If no material is found, return a default or null
            return null;
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
            if (form == null)
            {
                throw new ArgumentNullException(nameof(form), "Form is null");
            }

            if (element == null)
            {
                throw new ArgumentNullException(nameof(element), "Element is null");
            }

            if (doc == null)
            {
                throw new ArgumentNullException(nameof(doc), "Document is null");
            }

            string material = GetElementMaterial(element, doc);
            string location = GetProjectLocation(doc);

            form.SetMaterialField(material);
            //form.SetLocationField(location); // Ensure this method exists and works
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
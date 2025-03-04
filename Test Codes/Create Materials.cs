//using Autodesk.Revit.Attributes;
//using Autodesk.Revit.DB;
//using Autodesk.Revit.UI;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text.RegularExpressions;

//namespace RevitWoodLCC
//{
//    [Transaction(TransactionMode.Manual)]
//    public class CreateMaterials : IExternalCommand
//    {
//        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
//        {
//            UIApplication uiApp = commandData.Application;
//            Document doc = uiApp.ActiveUIDocument.Document;

//            List<string> materialNames = new List<string>
//            {
//                "Additional structural wooden beams",
//                "Bottom concrete - 10 cm",
//                "Ceramic plates - 1 cm",
//                "CLT plates, coated - 10 cm",
//                "Concrete screed C20/25 with floor heating pipes - 7.6 cm",
//                "Glue - 0.3 cm",
//                "Glue for ceramic plates - 0.5 cm",
//                "Gypsum fiberboard - 1.5 cm",
//                "Gypsum plasterboards - 1.25 cm",
//                "Hard mineral wool acoustic insulation - 3 cm",
//                "Hard mineral wool acoustic insulation - 4 cm",
//                "Hydro isolation: polymer-bitumen, one layer Like ORION FC 160 - 0.4 cm",
//                "Hydro-isolating layer - 0.3 cm",
//                "Load bearing construction profiles - 16/8 cm",
//                "Load bearing construction profiles - 6/10 cm - 10 cm",
//                "Load bearing construction profiles (rafters) 16/8 cm - 16 cm",
//                "Load bearing construction profiles 20x12 cm - 20 cm",
//                "Mineral wool between the load bearing construction profiles - 16 cm",
//                "Mineral wool between the load bearing construction profiles - 20 cm",
//                "OSB plate - 1.2 cm",
//                "OSB plates - 1.5 cm",
//                "Parquet - 1.1 cm",
//                "PE foil - 0.2 mm",
//                "Reinforced ALU foil - 0.2 cm",
//                "Reinforced concrete - 16 cm",
//                "Reinforced concrete - 25 cm",
//                "Reinforcing mortar, mesh and finishing plaster - 0.6 cm",
//                "Roof cover: wave fiber cement roof tiles - 0.5 cm",
//                "Roof foil like Tyvek, Eternit Meteo or similar - 0.2 cm",
//                "Stone wool - 10 cm",
//                "Stone wool like Knauf Insulation DP-5 Venti (between wooden construction) - 10 cm",
//                "Stone wool like Rockwool Roofrock - 10 cm",
//                "Thin hydro-isolating layer based on hydraulic binders and elastomer additives - 0.3 cm",
//                "Wooden boards - 2 cm",
//                "Wooden laths 3/2 cm - 2 cm",
//                "Wooden laths 3x4 cm - 3 cm",
//                "Wooden laths 5x5 cm - 5 cm",
//                "Wooden laths in opposite direction 3x5 cm - 3 cm",
//                "XPS insulation - 12 cm",
//                "XPS insulation - 15 cm"
//            };

//            List<string> successfulMaterials = new List<string>();
//            List<string> problematicMaterials = new List<string>();

//            using (Transaction tx = new Transaction(doc, "Create and Set Properties of Materials"))
//            {
//                try
//                {
//                    tx.Start();

//                    foreach (var originalName in materialNames)
//                    {
//                        string sanitizedMaterialName = SanitizeMaterialName(originalName);
//                        if (IsValidMaterialName(sanitizedMaterialName))
//                        {
//                            if (CreateAndSetMaterialProperties(doc, sanitizedMaterialName))
//                            {
//                                successfulMaterials.Add(sanitizedMaterialName);
//                            }
//                            else
//                            {
//                                problematicMaterials.Add(sanitizedMaterialName + " (creation/setting properties failed)");
//                            }
//                        }
//                        else
//                        {
//                            problematicMaterials.Add(sanitizedMaterialName + " (invalid name)");
//                        }
//                    }

//                    tx.Commit();

//                    DisplayResults(successfulMaterials, problematicMaterials);
//                    return Result.Succeeded;
//                }
//                catch (Exception ex)
//                {
//                    message = ex.Message;
//                    if (tx.HasStarted())
//                        tx.RollBack();
//                    return Result.Failed;
//                }
//            }
//        }

//        private bool CreateAndSetMaterialProperties(Document doc, string materialName)
//        {
//            Material material = new FilteredElementCollector(doc)
//                                .OfClass(typeof(Material))
//                                .Cast<Material>()
//                                .FirstOrDefault(m => m.Name.Equals(materialName));

//            if (material == null)
//            {
//                SelectedElementId materialId = Material.Create(doc, materialName);
//                if (materialId != SelectedElementId.InvalidElementId)
//                {
//                    material = doc.GetElement(materialId) as Material;
//                    if (material != null)
//                    {
//                        // Set thermal and physical properties
//                        SetMaterialProperties(doc, material);
//                        return true;
//                    }
//                }
//            }
//            return false; // Material already exists
//        }

//        private void SetMaterialProperties(Document doc, Material material)
//        {
//            // Set thermal conductivity, density, and specific heat
//            SetMaterialParameter(material, "Thermal Conductivity", 0.24); // W/(m·K)
//            SetMaterialParameter(material, "Density", 7850); // kg/m³
//            SetMaterialParameter(material, "Specific Heat", 500); // J/(kg·K)
//        }

//        private void SetMaterialParameter(Material material, string paramName, double value)
//        {
//            Parameter param = material.LookupParameter(paramName);
//            if (param != null && !param.IsReadOnly)
//            {
//                // Revit parameters may require the value in a different unit system (e.g., Imperial units)
//                // You might need to convert these values appropriately depending on your Revit setup
//                param.Set(value);
//            }
//        }


//        private string SanitizeMaterialName(string originalName)
//        {
//            return Regex.Replace(originalName, "[{}\\[\\]|;<>?:`,~]", "-");
//        }

//        private bool IsValidMaterialName(string name)
//        {
//            // Further validation to check if any prohibited characters remain
//            return !Regex.IsMatch(name, "[{}\\[\\]|;<>?:`,~]");
//        }

//        private void DisplayResults(List<string> successfulMaterials, List<string> problematicMaterials)
//        {
//            string successMessage = "Successfully created materials:\n" + string.Join("\n", successfulMaterials);
//            string failureMessage = "Problems with these materials:\n" + string.Join("\n", problematicMaterials);

//            // Show results in a Task Dialog or similar UI component
//            TaskDialog dialog = new TaskDialog("Material Creation Results")
//            {
//                MainInstruction = "Material creation summary",
//                MainContent = successMessage + "\n\n" + failureMessage,
//                CommonButtons = TaskDialogCommonButtons.Ok
//            };
//            dialog.Show();
//        }
//    }
//}


using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace RevitWoodLCC
{
    [Transaction(TransactionMode.Manual)]
    public class CreateMaterials : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiApp = commandData.Application;
            Document doc = uiApp.ActiveUIDocument.Document;

            List<string> materialNames = new List<string>
            {
                "Additional structural wooden beams",
                "Bottom concrete - 10 cm",
                "Ceramic plates - 1 cm",
                "CLT plates, coated - 10 cm",
                "Concrete screed C20/25 with floor heating pipes - 7.6 cm",
                "Glue - 0.3 cm",
                "Glue for ceramic plates - 0.5 cm",
                "Gypsum fiberboard - 1.5 cm",
                "Gypsum plasterboards - 1.25 cm",
                "Hard mineral wool acoustic insulation - 3 cm",
                "Hard mineral wool acoustic insulation - 4 cm",
                "Hydro isolation: polymer-bitumen, one layer Like ORION FC 160 - 0.4 cm",
                "Hydro-isolating layer - 0.3 cm",
                "Load bearing construction profiles - 16/8 cm",
                "Load bearing construction profiles - 6/10 cm - 10 cm",
                "Load bearing construction profiles (rafters) 16/8 cm - 16 cm",
                "Load bearing construction profiles 20x12 cm - 20 cm",
                "Mineral wool between the load bearing construction profiles - 16 cm",
                "Mineral wool between the load bearing construction profiles - 20 cm",
                "OSB plate - 1.2 cm",
                "OSB plates - 1.5 cm",
                "Parquet - 1.1 cm",
                "PE foil - 0.2 mm",
                "Reinforced ALU foil - 0.2 cm",
                "Reinforced concrete - 16 cm",
                "Reinforced concrete - 25 cm",
                "Reinforcing mortar, mesh and finishing plaster - 0.6 cm",
                "Roof cover: wave fiber cement roof tiles - 0.5 cm",
                "Roof foil like Tyvek, Eternit Meteo or similar - 0.2 cm",
                "Stone wool - 10 cm",
                "Stone wool like Knauf Insulation DP-5 Venti (between wooden construction) - 10 cm",
                "Stone wool like Rockwool Roofrock - 10 cm",
                "Thin hydro-isolating layer based on hydraulic binders and elastomer additives - 0.3 cm",
                "Wooden boards - 2 cm",
                "Wooden laths 3/2 cm - 2 cm",
                "Wooden laths 3x4 cm - 3 cm",
                "Wooden laths 5x5 cm - 5 cm",
                "Wooden laths in opposite direction 3x5 cm - 3 cm",
                "XPS insulation - 12 cm",
                "XPS insulation - 15 cm"
            };

            using (Transaction tx = new Transaction(doc, "Create and Set Properties of Materials"))
            {
                tx.Start();

                foreach (var originalName in materialNames)
                {
                    string sanitizedMaterialName = SanitizeMaterialName(originalName);
                    if (IsValidMaterialName(sanitizedMaterialName))
                    {
                        if (!CreateAndAssignProperties(doc, sanitizedMaterialName))
                        {
                            message += $"Failed to create or assign properties for {sanitizedMaterialName}\n";
                        }
                    }
                    else
                    {
                        message += $"Invalid material name: {sanitizedMaterialName}\n";
                    }
                }

                tx.Commit();
            }

            if (!string.IsNullOrEmpty(message))
            {
                TaskDialog.Show("Material Creation Results", message);
                return Result.Failed;
            }

            return Result.Succeeded;
        }

        private bool CreateAndAssignProperties(Document doc, string materialName)
        {
            Material material = new FilteredElementCollector(doc)
                                .OfClass(typeof(Material))
                                .Cast<Material>()
                                .FirstOrDefault(m => m.Name.Equals(materialName));

            if (material == null)
            {
                ElementId materialId = Material.Create(doc, materialName);
                material = (Material)doc.GetElement(materialId);
            }

            if (material != null)
            {
                // Assign Thermal Properties
                ThermalAsset thermalAsset = new ThermalAsset("Thermal Properties for " + materialName, ThermalMaterialType.Solid);
                PropertySetElement thermalPse = PropertySetElement.Create(doc, thermalAsset);
                material.SetMaterialAspectByPropertySet(MaterialAspect.Thermal, thermalPse.Id);

                // Assign Physical Properties
                StructuralAsset physicalAsset = new StructuralAsset("Physical Properties for " + materialName, StructuralAssetClass.Generic);
                PropertySetElement physicalPse = PropertySetElement.Create(doc, physicalAsset);
                material.SetMaterialAspectByPropertySet(MaterialAspect.Structural, physicalPse.Id);

                return true;
            }

            return false;
        }

        private string SanitizeMaterialName(string originalName)
        {
            return Regex.Replace(originalName, "[{}\\[\\]|;<>?:`,~]", "-");
        }

        private bool IsValidMaterialName(string name)
        {
            return !Regex.IsMatch(name, "[{}\\[\\]|;<>?:`,~]");
        }
    }
}

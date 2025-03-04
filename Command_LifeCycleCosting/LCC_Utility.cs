using Autodesk.Revit.DB;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RevitWoodLCC
{
    public static class LCC_Utility
    {
        // Method to retrieve material from an element
        public static Material GetElementMaterial(Element element, Document doc)
        {
            // Define the parameter names you want to check
            string[] parameterNames = new string[] { "Material", "Structural Material", "Finish Material", "Surface Material" };

            foreach (var paramName in parameterNames)
            {
                Parameter matParam = element.LookupParameter(paramName);
                if (matParam != null && matParam.HasValue)
                {
                    if (matParam.StorageType == StorageType.ElementId)
                    {
                        ElementId materialId = matParam.AsElementId();
                        Material material = doc.GetElement(materialId) as Material;
                        if (material != null)
                        {
                            return material;
                        }
                    }
                    else if (matParam.StorageType == StorageType.String)
                    {
                        string materialName = matParam.AsString();
                        if (!string.IsNullOrEmpty(materialName))
                        {
                            FilteredElementCollector collector = new FilteredElementCollector(doc)
                                .OfClass(typeof(Material))
                                .WhereElementIsElementType();

                            Material material = collector
                                .Cast<Material>()
                                .FirstOrDefault(m => m.Name.Equals(materialName, StringComparison.OrdinalIgnoreCase));

                            if (material != null)
                            {
                                return material;
                            }
                        }
                    }
                }
            }

            // Fallback to geometry-based material retrieval
            Options geomOptions = new Options();
            GeometryElement geomElement = element.get_Geometry(geomOptions);
            if (geomElement != null)
            {
                foreach (GeometryObject geomObject in geomElement)
                {
                    if (geomObject is Solid solid)
                    {
                        Material material = GetMaterialFromSolid(doc, solid);
                        if (material != null)
                        {
                            return material;
                        }
                    }
                    else if (geomObject is GeometryInstance geomInstance)
                    {
                        GeometryElement instanceGeomElement = geomInstance.GetInstanceGeometry();
                        foreach (GeometryObject instanceGeomObject in instanceGeomElement)
                        {
                            if (instanceGeomObject is Solid instanceSolid)
                            {
                                Material material = GetMaterialFromSolid(doc, instanceSolid);
                                if (material != null)
                                {
                                    return material;
                                }
                            }
                        }
                    }
                }
            }

            // If no material is found, return null
            return null;
        }

        private static Material GetMaterialFromSolid(Document doc, Solid solid)
        {
            foreach (Face face in solid.Faces)
            {
                ElementId materialId = face.MaterialElementId;
                Material material = doc.GetElement(materialId) as Material;
                if (material != null)
                {
                    return material;
                }
            }
            return null;
        }

        // Method to convert parameter value from internal units to specified Forge type units
        public static double GetParameterAsDoubleConverted(Element element, string parameterName, ForgeTypeId targetUnitType)
        {
            Parameter parameter = element.LookupParameter(parameterName);
            if (parameter != null && parameter.HasValue)
            {
                return UnitUtils.ConvertFromInternalUnits(parameter.AsDouble(), targetUnitType);
            }
            return 0; // Default value if parameter not found or has no value
        }

        // Method to get double parameter values
        public static double GetParameterAsDouble(Element element, string parameterName)
        {
            Parameter parameter = element.LookupParameter(parameterName);
            if (parameter != null && parameter.HasValue)
                return parameter.AsDouble();
            return 0.0; // Default value if parameter not found or has no value
        }

        // Method to get integer parameter values
        public static int GetParameterAsInteger(Element element, string parameterName)
        {
            Parameter parameter = element.LookupParameter(parameterName);
            if (parameter != null && parameter.HasValue)
                return parameter.AsInteger();
            return 0; // Default value if parameter not found or has no value
        }

        // Method to get string parameters safely
        public static string GetParameterAsString(Element element, string parameterName, string defaultValue = "")
        {
            Parameter parameter = element.LookupParameter(parameterName);
            return parameter != null && parameter.HasValue ? parameter.AsString() : defaultValue;
        }

        // Method to get the volume of an element in cubic meters
        public static double GetMaterialVolume(Element element)
        {
            Parameter volumeParam = element.LookupParameter("Volume");
            return volumeParam != null ? UnitUtils.ConvertFromInternalUnits(volumeParam.AsDouble(), UnitTypeId.CubicMeters) : 0;
        }
    }
}

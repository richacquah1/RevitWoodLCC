//using Autodesk.Revit.Attributes;
//using Autodesk.Revit.DB;
//using Autodesk.Revit.DB.Architecture;
//using Autodesk.Revit.DB.Visual;
//using Autodesk.Revit.UI;
//using Newtonsoft.Json;
//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Reflection;

//namespace RevitWoodLCC
//{
//    [Transaction(TransactionMode.Manual)]
//    public class CreateElement : IExternalCommand
//    {
//        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
//        {
//            UIApplication uiApp = commandData.Application;
//            Document doc = uiApp.ActiveUIDocument.Document;

//            // Get the current assembly
//            Assembly assembly = Assembly.GetExecutingAssembly();

//            // Construct the correct resource name
//            string resourceName = "RevitWoodLCC.assets.Create_Revit_Elements.json";

//            // Read the embedded JSON file
//            string jsonContent;
//            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
//            {
//                if (stream == null)
//                {
//                    message = $"Resource '{resourceName}' not found.";
//                    return Result.Failed;
//                }

//                using (StreamReader reader = new StreamReader(stream))
//                {
//                    jsonContent = reader.ReadToEnd();
//                }
//            }

//            // Deserialize JSON content into the wrapper class
//            RevitElementWrapper elementWrapper = JsonConvert.DeserializeObject<RevitElementWrapper>(jsonContent);

//            // Begin a transaction for creating elements
//            using (Transaction tx = new Transaction(doc, "Create Elements"))
//            {
//                tx.Start();
//                try
//                {
//                    // Process WallTypes
//                    foreach (var wallTypeData in elementWrapper.WallTypes)
//                    {
//                        WallType wallType = CreateWallType(doc, wallTypeData);
//                        //PlaceWall(doc, wallType, wallTypeData); // Pass WallTypeData here
//                    }

//                    // Process FloorTypes
//                    foreach (var floorTypeData in elementWrapper.FloorTypes)
//                    {
//                        FloorType floorType = CreateFloorType(doc, floorTypeData);
//                        //PlaceFloor(doc, floorType, floorTypeData); // Pass FloorTypeData here
//                    }

//                    // Process RoofTypes
//                    foreach (var roofTypeData in elementWrapper.RoofTypes)
//                    {
//                        RoofType roofType = CreateRoofType(doc, roofTypeData);
//                        //PlaceRoof(doc, roofType, roofTypeData); // Pass RoofTypeData here
//                    }


//                    tx.Commit();
//                    // Display a confirmation message to the user
//                    TaskDialog.Show("Creation Result", $"{elementWrapper.WallTypes.Count} Wall Types, {elementWrapper.FloorTypes.Count} Floor Types, and {elementWrapper.RoofTypes.Count} Roof Types created.");

//                    return Result.Succeeded;
//                }
//                catch (Exception ex)
//                {
//                    message = ex.Message;
//                    tx.RollBack();
//                    return Result.Failed;
//                }
//            }
//        }

//        private WallType CreateWallType(Document doc, WallTypeData wallTypeData)
//        {
//            // Try to find an existing WallType by name
//            WallType wallType = new FilteredElementCollector(doc)
//                .OfClass(typeof(WallType))
//                .Cast<WallType>()
//                .FirstOrDefault(wt => wt.Name.Equals(wallTypeData.Name));

//            if (wallType == null)
//            {
//                // Duplicate an existing WallType if the specified one doesn't exist
//                wallType = new FilteredElementCollector(doc)
//                    .OfClass(typeof(WallType))
//                    .Cast<WallType>()
//                    .FirstOrDefault();

//                if (wallType != null)
//                {
//                    wallType = wallType.Duplicate(wallTypeData.Name) as WallType;
//                }
//                else
//                {
//                    throw new InvalidOperationException("No wall types available to duplicate.");
//                }
//            }

//            // Apply wall layers and materials here based on wallTypeData.Layers

//            return wallType;
//        }

//        private FloorType CreateFloorType(Document doc, FloorTypeData floorTypeData)
//        {
//            // Try to find an existing FloorType by name
//            FloorType floorType = new FilteredElementCollector(doc)
//                .OfClass(typeof(FloorType))
//                .Cast<FloorType>()
//                .FirstOrDefault(ft => ft.Name.Equals(floorTypeData.Name));

//            if (floorType == null)
//            {
//                // Duplicate an existing FloorType if the specified one doesn't exist
//                floorType = new FilteredElementCollector(doc)
//                    .OfClass(typeof(FloorType))
//                    .Cast<FloorType>()
//                    .FirstOrDefault();

//                if (floorType != null)
//                {
//                    floorType = floorType.Duplicate(floorTypeData.Name) as FloorType;
//                }
//                else
//                {
//                    throw new InvalidOperationException("No floor types available to duplicate.");
//                }
//            }

//            // Apply floor layers and materials here based on floorTypeData.Layers

//            return floorType;
//        }

//        private RoofType CreateRoofType(Document doc, RoofTypeData roofTypeData)
//        {
//            // Try to find an existing RoofType by name
//            RoofType roofType = new FilteredElementCollector(doc)
//                .OfClass(typeof(RoofType))
//                .Cast<RoofType>()
//                .FirstOrDefault(rt => rt.Name.Equals(roofTypeData.Name));

//            if (roofType == null)
//            {
//                // Duplicate an existing RoofType if the specified one doesn't exist
//                roofType = new FilteredElementCollector(doc)
//                    .OfClass(typeof(RoofType))
//                    .Cast<RoofType>()
//                    .FirstOrDefault();

//                if (roofType != null)
//                {
//                    roofType = roofType.Duplicate(roofTypeData.Name) as RoofType;
//                }
//                else
//                {
//                    throw new InvalidOperationException("No roof types available to duplicate.");
//                }
//            }

//            // Apply roof layers and materials here based on roofTypeData.Layers

//            return roofType;
//        }

//        private Material CreateOrGetMaterial(Document doc, RevitElementData data)
//        {
//            // Ensure MaterialName is not null or empty
//            if (string.IsNullOrEmpty(data.MaterialName))
//            {
//                throw new ArgumentException("MaterialName cannot be null or empty.");
//            }

//            // Try to find an existing material with the specified name
//            Material material = new FilteredElementCollector(doc)
//                .OfClass(typeof(Material))
//                .Cast<Material>()
//                .FirstOrDefault(m => m.Name.Equals(data.MaterialName));

//            // If material does not exist, create a new one
//            if (material == null)
//            {
//                SelectedElementId materialId = Material.Create(doc, data.MaterialName);
//                material = doc.GetElement(materialId) as Material;
//            }

//            // Check if color data is provided and not null
//            if (data.Color != null)
//            {
//                // Set the material's color using the appearance asset
//                SelectedElementId appearanceAssetId = material.AppearanceAssetId;
//                AppearanceAssetElement appearanceAsset = doc.GetElement(appearanceAssetId) as AppearanceAssetElement;

//                if (appearanceAsset != null)
//                {
//                    // Access the editable version of the appearance asset
//                    using (AppearanceAssetEditScope editScope = new AppearanceAssetEditScope(doc))
//                    {
//                        Asset editableAsset = editScope.Start(appearanceAssetId);

//                        // Find and set the color property
//                        AssetProperty assetProperty = editableAsset.FindByName("generic_diffuse");

//                        if (assetProperty != null)
//                        {
//                            AssetPropertyDoubleArray4d colorProperty = assetProperty as AssetPropertyDoubleArray4d;
//                            if (colorProperty != null)
//                            {
//                                colorProperty.SetValueAsDoubles(new double[]
//                                {
//                            data.Color.R / 255.0,
//                            data.Color.G / 255.0,
//                            data.Color.B / 255.0,
//                            1
//                                });
//                            }
//                        }

//                        // Save changes and finish editing
//                        editScope.Commit(true);
//                    }
//                }
//            }

//            return material;
//        }

//        private WallType GetWallType(Document doc, string typeName)
//        {
//            WallType wallType = new FilteredElementCollector(doc)
//                .OfClass(typeof(WallType))
//                .Cast<WallType>()
//                .FirstOrDefault(wt => wt.Name.Equals(typeName));

//            // If no specific type is found, return the first available wall type
//            if (wallType == null)
//            {
//                wallType = new FilteredElementCollector(doc)
//                    .OfClass(typeof(WallType))
//                    .Cast<WallType>()
//                    .FirstOrDefault();
//            }

//            return wallType;
//        }

//        private FloorType GetFloorType(Document doc, string typeName)
//        {
//            FloorType floorType = new FilteredElementCollector(doc)
//                .OfClass(typeof(FloorType))
//                .Cast<FloorType>()
//                .FirstOrDefault(ft => ft.Name.Equals(typeName));

//            // If no specific type is found, return the first available floor type
//            if (floorType == null)
//            {
//                floorType = new FilteredElementCollector(doc)
//                    .OfClass(typeof(FloorType))
//                    .Cast<FloorType>()
//                    .FirstOrDefault();
//            }

//            return floorType;
//        }

//        private RoofType GetRoofType(Document doc, string typeName)
//        {
//            RoofType roofType = new FilteredElementCollector(doc)
//                .OfClass(typeof(RoofType))
//                .Cast<RoofType>()
//                .FirstOrDefault(rt => rt.Name.Equals(typeName));

//            // If no specific type is found, return the first available roof type
//            if (roofType == null)
//            {
//                roofType = new FilteredElementCollector(doc)
//                    .OfClass(typeof(RoofType))
//                    .Cast<RoofType>()
//                    .FirstOrDefault();
//            }

//            return roofType;
//        }

//        private WallType CreateOrModifyWallType(Document doc, Material material, RevitElementData data)
//        {
//            WallType wallType = new FilteredElementCollector(doc)
//                .OfClass(typeof(WallType))
//                .Cast<WallType>()
//                .FirstOrDefault(wt => wt.Name.Equals(data.WallTypeName));

//            if (wallType == null)
//            {
//                wallType = new FilteredElementCollector(doc)
//                    .OfClass(typeof(WallType))
//                    .Cast<WallType>()
//                    .FirstOrDefault();
//                if (wallType != null)
//                {
//                    wallType = wallType.Duplicate(data.WallTypeName) as WallType;
//                }
//                else
//                {
//                    // Handle case where no wall types are available
//                    throw new InvalidOperationException("No wall types available to duplicate.");
//                }
//            }

//            // Assume the structure setting code follows here
//            return wallType;
//        }

//        private FloorType CreateOrModifyFloorType(Document doc, Material material, RevitElementData data)
//        {
//            FloorType floorType = new FilteredElementCollector(doc)
//                .OfClass(typeof(FloorType))
//                .Cast<FloorType>()
//                .FirstOrDefault(ft => ft.Name.Equals(data.FloorTypeName));

//            if (floorType == null)
//            {
//                floorType = new FilteredElementCollector(doc)
//                    .OfClass(typeof(FloorType))
//                    .Cast<FloorType>()
//                    .FirstOrDefault();
//                if (floorType != null)
//                {
//                    floorType = floorType.Duplicate(data.FloorTypeName) as FloorType;
//                }
//                else
//                {
//                    // Handle case where no floor types are available
//                    throw new InvalidOperationException("No floor types available to duplicate.");
//                }
//            }

//            // Assume the structure setting code follows here
//            return floorType;
//        }

//        private RoofType CreateOrModifyRoofType(Document doc, Material material, RevitElementData data)
//        {
//            RoofType roofType = new FilteredElementCollector(doc)
//                .OfClass(typeof(RoofType))
//                .Cast<RoofType>()
//                .FirstOrDefault(rt => rt.Name.Equals(data.RoofTypeName));

//            if (roofType == null)
//            {
//                roofType = new FilteredElementCollector(doc)
//                    .OfClass(typeof(RoofType))
//                    .Cast<RoofType>()
//                    .FirstOrDefault();
//                if (roofType != null)
//                {
//                    roofType = roofType.Duplicate(data.RoofTypeName) as RoofType;
//                }
//                else
//                {
//                    // Handle case where no roof types are available
//                    throw new InvalidOperationException("No roof types available to duplicate.");
//                }
//            }

//            // Assume the structure setting code follows here
//            return roofType;
//        }

//        //private void PlaceWall(Document doc, WallType wallType, WallTypeData data)
//        //{
//        //    // Assume the start and end points are derived from the Layers or other data
//        //    // Adjust to ensure points are planar
//        //    XYZ start = new XYZ(data.Layers[0].Properties.ThermalConductivity, data.Layers[0].Properties.SpecificHeatCapacity, 0);
//        //    XYZ end = new XYZ(data.Layers[1].Properties.ThermalConductivity, data.Layers[1].Properties.SpecificHeatCapacity, 0);

//        //    // Ensure the points form a closed loop
//        //    XYZ intermediatePoint1 = new XYZ(start.X, end.Y, start.Z);
//        //    XYZ intermediatePoint2 = new XYZ(end.X, start.Y, end.Z);

//        //    // Create a list of curves that form a closed loop
//        //    IList<Curve> curves = new List<Curve>
//        //{
//        //    Line.CreateBound(start, intermediatePoint1),
//        //    Line.CreateBound(intermediatePoint1, end),
//        //    Line.CreateBound(end, intermediatePoint2),
//        //    Line.CreateBound(intermediatePoint2, start)  // This closes the loop
//        //};

//        //    // Ensure a transaction is started before calling Create
//        //    if (!doc.IsModifiable)
//        //        throw new InvalidOperationException("Document needs to be in a transaction to create walls.");

//        //    Wall.Create(doc, curves, wallType.Id, doc.ActiveView.GenLevel.Id, false);
//        //}


//        //private void PlaceFloor(Document doc, FloorType floorType, FloorTypeData data)
//        //{
//        //    List<XYZ> points = new List<XYZ>
//        //{
//        //    new XYZ(0, 0, 0),
//        //    new XYZ(10, 0, 0),
//        //    new XYZ(10, 10, 0),
//        //    new XYZ(0, 10, 0)
//        //};

//        //    CurveLoop curveLoop = new CurveLoop();
//        //    for (int i = 0; i < points.Count; i++)
//        //    {
//        //        int nextIndex = (i + 1) % points.Count;
//        //        curveLoop.Append(Line.CreateBound(points[i], points[nextIndex]));
//        //    }

//        //    Level level = doc.ActiveView.GenLevel;

//        //    using (Transaction trans = new Transaction(doc, "Create Floor"))
//        //    {
//        //        trans.Start();
//        //        try
//        //        {
//        //            List<CurveLoop> curveLoops = new List<CurveLoop> { curveLoop };
//        //            Floor floor = Floor.Create(doc, curveLoops, floorType.Id, level.Id, false, null, 0.0);
//        //            trans.Commit();
//        //        }
//        //        catch (Exception ex)
//        //        {
//        //            trans.RollBack();
//        //            throw new InvalidOperationException("Failed to create floor: " + ex.Message);
//        //        }
//        //    }
//        //}

//        //private void PlaceRoof(Document doc, RoofType roofType, RoofTypeData data)
//        //{
//        //    // Use the LevelId from the RoofTypeData to get the Level element
//        //    Level level = doc.GetElement(data.LevelId) as Level;

//        //    if (level == null)
//        //    {
//        //        throw new InvalidOperationException("The level associated with the provided LevelId could not be found.");
//        //    }

//        //    CurveArray curves = new CurveArray();
//        //    for (int i = 0; i < data.Layers.Count; i++)
//        //    {
//        //        curves.Append(Line.CreateBound(
//        //            new XYZ(data.Layers[i].Properties.ThermalConductivity, data.Layers[i].Properties.SpecificHeatCapacity, 10),
//        //            new XYZ(data.Layers[i].Properties.ThermalConductivity + 10, data.Layers[i].Properties.SpecificHeatCapacity, 10)));
//        //    }

//        //    using (Transaction trans = new Transaction(doc, "Create Roof"))
//        //    {
//        //        trans.Start();
//        //        try
//        //        {
//        //            ModelCurveArray modelCurveArray;
//        //            FootPrintRoof roof = doc.Create.NewFootPrintRoof(curves, level, roofType, out modelCurveArray);

//        //            foreach (ModelCurve mc in modelCurveArray)
//        //            {
//        //                roof.set_DefinesSlope(mc, true);
//        //                roof.set_SlopeAngle(mc, 0.25);
//        //            }
//        //            trans.Commit();
//        //        }
//        //        catch (Exception ex)
//        //        {
//        //            trans.RollBack();
//        //            throw new InvalidOperationException("Failed to create roof: " + ex.Message);
//        //        }
//        //    }
//        //}



//    }

//    public class RevitElementWrapper
//    {
//        public List<WallTypeData> WallTypes { get; set; }
//        public List<FloorTypeData> FloorTypes { get; set; }
//        public List<RoofTypeData> RoofTypes { get; set; }
//    }

//    public class WallTypeData
//    {
//        public string Name { get; set; }
//        public double UValue { get; set; }
//        public List<LayerData> Layers { get; set; }
//    }

//    public class FloorTypeData
//    {
//        public string Name { get; set; }
//        public double UValue { get; set; }
//        public List<LayerData> Layers { get; set; }
//    }

//    public class RoofTypeData
//    {
//        public string Name { get; set; }
//        public double UValue { get; set; }
//        public List<LayerData> Layers { get; set; }
//        public SelectedElementId LevelId { get; set; }  // Add this property
//    }


//    public class LayerData
//    {
//        public string Material { get; set; }
//        public string Function { get; set; }
//        public double Thickness { get; set; }
//        public bool Structural { get; set; }
//        public MaterialProperties Properties { get; set; }
//    }

//    public class MaterialProperties
//    {
//        public double ThermalConductivity { get; set; }
//        public double SpecificHeatCapacity { get; set; }
//        public double Density { get; set; }
//        public double Emissivity { get; set; }
//    }

//    public class RgbColor
//    {
//        public byte R { get; set; }
//        public byte G { get; set; }
//        public byte B { get; set; }
//    }

//    public class Coordinate
//    {
//        public double X { get; set; }
//        public double Y { get; set; }
//        public double Z { get; set; }
//    }

//    public class RevitElementData
//    {
//        public string WallTypeName { get; set; }
//        public string FloorTypeName { get; set; }
//        public string RoofTypeName { get; set; }
//        public List<Coordinate> Boundary { get; set; }
//        public Coordinate Start { get; set; }
//        public Coordinate End { get; set; }
//        public RgbColor Color { get; set; }
//        public string MaterialName { get; set; }
//        public SelectedElementId LevelId { get; set; }
//    }
//}


using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.DB.Visual;
using Autodesk.Revit.UI;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Globalization;

namespace RevitWoodLCC
{
    [Transaction(TransactionMode.Manual)]
    public class CreateElement : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiApp = commandData.Application;
            Document doc = uiApp.ActiveUIDocument.Document;

            // Get the current assembly
            Assembly assembly = Assembly.GetExecutingAssembly();

            // Construct the correct resource name
            string resourceName = "RevitWoodLCC.assets.Create_Revit_Elements.json";

            // Read the embedded JSON file
            string jsonContent;
            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    message = $"Resource '{resourceName}' not found.";
                    return Result.Failed;
                }

                using (StreamReader reader = new StreamReader(stream))
                {
                    jsonContent = reader.ReadToEnd();
                }
            }

            // Deserialize JSON content into the wrapper class
            RevitElementWrapper elementWrapper = JsonConvert.DeserializeObject<RevitElementWrapper>(jsonContent);

            // Lists to hold the names of created elements
            List<string> createdWallTypes = new List<string>();
            List<string> createdRoofTypes = new List<string>();

            // Begin a transaction for creating elements
            using (Transaction tx = new Transaction(doc, "Create Elements"))
            {
                tx.Start();
                try
                {
                    // Process WallTypes
                    foreach (var wallTypeData in elementWrapper.WallTypes)
                    {
                        WallType wallType = CreateWallType(doc, wallTypeData);
                        createdWallTypes.Add(wallType.Name);
                    }

                    // Process RoofTypes
                    foreach (var roofTypeData in elementWrapper.RoofTypes)
                    {
                        RoofType roofType = CreateRoofType(doc, roofTypeData);
                        createdRoofTypes.Add(roofType.Name);
                    }

                    tx.Commit();

                    // Display a confirmation message with the names of the created elements
                    string resultMessage = "Creation Results:\n\n" +
                        $"Wall Types:\n{string.Join("\n", createdWallTypes)}\n\n" +
                        $"Roof Types:\n{string.Join("\n", createdRoofTypes)}";

                    TaskDialog.Show("Creation Result", resultMessage);

                    return Result.Succeeded;
                }
                catch (Exception ex)
                {
                    message = ex.Message;
                    tx.RollBack();
                    return Result.Failed;
                }
            }
        }

        private WallType CreateWallType(Document doc, WallTypeData wallTypeData)
        {
            WallType wallType = null;

            // Check for the wall category specified in the JSON data
            if (wallTypeData.WallCategory == "Basic")
            {
                // Find an existing Basic WallType with the specified name that is not a Curtain Wall
                wallType = new FilteredElementCollector(doc)
                    .OfClass(typeof(WallType))
                    .Cast<WallType>()
                    .FirstOrDefault(wt => wt.Name.Equals(wallTypeData.Name) && !wt.Kind.Equals(WallKind.Curtain));

                // If no such WallType is found, duplicate an existing Basic WallType
                if (wallType == null)
                {
                    wallType = new FilteredElementCollector(doc)
                        .OfClass(typeof(WallType))
                        .Cast<WallType>()
                        .FirstOrDefault(wt => !wt.Kind.Equals(WallKind.Curtain));

                    if (wallType != null)
                    {
                        wallType = wallType.Duplicate(wallTypeData.Name) as WallType;
                    }
                    else
                    {
                        throw new InvalidOperationException("No basic wall types available to duplicate.");
                    }
                }
            }
            else if (wallTypeData.WallCategory == "Curtain")
            {
                // Find an existing Curtain WallType with the specified name
                wallType = new FilteredElementCollector(doc)
                    .OfClass(typeof(WallType))
                    .Cast<WallType>()
                    .FirstOrDefault(wt => wt.Name.Equals(wallTypeData.Name) && wt.Kind.Equals(WallKind.Curtain));

                // If no such WallType is found, duplicate an existing Curtain WallType
                if (wallType == null)
                {
                    wallType = new FilteredElementCollector(doc)
                        .OfClass(typeof(WallType))
                        .Cast<WallType>()
                        .FirstOrDefault(wt => wt.Kind.Equals(WallKind.Curtain));

                    if (wallType != null)
                    {
                        wallType = wallType.Duplicate(wallTypeData.Name) as WallType;
                    }
                    else
                    {
                        throw new InvalidOperationException("No curtain wall types available to duplicate.");
                    }
                }
            }
            else if (wallTypeData.WallCategory == "Stacked")
            {
                // Find an existing Stacked WallType with the specified name
                wallType = new FilteredElementCollector(doc)
                    .OfClass(typeof(WallType))
                    .Cast<WallType>()
                    .FirstOrDefault(wt => wt.Name.Equals(wallTypeData.Name) && wt.Kind.Equals(WallKind.Stacked));

                // If no such WallType is found, duplicate an existing Stacked WallType
                if (wallType == null)
                {
                    wallType = new FilteredElementCollector(doc)
                        .OfClass(typeof(WallType))
                        .Cast<WallType>()
                        .FirstOrDefault(wt => wt.Kind.Equals(WallKind.Stacked));

                    if (wallType != null)
                    {
                        wallType = wallType.Duplicate(wallTypeData.Name) as WallType;
                    }
                    else
                    {
                        throw new InvalidOperationException("No stacked wall types available to duplicate.");
                    }
                }
            }
            else
            {
                throw new InvalidOperationException($"Wall category '{wallTypeData.WallCategory}' is not recognized.");
            }

            // If wallType is still null, something went wrong
            if (wallType == null)
            {
                throw new InvalidOperationException($"Wall type '{wallTypeData.Name}' could not be created.");
            }

            return wallType;
        }

        private RoofType CreateRoofType(Document doc, RoofTypeData roofTypeData)
        {
            RoofType roofType = new FilteredElementCollector(doc)
                .OfClass(typeof(RoofType))
                .Cast<RoofType>()
                .FirstOrDefault(rt => rt.Name.Equals(roofTypeData.Name));

            if (roofType == null)
            {
                RoofType basicRoofType = new FilteredElementCollector(doc)
                    .OfClass(typeof(RoofType))
                    .Cast<RoofType>()
                    .FirstOrDefault(rt => rt.FamilyName == "Basic Roof");

                if (basicRoofType != null)
                {
                    roofType = basicRoofType.Duplicate(roofTypeData.Name) as RoofType;
                }
                else
                {
                    throw new InvalidOperationException("No basic roof types available to duplicate.");
                }
            }

            CompoundStructure compStructure = roofType.GetCompoundStructure();
            if (compStructure == null)
            {
                throw new InvalidOperationException("The roof type does not have a valid compound structure.");
            }

            List<CompoundStructureLayer> newLayers = new List<CompoundStructureLayer>();

            foreach (var layer in roofTypeData.Layers)
            {
                Material material = GetOrCreateMaterial(doc, layer.Material);
                CompoundStructureLayer newLayer = new CompoundStructureLayer(layer.Thickness,
                    layer.Structural ? MaterialFunctionAssignment.Structure : MaterialFunctionAssignment.Finish1, material.Id);

                newLayers.Add(newLayer);
            }

            compStructure.SetLayers(newLayers);

            IDictionary<int, CompoundStructureError> errors;
            IDictionary<int, int> errMap;

            if (!compStructure.IsValid(doc, out errors, out errMap))
            {
                string errorMsg = "Invalid CompoundStructure:";
                foreach (var error in errors)
                {
                    errorMsg += $"\n - Error at layer index {error.Key}: {error.Value.GetType().Name}";
                    errorMsg += $"\n   Layer Material: {newLayers[error.Key].MaterialId}";
                    errorMsg += $"\n   Layer Thickness: {newLayers[error.Key].Width}";
                    errorMsg += $"\n   Layer Function: {newLayers[error.Key].Function}";
                }
                throw new InvalidOperationException(errorMsg);
            }

            roofType.SetCompoundStructure(compStructure);

            return roofType;
        }

        // Helper method to fetch or create a material by name
        private Material GetOrCreateMaterial(Document doc, string materialName)
        {
            Material material = new FilteredElementCollector(doc)
              .OfClass(typeof(Material))
              .Cast<Material>()
              .FirstOrDefault(m => m.Name == materialName);

            if (material == null)
            {
                ElementId materialId = Material.Create(doc, materialName);
                material = doc.GetElement(materialId) as Material;
            }

            return material;

        }

    }

    public class RevitElementWrapper
    {
        public List<WallTypeData> WallTypes { get; set; }
        public List<FloorTypeData> FloorTypes { get; set; }
        public List<RoofTypeData> RoofTypes { get; set; }
    }

    public class WallTypeData
    {
        public string Name { get; set; }
        public double UValue { get; set; }
        public string WallCategory { get; set; }  // Add this property to specify the type of wall
        public List<LayerData> Layers { get; set; }
    }


    public class FloorTypeData
    {
        public string Name { get; set; }
        public double UValue { get; set; }
        public string FloorCategory { get; set; }  // Add this property to specify the type of floor
        public List<LayerData> Layers { get; set; }
    }


    public class RoofTypeData
    {
        public string Name { get; set; }
        public double UValue { get; set; }
        public string RoofCategory { get; set; }  // Add this property to specify the type of roof
        public List<LayerData> Layers { get; set; }
        public ElementId LevelId { get; set; }  // The level where the roof is placed
    }


    public class LayerData
    {
        public string Material { get; set; }
        public string Function { get; set; }
        public double Thickness { get; set; }
        public bool Structural { get; set; }
        public MaterialProperties Properties { get; set; }
    }

    public class MaterialProperties
    {
        public double ThermalConductivity { get; set; }
        public double SpecificHeatCapacity { get; set; }
        public double Density { get; set; }
        public double Emissivity { get; set; }
    }

    public class RgbColor
    {
        public byte R { get; set; }
        public byte G { get; set; }
        public byte B { get; set; }
    }

    public class Coordinate
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
    }

    public class RevitElementData
    {
        public string WallTypeName { get; set; }
        public string FloorTypeName { get; set; }
        public string RoofTypeName { get; set; }
        public List<Coordinate> Boundary { get; set; }
        public Coordinate Start { get; set; }
        public Coordinate End { get; set; }
        public RgbColor Color { get; set; }
        public string MaterialName { get; set; }
        public ElementId LevelId { get; set; }
    }
}



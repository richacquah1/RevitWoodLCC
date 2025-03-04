using Autodesk.Revit.DB;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;

namespace RevitWoodLCC
{
    public static class MaterialImportUtility
    {
        public static string MaterialJsonFilePath { get; } = @"C:\Users\Richard\source\repos\RevitWoodLCC\assets\MaterialResistance.json";
        public static List<SharedParameterInfo> ReadSharedParameters(string sharedParametersFilePath)
        {
            List<SharedParameterInfo> sharedParameters = new List<SharedParameterInfo>();
            string[] lines = File.ReadAllLines(sharedParametersFilePath);

            Regex paramLineRegex = new Regex(@"PARAM\s+([0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12})\s+(\w+)");

            foreach (string line in lines)
            {
                Match match = paramLineRegex.Match(line);
                if (match.Success)
                {
                    sharedParameters.Add(new SharedParameterInfo
                    {
                        Guid = new Guid(match.Groups[1].Value),
                        Name = match.Groups[2].Value
                    });
                }
            }

            return sharedParameters;
        }

        public static List<MaterialData> GetAllMaterials()
        {
            // Use the fully qualified name of the resource (default namespace + folder structure + file name)
            string resourceName = "RevitWoodLCC.assets.MaterialResistance.json";

            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
            using (StreamReader reader = new StreamReader(stream))
            {
                string json = reader.ReadToEnd();
                return JsonConvert.DeserializeObject<List<MaterialData>>(json);
            }
        }
        public static string GetSharedParametersFilePath()
        {
            // Modify the path as needed to fit your environment setup.
            string path = @"C:\Users\Richard\source\repos\RevitWoodLCC\assets\MaterialSharedParameters.txt";
            SetFileAsReadOnly(path);
            return path;
        }

        private static void SetFileAsReadOnly(string filePath)
        {
            FileInfo fileInfo = new FileInfo(filePath);
            if (fileInfo.Exists)
            {
                fileInfo.IsReadOnly = true;
            }
        }
    }

    public class SharedParameterInfo
    {
        public Guid Guid { get; set; }
        public string Name { get; set; }
    }

    public class MaterialData
    {
        [JsonProperty("name")]
        public List<string> Name { get; set; }

        [JsonProperty("latinName")]
        public List<string> LatinName { get; set; }

        [JsonProperty("treatment")]
        public List<string> Treatment { get; set; }

        [JsonProperty("resistanceDoseUC3")]
        public int ResistanceDoseUC3 { get; set; }

        [JsonProperty("resistanceDoseUC4")]
        public int ResistanceDoseUC4 { get; set; }
    }
}


/*
 Common beech: 4 times
Ash: 2 times
Coco: 2 times
Cutarro: 2 times
Southern yellow pine: 3 times
Scots pine: 4 times
Western red cedar: 2 times
Norway spruce: 3 times
Scots pine sapwood: 8 times
 * */
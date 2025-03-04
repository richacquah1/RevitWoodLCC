
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using static Autodesk.Revit.DB.SpecTypeId;

namespace RevitWoodLCC
{
    [Transaction(TransactionMode.Manual)]
    public class AddSharedParameters : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            var sph = new SharedParameterUtility.SharedParameterHandler();
            string sharedParametersPath = sph.SaveAndLoadSharedParameters(commandData.Application);
            if (sharedParametersPath == null)
            {
                return Result.Failed;
            }

            // Assign shared parameters to categories
            using (Transaction t = new Transaction(commandData.Application.ActiveUIDocument.Document, "Assign Shared Parameters"))
            {
                t.Start();
                sph.AssignSharedParametersToCategories(commandData.Application);
                t.Commit();
            }

            // List of project-specific parameters
            //var projectParameters = new List<(string ParameterName, ForgeTypeId ParameterType)>
            //{
            //    ("Project Service Life Duration", SpecTypeId.Int.Integer),
            //    ("Project Initial Cost", SpecTypeId.Int.Integer),
            //    ("Project Maintenance Cost", SpecTypeId.Int.Integer),
            //    ("Project End of Life Value", SpecTypeId.Int.Integer),
            //    ("Project Budget", SpecTypeId.Int.Integer)
            //};

            var projectParameters = new List<(string ParameterName, ForgeTypeId ParameterType)>
            {
                ("Project Service Life Duration", SpecTypeId.Number),
                ("Project Initial Cost", SpecTypeId.Number),
                ("Project Maintenance Cost", SpecTypeId.Number),
                ("Project End of Life Value", SpecTypeId.Number),
                ("Project Budget", SpecTypeId.Number)
            };

            // Add project-specific parameters
            using (Transaction t = new Transaction(commandData.Application.ActiveUIDocument.Document, "Bind Project Parameters"))
            {
                t.Start();
                if (!sph.BindProjectParameters(commandData.Application, projectParameters))
                {
                    TaskDialog.Show("Error", "Failed to bind one or more project-specific parameters.");
                    t.RollBack();
                    return Result.Failed;
                }
                t.Commit();
            }

            // Set default values for project-specific parameters
            var defaultValues = new Dictionary<string, double>
            {
                { "Project Service Life Duration", 60.00 },
                //{ "Project Initial Cost", 100000 },
                //{ "Project Maintenance Cost", 5000.00 },
                //{ "Project End of Life Value", 20000 },
                //{ "Project Budget", 150000 }
            };

            using (Transaction t = new Transaction(commandData.Application.ActiveUIDocument.Document, "Set Default Project Parameter Values"))
            {
                t.Start();
                SetDefaultProjectParameterValues(commandData.Application, defaultValues);
                t.Commit();
            }

            TaskDialog.Show("Success", $"Shared parameters file has been saved to {sharedParametersPath} and parameters have been assigned to elements in the project.");

            return Result.Succeeded;
        }

        private void SetDefaultProjectParameterValues(UIApplication uiApp, Dictionary<string, double> defaultValues)
        {
            Document doc = uiApp.ActiveUIDocument.Document;
            ProjectInfo projectInfo = doc.ProjectInformation;

            foreach (var kvp in defaultValues)
            {
                Parameter parameter = projectInfo.LookupParameter(kvp.Key);
                if (parameter != null && !parameter.IsReadOnly)
                {
                    // Check if the parameter is being set correctly
                    parameter.Set(kvp.Value);
                    //TaskDialog.Show("Debug", $"Parameter {kvp.Key} set to {kvp.Value}");
                }
            }
        }
    }

    public static class SharedParameterUtility
    {
        public static string EnsureSharedParameters(UIApplication uiApp)
        {
            var sph = new SharedParameterHandler();
            string sharedParametersPath = sph.SaveAndLoadSharedParameters(uiApp);
            if (sharedParametersPath == null)
            {
                TaskDialog.Show("Error", "Shared parameters could not be loaded.");
                return null;
            }
            sph.AssignSharedParametersToCategories(uiApp);
            return sharedParametersPath;
        }

        public class SharedParameterHandler
        {
            public string SaveAndLoadSharedParameters(UIApplication uiApp)
            {
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                string resourceName = "RevitWoodLCC.NewWoodLCC.txt"; // Ensure this matches your embedded resource

                string sharedParametersContent;
                try
                {
                    using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                    {
                        if (stream == null)
                            throw new InvalidOperationException($"Resource '{resourceName}' not found.");

                        using (StreamReader reader = new StreamReader(stream))
                        {
                            sharedParametersContent = reader.ReadToEnd();
                        }
                    }
                }
                catch (Exception ex)
                {
                    TaskDialog.Show("Error", $"Failed to read shared parameters file: {ex.Message}");
                    return null;
                }

                string saveDirPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
                string saveFilePath = Path.Combine(saveDirPath, "WoodLCCParameters.txt");

                File.WriteAllText(saveFilePath, sharedParametersContent);

                Document doc = uiApp.ActiveUIDocument.Document;
                if (doc.IsFamilyDocument)
                {
                    TaskDialog.Show("Error", "Cannot define a new shared parameter while editing a family.");
                    return null;
                }

                uiApp.Application.SharedParametersFilename = saveFilePath;
                return saveFilePath;
            }

            public void AssignSharedParametersToCategories(UIApplication uiApp)
            {
                Document doc = uiApp.ActiveUIDocument.Document;
                if (doc.IsFamilyDocument)
                {
                    throw new InvalidOperationException("Cannot define a new shared parameter while editing a family.");
                }

                DefinitionFile sharedParamFile = uiApp.Application.OpenSharedParameterFile();

                foreach (DefinitionGroup group in sharedParamFile.Groups)
                {
                    foreach (ExternalDefinition def in group.Definitions)
                    {
                        if (def.Name == "Project Service Life Duration")
                        {
                            // Skip handling here, handled separately
                            continue;
                        }

                        // Handle element parameters separately
                        CategorySet categorySet = uiApp.Application.Create.NewCategorySet();
                        foreach (Category category in doc.Settings.Categories)
                        {
                            // Add only model element categories that allow parameters
                            // Exclude Project Information category
                            if (category.CategoryType == CategoryType.Model && category.AllowsBoundParameters && category.Id.IntegerValue != (int)BuiltInCategory.OST_ProjectInformation)
                            {
                                categorySet.Insert(category);
                            }
                        }

                        if (categorySet.IsEmpty) continue;

                        InstanceBinding instanceBinding = uiApp.Application.Create.NewInstanceBinding(categorySet);
                        bool bindSuccess = doc.ParameterBindings.Insert(def, instanceBinding, BuiltInParameterGroup.PG_DATA);

                        if (!bindSuccess)
                        {
                            doc.ParameterBindings.ReInsert(def, instanceBinding, BuiltInParameterGroup.PG_DATA);
                        }
                    }
                }
            }

            public bool BindProjectParameters(UIApplication uiApp, List<(string ParameterName, ForgeTypeId ParameterType)> parameters)
            {
                DefinitionFile sharedParamFile = uiApp.Application.OpenSharedParameterFile();
                if (sharedParamFile == null)
                {
                    TaskDialog.Show("Error", "Shared parameter file not set or found.");
                    return false;
                }

                DefinitionGroup group = sharedParamFile.Groups.get_Item("Project_Parameters") ?? sharedParamFile.Groups.Create("Project_Parameters");
                CategorySet categorySet = uiApp.Application.Create.NewCategorySet();
                Category projectInfoCategory = uiApp.ActiveUIDocument.Document.Settings.Categories.get_Item(BuiltInCategory.OST_ProjectInformation);
                categorySet.Insert(projectInfoCategory);

                bool bindSuccess = true;

                foreach (var (parameterName, parameterType) in parameters)
                {
                    ExternalDefinitionCreationOptions options = new ExternalDefinitionCreationOptions(parameterName, parameterType);
                    Definition definition = group.Definitions.get_Item(parameterName) ?? group.Definitions.Create(options);

                    InstanceBinding binding = uiApp.Application.Create.NewInstanceBinding(categorySet);
                    if (!uiApp.ActiveUIDocument.Document.ParameterBindings.Insert(definition, binding, BuiltInParameterGroup.PG_DATA))
                    {
                        if (!uiApp.ActiveUIDocument.Document.ParameterBindings.ReInsert(definition, binding, BuiltInParameterGroup.PG_DATA))
                        {
                            bindSuccess = false;
                        }
                    }
                }

                return bindSuccess;
            }

        }

        public static Definition FindOrCreateSharedParameter(UIApplication uiApp, string parameterName, ForgeTypeId parameterType, BuiltInParameterGroup parameterGroup = BuiltInParameterGroup.PG_DATA)
        {
            DefinitionFile sharedParamFile = uiApp.Application.OpenSharedParameterFile();
            if (sharedParamFile == null)
            {
                throw new InvalidOperationException("Shared parameter file not set or found.");
            }

            DefinitionGroup group = sharedParamFile.Groups.get_Item("YourGroupName") ?? sharedParamFile.Groups.Create("YourGroupName");
            ExternalDefinitionCreationOptions options = new ExternalDefinitionCreationOptions(parameterName, parameterType);
            Definition definition = group.Definitions.get_Item(parameterName) ?? group.Definitions.Create(options);

            // Assign the parameter to all categories that allow bound parameters
            CategorySet categorySet = uiApp.Application.Create.NewCategorySet();
            foreach (Category category in uiApp.ActiveUIDocument.Document.Settings.Categories)
            {
                if (category.AllowsBoundParameters)
                {
                    categorySet.Insert(category);
                }
            }

            InstanceBinding binding = uiApp.Application.Create.NewInstanceBinding(categorySet);
            bool bindSuccess = uiApp.ActiveUIDocument.Document.ParameterBindings.Insert(definition, binding, parameterGroup);
            if (!bindSuccess)
            {
                bindSuccess = uiApp.ActiveUIDocument.Document.ParameterBindings.ReInsert(definition, binding, parameterGroup);
            }

            return definition;
        }
    }
}

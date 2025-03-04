using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace RevitWoodLCC
{
    [Transaction(TransactionMode.Manual)]
    public class AssignMaterialToAllElements : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            // Get the UI document and the current selection
            UIDocument uiDoc = commandData.Application.ActiveUIDocument;
            Document doc = uiDoc.Document;

            // Input form for material
            var inputForm = new InputForm();
            inputForm.ShowDialog();

            string materialName = inputForm.MaterialName;
            if (string.IsNullOrWhiteSpace(materialName))
            {
                TaskDialog.Show("Error", "You must input a material name.");
                return Result.Cancelled;
            }

            Material materialToAssign = new FilteredElementCollector(doc)
              .OfClass(typeof(Material))
              .Cast<Material>()
              .FirstOrDefault(m => m.Name == materialName);

            if (materialToAssign == null)
            {
                TaskDialog.Show("Error", $"No material named {materialName} found.");
                return Result.Cancelled;
            }

            // Get all elements in the document
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            ICollection<Element> allElements = collector.WhereElementIsNotElementType().ToElements();

            // Create a new list to store the elements with the material parameter
            List<Element> materialElements = new List<Element>();

            using (Transaction trans = new Transaction(doc, "Assign Material"))
            {
                trans.Start();

                foreach (Element e in allElements)
                {
                    Parameter matParam = e.get_Parameter(BuiltInParameter.MATERIAL_ID_PARAM);
                    if (matParam != null)
                    {
                        // Add the element to the list
                        materialElements.Add(e);

                        // Continue with your existing logic to assign material
                        try
                        {
                            matParam.Set(materialToAssign.Id);
                        }
                        catch
                        {
                            // Handle exceptions as required
                        }
                    }
                }

                trans.Commit();
            }

            TaskDialog.Show("Material Assignation", $"Successfully assigned material to {materialElements.Count} elements.");

            return Result.Succeeded;
        }
    }

    public class InputForm : System.Windows.Forms.Form
    {
        private System.Windows.Forms.TextBox textBoxMaterialName;
        private System.Windows.Forms.Button buttonSubmit;

        public string MaterialName
        {
            get { return textBoxMaterialName.Text; }
        }

        public InputForm()
        {
            this.Text = "Enter Material Name";
            this.Size = new System.Drawing.Size(400, 200);

            textBoxMaterialName = new System.Windows.Forms.TextBox();
            textBoxMaterialName.Location = new System.Drawing.Point(15, 15);
            textBoxMaterialName.Size = new System.Drawing.Size(350, 20);
            this.Controls.Add(textBoxMaterialName);

            buttonSubmit = new System.Windows.Forms.Button();
            buttonSubmit.Text = "Submit";
            buttonSubmit.Location = new System.Drawing.Point(15, 45);
            buttonSubmit.Click += new System.EventHandler(this.ButtonSubmit_Click);
            this.Controls.Add(buttonSubmit);
        }

        private void ButtonSubmit_Click(object sender, EventArgs e)
        {
            this.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.Close();
        }
    }
}

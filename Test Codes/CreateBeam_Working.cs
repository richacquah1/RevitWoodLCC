using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;

namespace RevitWoodLCC
{
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public class CreateBeamSystem : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            // Get the current document
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;

            // Prompt the user for spacing and number of beams
            double beamSpacing = GetUserInput("Enter beam spacing:");
            int numberOfBeams = (int)GetUserInput("Enter number of beams:");

            // Get the level
            Level level = new FilteredElementCollector(doc)
                .OfClass(typeof(Level))
                .FirstElement() as Level;

            // Define the boundary of the beam system
            IList<Curve> boundary = new List<Curve>();

            // Define the beam segments within the boundary
            double beamWidth = 1.0; // Width of each beam

            for (int i = 0; i < numberOfBeams; i++)
            {
                double startX = (beamWidth + beamSpacing) * i;
                double startY = 0;
                double endX = startX + beamWidth;
                double endY = startY;

                XYZ startPoint = new XYZ(startX, startY, 0);
                XYZ endPoint = new XYZ(endX, endY, 0);
                Curve curve = Line.CreateBound(startPoint, endPoint);

                boundary.Add(curve);
            }

            // Start a transaction to create the beams
            using (Transaction trans = new Transaction(doc, "Create Beams"))
            {
                trans.Start();

                // Get the beam family symbol
                FilteredElementCollector collector = new FilteredElementCollector(doc);
                FamilySymbol beamType = collector.OfClass(typeof(FamilySymbol))
                    .OfCategory(BuiltInCategory.OST_StructuralFraming)
                    .FirstElement() as FamilySymbol;

                // Create beams within the boundary
                foreach (Curve curve in boundary)
                {
                    // Create a beam using the NewFamilyInstance method
                    FamilyInstance beam = doc.Create.NewFamilyInstance(curve, beamType, level, StructuralType.Beam);
                }

                trans.Commit();
            }

            return Result.Succeeded;
        }

        // Helper method to prompt the user for input and return the value
        private double GetUserInput(string prompt)
        {
            // Create a new form to display the input prompt
            System.Windows.Forms.Form inputForm = new System.Windows.Forms.Form();
            inputForm.Width = 500;
            inputForm.Height = 150;
            inputForm.Text = "Input";
            inputForm.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;

            // Create a label to display the prompt
            System.Windows.Forms.Label label = new System.Windows.Forms.Label();
            label.Text = prompt;
            label.Left = 50;
            label.Top = 20;
            label.Parent = inputForm;

            // Create a text box for the user to enter their input
            System.Windows.Forms.TextBox textBox = new System.Windows.Forms.TextBox();
            textBox.Left = 50;
            textBox.Top = 50;
            textBox.Width = 400;
            textBox.Parent = inputForm;

            // Create an OK button to confirm the input
            System.Windows.Forms.Button okButton = new System.Windows.Forms.Button();
            okButton.Text = "OK";
            okButton.Left = 350;
            okButton.Top = 80;
            okButton.Parent = inputForm;
            okButton.DialogResult = System.Windows.Forms.DialogResult.OK;

            // Set the form's accept button to the OK button
            inputForm.AcceptButton = okButton;

            // Show the form and get the result
            System.Windows.Forms.DialogResult result = inputForm.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.OK)
            {
                double value;
                if (double.TryParse(textBox.Text, out value))
                    return value;
            }

            return 0.0;
        }

    }
}

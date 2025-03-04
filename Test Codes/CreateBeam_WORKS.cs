//WORKS
using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;

namespace RevitWoodLCC
{
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public class CreateBeamSystem3 : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            // Get the current document
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;

            // Get the level
            Level level = new FilteredElementCollector(doc)
                .OfClass(typeof(Level))
                .FirstElement() as Level;

            // Define the boundary of the beam system
            IList<Curve> boundary = new List<Curve>();

            // Define the beam segments within the boundary
            double beamWidth = 1; // Width of each beam
            double beamSpacing = 0.1; // Spacing between each beam
            int numberOfBeams = 5; // Number of beams in the panel

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
    }
}

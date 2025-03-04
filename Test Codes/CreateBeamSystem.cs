using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;

namespace RevitWoodLCC
{
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public class CreateBeamSystem2 : IExternalCommand
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

            // Define the corners of the rectangle
            XYZ bottomLeft = new XYZ(0, 0, 0);
            XYZ topLeft = new XYZ(0, 10, 0);
            XYZ topRight = new XYZ(10, 10, 0);
            XYZ bottomRight = new XYZ(10, 0, 0);

            // Create lines between the corners
            Line left = Line.CreateBound(bottomLeft, topLeft);
            Line top = Line.CreateBound(topLeft, topRight);
            Line right = Line.CreateBound(topRight, bottomRight);
            Line bottom = Line.CreateBound(bottomRight, bottomLeft);

            // Add the lines to the boundary
            boundary.Add(left);
            boundary.Add(top);
            boundary.Add(right);
            boundary.Add(bottom);


            // Start a transaction to create the beam system
            using (Transaction trans = new Transaction(doc, "Create Beam System"))
            {
                trans.Start();

                // Get the beam family symbol
                FilteredElementCollector collector = new FilteredElementCollector(doc);
                FamilySymbol beamType = collector.OfClass(typeof(FamilySymbol))
                    .OfCategory(BuiltInCategory.OST_StructuralFraming)
                    .FirstElement() as FamilySymbol;

                // Get an existing beam system
                BeamSystem existingBeamSystem = collector.OfClass(typeof(BeamSystem))
                    .FirstElement() as BeamSystem;

                // Get the beam system type of the existing beam system
                BeamSystemType existingBeamSystemType = existingBeamSystem.BeamSystemType;

                // Duplicate the existing beam system type
                BeamSystemType beamSystemType = existingBeamSystemType.Duplicate("My Beam System Type") as BeamSystemType;

                // Set the maximum spacing
                double maxSpacing = 0.1;
                Parameter maxSpacingParam = beamSystemType.LookupParameter("Maximum Spacing");
                maxSpacingParam.Set(maxSpacing);

                // Create the beam system using the NewBeamSystem method
                BeamSystem beamSystem = BeamSystem.Create(doc, boundary, level, 5, true);

                // Set the beam system type of the beam system
                beamSystem.BeamSystemType = beamSystemType;

                trans.Commit();
            }





            return Result.Succeeded;
        }
    }
}

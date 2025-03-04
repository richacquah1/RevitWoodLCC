
//Display project location latitude, longitude, Elevation and Place Name
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;

namespace RevitWoodLCC
{
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.ReadOnly)]
    public class GetLocation : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Document doc = commandData.Application.ActiveUIDocument.Document;

            try
            {
                // Get the active Project Location
                ProjectLocation projectLocation = doc.ActiveProjectLocation;

                // Get the Site Location
                SiteLocation siteLocation = doc.SiteLocation;

                // Retrieve details
                double latitude = siteLocation.Latitude;
                double longitude = siteLocation.Longitude;
                double elevation = siteLocation.Elevation;
                string placeName = siteLocation.PlaceName;

                string locationInfo = $"Project Location:\n\nLatitude: {ToDegreesMinutesSeconds(latitude)}\nLongitude: {ToDegreesMinutesSeconds(longitude)}\nElevation: {elevation} m\nPlace Name: {placeName}";

                TaskDialog.Show("Project Location", locationInfo);
                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }

        private string ToDegreesMinutesSeconds(double angleInRadians)
        {
            double degreesDouble = angleInRadians * (180.0 / Math.PI);
            int degrees = (int)degreesDouble;
            double minutesDouble = (degreesDouble - degrees) * 60;
            int minutes = (int)minutesDouble;
            int seconds = (int)((minutesDouble - minutes) * 60);

            return $"{degrees}° {minutes}' {seconds}\"";
        }
    }
}

﻿using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using Autodesk.Revit.Attributes;
using System;

namespace RevitWoodLCC
{
    [Transaction(TransactionMode.Manual)]
    public class ShowMapViewCommand : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            try
            {
                // Create and show the map view window
                ShowMapView showMapView = new ShowMapView();
                showMapView.ShowDialog();

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }
    }
}

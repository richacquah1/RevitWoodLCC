using System;
using System.Runtime.InteropServices;

namespace RevitWoodLCC.InteractiveMapWindow
{
    [ComVisible(true)]
    public class ScriptManager
    {
        private readonly Action<double, double> _setCoordinates;

        public ScriptManager(Action<double, double> setCoordinates)
        {
            _setCoordinates = setCoordinates;
        }

        public void SetCoordinates(double latitude, double longitude)
        {
            _setCoordinates?.Invoke(latitude, longitude);
        }
    }
}

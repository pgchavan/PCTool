using System.Windows;
using System.Windows.Interop;
using Autodesk.Revit.UI;

namespace HPDTool
{
    public static class RevitWindowHelper
    {
        public static void SetOwner(Window window, UIApplication uiApp)
        {
            WindowInteropHelper helper = new WindowInteropHelper(window);
            helper.Owner = uiApp.MainWindowHandle;
        }
    }
}

using Autodesk.Revit.DB;

namespace HPDTool
{
    public class FamilyItem
    {
        public Family Family { get; set; }
        public string Name => Family.Name;
        public bool IsSelected { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.DB;

namespace SocketModifier
{
    public class TargetWalls
    {
        public Element element { get; set; }
        public string material { get; set; }
        public BoundingBoxXYZ box { get; set; }
        public string fireRate { get; set; }
    }
}

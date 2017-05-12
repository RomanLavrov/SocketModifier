#region Namespaces
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
#endregion

namespace SocketModifier
{
    public class TargetWalls
    {
        public Element element { get; set; }
        public string material { get; set; }
        public BoundingBoxXYZ box { get; set; }
    }

    [Transaction(TransactionMode.Manual)]
    public class Command : IExternalCommand
    {
        public Result Execute(
          ExternalCommandData commandData,
          ref string message,
          ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Application app = uiapp.Application;
            Document doc = uidoc.Document;
           
            ChangeParameters(app, doc);

            return Result.Succeeded;
        }

        private void DisplayWalls(List<TargetWalls> list)
        {
            string temp = string.Empty;
            foreach (TargetWalls wall in list)
            {
                temp += wall.element.Name + " - " + wall.material +  "\n";
            }
            TaskDialog.Show("Walls", temp);
        }

        public BoundingBoxIntersectsFilter Filter(Element wall, Document doc)
        {
            BoundingBoxXYZ box = wall.get_BoundingBox(null);
            if (box != null)
            {
               // TaskDialog.Show("BoundingBox", box.Min.ToString());
                Outline outline = new Outline(box.Min, box.Max);
                BoundingBoxIntersectsFilter filter = new BoundingBoxIntersectsFilter(outline);
                return filter;
            }
            return null;
        }

        public List<FamilyInstance> GetFireAlarmDevices(Document doc, Element wall)
        {
            BoundingBoxIntersectsFilter filter = Filter(wall, doc);
            List<FamilyInstance> list = new List<FamilyInstance>();
            if (filter != null)
            {
                FilteredElementCollector collector = new FilteredElementCollector(doc)
                    .OfClass(typeof(FamilyInstance))
                    .OfCategory(BuiltInCategory.OST_FireAlarmDevices)
                    .WherePasses(filter);

                string temp = string.Empty;
                foreach (FamilyInstance instance in collector)
                {
                    list.Add(instance);
                    temp += instance.Id + " " + instance.Name + "\n";
                }

               // if (!string.IsNullOrEmpty(temp))
                  //  TaskDialog.Show("Fire Alarm Devices", temp);
            }
           

            return list;
        }

        public List<FamilyInstance> GetDataDevices(Document doc, Element wall)
        {
            BoundingBoxIntersectsFilter filter = Filter(wall, doc);
            List<FamilyInstance> list = new List<FamilyInstance>();

            if (filter != null)
            {
                FilteredElementCollector collector = new FilteredElementCollector(doc)
                    .OfClass(typeof(FamilyInstance))
                    .OfCategory(BuiltInCategory.OST_DataDevices)
                    .WherePasses(filter);

                string temp = string.Empty;
                foreach (FamilyInstance instance in collector)
                {
                    list.Add(instance);
                    temp += instance.Id + " " + instance.Name + "\n";
                }

               // if (!string.IsNullOrEmpty(temp))
                   // TaskDialog.Show("Data Devices", temp);
            }
            return list;
        }

        public List<FamilyInstance> GetLightingDevices(Document doc, Element wall)
        {
            BoundingBoxIntersectsFilter filter = Filter(wall, doc);
            List<FamilyInstance> list = new List<FamilyInstance>();
            if (filter != null)
            {
                FilteredElementCollector collector = new FilteredElementCollector(doc)
                    .OfClass(typeof(FamilyInstance))
                    .OfCategory(BuiltInCategory.OST_LightingDevices)
                    .WherePasses(filter);

                string temp = string.Empty;
                foreach (FamilyInstance instance in collector)
                {
                    list.Add(instance);
                    temp += instance.Id + " " + instance.Name + "\n";
                }

               // if (!string.IsNullOrEmpty(temp))
                   // TaskDialog.Show("Lighting Devices", temp);
            }
            return list;
        }

        public List<FamilyInstance> GetElectricalFixtures(Document doc, Element wall)
        {
            BoundingBoxIntersectsFilter filter = Filter(wall, doc);
            List<FamilyInstance> list = new List<FamilyInstance>();
            if (filter != null)
            {
                FilteredElementCollector collector = new FilteredElementCollector(doc)
                    .OfClass(typeof(FamilyInstance))
                    .OfCategory(BuiltInCategory.OST_ElectricalFixtures)
                    .WherePasses(filter);

                string temp = string.Empty;
                foreach (FamilyInstance instance in collector)
                {
                    list.Add(instance);
                    temp += instance.Id + " " + instance.Name + "\n";
                }

               // if (!string.IsNullOrEmpty(temp))
                   // TaskDialog.Show("Electrical Fixtures", temp);
            }
            return list;
        }

        public List<FamilyInstance> GetTelephoneDevices(Document doc, Element wall)
        {
            BoundingBoxIntersectsFilter filter = Filter(wall, doc);
            List<FamilyInstance> list = new List<FamilyInstance>();
            if (filter != null)
            {
                FilteredElementCollector collector = new FilteredElementCollector(doc)
                    .OfClass(typeof(FamilyInstance))
                    .OfCategory(BuiltInCategory.OST_TelephoneDevices)
                    .WherePasses(filter);

                string temp = string.Empty;
                foreach (FamilyInstance instance in collector)
                {
                    list.Add(instance);
                    temp += instance.Id + " " + instance.Name + "\n";
                }

               // if (!string.IsNullOrEmpty(temp))
                    //TaskDialog.Show("Telephone Devices", temp);
            }
            return list;
        }

        public List<FamilyInstance> GetDevices(Document doc, Element wall)
        {
            List<FamilyInstance> deviceList = new List<FamilyInstance>();
            deviceList.AddRange(GetElectricalFixtures(doc, wall));
            deviceList.AddRange(GetLightingDevices(doc, wall));
            deviceList.AddRange(GetDataDevices(doc, wall));
            deviceList.AddRange(GetTelephoneDevices(doc, wall));
            deviceList.AddRange(GetFireAlarmDevices(doc, wall));

            return deviceList;
        }

        public void AddParameterData(Document doc, List<FamilyInstance> devices, TargetWalls wall)
        {
            using (Transaction trans = new Transaction(doc, "Adding Parameter"))
            {
                trans.Start();

                foreach (FamilyInstance instance in devices)
                {
                    Parameter material = instance.LookupParameter("WandTyp");
                    material.Set(wall.material);
                }
                trans.Commit();
            }
        }

        private List<Document> GetLinkedDocuments(Application app)
        {
            List<Document> list = new List<Document>();
            foreach (Document doc in app.Documents)
            {
                if (doc.IsLinked)
                {
                    list.Add(doc);
                }
            }
            return list;
        }

        public List<TargetWalls> GetAllWalls(List<Document> documents)
        {
            List<TargetWalls> list = new List<TargetWalls>();
            foreach (Document doc in documents)
            {
                FilteredElementCollector collector = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Walls);
                foreach (Element element in collector)
                {
                    TargetWalls tWall = new TargetWalls();
                    tWall.element = element;
                    foreach (Parameter param in element.Parameters)
                    {
                        if (param.Definition.Name.Contains("IfcMaterial"))
                        {
                            tWall.material = param.AsString();
                        }
                      
                    }
                    list.Add(tWall);
                }
            }
            return list;
        }

        public void ChangeParameters(Application app, Document doc)
        {
            List<TargetWalls> walls = GetAllWalls(GetLinkedDocuments(app));
            foreach (TargetWalls wall in walls)
            {
                List<FamilyInstance> wallDevices = GetDevices(doc, wall.element);
                AddParameterData(doc, wallDevices, wall);
            }
        }
    }
}

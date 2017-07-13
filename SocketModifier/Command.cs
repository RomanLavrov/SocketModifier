#region Namespaces
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using Application = Autodesk.Revit.ApplicationServices.Application;

#endregion

namespace SocketModifier
{
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
            DocSelection dc = new DocSelection();
            dc.GetDocList(GetDocuments(app));
           
            Window docSelectionWindow = new Window();
            docSelectionWindow.ResizeMode = ResizeMode.NoResize;
            docSelectionWindow.Width = 500;
            docSelectionWindow.Height = 350;
            docSelectionWindow.Topmost = true;
            docSelectionWindow.Content = dc;
            docSelectionWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            docSelectionWindow.ShowDialog();

            List<Document> UserDefinedDocuments = new List<Document>();
            if (docSelectionWindow.DialogResult == true)
            {
                if (dc.Docs.Count > 0)
                {
                    UserDefinedDocuments = dc.Docs;
                }
                else
                {
                    MessageBox.Show("No projects selected");
                    return Result.Cancelled;
                }
            }

            LevelSelection ls = new LevelSelection();
            foreach (Document docItem in UserDefinedDocuments)
            {
                ls.GetLevels(GetLevels(docItem));
            }

            //Window levelSelectionWindow = new Window();
            //levelSelectionWindow.ResizeMode = ResizeMode.NoResize;
            //levelSelectionWindow.Width = 320;
            //levelSelectionWindow.Height = 350;
            //levelSelectionWindow.Topmost = true;
            //levelSelectionWindow.Content = ls;
            //levelSelectionWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            //levelSelectionWindow.ShowDialog();

            //List<Level> UserDefinedLevels = new List<Level>();
            //if (levelSelectionWindow.DialogResult == true)
            //{
            //    if (ls.Levels.Count > 0)
            //    {
            //        UserDefinedLevels = ls.Levels;
            //    }
            //    else
            //    {
            //        MessageBox.Show("No levels selected");
            //        return Result.Cancelled;
            //    }
            //}

            ChangeParameters(app, doc);
            TaskDialog.Show("Task completed", "Task completed");
            return Result.Succeeded;
        }

        private void DisplayWalls(List<TargetWalls> list)
        {
            string temp = string.Empty;
            foreach (TargetWalls wall in list)
            {
                temp += wall.element.Name + " - " + wall.material + "\n";
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

        private List<FamilyInstance> GetDevice(Document doc, Element wall, BuiltInCategory category)
        {
            BoundingBoxIntersectsFilter filter = Filter(wall, doc);
            List<FamilyInstance> devicesList = new List<FamilyInstance>();
            if (filter != null)
            {
                FilteredElementCollector collector = new FilteredElementCollector(doc)
                    .OfClass(typeof(FamilyInstance))
                    .OfCategory(category)
                    .WherePasses(filter);

                string temp = string.Empty;
                foreach (FamilyInstance instance in collector)
                {
                    // if (instance.LevelId == selectedLevel.Id)
                    {
                        devicesList.Add(instance);
                        temp += instance.Id + " " + instance.Name + "\n";
                    }
                }

                // if (!string.IsNullOrEmpty(temp))
                // TaskDialog.Show("Selected Devices", temp);
            }

            return devicesList;
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

            deviceList.AddRange(GetDevice(doc, wall, BuiltInCategory.OST_CommunicationDevices));
            deviceList.AddRange(GetDevice(doc, wall, BuiltInCategory.OST_DataDeviceTags));
            deviceList.AddRange(GetDevice(doc, wall, BuiltInCategory.OST_ElectricalEquipment));
            deviceList.AddRange(GetDevice(doc, wall, BuiltInCategory.OST_ElectricalFixtures));
            deviceList.AddRange(GetDevice(doc, wall, BuiltInCategory.OST_FireAlarmDevices));
            deviceList.AddRange(GetDevice(doc, wall, BuiltInCategory.OST_LightingDevices));
            deviceList.AddRange(GetDevice(doc, wall, BuiltInCategory.OST_LightingFixtures));
            deviceList.AddRange(GetDevice(doc, wall, BuiltInCategory.OST_TelephoneDevices));

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
                    Parameter fireRate = instance.LookupParameter("Brandschutz");
                    fireRate.Set(wall.fireRate);
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

        public List<TargetWalls> GetAllWalls(List<Document> documents, List<Level> levels)
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
                        if (param.Definition.Name.Contains("Description"))
                        {
                            // MessageBox.Show(param.AsString());
                            tWall.material = param.AsString();
                        }

                        if (param.Definition.Name.Contains("Brandschutzanforderungen") && !string.IsNullOrEmpty(param.AsString()))
                        {
                            tWall.fireRate = param.AsString();
                        }
                    }
                    list.Add(tWall);
                }
            }
            return list;
        }

        private void ChangeParameters(Application app, Document doc)
        {
            string temp = string.Empty;
            List<TargetWalls> walls = GetAllWalls(GetLinkedDocuments(app), GetLevels(doc));
            foreach (TargetWalls wall in walls)
            {
                temp += wall.material + "\n";
                List<FamilyInstance> wallDevices = GetDevices(doc, wall.element);
                AddParameterData(doc, wallDevices, wall);
            }
            MessageBox.Show(temp);
        }

        public List<Document> GetDocuments(Application app)
        {
            List<Document> docs = new List<Document>();
            foreach (Document d in app.Documents)
            {
                docs.Add(d);
            }
            return docs;
        }

        public List<Level> GetLevels(Document doc)
        {
            List<Level> levels = new List<Level>();
            FilteredElementCollector levelCollector = new FilteredElementCollector(doc).OfClass(typeof(Level));

            foreach (Element level in levelCollector)
            {
                levels.Add(level as Level);
            }
            return levels;
        }
    }
}

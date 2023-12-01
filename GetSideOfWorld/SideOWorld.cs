using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Electrical;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Events;
using Autodesk.Revit.UI.Selection;


namespace GetSideOfWorld
{
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    public class SideOWorld : IExternalCommand
    {
        public XYZ GetVector(double angle)
        {
            XYZ result = new XYZ(Math.Cos(angle), Math.Sin(angle),0);
            return result;
        }
        public double GetAngle(XYZ tNorth, XYZ vector)
        {
            double tnX = tNorth.X;
            double tnY = tNorth.Y;
            double tnZ = tNorth.Z;
            double vx = vector.X;
            double vy = vector.Y;
            double vz = vector.Z;

            

            double result = (tnX*vx+tnY*vy+tnZ*vz)/(Math.Sqrt(Math.Pow(vx,2)+ Math.Pow(vy, 2))* (Math.Sqrt(Math.Pow(tnX, 2) + Math.Pow(tnY, 2))));
            double angle= Math.Acos(result)*180/Math.PI;
            return angle;
        }
            static string  GetInfo(List<double> values)
        {
            string text = null;
            int n = values.Count - 1;
            for (int i = 0; i < n; i++)
            {
                string a = Convert.ToString(values[i]);
                text = text + "\n" + a;
            }
            return text;

        }
        public void GetSide (Document doc, List<Element> gmodels,List<double>angles)
        {
            Parameter parameter = null;
            

            for (int i=0;i<gmodels.Count;i++)
            {
                parameter = gmodels[i].LookupParameter("ADSK_Код изделия");
                string orientation = null;
                double a = angles[i];
                if (0<=a && a<22.5)
                {
                    orientation = "В";
                }
                else if (337.5<a && a<360)
                {
                    orientation = "В";
                }
                else if (22.5<a && a<=67.5)
                {
                    orientation = "СВ";
                }
                else if (a<67.5&& a<=112.5)
                {
                    orientation = "С";
                }
                else if (a < 112.5 && a <= 157.5)
                {
                    orientation = "СЗ";
                }
                else if (a < 157.5 && a <= 202.5)
                {
                    orientation = "З";
                }
                else if (a < 202.5 && a <= 247.5)
                {
                    orientation = "ЮЗ";
                }
                else if (a < 202.5 && a <= 247.5)
                {
                    orientation = "ЮЗ";
                }
                else if (a < 247.5 && a <= 292.5)
                {
                    orientation = "Ю";
                }
                else  
                {
                    orientation = "ЮВ";
                }



                using (Transaction t =new Transaction(doc,"SetParameter"))
                {
                    t.Start("param");
                    parameter.Set(orientation);
                    t.Commit();
                }
            }
        }
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            //Тут работаем с базовой точкой и снимаем угол относительно истинного севера
            UIDocument uidoc = commandData.Application.ActiveUIDocument;
            Document doc = uidoc.Document;

            FilteredElementCollector BPCollector = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_ProjectBasePoint);
            Element basepoint = BPCollector.First();
            Parameter trueNorth = basepoint.get_Parameter(BuiltInParameter.BASEPOINT_ANGLETON_PARAM);
           double azimuth = trueNorth.AsDouble();
            double text = UnitUtils.ConvertFromInternalUnits(azimuth, DisplayUnitType.DUT_DECIMAL_DEGREES);
            //TaskDialog.Show("Revit", Convert.ToString(text));



            //Тут работаем с элементами обобщенных моделей
            FilteredElementCollector GModelCollector = new FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_GenericModel).WhereElementIsNotElementType();
            List<Element> gmodels = new List<Element>();
            List <XYZ> orientations = new List<XYZ>();
            List<double> angles = new List<double>();
            XYZ TrueNorthVector = GetVector(azimuth);
            TaskDialog.Show("Revit", $"{TrueNorthVector.X};{TrueNorthVector.Y};{TrueNorthVector.Z}");

            foreach (Element element in GModelCollector)
            {
                if (element.Name == "Дуга" || element.Name == "Прямая")
                {
                    gmodels.Add(element);
                }

            }
            foreach (Element element in gmodels)
            {

                FamilyInstance familyInstance = element as FamilyInstance;
                XYZ vector = familyInstance.FacingOrientation;
                orientations.Add(vector);
            }
            foreach (XYZ xYZ in orientations)
            {
                angles.Add(GetAngle(TrueNorthVector, xYZ));
            }
           // TaskDialog.Show("Revit", GetInfo(angles));

            // TaskDialog.Show("Revit", Convert.ToString(gmodels.Count));

            GetSide(doc,gmodels, angles);
            return Result.Succeeded;
        }
    }
}

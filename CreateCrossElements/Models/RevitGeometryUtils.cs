using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CreateCrossElements.Models
{
    public class RevitGeometryUtils
    {
        // Получение блоков пролетного строения с помощью пользовательского выбора
        public static List<Element> GetElementsBySelection(UIApplication uiapp, ISelectionFilter filter, out string elementIds)
        {
            Selection sel = uiapp.ActiveUIDocument.Selection;
            var pickedElems = sel.PickElementsByRectangle(filter, "Select Blocks");
            elementIds = ElementIdToString(pickedElems);

            return pickedElems.ToList();
        }

        // Проверка на то существуют ли элементы с данным Id в модели
        public static bool IsElemsExistInModel(Document doc, IEnumerable<int> elems, Type type)
        {
            if (elems is null)
            {
                return false;
            }

            foreach (var elem in elems)
            {
                ElementId id = new ElementId(elem);
                Element curElem = doc.GetElement(id);
                if (curElem is null || !(curElem.GetType() == type))
                {
                    return false;
                }
            }

            return true;
        }

        // Получение блоков по их id
        public static List<Element> GetElementsById(Document doc, IEnumerable<int> ids)
        {
            var elems = new List<Element>();
            foreach (var id in ids)
            {
                ElementId elemId = new ElementId(id);
                Element block = doc.GetElement(elemId);
                elems.Add(block);
            }

            return elems;
        }

        // Получение id элементов на основе списка в виде строки
        public static List<int> GetIdsByString(string elems)
        {
            if (string.IsNullOrEmpty(elems))
            {
                return null;
            }

            var elemIds = elems.Split(new char[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                         .Select(s => int.Parse(s.Remove(0, 2)))
                         .ToList();

            return elemIds;
        }

        // Метод получения строки с ElementId
        private static string ElementIdToString(IEnumerable<Element> elements)
        {
            var stringArr = elements.Select(e => "Id" + e.Id.IntegerValue.ToString()).ToArray();
            string resultString = string.Join(", ", stringArr);

            return resultString;
        }
    }
}

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

        // Метод получения строки с ElementId
        private static string ElementIdToString(IEnumerable<Element> elements)
        {
            var stringArr = elements.Select(e => "Id" + e.Id.IntegerValue.ToString()).ToArray();
            string resultString = string.Join(", ", stringArr);

            return resultString;
        }
    }
}

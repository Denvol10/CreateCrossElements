using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CreateCrossElements.Models.Filters
{
    public class GenericModelCategoryFilter : ISelectionFilter
    {
        public bool AllowElement(Element elem)
        {
            int elemCategoryId = elem.Category.Id.IntegerValue;
            int genericModelCategoryId = (int)BuiltInCategory.OST_GenericModel;

            if (elemCategoryId == genericModelCategoryId)
            {
                return true;
            }

            return false;
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            return true;
        }
    }
}

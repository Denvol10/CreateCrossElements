using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using Autodesk.Revit.DB.Architecture;
using System.Collections.ObjectModel;
using CreateCrossElements.Models;
using CreateCrossElements.Models.Filters;
using System.IO;

namespace CreateCrossElements
{
    public class RevitModelForfard
    {
        private UIApplication Uiapp { get; set; } = null;
        private Application App { get; set; } = null;
        private UIDocument Uidoc { get; set; } = null;
        private Document Doc { get; set; } = null;

        public RevitModelForfard(UIApplication uiapp)
        {
            Uiapp = uiapp;
            App = uiapp.Application;
            Uidoc = uiapp.ActiveUIDocument;
            Doc = uiapp.ActiveUIDocument.Document;
        }

        #region Блоки пролетного строения
        public List<Element> BlockElements { get; set; }

        private string _blockElementIds;
        public string BlockElementIds
        {
            get => _blockElementIds;
            set => _blockElementIds = value;
        }

        public void GetBlockElementsBySelection()
        {
            BlockElements = RevitGeometryUtils.GetElementsBySelection(Uiapp, new GenericModelCategoryFilter(), out _blockElementIds);
        }
        #endregion

        // Проверка на то существуют ли блоки в модели
        public bool IsElementsExistInModel(string elemIdsInSettings)
        {
            var elemIds = RevitGeometryUtils.GetIdsByString(elemIdsInSettings);

            return RevitGeometryUtils.IsElemsExistInModel(Doc, elemIds, typeof(FamilyInstance));
        }

        // Получение блоков из Settings
        public void GetBlocksBySettings(string elemIdsInSettings)
        {
            var elemIds = RevitGeometryUtils.GetIdsByString(elemIdsInSettings);
            BlockElements = RevitGeometryUtils.GetElementsById(Doc, elemIds);
        }

        #region Список названий типоразмеров семейств
        public ObservableCollection<FamilySymbolSelector> GetFamilySymbolNames()
        {
            var familySymbolNames = new ObservableCollection<FamilySymbolSelector>();
            var allFamilies = new FilteredElementCollector(Doc).OfClass(typeof(Family)).OfType<Family>();
            var genericModelFamilies = allFamilies.Where(f => f.FamilyCategory.Id.IntegerValue == (int)BuiltInCategory.OST_GenericModel);
            if (genericModelFamilies.Count() == 0)
                return familySymbolNames;

            foreach (var family in genericModelFamilies)
            {
                foreach (var symbolId in family.GetFamilySymbolIds())
                {
                    var familySymbol = Doc.GetElement(symbolId);
                    familySymbolNames.Add(new FamilySymbolSelector(family.Name, familySymbol.Name));
                }
            }

            return familySymbolNames;
        }
        #endregion

        #region Созданные элементы в модели
        private ICollection<ElementId> ElementSet { get; set; } = null;
        #endregion

        #region Создание поперечных элементов в модели
        public void CreateCrossElementsInModel(FamilySymbolSelector familyAndSymbolName,
                                               double blockHeight,
                                               bool isChangeSite,
                                               bool isCreateDown,
                                               double rotationAngle)
        {
            FamilySymbol fSymbol = GetFamilySymbolByName(familyAndSymbolName);

            var creationDataList = new List<Autodesk.Revit.Creation.FamilyInstanceCreationData>();

            foreach(var block in BlockElements)
            {
                var superstructureBlock = new SuperstructureBlock(Doc, block, blockHeight);
                var blockAdaptivePoints = superstructureBlock.GetPointsForCrossElements(isChangeSite, rotationAngle);
                foreach(var adaptivePoints in blockAdaptivePoints)
                {
                    creationDataList.Add(new Autodesk.Revit.Creation.FamilyInstanceCreationData(fSymbol, adaptivePoints));
                }
            }

            //string path = @"O:\Revit Infrastructure Tools\CreateCrossElements\CreateCrossElements\result.txt";
            //using(StreamWriter sw = new StreamWriter(path, false, Encoding.Default))
            //{
            //    var superstructureBlock = new SuperstructureBlock(Doc, BlockElements.ElementAt(2));
            //    foreach(var parameter in superstructureBlock.BlockParameters)
            //    {
            //        sw.WriteLine(parameter);
            //    }
            //}

            using (Transaction trans = new Transaction(Doc, "Create Cross Elements"))
            {
                trans.Start();
                if (!(ElementSet is null))
                {
                    Doc.Delete(ElementSet);
                }

                if (!fSymbol.IsActive)
                {
                    fSymbol.Activate();
                }

                if (Doc.IsFamilyDocument)
                {
                    ElementSet = Doc.FamilyCreate.NewFamilyInstances2(creationDataList);
                }
                else
                {
                    ElementSet = Doc.Create.NewFamilyInstances2(creationDataList);
                }

                trans.Commit();

                Uidoc.ShowElements(ElementSet.FirstOrDefault());
                Uidoc.RefreshActiveView();
            }
        }
        #endregion


        #region Получение типоразмера по имени
        private FamilySymbol GetFamilySymbolByName(FamilySymbolSelector familyAndSymbolName)
        {
            var familyName = familyAndSymbolName.FamilyName;
            var symbolName = familyAndSymbolName.SymbolName;

            Family family = new FilteredElementCollector(Doc).OfClass(typeof(Family)).Where(f => f.Name == familyName).First() as Family;
            var symbolIds = family.GetFamilySymbolIds();
            foreach (var symbolId in symbolIds)
            {
                FamilySymbol fSymbol = (FamilySymbol)Doc.GetElement(symbolId);
                if (fSymbol.get_Parameter(BuiltInParameter.SYMBOL_NAME_PARAM).AsString() == symbolName)
                {
                    return fSymbol;
                }
            }
            return null;
        }
        #endregion

    }
}

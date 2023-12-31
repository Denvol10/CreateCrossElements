﻿using System;
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
using System.Windows.Input;
using CreateCrossElements.Infrastructure;
using CreateCrossElements.Models;

namespace CreateCrossElements.ViewModels
{
    internal class MainWindowViewModel : Base.ViewModel
    {
        private RevitModelForfard _revitModel;

        internal RevitModelForfard RevitModel
        {
            get => _revitModel;
            set => _revitModel = value;
        }

        #region Заголовок
        private string _title = "Создание поперечных элементов";

        public string Title
        {
            get => _title;
            set => Set(ref _title, value);
        }
        #endregion

        #region Блоки пролетного строения
        private string _blockElementIds;
        public string BlockElementIds
        {
            get => _blockElementIds;
            set => Set(ref _blockElementIds, value);
        }
        #endregion

        #region Список семейств и их типоразмеров
        private ObservableCollection<FamilySymbolSelector> _genericModelFamilySymbols = new ObservableCollection<FamilySymbolSelector>();
        public ObservableCollection<FamilySymbolSelector> GenericModelFamilySymbols
        {
            get => _genericModelFamilySymbols;
            set => Set(ref _genericModelFamilySymbols, value);
        }
        #endregion

        #region Выбранный типоразмер семейства
        private FamilySymbolSelector _familySymbolName;
        public FamilySymbolSelector FamilySymbolName
        {
            get => _familySymbolName;
            set => Set(ref _familySymbolName, value);
        }
        #endregion

        #region Индекс выбранного семейства
        private int _familySymbolIndex = Properties.Settings.Default.FamilySymbolIndex;
        #endregion

        #region Высота блока
        private double _blockHeight = Properties.Settings.Default.BlockHeight;
        public double BlockHeight
        {
            get => _blockHeight;
            set => Set(ref _blockHeight, value);
        }
        #endregion

        #region Сторона построения
        private bool _isChangeSite = Properties.Settings.Default.IsChangeSite;
        public bool IsChangeSite
        {
            get => _isChangeSite;
            set => Set(ref _isChangeSite, value);
        }
        #endregion

        #region Угол поворота поперечного элемента
        private double _rotationAngle = Properties.Settings.Default.RotationAngle;
        public double RotationAngle
        {
            get => _rotationAngle;
            set => Set(ref _rotationAngle, value);
        }
        #endregion

        #region Команды

        #region Получение блоков пролетного строения
        public ICommand GetBlockElementsCommand { get; }

        private void OnGetBlockElementsCommandExecuted(object parameter)
        {
            RevitCommand.mainView.Hide();
            RevitModel.GetBlockElementsBySelection();
            BlockElementIds = RevitModel.BlockElementIds;
            RevitCommand.mainView.ShowDialog();
        }

        private bool CanGetBlockElementsCommandExecute(object parameter)
        {
            return true;
        }
        #endregion

        #region Создать поперечные элементы
        public ICommand CreateCrossElementsCommand { get; }

        private void OnCreateCrossElementsCommandExecuted(object parameter)
        {
            RevitModel.CreateCrossElementsInModel(FamilySymbolName, BlockHeight, IsChangeSite, RotationAngle);
            SaveSettings();
        }

        private bool CanCreateCrossElementsCommandExecute(object parameter)
        {
            if (string.IsNullOrEmpty(BlockElementIds))
            {
                return false;
            }

            return true;
        }
        #endregion

        #region Закрыть окно
        public ICommand CloseWindowCommand { get; }

        private void OnCloseWindowCommandExecuted(object parameter)
        {
            SaveSettings();
            RevitCommand.mainView.Close();
        }

        private bool CanCloseWindowCommandExecute(object parameter)
        {
            return true;
        }
        #endregion

        #endregion

        private void SaveSettings()
        {
            Properties.Settings.Default.BlockElementIds = BlockElementIds;
            Properties.Settings.Default.FamilySymbolIndex = GenericModelFamilySymbols.IndexOf(FamilySymbolName);
            Properties.Settings.Default.BlockHeight = BlockHeight;
            Properties.Settings.Default.IsChangeSite = IsChangeSite;
            Properties.Settings.Default.RotationAngle = RotationAngle;
            Properties.Settings.Default.Save();
        }

        #region Конструктор класса MainWindowViewModel
        public MainWindowViewModel(RevitModelForfard revitModel)
        {
            RevitModel = revitModel;

            GenericModelFamilySymbols = RevitModel.GetFamilySymbolNames();

            #region Инициализация свойств из Settings

            #region Инициализация блоков
            if (!(Properties.Settings.Default.BlockElementIds is null))
            {
                string blockElemIdsInSettings = Properties.Settings.Default.BlockElementIds;
                if (RevitModel.IsElementsExistInModel(blockElemIdsInSettings) && !string.IsNullOrEmpty(blockElemIdsInSettings))
                {
                    BlockElementIds = blockElemIdsInSettings;
                    RevitModel.GetBlocksBySettings(blockElemIdsInSettings);
                }
            }
            #endregion

            #region Инициализация значения типоразмера семейства
            if (_familySymbolIndex >= 0 && _familySymbolIndex <= GenericModelFamilySymbols.Count - 1)
            {
                FamilySymbolName = GenericModelFamilySymbols.ElementAt(_familySymbolIndex);
            }
            #endregion

            #endregion

            #region Команды
            GetBlockElementsCommand = new LambdaCommand(OnGetBlockElementsCommandExecuted, CanGetBlockElementsCommandExecute);
            CreateCrossElementsCommand = new LambdaCommand(OnCreateCrossElementsCommandExecuted, CanCreateCrossElementsCommandExecute);
            CloseWindowCommand = new LambdaCommand(OnCloseWindowCommandExecuted, CanCloseWindowCommandExecute);
            #endregion
        }

        public MainWindowViewModel() { }
        #endregion
    }
}

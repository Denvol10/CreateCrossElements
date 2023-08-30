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
using System.Windows.Input;
using CreateCrossElements.Infrastructure;

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
            Properties.Settings.Default.Save();
        }

        #region Конструктор класса MainWindowViewModel
        public MainWindowViewModel(RevitModelForfard revitModel)
        {
            RevitModel = revitModel;

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

            #endregion

            #region Команды
            GetBlockElementsCommand = new LambdaCommand(OnGetBlockElementsCommandExecuted, CanGetBlockElementsCommandExecute);
            CloseWindowCommand = new LambdaCommand(OnCloseWindowCommandExecuted, CanCloseWindowCommandExecute);
            #endregion
        }

        public MainWindowViewModel() { }
        #endregion
    }
}

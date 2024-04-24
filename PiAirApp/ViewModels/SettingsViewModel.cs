using YMModsApp.Common.Models;
using YMModsApp.Common.Tool;
using YMModsApp.Extensions;
using YMModsApp.Views;
using Prism.Commands;
using Prism.Ioc;
using Prism.Mvvm;
using Prism.Regions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace YMModsApp.ViewModels
{
    public class SettingsViewModel : BindableBase
    {
        public int IsMouseEnter { get; set; } = 0;

        public SettingsViewModel(IRegionManager regionManager)
        {
            MenuBars = new ObservableCollection<MenuBar>();
            this.regionManager = regionManager;
            NavigateCommand = new DelegateCommand<MenuBar>(Navigate);
            MouseEnterCommand = new DelegateCommand(MouseEnterHandle);
            CreateMenuBar();
        }

        /// <summary>
        /// 默认选中第一行设置
        /// </summary>
        private void MouseEnterHandle()
        {
            if (IsMouseEnter != 1)
            {
                IsMouseEnter = 1;
                this.regionManager.Regions[PrismManager.SettingsViewRegionName].RequestNavigate("SettingsUserView");
                ListBox menuBar = Public.FindChild<ListBox>(Application.Current.MainWindow, "menuBar");
                if (menuBar != null)
                    menuBar.SelectedIndex = 0;
            }

        }

        private void Navigate(MenuBar obj)
        {
            if (obj == null || string.IsNullOrWhiteSpace(obj.NameSpace))
                return;
            regionManager.Regions[PrismManager.SettingsViewRegionName].RequestNavigate(obj.NameSpace);
        }

        public DelegateCommand<MenuBar> NavigateCommand { get; private set; }
        public DelegateCommand MouseEnterCommand { get; private set; }
        private ObservableCollection<MenuBar> menuBars;
        private readonly IRegionManager regionManager;

        public ObservableCollection<MenuBar> MenuBars
        {
            get { return menuBars; }
            set { menuBars = value; RaisePropertyChanged(); }
        }


        void CreateMenuBar()
        {
            MenuBars.Add(new MenuBar() { Icon = "Cog", Title = "系统设置", NameSpace = "SettingsUserView" });
            MenuBars.Add(new MenuBar() { Icon = "Palette", Title = "个性化", NameSpace = "SkinView" });
            MenuBars.Add(new MenuBar() { Icon = "Information", Title = "关于更多", NameSpace = "AboutView" });
        }

    }
}

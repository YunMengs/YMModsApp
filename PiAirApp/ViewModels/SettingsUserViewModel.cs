using YMModsApp.Common.Models;
using YMModsApp.Common.Tool;
using Prism.Commands;
using Prism.Mvvm;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Windows.Controls;

namespace YMModsApp.ViewModels
{
    public class SettingsUserViewModel : BindableBase
    {
        public AutoStartTool autoStart = new AutoStartTool();

        public SettingsUserViewModel()
        {
            CheckButtons = new List<CheckBottonModel>()
            {
                 new CheckBottonModel(){ Id="1",  Content="开机启动", IsCheck = MySqLite.GetConfig("开机启动") == "1" ? true : false },
                 new CheckBottonModel(){ Id="2",  Content="自动优化内存", IsCheck = MySqLite.GetConfig("自动优化内存") == "1" ? true : false },
            };
            this.CheckCommand = new DelegateCommand<CheckBox>(Check);
        }

        public List<CheckBottonModel> CheckButtons { get; }
        public DelegateCommand<CheckBox> CheckCommand { get; private set; }

        public void Check(CheckBox c)
        {
            switch (c.Content)
            {
                case "开机启动":
                    autoStart.SetMeAutoStart((bool)c.IsChecked);
                    break;
            }
            MySqLite.SaveConfig((string)c.Content, (bool)c.IsChecked ? "1" : "0");

        }
    }
}

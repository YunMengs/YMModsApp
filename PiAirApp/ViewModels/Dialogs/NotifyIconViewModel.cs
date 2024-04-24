using YMModsApp.Common;
using YMModsApp.Common.Tool;
using YMModsApp.Extensions;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace YMModsApp.ViewModels.Dialogs
{
    internal class TaskbarIconViewModel : BindableBase
    {
        public DelegateCommand<string> NotifyCommand { get; private set; }

        public TaskbarIconViewModel()
        {
            NotifyCommand = new DelegateCommand<string>(NotifyMenu);
        }
        public void NotifyMenu(string type)
        {
            switch (type)
            {
                case "0":
                    Application.Current.MainWindow.Hide();
                    break;
                case "1":
                    if (MySqLite.GetConfig("isLogin") != "1")
                    {
                        return;
                    }
                    Application.Current.MainWindow.Show();
                    Application.Current.MainWindow.Activate();
                    break;
                case "2":
                    if (MySqLite.GetConfig("isLogin") != "1")
                    {
                        return;
                    }
                    if (Application.Current.MainWindow.Visibility == Visibility.Visible)
                    {
                        Application.Current.MainWindow.Hide();
                    }
                    else
                    {
                        Application.Current.MainWindow.Show();
                        Application.Current.MainWindow.Activate();
                    }
                    break;
                case "99":
                    var service = App.Current.MainWindow.DataContext as IConfigureService;
                    if (service != null)
                    {
                        service.ExitApp();
                    }
                    else
                    {
                        Application.Current.Shutdown();
                    }
                    break;
            }
        }
    }

}

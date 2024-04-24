using DryIoc;
using Hardcodet.Wpf.TaskbarNotification;
using YMModsApp.Common;
using YMModsApp.Common.Tool;
using YMModsApp.ViewModels;
using YMModsApp.ViewModels.Dialogs;
using YMModsApp.Views;
using Prism.DryIoc;
using Prism.Ioc;
using Prism.Services.Dialogs;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace YMModsApp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : PrismApplication
    {
        protected override Window CreateShell()
        {
            return Container.Resolve<MainView>();
        }

        public static void LoginOut(IContainerProvider containerProvider)
        {

            Current.MainWindow.Hide();
            MySqLite.SaveConfig("isLogin", "0");
            var dialog = containerProvider.Resolve<IDialogService>();

            dialog.ShowDialog("LoginView", callback =>
            {
                if (callback.Result != ButtonResult.OK)
                {
                    Environment.Exit(0);
                    return;
                }

                Current.MainWindow.Show();
            });
        }

        protected override void OnInitialized()
        {
            string username = MySqLite.GetConfig("username");
            string password = MySqLite.GetConfig("password");
            int isLogin = int.Parse(MySqLite.GetConfig("isLogin"));

            if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password) && isLogin == 1)
            {
                Init();
                base.OnInitialized();
                return;
            }

            var dialog = Container.Resolve<IDialogService>();

            dialog.ShowDialog("LoginView", callback =>
            {
                if (callback.Result != ButtonResult.OK)
                {
                    Environment.Exit(0);
                    return;
                }

                Init();
                base.OnInitialized();
            });
        }

        public void Init()
        {
            var service = App.Current.MainWindow.DataContext as IConfigureService;
            if (service != null)
            {
                service.Configure();
                // 监听扫码枪
                ScanerHook sh = new ScanerHook();
                sh.Start();
                sh.ScanerEvent += service.ScanerRe;
            }
        }

        private static System.Threading.Mutex mutex;
        protected override void OnStartup(StartupEventArgs e)
        {
            mutex = new System.Threading.Mutex(true, "OnlyRun_CRNS");
            if (mutex.WaitOne(0, false))
            {
                SplashScreen splashScreen = new SplashScreen("/Images/StartForm.png");
                splashScreen.Show(true);
                //上面Show()方法中设置为true时，程序启动完成后启动图片就会自动关闭，
                //设置为false时，启动图片不会自动关闭，需要使用下面一句设置显示时间，例如23s
                //splashScreen.Close(new TimeSpan(0, 0, 23));

                base.OnStartup(e);
                mTaskbarIcon = (TaskbarIcon)FindResource("Taskbar");
                mTaskbarIcon.DataContext = new TaskbarIconViewModel();
            }
            else
            {
                MessageBox.Show("发货猫已经在运行！", "提示");
                this.Shutdown();
            }


        }
        public static TaskbarIcon? mTaskbarIcon;

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.Register<IDialogHostService, DialogHostService>();

            containerRegistry.RegisterDialog<LoginView, LoginViewModel>();

            containerRegistry.RegisterForNavigation<AboutView>();
            containerRegistry.RegisterForNavigation<MsgView, MsgViewModel>();
            containerRegistry.RegisterForNavigation<SkinView, SkinViewModel>();
            containerRegistry.RegisterForNavigation<IndexView, IndexViewModel>();
            containerRegistry.RegisterForNavigation<SettingsView, SettingsViewModel>();
            containerRegistry.RegisterForNavigation<SettingsUserView, SettingsUserViewModel>();
        }
    }
}

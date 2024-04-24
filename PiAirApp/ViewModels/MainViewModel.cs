using MaterialDesignThemes.Wpf;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using YMModsApp.Common;
using YMModsApp.Common.ESP32;
using YMModsApp.Common.Exceptions;
using YMModsApp.Common.Models;
using YMModsApp.Common.Tool;
using YMModsApp.Extensions;
using Prism.Commands;
using Prism.Ioc;
using Prism.Mvvm;
using Prism.Regions;
using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Threading;
using static YMModsApp.Common.Tool.ScanerHook;

namespace YMModsApp.ViewModels
{
    public class MainViewModel : BindableBase, IConfigureService
    {
        public ArrayList TaskList { get; set; } = new ArrayList();

        public Snackbar snackbar;

        private string snackbarColor = SnackbarColorModels.Info;
        public string SnackbarColor
        {
            get { return snackbarColor; }
            set { SetProperty(ref snackbarColor, value); }
        }

        private string userName;
        public string UserName
        {
            get { return userName; }
            set { userName = value; RaisePropertyChanged(); }
        }

        public DelegateCommand LoginOutCommand { get; private set; }

        public IndexViewModel indexViewModel;
        public MainViewModel(IContainerProvider containerProvider,
            IRegionManager regionManager, IDialogHostService dialog)
        {
            this.dialog = dialog;
            MenuBars = new ObservableCollection<MenuBar>();
            NavigateCommand = new DelegateCommand<MenuBar>(Navigate);
            GoBackCommand = new DelegateCommand(() =>
            {
                if (journal != null && journal.CanGoBack)
                    journal.GoBack();
            });
            GoForwardCommand = new DelegateCommand(() =>
            {
                if (journal != null && journal.CanGoForward)
                    journal.GoForward();
            });
            LoginOutCommand = new DelegateCommand(async () =>
              {
                  var dialogResult = await dialog.Question("温馨提示", "是否需要退出登录？");
                  if (dialogResult.Result != Prism.Services.Dialogs.ButtonResult.OK) return;

                  indexViewModel.ScanMode = 0;
                  indexViewModel.ScanTips = "请扫描待绑定的快递单号";
                  //注销当前用户
                  App.LoginOut(containerProvider);
              });
            this.containerProvider = containerProvider;
            this.regionManager = regionManager;

            ESP32BleManager.RegisterMainViewModel(this);
            ESP32BleManager.getIntence();

            // 循环任务
            //new Task(() =>
            //{
            //    while (true)
            //    {
            //        UserName = MySqLite.GetConfig("nickname");
            //        Thread.Sleep(2000);
            //    }
            //}).Start();
        }

        private void Navigate(MenuBar obj)
        {
            if (obj == null || string.IsNullOrWhiteSpace(obj.NameSpace))
                return;

            regionManager.Regions[PrismManager.MainViewRegionName].RequestNavigate(obj.NameSpace, back =>
             {
                 journal = back.Context.NavigationService.Journal;
             });
        }

        public DelegateCommand<MenuBar> NavigateCommand { get; private set; }
        public DelegateCommand GoBackCommand { get; private set; }
        public DelegateCommand GoForwardCommand { get; private set; }

        private ObservableCollection<MenuBar> menuBars;
        private readonly IContainerProvider containerProvider;
        private readonly IRegionManager regionManager;
        private IRegionNavigationJournal journal;
        private IDialogHostService dialog;

        public ObservableCollection<MenuBar> MenuBars
        {
            get { return menuBars; }
            set { menuBars = value; RaisePropertyChanged(); }
        }


        void CreateMenuBar()
        {
            MenuBars.Add(new MenuBar() { Icon = "Home", Title = "首页", NameSpace = "IndexView" });
            MenuBars.Add(new MenuBar() { Icon = "Cog", Title = "设置", NameSpace = "SettingsView" });
        }

        /// <summary>
        /// 配置首页初始化参数
        /// </summary>
        public void Configure()
        {
            UserName = MySqLite.GetConfig("nickname");
            CreateMenuBar();
            SkinViewModel.ModifyTheme(theme => theme.SetBaseTheme(int.Parse(MySqLite.GetConfig("basetheme")) == 0 ? Theme.Dark : Theme.Light));
            SkinViewModel.ChangeHue(Public.GetMediaColorFromDrawingColor(MySqLite.GetConfig("theme")));
            regionManager.Regions[PrismManager.MainViewRegionName].RequestNavigate("IndexView");
        }

        public void RegisterIndexViewModel(IndexViewModel indexViewModel)
        {
            this.indexViewModel = indexViewModel;
            indexViewModel.RegisterMainViewModel(this);
        }

        private delegate void ScanerRe_Delegate(ScanerCodes codes);
        /// <summary>
        /// 扫码枪接收事件处理
        /// </summary>
        public void ScanerRe(ScanerCodes codes)
        {
            Task.Factory.StartNew(() => ScannerHandle(codes.Result));
        }

        /// <summary>
        /// 扫码枪接收事件处理函数
        /// </summary>
        public void ScannerHandle(string CodeResult)
        {
            indexViewModel.ScannerHandle(CodeResult);
        }

        /// <summary>
        /// 获取Snackbar对象
        /// </summary>
        /// <returns></returns>
        public Snackbar GetSnackbar()
        {
            if (snackbar == null)
            {
                snackbar = Public.FindChild<Snackbar>(Application.Current.MainWindow, "Snackbar");
            }
            return snackbar;
        }

        /// <summary>
        /// 发送Snackbar消息
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="snackbarColorModels"></param>
        public void SendMessage(string msg, string snackbarColorModels = SnackbarColorModels.Danger)
        {
            new Thread(() =>
            {
                Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                    new Action(() =>
                    {
                        SnackbarColor = snackbarColorModels;
                        GetSnackbar().MessageQueue.Clear();
                        GetSnackbar().MessageQueue.Enqueue(msg);
                    }));
            }).Start();
        }

        /// <summary>
        /// 获取一条未执行的任务
        /// </summary>
        /// <returns>任务</returns>
        public TaskListModels GetTaskList()
        {
            TaskListModels t = null;
            lock (TaskList)
            {
                if (TaskList.Count > 0)
                {
                    int index = TaskList.Count - 1;
                    t = (TaskListModels)TaskList[index];
                    TaskList.RemoveAt(index);
                    Debug.WriteLine(t.mac);
                    return t;
                }
            }
            return t;
        }

        /// <summary>
        /// 退出程序
        /// </summary>
        /// <returns>任务</returns>
        public async void ExitApp()
        {
            try
            {
                var dialogResult = await dialog.Question("温馨提示", "确认退出系统?");
                if (dialogResult.Result != Prism.Services.Dialogs.ButtonResult.OK) return;
                Application.Current.Shutdown();
            }
            catch (Exception)
            {
                Application.Current.Shutdown();
            }
        }

        /// <summary>
        /// 检测更新
        /// </summary>
        /// <exception cref="UpgradeException">检测升级异常</exception>
        public async void CheckUpgrade()
        {
            try
            {
                string ret = HttpRequest.GetWebRequest(HttpRequest.GetURL(XWSApis.VersionURL));
                JObject res = JObject.Parse(ret);
                if (res == null || res.Property("version") == null || res.Property("downurl") == null)
                {
                    SendMessage("获取新版本更新失败");
                    return;
                }
                string vs1 = Public.GetVersion();
                string vs2 = res["version"].ToString();
                Version v1 = new Version(vs1);
                Version v2 = new Version(vs2);
                if (v1 >= v2)
                {
                    SendMessage("当前已是最新版", SnackbarColorModels.Success);
                    return;
                }
                var dialogResult = await dialog.Question("温馨提示", "当前有版本更新，是否前往下载?");
                if (dialogResult.Result != Prism.Services.Dialogs.ButtonResult.OK) return;
                Process.Start(new ProcessStartInfo(res["downurl"].ToString()) { UseShellExecute = true });
            }
            catch (Exception)
            {
                new Thread(() =>
                {
                    Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                        new Action(() =>
                        {
                            SendMessage("网络异常！");
                        }));
                }).Start();
            }


        }
    }
}

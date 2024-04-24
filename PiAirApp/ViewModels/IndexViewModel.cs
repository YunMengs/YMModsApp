using ImTools;
using MaterialDesignThemes.Wpf;
using Microsoft.Xaml.Behaviors.Layout;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using YMModsApp.Common;
using YMModsApp.Common.Exceptions;
using YMModsApp.Common.Models;
using YMModsApp.Common.Tool;
using YMModsApp.Extensions;
using Prism.Commands;
using Prism.Ioc;
using Prism.Regions;
using System;
using System.Collections.ObjectModel;
using System.Configuration;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Threading;

namespace YMModsApp.ViewModels
{
    public class IndexViewModel : NavigationViewModel
    {
        public Snackbar snackbar;
        public int ScanMode = 0;
        public string cnTips = "待绑定单号：";
        public string CNTips
        {
            get { return cnTips; }
            set { SetProperty(ref cnTips, value); }
        }
        private string courierNumber;
        public string CourierNumber
        {
            get { return courierNumber; }
            set { SetProperty(ref courierNumber, value); }
        }

        private bool cnVisible = false;
        public bool CNVisible
        {
            get { return cnVisible; }
            set { SetProperty(ref cnVisible, value); }
        }

        private string scanTips = "请扫描待绑定的快递单号";
        public string ScanTips
        {
            get { return scanTips; }
            set { SetProperty(ref scanTips, value); }
        }

        private string version;
        public string Version
        {
            get { return version; }
            set { SetProperty(ref version, value); }
        }

        private readonly IDialogHostService dialog;
        private readonly IRegionManager regionManager;
        private MainViewModel mainViewModel;

        public IndexViewModel(IContainerProvider provider,
            IDialogHostService dialog) : base(provider)
        {
            //Title = $"你好，{MySqLite.GetConfig("nickname")} {DateTime.Now.GetDateTimeFormats('D')[1]}";
            Title = $"{DateTime.Now.GetDateTimeFormats('D')[1]}";
            Version = "V " + Public.GetVersion();
            CreateTaskBars();
            this.regionManager = provider.Resolve<IRegionManager>();
            this.dialog = dialog;
            NavigateCommand = new DelegateCommand<TaskBar>(Navigate);
            DoCommand = new DelegateCommand<string>(DoCommandFunc);
            CheckUpgradeCommand = new DelegateCommand(CheckUpgradeFunc);
            var service = App.Current.MainWindow.DataContext as IConfigureService;
            if (service != null)
            {
                service.RegisterIndexViewModel(this);
            }
            // 循环任务
            new Task(() =>
            {
                while (true)
                {
                    NetWorkPingTest();
                    Thread.Sleep(2000);
                }
            }).Start();

            new Task(() =>
            {
                int i = int.Parse(MySqLite.GetConfig("tips"));
                Application.Current.Dispatcher.Invoke((Action)(async () =>
                {
                    //回归主线程进行操作
                    if (i < 2)
                    {
                        i++;
                        MySqLite.SaveConfig("tips", i.ToString());
                        await dialog.Question("温馨提示", "1. 点击右上角头像可以注销操作\n" +
                    "2. 点击左下角版本号可以进行更新检测");
                    }
                    CheckUpgradeFunc();
                }));
            })
            {
            }.Start();
        }

        /// <summary>
        /// 检测更新
        /// </summary>
        public void CheckUpgradeFunc()
        {
            new Task(() =>
            {
                Application.Current.Dispatcher.Invoke((Action)(() =>
                {
                    //回归主线程进行操作
                    mainViewModel.CheckUpgrade();
                }));
            }).Start();
        }

        /// <summary>
        /// 网络检测任务
        /// </summary>
        public void NetWorkPingTest()
        {
            try
            {
                PingReply reply = NetWorkTool.PingTest();
                if (reply != null)
                {
                    if (reply.Status == IPStatus.Success)
                    {
                        if (TaskBars[3].Color != "#FF1ECA3A")
                        {
                            Application.Current.Dispatcher.Invoke((Action)(() =>
                            {
                                //回归主线程进行操作
                                TaskBars[3] = new TaskBar() { Icon = "WifiArrowLeftRight", Title = "网络", Content = reply.RoundtripTime.ToString() + "ms", Color = "#FF1ECA3A", Target = "" };
                            }));
                        }
                        else
                        {
                            TaskBars[3].Content = reply.RoundtripTime.ToString() + "ms";
                        }
                    }
                    else
                    {
                        throw new Exception();
                    }
                }
                else
                {
                    throw new Exception();
                }
            }
            catch (Exception)
            {
                if (TaskBars[3].Color != "red")
                {
                    Application.Current.Dispatcher.Invoke((Action)(() =>
                    {
                        //回归主线程进行操作
                        TaskBars[3] = new TaskBar() { Icon = "WifiCancel", Title = "网络", Content = "---", Color = "red", Target = "" };
                    }));
                }
            }
        }

        public void RegisterMainViewModel(MainViewModel mainViewModel)
        {
            this.mainViewModel = mainViewModel;
        }

        private void Navigate(TaskBar obj)
        {
            if (string.IsNullOrWhiteSpace(obj.Target)) return;
            regionManager.Regions[PrismManager.MainViewRegionName].RequestNavigate(obj.Target);
        }
        private void DoCommandFunc(string command)
        {
            new Task(() =>
            {
                ScannerHandle(command);
            }).Start();
        }

        public DelegateCommand<string> DoCommand { get; private set; }
        public DelegateCommand<string> ExecuteCommand { get; private set; }

        public DelegateCommand MouseEnterCommand { get; private set; }
        public DelegateCommand CheckUpgradeCommand { get; private set; }
        public DelegateCommand<TaskBar> NavigateCommand { get; private set; }

        #region 属性

        private string title;

        public string Title
        {
            get { return title; }
            set { title = value; RaisePropertyChanged(); }
        }

        private ObservableCollection<TaskBar> taskBars;

        public ObservableCollection<TaskBar> TaskBars
        {
            get { return taskBars; }
            set { taskBars = value; RaisePropertyChanged(); }
        }

        #endregion

        void CreateTaskBars()
        {
            TaskBars = new ObservableCollection<TaskBar>
            {
                new TaskBar() { Icon = "Bluetooth", Title = "可用蓝牙", Content = "0", Color = "#FF0CA0FF", Target = "" },
                new TaskBar() { Icon = "BluetoothAudio", Title = "执行蓝牙", Content = "0", Color = "#FF02C6DC", Target = "" },
                new TaskBar() { Icon = "BluetoothOff", Title = "非可用蓝牙", Content = "0", Color = "#FFFFA000", Target = "" },
                new TaskBar() { Icon = "WifiArrowLeftRight", Title = "网络", Content = "15ms", Color = "#FF1ECA3A", Target = "" }
            };
        }

        /// <summary>
        /// 扫码枪接收事件处理函数
        /// </summary>
        public void ScannerHandle(string CodeResult)
        {
            Debug.WriteLine(CodeResult);
            if (MySqLite.GetConfig("isLogin") != "1")
            {
                return;
            }
            try
            {
                // 有扫码枪输入时
                JObject jo = new JObject();
                JObject res = null;
                int Code;
                string mac;
                string CodeString;
                if (CodeResult.GetType() != typeof(String))
                {
                    SoundPlayerTool.SoundPlay("错误通用");
                    mainViewModel.SendMessage("接收的数据异常");
                    throw new DGException("接收的数据异常");
                }
                // 判断命令
                if (CodeResult[..3] == "xws")
                {
                    string command = CodeResult.Substring(3, 4);
                    Debug.WriteLine(command);
                    // 清空绑定
                    CourierNumber = null;
                    switch (command)
                    {
                        case "0001":
                            // 清空绑定
                            SoundPlayerTool.SoundPlay("14清空成功");
                            mainViewModel.SendMessage("清空成功！", SnackbarColorModels.Success);
                            CourierNumber = null;
                            ScanMode = 0;
                            ScanTips = "请扫描待绑定的快递单号";
                            return;
                        case "0002":
                            // 解绑设备
                            SoundPlayerTool.SoundPlay("10解绑");
                            ScanMode = 1;
                            ScanTips = "请扫描需要解绑的设备或快递单号";
                            return;
                        case "0003":
                            // 开锁检查
                            SoundPlayerTool.SoundPlay("7开箱");
                            ScanMode = 2;
                            ScanTips = "开箱检查，请扫描一个设备";
                            return;
                    }
                }

                switch (ScanMode)
                {
                    case 1:
                        // 解绑设备
                        // 判断输入的是mac还是订单号
                        if (CodeResult.Length < 70)
                        {
                            // 订单号
                            jo["courier_number"] = CodeResult;
                        }
                        else
                        {
                            // mac
                            String fy = CodeResult.Substring(0, 28);
                            if (string.Compare(fy, XWSApis.QRURL) != 0)
                            {
                                SoundPlayerTool.SoundPlay("错误通用");
                                mainViewModel.SendMessage("请扫描正确的πair码！");
                                ScanTips = "请扫待绑定πair码";
                                throw new DGException("请扫描正确的πair码！");
                            }

                            // 解密MAC地址
                            CodeString = CodeResult.Substring(32);
                            Debug.WriteLine(CodeString);
                            mac = Public.UnlockString(CodeString);
                            Debug.WriteLine(mac);
                            jo["mac"] = mac;
                        }

                        jo["own_bid"] = MySqLite.GetConfig("id");
                        jo["iden"] = MySqLite.GetConfig("iden");

                        res = HttpRequest.PostRequest(XWSApis.IsUnBindingUrl, jo.ToString());

                        if (res.Property("code") == null)
                        {
                            SoundPlayerTool.SoundPlay("ServiceError");
                            mainViewModel.SendMessage("服务器返回异常！");
                            throw new DGException("返回值不正确");
                        }
                        Code = res["code"].ToObject<int>();

                        switch (Code)
                        {
                            case 1:
                                SoundPlayerTool.SoundPlay("11解绑成功");
                                mainViewModel.SendMessage("解绑成功！", SnackbarColorModels.Success);
                                break;
                            case 2:
                                SoundPlayerTool.SoundPlay("错误提示3π无效");
                                mainViewModel.SendMessage("请扫描正确的πAir码");
                                break;
                            case 3:
                                SoundPlayerTool.SoundPlay("11解绑成功");
                                mainViewModel.SendMessage(res["info"].ToString(), SnackbarColorModels.Success);
                                break;
                            case 4:
                                SoundPlayerTool.SoundPlay("无权限通用");
                                mainViewModel.SendMessage(res["info"].ToString());
                                break;
                            default:
                                SoundPlayerTool.SoundPlay("失败通用");
                                mainViewModel.SendMessage(res["info"].ToString());
                                break;
                        }
                        ScanMode = 0;
                        ScanTips = "请扫描待绑定的快递单号";
                        break;
                    case 2:
                        // 开锁检查
                        // 二维码
                        if (CodeResult.Length > 68)
                        {
                            string fy3 = CodeResult[..28];
                            if (string.Compare(fy3, XWSApis.QRURL) == 0)
                            {
                                CodeString = CodeResult[32..];
                                Debug.WriteLine(CodeString);
                                mac = Public.UnlockString(CodeString);
                                Debug.WriteLine(mac);

                                jo["own_bid"] = MySqLite.GetConfig("id");
                                jo["iden"] = MySqLite.GetConfig("iden");
                                jo["mac"] = mac;

                                // 查询是否有权限
                                res = HttpRequest.PostRequest(XWSApis.IsRuleUrl, jo.ToString(0));

                                Code = res["code"].ToObject<int>();

                                switch (Code)
                                {
                                    case 1:
                                        //if (WindowsBlueToothStatus == 0)
                                        //{
                                        //    mainViewModel.SendMessage("蓝牙未开启，开锁任务已发送......");
                                        //}
                                        //else
                                        //{
                                        //    mainViewModel.SendMessage("正在开锁中......");
                                        //}

                                        TaskListModels taskListModels = new TaskListModels();
                                        taskListModels.mac = mac;
                                        mainViewModel.TaskList.Add(taskListModels);
                                        Task.Factory.StartNew(() =>
                                        {
                                            Thread.Sleep(3000);
                                            mainViewModel.SendMessage("开箱任务发送成功", SnackbarColorModels.Success);
                                            SoundPlayerTool.SoundPlay("8开箱成功");
                                        });
                                        
                                        break;
                                    case 2:
                                        SoundPlayerTool.SoundPlay("错误提示3π无效");
                                        mainViewModel.SendMessage("请扫描正确的πAir码");
                                        break;
                                    case 4:
                                        SoundPlayerTool.SoundPlay("无权限通用");
                                        mainViewModel.SendMessage(res["msg"].ToString());
                                        break;
                                    default:
                                        SoundPlayerTool.SoundPlay("失败通用");
                                        mainViewModel.SendMessage(res["msg"].ToString());
                                        break;
                                }

                                ScanMode = 0;
                                ScanTips = "请扫描待绑定的快递单号";
                                return;
                            }
                        }
                        SoundPlayerTool.SoundPlay("9请扫描设备");
                        mainViewModel.SendMessage("请扫描设备！");
                        throw new DGException("请扫描设备！");
                    case 0:
                    default:
                        // 绑定设备
                        // 判断是否有绑定快递
                        if (CourierNumber == null)
                        {
                            // 判断输入长度是否正确
                            if (CodeResult.Length < 8 || CodeResult.Length > 30)
                            {
                                SoundPlayerTool.SoundPlay("错误通用");
                                mainViewModel.SendMessage("输入异常，无效的单号！");
                                throw new DGException("订单异常，长度不正确");
                            }

                            jo["courier_number"] = CodeResult;
                            res = HttpRequest.PostRequest(XWSApis.CheckOrderUrl, jo.ToString(0));

                            if (res.Property("code") == null)
                            {
                                SoundPlayerTool.SoundPlay("ServiceError");
                                throw new DGException("返回值不正确");
                            }
                            Code = res["code"].ToObject<int>();
                            switch (Code)
                            {
                                case 0:
                                    SoundPlayerTool.SoundPlay("错误提示1单号无效");
                                    mainViewModel.SendMessage("请扫描正确的单号");
                                    break;
                                case 1:
                                    SoundPlayerTool.SoundPlay("成功提示1成功");
                                    mainViewModel.SendMessage("扫码成功，请扫待绑定πair码", SnackbarColorModels.Success);
                                    ScanTips = "请扫待绑定πair码";
                                    CourierNumber = CodeResult;
                                    break;
                                case 2:
                                    SoundPlayerTool.SoundPlay("错误提示2已绑定");
                                    mainViewModel.SendMessage("当前单号已绑定，请扫描正确的单号");
                                    break;
                            }
                        }
                        else
                        {
                            if (CodeResult.Length < 68 || CodeResult.Length > 80)
                            {
                                SoundPlayerTool.SoundPlay("错误通用");
                                mainViewModel.SendMessage("设备码异常，请扫待绑定πair码！");
                                throw new DGException("设备码异常，长度不正确！");
                            }

                            String fy = CodeResult.Substring(0, 28);
                            if (string.Compare(fy, XWSApis.QRURL) != 0)
                            {
                                SoundPlayerTool.SoundPlay("错误通用");
                                mainViewModel.SendMessage("请扫描待绑定的πair码！");
                                throw new DGException("请扫描待绑定的πair码！");
                            }

                            // 解密MAC地址
                            CodeString = CodeResult[32..];
                            Debug.WriteLine(CodeString);
                            mac = Public.UnlockString(CodeString);
                            Debug.WriteLine(mac);

                            jo["courier_number"] = CourierNumber;
                            jo["own_bid"] = MySqLite.GetConfig("id");
                            jo["iden"] = MySqLite.GetConfig("iden");
                            jo["mac"] = mac;

                            // 已经有快递单号了，准备绑定πAir
                            res = HttpRequest.PostRequest(XWSApis.IsBindingUrl, jo.ToString(0));

                            if (res.Property("code") == null)
                            {
                                SoundPlayerTool.SoundPlay("ServiceError");
                                throw new DGException("返回值不正确");
                            }
                            Code = res["code"].ToObject<int>();

                            switch (Code)
                            {
                                case 1:
                                    SoundPlayerTool.SoundPlay("成功提示2绑定");
                                    mainViewModel.SendMessage("绑定成功！等待下一个单号", SnackbarColorModels.Success);
                                    CourierNumber = null;
                                    ScanMode = 0;
                                    ScanTips = "请扫描待绑定的快递单号";
                                    break;
                                case 2:
                                    SoundPlayerTool.SoundPlay("错误提示3π无效");
                                    mainViewModel.SendMessage("请扫描正确的πAir码");
                                    break;
                                case 3:
                                    SoundPlayerTool.SoundPlay("错误提示4π已绑定");
                                    mainViewModel.SendMessage("当前πAir已绑定，请扫描正确的πAir码");
                                    break;
                                case 4:
                                    SoundPlayerTool.SoundPlay("无权限通用");
                                    mainViewModel.SendMessage(res["msg"].ToString());
                                    break;
                                default:
                                    SoundPlayerTool.SoundPlay("失败通用");
                                    mainViewModel.SendMessage(res["msg"].ToString());
                                    break;
                            }
                        }
                        break;
                }
            }
            catch (RuleException e)
            {
                Debug.WriteLine("权限报错：" + e.Message);
                ScanMode = 0;
                ScanTips = "请扫描待绑定的快递单号";
            }
            catch (DGException e)
            {
                Debug.WriteLine("已知错误：" + e.Message);
                ScanMode = 0;
                ScanTips = "请扫描待绑定的快递单号";
            }
            catch (JsonReaderException)
            {
                ScanMode = 0;
                ScanTips = "请扫描待绑定的快递单号";
                new Thread(() =>
                {
                    Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                        new Action(() =>
                        {
                            mainViewModel.SendMessage("网络异常！");
                        }));
                }).Start();
            }
            catch (WebException)
            {
                ScanMode = 0;
                ScanTips = "请扫描待绑定的快递单号";
                new Thread(() =>
                {
                    Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                        new Action(() =>
                        {
                            mainViewModel.SendMessage("网络异常！");
                        }));
                }).Start();
            }
            catch (Exception e)
            {
                ScanMode = 0;
                ScanTips = "请扫描待绑定的快递单号";
                Debug.WriteLine("未知错误：" + e.Message);
            }
        }

    }
}

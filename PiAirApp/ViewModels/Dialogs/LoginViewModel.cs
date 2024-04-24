using DryIoc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using YMModsApp.Common.Exceptions;
using YMModsApp.Common.Models;
using YMModsApp.Common.Tool;
using YMModsApp.Extensions;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace YMModsApp.ViewModels.Dialogs
{
    public class LoginViewModel : BindableBase, IDialogAware
    {
        //定义BackgroundWorker
        public BackgroundWorker backgroundWorker;

        public LoginViewModel(IEventAggregator aggregator)
        {
            //BackgroundWorker实例化
            backgroundWorker = new BackgroundWorker();
            //指示BackgroundWorker是否可以报告进度更新
            //当该属性值为True是，将可以成功调用ReportProgress方法，否则将引发InvalidOperationException异常。
            backgroundWorker.WorkerReportsProgress = true;
            //指示BackgroundWorker是否支持异步取消操作
            //当该属性值为True是，将可以成功调用CancelAsync方法，否则将引发InvalidOperationException异常。
            backgroundWorker.WorkerSupportsCancellation = true;
            //用于承载异步操作
            backgroundWorker.DoWork += backgroundWorker_DoWork;
            //报告操作进度
            backgroundWorker.ProgressChanged += backgroundWorker_ProgressChanged;
            //异步操作完成或取消时执行的操作，当调用DoWork事件执行完成时触发
            backgroundWorker.RunWorkerCompleted += backgroundWorker_RunWorkerCompleted;
            ExecuteCommand = new DelegateCommand<string>(Execute);
            this.aggregator = aggregator;
        }

        public string Title { get; set; } = "发货猫";

        private bool isLoading = false;
        public bool IsLoading
        {
            get { return isLoading; }
            set { SetProperty(ref isLoading, value); }
        }

        public event Action<IDialogResult> RequestClose;

        public bool CanCloseDialog()
        {
            return true;
        }

        public void OnDialogClosed()
        {
            LoginOut();
        }

        public void OnDialogOpened(IDialogParameters parameters)
        {
            UserName = MySqLite.GetConfig("username");
            PassWord = MySqLite.GetConfig("password");
        }

        #region Login
        public DelegateCommand<string> ExecuteCommand { get; private set; }


        private string userName;

        public string UserName
        {
            get { return userName; }
            set { userName = value; RaisePropertyChanged(); }
        }

        private string passWord;
        private readonly IEventAggregator aggregator;

        public string PassWord
        {
            get { return passWord; }
            set { passWord = value; RaisePropertyChanged(); }
        }

        private void Execute(string obj)
        {
            switch (obj)
            {
                case "Login": backgroundWorker.RunWorkerAsync(); break;
                case "LoginOut": LoginOut(); break;
            }
        }

        public void Login()
        {
            IsLoading = true;
            try
            {
                if (string.IsNullOrWhiteSpace(UserName) ||
            string.IsNullOrWhiteSpace(PassWord))
                {
                    throw new LoginException("账号密码不能为空");
                }
                JObject param = new JObject();
                param["login_account"] = UserName;
                param["login_pwd"] = PassWord;
                JObject res = HttpRequest.PostRequest(XWSApis.LoginUrl, param.ToString(0));
                int code = res["code"].ToObject<int>();
                switch (code)
                {
                    case 1:
                        res = res["data"].ToObject<JObject>();
                        int iden = res["iden"].ToObject<int>();
                        switch (iden)
                        {
                            // 0.管理员  1. 商户
                            case 0:
                                MySqLite.SaveConfig("nickname", res["nickname"].ToString());
                                break;
                            case 1:
                                MySqLite.SaveConfig("nickname", res["contacts"].ToString());
                                break;
                            default:
                                throw new LoginException("账户身份异常");
                        }
                        MySqLite.SaveConfig("username", UserName);
                        MySqLite.SaveConfig("password", PassWord);
                        MySqLite.SaveConfig("iden", iden.ToString());
                        MySqLite.SaveConfig("isLogin", "1");
                        new Thread(() =>
                        {

                            Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                                new Action(() =>
                                {
                                    RequestClose?.Invoke(new DialogResult(ButtonResult.OK));
                                }));
                        }).Start();
                        break;
                    default:
                        //登录失败提示...
                        throw new LoginException(res["info"].ToObject<string>());
                }
            }
            catch (LoginException e)
            {
                new Thread(() =>
                {
                    Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                        new Action(() =>
                        {
                            IsLoading = false;
                            aggregator.SendMessage(e.Message, "Login");
                        }));
                }).Start();
            }
            catch (JsonReaderException e)
            {
                new Thread(() =>
                {
                    Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                        new Action(() =>
                        {
                            IsLoading = false;
                            aggregator.SendMessage("网络异常！", "Login");
                        }));
                }).Start();
            }
            catch (WebException e)
            {
                new Thread(() =>
                {
                    Application.Current.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                        new Action(() =>
                        {
                            IsLoading = false;
                            aggregator.SendMessage("网络异常！", "Login");
                        }));
                }).Start();
            }
        }

        void LoginOut()
        {
            RequestClose?.Invoke(new DialogResult(ButtonResult.No));
            //(Application.Current as App)
        }

        #endregion

        //取消命令
        public DelegateCommand<Window> CancelCommand
        {
            get
            {
                return new DelegateCommand<Window>(Cancel);
            }
        }
        //取消业务
        public void Cancel(Window window)
        {
            //请求取消当前正在执行的异步操作
            backgroundWorker.CancelAsync();
        }
        //BackgroundWorker报告操作进度
        private void backgroundWorker_ProgressChanged(object sender, ProgressChangedEventArgs args)
        {
            //状态说明
            //object userState = args.UserState;

            //状态参数
            //progressBar.Value = args.ProgressPercentage;

        }
        //BackgroundWorker执行业务
        private void backgroundWorker_DoWork(object sender, DoWorkEventArgs args)
        {

            //指示应用程序是否已请求取消后台操作
            if (backgroundWorker.CancellationPending)
            {
                args.Cancel = true;
                return;
            }
            else
            {
                Login();
            }
        }
        ///BackgroundWorker执行完成
        //此处UI会阻塞，所以不要放过多的处理程序
        private void backgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs args)
        {
            if (args.Error != null)
            {
                MessageBox.Show(args.Error.ToString());
                return;
            }
            //线程取消
            if (args.Cancelled)
            {
                IsLoading = false;
            }
            if (!args.Cancelled)
            {
                Application.Current.Dispatcher.Invoke((Action)(() =>
                {

                }));
            }
        }
    }
}

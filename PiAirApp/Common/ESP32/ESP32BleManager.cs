using YMModsApp.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace YMModsApp.Common.ESP32
{
    class ESP32BleManager
    {
        private static ESP32BleManager intence = null;
        private static MainViewModel mainViewModel = null;
        public static ESP32BleManager getIntence()
        {
            if (intence == null)
            {
                intence = new ESP32BleManager();
                intence.Init();
            }
            return intence;
        }
        public static void RegisterMainViewModel(MainViewModel m)
        {
            mainViewModel = m;
        }

        public bool isWork()
        {
            bool b = false;
            foreach (KeyValuePair<string, ESP32BleCom> kv in dic)
            {
                if (kv.Value.connectType == ESPConnectType.BleWaiting || kv.Value.connectType == ESPConnectType.BleSending)
                {
                    b = true;
                    break;
                }
            }
            return b;
        }
        private void Init()
        {
            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    try
                    {
                        int BleWaitingNum = 0;
                        int BleSendingNum = 0;
                        int ComOpenedNum = 0;
                        //维护串口列表
                        string[] PortNames = System.IO.Ports.SerialPort.GetPortNames();
                        foreach (string name in PortNames)
                        {
                            //Console.Write("name:" + name);
                            ESP32BleCom esp32BleCom = null;
                            dic.TryGetValue(name, out esp32BleCom);
                            if (esp32BleCom == null)
                            {
                                esp32BleCom = new ESP32BleCom(name);
                                esp32BleCom.RegisterMainViewModel(mainViewModel);
                                dic[name] = esp32BleCom;
                            }
                        }
                        foreach (KeyValuePair<string, ESP32BleCom> kv in dic)
                        {
                            // 检查串口列表是否存在 遍历如果不存在了，设置成未插入状态
                            string key = "";
                            foreach (string name in PortNames)
                            {
                                if (name == kv.Key)
                                {
                                    key = name;
                                }
                            }
                            if (key == kv.Key)
                            {
                                if (kv.Value.connectType == ESPConnectType.ComNotInserted)
                                {
                                    kv.Value.connectType = ESPConnectType.ComInserted;
                                }
                            }
                            else
                            {
                                // 设置成未插入状态
                                kv.Value.connectType = ESPConnectType.ComNotInserted;
                            }
                            if (kv.Value.connectType == ESPConnectType.BleWaiting)
                            {
                                BleWaitingNum++;

                            }
                            if (kv.Value.connectType == ESPConnectType.BleSending)
                            {
                                BleSendingNum++;
                            }
                            if (kv.Value.connectType == ESPConnectType.ComOpened)
                            {
                                ComOpenedNum++;
                            }
                            if (mainViewModel.indexViewModel != null)
                            {
                                mainViewModel.indexViewModel.TaskBars[0].Content = BleWaitingNum.ToString();
                                mainViewModel.indexViewModel.TaskBars[1].Content = BleSendingNum.ToString();
                                mainViewModel.indexViewModel.TaskBars[2].Content = ComOpenedNum.ToString();
                            }
                        }
                        // 维护本地蓝牙状态
                        //switch (MainForm.WindowsBlueToothStatus)
                        //{
                        //    case 0:
                        //        ComOpenedNum++;
                        //        break;
                        //    case 1:
                        //        BleWaitingNum++;
                        //        break;
                        //    case 2:
                        //        BleSendingNum++;
                        //        break;
                        //}

                        //MainForm.form.SetWaitNum(BleWaitingNum);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("ESP32BleManager 121行：" + ex.ToString());
                    }
                    //MainForm.form.WriteLine(dic);
                    Thread.Sleep(100);
                }
            });
        }
        Dictionary<string, ESP32BleCom> dic = new Dictionary<string, ESP32BleCom>();
    }
}

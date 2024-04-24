using Microsoft.Xaml.Behaviors.Layout;
using YMModsApp.Common.Models;
using YMModsApp.Common.Tool;
using YMModsApp.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace YMModsApp.Common.ESP32
{
    enum ESPConnectType
    {
        ComNotInserted,//未插入
        ComInserted,//插入
        ComOpened,//已打开
        BleSending,//正在发送数据
        BleWaiting,//空闲等待
    }
    class ESP32BleCom
    {
        private SerialPort serialPort = null;
        public string PortName = "";
        public ESPConnectType connectType = ESPConnectType.ComNotInserted;
        /// <summary>
        /// 串口任务状态 0.正常 1.任务结束
        /// </summary>
        public int ProtStatus = 0;
        Stopwatch sw = new Stopwatch(); // 发送超时
        //Stopwatch msgsw = new Stopwatch(); // 消息返回超时
        //long nowElapsed = 0; // 当前逝去时间

        public ESP32BleCom(string name)
        {
            PortName = name;


            Task.Factory.StartNew(async () =>
            {
                while (true)
                {
                    //if (taskList != null)
                    //{
                    //    toDisconnect();
                    //    taskList.status = 3;
                    //    if (ProtStatus == 0)
                    //    {
                    //        taskList = null;
                    //    }
                    //    continue;
                    //}
                    switch (connectType)
                    {
                        case ESPConnectType.ComNotInserted:
                            // 串口异常断开
                            if (taskList != null)
                            {
                                string error = "ESP串口关闭";
                                Debug.WriteLine(error);
                                taskList.error_info = error;
                                XWSApis.SendToServer(3, error, taskList);
                                taskList.status = 3;
                            }
                            if (serialPort != null)
                            {
                                close();
                            }
                            break;
                        case ESPConnectType.ComInserted:
                            if (serialPort == null || serialPort.IsOpen == false)
                            {
                                open();
                            }
                            break;
                        case ESPConnectType.ComOpened:

                            break;
                        case ESPConnectType.BleWaiting:
                            // 已确认设备
                            if (taskList == null)
                            {
                                taskList = mainViewModel.GetTaskList();
                                //Debug.WriteLine("Object: " + taskList);
                                if (taskList != null)
                                {
                                    Debug.WriteLine("MAC: " + taskList.mac);
                                }

                                if (taskList != null && !string.IsNullOrEmpty(taskList.mac))
                                {
                                    //Debug.WriteLine("ESP32任务开始：name ：" + name + "MAC :" + taskList.data.mac);
                                    toConnect();
                                }
                                else
                                {
                                    // 没有空闲发送项
                                    //taskList = null;
                                }
                            }
                            else
                            {
                                // 等待完成
                                // 设备正在工作
                            }
                            break;
                        case ESPConnectType.BleSending:
                            // 设备正在工作
                            break;
                        default:
                            break;
                    }

                    task();
                    Thread.Sleep(200);
                }
            });
        }

        public void task()
        {
            if (sw.IsRunning)
            {
                if (sw.ElapsedMilliseconds > 20000 && ProtStatus != 1)
                {
                    Debug.WriteLine("当前逝去时间Elapsed ：" + sw.ElapsedMilliseconds + "毫秒");
                    Debug.WriteLine("D:esp32硬件超时...");
                    ProtStatus = 1;
                    End(false, "D:esp32硬件超时");
                    ProtStatus = 0;
                }

            }
            else
            {
                if (ProtStatus == 1)
                {
                    toDisconnect();
                    Debug.WriteLine("ESP32任务结束：" + sw.ElapsedMilliseconds + "毫秒");
                    if (sw.IsRunning) sw.Reset();
                    return;
                }
            }
        }

        private TaskListModels taskList = null;
        //结束
        public void End(bool b, string error = "")
        {
            ProtStatus = 1;
            if (sw.IsRunning)
            {
                sw.Reset();
            }
            if (b)
            {
                //taskList.status = 2;
                //taskList.SaveTask();
                taskList = null;
            }
            else
            {
                if (taskList != null)
                {
                    if (taskList.now_send_num >= taskList.max_send_num)
                    {
                        Debug.WriteLine("出现异常:" + error);
                        taskList.error_info = error;
                        XWSApis.SendToServer(3, error, taskList);
                        //taskList.SaveTask();
                        taskList = null;
                    }
                    else
                    {
                        // 重试延迟毫秒
                        Thread.Sleep(1000);
                        Debug.WriteLine("出现异常:" + error);
                        if (taskList != null)
                        {
                            taskList.error_info = error;
                            taskList.now_send_num++;
                            taskList.status = 0;
                            mainViewModel.TaskList.Add(taskList);
                        }
                    }
                }
            }

            ProtStatus = 0;
        }

        //打开com口
        private void open()
        {
            close();
            serialPort = new SerialPort();
            serialPort.Dispose();
            serialPort.PortName = PortName;//通信端口
            serialPort.BaudRate = 256000;//波特率
            serialPort.Encoding = Encoding.ASCII;
            serialPort.Parity = Parity.None;//奇偶校验位
            serialPort.DataBits = 8;//标准数据位长度
            serialPort.StopBits = StopBits.One;//每个字节的标准停止位数
            serialPort.DataReceived += new SerialDataReceivedEventHandler(Serial_DataReceived);
            try
            {
                serialPort.Open();
                if (serialPort.IsOpen)
                {
                    connectType = ESPConnectType.ComOpened;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("ESP32BleCom - 159行：" + ex.Message);
                close();
            }
        }

        pack_t pack = new pack_t();
        private MainViewModel mainViewModel;

        /// <summary>
        /// 接受串口返回
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Serial_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            int len = serialPort.BytesToRead;
            byte[] buf = new byte[len];
            serialPort.Read(buf, 0, len);
            pack.msg.AddRange(buf);
            ESP32BleTools.pack_decode(pack);

            foreach (List<byte> body in pack.bodys)
            {
                MsgType msgType = (MsgType)body[0];
                //Debug.WriteLine("111111111111串口返回11111111111111:" + msgType);
                //msgsw.Restart();
                switch (msgType)
                {
                    //返回消息
                    case MsgType.s2c_Success:
                        Debug.WriteLine("msgType:" + msgType);
                        if (taskList == null)
                        {
                            continue;
                        }
                        switch (taskList.type)
                        {
                            case 1:
                                //Public.SendToServer(2, "ESP32切换图1", taskList);
                                //手动关闭
                                toDisconnect();
                                End(true);
                                break;

                            case 2:
                                //手动关闭
                                toDisconnect();
                                End(true);
                                break;
                            case 3:
                                XWSApis.SendToServer(3, "ESP32开锁", taskList);
                                //手动关闭
                                toDisconnect();
                                End(true);
                                break;
                            default:
                                //taskList.data.power = body[1];
                                //if (Public.SendToServer(true, "发货成功", taskList))
                                //{
                                //    //手动关闭
                                //    toDisconnect();
                                //    End(true);
                                //}
                                //else
                                //{
                                //    //通知回滚
                                //    toRollBack();
                                //    End(false, "服务器异常");
                                //}
                                break;
                        }

                        break;
                    case MsgType.s2c_Error01:
                        Debug.WriteLine("msgType:" + msgType);
                        End(false, "超时");
                        break;
                    case MsgType.s2c_Error02:
                        Debug.WriteLine("msgType:" + msgType);
                        End(false, "蓝牙连接失败");
                        break;
                    case MsgType.s2c_Error03:
                        Debug.WriteLine("msgType:" + msgType);
                        End(false, "主服务未找到");
                        break;
                    case MsgType.s2c_Error04:
                        Debug.WriteLine("msgType:" + msgType);
                        End(false, "写服务未找到");
                        break;
                    case MsgType.s2c_Error05:
                        Debug.WriteLine("msgType:" + msgType);
                        End(false, "通知服务未找到");
                        break;
                    case MsgType.s2c_Error06:
                        Debug.WriteLine("msgType:" + msgType);
                        End(false, "状态切换失败");
                        break;
                    case MsgType.s2c_Error07:
                        Debug.WriteLine("msgType:" + msgType);
                        End(false, "设置E-INK1失败");
                        break;
                    case MsgType.s2c_Error08:
                        Debug.WriteLine("msgType:" + msgType);
                        End(false, "设置E-INK2失败");
                        break;
                    case MsgType.s2c_Error09:
                        Debug.WriteLine("msgType:" + msgType);
                        End(false, "开锁失败");
                        break;
                    case MsgType.s2c_Error0A:
                        Debug.WriteLine("msgType:" + msgType);
                        End(false, "写入失败");
                        break;
                    case MsgType.s2c_Error0B:
                        Debug.WriteLine("msgType:" + msgType);
                        End(false, "CRC16校验失败");
                        break;
                    case MsgType.s2c_Error0C:
                        Debug.WriteLine("msgType:" + msgType);
                        End(false, "目标设备异常断开连接");
                        break;
                    case MsgType.s2c_Error0E:
                        Debug.WriteLine("msgType:" + msgType);
                        End(false, "ESP32硬件超时");
                        break;
                    case MsgType.s2c_State_BleWaiting:
                        if (connectType != ESPConnectType.BleWaiting)
                        {
                            Debug.WriteLine("msgType:" + msgType);
                            connectType = ESPConnectType.BleWaiting;
                        }
                        break;
                    case MsgType.s2c_State_BleSending:
                        Debug.WriteLine("msgType:" + msgType);
                        if (connectType != ESPConnectType.BleSending)
                        {
                            connectType = ESPConnectType.BleSending;
                        }
                        break;
                }
            }
            pack.bodys.Clear();
        }

        // 关闭com口
        private void close()
        {
            //try
            //{
            //    if (serialPort != null && serialPort.IsOpen)
            //    {
            //        // serialPort.Close();
            //    }
            //}
            //catch (Exception ex)
            //{
            //    Debug.WriteLine("ESP32BleCom - 375行：" + ex.Message);
            //}
            serialPort = null;
        }

        // 开始连接并发送数据
        private void toConnect()
        {
            ProtStatus = 0;
            if (sw.IsRunning) sw.Reset();
            sw.Start();
            if (taskList != null)
            {
                switch (taskList.type)
                {
                    //case 1:
                    //    toResetLink1();
                    //    break;

                    //case 2:
                    //    toResetLink3();
                    //    break;
                    case 3:
                        toOpenBox();
                        break;
                    default:
                        //ble_ask_t ask = new ble_ask_t();
                        //ask.setData(taskList.data);
                        //ask.buffer.Insert(0, (byte)MsgType.c2s_Connect);
                        //toSend(ask.buffer);
                        break;
                }
            }
        }

        // 重置图1
        //private void toResetLink1()
        //{
        //    ble_ask_t ask = new ble_ask_t();
        //    ask.setDataResetLink1(taskList.data);
        //    ask.buffer.Insert(0, (byte)MsgType.c2s_ResetLink1);
        //    toSend(ask.buffer);
        //}

        //// 重置图3
        //private void toResetLink3()
        //{
        //    ble_ask_t ask = new ble_ask_t();
        //    ask.setDataResetLink3(taskList.data);
        //    ask.buffer.Insert(0, (byte)MsgType.c2s_ResetLink3);
        //    toSend(ask.buffer);
        //}

        //// 重置图3
        private void toOpenBox()
        {
            ble_ask_t ask = new ble_ask_t();
            ask.setDataOpenBox(taskList);
            ask.buffer.Insert(0, (byte)MsgType.c2s_OpenBox);
            toSend(ask.buffer);
        }

        // 访问服务器失败，数据回滚
        private void toRollBack()
        {
            List<byte> buffer = new List<byte>() { (byte)MsgType.c2s_RollBack };
            toSend(buffer);
        }

        // 断开连接
        private void toDisconnect()
        {
            List<byte> buffer = new List<byte>() { (byte)MsgType.c2s_Disconnect };
            toSend(buffer);
        }

        // 透传数据
        private void toSend(List<byte> _buf)
        {
            if (serialPort != null && serialPort.IsOpen)
            {
                byte[] buffer = ESP32BleTools.pack_encode(_buf).ToArray();
                Debug.WriteLine("打印发送的信息：");
                Debug.WriteLine(BitConverter.ToString(buffer));
                serialPort.Write(buffer, 0, buffer.Length);
                Debug.WriteLine("buffer.Length:" + buffer.Length);
            }
        }

        public void RegisterMainViewModel(MainViewModel m)
        {
            mainViewModel = m;
        }
    }
}
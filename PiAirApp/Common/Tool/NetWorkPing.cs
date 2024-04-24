using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace YMModsApp.Common.Tool
{
    public class NetWorkTool
    {
        /// <summary>
        /// 使用Ping测试网络
        /// </summary>
        /// <param name="host"></param>
        /// <returns></returns>
        public static PingReply PingTest(string host = "www.baidu.com")
        {
            Ping p1 = new Ping();
            PingReply reply;
            try
            {
                p1 = new Ping();
                reply = p1.Send(host); //发送主机名或Ip地址

            }
            catch (Exception ex)
            {
                reply = null;
            }
            finally
            {
                p1.Dispose();
            }
            return reply;
        }

        /// <summary>
        /// 获取本地IP地址信息
        /// </summary>
        public static string GetAddressIP()
        {
            ///获取本地的IP地址
            string AddressIP = string.Empty;
            foreach (IPAddress _IPAddress in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
            {
                if (_IPAddress.AddressFamily.ToString() == "InterNetwork")
                {
                    AddressIP = _IPAddress.ToString();
                }
            }
            return AddressIP;
        }

        public static PingReply PingTest2(string ip = "180.101.50.188")
        {
            PingReply reply = null;
            Ping pingSender = null;
            try
            {
                pingSender = new Ping();

                PingOptions options = new PingOptions();
                options.DontFragment = true;

                string data = "hello world";
                byte[] buffer = Encoding.ASCII.GetBytes(data);
                int timeout = 1000;

                IPAddress ipa = IPAddress.Parse(ip);
                PingReply replyPing = pingSender.Send(ip, timeout, buffer, options);
                reply = replyPing;
            }
            catch (Exception ex)
            {
                reply = null;
            }
            finally
            {
                pingSender.Dispose();
            }
            return reply;
        }

        public String GetIPGlobal()
        {
            //得到本机Internet协议IPV4的统计数据;
            IPGlobalProperties properties = IPGlobalProperties.GetIPGlobalProperties();
            IPGlobalStatistics ipstat = properties.GetIPv4GlobalStatistics();

            Console.WriteLine("  Inbound Packet Data:");
            //获取收到的 Internet 协议 (IP) 数据包数
            Console.WriteLine("      Received ............................ : {0}", ipstat.ReceivedPackets);
            //获取转发的 Internet 协议 (IP) 数据包数
            Console.WriteLine("      Forwarded ........................... : {0}", ipstat.ReceivedPacketsForwarded);
            //获取传送的 Internet 协议(IP) 数据包数
            Console.WriteLine("      Delivered ........................... : {0}", ipstat.ReceivedPacketsDelivered);
            //获取已收到但被丢弃的 Internet 协议 (IP) 数据包数
            Console.WriteLine("      Discarded ........................... : {0}", ipstat.ReceivedPacketsDiscarded);

            double percent = (double)ipstat.ReceivedPacketsDiscarded / ipstat.ReceivedPacketsDelivered;
            string packetsPercent = percent.ToString("P");

            return packetsPercent;

        }
    }
}

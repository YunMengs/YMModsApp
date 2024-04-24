using System.Windows.Controls;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using System;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Win32;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using System.Windows.Documents;

namespace YMModsApp.Common.Tool
{
    public class Public
    {
        /// <summary>
        /// 设置开机自启
        /// </summary>
        public static void AutoStart(bool start)
        {
            //计算机\HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Run
            //计算机\HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Run
            string strName = AppDomain.CurrentDomain.BaseDirectory + "YMModsApp.exe";//获取要自动运行的应用程序名
            // string regPath = Public.Is64Bit() ? "SOFTWARE\\WOW6432Node\\Microsoft\\Windows\\CurrentVersion\\Run" : "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run";
            string regPath = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run";
            if (!System.IO.File.Exists(strName))//判断要自动运行的应用程序文件是否存在
                return;
            string strnewName = strName.Substring(strName.LastIndexOf("\\") + 1);//获取应用程序文件名，不包括路径
            RegistryKey registry = Registry.LocalMachine.OpenSubKey(regPath, true);//检索指定的子项
            if (registry == null)//若指定的子项不存在
                registry = Registry.LocalMachine.CreateSubKey(regPath);//则创建指定的子项

            if (start)
            {
                registry.SetValue(strnewName, strName);//设置该子项的新的“键值对”
            }
            else
            {
                registry.DeleteValue(strnewName, false);//删除指定“键名称”的键/值对
            }
        }

        /// <summary>
        /// 获取URL 根据系统判断自动使用http和https
        /// 返回值为True则表示是64位，如返回值为False则表示为32位
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static bool Is64Bit()
        {
            return Environment.Is64BitOperatingSystem;
        }

        public static void CheckUpgrade()
        {

        }

        /// <summary>
        /// 获取软件版本号
        /// </summary>
        /// <returns></returns>
        public static string GetVersion()
        {
            return System.Diagnostics.FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly().Location).FileVersion;//软件版本号

        }

        public static Color GetMediaColorFromDrawingColor(string co)
        {
            System.Drawing.Color color = System.Drawing.ColorTranslator.FromHtml(co);
            return Color.FromArgb(color.A, color.R, color.G, color.B);
        }

        public static T FindChild<T>(DependencyObject parent, string childName)
    where T : DependencyObject
        {
            // Confirm parent and childName are valid. 
            if (parent == null) return null;

            T foundChild = null;

            int childrenCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childrenCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                // If the child is not of the request child type child
                T childType = child as T;
                if (childType == null)
                {
                    // recursively drill down the tree
                    foundChild = FindChild<T>(child, childName);

                    // If the child is found, break so we do not overwrite the found child. 
                    if (foundChild != null) break;
                }
                else if (!string.IsNullOrEmpty(childName))
                {
                    var frameworkElement = child as FrameworkElement;
                    // If the child's name is set for search
                    if (frameworkElement != null && frameworkElement.Name == childName)
                    {
                        // if the child's name is of the request name
                        foundChild = (T)child;
                        break;
                    }
                }
                else
                {
                    // child element found.
                    foundChild = (T)child;
                    break;
                }
            }

            return foundChild;
        }

        /// <summary>
        /// Base64加密，解密方法
        /// </summary>
        /// <paramname="s">输入字符串</param>
        /// <paramname="c">true-加密,false-解密</param>
        public static string base64(string s, bool c)
        {
            if (c)
            {
                return System.Convert.ToBase64String(System.Text.Encoding.Default.GetBytes(s));
            }
            else
            {
                try
                {
                    return System.Text.Encoding.Default.GetString(System.Convert.FromBase64String(s));
                }
                catch (Exception exp)
                {
                    return exp.Message;
                }
            }
        }

        /// <summary>
        /// 32位MD5加密
        /// </summary>
        /// <param name="srcstr">需要加密的字符串</param>
        /// <returns></returns>
        public static string TO32MD5(string srcstr)
        {
            MD5 md5 = MD5.Create();
            string md5str = "";//加密后的string
            byte[] s = md5.ComputeHash(Encoding.UTF8.GetBytes(srcstr));
            for (int i = 0; i < s.Length; i++)
            {
                string btos = s[i].ToString("X2");//每次转换得到的都是2位
                md5str += btos;//转换成十六进制
            }
            return md5str;
        }

        /// <summary>
        /// 小程序加密串解密
        /// </summary>
        /// <returns></returns>
        public static string UnlockString(string CodeString, string key = "fuyi")
        {
            string base64String = System.Web.HttpUtility.UrlDecode(CodeString, System.Text.Encoding.UTF8);
            string text = Public.base64(base64String, false);
            string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-=+";
            char ch = text[0];
            int nh = chars.IndexOf(ch);
            string mdKey = Public.TO32MD5(key + ch);
            mdKey = mdKey.Substring(nh % 8, nh % 8 + 7).ToLower();
            text = text.Substring(1);
            string tmp = "";
            int k = 0, j = 0;
            for (int i = 0; i < text.Length; i++)
            {
                k = (k == (mdKey.Length)) ? 0 : k;
                j = chars.IndexOf(text[i]) - nh - (int)mdKey[k++];
                while (j < 0) j += 64;
                tmp += chars[j];
            }
            string b = Public.base64(tmp, false).Trim(key.ToCharArray());
            return b;

        }

        public static ushort[] wCRCTalbeAbs = {
            0x0000, 0xCC01, 0xD801, 0x1400, 0xF001,
            0x3C00, 0x2800, 0xE401, 0xA001, 0x6C00,
            0x7800, 0xB401, 0x5000, 0x9C01, 0x8801,
            0x4400};

        public static ushort CRC16(ref byte[] pchMsg)
        {
            ushort wCRC = 0xFFFF;
            byte chChar;
            int len = pchMsg.Length - 2;
            for (int i = 0; i < len; i++)
            {
                chChar = pchMsg[i];
                wCRC = (ushort)(wCRCTalbeAbs[(chChar ^ wCRC) & 15] ^ (wCRC >> 4));
                wCRC = (ushort)(wCRCTalbeAbs[((chChar >> 4) ^ wCRC) & 15] ^ (wCRC >> 4));
            }
            byte[] crc = GetBytes(wCRC);
            pchMsg[pchMsg.Length - 2] = crc[0];
            pchMsg[pchMsg.Length - 1] = crc[1];
            return wCRC;
        }

        /// <summary>
        /// 16进制字符串转换成16进制byte数组
        /// </summary>
        /// <param name="hexString"></param>
        /// <returns></returns>
        public static byte[] strToHexByte(string hexString)
        {
            hexString = hexString.Replace(":", "");
            if ((hexString.Length % 2) != 0)
                hexString += " ";
            byte[] returnBytes = new byte[hexString.Length / 2];
            for (int i = 0; i < returnBytes.Length; i++)
                returnBytes[i] = Convert.ToByte(hexString.Substring(i * 2, 2), 16);
            return returnBytes;
        }

        public static byte[] GetBytes(ushort us)
        {
            byte[] _byte = BitConverter.GetBytes(us);
            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(_byte);
            }
            return _byte;
        }

        /// <summary>
        /// 获取当前的毫秒时间戳
        /// </summary>
        /// <returns></returns>
        public static long Timestamp()
        {
            return ConvertDateTimeToInt(DateTime.Now);
        }

        /// <summary>  
        /// 将c# DateTime时间格式转换为Unix时间戳格式  
        /// </summary>  
        /// <param name="time">时间</param>  
        /// <returns>long</returns>  
        public static long ConvertDateTimeToInt(System.DateTime time)
        {
            return (time.Ticks - 621356256000000000) / 10000;
        }
    }
}

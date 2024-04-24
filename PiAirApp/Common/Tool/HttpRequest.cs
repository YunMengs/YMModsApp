using DryIoc;
using Newtonsoft.Json.Linq;
using YMModsApp.Common.Exceptions;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace YMModsApp.Common.Tool
{

    class HttpRequest
    {
        /// <summary>
        /// Get数据接口
        /// </summary>
        /// <param name="getUrl">接口地址</param>
        /// <returns></returns>
        public static string GetWebRequest(string getUrl)
        {
            string responseContent = "";
            if (getUrl.Substring(0, 5).ToLower() == "https")
            {
                ServicePointManager.SecurityProtocol = (SecurityProtocolType)192 | (SecurityProtocolType)768 | (SecurityProtocolType)3072;
            }

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(getUrl);
            request.ContentType = "application/json";
            request.Method = "GET";
            request.Timeout = 10000;
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            //在这里对接收到的页面内容进行处理
            using (Stream resStream = response.GetResponseStream())
            {
                using (StreamReader reader = new StreamReader(resStream, Encoding.UTF8))
                {
                    responseContent = reader.ReadToEnd().ToString();
                    Debug.WriteLine("-->Get Success");
                    Debug.WriteLine("-->url:" + getUrl);
                    Debug.WriteLine("-->resp:" + responseContent);
                }
            }
            return responseContent;
        }

        /// <summary>
        /// Get数据接口
        /// </summary>
        /// <param name="getUrl">接口地址</param>
        /// <returns></returns>
        public static JObject GetRequest(string getUrl)
        {
            string ret = HttpRequest.GetWebRequest(GetURL(getUrl));

            JObject res = JObject.Parse(ret);
            if (res == null || res.Property("code") == null)
            {
                throw new NetWorkException("返回值不正确");
            }

            return res;
        }

        /// <summary>
        /// Post数据接口
        /// </summary>
        /// <param name="postUrl">接口地址</param>
        /// <param name="paramData">提交json数据</param>
        /// <returns></returns>
        public static string PostWebRequest(string postUrl, string paramData)
        {
            string responseContent = string.Empty;
            try
            {
                if (postUrl.Substring(0, 5).ToLower() == "https")
                {
                    ServicePointManager.SecurityProtocol = (SecurityProtocolType)192 | (SecurityProtocolType)768 | (SecurityProtocolType)3072;
                }
                byte[] byteArray = Encoding.UTF8.GetBytes(paramData); //转化
                HttpWebRequest webReq = (HttpWebRequest)WebRequest.Create(new Uri(postUrl));
                webReq.Method = "POST";
                webReq.ContentType = "application/json";
                webReq.ContentLength = byteArray.Length;
                webReq.Timeout = 10000;
                //webReq.ServicePoint.Expect100Continue = false;
                using (Stream reqStream = webReq.GetRequestStream())
                {
                    reqStream.Write(byteArray, 0, byteArray.Length);//写入参数
                }
                using (HttpWebResponse response = (HttpWebResponse)webReq.GetResponse())
                {
                    //在这里对接收到的页面内容进行处理
                    using (StreamReader sr = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
                    {
                        responseContent = sr.ReadToEnd().ToString();

                        Debug.WriteLine("-->Post Success");
                        Debug.WriteLine("-->url:" + postUrl);
                        Debug.WriteLine("-->post:" + paramData);
                        Debug.WriteLine("-->resp:" + responseContent);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("-->Post Error");
                Debug.WriteLine("-->url:" + postUrl);
                Debug.WriteLine("-->msg:" + ex.Message);
                return ex.Message;
            }
            return responseContent;
        }

        /// <summary>
        /// Post数据接口
        /// </summary>
        /// <param name="postUrl">接口地址</param>
        /// <param name="paramData">提交json数据</param>
        /// <returns></returns>
        public static JObject PostRequest(string postUrl, string paramData)
        {
            string ret = HttpRequest.PostWebRequest(GetURL(postUrl), paramData);

            JObject res = JObject.Parse(ret);
            if (res == null || res.Property("code") == null)
            {
                throw new NetWorkException("返回值不正确");
            }

            return res;
        }

        /// <summary>
        /// 获取URL 根据系统判断自动使用http和https
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static string GetURL(string url)
        {
            //Debug.WriteLine("Environment.OSVersion.Version.Major:" + Environment.OSVersion.Version.Major);
            //Debug.WriteLine("System.Environment.OSVersion.Version.Minor:" + System.Environment.OSVersion.Version.Minor);
            if (((Environment.OSVersion.Version.Major * 10) + System.Environment.OSVersion.Version.Minor < 62))
            {
                // win10以下
                return "http://" + url;
            }
            else
            {
                // win10及以上
                return "https://" + url;
            }
        }
    }
}
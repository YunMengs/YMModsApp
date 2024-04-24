using Newtonsoft.Json.Linq;
using YMModsApp.Common.Tool;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YMModsApp.Common.Models
{
    public static class XWSApis
    {
        /// <summary>
        /// 域名
        /// </summary>
        public const string DemoinName = "app.cnsmarto.com";

        /// <summary>
        /// 二维码
        /// </summary>
        public const string QRURL = "https://box.cnsmarto.com/mac";

        /// <summary>
        /// 最新版本
        /// </summary>
        public const string VersionURL = "xwsoss.oss-cn-hangzhou.aliyuncs.com/PC/PiAIr/version.json";

        /// <summary>
        /// 登录接口
        /// </summary>
        public const string LoginUrl = DemoinName + "/dgcatPiAirApi/Login/login";

        /// <summary>
        /// 绑定订单
        /// </summary>
        public const string IsBindingUrl = DemoinName + "/dgcatPiAirApi/Index/isBinding";

        /// <summary>
        /// 取消绑定订单
        /// </summary>
        public const string IsUnBindingUrl = DemoinName + "/dgcatPiAirApi/Index/isUnbinding";

        /// <summary>
        /// 权限检测
        /// </summary>
        public const string IsRuleUrl = DemoinName + "/dgcatPiAirApi/Index/isRule";

        /// <summary>
        /// 检测订单
        /// </summary>
        public const string CheckOrderUrl = DemoinName + "/dgcatPiAirApi/Index/index";

        /// <summary>
        /// 发货猫完成后调用（开锁版）
        /// </summary>
        public const string DGUrl = DemoinName + "/dgcatapi/deliver_goods";

        /// <summary>
        /// 发送至服务器
        /// </summary>
        /// <param name="isSuccess"></param>
        /// <param name="info"></param>
        /// <param name="tasklist"></param>
        /// <returns>成功 or 失败</returns>
        public static bool SendToServer(int code, string info, TaskListModels tasklist)
        {
            tasklist.ip = NetWorkTool.GetAddressIP();
            JObject jo = tasklist.ToJson();
            jo["code"] = code;
            jo["info"] = info;
            // 登录信息
            jo["iden"] = MySqLite.GetConfig("iden");
            jo["iden_id"] = MySqLite.GetConfig("id");
            jo["operator_id"] = MySqLite.GetConfig("id");

            try
            {
                //发送数据到服务器
                JObject jo_ret = HttpRequest.PostRequest(XWSApis.DGUrl, jo.ToString(0));
                bool b = jo_ret["code"].ToObject<int>() == 1 ? true : false;
                if (b)
                {
                    Debug.WriteLine("服务端发送成功");
                }
                else
                {
                    Debug.WriteLine("服务端发送失败");
                }
                return b;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("SendToServer: " + ex.Message);
                return false;
            }
        }
    }
}

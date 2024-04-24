using Newtonsoft.Json.Linq;
using YMModsApp.Common.Tool;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;
using static YMModsApp.Common.Tool.MySqLite;

namespace YMModsApp.Common.Models
{
    public class TaskListModels
    {
        /// <summary>
        /// 数据库队列ID
        /// </summary>
        public long id = 0;

        /// <summary>
        /// Mac地址
        /// </summary>
        public string mac = "";

        /// <summary>
        /// 设备编号ID
        /// </summary>
        public string case_number = "";

        /// <summary>
        /// 快递单号
        /// </summary>
        public string courier_number = "";

        /// <summary>
        /// 任务类型 0.发货 1.重置 2.OTA批量刷图3 3.开锁模式
        /// </summary>
        public long type = 3;

        /// <summary>
        /// 任务状态 0.等待中 1.发送中 2.成功 3.失败 4.扫码等待中 5.任务废弃
        /// </summary>
        public long status = 0;

        /// <summary>
        /// 最大重试次数
        /// </summary>
        public long max_send_num = 2;

        /// <summary>
        /// 当前重试次数
        /// </summary>
        public long now_send_num = 0;

        /// <summary>
        /// 超级密码
        /// </summary>
        public string super_pwd = "qwer1234";

        /// <summary>
        /// IP
        /// </summary>
        public string ip = "";

        /// <summary>
        /// 创建时间
        /// </summary>
        public long create_time = 0;

        /// <summary>
        /// 更新时间
        /// </summary>
        public long update_time = 0;

        /// <summary>
        /// 错误信息
        /// </summary>
        public string error_info = "";

        /// <summary>
        /// 添加任务队列
        /// </summary>
        /// <returns>添加ID</returns>
        public long SaveTask()
        {
            create_time = Public.Timestamp();
            update_time = Public.Timestamp();
            string sql = "INSERT INTO `task_list` VALUES(";
            sql += "null,'";
            sql += mac + "','";
            sql += case_number + "','";
            sql += courier_number + "',";
            sql += type + ",";
            sql += status + ",";
            sql += max_send_num + ",";
            sql += now_send_num + ",'";
            sql += super_pwd + "',";
            sql += create_time + ",";
            sql += update_time + ")";
            id = MySqLite.ExecuteNonQueryGetID(sql);
            return id;
        }

        /// <summary>
        /// 获取一条未执行的任务队列
        /// </summary>
        /// <returns>受影响行数</returns>
        public void GetFirstTask()
        {
            string sql = "SELECT * FROM `task_list` WHERE `status` = 0";
            SQLiteDataReader reader = MySqLite.ExecuteQueryFirst(sql);
            if (reader.Read())
            {
                id = (long)reader["id"];
                mac = (string)reader["mac"];
                case_number = (string)reader["case_number"];
                courier_number = (string)reader["courier_number"];
                type = (long)reader["type"];
                max_send_num = (long)reader["max_send_num"];
                now_send_num = (long)reader["now_send_num"];
                super_pwd = (string)reader["super_pwd"];
                create_time = (long)reader["create_time"];
                update_time = (long)reader["update_time"];
            }
            else
            {
                Debug.WriteLine("没有查询到数据");
            }
        }

        /// <summary>
        /// 删除任务队列
        /// </summary>
        /// <returns>受影响行数</returns>
        public long DelTask()
        {
            string sql = "DELETE FROM `task_list` WHERE `id` = " + id;
            return MySqLite.ExecuteNonQuery(sql);
        }

        /// <summary>
        /// ToJSON
        /// </summary>
        /// <returns></returns>
        public JObject ToJson()
        {
            JObject jo = new JObject();
            jo["mac"] = mac;
            jo["super_pwd"] = super_pwd;
            jo["courier_number"] = courier_number;
            jo["express_number"] = courier_number;
            jo["case_number"] = case_number;
            jo["ip"] = ip;

            return jo;
        }
    }
}

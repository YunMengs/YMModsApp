using YMModsApp.Common.Models;
using YMModsApp.Common.Tool;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YMModsApp.Common.ESP32
{
    enum MsgType
    {
        //返回消息
        s2c_Success = 0x00,//发送成功
        s2c_Error01 = 0x01,//超时
        s2c_Error02 = 0x02,//蓝牙连接失败
        s2c_Error03 = 0x03,//主服务未找到
        s2c_Error04 = 0x04,//写服务未找到
        s2c_Error05 = 0x05,//通知服务未找到
        s2c_Error06 = 0x06,//状态切换失败
        s2c_Error07 = 0x07,//设置E-INK1失败
        s2c_Error08 = 0x08,//设置E-INK2失败
        s2c_Error09 = 0x09,//开锁失败
        s2c_Error0A = 0x0A,//写入失败
        s2c_Error0B = 0x0B,//CRC16校验失败
        s2c_Error0C = 0x0C,//设备异常断开连接
        s2c_Error0E = 0x0E,//超时

        s2c_State_BleWaiting = 0xB0,//返回状态信息
        s2c_State_BleSending = 0xB1,//返回状态信息

        //接收消息
        c2s_State = 0xA0,//查询状态信息
        c2s_Connect = 0xA1,//连接蓝牙 发送数据
        c2s_RollBack = 0xA2,//切换回E-INK1
        c2s_Disconnect = 0xA3,//断开蓝牙
        c2s_ResetLink1 = 0xA4,// 重置图1
        c2s_ResetLink3 = 0xA5,// 重置图3
        c2s_OpenBox = 0xA7,// 开箱

    };
    class ESP32BleTools
    {
        static ushort[] wCRCTalbeAbs = {
            0x0000, 0xCC01, 0xD801, 0x1400, 0xF001,
            0x3C00, 0x2800, 0xE401, 0xA001, 0x6C00,
            0x7800, 0xB401, 0x5000, 0x9C01, 0x8801,
            0x4400};
        static UInt16 CRC16(List<byte> pchMsg, UInt16 len)
        {
            UInt16 wCRC = 0xFFFF;
            byte chChar;
            for (UInt16 i = 0; i < len; i++)
            {
                chChar = pchMsg[i];
                wCRC = (UInt16)(wCRCTalbeAbs[(chChar ^ wCRC) & 15] ^ (wCRC >> 4));
                wCRC = (UInt16)(wCRCTalbeAbs[((chChar >> 4) ^ wCRC) & 15] ^ (wCRC >> 4));
            }
            return wCRC;
        }

        public static void write_uint16(ref List<byte> msg, UInt16 value)
        {
            msg.Add((byte)(0xff & value));
            msg.Add((byte)((0xff00 & value) >> 8));
        }

        public static UInt16 read_uint16(List<byte> msg, UInt16 idx)
        {
            UInt16 ret = (UInt16)(msg[idx + 0] & 0xFF);
            ret |= (UInt16)((msg[idx + 1] << 8) & 0xFF00);
            return ret;
        }
        //双重CRC校验
        //解码 head[2] len[2] crc1[2] body[n] crc2[2]  
        //head:包头   len:包体长度   crc1:包头+包体长度 校验   body:包体   crc2：包头+包体长度+包体 校验
        public static void pack_decode(pack_t pack)
        {
            while (true)
            {
                UInt16 len = (UInt16)pack.msg.Count;
                if (len >= 8)
                {
                    //正常一个包至少8字节
                    if (pack.msg[0] == 0xAA && pack.msg[1] == 0xAA)
                    {
                        if (CRC16(pack.msg, 4) == read_uint16(pack.msg, 4))
                        {
                            UInt16 body_len = read_uint16(pack.msg, 2);
                            UInt16 pack_len = (UInt16)(body_len + 8);
                            if (len >= pack_len)
                            {
                                UInt16 crc2_idx = (UInt16)(body_len + 6);
                                if (CRC16(pack.msg, crc2_idx) == read_uint16(pack.msg, crc2_idx))
                                {
                                    //删除包头
                                    pack.msg.RemoveAt(0);
                                    pack.msg.RemoveAt(0);
                                    //删除包体长度
                                    pack.msg.RemoveAt(0);
                                    pack.msg.RemoveAt(0);
                                    //删除校验1
                                    pack.msg.RemoveAt(0);
                                    pack.msg.RemoveAt(0);
                                    //获得包体
                                    List<byte> byte_arr = new List<byte>();
                                    for (UInt16 i = 0; i < body_len; i++)
                                    {
                                        byte_arr.Add(pack.msg[0]);
                                        pack.msg.RemoveAt(0);
                                    }
                                    //添加到包体列表
                                    pack.bodys.Add(byte_arr);
                                    //删除校验2
                                    pack.msg.RemoveAt(0);
                                    pack.msg.RemoveAt(0);
                                }
                                else
                                {
                                    //校验失败 删除第一个元素
                                    pack.log.Add(pack.msg[0]);
                                    pack.msg.RemoveAt(0);
                                }
                            }
                            else
                            {
                                //数据不够，等待下次增加后继续
                                return;
                            }
                        }
                        else
                        {
                            //校验失败 删除第一个元素
                            pack.log.Add(pack.msg[0]);
                            pack.msg.RemoveAt(0);
                        }
                    }
                    else
                    {
                        //包头不对 删除第一个元素
                        pack.log.Add(pack.msg[0]);
                        pack.msg.RemoveAt(0);
                    }
                }
                else
                {
                    return;
                }
            }
        }

        //编码
        public static List<byte> pack_encode(List<byte> body)
        {
            UInt16 crc2_idx = (UInt16)(body.Count + 6);
            List<byte> msg = new List<byte>();
            //写入包头
            msg.Add(0xAA);
            msg.Add(0xAA);
            //写入包体长度
            write_uint16(ref msg, (UInt16)body.Count);
            //写入包头+包体长度 校验
            write_uint16(ref msg, CRC16(msg, 4));
            //写入包体
            for (UInt16 i = 0; i < body.Count; i++)
            {
                msg.Add(body[i]);
            }
            //写入包头+包体长度+包体 校验
            write_uint16(ref msg, CRC16(msg, crc2_idx));
            return msg;
        }
    }

    class pack_t
    {
        public List<List<byte>> bodys = new List<List<byte>>();//整包列表
        public List<byte> msg = new List<byte>();//剩余内容
        public List<byte> log = new List<byte>();//日志
    };

    class ble_ask_t
    {
        private byte[] Mac;
        private byte[] Ask_INFO;
        private List<byte[]> Ask_E_INK1 = new List<byte[]>();
        private List<byte[]> Ask_E_INK2 = new List<byte[]>();
        //private byte[] Ask_SET_PWD;
        //private byte[] Ask_OPEN_LOCK;
        //private byte[] Ask_SUPER_SET_PWD;
        private byte[] Ask_IF_OPEN_LOCK;
        private byte[] Ask_SUPER_OPEN_LOCK;
        private byte[] Ask_E_INK_CHANGE1;
        private byte[] Ask_E_INK_CHANGE2;

        public List<byte> buffer = new List<byte>();

        public void setDataOpenBox(TaskListModels cc)
        {
            buffer.Clear();
            byte[] mac = Public.strToHexByte(cc.mac);
            byte[] super_pwd = Encoding.UTF8.GetBytes(cc.super_pwd);

            Mac = mac;
            write_bytes(Mac);

            Ask_INFO = new byte[] { 0xAA, 0x11, 0x00, 0x00, 0xFF, 0xFF };
            Public.CRC16(ref Ask_INFO);
            write_bytes(Ask_INFO);

            Ask_SUPER_OPEN_LOCK = new byte[] { 0xAA, 0x14, 0x00, 0x08, super_pwd[0], super_pwd[1], super_pwd[2], super_pwd[3], super_pwd[4], super_pwd[5], super_pwd[6], super_pwd[7], 0xFF, 0xFF };
            Public.CRC16(ref Ask_SUPER_OPEN_LOCK);
            write_bytes(Ask_SUPER_OPEN_LOCK);
        }

        public void write_bytes(byte[] bytes)
        {
            ESP32BleTools.write_uint16(ref buffer, (UInt16)bytes.Length);
            buffer.AddRange(bytes);
        }
        public void write_bytes(List<byte[]> list_bytes)
        {
            ESP32BleTools.write_uint16(ref buffer, (UInt16)list_bytes.Count);
            foreach (byte[] bytes in list_bytes)
            {
                write_bytes(bytes);
            }
        }
    };
}

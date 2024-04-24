using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace YMModsApp.Common.Exceptions
{
    /// <summary>
    /// 登录错误异常
    /// </summary>
    class NetWorkException : Exception
    {
        public int code = 0;

        //public virtual string Code { get; }

        public NetWorkException()
        {
        }

        public NetWorkException(string message) : base(message)
        {
        }
        public NetWorkException(string message, int codes) : base(message)
        {
            code = codes;
        }

        public NetWorkException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected NetWorkException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}

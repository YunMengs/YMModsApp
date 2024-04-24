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
    class LoginException : Exception
    {
        public int code = 0;

        //public virtual string Code { get; }

        public LoginException()
        {
        }

        public LoginException(string message) : base(message)
        {
        }
        public LoginException(string message, int codes) : base(message)
        {
            code = codes;
        }

        public LoginException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected LoginException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}

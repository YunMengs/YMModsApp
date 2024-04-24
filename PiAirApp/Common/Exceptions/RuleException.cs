using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace YMModsApp.Common.Exceptions
{
    /// <summary>
    /// 权限错误异常
    /// </summary>
    class RuleException : Exception
    {
        public int code = 0;

        //public virtual string Code { get; }

        public RuleException()
        {
        }

        public RuleException(string message) : base(message)
        {
        }
        public RuleException(string message, int codes) : base(message)
        {
            code = codes;
        }

        public RuleException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected RuleException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}

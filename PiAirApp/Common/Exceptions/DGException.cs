using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace YMModsApp.Common.Exceptions
{
    /// <summary>
    /// 发货错误异常
    /// </summary>
    class DGException : Exception
    {
        public int code = 0;

        //public virtual string Code { get; }

        public DGException()
        {
        }

        public DGException(string message) : base(message)
        {
        }
        public DGException(string message, int codes) : base(message)
        {
            code = codes;
        }

        public DGException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected DGException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}

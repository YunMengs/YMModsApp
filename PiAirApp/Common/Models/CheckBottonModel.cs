using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YMModsApp.Common.Models
{
    /// <summary>
    /// 系统配置类
    /// </summary>
    public class CheckBottonModel : BindableBase
    {
        private string id;
        public string Id
        {
            get { return id; }
            set { SetProperty(ref id, value); }
        }

        private string content;
        /// <summary>
        /// 选项名称
        /// </summary>
        public string Content
        {
            get { return content; }
            set { content = value; }
        }

        private bool isCheck;
        /// <summary>
        /// 是否选中
        /// </summary>
        public bool IsCheck
        {
            get { return isCheck; }
            set { isCheck = value; }
        }

    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static YMModsApp.Common.Tool.ScanerHook;

namespace YMModsApp.Common
{
    public interface IScannerService
    {
        void ScanerRe(ScanerCodes codes);
    }
}

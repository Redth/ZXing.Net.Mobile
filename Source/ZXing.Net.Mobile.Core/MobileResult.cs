using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZXing.Mobile
{
    public class MobileResult
    {
        public ZXing.Result Result { get; private set; }
        public byte[] Jpg { get; private set; }

        public MobileResult(ZXing.Result ZXing_Result)
        {
            this.Result = ZXing_Result;
            this.Jpg = null;
        }

        public MobileResult(ZXing.Result ZXing_Result, byte[] jpg)
        {
            this.Result = ZXing_Result;
            this.Jpg = jpg;
        }
    }
}

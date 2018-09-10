using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZXing.Mobile
{
    public class ResultWithSource
    {
        public ZXing.Result ZXing_Result { get; private set; }
        public LuminanceSource luminanceSource { get; private set; }

        public ResultWithSource(ZXing.Result ZXing_Result, LuminanceSource luminanceSource)
        {
            this.ZXing_Result = ZXing_Result;
            this.luminanceSource = luminanceSource;
        }
    }
}

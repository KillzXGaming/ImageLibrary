using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ImageLibrary
{
    public static class SizeUtil
    {
        private const long SizeOfKb = 1024;
        private const long SizeOfMb = SizeOfKb * 1024;
        private const long SizeOfGb = SizeOfMb * 1024;
        private const long SizeOfTb = SizeOfGb * 1024;

        public static double ConvertBytesToMegabytes(long bytes)
        {
            return (bytes / SizeOfKb) / SizeOfKb;
        }

        static double ConvertKilobytesToMegabytes(long kilobytes)
        {
            return kilobytes / SizeOfKb;
        }

        public static string GetFileSize(this long value, int decimalPlaces = 0)
        {
            var asTb = Math.Round((double)value / SizeOfTb, decimalPlaces);
            var asGb = Math.Round((double)value / SizeOfGb, decimalPlaces);
            var asMb = Math.Round((double)value / SizeOfMb, decimalPlaces);
            var asKb = Math.Round((double)value / SizeOfKb, decimalPlaces);
            string chosenValue = asTb > 1 ? string.Format("{0} TB", asTb)
                : asGb > 1 ? string.Format("{0} GB", asGb)
                : asMb > 1 ? string.Format("{0} MB", asMb)
                : asKb > 1 ? string.Format("{0} KB", asKb)
                : string.Format("{0} bytes", Math.Round((double)value, decimalPlaces));
            return chosenValue;
        }
    }
}

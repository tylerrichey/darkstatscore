using System;

namespace DarkStatsCore
{
    public static class Helpers
    {
        public static string BytesToString(this long bytes)
        {
            string[] Suffix = { "B", "KB", "MB", "GB" };
            int i;
            double dblSByte = bytes;
            for (i = 0; i < Suffix.Length - 1 && bytes >= 1024; i++, bytes /= 1024)
            {
                dblSByte = bytes / 1024.0;
            }

            return String.Format("{0:0.##} {1}", dblSByte, Suffix[i]);
        }

        public static string BytesToBitsPsToString(this long bytes, TimeSpan ts)
        {
            double bits = bytes.BytesToBitsPs(ts);
            string[] Suffix = { "bps", "Kbps", "Mbps", "Gbps" };
            int i;
            double dblSBits = bits;
            for (i = 0; i < Suffix.Length - 1 && bits >= 1024; i++, bits /= 1024)
            {
                dblSBits = bits / 1024.0;
            }

            return String.Format("{0:0.##} {1}", dblSBits, Suffix[i]);
        }

        public static double BytesToBitsPs(this long bytes, TimeSpan ts)
        {
            return bytes * 8 / ts.TotalSeconds;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace WpfApp2
{
    public class RNNoise
    {
        [DllImport("rnnoise.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr rnnoise_create();

        [DllImport("rnnoise.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void rnnoise_destroy(IntPtr st);

        [DllImport("rnnoise.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern float rnnoise_process_frame(IntPtr st, float[] outFrame, float[] inFrame);
    }
}

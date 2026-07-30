using System.Runtime.InteropServices;

namespace Evergine.Bindings.Vuforia
{
    public static class Native
    {
#if __IOS__
        public const string Dll = "__Internal";
#else
        public const string Dll = "VuforiaEngine";
#endif
        public const CallingConvention Conv = CallingConvention.StdCall;
    }
}
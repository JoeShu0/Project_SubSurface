using System.Runtime.InteropServices;

public static class ReinterpretExtensions
{
    //make a struct to with int and float share the same data so we can to direct interpret
    [StructLayout(LayoutKind.Explicit)]
    struct IntFloat
    {
        [FieldOffset(0)]
        public int intValue;
        [FieldOffset(0)]
        public float floatValue;
    }
    public static float ReinterpretAsFloat(this int value)
    {
        IntFloat converter = default;
        converter.intValue = value;
        return converter.floatValue;
    }
}
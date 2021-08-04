using System;

namespace Utility {
    public class Misc {
        public static T[] BlockCopy<T>(in T[] data, in int offset = 0, in int length = 0) {
            int _length = (length <= 0) ? data.Length - offset : length;
            T[] result = new T[_length];
            int typeSize = System.Runtime.InteropServices.Marshal.SizeOf(
                data.GetType().GetElementType());
            Buffer.BlockCopy(data, offset, result, 0, (_length) * typeSize);
            return result;
        }

        public static T[] BlockCopy<T>(ref T[] dest, in T[] data, in int offset = 0, in int length = 0) {
            int _length = (length > 0)? length : dest.Length;
            if(_length + offset > data.Length) {
                _length = data.Length - offset;
            }
            int typeSize = System.Runtime.InteropServices.Marshal.SizeOf(
                data.GetType().GetElementType());
            Buffer.BlockCopy(data, offset, dest, 0, (_length) * typeSize);
            return dest;
        }
    }
}

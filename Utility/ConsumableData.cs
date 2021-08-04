using System;
using System.Text;

namespace Utility
{
	public class ConsumableData
	{
		private byte[] data;
		private int offset;
		public ConsumableData(byte[] data)
		{
			this.data = data;
			this.offset = 0;
		}

		public ConsumableData(string data)
		{
			this.data = System.Text.Encoding.UTF8.GetBytes(data);
			this.offset = 0;
		}

        public static ConsumableData operator +(in ConsumableData a, in ConsumableData b)
        {
			byte[] arrayA = a.SubArray();
			byte[] arrayB = b.SubArray();
			byte[] c = new byte[arrayA.Length + arrayB.Length];
			int typeSize = System.Runtime.InteropServices.Marshal.SizeOf(
			    arrayA.GetType().GetElementType());
			Buffer.BlockCopy(arrayA, 0, c, 0, arrayA.Length * typeSize);
			Buffer.BlockCopy(arrayB, 0, c, arrayA.Length, arrayB.Length);
			return new(c);
		}

	public int Size
	{
	    get
	    {
		return data.Length;
	    }
	}

	public UInt32 U32
	{
	    get
	    {
		return (UInt32)DataLengthU32;
	    }
	}

	public byte U8
	{
	    get
	    {
            byte result = data[offset];
            Consume(1);
            return result;
	    }
	}

	private int DataLengthU32
	{
	    get
	    {
		int length = (int)ByteConverter.convertToU32(in data, Endian.BIG, offset);
		Consume(4);
		return length;
	    }
	}

	public string readString(int length)
	{
	    Encoding enc = Encoding.GetEncoding("UTF-8");	    
	    string result = enc.GetString(SubArray(length));
	    Consume(length);
	    return result;
	}

	public byte[] readByte(int length)
	{
	    byte[] result = SubArray(length);
	    Consume(length);
	    return result;
	}

	public string StrData
	{
	    get
	    {
		int length = DataLengthU32;
		return readString(length);
	    }
	}

	public void dump() {
	    for(int i = offset; i < Size; ++i) {
		Console.Write(string.Format("{0,2:X2}", data[i]) + " ");
	    }
	    Console.WriteLine();
	}

	public byte[] trimmedRawData
        {
	    get
            {
		byte[] rawArray = rawData;
		int length = rawArray.Length;
		int ptr = 0;
		while(length > 0 && rawArray[ptr] == 0x0)
                {
		    ptr++;
		    length--;
                }
		return Misc.BlockCopy(rawArray, ptr, length);
	    }
        }

	public byte[] rawData
	{
	    get
	    {
		int length = DataLengthU32;
		byte[] result = SubArray(length);
		Consume(length);
		return result;
	    }
	}

	public int Remain
	{
	    get
	    {
		return data.Length - offset;
	    }

	}

	public void Consume(int num)
	{
	    this.offset += num;
	    if (this.offset > data.Length)
	    {
		this.offset = data.Length;
	    }
	    return;
	}

	public byte this[int idx]
	{
	    get
	    {
		int trueIdx = offset + idx;
		if (trueIdx >= this.data.Length)
		{
		    trueIdx = this.data.Length - 1;
		}
		return this.data[trueIdx];
	    }
	}

	public byte[] Remains
	{
	    get {
		return SubArray(this.data.Length);
	    }
	}

	public byte[] SubArray(int num = -1)
	{
	    int tail = offset + num;
	    if (num < 0 || tail > this.data.Length)
	    {
		tail = this.data.Length;
	    }
	    byte[] result = new byte[tail - offset];
	    int typeSize = System.Runtime.InteropServices.Marshal.SizeOf(
		data.GetType().GetElementType());
	    Buffer.BlockCopy(data, offset, result, 0, result.Length * typeSize);
	    return result;
	}


    }

}

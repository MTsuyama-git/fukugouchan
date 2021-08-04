using System;

namespace Utility
{
	public enum Endian
	{
		BIG,
		LITTLE
	}

	public class ByteConverter
	{
		public static byte[] trim(in byte[] data)
		{
			int ptr = 0;
			int length = data.Length;
			while (data[ptr] == 0x0 && length > 0)
			{
				ptr++;
				length--;
			}
			return Misc.BlockCopy(in data, in ptr, in length);
		}

		public static byte[] ParseStrAsByteArray(in string data)
		{
			byte[] result = new byte[data.Length / 2 + ((data.Length % 2 == 1) ? 1 : 0)];
			int idx = 0;
			foreach (char c in data)
			{
				byte val;
				if ('a' <= c && c <= 'f')
				{
					val = (byte)(c - 'a' + 10);
				}
				else if ('A' <= c && c <= 'F')
				{
					val = (byte)(c - 'A' + 10);
				}
				else if ('0' <= c && c <= '9')
				{
					val = (byte)(c - '0');
				}
				else
				{
					continue;
				}
				if (idx % 2 == 0)
				{
					result[idx / 2] = (byte)(val << 4);
				}
				else
				{
					result[idx / 2] |= (byte)(val & 0xFF);
				}
				idx++;

			}
			return result;
		}


		public static byte[] Str2ByteArray(in string data)
		{
			return System.Text.Encoding.UTF8.GetBytes(data);
		}

		public static byte[] convertToByte(in UInt16 num, in Endian endian = Endian.BIG)
		{
			byte[] result =
			(endian == Endian.BIG) ? new byte[] { (byte)((num >> 8) & 0xFF), (byte)(num & 0xFF) }
			: new byte[] { (byte)(num & 0xFF), (byte)((num >> 8) & 0xFF) };
			return result;
		}


		public static UInt16 convertToU16(in byte[] data, in Endian endian = Endian.BIG, in int offset = 0)
		{
			UInt16 result = (
			(endian == Endian.BIG)
			? (UInt16)((data[offset + 0] << 8) | data[offset + 1])
			: (UInt16)((data[offset + 1] << 8) | data[offset + 0])
			);
			return result;
		}

		public static UInt32 convertToU32(in byte[] data, in Endian endian = Endian.BIG, in int offset = 0)
		{
			UInt32 result = (
			(endian == Endian.BIG)
			? (UInt32)((data[offset + 0] << 24) | (data[offset + 1] << 16) | (data[offset + 2] << 8) | data[offset + 3])
			: (UInt32)((data[offset + 3] << 24) | (data[offset + 2] << 16) | (data[offset + 1] << 8) | data[offset + 0])
			);
			return result;
		}

		public static UInt32 convertToU32_rijndael(in byte[] data, in Endian endian = Endian.BIG, in int offset = 0)
		{
			UInt32 result = (
			(endian == Endian.BIG)
			? (UInt32)((data[offset + 0] << 24) ^ (data[offset + 1] << 16) ^ (data[offset + 2] << 8) ^ (data[offset + 3]))
			: (UInt32)((data[offset + 3] << 24) ^ (data[offset + 2] << 16) ^ (data[offset + 1] << 8) ^ (data[offset + 0]))
			);
			return result;
		}

		public static void putU32(ref byte[] dest, in UInt32 data, in Endian endian = Endian.BIG, in int offset = 0)
		{
			if (endian == Endian.BIG)
			{
				dest[offset + 0] = (byte)(data >> 24);
				dest[offset + 1] = (byte)(data >> 16);
				dest[offset + 2] = (byte)(data >> 8);
				dest[offset + 3] = (byte)data;
			}
			else
			{
				dest[offset + 3] = (byte)(data >> 24);
				dest[offset + 2] = (byte)(data >> 16);
				dest[offset + 1] = (byte)(data >> 8);
				dest[offset + 0] = (byte)data;
			}
		}
	}
};

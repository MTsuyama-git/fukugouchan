using System;
using System.Security.Cryptography;
using System.Text;

namespace Utility
{
    public class Bcrypt
    {
        static readonly int SHA512_DIGEST_LENGTH = 64;
        static readonly int BCRYPT_WORDS = 8;
        static readonly int BCRYPT_HASHSIZE = BCRYPT_WORDS * 4;

        static void dump<T>(T[] arr) {
            foreach(T b in arr) {
		Console.Write(string.Format("{0,2:X2}", b) + " ");
	    }
	    Console.WriteLine();
	}

        static byte[] hash(ref byte[] pass, ref byte[] salt)
        {
            Blowfish state = new ();
            string ciphertext = "OxychromaticBlowfishSwatDynamite";
            UInt32[] cdata = new UInt32[BCRYPT_WORDS];
            byte[] result = new byte[BCRYPT_HASHSIZE];
            UInt16 j;
            int shalen = SHA512_DIGEST_LENGTH;

            state.expandstate(pass, salt);
            for(int i = 0; i < 64; ++i) {
                state.expandstate(salt);
                state.expandstate(pass);
            }
            
            j = 0;

            for(int i = 0; i < BCRYPT_WORDS; ++i) 
                cdata[i] = Blowfish.stream2word(ciphertext, ref j);
            int sizeofCdata = System.Runtime.InteropServices.Marshal.SizeOf(
                cdata.GetType().GetElementType())*cdata.Length;
            for(int i = 0; i < 64; ++i)
                state.enc(cdata, (UInt16)(sizeofCdata/sizeof(UInt64)));
            for(int i = 0; i < BCRYPT_WORDS; ++i) {
                result[4 * i + 3] = (byte)((cdata[i] >> 24) & 0xFF);
                result[4 * i + 2] = (byte)((cdata[i] >> 16) & 0xFF);
                result[4 * i + 1] = (byte)((cdata[i] >> 8) & 0xFF);
                result[4 * i + 0] = (byte)((cdata[i] >> 0) & 0xFF);
            }
            
            return result;
        }

        public static int pbkdf(string password, byte[] salt, ref byte[] key, int rounds)
        {
            byte[]  sha2salt  = new byte[SHA512_DIGEST_LENGTH];
            byte[]  output    = new byte[BCRYPT_HASHSIZE];
            byte[]  tmpoutput = new byte[BCRYPT_HASHSIZE];
            byte[]  countsalt = new byte[salt.Length + 4];
            int amt, stride, i;

            Array.Fill<byte>(countsalt, 1);

            if(rounds < 1)
                throw new Exception("Rounds must be greater than 0.");
            if(password.Length == 0 || salt.Length == 0 || key.Length == 0 || key.Length > Math.Pow(output.Length, 2.0) || salt.Length > (1 << 20))
                throw new Exception("Invalid parameters.");
            stride = (key.Length + output.Length - 1) / output.Length;
            amt = (key.Length + stride - 1) / stride;
            int typeSize = System.Runtime.InteropServices.Marshal.SizeOf(salt.GetType().GetElementType());
            Buffer.BlockCopy(salt, 0, countsalt, 0, typeSize * salt.Length);
            
	    SHA512 shaM = new SHA512Managed();
            byte[]  sha2pass  = shaM.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password));
	    int keylen = key.Length;
	    for (int count = 1; keylen > 0; count++) {
		countsalt[salt.Length + 0] = (byte)((count >> 24) & 0xff);
		countsalt[salt.Length + 1] = (byte)((count >> 16) & 0xff);
		countsalt[salt.Length + 2] = (byte)((count >> 8) & 0xff);
		countsalt[salt.Length + 3] = (byte)(count & 0xff);
		sha2salt = shaM.ComputeHash(countsalt);
		tmpoutput = hash(ref sha2pass, ref sha2salt);
		Buffer.BlockCopy(tmpoutput, 0, output, 0, System.Runtime.InteropServices.Marshal.SizeOf(output.GetType().GetElementType()) * output.Length);
		for(i = 1; i < rounds; ++i) {
		    sha2salt = shaM.ComputeHash(tmpoutput);
		    tmpoutput = hash(ref sha2pass, ref sha2salt);
		    for (int j = 0; j < output.Length; j++)
			output[j] ^= tmpoutput[j];
		}
		
		amt = Math.Min(amt, keylen);
		for(i = 0; i < amt; ++i) {
		    int dest = (i * stride) + (count - 1);
		    if(dest >= key.Length)
			break;
		    key[dest] = output[i];
		}
		keylen -= i;
	    }
	    return 0;
        }
    }
}

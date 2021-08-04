using System;
using System.Security.Cryptography;
using System.Text;

namespace Utility
{
    public class PassphraseGenerator {
	public static byte[] Generate(in int length) {
	    byte[] dest = new byte[length];
	    RandomNumberGenerator rng = RandomNumberGenerator.Create();
	    rng.GetBytes(dest, 0, length);
	    return dest;
	}
    }
}

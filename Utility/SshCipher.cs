using System;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace Utility {
    
    public class SshCipher {
        public readonly string name;
        public readonly int blockSize;
        public readonly int keyLen;
        private readonly int _ivLen;
        public readonly int authLen;
        public readonly int cipherMode;

	private const int NOCIPHER_NUM = 0;
	private const int CHACHA_NUM = 0;
	private const int CTR_NUM = 7;

	public int SecLen {
	    get {
		return (name == "3des-cbc") ? 14 : keyLen;
	    }
	}

	public int ivLen {
	    get {
		return (_ivLen != 0 || cipherMode == 6) ? _ivLen : blockSize;
	    }
	}

	public bool isCBC {
	    get {
		return (cipherMode == (int)CipherMode.CBC);
	    }
	}

	public bool isCTR {
	    get {
		return (cipherMode == CTR_NUM);
	    }
	}

	public bool isCHACHA20Poly {
	    get {
		return (cipherMode == CHACHA_NUM);
	    }
	}

	public bool isNoCipher {
	    get {
		return (cipherMode == NOCIPHER_NUM);
	    }
	}
       

        // 0: none, 6: CHACHA20-poly, 7: CTR, 1-5 CipherMode
        public static readonly Dictionary<string, SshCipher> ciphers = new () {
            {"3des-cbc", new SshCipher("3des-cbc", 8, 24, 0, 0, (int)CipherMode.CBC)},
            {"aes128-cbc", new SshCipher("aes128-cbc", 16, 16, 0, 0, (int)CipherMode.CBC)},
            {"aes192-cbc", new SshCipher("aes192-cbc", 16, 24, 0, 0, (int)CipherMode.CBC)},
            {"aes256-cbc", new SshCipher("aes256-cbc", 16, 32, 0, 0, (int)CipherMode.CBC)},
            {"aes128-ctr", new SshCipher("aes128-ctr", 16, 16, 0, 0, CTR_NUM)},
            {"aes192-ctr", new SshCipher("aes192-ctr", 16, 24, 0, 0, CTR_NUM)},
            {"aes256-ctr", new SshCipher("aes256-ctr", 16, 32, 0, 0, CTR_NUM)},
            {"aes128-gcm@openssh.com", new SshCipher("aes128-gcm@openssh.com", 16, 16, 12, 16, NOCIPHER_NUM)},
            {"aes256-gcm@openssh.com", new SshCipher("aes256-gcm@openssh.com", 16, 32, 12, 16, NOCIPHER_NUM)},
            {"chacha20-poly1305@openssh.com", new SshCipher("chacha20-poly1305@openssh.com", 8, 64, 0, 16, CHACHA_NUM)},
            {"none", new SshCipher("none", 8, 0, 0, 0, 0)},
        };



        private SshCipher(string name, int blockSize, int keyLen, int ivLen, int authLen, int cipherMode)
        {
            this.name = name;
            this.blockSize = blockSize;
            this.keyLen = keyLen;
            this._ivLen = ivLen;
            this.authLen = authLen;
            this.cipherMode = cipherMode;
        }

        public CipherMode cipherModeEnum  {
            get {
                return (CipherMode)Enum.ToObject(typeof(CipherMode), cipherMode);
            }
        }

    }
}

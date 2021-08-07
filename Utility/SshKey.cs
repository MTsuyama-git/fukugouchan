using System;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Numerics;

namespace Utility
{
    public class SSHKeyManager
    {
        private static readonly char[] del = { '\t', ' ' };
        private static readonly string RsaPrivateKeyHeader = @"-----BEGIN RSA PRIVATE KEY-----";
        private static readonly string RsaPrivateKeyFooter = @"-----END RSA PRIVATE KEY-----";
        private static readonly string OpenSSHPrivateKeyHeader = @"-----BEGIN OPENSSH PRIVATE KEY-----";
        private static readonly string OpenSSHPrivateKeyFooter = @"-----END OPENSSH PRIVATE KEY-----";

        public static RSACryptoServiceProvider ReadSSHPublicKeyFromContent(string contents)
        {

            string[] lines = contents.Split("\n");
            return ReadSSHPublieKeyBody(lines);
        }

        private static RSACryptoServiceProvider ReadSSHPublieKeyBody(string[] lines)
        {
            RSACryptoServiceProvider rsa = null;
            foreach (string line in lines)
            {
                string c = line.Substring(0, 1);
                if (c == "#" || c == "\n" || c == "\0") // separator or comment
                {
                    continue;
                }

                _ = CheckKeyFmt(line);
                var items = line.Split(del, StringSplitOptions.RemoveEmptyEntries);
                // body
                if (items.Length >= 3)
                {
                    try
                    {
                        rsa = new();
                        RSAParameters rsaParams = ParseSSHPublicKey(line);
                        rsa.ImportParameters(rsaParams);
                    }
                    catch (Exception e)
                    {
                        throw new Exception(e.Message);
                    }
                    if (rsa != null)
                    {
                        return rsa;
                    }
                }
            }
            return null;
        }
        public static RSACryptoServiceProvider ReadSSHPublicKey(string keyPath)
        {
            RSACryptoServiceProvider rsa = null;
            string line;
            using (StreamReader sr = new(keyPath))
            {
                while ((line = sr.ReadLine()) != null)
                {
                    var c = line.Substring(0, 1);
                    if (c == "#" || c == "\n" || c == "\0") // separator or comment
                        continue;
                    CheckKeyFmt(line);
                    var items = line.Split(del, StringSplitOptions.RemoveEmptyEntries);
                    // body
                    if (items.Length >= 3)
                    {
                        try
                        {
                            rsa = new();
                            RSAParameters rsaParams = ParseSSHPublicKey(line);
                            rsa.ImportParameters(rsaParams);
                        }
                        catch (Exception e)
                        {
                            throw new Exception(e.Message);
                        }
                        if (rsa != null)
                        {
                            return rsa;
                        }
                    }
                }
            }
            return null;
        }

        private static RSAParameters ParseSSHPublicKey(string contents)
        {
            var items = contents.Split(del, StringSplitOptions.RemoveEmptyEntries);
            ConsumableData data = new(Convert.FromBase64String(items[1]));
            string name = data.StrData;
            if (name != items[0])
            {
                throw new Exception("Invalid Key Type");
            }
            byte[] rsa_e = data.trimmedRawData; //exponent
            byte[] rsa_n = data.trimmedRawData; // modulus

            return new RSAParameters
            {
                Exponent = rsa_e,
                Modulus = rsa_n
            };
        }

        private static bool CheckKeyFmt(string line)
        {
            if (line.Substring(0, 10) == "-----BEGIN" || String.Compare(line, "SSH PRIVATE KEY FILE") == 0)
            {
                throw new Exception("Invalid format error");
            }
            return true;
        }

        public static RSA ReadSSHPrivateKey(string keyPath, Func<string> passwordCb = null, bool verbose = false)
        {
            RSA rsa = RSA.Create();

            string contents = System.IO.File.ReadAllText(keyPath);
            if (contents.Substring(0, RsaPrivateKeyHeader.Length) == RsaPrivateKeyHeader)
            {
                rsa.ImportRSAPrivateKey(ReadRSAPrivateKey(contents, passwordCb, verbose), out _);
            }
            else if (contents.Substring(0, OpenSSHPrivateKeyHeader.Length) == OpenSSHPrivateKeyHeader)
            {
                rsa.ImportParameters(ReadOpenSSHPrivateKey(contents, passwordCb, verbose));
            }
            else
            {
                throw new Exception("Invalid of unspported SSH key type");
            }
            return rsa;
        }

        public static ReadOnlySpan<byte> ReadRSAPrivateKey(string contents, Func<string> passwordCb = null, bool verbose = false)
        {
            contents = contents.Replace("\r", String.Empty);
            string[] contentlines = contents.Split("\n", StringSplitOptions.RemoveEmptyEntries);
            contents = String.Empty;
            List<string> headers = new();
            foreach (string line in contentlines)
            {
                if (line.Contains(':'))
                {
                    headers.Add(line);
                    continue;
                }
                else if (line.Length == 0)
                {
                    continue;
                }
                contents += line;
            }
            contents = contents.Replace(RsaPrivateKeyHeader, newValue: string.Empty).Replace(RsaPrivateKeyFooter, newValue: string.Empty).Replace("\n", newValue: string.Empty);
            byte[] encrypted_data = Convert.FromBase64String(contents);
            string[] encryptInfo = new string[1];
            int ivlen;
            byte[] iv = null;
            foreach (string header in headers)
            {
                string[] h = header.Split(":", StringSplitOptions.RemoveEmptyEntries);
                if (h[0] == "DEK-Info")
                {
                    string empty = string.Empty;
                    encryptInfo = h[1].Split(",", options: StringSplitOptions.RemoveEmptyEntries)[0].Replace(" ", newValue: empty).Split('-', options: StringSplitOptions.RemoveEmptyEntries);
                    string ivInfo = h[1].Split(",", options: StringSplitOptions.RemoveEmptyEntries)[1].Replace(" ", newValue: empty);
                    iv = ByteConverter.ParseStrAsByteArray(ivInfo);
                    //cipher = SshCipher.ciphers[encryptInfo];
                }
            }
            if (encryptInfo[0] != "AES")
            {
                Console.WriteLine("Unsupported Type:", encryptInfo[0]);
                throw new Exception("Unspported Encryption");
            }
            ivlen = Int32.Parse(encryptInfo[1]);
            CipherMode cipherMode = CipherModeDic[encryptInfo[2]];
            using MD5 md5 = MD5.Create();
            md5.Initialize();
            string password = "";
            if (passwordCb != null)
            {
                password = passwordCb();
            }
            ConsumableData rawData = new ConsumableData(ByteConverter.Str2ByteArray(password)) + new ConsumableData(Misc.BlockCopy(iv, 0, 8));
            byte[] result = md5.ComputeHash(rawData.SubArray());
            byte[] decrypted = null;
            using (AesManaged aes = new())
            {
                aes.KeySize = ivlen;
                aes.BlockSize = ivlen;
                aes.IV = iv;
                aes.Key = result;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;
                using (var decryptor = aes.CreateDecryptor())
                using (var mstream1 = new MemoryStream(encrypted_data))
                using (var cstream = new CryptoStream(mstream1, decryptor, CryptoStreamMode.Read))
                using (var mstream2 = new MemoryStream())
                {
                    cstream.CopyTo(mstream2);
                    decrypted = mstream2.ToArray();
                }
            }
            return new ReadOnlySpan<byte>(decrypted);
        }

        private static readonly Dictionary<string, CipherMode> CipherModeDic = new()
        {
            { "CBC", CipherMode.CBC },
            { "CFB", CipherMode.CFB },
            { "CTS", CipherMode.CTS },
            { "ECB", CipherMode.ECB },
            { "OFB", CipherMode.OFB },
        };


        public static RSAParameters ReadOpenSSHPrivateKey(string contents, Func<string> passwordCb = null, bool verbose = false)
        {
            contents = contents.Replace(OpenSSHPrivateKeyHeader, String.Empty).Replace(OpenSSHPrivateKeyFooter, String.Empty).Replace("\r", String.Empty).Replace("\n", String.Empty);
            ConsumableData data = new(Convert.FromBase64String(contents));
            string magic = data.readString(14);
            data.Consume(1); // null terminator
            string cipher_name = data.StrData;
            string kdf_name = data.StrData;
            ConsumableData kdf = new(data.rawData);
            uint nkeys = data.U32;
            ConsumableData pubkey = new(data.rawData);
            uint encryptedLen = data.U32;
            byte[] key;


            byte[] rsa_e = pubkey.trimmedRawData;
            byte[] rsa_n = pubkey.trimmedRawData;
            SshCipher cipher = SshCipher.ciphers[cipher_name];
            int keyLen = cipher.keyLen;
            int ivLen = cipher.ivLen;
            int authLen = cipher.authLen;
            int blockSize = cipher.blockSize;

            if (verbose)
            {
                Console.WriteLine("magic:{0}", magic);
                Console.WriteLine("cipherName:{0}", cipher_name);
                Console.WriteLine("kdfName:{0}", kdf_name);
                Console.WriteLine("kdf:" + kdf.Size);
                Console.WriteLine("nkeys:" + nkeys);
                Console.WriteLine("encryptedLen:" + encryptedLen);
                Console.WriteLine("pubKey:" + pubkey.Size);
                Console.WriteLine("pubKeyType:" + pubkey.StrData);
                Console.Write("pubkey:");
                pubkey.dump();
                Console.WriteLine("keyLen:" + keyLen);
                Console.WriteLine("ivLen:" + ivLen);
                Console.WriteLine("authLen:" + authLen);
                Console.WriteLine("blockSize:" + blockSize);
            }

            if (encryptedLen < blockSize || (encryptedLen % blockSize) != 0)
            {
                throw new Exception("Invalid Key Format");
            }

            key = new byte[keyLen + ivLen];
            Array.Fill<byte>(key, 1);

            if (kdf_name == "bcrypt")
            {
                byte[] salt = kdf.rawData;
                uint round = kdf.U32;
                string passphrase = "";
                if (passwordCb != null)
                {
                    passphrase = passwordCb();
                }
                if (Bcrypt.pbkdf(passphrase, salt, ref key, (int)round) < 0)
                {
                    throw new Exception("Invalid format@pbkdf");
                }
                else if (verbose)
                {
                    Console.Write("salt:"); new ConsumableData(salt).dump();
                    ConsumableData cdkey = new(key);
                    Console.Write("key:");
                    cdkey.dump();
                }
            }

            if (data.Remain < authLen || data.Remain - authLen < encryptedLen)
            {
                throw new Exception("INVALID format@RemainCheck");
            }
            byte[] keyBody = Misc.BlockCopy(key, 0, keyLen);
            byte[] ivBody = Misc.BlockCopy(key, keyLen, ivLen);

            SshCipherCtx cipherCtx = new(cipher, keyBody, ivBody, false);
            ConsumableData decrypted = new(cipherCtx.Crypt(0, data.Remains, (int)encryptedLen, 0, authLen));
            data.Consume((int)(encryptedLen + authLen));
            if (data.Remain != 0)
            {
                throw new Exception("INVALID FORMAT of data");
            }

            uint check1 = decrypted.U32;
            uint check2 = decrypted.U32;
            if (check1 != check2)
            {
                throw new Exception("Wrong Pass pharase");
            }
            return DeserializeKey(decrypted, verbose);
        }

        private static RSAParameters DeserializeKey(ConsumableData decrypted, bool verbose = false)
        {
            string tname = decrypted.StrData;
            ConsumableData rsa_n = new(decrypted.trimmedRawData);
            ConsumableData rsa_e = new(decrypted.trimmedRawData);
            ConsumableData rsa_d = new(decrypted.rawData);
            ConsumableData rsa_iqmp = new(decrypted.trimmedRawData);
            ConsumableData rsa_p = new(decrypted.rawData);
            ConsumableData rsa_q = new(decrypted.rawData);
            string comment = decrypted.StrData;
            CheckPadding(decrypted);
            var d_data = rsa_d.SubArray();
            Array.Reverse(d_data);
            var p_data = rsa_p.SubArray();
            Array.Reverse(p_data);
            var q_data = rsa_q.SubArray();
            Array.Reverse(q_data);

            BigInteger brsa_p = new(p_data);
            BigInteger brsa_q = new(q_data);
            BigInteger brsa_d = new(d_data);

            brsa_p -= 1;
            brsa_q -= 1;

            BigInteger brsa_dmp1 = brsa_d % brsa_p;
            BigInteger brsa_dmq1 = brsa_d % brsa_q;

            var dmp1b = brsa_dmp1.ToByteArray();
            var dmq1b = brsa_dmq1.ToByteArray();

            Array.Reverse(dmp1b);
            Array.Reverse(dmq1b);


            return new RSAParameters
            {
                Exponent = rsa_e.SubArray(),
                Modulus = rsa_n.SubArray(),
                D = ByteConverter.trim(rsa_d.SubArray()),
                P = ByteConverter.trim(rsa_p.SubArray()),
                Q = ByteConverter.trim(rsa_q.SubArray()),
                InverseQ = rsa_iqmp.SubArray(),
                DP = ByteConverter.trim(dmp1b),
                DQ = ByteConverter.trim(dmq1b),
            };

        }

        private static void CheckPadding(ConsumableData decrypted)
        {
            byte pad;
            UInt64 i;
            i = 0;
            while (decrypted.Remain > 0)
            {
                pad = decrypted.U8;
                if (pad != (++i & 0xff))
                {
                    throw new Exception("Invalid padding");
                }
            }
            /* success */
        }

    }

}

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Security.Cryptography;
using Utility;
using Microsoft.Win32;
using System.Diagnostics;
using System.Threading;

namespace fukugouchan
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            string userprofile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string keyPath = System.IO.Path.Combine(userprofile, ".ssh", "id_rsa");
            if(File.Exists(keyPath))
            {
                KeyFile.Text = keyPath;
            }
        }

        public void OpenIdentityDialog(object sender, RoutedEventArgs e)
        {
            OpenFileDialog od = new()
            {
                Filter = "All Files (*.*)|*.*",
                FileName = "", // Default file name
                DefaultExt = "" // Default file extension
            };
            if(od.ShowDialog() == true)
            {
                KeyFile.Text = od.FileName;
            }
        }

        public void OpenFileDialog(object sender, RoutedEventArgs e)
        {
            OpenFileDialog od = new()
            {
                Filter = "All Files (*.*)|*.*",
                FileName = "", // Default file name
                DefaultExt = "" // Default file extension
            };
            if (od.ShowDialog() == true)
            {
                Eintity.Text = od.FileName;
            }
        }

        public void ProceedClear(object sender, RoutedEventArgs e)
        {
            Eintity.Text = string.Empty;

        }

        public string passwordCb()
        {
            string ret = null;
            Application.Current.Dispatcher.Invoke(() =>
            {
                PassphraseInput pi = new();

                if (pi.ShowDialog() == true)
                {
                    ret = pi.Text;
                }
                else
                {
                    throw new Exception("Canceled");
                }
            });
            return ret;
        }

        struct ThreadArgs
        {
            public string KeyFile;
            public string Eintity;
            public string Dest;

            public ThreadArgs(string KeyFile, string Eintity, string Dest)
            {
                this.KeyFile = KeyFile;
                this.Eintity = Eintity;
                this.Dest = Dest;
            }
        }

        private void ProceedDecryptCore(Object Obj)
        {
            ThreadArgs args = (ThreadArgs)Obj;
            try
            {
                RSA rsa = SSHKeyManager.ReadSSHPrivateKey(args.KeyFile, passwordCb);
                using FileStream inputStream = File.Open(args.Eintity, FileMode.Open);
                using FileStream outputStream = File.Open(args.Dest, FileMode.Create);
                long inputSize = inputStream.Length;
                long totalRead = 0;
                Stopwatch sw = new();
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Progress0.Minimum = totalRead;
                    Progress0.Maximum = inputSize;
                });
                sw.Start();
                using BinaryReader br = new(inputStream);
                Span<byte> readBuffer = new(new byte[8192]);
                byte[] headerSizeB = new byte[2];
                br.Read(headerSizeB, 0, headerSizeB.Length);
                UInt16 headerSize = ByteConverter.convertToU16(headerSizeB, Endian.LITTLE);
                Console.WriteLine("headerSize:{0}", headerSize);
                byte[] cipherBytes = new byte[headerSize];
                _ = br.Read(cipherBytes, 0, cipherBytes.Length);
                byte[] decrypted = rsa.Decrypt(cipherBytes, RSAEncryptionPadding.Pkcs1);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Progress0.Value = inputStream.Position;
                });
                byte[] magic = new byte[8];
                byte[] salt = new byte[8];
                _ = br.Read(magic, 0, magic.Length);
                _ = br.Read(salt, 0, salt.Length);
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Progress0.Value = inputStream.Position;
                });
                Rfc2898DeriveBytes b = new(decrypted, salt, 10000, HashAlgorithmName.SHA256);
                byte[] keyIv = b.GetBytes(48);
                byte[] key = Misc.BlockCopy(keyIv, 0, 32);
                byte[] iv = Misc.BlockCopy(keyIv, 32, 16);
                Aes encAlg = Aes.Create();
                encAlg.Key = key;
                encAlg.IV = iv;
                int readLen = 0;
                using CryptoStream decrypt = new(outputStream, encAlg.CreateDecryptor(), CryptoStreamMode.Write);
                while ((readLen = inputStream.Read(readBuffer)) > 0)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        Progress0.Value = inputStream.Position;
                    });
                    decrypt.Write(readBuffer.ToArray(), 0, readLen);
                }
                sw.Stop();
                Application.Current.Dispatcher.Invoke(() =>
                {
                    System.Media.SystemSounds.Asterisk.Play();
                    Progress0.Value = inputStream.Length;
                    _ = MessageBox.Show("End Elapsed: " + sw.ElapsedMilliseconds + " ms.");
                });
                decrypt.FlushFinalBlock();
                decrypt.Close();
            }
            catch (CryptographicException ce)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    _ = MessageBox.Show(ce.Message);
                });
            }
            catch (Exception e)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    System.Media.SystemSounds.Hand.Play();
                    _ = MessageBox.Show(e.Message);
                });
              
            }
        }

        public void ProceedDecrypt(object sender, RoutedEventArgs e)
        {
            if (Eintity.Text == "")
            {
                _ = MessageBox.Show("Please set file path to decrypt");
                return;
            }
            if (KeyFile.Text == "")
            {
                _ = MessageBox.Show("Please set key file");
                return;
            }
            SaveFileDialog dialog = new()
            {
                Filter = "All Files (*.*)|*.*",
                FileName = "", // Default file name
                DefaultExt = "" // Default file extension
            };
            if (dialog.ShowDialog() == true)
            {
                Thread th = new(ProceedDecryptCore);
                th.Start(new ThreadArgs(KeyFile.Text, Eintity.Text, dialog.FileName));
            }
        }
    }
}

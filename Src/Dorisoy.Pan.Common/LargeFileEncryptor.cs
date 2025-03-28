﻿
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using static System.Net.WebRequestMethods;


namespace Dorisoy.Pan.Common
{
    public class LargeFileEncryptor
    {
        private static readonly byte[] SALT = [0x26, 0xdc, 0xff, 0x00, 0xad, 0xed, 0x7a, 0xee, 0xc5, 0xfe, 0x07, 0xaf, 0x4d, 0x08, 0x22, 0x3c];

        // 加密（AES-CBC-PKCS7）
        public static void EncryptFile(string tempPath, string outputFile, string password)
        {
            using (var aes = Aes.Create())
            {
                var pdb = new Rfc2898DeriveBytes(password, SALT, 10000, HashAlgorithmName.SHA256);
                aes.Key = pdb.GetBytes(32);
                aes.IV = pdb.GetBytes(16);
           

                // 写入盐和IV到输出文件头部
                using (var outputStream = new FileStream(outputFile, FileMode.Create,FileAccess.Write))
                {
                    // 创建加密流
                    using (var cryptoStream = new CryptoStream(
                        outputStream,
                        aes.CreateEncryptor(),
                        CryptoStreamMode.Write))
                    {
                        var files = new DirectoryInfo(tempPath).GetFiles().OrderBy(p => p.Name);
                        foreach (var item in files)
                        {
                            using (var inputStream = new FileStream(item.FullName,FileMode.Open))
                            {
                                byte[] buffer = new byte[1048576]; // 1MB 缓冲区
                                int bytesRead;
                                while ((bytesRead = inputStream.Read(buffer, 0, buffer.Length)) > 0)
                                {
                                    cryptoStream.Write(buffer, 0, bytesRead);
                                }
                            }
                        }
                    }
                }
            }
        }

        // 解密
        public static void DecryptFile(string inputFile,string password, Action<byte[]> handler)
        {
            using (var aes = Aes.Create())
            {
                var pdb = new Rfc2898DeriveBytes(password, SALT, 10000, HashAlgorithmName.SHA256);
                aes.Key = pdb.GetBytes(32);
                aes.IV = pdb.GetBytes(16);

                // 从加密文件头部读取盐和IV
                using (var inputStream = new FileStream(inputFile, FileMode.Open,FileAccess.Read))
                {
                    // 创建解密流
                    using (var cryptoStream = new CryptoStream(
                        inputStream,
                        aes.CreateDecryptor(),
                        CryptoStreamMode.Read))
                    {
                        // 分块解密并写入文件
                        using (var outputStream = new MemoryStream())
                        {
                            byte[] buffer = new byte[1048576];
                            int bytesRead;
                            while ((bytesRead = cryptoStream.Read(buffer, 0, buffer.Length)) > 0)
                            {
                                outputStream.Write(buffer, 0, bytesRead);
                                handler?.Invoke(buffer);
                            }
                        }
                    }
                }
            }
        }

    }

}

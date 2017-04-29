/******************************************
 * 2012��4��25�� ������ ���
 * 
 * ***************************************/

using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace MailSender
{

	/// <summary>
	/// �ӽ��ܸ�����
	/// </summary>
    public static class EncryptDecryptHelper
    {

        //Ĭ����Կ����
        private static byte[] _keys = { 0x34, 0x56, 0x78, 0xAB, 0xCD,0x12, 0x90, 0xEF };

        /// <summary>
        /// DES�����ַ���
        /// </summary>
        /// <param name="encryptString">�����ܵ��ַ���</param>
        /// <param name="encryptKey">������Կ,Ҫ��Ϊ8λ</param>
        /// <returns>���ܳɹ����ؼ��ܺ���ַ�����ʧ�ܷ���Դ��</returns>
        public static string EncryptDES(string encryptString, string encryptKey)
        {
            try
            {
                byte[] rgbKey = Encoding.UTF8.GetBytes(encryptKey.Substring(0, 8));
                byte[] rgbIV = _keys;
                byte[] inputByteArray = Encoding.UTF8.GetBytes(encryptString);
                DESCryptoServiceProvider dCSP = new DESCryptoServiceProvider();
                MemoryStream mStream = new MemoryStream();
                CryptoStream cStream = new CryptoStream(mStream, dCSP.CreateEncryptor(rgbKey, rgbIV), CryptoStreamMode.Write);
                cStream.Write(inputByteArray, 0, inputByteArray.Length);
                cStream.FlushFinalBlock();
                return Convert.ToBase64String(mStream.ToArray());
            }
            catch
            {
                return encryptString;
            }
        }

        /// <summary>
        /// DES�����ַ���
        /// </summary>
        /// <param name="decryptString">�����ܵ��ַ���</param>
        /// <param name="decryptKey">������Կ,Ҫ��Ϊ8λ,�ͼ�����Կ��ͬ</param>
        /// <returns>���ܳɹ����ؽ��ܺ���ַ�����ʧ�ܷ�Դ��</returns>
        public static string DecryptDES(string decryptString, string decryptKey)
        {
            try
            {
                byte[] rgbKey = Encoding.UTF8.GetBytes(decryptKey);
                byte[] rgbIV = _keys;
                byte[] inputByteArray = Convert.FromBase64String(decryptString);
                DESCryptoServiceProvider DCSP = new DESCryptoServiceProvider();
                MemoryStream mStream = new MemoryStream();
                CryptoStream cStream = new CryptoStream(mStream, DCSP.CreateDecryptor(rgbKey, rgbIV), CryptoStreamMode.Write);
                cStream.Write(inputByteArray, 0, inputByteArray.Length);
                cStream.FlushFinalBlock();
                return Encoding.UTF8.GetString(mStream.ToArray());
            }
            catch
            {
                return decryptString;
            }
        }

        /// <summary>
        /// MD5�����ַ���
        /// </summary>
        /// <param name="encryptString">��Ҫ���ܵ��ַ���</param>
        /// <returns></returns>
        public static string EncryptMD5(string encryptString)
        {
            if (string.IsNullOrEmpty(encryptString)) { return string.Empty; }
            HashAlgorithm hashAlgorithm = (HashAlgorithm) CryptoConfig.CreateFromName("MD5");

            using (hashAlgorithm)
            {
                return BinaryToHex(hashAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(encryptString)));
            }

            //Byte[] clearBytes = new UnicodeEncoding().GetBytes(EncryptString);
            //Byte[] hashedBytes = ((HashAlgorithm)CryptoConfig.CreateFromName("MD5")).ComputeHash(clearBytes);

            //return BitConverter.ToString(hashedBytes);

            ////string cl = EncryptString;
            ////string pwd = "";
            ////MD5 md5 = MD5.Create();//ʵ����һ��md5���� 
            ////// ���ܺ���һ���ֽ����͵����飬����Ҫע�����UTF8/Unicode�ȵ�ѡ�� 
            ////byte[] s = md5.ComputeHash(Encoding.UTF8.GetBytes(cl));
            ////// ͨ��ʹ��ѭ�������ֽ����͵�����ת��Ϊ�ַ��������ַ����ǳ����ַ���ʽ������ 
            ////for (int i = 0; i < s.Length; i++)
            ////{
            ////    // ���õ����ַ���ʹ��ʮ���������͸�ʽ����ʽ����ַ���Сд����ĸ�����ʹ�ô�д��X�����ʽ����ַ��Ǵ�д�ַ� 
            ////    pwd = pwd + s[i].ToString("X");

            ////}
            ////return pwd;

            ////MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider();
            ////string t2 = BitConverter.ToString(md5.ComputeHash(UTF8Encoding.Default.GetBytes(EncryptString)), 4, 8);
            ////t2 = t2.Replace("-", "");
            ////return t2;

            ////MD5CryptoServiceProvider md5 = new MD5CryptoServiceProvider();
            ////byte[] encryptedBytes = md5.ComputeHash(Encoding.ASCII.GetBytes(EncryptString));
            ////StringBuilder sb = new StringBuilder();
            ////for (int i = 0; i < encryptedBytes.Length; i++)
            ////{
            ////    sb.AppendFormat("{0:x2}", encryptedBytes[i]);
            ////}
            ////return sb.ToString();

        }

        /// <summary>
        /// Converts a byte array into its hexadecimal representation.
        /// </summary>
        /// <param name="data">The binary byte array.</param>
        /// <returns>The hexadecimal (uppercase) equivalent of the byte array.</returns>
        public static string BinaryToHex(byte[] data)
        {
            if (data == null)
            {
                return null;
            }

            char[] hex = new char[checked(data.Length * 2)];

            for (int i = 0; i < data.Length; i++)
            {
                byte thisByte = data[i];
                hex[2 * i] = NibbleToHex((byte)(thisByte >> 4)); // high nibble
                hex[2 * i + 1] = NibbleToHex((byte)(thisByte & 0xf)); // low nibble
            }

            return new string(hex);
        }

        // converts a nibble (4 bits) to its uppercase hexadecimal character representation [0-9, A-F]
        private static char NibbleToHex(byte nibble)
        {
            return (char)((nibble < 10) ? (nibble + '0') : (nibble - 10 + 'A'));
        }

	}
}

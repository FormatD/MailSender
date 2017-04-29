using System;
using System.IO;
using System.Text;
using ICSharpCode.SharpZipLib.Zip;
using NLog;

namespace MailSender
{

    /// <summary>
    /// 使用SharpZipLib来完成打包解包
    /// </summary>
    public class ZipHelper
    {
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();
        /// <summary>
        /// 打包
        /// </summary>
        /// <param name="zipFileName">输出压缩文件名称</param>
        /// <param name="sourceFolderName">需要压缩的文件夹名称</param>
        /// <returns>成功true,失败false</returns>
        public static bool Pack(string zipFileName, string sourceFolderName)
        {
            try
            {
                var fastZip = CreateZipComponent();
                
                fastZip.CreateZip(zipFileName, sourceFolderName, true, null);
                return true;
            }
            catch (Exception ex)
            {
                // 记录一个未处理异常的日志
                _logger.Error(ex);
            }

            return false;
        }

        /// <summary>
        /// 解包
        /// </summary>
        /// <param name="zipFileName">压缩文件名称</param>
        /// <param name="targetFolderName">解压缩的目标文件夹名称</param>
        /// <returns>成功true,失败false</returns>
        public static bool Unpack(string zipFileName, string targetFolderName)
        {
            try
            {
                var fastZip = CreateZipComponent();
                fastZip.ExtractZip(zipFileName, targetFolderName, FastZip.Overwrite.Always, null, null, null, true);
                return true;
            }
            catch (Exception ex)
            {
                // 记录一个未处理异常的日志
                _logger.Error(ex);
            }

            return false;
        }

        /// <summary>
        /// 创建
        /// </summary>
        /// <returns></returns>
        private static FastZip CreateZipComponent()
        {
            var fastZip = new FastZip
            {
                CreateEmptyDirectories = true,
                RestoreAttributesOnExtract = true,
                RestoreDateTimeOnExtract = true
            };
            return fastZip;
        }

        /// <summary>
        /// 压缩文件(附带密码)
        /// </summary>
        /// <param name="inputFileName">待压缩的文件名称</param>
        /// <param name="outZipFileName">压缩后的文件名称</param>
        /// <param name="password">压缩密码</param>
        /// <returns>成功返回True</returns>
        public static bool ZipFileWithPassword(string inputFileName, string outZipFileName, string password)
        {
            if (!File.Exists(inputFileName))
            {
                throw new FileNotFoundException();
            }

            System.IO.FileStream streamToZip = new System.IO.FileStream(inputFileName, System.IO.FileMode.Open, System.IO.FileAccess.Read);
            
            System.IO.FileStream zipFileStream = System.IO.File.Create(outZipFileName);

            ZipOutputStream zipOutputStream = new ZipOutputStream(zipFileStream);
            System.IO.FileInfo f = new System.IO.FileInfo(inputFileName);

            ZipEntry ZipEntry = new ZipEntry(f.Name);
            zipOutputStream.Password = password;
            zipOutputStream.PutNextEntry(ZipEntry);
            //把密码用MD5加密的字符写入文件头
            string passwordMD5 = EncryptDecryptHelper.EncryptMD5(password);
            byte[] passwordData = Encoding.Unicode.GetBytes(passwordMD5);

            int dataLength = passwordData.Length + 4;
            byte[] headerData = BitConverter.GetBytes(dataLength);
            //前4位为真实数据存放起始位置
            zipOutputStream.Write(headerData,0,headerData.Length);
            //写入密码数据
            zipOutputStream.Write(passwordData, 0,passwordData.Length);

            byte[] buffer = new byte[1024];
            System.Int32 size = streamToZip.Read(buffer, 0, buffer.Length);
            
            zipOutputStream.Write(buffer, 0, size);

            try
            {
                while (size < streamToZip.Length)
                {
                    int sizeRead = streamToZip.Read(buffer, 0, buffer.Length);
                    zipOutputStream.Write(buffer, 0, sizeRead);
                    size += sizeRead;
                }
            }
            catch (System.Exception ex)
            {
                throw ex;
            }
            finally
            {
                zipOutputStream.Finish();
                zipOutputStream.Close();
                streamToZip.Close();
            }
            return true;
        }
        /// <summary>
        /// 解压文件(附带密码)
        /// </summary>
        /// <param name="zipFileName">压缩文件名称</param>
        /// <param name="unZipFileName">解压后文件名</param>
        /// <param name="password">密码</param>
        /// <returns>成功返回true</returns>
        public static bool UnZipFileWithPassword(string zipFileName, string unZipFileName, string password)
        {
            using (ZipInputStream zs = new ZipInputStream(File.OpenRead(zipFileName)))
            {

                ZipEntry theEntry = zs.GetNextEntry();
                if (!theEntry.IsCrypted)
                {
                    throw new Exception("不是合法的文件。");
                }
                zs.Password = password;
                string passwordMD5 = EncryptDecryptHelper.EncryptMD5(password);
                string dir = System.IO.Path.GetDirectoryName(unZipFileName);

                if (!System.IO.Directory.Exists(dir))
                    System.IO.Directory.CreateDirectory(dir);

                using (FileStream streamWriter = File.Create(unZipFileName))
                {
                    byte[] buffer = new byte[1024];
                    //读取头数据
                    try
                    {
                        zs.Read(buffer, 0, 4);
                    }
                    catch (ICSharpCode.SharpZipLib.Zip.ZipException zipEx)
                    {
                        if (zipEx.Message == "Invalid password")
                            throw new Exception("解压密码错误。");
                    }

                    int headerLength = BitConverter.ToInt32(buffer, 0);
                    
                    //写输出参数
                    byte[] passwordData = new byte[headerLength - 4];
                    zs.Read(passwordData, 0, headerLength - 4);
                    string usedPassword = Encoding.Unicode.GetString(passwordData);
                    
                    if (usedPassword != passwordMD5)
                    {
                        throw new Exception("解压密码错误。");
                    }
                    int size = 0;
                    size = zs.Read(buffer, 0, 1024);
                    while (size>0)
                    {
                        streamWriter.Write(buffer, 0, size);
                        size = zs.Read(buffer, 0, 1024);
                    }
                }
            }
            return false;
        }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NLog;

namespace MailSender
{
    public class Config
    {
        public string SendFrom { get; set; } = "sample@live.cn";

        public string SendTo { get; set; } = "dengqianjun@huawei.com";

        public int MaxSize { get; set; } = 9000000;
        public string SmtpServerAddress { get; set; } = "smtp-mail.outlook.com";
        public int SmtpServerPort { get; set; } = 587;
        public string UserName { get; set; } = "sample@live.cn";
        public string UserPass { get; set; } = "";
    }

    class Program
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private static Config _config;
        private static string _settingFile = "setting.json";

        static void Main(string[] args)
        {
            EnsureConfigFile();

            if (args.Length == 0)
            {
                Logger.Info("Usage:");
                Logger.Info(@"MailSender C:\Users\formatd\Desktop\anyexec-1.1-linux_x64.tar.gz [anyexec-1.1-linux_x64]");
                return;
            }

            //   GetNuget("EntityFrameWork", @"D:\Microsoft.Nuget.Packages\");
            SendEmail(args[0], args.Length > 1 ? args[1] : null);
        }

        private static void EnsureConfigFile()
        {
            if (!File.Exists(_settingFile))
                File.WriteAllText(_settingFile, JsonConvert.SerializeObject(new Config(), new JsonSerializerSettings { Formatting = Formatting.Indented }));

            try
            {
                _config = JsonConvert.DeserializeObject<Config>(File.ReadAllText(_settingFile));
            }
            catch (Exception)
            {
                _config = new Config();
                Logger.Info($"config file {_settingFile} was broken.");
            }
        }

        public static void GetNuget(string packageName, string targetPath)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "nuget.exe",
                    Arguments = $"install {packageName} -NoCache -Verbosity detailed -OutputDirectory {targetPath}",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                }
            };

            var sb = new StringBuilder();
            process.ErrorDataReceived += (sender, args) =>
            {
                Logger.Error(args.Data);
                //   sb.Append(args.Data);
            };
            process.OutputDataReceived += (sender, args) =>
            {
                Logger.Info(args.Data);
                sb.Append(args.Data);
            };

            process.Start();

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            //using (StreamReader reader = process.StandardOutput)
            //{
            //    string result = reader.ReadToEnd();
            //    Console.Write(result);
            //}
            process.WaitForExit();
            var regex = new Regex(@"(Successfully\sinstalled\s'(?<PackageName>.+?)'\sto)|(Package\s""(?<PackageName>.+?)""\sis\salready\sinstalled)", RegexOptions.Compiled);
            var matches = regex.Matches(sb.ToString());
            string tempDirectory = GetTemporaryDirectory();

            foreach (Match match in matches)
            {
                if (match.Success)
                {
                    var downloadedPackageName = match.Groups["PackageName"].Value;
                    Logger.Info($"Found Package:{downloadedPackageName}");
                    var newFile = Path.Combine(tempDirectory, $"{downloadedPackageName}.zip");
                    ZipHelper.Pack(newFile, Path.Combine(targetPath, downloadedPackageName));
                    Logger.Info($"Ziped Package:{downloadedPackageName}");

                    SendEmail(newFile, downloadedPackageName);
                    Logger.Info($"Sended Package:{downloadedPackageName} to {_config.SendTo}");
                }
            }
        }

        public static string GetTemporaryDirectory()
        {
            string tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempDirectory);
            return tempDirectory;
        }

        private static void SendEmail(string file, string subject = null)
        {
            subject = subject ?? Path.GetFileNameWithoutExtension(file);
            var fileList = SplitFile(file);

            foreach (var fileFragement in fileList)
            {
                SmtpClient client = new SmtpClient(_config.SmtpServerAddress, _config.SmtpServerPort)
                {
                    EnableSsl = true,
                    UseDefaultCredentials = true,
                    Credentials = new NetworkCredential(_config.UserName, _config.UserPass),
                    Timeout = 120000,
                };

                client.SendCompleted += Client_SendCompleted;

                var message = CreateMessage(fileFragement, subject);
                try
                {
                    Logger.Info($"begin send file {fileFragement}.");
                    //client.Send(message);
                    client.SendAsync(message, new SendArg { File = fileFragement, RetryTime = 0, Client = client, Message = message });
                }
                catch (Exception ex)
                {
                    Logger.Error($"Exception caught in SendEmail(): {ex}");
                }
            }

            Console.ReadKey();
        }

        private static void Client_SendCompleted(object sender, AsyncCompletedEventArgs e)
        {
            var sendArg = e.UserState as SendArg;
            if (sendArg == null)
                return;

            if (e.Error != null)
            {
                Logger.Warn($"Error when send file {sendArg.File} ,the {sendArg.RetryTime + 1} times.");
                //if (sendArg.RetryTime < 3)
                sendArg.Client.SendAsync(sendArg.Message, sendArg.Retry());
            }
            else
            {
                Logger.Info($"Sucess when send file {sendArg.File} ,the {sendArg.RetryTime + 1} times.");
            }
        }

        private static IEnumerable<string> SplitFile(string file)
        {
            var fileList = new List<string>();
            var fileInfo = new FileInfo(file);
            if (!fileInfo.Exists)
                return fileList;

            if (fileInfo.Length <= _config.MaxSize)
            {
                fileList.Add(file);
                return fileList;
            }

            var offset = 0;
            var index = 1;
            using (var fs = File.OpenRead(file))
            {
                byte[] fileBytes = new byte[_config.MaxSize];

                var readByte = fs.Read(fileBytes, offset, _config.MaxSize);

                while (readByte > 0)
                {
                    var targetBytes = readByte == _config.MaxSize ? fileBytes : fileBytes.Take(readByte).ToArray();
                    var fileName = $"{file}.{index:D3}";
                    File.WriteAllBytes(fileName, targetBytes);

                    fileList.Add(fileName);

                    offset += readByte;
                    index++;
                    readByte = fs.Read(fileBytes, 0, _config.MaxSize);
                }
            }

            return fileList;
        }

        private static MailMessage CreateMessage(string file, string subject = null)
        {
            MailMessage message = new MailMessage()
            {
                From = new MailAddress(_config.SendFrom),
                Sender = new MailAddress(_config.SendFrom),
                Subject = subject ?? $"{Path.GetFileNameWithoutExtension(file)}.",
                Body = $"file of {file}."
            };

            foreach (var mailAddress in _config.SendTo.Split(';'))
            {
                message.To.Add(new MailAddress(mailAddress, "Deng qianjun"));
            }

            Attachment data = new Attachment(file, MediaTypeNames.Application.Octet);
            var disposition = data.ContentDisposition;
            disposition.CreationDate = File.GetCreationTime(file);
            disposition.ModificationDate = File.GetLastWriteTime(file);
            disposition.ReadDate = File.GetLastAccessTime(file);
            message.Attachments.Add(data);
            return message;
        }

    }



    public class SendArg
    {
        public string File { get; set; }

        public int RetryTime { get; set; }

        public SmtpClient Client { get; set; }

        public MailMessage Message { get; set; }

        public SendArg Retry()
        {
            return new SendArg { File = File, RetryTime = RetryTime + 1, Client = Client, Message = Message };
        }
    }
}
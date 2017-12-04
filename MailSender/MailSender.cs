using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using NLog;
using System.Collections.Concurrent;
using System.Threading;

namespace MailSender
{
    public class MailSender
    {
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private readonly Config _config;
        private string _taskFile;
        private const string TaskFolder = "tasks";

        public MailSender(Config config)
        {
            _config = config;
        }

        public void SendEmail(string file, string subject = null, bool reuse = false)
        {
            var queue = new ConcurrentQueue<string>();
            if (!File.Exists(file))
            {
                _logger.Error("file \"{0}\" was not existed.", file);
                return;
            }
            subject = subject ?? Path.GetFileNameWithoutExtension(file);

            var size = new FileInfo(file).Length;
            _taskFile = Path.Combine(TaskFolder, $"{subject}_{size}");

            EnsureDirectory(TaskFolder);

            var fileList = SplitFile(file).ToList();
            if (reuse)
            {
                var finishedFragements = File.ReadAllLines(_taskFile);
                fileList = fileList.Where(x => !finishedFragements.Contains(x)).ToList();
            }
            else
            {
                File.Delete(_taskFile);
            }

            foreach (var fileFragement in fileList)
            {
                queue.Enqueue(fileFragement);

                var client = new SmtpClient(_config.SmtpServerAddress, _config.SmtpServerPort)
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
                    _logger.Info($"begin send file {fileFragement}.");
                    // client.Send(message);
                    client.SendAsync(message, new SendArg { File = fileFragement, RetryTime = 0, Client = client, Message = message, QueuedFiles = queue });
                }
                catch (Exception ex)
                {
                    _logger.Error($"Exception caught in SendEmail(): {ex}");
                }
            }

            while (queue.Any())
            {
                Thread.Sleep(100);
            }
        }

        private static void EnsureDirectory(string folderName)
        {
            if (!Directory.Exists(folderName))
                Directory.CreateDirectory(folderName);
        }

        private void Client_SendCompleted(object sender, AsyncCompletedEventArgs e)
        {
            var sendArg = e.UserState as SendArg;
            if (sendArg == null)
                return;

            if (e.Error != null)
            {
                _logger.Warn($"Error when send file {sendArg.File} ,the {sendArg.RetryTime + 1} times.");
                _logger.Warn(e.Error);
                //if (sendArg.RetryTime < 3)
                sendArg.Client.SendAsync(sendArg.Message, sendArg.Retry());
            }
            else
            {
                _logger.Info($"Sucess when send file {sendArg.File} ,the {sendArg.RetryTime + 1} times.");

                File.AppendAllText(_taskFile, sendArg.File + Environment.NewLine);

                string fileName;
                while (!sendArg.QueuedFiles.TryDequeue(out fileName))
                {
                }
            }
        }

        private IEnumerable<string> SplitFile(string file)
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

        private MailMessage CreateMessage(string file, string subject = null)
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
}
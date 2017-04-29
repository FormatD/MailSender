﻿using System;
using System.IO;
using System.Net.Mail;
using CommandLine;
using Newtonsoft.Json;
using NLog;

namespace MailSender
{
    class Program
    {
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private static Config _config;
        private static string _settingFile = "setting.json";

        static int Main(string[] args)
        {
            var result = Parser.Default.ParseArguments<SendNugetOptions, SendFileOptions>(args);
            var texts = result
                .MapResult(
                    (SendNugetOptions opts) =>
                    {
                        EnsureConfigFile(opts);
                        new NugetSender(_config, new MailSender(_config)).DownloadAndSendNuget(opts.PackageName, _config.PackagesSavePath);
                        return "0";
                    },
                    (SendFileOptions opts) =>
                    {
                        EnsureConfigFile(opts);
                        new MailSender(_config).SendEmail(opts.FileToSend, opts.Subject);
                        return "0";
                    },
                    _ =>
                    {
                        return "2";
                    });

            return texts.Equals("1") ? 1 : 0;
        }

        private static void EnsureConfigFile(IMailOptions options)
        {
            string configFile = options.ConfigFile;
            if (!string.IsNullOrWhiteSpace(configFile) && _settingFile != configFile)
            {
                if (File.Exists(configFile))
                {
                    _settingFile = configFile;
                }
                else
                {
                    _logger.Warn("special config file \"{0}\" not exist,use defalt config file \"{1}\".", configFile, _settingFile);
                }
            }

            if (!File.Exists(_settingFile))
            {
                File.WriteAllText(_settingFile, JsonConvert.SerializeObject(new Config(), new JsonSerializerSettings { Formatting = Formatting.Indented }));
                _logger.Info("defalt config file \"{0}\" created.", configFile);
            }

            try
            {
                _config = JsonConvert.DeserializeObject<Config>(File.ReadAllText(_settingFile));

                if (string.IsNullOrWhiteSpace(options.SendTo))
                    _config.SendTo = options.SendTo;
            }
            catch (Exception)
            {
                _config = new Config();
                _logger.Info($"config file {_settingFile} was broken.");
            }
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
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using NLog;

namespace MailSender
{
    public class NugetSender
    {
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private readonly Config _config;
        private MailSender _mailSender;

        public NugetSender(Config config, MailSender mailSender)
        {
            _config = config;
            _mailSender = mailSender;
        }

        public void DownloadAndSendNuget(string packageName, string targetPath)
        {
            if (!Directory.Exists(targetPath))
                Directory.CreateDirectory(targetPath);

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
                _logger.Error(args.Data);
                //   sb.Append(args.Data);
            };
            process.OutputDataReceived += (sender, args) =>
            {
                _logger.Info(args.Data);
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
                    _logger.Info($"Found Package:{downloadedPackageName}");
                    var newFile = Path.Combine(tempDirectory, $"{downloadedPackageName}.zip");
                    ZipHelper.Pack(newFile, Path.Combine(targetPath, downloadedPackageName));
                    _logger.Info($"Ziped Package:{downloadedPackageName}");

                    _mailSender.SendEmail(newFile, downloadedPackageName);
                    _logger.Info($"Sended Package:{downloadedPackageName} to {_config.SendTo}");
                }
            }
        }

        public static string GetTemporaryDirectory()
        {
            string tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempDirectory);
            return tempDirectory;
        }
    }
}
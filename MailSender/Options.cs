using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;
using CommandLine.Text;

namespace MailSender
{

    [Verb("Send", HelpText = "Send file via emal")]
    internal class SendFileOptions : IMailOptions
    {
        [Option('f', "fileName", Required = true, HelpText = "File to send.")]
        public string FileToSend { get; set; }

        [Option('s', "subject", HelpText = "email subject.")]
        public string Subject { get; set; }

        [Option('c', "config", Default = "setting.json", HelpText = "special config file.")]
        public string ConfigFile { get; set; }

        [Option('t', "to", HelpText = "special the address to send to.")]
        public string SendTo { get; set; }

        [Option('m', "maxSize", HelpText = "max splited file size in byte.")]
        public int MaxFileSize { get; set; }

        [Usage]
        public static IEnumerable<Example> Usage => new List<Example>
        {
            new Example("simple use", new SendFileOptions {FileToSend = @"C:\bigfile.big"}),
            new Example("special send to address", new SendFileOptions {FileToSend = @"C:\bigfile.big",SendTo ="anather@email.com"}),
            new Example("special config file", new SendFileOptions {FileToSend = @"C:\bigfile.big",ConfigFile = "path.to.config.json"}),
            new Example("special max splited file size(10M)", new SendFileOptions {FileToSend = @"C:\bigfile.big",MaxFileSize = 10000000 }),
        };
    }

    [Verb("Nuget", HelpText = "Send Nuget packages via email")]
    internal class SendNugetOptions : IMailOptions
    {
        [Option('p', "packageName", Required = true, HelpText = "name of package to send.")]
        public string PackageName { get; set; }

        [Option('c', "config", Default = "setting.json", HelpText = "special config file.")]
        public string ConfigFile { get; set; }

        [Option('t', "to", HelpText = "special the address to send to.")]
        public string SendTo { get; set; }

        [Option('m', "maxSize", HelpText = "max splited file size in byte.")]
        public int MaxFileSize { get; set; }

        [Usage]
        public static IEnumerable<Example> Usage => new List<Example>
        {
            new Example("simple use", new SendNugetOptions {}),
            new Example("special send to address", new SendNugetOptions {PackageName = @"EntityFramework",SendTo ="anather@email.com"}),
            new Example("special config file", new SendNugetOptions {PackageName = @"EntityFramework",ConfigFile = "path.to.config.json"}),
            new Example("special max splited file size(10M)", new SendNugetOptions {PackageName = @"EntityFramework",MaxFileSize = 10000000 }),

        };
    }

}

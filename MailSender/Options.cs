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

        [Usage]
        public static IEnumerable<Example> Usage => new List<Example>
        {
            new Example("simple use", new SendFileOptions {FileToSend = @"C:\bigfile.big"}),
        };
    }

    [Verb("Nuget", HelpText = "Send Nuget packages via email")]
    internal class SendNugetOptions: IMailOptions
    {
        [Option('p', "packageName", Required = true, HelpText = "name of package to send.")]
        public string PackageName { get; set; }

        [Option('c', "config", Default = "setting.json", HelpText = "special config file.")]
        public string ConfigFile { get; set; }

        [Option('t', "to", HelpText = "special the address to send to.")]
        public string SendTo { get; set; }

        [Usage]
        public static IEnumerable<Example> Usage => new List<Example>
        {
            new Example("simple use", new SendNugetOptions {}),
        };
    }

}

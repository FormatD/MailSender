﻿namespace MailSender
{
    internal interface IMailOptions
    {
        string ConfigFile { get; set; }
        string SendTo { get; set; }
    }
}
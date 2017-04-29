# MailSender
a small tool to split big files and send via email

## Functions ##

### Split File And Send Via Email ###
usage:
- MailSender Send -f path-to-file -t to@email.com


### Download Nuget package and send via Email ###
usage:
- MailSender Nuget -p pacakgeName -t to@email.com

## Config Files ##
```json
{
  "SendFrom": "sample@qq.com",
  "SendTo": "sample1@xx.com",
  "MaxSize": 9000000,
  "SmtpServerAddress": "smtp.qq.com",
  "SmtpServerPort": 587,
  "UserName": "sample@qq.com",
  "UserPass": "password",
  "PackagesSavePath": "path_to_save_temp_package_"
}
```

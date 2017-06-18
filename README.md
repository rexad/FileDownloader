# FileDownloader
File downloader : downloads files from different sources and supports multiple protocols.

Main entry is from the Console project but you can test it from the unit test project
FileDownloader.cs contains the workflow/different steps to download files .

The files are downloaded simultaneously each file is divided into segments and they are downloaded simultaneously too.

You can configure the different parameter from the app.config 0

The solution proposed for the different constraints :

The program should be extensible to support different protocols: We use Variance and Composition to achieve this, each protocol downloader needs to implement IProtocolDownloader and registered in the startup file (program.cs), we extract the protocol name from the Url and it is automatically resolved.

Some sources might be very big (more than memory): A buffer is used we write small chunks of data from the  stream and we write it to the file 

Some sources might be very slow, while others might be fast: The program uses multi threading each file downloader runs in it's own thread, in addition each file is subdivided into segments (configurable in the app.config) and they runs in their own thread (speed up download) and extensible to mirrors.

Some sources might fail in the middle of download :A retrial system exist (the number of retry and the delay between retrial can be configured in the app.config)

We don't want to have partial data in the final location in any case: If a segment fails to download it cancel all other segments and delete the file .

3 rd parties libraries used :

Unity as a DI Injection  https://github.com/unitycontainer/unity

Serilog for logs https://serilog.net/

Nunit for unit tests https://www.nunit.org/

Moq for the mock https://github.com/Moq/moq4/wiki/Quickstart

ps: when you use the project please restore the nuget packages 




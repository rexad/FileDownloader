# FileDownloader
File downloader : downloads files from different sources and supports multiple protocols.

Main entry is from the Console project (not done yet) but you can test it from the unit test project
FileDownloader.cs contains the workflow/different steps to download files 

the files are downloaded simultaneously each file is divided into segment and those segments are donwloaded simultaneously too

you can configure the different parameter from the app.config in \FileDownloader\AgodaFileDownloader.UTest

the solution proposed for the different constraints

The program should extensible to support different protocols :We use Variance and Composition to achieve this, each protocol downloader needs to implement IProtocolDownloader and registered inthe startup, we extract the protocol name from the Url and it is automatically resolved

Some sources might very big (more than memory) :A bufffer is used we write smallchunks of data from the data stream and we write it tothe file 

Some sources might be very slow, while others might be fast :the program uses multi threading each file downloader runs in it's own thread, in addition each file is subdivided into segments (configurable in the app.config) and they runs in their own thread (speed up doiwnload) and extensible to mirrors

Some sources might fail in the middle of download :aretry system exist (tyhe number of retry adn the delay between retry can be configured in the app.config)

We don't want to have partial data in the final location in any case:if a segment fails to download it cancel all other segments and delete the file 


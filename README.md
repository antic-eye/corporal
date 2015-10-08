#Corporal
A command line utility to create xml corpuses from excel files.
##Installation
Extract `Corporal.exe` and all .dll files in the archive to a location of your choice.
##Configuration
If you want to tag your texts with TreeTagger, you need to install it somewhere and tell the Corporal the commandline to use. If you pass no commandline, the Corporal assumes default values.
##Usage
Run the Corporal using the following commandline:

	corporal.exe -f "myFile.xslx" -v -t

| -f $FILE | The file to be read |
| -v | Verbose output |
| -t | Tag with TreeTagger |

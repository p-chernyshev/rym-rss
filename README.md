# RYM RSS

RYM RSS is a service that scrapes [Rate Your Music](https://rateyourmusic.com/)'s Upcoming Releases list from a specified user and turns it into a locally served RSS / Atom feed.

## Installation

RYM RSS can be installed using the pre-built .msi installation executable (Windows x64).
RYM RSS is registered as a Windows service and runs at Windows startup.

To work, the service needs to be provided with a RYM username and cookies rateyourmusic.com uses to authenticate a registered user (at least `username` and `ulv`).
Cookies can be copied from a browser after signing into rateyourmusic.com.
In Chrome, cookies can be found in DevTools (F12) - 'Application' tab - Cookies.

After installing the service, the feeds are available at <http://localhost:5115/rym/rss> / <http://localhost:5115/rym/atom>.

## Configuration

RYM RSS service can be configured from different configuration sources (in order):

* [Standard .NET configuration providers](https://learn.microsoft.com/en-us/dotnet/core/extensions/configuration-providers) such as environment variables
* Windows registry (`HKLM\Software\RymRss`)
* `appsetting.Default.json` in installation directory
* `appsettings.json` in data directory (by default, `%ProgramData%\RymRss`)

Configuration structure:

```
AppOptions:
    Port = 5115: the port under which the RSS feed can be accessed
    InstallFolder: installation directory where the service executable is located
    DataFolder: data directory with user settings file (the value included in the user settings file itself is ignored)
ScrapeOptions:
    User: username of a RYM user to scrape upcoming albums from
    IntervalMinutes = 60: time (in minutes) between webpage scrapes
    CheckOnLaunch = true: specifies if the service should run the first scrape on start or only after the specified interval
    Cookies: list of web cookies for 'rateyourmusic.com' in 'Set-Cookie' format
```

## Development

[.NET SDK](https://dotnet.microsoft.com/en-us/download) and [dotnet-ef](https://learn.microsoft.com/en-us/ef/core/get-started/overview/install#get-the-net-core-cli-tools) are required to run the source code and build an installer.

### Local testing

Before running the code for the first time you need to create the local database:

    dotnet ef database update --project RymRss

After that, to launch the program and test the local changes to the code you can use `dotnet watch`:

    dotnet watch run --project RymRss --environment Development

### Building an installer

To build a Windows x64 .msi installation file you can launch the batch file in the repository root ([build.bat](build.bat)) or run the following commands:

```batch
dotnet publish RymRss --configuration Release --output bin\publish
dotnet ef migrations bundle --project RymRss --configuration Release --self-contained --target-runtime win-x64 --output bin\sqlitebundle.exe --force
dotnet build Setup --configuration Release -p:Platform=x64 --no-dependencies --output bin\setup
```

The output files will be under [\bin\setup](./bin/setup)

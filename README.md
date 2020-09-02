# IcsFilter

Provides a filtered ICS calendar, showing only when you are busy, but no details. Takes as an input one or more ICS URLs and provides ICS output through an Webserver.

## Prerequisites

This applications needs [mono](https://www.mono-project.com/) to build and run.

## Building

To build:

```
xbuild IcsFilter.sln
```

## Debugging

Debug this application using [Monodevelop](https://www.monodevelop.com/).

## Config

The application takes a xml configuration file with the following contents:

```
<Config>
        <LogFilePrefix>/path/to/log/file</LogFilePrefix>
        <Calendar>
                <Name>MyFancyCalendar_a372d9d7c4c84a2d4c701582f3e70cf0</Name>
                <PrivateUrl>https://nextcoud.example.tld/remote.php/dav/public-calendars/xxxxxxxxx?export</PrivateUrl>
                <PrivateUrl>https://nextcloud.exmpale.tld/remote.php/dav/public-calendars/yyyyyyyyy?export</PrivateUrl>
        </Calendar>
</Config>
```

## Webserver

The web server is provided at `http://localhost:8888`.

## Usage

Start the application using mono:

```
mono IcsFilter.exe config.xml
```



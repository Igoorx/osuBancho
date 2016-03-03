# osuBancho [![AGPL V3 Licence](https://img.shields.io/badge/license-AGPL%20V3-blue.svg)](LICENCE)
An osu!Bancho server written in C# for fun and educational purposes.

## Main things not yet implemented
- Friends List
- IRC (Private Message, Channels)
- BanchoBot

## How use
To use it, you need to have Mysql and Visual Studio (or any IDE/compiler) to compile the C# Source code.
If you have these things, you will need to import the database `osu.sql` into your mysql and then change the settings in the file [Bancho.cs](https://github.com/Igoorx/osuBancho/blob/master/Bancho/Bancho.cs#L55) and create an account on the database on `users_info` table with the password hashed in MD5.

To access the Bancho you need insert the following lines in your [hosts](https://en.wikipedia.org/wiki/Hosts_(file)) file:
```
127.0.0.1 osu.ppy.sh
127.0.0.1 a.ppy.sh
127.0.0.1 c.ppy.sh
127.0.0.1 c1.ppy.sh
```
or you can use [my program](https://github.com/Igoorx/osuPatcher) to edit the Bancho IP in the executable of osu.

## Thanks
- [Aardwolf Rep](https://github.com/JamesDunne/aardwolf) for a part of HttpHost
- peppy for making the osu!

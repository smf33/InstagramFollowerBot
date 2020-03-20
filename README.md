# Instagram Follower Bot

Bot for Instagram, in .Net Core, using a Chrome client and Selenium for command it.

Main functions :
- Unfollow users whose doesn't follow you
- Follow users which are following you
- Follow based on Insta Explore Suggestions
- Search or Explore Photos to Like and/or follow
- Can work with a remote Selenium grid and/or in docker

*Tags	: Instagram, Chrome, Selenium, C#, .Net, Core, bot, robot*

## Requirement

- Windows, Linux or Mac
- .Net Core SDK 3.1 : https://dotnet.microsoft.com/download/dotnet-core/3.1
- Chrome, or a Selenium server with Chrome clients

## Usage

### DotNet run
![.NET Core](https://github.com/smf33/InstagramFollowerBot/workflows/.NET%20Core/badge.svg)

Download the sources and run donet sdk command in the folder of your Windows, Linux or Mac.

- Run with default (users settings have been set in the .json or the environnement variable) :
```
dotnet run
```

- On a daily base, unfollow users whose doesn't follow you :
```
dotnet run BotTasks=DetectContactsUnfollowBack,DoContactsUnfollow BotUserEmail=you@dom.com BotUserPassword=Passw0rd
```

### Docker run
![Docker](https://github.com/smf33/InstagramFollowerBot/workflows/Docker/badge.svg)

- Build and Run default BotTasks with Docker with a remote Selenium Hub (here another docker) :
Exemple with Z:\InstagramFollowerBot as the source path, on a Windows system
```
docker build -f Z:\InstagramFollowerBot\Dockerfile -t instagramfollowerbot Z:\InstagramFollowerBot
docker run --name seleniumContainer --detach --publish 4444:4444 selenium/standalone-chrome --volume /dev/shm:/dev/shm 
docker run --link seleniumContainer:seleniumhost instagramfollowerbot BotUserEmail=you@dom.com BotUserPassword=Passw0rd SeleniumRemoteServer=http://seleniumhost:4444/wd/hub
```

### Docker Compose run
![Docker Compose](https://github.com/smf33/InstagramFollowerBot/workflows/Docker%20Compose/badge.svg)

- Build and Run default BotTasks with Docker and an standalone Selenium

Exemple with BotUserEmail&BotUserPassword provided in the InstagramFollowerBot.json or in the "environment:" of the docker-compose.yml
```
docker-compose up
```

## Configuration
- Main settings :
Settings may be read in command line parameter, else in environnement variable, else in InstagramFollowerBot.json.
Only BotUserEmail and BotUserPassword won't have default working values from the initial configuration file.
BotUserPassword may be set to null in debug mode (the user will be able to insert the password himself)

| Parameter | Description |
| :-------- | :---------- |
| BotUserEmail | Email for auto-login and filename of the session file |
| BotUserPassword | Password for auto-login, may be set to null if session file already created |
| BotUsePersistence | Will create a file for the user session and cookies |
| SeleniumRemoteServer | Url of the Selenium Hub web service |
| BotTasks | Tasks to do, separatedd by a comma |
| BotUserSaveFolder | Where user informations (like cookie) are stored |

- Taks :
Task name is case insensitive
A lot of settings in order to randomize or limit the batch, in the Bot.Json

| Name | Description |
| :--- | :---------- |
| DetectContactsUnfollowBack | Push contacts for DoContactsUnfollow |
| DoContactsUnfollow | Pop elements that DetectContactsUnfollowBack have send to this queue |
| Save | Update the session file |
| Wait | Pause the worker |
| Loop | Restart from first task |

## Notes
- Selenium Chrome Driver must have the same version than your Chrome (change the lib version in the project if required)
- Don't be evil, else Instagram will delete your spamming account
- The solution is micro-service oriented, but Instagram will detected the spamming account if the bot is too fast
- If you want to publish without remote Selenium, add _PUBLISH_CHROMEDRIVER in the DefineConstants of the .csproj
- The account should follow at last one account, else the bot will fail to detect this
- About "Unusual Login Attempt Detected" : If the bot connect from a location, OS, Browser that you never used before, you will get this email code chalenge. Pass it before lauching the bot again. You can change the OS/Browser (Chrome/Windows 10 by default) with the --user-agent in the SeleniumBrowserArguments setting.
## TODO :
- Update this readme for all function now working :-)
- Enable more functions already working on the Flickr version of this bot
- Like pict found and add follow the account by default
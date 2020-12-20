# Instagram Follower Bot

Bot for Instagram, in .Net Core, using Chrome and Selenium for actions.

Main functions :
- Follow&Like with Insta Photos Explore
- Like on Insta home page
- Can work with a remote Selenium grid and/or in docker
- Colored console trace or VSO format for Azure DevOps execition
- Support ApplicationInsights (allow easier tracking of multiple context execution)

*Tags	: Instagram, Chrome, Selenium, C#, .Net, Core, bot, robot*

## Requirement

- Windows, Linux or Mac
- .Net SDK 5.0 : https://dotnet.microsoft.com/download
- Chrome (not Chromium), or a Selenium server with Chrome clients
*If you use Chrome (not Selenium server so), the ChromeDriver of this bot version must match your Chrome version.*

## Usage

### DotNet run
![.NET Core](https://github.com/smf33/InstagramFollowerBot/workflows/.NET/badge.svg)

Download the sources and run donet sdk command in the folder of your Windows, Linux or Mac.

- Run with default (users settings have been set in the .json or the environnement variable) :
```
dotnet run
```

- On a daily base, unfollow users whose doesn't follow you :
```
dotnet run IFB_TaskManager__TaskList=DetectContactsUnfollowBack,DoContactsUnfollow IFB_Logging__User=you@dom.com IFB_Logging__Password=Passw0rd
```

### Docker run
![Docker](https://github.com/smf33/InstagramFollowerBot/workflows/Docker/badge.svg)

- Build and Run default IFB_TaskManager__TaskList with Docker with a remote Selenium Hub (here another docker) :
Exemple with Z:\InstagramFollowerBot as the source path, on a Windows system
```
docker build -f Z:\InstagramFollowerBot\Dockerfile -t instagramfollowerbot Z:\InstagramFollowerBot
docker run --name seleniumContainer --detach --publish 4444:4444 selenium/standalone-chrome --volume /dev/shm:/dev/shm 
docker run --link seleniumContainer:seleniumhost instagramfollowerbot IFB_Logging__User=you@dom.com IFB_Logging__Password=Passw0rd IFB_Selenium__RemoteServer=http://seleniumhost:4444/wd/hub
```

### Docker Compose run
![Docker Compose](https://github.com/smf33/InstagramFollowerBot/workflows/Docker%20Compose/badge.svg)

- Build and Run default IFB_TaskManager__TaskList with Docker and an standalone Selenium

Exemple with IFB_Logging:User&IFB_Logging:Password provided in the InstagramFollowerBot.json or in the "environment:" of the docker-compose.yml
```
docker-compose up
```

## Configuration
- Main settings :
Settings may be read in command line parameter, else in environnement variable, else in InstagramFollowerBot.json.
Only IFB_Logging__User and IFB_Logging__Password won't have default working values from the initial configuration file.
IFB_Logging__Password may be set to null in debug mode (the user will be able to insert the password himself)

| Parameter | Description |
| :-------- | :---------- |
| IFB_Logger_UseApplicationInsights | Enable Microsoft Azure ApplicationInsights, you must define the environnement variable APPINSIGHTS_INSTRUMENTATIONKEY with your key |
| IFB_Logger_UseAzureDevOpsFormating | Use a VSO log format instead of the default colored output, enable it if you run your bot through Azure DevOps pipeline |
| IFB_Logging__Password | Password for auto-login, may be set to null if session file already created |
| IFB_Logging__User | Email or UserName for login and filename for the session file |
| IFB_Persistence__SaveFolder | Where user informations (like cookie) are stored |
| IFB_Persistence__UsePersistence | Will create a file for the user session and cookies |
| IFB_Selenium__RemoteServer | Url of the Selenium Hub web service |
| IFB_Selenium__DumpBrowserContextOnCrash | In case of bot crash, generate a dump of the Chrome browser as .html for the html source and .png for the current view |
| IFB_TaskManager__TaskList | Tasks to do, separatedd by a comma |

- Takss :
Task name is case insensitive
A lot of settings in order to randomize or limit the batch, in the Bot.Json

| Name | Description |
| :--- | :---------- |
| CHECKACTIVITY | Open/Close the Activity popup |
| DOHOMEPAGELIKE | Like post in the home screen, like between IFB_HomePage__LikeMin and IFB_HomePage__LikeMax post |
| DOEXPLOREPHOTOSFOLLOW | Follow only of the DOEXPLOREPHOTOSFOLLOWLIKE, follow between IFB_ExplorePhotos__FollowMin and IFB_ExplorePhotos__FollowMax post |
| DOEXPLOREPHOTOSLIKE | Like only of the DOEXPLOREPHOTOSFOLLOWLIKE, like between IFB_ExplorePhotos__LikeMin and IFB_ExplorePhotos__LikeMax post |
| DOEXPLOREPHOTOSFOLLOWLIKE | Follow and Like of the DOEXPLOREPHOTOSFOLLOWLIKE |
| SAVE | Save the user session file, including cookie, allowing session to be resumed next application launch |
| WAIT | Pause the worker |
| LOOP | Restart from first task (or BEGINLOOP if present), loop for IFB_TaskManager__LoopTaskLimit times |

## Notes
- Selenium Chrome Driver must have the same version than your Chrome (change the lib version in the project if required)
- Don't be evil, else Instagram will delete your spamming account
- The solution is micro-service oriented, but Instagram will detected the spamming account if the bot is too fast
- If you want to publish without remote Selenium, add _PUBLISH_CHROMEDRIVER in the DefineConstants of the .csproj
- The account should follow at last one account, else the bot will fail to detect this
- About "Unusual Login Attempt Detected" : If the bot connect from a location, OS, Browser that you never used before, you will get this email code chalenge. Pass it before lauching the bot again. You can change the OS/Browser (Chrome/Windows 10 by default) with the --user-agent in the SeleniumBrowserArguments setting.
## TODO :
- Enable more functions already working on the Flickr version of this bot
FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build
WORKDIR /src
ENV DOTNET_CLI_TELEMETRY_OPTOUT 1 \
    DOTNET_SKIP_FIRST_TIME_EXPERIENCE 1
RUN apt-get update && apt-get upgrade -y
COPY . .
RUN dotnet publish -c Release -o /app

FROM mcr.microsoft.com/dotnet/core/runtime:3.1 AS final
WORKDIR /app
ENV DOTNET_CLI_TELEMETRY_OPTOUT 1 \
    DOTNET_SKIP_FIRST_TIME_EXPERIENCE 1
RUN apt-get update && apt-get upgrade -y
COPY --from=build /app .
ENTRYPOINT ["dotnet", "InstagramFollowerBot.dll"]
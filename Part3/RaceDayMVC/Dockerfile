FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Part3/RaceDayMVC/RaceDayMVC.csproj Part3/RaceDayMVC/
RUN dotnet restore Part3/RaceDayMVC/RaceDayMVC.csproj

COPY Part3/RaceDayMVC/ Part3/RaceDayMVC/
RUN dotnet publish Part3/RaceDayMVC/RaceDayMVC.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "RaceDayMVC.dll"]

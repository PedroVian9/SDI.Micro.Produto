FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

COPY back-end/SDI.Back.API/SDI.Back.API.csproj back-end/SDI.Back.API/
RUN dotnet restore back-end/SDI.Back.API/SDI.Back.API.csproj

COPY back-end/ back-end/
WORKDIR /src/back-end/SDI.Back.API
RUN dotnet publish SDI.Back.API.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

WORKDIR /app

ENV ASPNETCORE_URLS=http://0.0.0.0:5002
ENV ASPNETCORE_ENVIRONMENT=Production

EXPOSE 5002

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "SDI.Back.API.dll"]

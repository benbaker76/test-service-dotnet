#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["TestService/TestService.csproj", "TestService/"]
RUN dotnet restore "TestService/TestService.csproj"
COPY . .
WORKDIR "/src/TestService"
RUN dotnet build "TestService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "TestService.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "TestService.dll"]

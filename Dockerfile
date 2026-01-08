#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.
#See https://learn.microsoft.com/en-us/visualstudio/containers/container-msbuild-properties?view=vs-2022

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 80
#EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY src .
COPY *.sln ./
COPY Directory.Build.props ./
COPY Directory.Packages.props ./

RUN dotnet restore "Mars.WebApp/Mars.WebApp.csproj"
WORKDIR "/src/Mars.WebApp"
RUN dotnet build "./Mars.WebApp.csproj" -c $BUILD_CONFIGURATION -o /app/build /p:DockerBuild=true

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Mars.WebApp.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false -r linux-x64

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish ./
ENTRYPOINT ["dotnet", "Mars.dll"]
#CMD ["sleep","3600"]

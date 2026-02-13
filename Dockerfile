#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.
#See https://learn.microsoft.com/en-us/visualstudio/containers/container-msbuild-properties?view=vs-2022

# ===========================
# Base runtime image
# ===========================
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 80
#EXPOSE 443

# ===========================
# Build stage
# ===========================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

ARG BUILD_CONFIGURATION=Release

# Git metadata (будет передано через --build-arg)
ARG GIT_SHA=unknown
ARG BUILD_VERSION=0.0.0

WORKDIR /src

# ---------------------------
# OCI Labels (standard)
# ---------------------------
LABEL org.opencontainers.image.title="Mars"
LABEL org.opencontainers.image.description="Mars Web Application"
LABEL org.opencontainers.image.version="${BUILD_VERSION}"
LABEL org.opencontainers.image.revision="${GIT_SHA}"
LABEL org.opencontainers.image.vendor="mdimai666"

# ---------------------------
# Copy only metadata first (best cache)
# ---------------------------
COPY *.slnx Directory.Build.props Directory.Packages.props ./
COPY src/ /tmp/src/
COPY *.slnx Directory.Build.props Directory.Packages.props /tmp/src
# Скопировать только *.csproj с сохранением структуры
RUN cd /tmp/src && \
    find . -name "*.csproj" -exec mkdir -p $(dirname {}) \; && \
    find . -name "*.csproj" -exec cp {} {} \;
#COPY src/Mars.WebApp/*.csproj src/Mars.WebApp/

# ---------------------------
# Restore with NuGet cache (BuildKit)
# ---------------------------
RUN --mount=type=cache,target=/root/.nuget/packages \
    dotnet restore "/tmp/src/Mars.WebApp/Mars.WebApp.csproj"

# ---------------------------
# Copy full source
# ---------------------------
COPY src/ .

# ===========================
# Publish stage
# ===========================
FROM build AS publish

ARG BUILD_CONFIGURATION=Release
ARG GIT_SHA=unknown
ARG BUILD_VERSION=0.0.0

LABEL org.opencontainers.image.title="Mars"
LABEL org.opencontainers.image.description="Mars Web Application"
LABEL org.opencontainers.image.version="${BUILD_VERSION}"
LABEL org.opencontainers.image.revision="${GIT_SHA}"
LABEL org.opencontainers.image.vendor="mdimai666"

WORKDIR "/src/Mars.WebApp"

RUN --mount=type=cache,target=/root/.nuget/packages \
        dotnet publish "./Mars.WebApp.csproj" \
            -c $BUILD_CONFIGURATION \
            -o /app/publish \
            -p:UseAppHost=false \
            -p:DockerBuild=true \
            -p:SourceRevisionId="${GIT_SHA}" \
            -r linux-x64

# ===========================
# Final runtime image
# ===========================
FROM base AS final

# ---------------------------
# Create non-root user
# ---------------------------
#RUN useradd -m marsuser
#
## Ensure app folders exist + permissions
#RUN mkdir -p /app/data \
    #&& chown -R marsuser:marsuser /app
#
#USER marsuser

WORKDIR /app

# Copy published output
COPY --from=publish /app/publish ./

ENTRYPOINT ["dotnet", "Mars.dll"]
#CMD ["sleep","3600"]

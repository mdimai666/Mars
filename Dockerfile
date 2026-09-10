#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.
#See https://learn.microsoft.com/en-us/visualstudio/containers/container-msbuild-properties?view=vs-2022

# ===========================
# Base runtime image
# Chiseled Ubuntu 24.04 (distroless: без shell/apt; ICU+tzdata включены),
# non-root: USER app (uid 1654) задан базовым образом.
# ===========================
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled-extra AS base
# ВАЖНО: WORKDIR /app здесь не ставить — /app обязан создать COPY --chown в final-стадии,
# чтобы владельцем каталога был uid 1654 (в chiseled нет shell, RUN/chown недоступны).

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
COPY *.slnx Directory.Build.props Directory.Packages.props /tmp/src/
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
            --self-contained false \
            -p:UseAppHost=false \
            -p:DockerBuild=true \
            -p:SourceRevisionId="${GIT_SHA}" \
            -r linux-x64

# ===========================
# Final runtime image
# ===========================
FROM base AS final

# Non-root uid 1654 (USER задан базовым образом). В chiseled нет shell, поэтому
# никаких RUN: /app создаётся первым же COPY с владельцем 1654 (иначе каталог
# остался бы root и не-root процесс не смог бы писать в /app).
COPY --from=publish --chown=1654:1654 /app/publish /app

# DataProtection-ключи по умолчанию хранятся в $HOME/.aspnet/DataProtection-Keys:
# фиксируем HOME=/app, чтобы путь был детерминированным (/app/.aspnet/...),
# его монтирует оркестратор CloudPanel.
ENV HOME=/app

# Приложение слушает порт 80 (Urls в appsettings.json перекрывает дефолтные 8080
# базового образа); не-root контейнеру порт 80 доступен (ip_unprivileged_port_start=0).
EXPOSE 80

WORKDIR /app

ENTRYPOINT ["dotnet", "Mars.dll"]

# syntax=docker/dockerfile:1
# Builds the Grootscape world server. Run from the repository root:
#   docker build -f deploy/server.Dockerfile -t grootscape-server .

FROM mcr.microsoft.com/dotnet/sdk:11.0 AS build
WORKDIR /src
COPY global.json Directory.Build.props Directory.Packages.props .editorconfig ./
COPY src/server/ src/server/
RUN dotnet restore src/server/Grootscape.Server/Grootscape.Server.csproj --locked-mode
RUN dotnet publish src/server/Grootscape.Server/Grootscape.Server.csproj \
    --configuration Release --no-restore --output /app

# Chiseled images ship no shell or package manager and run as a non-root user.
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled
WORKDIR /app
COPY --from=build /app ./
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["./Grootscape.Server"]

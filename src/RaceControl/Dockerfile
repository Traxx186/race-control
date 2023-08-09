FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build-env
WORKDIR /App

# Copy everything
COPY . ./

# Restore as distinct layers
RUN dotnet restore --use-current-runtime

# Build and publish a release
RUN dotnet publish -c Release -o out --use-current-runtime --self-contained false --no-restore

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:7.0
WORKDIR /App
COPY --from=build-env /App/out .

# Disables diagnostic pipeline for security
ENV DOTNET_EnableDiagnostics=0

EXPOSE 8080;
ENTRYPOINT ["dotnet", "RaceControl.dll"]
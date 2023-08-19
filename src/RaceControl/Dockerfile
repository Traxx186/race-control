FROM mcr.microsoft.com/dotnet/sdk:7.0-alpine AS build-env
WORKDIR /App

# Copy everything
COPY . ./

# Restore as distinct layers
RUN dotnet restore --runtime alpine-x64

# Build and publish a release
RUN dotnet publish -c Release -o out  \
   --runtime alpine-x64 \
   --self-contained true \
    /p:PublishTrimmed=true \
    /p:TrimMode=Link \
    /p:PublishSingleFile=true 
 
# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:7.0-alpine
WORKDIR /App
COPY --from=build-env /App/out .

# Disables diagnostic pipeline for security
ENV DOTNET_EnableDiagnostics=0

ENTRYPOINT ["dotnet", "RaceControl.dll"]
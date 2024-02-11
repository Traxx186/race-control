#####################################################################
## Build project
####################################################################
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS base
WORKDIR /App

# Copy csproj and restore as distinct layers
COPY ./src/RaceControl/RaceControl.csproj ./

# Restore as distinct layers
RUN dotnet restore --runtime linux-musl-x64

# Copy everything
COPY ./src/RaceControl/ ./

# Build and publish a release
RUN dotnet publish -c Release -o out  \
   --runtime linux-musl-x64 \
   --self-contained true 
 
#####################################################################
## Final image
####################################################################
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine
WORKDIR /App
COPY --from=base /App/out ./

# Disables diagnostic pipeline for security
ENV DOTNET_EnableDiagnostics=0

EXPOSE 5050

ENTRYPOINT ["./RaceControl"]
#####################################################################
## Build project
####################################################################
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build

WORKDIR /race-control

# Copy csproj and restore as distinct layers
COPY ./src/RaceControl/RaceControl.csproj ./

# Restore as distinct layers
RUN dotnet restore --runtime linux-musl-x64

# Copy project files
COPY ./src/RaceControl/ ./
COPY ./app.json ./

# Build and publish a release
RUN dotnet publish -c Release -o out  \
   --runtime linux-musl-x64 \
   --self-contained true 
 
#####################################################################
## Final image
####################################################################
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine

WORKDIR /race-control

# Copy required files
COPY --from=build /race-control/out ./
COPY --from=build /race-control/app.json ./

# Disables diagnostic pipeline for security
ENV DOTNET_EnableDiagnostics=0

# Install required dependencies
RUN apk --no-cache add curl

# create appuser
ENV USER=race-control
ENV UID=32767

# Create new non-root user
RUN adduser \
   --disabled-password \
   --gecos "" \
   --shell "/sbin/nologin" \
   --no-create-home \
   --uid "${UID}" \
   "${USER}"

# Set file permissions
RUN chmod +rw *
RUN chown -R ${USER}:${USER} *

# Use an unprivileged user
USER ${USER}

EXPOSE 8080

# Add healthcheck to the container
HEALTHCHECK --interval=5m --timeout=3s \
    CMD curl -f  http://localhost:8080/health || exit 1

ENTRYPOINT ["./RaceControl"]
# Versions
ARG DOTNET_IMAGE_VERSION=10.0-alpine

# Set appuser info
ARG USER=race-control
ARG USER_ID=32767

#####################################################################
## Build project
#####################################################################
FROM mcr.microsoft.com/dotnet/sdk:${DOTNET_IMAGE_VERSION} AS build

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
#####################################################################
FROM mcr.microsoft.com/dotnet/aspnet:${DOTNET_IMAGE_VERSION}

WORKDIR /race-control

ARG USER
ARG USER_ID

# Copy required files
COPY --from=build /race-control/out ./
COPY --from=build /race-control/app.json ./

# Disables diagnostic pipeline for security
ENV DOTNET_EnableDiagnostics=0

# Install required dependencies
RUN apk add --no-cache --upgrade krb5-libs

# Create new non-root user
RUN adduser \
   --disabled-password \
   --gecos "" \
   --shell "/sbin/nologin" \
   --no-create-home \
   --uid "${USER_ID}" \
   "${USER}"

# Set file permissions
RUN chmod +rw *
RUN chown -R ${USER}:${USER} *

# Use an unprivileged user
USER ${USER}

EXPOSE 8080

# Add healthcheck to the container
HEALTHCHECK --interval=5m --timeout=3s \
    CMD wget --no-verbose --tries=1 --spider http://localhost:8080/health || exit 1

ENTRYPOINT ["./RaceControl"]
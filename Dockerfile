ARG RUNTIMEVERSION=5.0-alpine

FROM mcr.microsoft.com/dotnet/aspnet:$RUNTIMEVERSION AS final
ARG DAMSELFLY_VERSION

WORKDIR /app
COPY /publish .
RUN chmod +x Damselfly.Web 

# Copy the entrypoint script
COPY ./damselfly-entrypoint.sh /
RUN ["chmod", "+x", "/damselfly-entrypoint.sh"]
ADD VERSION .

# Need sudo for the iNotify count increase
RUN set -ex && apk --no-cache add sudo

# Add Microsoft fonts that'll be used for watermarking
RUN apk add --no-cache msttcorefonts-installer fontconfig && update-ms-fonts

# Add ExifTool
RUN apk --update add exiftool && rm -rf /var/cache/apk/*

EXPOSE 6363
ENTRYPOINT ["sh","/damselfly-entrypoint.sh"]

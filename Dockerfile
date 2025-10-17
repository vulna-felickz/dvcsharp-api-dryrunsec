FROM mcr.microsoft.com/dotnet/core/sdk:2.1-focal
LABEL MAINTAINER="Appsecco"

ENV ASPNETCORE_URLS=http://0.0.0.0:5000

# Install ca-certificates to help with SSL issues
RUN apt-get update && \
    apt-get install -y --no-install-recommends ca-certificates && \
    update-ca-certificates && \
    rm -rf /var/lib/apt/lists/*

COPY . /app

WORKDIR /app

EXPOSE 5000

# Use start.sh script which handles restore and database migration at runtime
CMD ["bash", "-c", "./start.sh"]

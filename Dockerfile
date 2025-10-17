FROM mcr.microsoft.com/dotnet/core/sdk:2.1
LABEL MAINTAINER "Appsecco"

ENV ASPNETCORE_URLS=http://0.0.0.0:5000

COPY . /app

WORKDIR /app

RUN sed -i 's/\r$//' start.sh \
    && chmod +x start.sh \
    && dotnet restore \
    && dotnet build \
    && dotnet ef database update

EXPOSE 5000

CMD ["dotnet", "watch", "run"]

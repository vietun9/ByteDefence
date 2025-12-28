FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/ByteDefence.Web/ByteDefence.Web.csproj", "src/ByteDefence.Web/"]
COPY ["src/ByteDefence.Shared/ByteDefence.Shared.csproj", "src/ByteDefence.Shared/"]
RUN dotnet restore "src/ByteDefence.Web/ByteDefence.Web.csproj"

COPY . .
WORKDIR "/src/src/ByteDefence.Web"
RUN dotnet build "ByteDefence.Web.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ByteDefence.Web.csproj" -c Release -o /app/publish

FROM nginx:alpine AS final
WORKDIR /usr/share/nginx/html
COPY --from=publish /app/publish/wwwroot .
COPY src/ByteDefence.Web/nginx.conf /etc/nginx/nginx.conf
EXPOSE 80

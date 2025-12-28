FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/ByteDefence.Api/ByteDefence.Api.csproj", "src/ByteDefence.Api/"]
COPY ["src/ByteDefence.Shared/ByteDefence.Shared.csproj", "src/ByteDefence.Shared/"]
RUN dotnet restore "src/ByteDefence.Api/ByteDefence.Api.csproj"

COPY . .
WORKDIR "/src/src/ByteDefence.Api"
RUN dotnet build "ByteDefence.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ByteDefence.Api.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated8.0 AS final
WORKDIR /home/site/wwwroot
COPY --from=publish /app/publish .
ENV AzureWebJobsScriptRoot=/home/site/wwwroot
ENV AzureFunctionsJobHost__Logging__Console__IsEnabled=true

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/ByteDefence.SignalR/ByteDefence.SignalR.csproj", "src/ByteDefence.SignalR/"]
COPY ["src/ByteDefence.Shared/ByteDefence.Shared.csproj", "src/ByteDefence.Shared/"]
RUN dotnet restore "src/ByteDefence.SignalR/ByteDefence.SignalR.csproj"

COPY . .
WORKDIR "/src/src/ByteDefence.SignalR"
RUN dotnet build "ByteDefence.SignalR.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ByteDefence.SignalR.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 5000
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ByteDefence.SignalR.dll"]

FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["HttpScaner/HttpScaner.csproj", "HttpScaner/"]
RUN dotnet restore "HttpScaner/HttpScaner.csproj"
COPY . .
WORKDIR "/src/HttpScaner"
RUN dotnet build "HttpScaner.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "HttpScaner.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "HttpScaner.dll"]

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY AzureDevOpsMcp.sln ./
COPY TfvcMcpServer/TfvcMcpServer.csproj TfvcMcpServer/
COPY TfvcMcpServer.Tests/TfvcMcpServer.Tests.csproj TfvcMcpServer.Tests/
RUN dotnet restore AzureDevOpsMcp.sln
COPY . .
RUN dotnet publish TfvcMcpServer/TfvcMcpServer.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .
ENV Mcp__Transport=http \
    ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
USER app
ENTRYPOINT ["dotnet", "TfvcMcpServer.dll"]

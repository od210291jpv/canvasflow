# Build Stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["CanvasFlow.Web/CanvasFlow.Web.csproj", "CanvasFlow.Web/"]
COPY ["CanvasFlow.Api/CanvasFlow.Api.csproj", "CanvasFlow.Api/"]
RUN dotnet restore "CanvasFlow.Web/CanvasFlow.Web.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/CanvasFlow.Web"
RUN dotnet build "CanvasFlow.Web.csproj" -c Release -o /app/build

# Publish Stage
FROM build AS publish
RUN dotnet publish "CanvasFlow.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Final Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080
EXPOSE 8081
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CanvasFlow.Web.dll"]

# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files first to leverage Docker cache for dependencies
COPY ["CanvasFlow.Api/CanvasFlow.Api.csproj", "CanvasFlow.Api/"]
RUN dotnet restore "CanvasFlow.Api/CanvasFlow.Api.csproj"

# Copy the rest of the source code and build
COPY . .
WORKDIR "/src/CanvasFlow.Api"
RUN dotnet build "CanvasFlow.Api.csproj" -c Release -o /app/build

# Stage 2: Publish
FROM build AS publish
RUN dotnet publish "CanvasFlow.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 3: Final Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CanvasFlow.Api.dll"]

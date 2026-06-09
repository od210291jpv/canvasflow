# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files first to leverage Docker cache for dependencies
COPY ["CanvasFlow.Web/CanvasFlow.Web.csproj", "CanvasFlow.Web/"]
RUN dotnet restore "CanvasFlow.Web/CanvasFlow.Web.csproj"

# Copy the rest of the source code and build
COPY . .
WORKDIR "/src/CanvasFlow.Web"
RUN dotnet build "CanvasFlow.Web.csproj" -c Release -o /app/build

# Stage 2: Publish
FROM build AS publish
RUN dotnet publish "CanvasFlow.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 3: Final Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 80
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CanvasFlow.Web.dll"]

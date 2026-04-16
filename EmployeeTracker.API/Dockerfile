FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
# Use Render's default port 8080 OR the dynamic $PORT
EXPOSE 8080
EXPOSE 5284

# Critical memory reduction settings for 512MB RAM
ENV DOTNET_EnableDiagnostics=0
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1
ENV DOTNET_GCHeapHardLimit=0x1C000000 
# Disable heavy Server Garbage Collection
ENV DOTNET_gcServer=0

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["EmployeeTracker.API.csproj", "./"]
RUN dotnet restore "EmployeeTracker.API.csproj"
COPY . .
WORKDIR "/src"
RUN dotnet build "EmployeeTracker.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "EmployeeTracker.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
# Ensure permissions for the app to run
ENTRYPOINT ["dotnet", "EmployeeTracker.API.dll"]

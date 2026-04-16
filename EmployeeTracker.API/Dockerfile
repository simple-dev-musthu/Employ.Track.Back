FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 5284
ENV DOTNET_EnableDiagnostics=0
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
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
ENTRYPOINT ["dotnet", "EmployeeTracker.API.dll"]

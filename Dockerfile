FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["EmployeeSyncJob.csproj", "./"]
RUN dotnet restore "EmployeeSyncJob.csproj"
COPY . .
RUN dotnet publish "EmployeeSyncJob.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "EmployeeSyncJob.dll"]

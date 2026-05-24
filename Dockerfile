FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5000
EXPOSE 1883

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["parkmeter_management.Api/parkmeter_management.Api.csproj", "parkmeter_management.Api/"]
COPY ["parkmeter_management.Application/parkmeter_management.Application.csproj", "parkmeter_management.Application/"]
COPY ["parkmeter_management.Domain/parkmeter_management.Domain.csproj", "parkmeter_management.Domain/"]
COPY ["parkmeter_management.Infrastructure/parkmeter_management.Infrastructure.csproj", "parkmeter_management.Infrastructure/"]
RUN dotnet restore "parkmeter_management.Api/parkmeter_management.Api.csproj"
COPY . .
WORKDIR "/src/parkmeter_management.Api"
RUN dotnet publish -c Release -o /app/publish /p:TreatWarningsAsErrors=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "parkmeter_management.Api.dll"]
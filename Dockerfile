FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 10000
ENV ASPNETCORE_URLS=http://+:10000
ENV DOTNET_RUNNING_IN_CONTAINER=true

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["NuGet.config", "./"]
COPY ["Directory.Build.props", "./"]
COPY ["src/MedFund.Api/MedFund.Api.csproj", "src/MedFund.Api/"]
COPY ["src/MedFund.Application/MedFund.Application.csproj", "src/MedFund.Application/"]
COPY ["src/MedFund.Domain/MedFund.Domain.csproj", "src/MedFund.Domain/"]
COPY ["src/MedFund.Infrastructure/MedFund.Infrastructure.csproj", "src/MedFund.Infrastructure/"]
RUN dotnet restore "src/MedFund.Api/MedFund.Api.csproj" --configfile NuGet.config

COPY . .
RUN dotnet publish "src/MedFund.Api/MedFund.Api.csproj" -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "MedFund.Api.dll"]

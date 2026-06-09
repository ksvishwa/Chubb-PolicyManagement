FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5000
ENV ASPNETCORE_URLS=http://+:5000

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["backend/Chubb.PolicyManagement.Api/Chubb.PolicyManagement.Api.csproj", "backend/Chubb.PolicyManagement.Api/"]
COPY ["backend/Chubb.PolicyManagement.Application/Chubb.PolicyManagement.Application.csproj", "backend/Chubb.PolicyManagement.Application/"]
COPY ["backend/Chubb.PolicyManagement.Domain/Chubb.PolicyManagement.Domain.csproj", "backend/Chubb.PolicyManagement.Domain/"]
COPY ["backend/Chubb.PolicyManagement.Infrastructure/Chubb.PolicyManagement.Infrastructure.csproj", "backend/Chubb.PolicyManagement.Infrastructure/"]
RUN dotnet restore "backend/Chubb.PolicyManagement.Api/Chubb.PolicyManagement.Api.csproj"
COPY . .
WORKDIR "/src/backend/Chubb.PolicyManagement.Api"
RUN dotnet build "Chubb.PolicyManagement.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Chubb.PolicyManagement.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Chubb.PolicyManagement.Api.dll"]

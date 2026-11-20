# Etapa 1: restaurar y publicar con el SDK de .NET 10
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Veterinaria.sln ./
COPY Veterinaria.Api/Veterinaria.Api.csproj Veterinaria.Api/
COPY Veterinaria.Application/Veterinaria.Application.csproj Veterinaria.Application/
COPY Veterinaria.Domain/Veterinaria.Domain.csproj Veterinaria.Domain/
COPY Veterinaria.Infrastructure/Veterinaria.Infrastructure.csproj Veterinaria.Infrastructure/
COPY Veterinaria.Domain.Tests/Veterinaria.Domain.Tests.csproj Veterinaria.Domain.Tests/
RUN dotnet restore Veterinaria.sln

COPY . .
RUN dotnet publish Veterinaria.Api/Veterinaria.Api.csproj -c Release -o /app/publish --no-restore

# Etapa 2: runtime ASP.NET Core 10.0
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Veterinaria.Api.dll"]

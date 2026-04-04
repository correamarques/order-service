# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS build

WORKDIR /build

# Copy solution and project files
COPY ["OrderService.slnx", "./"]
COPY ["OrderService.Domain/", "OrderService.Domain/"]
COPY ["OrderService.Application/", "OrderService.Application/"]
COPY ["OrderService.Infrastructure/", "OrderService.Infrastructure/"]
COPY ["OrderService.Api/", "OrderService.Api/"]

# Restore and build
RUN dotnet restore "OrderService.Api/OrderService.Api.csproj"
RUN dotnet build "OrderService.Api/OrderService.Api.csproj" -c Release --no-restore

# Publish stage
FROM build AS publish
RUN dotnet publish "OrderService.Api/OrderService.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine

# Install timezone data
RUN apk add --no-cache tzdata

WORKDIR /app

# Copy published output from publish stage
COPY --from=publish /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080

EXPOSE 8080

ENTRYPOINT ["dotnet", "OrderService.Api.dll"]

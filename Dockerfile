FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy all the projects
COPY ["src/OrderManagement.API/OrderManagement.API.csproj", "src/OrderManagement.API/"]
COPY ["src/OrderManagement.Application/OrderManagement.Application.csproj", "src/OrderManagement.Application/"]
COPY ["src/OrderManagement.Domain/OrderManagement.Domain.csproj", "src/OrderManagement.Domain/"]
COPY ["src/OrderManagement.Infrastructure/OrderManagement.Infrastructure.csproj", "src/OrderManagement.Infrastructure/"]

# Restore dependencies
RUN dotnet restore "src/OrderManagement.API/OrderManagement.API.csproj"

# Copy the remaining source code
COPY . .

# Build and publish the API
WORKDIR "/src/src/OrderManagement.API"
RUN dotnet publish "OrderManagement.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Generate the runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "OrderManagement.API.dll"]

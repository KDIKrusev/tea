# Base runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER app
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Use the .NET SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy central package management manifest (required before csproj restore for CPM)
COPY ["Directory.Packages.props", "./"]

# Copy project files
COPY ["VoyageEnergyAdvisor.WebApi/VoyageEnergyAdvisor.WebApi.csproj", "VoyageEnergyAdvisor.WebApi/"]
COPY ["VoyageEnergyAdvisor.App/VoyageEnergyAdvisor.csproj", "VoyageEnergyAdvisor.App/"]
COPY ["VoyageEnergyAdvisor.Core/VoyageEnergyAdvisor.Core.csproj", "VoyageEnergyAdvisor.Core/"]
COPY ["VoyageEnergyAdvisor.Data/VoyageEnergyAdvisor.Data.csproj", "VoyageEnergyAdvisor.Data/"]

# Restore dependencies
RUN dotnet restore "VoyageEnergyAdvisor.App/VoyageEnergyAdvisor.csproj"

# Copy source code
COPY . .

# Set working directory to main app
WORKDIR "/src/VoyageEnergyAdvisor.App"

# Build application
RUN dotnet build "VoyageEnergyAdvisor.csproj" -c Debug -o /app/build --no-restore

# Publish application
FROM build AS publish
RUN dotnet publish "VoyageEnergyAdvisor.csproj" -c Debug -o /app/publish --no-restore

# Final runtime image
FROM base AS final
WORKDIR /app

# Copy published output
COPY --from=publish /app/publish .

# Copy DefaultResources from build output
COPY --from=publish /app/build/DefaultResources /app/DefaultResources

# Set environment variables
ENV LD_LIBRARY_PATH="./runtimes/linux-x64/native:${LD_LIBRARY_PATH}"
ENV DOTNET_RUNNING_IN_CONTAINER=true

# Entry point
ENTRYPOINT ["dotnet", "VoyageEnergyAdvisor.dll"]

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY AcademicRecords.sln ./
COPY src/AcademicRecords.Shared/AcademicRecords.Shared.csproj src/AcademicRecords.Shared/
COPY src/AcademicRecords.Coordinator/AcademicRecords.Coordinator.csproj src/AcademicRecords.Coordinator/
RUN dotnet restore src/AcademicRecords.Coordinator/AcademicRecords.Coordinator.csproj
COPY src/ src/
RUN dotnet publish src/AcademicRecords.Coordinator/AcademicRecords.Coordinator.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "AcademicRecords.Coordinator.dll"]

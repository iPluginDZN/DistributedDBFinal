FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY AcademicRecords.sln ./
COPY src/AcademicRecords.Shared/AcademicRecords.Shared.csproj src/AcademicRecords.Shared/
COPY src/AcademicRecords.Site/AcademicRecords.Site.csproj src/AcademicRecords.Site/
RUN dotnet restore src/AcademicRecords.Site/AcademicRecords.Site.csproj
COPY src/ src/
RUN dotnet publish src/AcademicRecords.Site/AcademicRecords.Site.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "AcademicRecords.Site.dll"]

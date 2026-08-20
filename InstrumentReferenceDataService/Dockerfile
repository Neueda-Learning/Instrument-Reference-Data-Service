FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY ["InstrumentReferenceDataService/InstrumentReferenceDataService.csproj", "InstrumentReferenceDataService/"]
RUN dotnet restore "InstrumentReferenceDataService/InstrumentReferenceDataService.csproj"

COPY . .
WORKDIR "/src/InstrumentReferenceDataService"
RUN dotnet publish "InstrumentReferenceDataService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:5105
EXPOSE 5105

ENTRYPOINT ["dotnet", "InstrumentReferenceDataService.dll"]

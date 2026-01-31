FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY PSM-Download.sln ./
COPY PSM-Download/PSM-Download.csproj PSM-Download/
RUN dotnet restore
COPY PSM-Download/ PSM-Download/
RUN dotnet publish PSM-Download/PSM-Download.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "PSM-Download.dll"]

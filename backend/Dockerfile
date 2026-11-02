FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Rubrica.Api.csproj ./
RUN dotnet restore
COPY . ./
RUN dotnet publish -c Debug -o /out --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /out ./
ENV ASPNETCORE_HTTP_PORTS=3000
USER $APP_UID
EXPOSE 3000
ENTRYPOINT ["dotnet", "Rubrica.Api.dll"]

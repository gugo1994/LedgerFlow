FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

COPY BankingApp.Api/BankingApp.Api.csproj BankingApp.Api/

RUN dotnet restore BankingApp.Api/BankingApp.Api.csproj

COPY . .

RUN dotnet publish BankingApp.Api/BankingApp.Api.csproj \
    -c Release \
    -o /app/publish \
    --no-restore


FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "BankingApp.Api.dll"]
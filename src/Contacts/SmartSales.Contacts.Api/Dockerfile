FROM mcr.microsoft.com/dotnet/sdk:10.0.300-noble@sha256:c0790639332692a0d56cdd81ed581cfd24d040d9839764c138994866df89a3b6 AS build
WORKDIR /src
COPY global.json Directory.Build.props Directory.Packages.props ./
COPY dotnet-tools.json ./
COPY src/Contacts/SmartSales.Contacts.Api/SmartSales.Contacts.Api.csproj src/Contacts/SmartSales.Contacts.Api/packages.lock.json src/Contacts/SmartSales.Contacts.Api/
RUN dotnet restore src/Contacts/SmartSales.Contacts.Api/SmartSales.Contacts.Api.csproj --locked-mode
COPY src/Contacts/SmartSales.Contacts.Api/ src/Contacts/SmartSales.Contacts.Api/
RUN dotnet publish src/Contacts/SmartSales.Contacts.Api/SmartSales.Contacts.Api.csproj -c Release --no-restore -p:UseAppHost=false -o /out/app
RUN dotnet tool restore && dotnet tool run dotnet-ef migrations bundle \
    --project src/Contacts/SmartSales.Contacts.Api/SmartSales.Contacts.Api.csproj \
    --startup-project src/Contacts/SmartSales.Contacts.Api/SmartSales.Contacts.Api.csproj \
    --configuration Release --self-contained --target-runtime linux-x64 --output /out/migrate

FROM mcr.microsoft.com/dotnet/aspnet:10.0.8-noble@sha256:8c0b6857eab7b2aa57884c839bf4678414606bd7d17370f18a842ac5cf414711 AS runtime
WORKDIR /app
COPY --from=build /out/app/ ./
COPY --from=build /out/migrate ./migrate
ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "SmartSales.Contacts.Api.dll"]

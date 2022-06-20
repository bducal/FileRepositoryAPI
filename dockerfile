FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build-env
WORKDIR /app

####Copy everything
COPY . ./

# Restore as distinct layers
RUN dotnet restore
#Build and publish a release
#RUN dotnet dev-certs https
RUN dotnet publish -c Release -o out


##Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:6.0
WORKDIR /app
COPY --from=build-env /app/out .
#COPY --from=build-env /root/.dotnet/corefx/cryptography/x509stores/my/* /root/.dotnet/corefx/cryptography/x509stores/my/
#COPY --from=build-env /app/Traefik/certs/local.pfx /root/.dotnet/corefx/cryptography/x509stores/my/
#ENV ASPNETCORE_URLS=https://+:7004;http://+:5126;
ENV ASPNETCORE_URLS=http://+:88;
ENV ASPNETCORE_ENVIRONMENT="development"
#ENV ASPNETCORE_Kestrel__Certificates__Default__Path=/root/.dotnet/corefx/cryptography/x509stores/my/local.pfx
EXPOSE 88

ENTRYPOINT ["dotnet", "FileRepositoryAPI.dll"]
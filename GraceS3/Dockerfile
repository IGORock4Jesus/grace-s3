# https://hub.docker.com/_/microsoft-dotnet
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source

# copy csproj and restore as distinct layers
COPY ./GraceS3/GraceS3.csproj ./GraceS3/
COPY ./GraceS3.Common/GraceS3.Common.csproj ./GraceS3.Common/
RUN dotnet restore ./GraceS3/GraceS3.csproj

# copy everything else and build app
COPY ./GraceS3/ ./GraceS3/
COPY ./GraceS3.Common/ ./GraceS3.Common/
RUN dotnet publish ./GraceS3/GraceS3.csproj -c release -o /app --no-restore

# final stage/image test2
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app ./

RUN mkdir -p /var/lib/graces3 && chown app:app /var/lib/graces3
VOLUME /var/lib/graces3

EXPOSE 80
ENV ASPNETCORE_URLS=http://*:80

ENTRYPOINT ["dotnet", "GraceS3.dll"]
ARG PROJ=MyLab.LogAgent
ARG VER

# https://hub.docker.com/_/microsoft-dotnet
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG PROJ
WORKDIR /src

# copy csproj and restore as distinct layers
COPY . .
RUN dotnet restore

# copy everything else and build app
WORKDIR ./${PROJ}
RUN dotnet publish -c release -o /app --no-restore

# final stage/image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
ARG PROJ 
ARG VER
ENV PROJ_FILE=$PROJ
ENV LogAgent__AddProperties__agentver=$VER
WORKDIR /app
COPY --from=build /app /app
EXPOSE 8080

ENTRYPOINT ["sh", "-c", "dotnet /app/${PROJ_FILE}.dll"]
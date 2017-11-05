FROM microsoft/aspnetcore-build AS builder
ARG SOURCE_BRANCH
ARG SOURCE_COMMIT
ENV DOTNET_CLI_TELEMETRY_OPTOUT 1
WORKDIR /source
COPY *.sln .
RUN mkdir DarkStatsCore && mkdir DarkStatsCore.Data
COPY DarkStatsCore/*.csproj DarkStatsCore/
COPY DarkStatsCore.Data/*.csproj DarkStatsCore.Data/
RUN dotnet restore
COPY DarkStatsCore/package.json DarkStatsCore/
COPY DarkStatsCore/package-lock.json DarkStatsCore/
COPY DarkStatsCore/copypackages.* DarkStatsCore/
RUN cd DarkStatsCore && npm install
COPY . .
WORKDIR /source/DarkStatsCore
RUN node copypackages.js
RUN dotnet publish --output /app/ --configuration Release
WORKDIR /app
RUN echo Docker ${SOURCE_BRANCH} ${SOURCE_COMMIT} >BUILD_VERSION

FROM microsoft/aspnetcore:2
ENV TZ=America/New_York
WORKDIR /app
COPY --from=builder /app .
ENTRYPOINT ["dotnet", "DarkStatsCore.dll"]

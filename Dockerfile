FROM microsoft/aspnetcore-build AS builder
ARG CACHE_TAG
ARG SOURCE_COMMIT
WORKDIR /source
COPY *.sln .
RUN mkdir DarkStatsCore && mkdir DarkStatsCore.Data
COPY DarkStatsCore/*.csproj DarkStatsCore/
COPY DarkStatsCore.Data/*.csproj DarkStatsCore.Data/
RUN dotnet restore
COPY DarkStatsCore/package.json DarkStatsCore/
COPY DarkStatsCore/package-lock.json DarkStatsCore/
COPY DarkStatsCore/copypackages.* DarkStatsCore/
RUN cd DarkStatsCore && npm install && node copypackages.js
COPY . .
WORKDIR /source/DarkStatsCore
RUN dotnet publish --output /app/ --configuration Release
WORKDIR /app
RUN echo Docker ${CACHE_TAG} ${SOURCE_COMMIT} >BUILD_VERSION

FROM microsoft/aspnetcore:2
ENV TZ=America/New_York
WORKDIR /app
COPY --from=builder /app .
ENTRYPOINT ["dotnet", "DarkStatsCore.dll"]
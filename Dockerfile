FROM microsoft/dotnet:2.1-sdk AS builder
# set up node
ENV NODE_VERSION 8.9.4
ENV NODE_DOWNLOAD_SHA 21fb4690e349f82d708ae766def01d7fec1b085ce1f5ab30d9bda8ee126ca8fc
RUN curl -SL "https://nodejs.org/dist/v${NODE_VERSION}/node-v${NODE_VERSION}-linux-x64.tar.gz" --output nodejs.tar.gz \
    && echo "$NODE_DOWNLOAD_SHA nodejs.tar.gz" | sha256sum -c - \
    && tar -xzf "nodejs.tar.gz" -C /usr/local --strip-components=1 \
    && rm nodejs.tar.gz \
    && ln -s /usr/local/bin/node /usr/local/bin/nodejs
	
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

FROM microsoft/dotnet:2.1-aspnetcore-runtime
ENV TZ=America/New_York
WORKDIR /app
COPY --from=builder /app .
ENTRYPOINT ["dotnet", "DarkStatsCore.dll"]

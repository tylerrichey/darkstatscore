FROM microsoft/aspnetcore-build AS builder
WORKDIR /source
COPY *.sln .
RUN mkdir DarkStatsCore && mkdir DarkStatsCore.Data
COPY DarkStatsCore/*.csproj DarkStatsCore/
COPY DarkStatsCore.Data/*.csproj DarkStatsCore.Data/
RUN dotnet restore
COPY DarkStatsCore/.bowerrc DarkStatsCore/
COPY DarkStatsCore/bower.json DarkStatsCore/
RUN cd DarkStatsCore && bower install --config.interactive=false 
COPY . .
WORKDIR /source/DarkStatsCore
RUN dotnet publish --output /app/ --configuration Release

FROM microsoft/aspnetcore:2
ENV TZ=America/New_York
WORKDIR /app
COPY --from=builder /app .
ENTRYPOINT ["dotnet", "DarkStatsCore.dll"]
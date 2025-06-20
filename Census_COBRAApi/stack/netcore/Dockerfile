# Build runtime image
FROM microsoft/dotnet:aspnetcore-runtime
WORKDIR /app
COPY publish/ .
COPY appsettings.json .
ENTRYPOINT ["dotnet", "CobraComputeAPI.dll"]

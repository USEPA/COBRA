# Build runtime image
FROM microsoft/dotnet:aspnetcore-runtime
WORKDIR /app
COPY publish/ .
#COPY --from=build-env /app/out .
ENTRYPOINT ["dotnet", "CobraComputeAPI.dll"]
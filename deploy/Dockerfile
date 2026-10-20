FROM mcr.microsoft.com/dotnet/sdk:8.0
WORKDIR /src
COPY backend ./backend
RUN dotnet publish backend/src/BpmnProperties.Api/BpmnProperties.Api.csproj -c Release -o /app
WORKDIR /app
ENTRYPOINT ["dotnet", "BpmnProperties.Api.dll"]

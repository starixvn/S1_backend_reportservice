FROM hub.lumigame.com/base/dotnet/core/sdk:8.0 AS builder

COPY . .
WORKDIR ReportService/S1.Alpha.Api
RUN dotnet publish -c Release -o /app

FROM hub.lumigame.com/base/dotnet/core/sdk:8.0
WORKDIR /app
COPY --from=builder /app .
EXPOSE 5000

ENTRYPOINT ["dotnet", "S1.Alpha.Api.dll"]
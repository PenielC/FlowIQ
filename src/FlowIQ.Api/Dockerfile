FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY FlowIQ.slnx .
COPY src/FlowIQ.Api/FlowIQ.Api.csproj src/FlowIQ.Api/
COPY src/FlowIQ.Application/FlowIQ.Application.csproj src/FlowIQ.Application/
COPY src/FlowIQ.Domain/FlowIQ.Domain.csproj src/FlowIQ.Domain/
COPY src/FlowIQ.Infrastructure/FlowIQ.Infrastructure.csproj src/FlowIQ.Infrastructure/
COPY src/FlowIQ.Contracts/FlowIQ.Contracts.csproj src/FlowIQ.Contracts/
COPY src/FlowIQ.Tests/FlowIQ.Tests.csproj src/FlowIQ.Tests/
RUN dotnet restore src/FlowIQ.Api/FlowIQ.Api.csproj

COPY src/ src/
RUN dotnet publish src/FlowIQ.Api/FlowIQ.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
COPY --from=build /app .
ENTRYPOINT ["dotnet", "FlowIQ.Api.dll"]

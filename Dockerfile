FROM mcr.microsoft.com/dotnet/sdk:6.0 as build-env
WORKDIR /app
COPY Base.API/Base.API.csproj Base.API/
COPY Base.Service/Base.Service.csproj Base.Service/
COPY Base.Repository/Base.Repository.csproj Base.Repository/
RUN dotnet restore "Base.API/Base.API.csproj"
COPY . .
WORKDIR "/app/Base.API"
RUN dotnet publish Base.API.csproj -c Release -o ../out

FROM mcr.microsoft.com/dotnet/aspnet:6.0
WORKDIR /app
COPY --from=build-env /app/out .
ENTRYPOINT ["dotnet", "Base.API.dll"]
FROM microsoft/dotnet:aspnetcore-runtime AS base
WORKDIR /app

FROM microsoft/dotnet:sdk AS build
WORKDIR /src
COPY HTTP-2-server.sln ./
COPY lib/*.csproj ./lib/
COPY Hpack/*.csproj ./Hpack/
COPY Benchmark/*.csproj ./Benchmark/
COPY ExampleServer/*.csproj ./ExampleServer/
COPY UnitTesting/*.csproj ./UnitTesting/

RUN dotnet restore
COPY . .
WORKDIR /src/lib
RUN dotnet build -c Release -o /app

WORKDIR /src/Hpack
RUN dotnet build -c Release -o /app

WORKDIR /src/UnitTesting/
RUN dotnet build -c Release -o /app

WORKDIR /src/ExampleServer/
RUN dotnet build -c Release -o /app

WORKDIR /src/Benchmark
RUN dotnet build -c Release -o /app

FROM build AS publish
RUN dotnet publish -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "ExampleServer.dll"]
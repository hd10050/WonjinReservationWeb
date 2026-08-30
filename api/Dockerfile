# 멀티스테이지 — 빌드는 SDK, 실행은 aspnet 런타임 이미지에 publish 산출물만 담는다.
# (이전: SDK 이미지에서 `dotnet run` = Debug 구성 + 컨테이너 기동마다 빌드 + 컴파일러·ef 툴체인 상주)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src
COPY WonjinApi.csproj .
RUN dotnet restore
COPY . .
RUN dotnet publish WonjinApi.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0

WORKDIR /app
COPY --from=build /app .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "WonjinApi.dll"]

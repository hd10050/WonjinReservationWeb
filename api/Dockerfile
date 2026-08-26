FROM mcr.microsoft.com/dotnet/sdk:10.0

WORKDIR /src
COPY WonjinApi.csproj .
RUN dotnet restore
COPY . .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

CMD ["dotnet", "run", "--no-restore", "--no-launch-profile"]

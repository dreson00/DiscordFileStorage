FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build-env

WORKDIR /app

COPY *.sln ./
COPY FileStorage/*.csproj ./FileStorage/
COPY FileStorageTests/*.csproj ./FileStorageTests/
COPY FileUploadWeb/*.csproj ./FileUploadWeb/
COPY VideoProcessing/*.csproj ./VideoProcessing/

RUN dotnet restore

# Kopírování celého projektu a sestavení aplikace
COPY . ./
RUN dotnet publish -c Release -o out

# Vytvoření výsledného běhového obrazu
FROM mcr.microsoft.com/dotnet/aspnet:6.0
WORKDIR /app
COPY --from=build-env /app/out .

# Instalace ffmpeg
RUN apt-get update && apt-get install -y ffmpeg

WORKDIR /app/files
WORKDIR /app

# Exponování portů pro jednotlivé aplikace
EXPOSE 80

# Spuštění aplikací
ENTRYPOINT ["dotnet", "FileUploadWeb.dll"]
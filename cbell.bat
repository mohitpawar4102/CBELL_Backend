@echo off
echo Starting Microservices and Gateway...

:: Set the root directory
set ROOT_DIR=C:\Users\TUF Gaming\OneDrive\Desktop\Candent Projects\CBELL_Candent

:: Use full path to dotnet.exe (adjust if needed)
set DOTNET_PATH="C:\Program Files\dotnet\dotnet.exe"

:: Start the Authentication microservice with error handling
start "Authentication API" cmd /k "cd /d %ROOT_DIR%\Microservices\Authentication\Authentication.API && %DOTNET_PATH% run || (echo Failed to start Authentication API && pause)"

timeout /t 5 /nobreak

:: Start the ContentCreator microservice with error handling
start "ContentCreator API" cmd /k "cd /d %ROOT_DIR%\Microservices\ContentCreator\ContentCreator.API && %DOTNET_PATH% run || (echo Failed to start ContentCreator API && pause)"

timeout /t 5 /nobreak

:: Start the API Gateway last with error handling
start "API Gateway" cmd /k "cd /d %ROOT_DIR%\Gateway\APIGateway && %DOTNET_PATH% run || (echo Failed to start API Gateway && pause)"

echo All services have been started!
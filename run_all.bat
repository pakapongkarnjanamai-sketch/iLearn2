@echo off
echo Starting iLearn.API...
start "iLearn.API" dotnet run --project iLearn.API

echo Starting iLearn.Admin...
start "iLearn.Admin" dotnet run --project iLearn.Admin

echo Starting iLearn.User...
start "iLearn.User" dotnet run --project iLearn.User

echo Waiting 5 seconds for services to start...
timeout /t 5

echo Opening in Chrome...
start chrome http://localhost:5214/swagger
start chrome http://localhost:5126
start chrome http://localhost:5182

echo All projects started.

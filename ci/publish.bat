@echo off

set tag=ghcr.io/mylab-log/agent

IF [%1]==[] goto noparam

echo "Copy sources ..."
xcopy /E /I /Y ..\src .\src\

echo "Build image '%tag%:%1' and '%tag%:latest'..."
docker build -t %tag%:%1 -t %tag%:latest .

echo "Publish image '%1' ..."
docker push %tag%:%1

echo "Publish image 'latest' ..."
docker push %tag%:latest

goto done

:noparam
echo "Please specify image version"
goto done

:done
echo "Done!"
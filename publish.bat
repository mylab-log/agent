@echo off

IF [%1]==[] goto noparam

echo "Copy sources ..."
xcopy /E /I /Y ..\src .\src\

echo "Build image '%1' and 'latest'..."
docker build -t mylabtools/promtail:%1 -t mylabtools/promtail:latest .

echo "Publish image '%1' ..."
docker push mylabtools/promtail:%1

echo "Publish image 'latest' ..."
docker push mylabtools/promtail:latest

goto done

:noparam
echo "Please specify image version"
goto done

:done
echo "Done!"
echo off

IF [%1]==[] goto noparam

echo "Build image '%1' and 'latest'..."
docker build --build-arg VER=%1 -f ./Dockerfile -t ghcr.io/mylab-log/agent:%1 -t ghcr.io/mylab-log/agent:latest ../src

echo "Publish image '%1' ..."
docker push ghcr.io/mylab-log/agent:%1

echo "Publish image 'latest' ..."
docker push ghcr.io/mylab-log/agent:latest

goto done

:noparam
echo "Please specify image version"
goto done

:done
echo "Done!"
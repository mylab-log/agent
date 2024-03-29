@echo off

IF [%1]==[] goto noparam

echo "Build image '%1'"

docker build --build-arg FLUENT_BIT_VER=2.1.8-debug -t ghcr.io/mylab-log/agent:%1 -t ghcr.io/mylab-log/agent:latest ../src

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
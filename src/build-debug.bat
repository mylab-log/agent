@echo off

set tag=ghcr.io/mylab-log/agent

echo "Build image ..."
docker build --build-arg FLUENT_BIT_VER=2.1.8-debug -t mylab-log/agent:debug .
echo "Done!"
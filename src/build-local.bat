@echo off

set tag=ghcr.io/mylab-log/agent

echo "Build image ..."
docker build -t mylab-log/agent:local .
echo "Done!"
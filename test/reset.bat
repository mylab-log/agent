@echo off

curl -X DELETE "localhost:9201/logs-test?pretty"
docker restart logagent-test
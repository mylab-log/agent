@echo off

curl "http://localhost:9201/_search?pretty" -H "Content-Type: application/json" -d "{ \"query\": { \"match_all\": {} }}"
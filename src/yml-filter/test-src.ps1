$rawContent = Get-Content .\single.log
$content = $rawContent.Replace("`"","[quotes]")

# go run filter.go '{\"log\":\"Message: Exception when sending a subscription act to backoffice\",\"stream\":\"stdout\",\"attrs\":{\"log_format\":\"mylab\",\"tag\":\"infonot-subscription-backoffice-act-sender-task\",\"time\":\"2023-01-31T13:17:00.833746728Z\"}}'
go run filter.go $content
# tinygo run filter.go $content

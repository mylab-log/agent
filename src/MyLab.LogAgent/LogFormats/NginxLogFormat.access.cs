﻿using MyLab.LogAgent.Model;

namespace MyLab.LogAgent.LogFormats;

partial class NginxLogFormat
{
    static class AssessLogExtractor
    {
        public static bool Extract(string logText, LogProperties props, out string message)
        {
            if (
                !TryExtractRemoteAddress(logText, props, out var remoteAddress) &
                !TryExtractRequest(logText, props, out int endRequestIndex, out var request) &
                !TryExtractStatus(logText, props, endRequestIndex, out var status)
            )
            {
                props.Add(LogPropertyNames.ParsingFailedFlag, "true");
                props.Add(LogPropertyNames.ParsingFailureReason, "nginx-access-log-parser");

                message = logText;

                return false;
            }
            else
            {
                message = $"{remoteAddress}: {request} -> {status}";
                return true;
            }
        }

        static bool TryExtractStatus(string logText, LogProperties props, int endRequestIndex, out string status)
        {
            string? statusStr = null;

            if (endRequestIndex != -1 && logText.Length > endRequestIndex + 2)
            {
                int endStatusIndex = logText.IndexOf(' ', endRequestIndex + 2);
                if (endStatusIndex != -1)
                {
                    statusStr = logText.Substring(endRequestIndex + 2, endStatusIndex - (endRequestIndex + 2)).Trim();
                }
            }

            status = !string.IsNullOrWhiteSpace(statusStr)
                ? statusStr
                : NotFound;

            props.Add(StatusProp, status);

            return !string.IsNullOrWhiteSpace(statusStr);
        }

        static bool TryExtractRequest(string logText, LogProperties props, out int endRequestIndex, out string request)
        {
            endRequestIndex = -1;
            string? requestStr = null;

            var firstRequestDelimiterIndex = logText.IndexOf("] \"", StringComparison.InvariantCulture);
            if (firstRequestDelimiterIndex != -1 && logText.Length > firstRequestDelimiterIndex + 3)
            {
                var lastRequestDelimiterIndex = logText.IndexOf("\"", firstRequestDelimiterIndex + 3, StringComparison.InvariantCulture);
                if (lastRequestDelimiterIndex != -1)
                {
                    string originRequest = logText.Substring(firstRequestDelimiterIndex + 3, lastRequestDelimiterIndex - (firstRequestDelimiterIndex + 3));
                    requestStr = TryCleanupRequest(originRequest);

                    endRequestIndex = lastRequestDelimiterIndex;
                }
            }

            request = !string.IsNullOrWhiteSpace(requestStr)
                ? requestStr
                : NotFound;

            props.Add(RequestProp, request);

            return !string.IsNullOrWhiteSpace(requestStr);
        }

        static bool TryExtractRemoteAddress(string logText, LogProperties props, out string remoteAddress)
        {
            var remoteAddressDelimiterIndex = logText.IndexOf(' ');

            remoteAddress = remoteAddressDelimiterIndex != -1
                ? logText.Remove(remoteAddressDelimiterIndex)
                : "[not detected]";

            props.Add(RemoteAddressProp, remoteAddress);

            return remoteAddressDelimiterIndex != -1;
        }


    }

}
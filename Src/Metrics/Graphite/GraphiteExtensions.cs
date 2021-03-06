﻿using System;
using System.Configuration;
using Metrics.Graphite;
using Metrics.Logging;
using Metrics.Reports;

namespace Metrics
{
    public static class GraphiteExtensions
    {
        private static readonly ILog log = LogProvider.GetCurrentClassLogger();

        public static MetricsReports WithGraphite(this MetricsReports reports, Uri graphiteUri, TimeSpan interval)
        {
            if (graphiteUri.Scheme.ToLowerInvariant() == "net.tcp")
            {
                return reports.WithTCPGraphite(graphiteUri.Host, graphiteUri.Port, interval);
            }

            if (graphiteUri.Scheme.ToLowerInvariant() == "net.udp")
            {
                return reports.WithUDPGraphite(graphiteUri.Host, graphiteUri.Port, interval);
            }

            if (graphiteUri.Scheme.ToLowerInvariant() == "net.pickled")
            {
                return reports.WithPickledGraphite(graphiteUri.Host, graphiteUri.Port, interval);
            }

            throw new ArgumentException("Graphite uri scheme must be either net.tcp or net.udp or net.pickled (ex: net.udp://graphite.myhost.com:2003 )", nameof(graphiteUri));
        }

        public static MetricsReports WithPickledGraphite(this MetricsReports reports, string host, int port, TimeSpan interval, int batchSize = PickleGraphiteSender.DefaultPickleJarSize)
        {
            return reports.WithGraphite(new PickleGraphiteSender(host, port, batchSize), interval);
        }

        public static MetricsReports WithTCPGraphite(this MetricsReports reports, string host, int port, TimeSpan interval)
        {
            return reports.WithGraphite(new TcpGraphiteSender(host, port), interval);
        }

        public static MetricsReports WithUDPGraphite(this MetricsReports reports, string host, int port, TimeSpan interval)
        {
            return reports.WithGraphite(new UdpGraphiteSender(host, port), interval);
        }

        public static MetricsReports WithGraphite(this MetricsReports reports, GraphiteSender graphiteLink, TimeSpan interval)
        {
            return reports.WithReport(new GraphiteReport(graphiteLink), interval);
        }

        public static void WithGraphiteFromConfig(this MetricsReports reports)
        {
            try
            {
                var graphiteMetricsUri = ConfigurationManager.AppSettings["Metrics.Graphite.Uri"];
                var graphiteMetricsInterval = ConfigurationManager.AppSettings["Metrics.Graphite.Interval.Seconds"];

                if (!string.IsNullOrEmpty(graphiteMetricsUri) && !string.IsNullOrEmpty(graphiteMetricsInterval))
                {
                    Uri uri;
                    int seconds;
                    if (Uri.TryCreate(graphiteMetricsUri, UriKind.Absolute, out uri) && int.TryParse(graphiteMetricsInterval, out seconds) && seconds > 0)
                    {
                        reports.WithGraphite(uri, TimeSpan.FromSeconds(seconds));
                        log.Debug(() => $"Metrics: Sending Graphite reports to {uri} every {seconds} seconds.");
                    }
                    else
                    {
                        log.Error(() => "Invalid Metrics Configuration: Metrics.Graphite.Uri must be a valid absolute URI and Metrics.Graphite.Interval.Seconds must be an integer > 0");
                    }
                }
                else
                {
                    log.Warn(() => "Invalid Metrics Configuration: Metrics.Graphite.Uri must be a valid absolute URI and Metrics.Graphite.Interval.Seconds must be an integer > 0");
                }
            }
            catch (Exception x)
            {
                MetricsErrorHandler.Handle(x, "Error while configuring graphite from config");
            }
        }
    }
}

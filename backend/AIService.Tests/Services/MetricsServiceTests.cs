using AIService.Services;
using Xunit;
using FluentAssertions;

namespace AIService.Tests.Services;

public class MetricsServiceTests
{
    [Fact]
    public void RecordAiAnalysis_ShouldIncrementTotalAnalyses()
    {
        // Arrange
        var metricsService = new MetricsService();

        // Act
        metricsService.RecordAiAnalysis("gpt-4o", 100, true, 0.003m);

        // Assert
        metricsService.GetTotalAnalyses().Should().Be(1);
    }

    [Fact]
    public void RecordAiAnalysis_ShouldAccumulateCosts()
    {
        // Arrange
        var metricsService = new MetricsService();

        // Act
        metricsService.RecordAiAnalysis("gpt-4o", 100, true, 0.003m);
        metricsService.RecordAiAnalysis("gpt-4o", 110, true, 0.002m);

        // Assert
        metricsService.GetTotalCostUsd().Should().BeApproximately(0.005m, 0.0001m);
    }

    [Fact]
    public void GetMetricsText_ShouldReturnPrometheusFormat()
    {
        // Arrange
        var metricsService = new MetricsService();
        metricsService.RecordAiAnalysis("gpt-4o", 100, true, 0.003m);

        // Act
        var metrics = metricsService.GetMetricsText();

        // Assert
        metrics.Should().Contain("ai_analysis_total");
        metrics.Should().Contain("ai_response_duration_seconds");
        metrics.Should().Contain("ai_costs_usd_total");
        metrics.Should().Contain("# TYPE");
        metrics.Should().Contain("# HELP");
    }

    [Fact]
    public void RecordApiRequest_ShouldIncludeInMetrics()
    {
        // Arrange
        var metricsService = new MetricsService();

        // Act
        metricsService.RecordApiRequest("/api/ai/analyze", "POST", 200, 100);

        // Assert
        var metrics = metricsService.GetMetricsText();
        metrics.Should().Contain("api_requests_total");
        metrics.Should().Contain("/api/ai/analyze");
        metrics.Should().Contain("POST");
        metrics.Should().Contain("200");
    }

    [Fact]
    public void RecordAiAnalysis_MultipleRecords_ShouldAccumulate()
    {
        // Arrange
        var metricsService = new MetricsService();

        // Act
        for (int i = 0; i < 5; i++)
        {
            metricsService.RecordAiAnalysis("gpt-4o", 100 + i * 10, true, 0.001m);
        }

        // Assert
        metricsService.GetTotalAnalyses().Should().Be(5);
        metricsService.GetTotalCostUsd().Should().BeApproximately(0.005m, 0.0001m);
    }
}

using AIService.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AIService.Tests.Services;

public class ResponseParsingServiceTests
{
    private readonly ResponseParsingService _service;

    public ResponseParsingServiceTests()
    {
        var logger = new Mock<ILogger<ResponseParsingService>>();
        _service = new ResponseParsingService(logger.Object);
    }

    [Fact]
    public void ParseAIResponse_WithMalformedJson_ShouldReturnInvalid()
    {
        var correlationId = Guid.NewGuid().ToString();
        var response = _service.ParseAIResponse("{\"possibleConditions\": [\"Migraine\"", correlationId);

        response.IsValid.Should().BeFalse();
        response.ErrorMessage.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void ParseAIResponse_WithMissingRequiredFields_ShouldReturnInvalid()
    {
        var correlationId = Guid.NewGuid().ToString();
        var response = _service.ParseAIResponse("{\"possibleConditions\": [\"Migraine\"]}", correlationId);

        response.IsValid.Should().BeFalse();
        response.ErrorMessage.Should().Contain("confidenceScore");
    }

    [Fact]
    public void ParseAIResponse_WithValidJson_ShouldNormalizeAndReturnValidResponse()
    {
        var correlationId = Guid.NewGuid().ToString();
        var aiJson = """
        {
          "possibleConditions": [" Migraine ", "migraine", "Tension Headache"],
          "confidenceScore": 0.82,
          "recommendedSpecialty": "  Neurology  ",
          "urgency": "high",
          "disclaimer": "  This is not medical advice. Consult a healthcare professional.  "
        }
        """;

        var response = _service.ParseAIResponse(aiJson, correlationId);

        response.IsValid.Should().BeTrue();
        response.PossibleConditions.Should().HaveCount(2);
        response.RecommendedSpecialty.Should().Be("Neurology");
        response.Urgency.Should().Be("High");
        response.Disclaimer.Should().StartWith("This is not medical advice");
    }
}

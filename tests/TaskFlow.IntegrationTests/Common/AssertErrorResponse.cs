using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace TaskFlow.IntegrationTests.Common;

/// <summary>
/// Shared assertion helper for the standard error response shape produced
/// by <c>TaskFlow.API.Middleware.ValidationExceptionHandler</c>:
/// <code>
/// {
///   "status": 400,
///   "error": "VALIDATION_ERROR",
///   "message": "One or more validation errors occurred.",
///   "details": [ { "field": "title", "issue": "title required" } ]
/// }
/// </code>
/// Every <c>*_Returns400</c> integration test MUST use this helper instead
/// of asserting only the HTTP status code, per TASKFLOW-TEST-HARNESS.
/// </summary>
public static class AssertErrorResponse
{
    public sealed record ErrorDetail(string Field, string Issue);

    public sealed record ErrorBody(int Status, string Error, string Message, List<ErrorDetail> Details);

    /// <summary>
    /// Asserts the response is HTTP 400 with the standard error envelope,
    /// and that <paramref name="expectedDetails"/> is exactly (unordered)
    /// the set of field/issue pairs present in "details".
    /// </summary>
    public static async Task<ErrorBody> HasValidationErrorAsync(
        HttpResponseMessage response,
        params (string Field, string Issue)[] expectedDetails)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<ErrorBody>(new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        });

        Assert.NotNull(body);
        Assert.Equal(400, body!.Status);
        Assert.Equal("VALIDATION_ERROR", body.Error);
        Assert.Equal("One or more validation errors occurred.", body.Message);
        Assert.NotNull(body.Details);

        if (expectedDetails.Length > 0)
        {
            Assert.Equal(expectedDetails.Length, body.Details.Count);

            foreach (var (field, issue) in expectedDetails)
            {
                Assert.Contains(
                    body.Details,
                    d => d.Field == field && d.Issue == issue);
            }
        }

        return body;
    }
}

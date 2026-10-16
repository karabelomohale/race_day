using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace TestProject1;

public class UnitTest1 : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public UnitTest1(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false 
        });
    }

    private async Task<string> GetSessionCookieAsync(string email, string role)
    {
        var registerUrl = $"/api/auth/register?FirstName=Unit&LastName=Tester&Email={Uri.EscapeDataString(email)}&Password=Password123!&Role={role}&PhoneNumber=0821234567";
        await _client.PostAsync(registerUrl, null);

        var loginUrl = $"/api/auth/login?Email={Uri.EscapeDataString(email)}&Password=Password123!";
        var loginResponse = await _client.PostAsync(loginUrl, null);

        if (loginResponse.Headers.TryGetValues("Set-Cookie", out var cookies))
        {
            var authCookie = cookies.FirstOrDefault(c => c.StartsWith("RaceDay.AuthSession"));
            if (authCookie != null)
            {
                return authCookie.Split(';')[0];
            }
        }

        return string.Empty;
    }


    [Fact]
    public async Task Get_All_Events_Returns_200OK()
    {
        var response = await _client.GetAsync("/api/events");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Organiser_Can_Create_Event_Returns_201Created()
    {
        var cookie = await GetSessionCookieAsync("organiser_admin@test.co.za", "Organiser");

        var eventUrl = "/api/events?Title=RaceDay%20Open%2010k&EventType=Running&EventDate=2026-12-01&StartTime=07:00:00&Location=Soweto&Province=Gauteng&Description=Official%20qualifying%20event";
        var request = new HttpRequestMessage(HttpMethod.Post, eventUrl);
        if (!string.IsNullOrEmpty(cookie))
        {
            request.Headers.Add("Cookie", cookie);
        }

        var response = await _client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Register_New_User_Returns_201Created()
    {
        var uniqueEmail = $"athlete_{Guid.NewGuid():N}@test.co.za";
        var registerUrl = $"/api/auth/register?FirstName=Kagiso&LastName=Rabada&Email={Uri.EscapeDataString(uniqueEmail)}&Password=StrongPassword123!&Role=Participant&PhoneNumber=0839876543";

        var response = await _client.PostAsync(registerUrl, null);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Participant_Cannot_Create_Event_Returns_403Forbidden()
    {
        var cookie = await GetSessionCookieAsync("participant_runner@test.co.za", "Participant");

        var eventUrl = "/api/events?Title=Restricted%20Event&EventType=Running&EventDate=2026-11-20&StartTime=06:00:00&Location=Johannesburg&Province=Gauteng&Description=Access%20test";
        var request = new HttpRequestMessage(HttpMethod.Post, eventUrl);
        if (!string.IsNullOrEmpty(cookie))
        {
            request.Headers.Add("Cookie", cookie);
        }

        var response = await _client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Unauthenticated_User_Cannot_Create_Enrolment_Returns_401Unauthorized()
    {
        var enrolmentUrl = "/api/enrolments?CategoryId=1&EmergencyContact=0829990000";
        var request = new HttpRequestMessage(HttpMethod.Post, enrolmentUrl);

        var response = await _client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }


    [Fact]
    public async Task Register_With_Duplicate_Email_Returns_409Conflict()
    {
        var duplicateEmail = $"duplicate_{Guid.NewGuid():N}@test.co.za";
        var registerUrl = $"/api/auth/register?FirstName=Original&LastName=User&Email={Uri.EscapeDataString(duplicateEmail)}&Password=Password123!&Role=Participant&PhoneNumber=0821234567";

        var firstResponse = await _client.PostAsync(registerUrl, null);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var secondResponse = await _client.PostAsync(registerUrl, null);
        secondResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Enrol_In_NonExistent_Category_Returns_404NotFound()
    {
        var cookie = await GetSessionCookieAsync("participant_enrollee@test.co.za", "Participant");

        var enrolmentUrl = "/api/enrolments?CategoryId=99999&EmergencyContact=0829990000";
        var request = new HttpRequestMessage(HttpMethod.Post, enrolmentUrl);
        if (!string.IsNullOrEmpty(cookie))
        {
            request.Headers.Add("Cookie", cookie);
        }

        var response = await _client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
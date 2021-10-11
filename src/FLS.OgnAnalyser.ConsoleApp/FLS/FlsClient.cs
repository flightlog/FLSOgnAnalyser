using FLS.OgnAnalyser.ConsoleApp.Config;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace FLS.OgnAnalyser.ConsoleApp.FLS
{
    public class FlsClient
    {
        private readonly FlsOptions _options;
        private readonly ILogger _logger;
        public FlsClient(IOptions<FlsOptions> options, ILogger<FlsClient> logger)
        {
            _options = options.Value;
            _logger = logger;
        }

        public async Task SendTakeOffAsync(TakeOffDetails takeOffDetails)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    //Gets the token for a user (which is already in the database (registered))
                    string token = await GetTokenAsync(_options.Username, _options.Password);

                    //gets the access token value
                    var json = JObject.Parse(token);
                    var accessToken = json["access_token"].ToString();

                    //sets the Bearer authorization header with the access token value 
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                    var content = new ObjectContent(typeof(TakeOffDetails), takeOffDetails, new JsonMediaTypeFormatter());

                    var response =
                            await client.PostAsync(_options.BaseURL + "/flights/takeoff", content);

                    if (response.IsSuccessStatusCode == false)
                    {
                        _logger.LogError("Error while trying to send take off to FLS: {0}, Message: {1}",
                            response.StatusCode, response.ReasonPhrase);
                    }
                    else
                    {
                        _logger.LogInformation(
                            "Sent take off successfully. For details please have a look in FLS or log files.");
                    }
                }
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Error sending take off data to FLS client. Error-Message: {message}", ex.Message);
            }
        }

        public async Task SendLandingAsync(LandingDetails landingDetails)
        {
            try
            {
                using (var client = new HttpClient())
                {
                    //Gets the token for a user (which is already in the database (registered))
                    string token = await GetTokenAsync(_options.Username, _options.Password);

                    //gets the access token value
                    var json = JObject.Parse(token);
                    var accessToken = json["access_token"].ToString();

                    //sets the Bearer authorization header with the access token value 
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

                    var content = new ObjectContent(typeof(TakeOffDetails), landingDetails, new JsonMediaTypeFormatter());

                    var response =
                            await client.PostAsync(_options.BaseURL + "/flights/landing", content);

                    if (response.IsSuccessStatusCode == false)
                    {
                        _logger.LogError("Error while trying to send landing to FLS: {0}, Message: {1}",
                            response.StatusCode, response.ReasonPhrase);
                    }
                    else
                    {
                        _logger.LogInformation(
                            "Sent landing successfully. For details please have a look in FLS or log files.");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending landing data to FLS client. Error-Message: {message}", ex.Message);
            }
        }

        private async Task<string> GetTokenAsync(string userName, string password)
        {
            var pairs = new List<KeyValuePair<string, string>>
                        {
                            new KeyValuePair<string, string>( "grant_type", "password" ),
                            new KeyValuePair<string, string>( "username", userName ),
                            new KeyValuePair<string, string> ( "Password", password )
                        };

            var content = new FormUrlEncodedContent(pairs);

            using (var client = new HttpClient())
            {
                var response = await client.PostAsync(_options.BaseURL + "/Token", content);
                if (response.IsSuccessStatusCode == false)
                {
                    _logger.LogError("Could not get token from server. Error Statuscode: {0}", response.StatusCode);
                    throw new Exception(response.ReasonPhrase);
                }

                return await response.Content.ReadAsStringAsync();
            }
        }
    }
}

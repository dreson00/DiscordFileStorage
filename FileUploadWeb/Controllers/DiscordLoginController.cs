using System.Net.Http.Headers;
using DiscordBot;
using FileUploadWeb.Discord;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

namespace FileUploadWeb.Controllers
{
    public class DiscordLoginController : Controller
    {
        private readonly UserManager<DiscordUser> _userManager;
        private readonly SignInManager<DiscordUser> _signInManager;
        private readonly ILogger<DiscordLoginController> _logger;
        private readonly string _clientId;
        private readonly string _clientSecret;
        private readonly string _redirectUri;
        private readonly string _allowedServerId;

        public DiscordLoginController(
            UserManager<DiscordUser> userManager,
            SignInManager<DiscordUser> signInManager,
            IConfiguration config,
            ILogger<DiscordLoginController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
            _clientId = config.GetSection("Authentication").GetValue<string>("ClientId");
            _clientSecret = config.GetSection("Authentication").GetValue<string>("ClientSecret");
            _redirectUri = config.GetSection("Authentication").GetValue<string>("RedirectUri");
            _allowedServerId = config.GetSection("Authentication").GetValue<string>("AllowedServerId");

        }

        public ActionResult LoginWithDiscord()
        {
            var discordAuthUrl = $"https://discord.com/api/oauth2/authorize?client_id={_clientId}&redirect_uri={Uri.EscapeDataString(_redirectUri)}&response_type=code&scope=identify%20guilds";
            _logger.LogInformation("Redirecting a user to discord auth web.");
            return Redirect(discordAuthUrl);
        }

        [HttpGet]
        [Route("DiscordLoginCallback")]
        public async Task<ActionResult> DiscordCallback(string code)
        {
            _logger.LogInformation($"User redirected from discord auth with code {code}");
            var tokenUrl = "https://discord.com/api/oauth2/token";
            var userInfoUrl = "https://discord.com/api/users/@me";
            var guildsUrl = "https://discord.com/api/users/@me/guilds";

            // Výměna autorizačního kódu za přístupový token
            var httpClient = new HttpClient();
            var tokenRequestContent = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                {"client_id", _clientId},
                {"client_secret", _clientSecret},
                {"grant_type", "authorization_code"},
                {"code", code},
                {"redirect_uri", _redirectUri}
            });

            var tokenResponse = await httpClient.PostAsync(tokenUrl, tokenRequestContent);
            _logger.LogInformation("HTTP POST request sent to {tokenUrl}", tokenUrl);
            var tokenResponseString = await tokenResponse.Content.ReadAsStringAsync();
            _logger.LogInformation("Token response received: {tokenResponseString}", tokenResponseString);
            var tokenData = JsonConvert.DeserializeObject<Dictionary<string, string>>(tokenResponseString);
            _logger.LogInformation("Token data deserialized");
            var accessToken = tokenData?["access_token"];
            _logger.LogInformation("Access token retrieved: {accessToken}", accessToken);

            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            _logger.LogInformation("Authorization header set with access token");
            var userResponse = await httpClient.GetAsync(userInfoUrl);
            _logger.LogInformation("HTTP GET request sent to {userInfoUrl}", userInfoUrl);
            var userResponseString = await userResponse.Content.ReadAsStringAsync();
            _logger.LogInformation("User response received: {userResponseString}", userResponseString);
            var userData = JsonConvert.DeserializeObject<DiscordUser>(userResponseString);
            _logger.LogInformation("User data deserialized");

            var guildsResponse = await httpClient.GetAsync(guildsUrl);
            _logger.LogInformation("HTTP GET request sent to {guildsUrl}", guildsUrl);
            var guildsResponseString = await guildsResponse.Content.ReadAsStringAsync();
            _logger.LogInformation("Guilds response received: {guildsResponseString}", guildsResponseString);
            var guildsData = JsonConvert.DeserializeObject<List<DiscordGuild>>(guildsResponseString);
            _logger.LogInformation("Guilds data deserialized");


            if (guildsData == null || userData == null || guildsData.All(guild => guild.Id != _allowedServerId))
            {
                _logger.LogWarning($"Unsuccessful login attempt from user {userData?.UserName}.");
                return RedirectToAction("Index", "Home");
            }

            var existingUser = await _userManager.Users.FirstOrDefaultAsync(user => user.DiscordId == userData!.Id);
            if (existingUser != null)
            {
                await _signInManager.SignInAsync(existingUser, isPersistent: false);
                _logger.LogInformation($"User {existingUser.UserName} successfully logged in.");
                return RedirectToAction("Index", "Home");
            }
            else
            {
                var newUser = new DiscordUser()
                {
                    DiscordId = userData!.Id,
                    UserName = userData.UserName
                };

                var result = await _userManager.CreateAsync(newUser);
                if (result.Succeeded)
                {
                    await _signInManager.SignInAsync(newUser, isPersistent: false);
                    _logger.LogInformation($"New user {newUser.UserName} successfully created and logged in.");
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    _logger.LogWarning($"New user {newUser.UserName} unsuccessfully created.");
                    return RedirectToAction("Error", "Home");
                }
            }
        }

        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            _logger.LogInformation($"New user {User.Identity?.Name} successfully logged out.");
            return RedirectToAction("Index", "Home");
        }
    }
}

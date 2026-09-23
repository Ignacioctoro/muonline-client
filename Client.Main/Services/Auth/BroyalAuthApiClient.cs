using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Client.Main.Services.Auth
{
    public sealed class BroyalAuthApiClient
    {
        private static readonly HttpClient HttpClient = new()
        {
            Timeout = TimeSpan.FromSeconds(15)
        };

#if ANDROID
        private const string BaseUrl = "http://100.100.149.96:5097";
#else
        private const string BaseUrl = "http://100.100.149.96:5097";
#endif

        public async Task<CaptchaChallengeResponse> CreateCaptchaChallengeAsync(
            CancellationToken cancellationToken = default)
        {
            using var response = await HttpClient.PostAsync(
                $"{BaseUrl}/api/captcha/challenge",
                content: null,
                cancellationToken);

            response.EnsureSuccessStatusCode();

            var result =
                await response.Content.ReadFromJsonAsync<CaptchaChallengeResponse>(
                    cancellationToken: cancellationToken);

            return result
                ?? throw new InvalidOperationException(
                    "La API devolvió una respuesta de CAPTCHA vacía.");
        }

        public async Task<CaptchaStatusResponse> GetCaptchaStatusAsync(
            Guid challengeId,
            CancellationToken cancellationToken = default)
        {
            using var response = await HttpClient.GetAsync(
                $"{BaseUrl}/api/captcha/challenge/{challengeId}",
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return new CaptchaStatusResponse
                {
                    Success = false,
                    Verified = false,
                    Used = false
                };
            }

            var result =
                await response.Content.ReadFromJsonAsync<CaptchaStatusResponse>(
                    cancellationToken: cancellationToken);

            return result ?? new CaptchaStatusResponse();
        }

        public async Task<RegisterResponse> RegisterAsync(
            string username,
            string email,
            string password,
            Guid challengeId,
            CancellationToken cancellationToken = default)
        {
            var request = new RegisterRequest
            {
                Username = username,
                Email = email,
                Password = password,
                ChallengeId = challengeId
            };

            using var response = await HttpClient.PostAsJsonAsync(
                $"{BaseUrl}/api/auth/register",
                request,
                cancellationToken);

            RegisterResponse? result = null;

            try
            {
                result =
                    await response.Content.ReadFromJsonAsync<RegisterResponse>(
                        cancellationToken: cancellationToken);
            }
            catch
            {
                // Si la API devuelve un error sin JSON válido,
                // entregamos un mensaje genérico más abajo.
            }

            if (result != null)
            {
                return result;
            }

            return new RegisterResponse
            {
                Success = false,
                Message = response.StatusCode switch
                {
                    System.Net.HttpStatusCode.TooManyRequests =>
                        "Has realizado demasiados intentos. Espera un momento.",

                    System.Net.HttpStatusCode.BadRequest =>
                        "Los datos enviados no son válidos.",

                    System.Net.HttpStatusCode.Conflict =>
                        "El usuario o correo ya está registrado.",

                    _ =>
                        "No fue posible crear la cuenta."
                }
            };
        }
    }

    public sealed class CaptchaChallengeResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("challengeId")]
        public Guid ChallengeId { get; set; }

        [JsonPropertyName("verifyUrl")]
        public string VerifyUrl { get; set; } = string.Empty;
    }

    public sealed class CaptchaStatusResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("verified")]
        public bool Verified { get; set; }

        [JsonPropertyName("used")]
        public bool Used { get; set; }
    }

    public sealed class RegisterRequest
    {
        [JsonPropertyName("username")]
        public string Username { get; set; } = string.Empty;

        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("password")]
        public string Password { get; set; } = string.Empty;

        [JsonPropertyName("challengeId")]
        public Guid ChallengeId { get; set; }
    }

    public sealed class RegisterResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;
    }
}
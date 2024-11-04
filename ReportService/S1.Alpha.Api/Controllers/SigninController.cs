using Microsoft.AspNetCore.Mvc;
using S1.Alpha.Api.Helper;
using S1.Alpha.Api.Services;
using S1.Alpha.Api.ViewModels;
namespace S1.Alpha.Api.Controllers;

public class SsoEncrypt
{
    public string? ReferenceId { get; set; }
    public long? Timestamp { get; set; }
}

[Route("api/[controller]")]
[ApiController]
public class SigninController : ControllerBase
{
    private readonly IUserService _userService;

    public SigninController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpPost]
    public async Task<SsoModel> GetSsoUrl(
        IConfiguration configuration,
        [FromQuery] SsoSignInRequest request,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(request.ReferenceId))
        {
            var key = configuration["Aes:Key"];
            var iv = configuration["Aes:Iv"];
            var isExitsUser = await _userService.CheckExistsUser(string.Empty, request.ReferenceId);
            if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(iv) && isExitsUser)
            {
                var url = configuration["BackOfficeUrl"];
                var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                var token = await EncryptHelper.EncryptMessage(key, iv, new SsoEncrypt { ReferenceId = request.ReferenceId, Timestamp = timestamp});
                var returlUrl = $"{url}/login?referenceId={request.ReferenceId}&token={Convert.ToHexString(token)}";
                return new SsoModel { ReturnUrl = returlUrl };  
            }
        }

        return new SsoModel { ReturnUrl = "" };
    }
}

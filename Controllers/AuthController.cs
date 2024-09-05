using Microsoft.AspNetCore.Mvc;
using Repository.Portal;
using Shared;
using Shared.Model;


namespace API.Controllers.Portal
{
    [ApiController]
    [ApiVersion("1.0")]
    [ApiExplorerSettings(GroupName = "portal")]
    [Route("api/{v:apiVersion}/auth")]
    public class AuthController : ControllerBase
    {
        private IAuth _authRepository;
        public AuthController(IAuth authRepository)
        {
            _authRepository = authRepository;
        }
        [HttpPost("signin")]
        public async Task<Response> SignIn(LoginDto loginDto) =>
            await _authRepository.SignIn(loginDto);

        [HttpPost("update-password")]
        public async Task<Response> UpdatePassword(UpdatePasswordDto updatePassword) =>
            await _authRepository.UpdatePassword(updatePassword, HttpContext.UserProfile());

        [HttpPost("forgot-password")]
        public async Task<Response> ForgotPassword(ForgotPasswordDto forgotPassword) =>
            await _authRepository.ForgotPassword(forgotPassword);
        [HttpPost("reset-password")]
        public async Task<Response> ResetPassword(ForgotPasswordDto resetPassword) =>
            await _authRepository.ResetPassword(resetPassword);
    }
}
